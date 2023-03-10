using Azure.Core;
using Azure.Identity;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AADAuthLib
{
    public class AADConfig
    {
        /// <summary>
        /// ApplicationId(ClientId)
        /// </summary>
        public string? ApplicationId { get; set; }

        /// <summary>
        /// TenantId
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// 使用証明書のIssuer
        /// </summary>
        public string? CertificateIssuer { get; set; }
    }

    public class AADAuthService
    {
        private readonly AADConfig Config;

        public Uri AuthorityEndpoint
        {
            get
            {
                return new Uri($"https://login.microsoftonline.com/{Config.TenantId}");
            }
        }

        public AADAuthService(AADConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// AD認証用証明書の検索
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private X509Certificate2 lookupCertificate()
        {
            // 「個人」ストアを開く
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            // 有効期限内の証明書を絞り込み
            X509Certificate2Collection col_all = store.Certificates;
            X509Certificate2Collection col_date = col_all.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

            // Issuer で絞り込み
            X509Certificate2Collection col_issuer = col_date.Find(X509FindType.FindByIssuerName, Config.CertificateIssuer!, false);

            if (col_issuer.Count == 0)
            {
                throw new ArgumentException("Certificate not found.");
            }
            return col_issuer[0];
        }

        /// <summary>
        /// Azure.Core用 TokenCredential 取得
        /// </summary>
        /// <returns></returns>
        public TokenCredential GetTokenCredential()
        {
            return new ClientCertificateCredential(
                    Config.TenantId,
                    Config.ApplicationId,
                    lookupCertificate(),
                    new TokenCredentialOptions() { AuthorityHost = AuthorityEndpoint });
        }

        /// <summary>
        /// アクセストークン取得
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetAccessTokenAsync(string scope)
        {
            var cts = new CancellationTokenSource();

            var accessToken = await GetTokenCredential().
                GetTokenAsync(new TokenRequestContext(new[] { scope }), cts.Token);

            return accessToken.Token;
        }

    }
}
