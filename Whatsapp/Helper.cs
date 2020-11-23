using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Whatsapp
{
    public class Helper
    {
        public async Task<string> GetKeyVaultValue(string URI)
        {
            try
            {
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                var secret = await keyVaultClient.GetSecretAsync(URI).ConfigureAwait(false);

                return secret.Value;
            }
            catch (KeyVaultErrorException keyVaultException)
            {
                return keyVaultException.Message;
            }
        }

    }
}
