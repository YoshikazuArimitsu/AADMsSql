using Azure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AADAuthLib
{
    public interface IAADAuthService
    {
        Task<string> GetAccessTokenAsync(string scope);
        TokenCredential GetTokenCredential();

    }
}
