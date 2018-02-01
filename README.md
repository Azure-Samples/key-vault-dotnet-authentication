---
services: key-vault
platforms: dotnet
author: dadesber
---

# Authentication sample for Azure Key Vault using the Azure Node SDK

This sample repo demonstrates how to connect and authenticate to an Azure Key Vault vault. 
To do so, it first uses the Key Vault Management Client to create a vault.
The Key Vault client is then used to authenticate to the vault and set/retrieve a sample secret. 


## How to run this sample

1. If you don't already have it, get and install Visual Studio. 

2. Clone the repo.

   ```
   git clone https://github.com/Azure-Samples/key-vault-dotnet-authentication.git key-vault
   ```

3. Create an Azure service principals, using one of the following:
   - [Azure CLI](https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal-cli/),
   - [PowerShell](https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal/)
   - [Azure Portal](https://azure.microsoft.com/documentation/articles/resource-group-create-service-principal-portal/). 

    This service principal is to run the sample on your azure account.

4. Set the variables in App.config using the information from the service principal that you created.

   ```
   AZURE_SUBSCRIPTION_ID={your subscription id}
   AZURE_CLIENT_ID={your client id}
   AZURE_CLIENT_SECRET={your client secret}
   AZURE_TENANT_ID={your tenant id as a GUID}
   AZURE_CLIENT_OID={Object id of the service principal}
   ```

5. Open the sample solution in Visual Studio. Build and run. 

## References and further reading

- [Azure SDK for .NET](https://github.com/Azure/azure-sdk-for-net)
- [Azure KeyVault Documentation](https://azure.microsoft.com/en-us/documentation/services/key-vault/)
- [Key Vault REST API Reference](https://msdn.microsoft.com/en-us/library/azure/dn903609.aspx)
- [Manage Key Vault using CLI](https://azure.microsoft.com/en-us/documentation/articles/key-vault-manage-with-cli/)
- [Storing and using secrets in Azure](https://blogs.msdn.microsoft.com/dotnet/2016/10/03/storing-and-using-secrets-in-azure/)
