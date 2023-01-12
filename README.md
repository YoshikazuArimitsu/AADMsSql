# AAD Auth SQL Database/Storage Example

AAD 証明書認証で SQLDatabase/Storage を構築するサンプルと、C# クライアントアプリで接続するサンプル。

## 環境構築

terraform で実験に必要な環境を構築します。

### 構築内容

- リソースグループ
- SQL Database
- Storage Account/Blob コンテナ
- DB/Storage アクセス用 AzureAD アプリケーション・サービスプリンシパル
- サービスプリンシパル用の権限(Blob 共同作成者)割り当て

### 備考

DB 管理者の AD ユーザは別途作成しておく必要あり。

terraform で AzureAD にアプリ追加・RBAC 割り当てを行う為、実行プリンシパルに `アプリケーション管理者・所有者` を追加しておく必要がある。

### インフラ構築後の作業

#### SQL Database にサービスプリンシパルでアクセスできるユーザを作成

構築後、DB 管理者で SQL Database にログインし、

```
CREATE USER [mssql_app] FROM EXTERNAL PROVIDER;
ALTER ROLE db_owner ADD MEMBER [mssql_app];
```

でサービスプリンシパルにアクセス権限を与える(権限はもうちょっと絞るべきかもしれない)

#### サービスプリンシパル用の証明書作成

```
$ openssl genrsa 2048 > private.key
$ openssl req -new -x509 -days 3650 -key private.key -sha512 -out cert.crt
$ openssl pkcs12 -export -out cert.p12 -inkey private.key -in cert.crt
```

cert.p12, server.crt を作成する。

server.crt をサービスプリンシパルの証明書としてアップロード。

cert.p12 は DB にアクセスするマシンの個人用証明書ストアにインポートする。

## アプリの実行

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

### もう少し詳しい事

![構成図](%E6%A7%8B%E6%88%90%E5%9B%B3.drawio.png)

Azure AD にアプリケーション・サービスプリンシパルを登録しておく。

サービスプリンシパルに SQL Database/Storage にアクセスする許可を与えておく。

- SQL Database は AD 認証 ON とサービスプリンシパル名の SQL Server ユーザを作る
- Storage はサブスクリプションの `Blob共同作成者` のロールをサービスプリンシパルに割り当てておく。

証明書を作成し、サービスプリンシパルの認証用に設定する。

(\*) サービスプリンシパルへの認証にはシークレット文字列 or 証明書を利用できる。
シークレットは単なる文字列なのでアプリに埋め込む必要がある。証明書はアプリに埋め込む事もできるが、OS の証明書ストアに登録する事でシークレットよりはいくらか安全な運用(インストール時に登録してエクスポートは不可にするとか)ができる。

クライアントアプリは図のフローで認証を行い、アクセストークンを利用して SQL Database/Storage へのアクセスを行う。

# 参考ページ

[Azure SQL での Azure AD 認証を構成して管理する](https://learn.microsoft.com/ja-jp/azure/azure-sql/database/authentication-aad-configure?view=azuresql&tabs=azure-powershell)

[Azure Active Directory を使用して BLOB へのアクセスを認可する](https://learn.microsoft.com/ja-jp/azure/storage/blobs/authorize-access-azure-active-directory)

[Azure 組み込みロール](https://learn.microsoft.com/ja-jp/azure/role-based-access-control/built-in-roles#storage-blob-data-contributor)

[Azure SQL Database の Azure Active Directory 認証](https://ayuina.github.io/ainaba-csa-blog/sqldb-aad-authentication/)
