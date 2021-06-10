﻿using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace key_vault_dotnet_authentication
{
    class KeyVaultAuthSample
    {
        class Settings
        {
            public Settings(string clientID, string clientSecret, string tenantID, string subscriptionID, string clientOID, string resourceGroupName, string azureLocation)
            {
                ClientID = clientID;
                ClientSecret = clientSecret;
                TenantID = tenantID;
                SubscriptionID = subscriptionID;
                ClientOID = clientOID;
                ResourceGroupName = resourceGroupName;
                AzureLocation = azureLocation;
            }

            public static Settings FromEnvironment()
            {
                return new Settings(
                    Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"),
                    Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET"),
                    Environment.GetEnvironmentVariable("AZURE_TENANT_ID"),
                    Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID"),
                    Environment.GetEnvironmentVariable("AZURE_CLIENT_OID"),
                    Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP") ?? "azure-sample-group",
                    Environment.GetEnvironmentVariable("AZURE_LOCATION") ?? "westus"
                );
            }

            public string ClientID { get; private set; }
            public string ClientSecret { get; private set; }
            public string TenantID { get; private set; }
            public string SubscriptionID { get; private set; }
            public string ClientOID { get; private set; }
            public string ResourceGroupName { get; private set; }
            public string AzureLocation { get; private set; }
        }

        // Set up sample's settings using environment variables
        private Settings settings = Settings.FromEnvironment();

        private async Task authUsingADALCallbackAsync(string vaultBaseURL)
        {
            Console.WriteLine("Authenticating to Key Vault using ADAL callback.");
            Console.WriteLine(vaultBaseURL);

            var credential = new DefaultAzureCredential();
            SecretClient secretClient = new SecretClient(new Uri(vaultBaseURL), credential);

            // Set and get an example secret
            await secretClient.SetSecretAsync("test-secret", "test-secret-value-using-adal");

            await foreach (SecretProperties secret in secretClient.GetPropertiesOfSecretsAsync())
            {
                if (secret.Managed) continue;

                // Getting a disabled secret will fail, so skip disabled secrets.
                if (!secret.Enabled.GetValueOrDefault())
                {
                    continue;
                }

                KeyVaultSecret secretWithValue = await secretClient.GetSecretAsync(secret.Name);
                Console.WriteLine("Retrieved \"test-secret\", value=\"" + secretWithValue.Value + "\"");
            }
        }

        public async Task RunAsync()
        {
            // Generate a random name for a new vault
            String vaultName = Util.generateRandomVaultId();

            // Set up credentials to access management plane to set up example key vault
            AzureCredentials credentials = new AzureCredentialsFactory().FromServicePrincipal(
                settings.ClientID,
                settings.ClientSecret,
                settings.TenantID,
                AzureEnvironment.AzureGlobalCloud
            );

            // Ensure that our sample resource group exists. 
            Console.WriteLine("Creating sample resource group");
            ResourceManagementClient resourceMgmtClient = new ResourceManagementClient(credentials);
            resourceMgmtClient.SubscriptionId = settings.SubscriptionID;

            await resourceMgmtClient.ResourceGroups.CreateOrUpdateAsync(settings.ResourceGroupName, new ResourceGroupInner(settings.AzureLocation));

            // Create the sample key vault.
            Console.WriteLine("Creating sample Key Vault - " + vaultName);

            // Set up the params for the API call
            VaultCreateOrUpdateParametersInner createParams = new VaultCreateOrUpdateParametersInner(
                settings.AzureLocation,
                new VaultProperties(
                    Guid.Parse(settings.TenantID),
                    new Microsoft.Azure.Management.KeyVault.Fluent.Models.Sku(SkuName.Standard),

                    // Create an access policy granting all secret permissions to our sample's service principal
                    new[] { new AccessPolicyEntry(Guid.Parse(settings.TenantID), settings.ClientOID, new Permissions(null, new[] { "all" })) }
                )
            );

            KeyVaultManagementClient kvMgmtClient = new KeyVaultManagementClient(credentials);
            kvMgmtClient.SubscriptionId = settings.SubscriptionID;

            VaultInner vault = await kvMgmtClient.Vaults.CreateOrUpdateAsync(settings.ResourceGroupName, vaultName, createParams);

            // Now demo authentication to the vault using ADAL
            // Add a delay to wait for KV DNS record to be created. See: https://github.com/Azure/azure-sdk-for-node/pull/1938
            System.Threading.Thread.Sleep(15000);

            await authUsingADALCallbackAsync(vault.Properties.VaultUri);
        }

        public static void Main(String[] args)
        {
            Console.WriteLine("Azure Key Vault Authentication Sample");
            KeyVaultAuthSample sample = new KeyVaultAuthSample();

            // Any synchronous invocation of asyncronous code (such as KV API calls) should follow the below pattern of calling ConfigureAwait(false) to avoid deadlock
            // Please see https://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
            Task.Run( () => sample.RunAsync().ConfigureAwait(false).GetAwaiter().GetResult() );

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }
    }
}
