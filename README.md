# AADMsSql

AD 認証 Azure SQL Database を構築するのとクライアントアプリ(.NET Core + SqlClient)で AD 認証を使い、
DB にアクセスするサンプル

## terraform

### 構築内容

- リソースグループ
- SQL Database
- Storage Account/Blob コンテナ
- DB/Storage アクセス用 AzureAD アプリケーション・サービスプリンシパル
- サービスプリンシパル用の権限(Blob 共同作成者)割り当て

### 備考

DB 管理者の AD ユーザは別途作成しておく必要あり。

terraform で AzureAD にアプリ追加・RBAC 割り当てを行う為、実行プリンシパルに `アプリケーション管理者・所有者` を追加しておく必要がある。

## インフラ構築後の作業

### SQL Database にサービスプリンシパルでアクセスできるユーザを作成

構築後、DB 管理者で SQL Database にログインし、

```
CREATE USER [mssql_app] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [mssql_app];
```

でサービスプリンシパルにアクセス権限を与える(権限はもうちょっと絞るべきかもしれない)

### サービスプリンシパル用の証明書作成

```
$ openssl genrsa 2048 > private.key
$ openssl req -new -x509 -days 3650 -key private.key -sha512 -out cert.crt
$ openssl pkcs12 -export -out cert.p12 -inkey private.key -in cert.crt
```

cert.p12, server.crt を作成する。

server.crt をサービスプリンシパルの証明書としてアップロード。

cert.p12 は DB にアクセスするマシンの個人用証明書ストアにインポートする。

### C#アプリ側設定

```json
{
  "AADConfig": {
    "ApplicationId": "cb6850e1-fcea-415b-93aa-da99ff2aff98",
    "TenantId": "da4e5376-e590-44ac-b4f4-35c36df9aecb",
    "CertificateIssuer": "yarimit"
  }
}
```

ApplicationId, TenantId に利用するサービスプリンシパルの ID、CertificateIssuer に証明書ストアから証明書を特定する為の Issuer を書いておく。

AzureAD サービスプリンシパルに証明書で認証を行い、AD トークンを受け取って SQL Database にサービスプリンシパルユーザでログインして DB/Storage を操作する。

# 参考ページ

https://ayuina.github.io/ainaba-csa-blog/sqldb-aad-authentication/

https://learn.microsoft.com/ja-jp/azure/azure-sql/database/authentication-aad-configure?view=azuresql&tabs=azure-powershell

https://learn.microsoft.com/ja-jp/azure/storage/blobs/authorize-access-azure-active-directory

https://learn.microsoft.com/ja-jp/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor
