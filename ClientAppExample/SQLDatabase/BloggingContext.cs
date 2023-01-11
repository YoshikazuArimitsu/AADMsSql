using AADAuthLib;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace SQLDbExample
{
    public class DbConfig
    {
        /// <summary>
        /// 接続文字列
        /// </summary>
        public string ConnectionString { get; set; }

        public AADConfig AADConfig { get; set; }
    }

    public class BloggingContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }

        private DbConfig Config { get; set; }

        private AADAuthService AADAuth { get; set; }

        public BloggingContext(DbConfig config)
        {
            Config = config;
            AADAuth = new AADAuthService(Config.AADConfig);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            SqlConnection connection = new SqlConnection();
            connection.ConnectionString = Config.ConnectionString;
            connection.AccessToken = AADAuth.GetAccessTokenAsync("https://database.windows.net/.default").Result;
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
