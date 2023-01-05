using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EFMigrateExample
{
    public class DbConfig
    {
        /// <summary>
        /// 接続文字列
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// ApplicationId(ClientId)
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// TenantId
        /// </summary>
        public string TenantId { get; set; }
        
        /// <summary>
        /// 使用証明書のIssuer
        /// </summary>
        public string CertificateIssuer { get; set; }

    }

    public class BloggingContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }

        private DbConfig Config { get; set; }

        public BloggingContext(DbConfig config)
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
            X509Certificate2Collection col_issuer = col_date.Find(X509FindType.FindByIssuerName, "Arimitsu", false);

            if (col_issuer.Count == 0)
            {
                throw new ArgumentException("Certificate not found.");
            }
            return col_issuer[0];
        }

        /// <summary>
        /// AD認証実行・アクセストークンの取得
        /// </summary>
        /// <returns></returns>
        private string getAccessToken()
        {
            // Getting Access Token
            var scopes = new[] { "https://database.windows.net/.default" };
            var app = ConfidentialClientApplicationBuilder
                        .Create(Config.ApplicationId)
                        .WithAuthority(new Uri($"https://login.microsoftonline.com/{Config.TenantId}"))
                        .WithCertificate(lookupCertificate())
                        .Build();
            var authresult = app.AcquireTokenForClient(scopes).ExecuteAsync().Result;
            return authresult.AccessToken;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            SqlConnection connection = new SqlConnection();


            connection.ConnectionString = Config.ConnectionString;
            connection.AccessToken = getAccessToken();

            options.UseSqlServer(connection);
        }
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string Url { get; set; }

        [NotMapped]
        public IEnumerable<Post> Posts { get; set; }
    }

    public class Post
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }
}
