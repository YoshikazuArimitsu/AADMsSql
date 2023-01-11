using AADAuthLib;
using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StorageExample
{
    public class StorageConfig
    {
        public string StorageAccount { get; set; }
        public string Container { get; set; }

        public AADConfig AADConfig {get;set;}

    }

    public class BearerTokenCredential : TokenCredential
    {
        private string _bearerToken;

        public BearerTokenCredential(string baererToken)
        {
            _bearerToken = baererToken;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(_bearerToken, DateTimeOffset.UtcNow.AddHours(1));
        }
    }

    internal class Program
    {
        private static void Main()
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", true)
                .Build();

            var config = Configuration.Get<StorageConfig>();

            var auth = new AADAuthService(config.AADConfig);


            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}",
                                                        config.StorageAccount,
                                                        config.Container);
            BlobContainerClient client = new BlobContainerClient(new Uri(containerEndpoint),
                auth.GetTokenCredential());
            
            client.CreateIfNotExists();
        }
    }
}
