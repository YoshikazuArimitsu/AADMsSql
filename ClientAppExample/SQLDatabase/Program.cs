using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace SQLDbExample
{
    internal class Program
    {
        private static void Main()
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", true)
                .Build();

            var dbConfig = Configuration.Get<DbConfig>();

            using (var db = new BloggingContext(dbConfig))
            {
                // Note: This sample requires the database to be created before running.
                // Create
                Console.WriteLine("Inserting a new blog");
                db.Add(new Blog { Url = "http://blogs.msdn.com/adonet" });
                db.SaveChanges();

                // Read
                Console.WriteLine("Querying for a blog");
                var blog = db.Blogs
                    .OrderBy(b => b.BlogId)
                    .First();

                // Update
                //Console.WriteLine("Updating the blog and adding a post");
                //blog.Url = "https://devblogs.microsoft.com/dotnet";
                //blog.Posts.Append(
                //    new Post { Title = "Hello World", Content = "I wrote an app using EF Core!" });
                //db.SaveChanges();

                // Delete
                Console.WriteLine("Delete the blog");
                db.Remove(blog);
                db.SaveChanges();
            }
        }
    }
}
