using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;

namespace key_vault_dotnet_authentication
{
    class KeyVaultAuthSample
    {
        // Callback for authenticating to Key Vault using ADAL
        public async Task<string> ADALGetTokenCallback(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(Properties.Settings.Default.AZURE_CLIENT_ID, Properties.Settings.Default.AZURE_CLIENT_SECRET);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);
            if(result == null)
            {
                throw new InvalidOperationException("Failed to retrieve access token for Key Vault");
            }

            return result.AccessToken;
        }

        private async Task authUsingADALCallback(string vaultBaseURL)
        {
            Console.WriteLine("Authenticating to Key Vault using ADAL callback.");
            Console.WriteLine(vaultBaseURL);
            // Set up a KV Client with the above ADAL callback method
            KeyVaultClient kvClient = new KeyVaultClient(ADALGetTokenCallback);

            // Set and get an example secret
            await kvClient.SetSecretAsync(vaultBaseURL, "test-secret", "test-secret-value-using-adal");
            SecretBundle s = await kvClient.GetSecretAsync(vaultBaseURL, "test-secret");
            Console.WriteLine("Retrieved \"test-secret\", value=\"" + s.Value + "\"");
        }

        public async Task run()
        {
            // Generate a random name for a new vault
            String vaultName = Util.generateRandomVaultId();

            // Set up credentials to access management plane to set up example key vault
            AzureCredentials credentials = new AzureCredentialsFactory().FromServicePrincipal(
                Properties.Settings.Default.AZURE_CLIENT_ID,
                Properties.Settings.Default.AZURE_CLIENT_SECRET,
                Properties.Settings.Default.AZURE_TENANT_ID,
                AzureEnvironment.AzureGlobalCloud
            );


            // Ensure that our sample resource group exists. 
            Console.WriteLine("Creating sample resource group");
            ResourceManagementClient resourceMgmtClient = new ResourceManagementClient(credentials);
            resourceMgmtClient.SubscriptionId = Properties.Settings.Default.AZURE_SUBSCRIPTION_ID;

            await resourceMgmtClient.ResourceGroups.CreateOrUpdateAsync(Properties.Settings.Default.AZURE_RESOURCE_GROUP, new ResourceGroupInner(Properties.Settings.Default.AZURE_LOCATION));

            // Create the sample key vault.
            Console.WriteLine("Creating sample Key Vault - " + vaultName);

            // Set up the params for the API call
            VaultCreateOrUpdateParametersInner createParams = new VaultCreateOrUpdateParametersInner(
                Properties.Settings.Default.AZURE_LOCATION,
                new VaultProperties(
                    Guid.Parse(Properties.Settings.Default.AZURE_TENANT_ID),
                    new Microsoft.Azure.Management.KeyVault.Fluent.Models.Sku(SkuName.Standard),

                    // Create an access policy granting all secret permissions to our sample's service principal
                    new[] { new AccessPolicyEntry(Guid.Parse(Properties.Settings.Default.AZURE_TENANT_ID), Properties.Settings.Default.AZURE_CLIENT_OID, new Permissions(null, new[] { "all" })) }
                )
            );

            KeyVaultManagementClient kvMgmtClient = new KeyVaultManagementClient(credentials);
            kvMgmtClient.SubscriptionId = Properties.Settings.Default.AZURE_SUBSCRIPTION_ID;

            VaultInner vault = await kvMgmtClient.Vaults.CreateOrUpdateAsync(Properties.Settings.Default.AZURE_RESOURCE_GROUP, vaultName, createParams);

            // Now demo authentication to the vault using ADAL
            // Add a delay to wait for KV DNS record to be created. See: https://github.com/Azure/azure-sdk-for-node/pull/1938
            System.Threading.Thread.Sleep(5000);

            await authUsingADALCallback(vault.Properties.VaultUri);
        }

        public static void Main(String[] args)
        {
            Console.WriteLine("Azure Key Vault Authentication Sample");
            KeyVaultAuthSample sample = new KeyVaultAuthSample();
            sample.run().Wait();

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }
    }
}
