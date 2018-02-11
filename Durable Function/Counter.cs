using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Crm.Sdk;
using Microsoft.Crm.SdkTypeProxy;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.ServiceModel.Description;
using System.Threading.Tasks;

namespace VSSample
{
    public static class Counter
    {
        [FunctionName("Counter")]
        public static async Task Run(
            [OrchestrationTrigger] DurableOrchestrationContext counterContext,
            TraceWriter log)
        {
            int numberOfExecutions = 0;
            try
            {
                numberOfExecutions = counterContext.GetInput<int>();
                log.Info($"********{counterContext.InstanceId}: Current counter state is {numberOfExecutions}. isReplaying: {counterContext.IsReplaying} Waiting for next operation.**************");

                log.Info($"*********{counterContext.InstanceId}: Call activity ExistFile from {numberOfExecutions}*************");
                var existsFile = await counterContext.CallActivityAsync<bool>("ExistFile", numberOfExecutions.ToString());

                if (existsFile)
                {
                    log.Info($"*********{counterContext.InstanceId}: EXISTS FILE {numberOfExecutions}.json *************");

                    log.Info($"*********{counterContext.InstanceId}: Call activity AddCRM from {numberOfExecutions}*************");
                    await counterContext.CallActivityAsync("AddCRM", numberOfExecutions.ToString());

                    log.Info($"*********Add element to queeue *************");
                    await counterContext.CallActivityAsync("AddQueueTrigger", numberOfExecutions.ToString());
                    log.Info($"*********END element to queeue *************");
                }
                else
                {
                    log.Info($"*********{counterContext.InstanceId}: NO EXIST FILE {numberOfExecutions}.json *************");
                }

                log.Info($"*********Return {counterContext.InstanceId}: FINISH from {numberOfExecutions}*************");
            }
            catch (Exception ex)
            {
                log.Error($"**********ERROR General execution: {numberOfExecutions} -  {counterContext.IsReplaying} - {counterContext.InstanceId} *********", ex.InnerException != null ? ex.InnerException : ex);                
                if (!counterContext.IsReplaying)
                {
                    log.Info($"**********RETRY execution: {numberOfExecutions} - {counterContext.InstanceId} *********");
                    counterContext.ContinueAsNew(numberOfExecutions);
                }
            }
        }

        [FunctionName("AddCRM")]
        public static async Task AddCRM(
           [ActivityTrigger] string numberOfExecution,
           TraceWriter log)
        {
            try
            {
                log.Info($"C# Queue trigger function processed: {numberOfExecution}");               

                var client = CreateOrganizationService();

                log.Info($"\n************* Connection opened {numberOfExecution} ******************");

                var requestWithResults = new ExecuteMultipleRequest()
                {
                    // Assign settings that define execution behavior: continue on error, return responses.  
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    },
                    // Create an empty organization request collection. 
                    Requests = new OrganizationRequestCollection()
                };

                log.Info($"\n************* START GetCollectionOfEntities {numberOfExecution} ******************");
                var input = await GetCollectionOfEntitiesToUpdate(numberOfExecution, log);
                log.Info($"\n************* END GetCollectionOfEntities {numberOfExecution} ******************");

                foreach (var entity in input.Entities)
                {
                    var createRequest = new Microsoft.Xrm.Sdk.Messages.CreateRequest { Target = entity };
                    requestWithResults.Requests.Add(createRequest);                   
                }

                log.Info($"\n************* Begin Execution {numberOfExecution} ******************");
                var result = (ExecuteMultipleResponse)client.Execute(requestWithResults);

                foreach (var responseItem in result.Responses)
                {
                    if (responseItem.Fault != null)
                    {
                        log.Info($"\n************* FAIL : {responseItem.Response.Results} ******************");
                    }

                }

                log.Info($"\n************* END execute CRM {numberOfExecution} ******************");
            }
            catch (Exception ex)
            {
                log.Error($"\n************* ERROR execute CRM {numberOfExecution} ******************", ex);
                throw;
            }
        }

        [FunctionName("AddQueueTrigger")]
        [return: Queue("operation2", Connection = "AzureWebJobsStorage")]
        public static string AddQueueTrigger(
          [ActivityTrigger] string numberOfExecution,
          TraceWriter log)
        {
            try
            {
                var nextNumber = Convert.ToInt32(numberOfExecution);
                nextNumber = nextNumber + 2;
                log.Info($"Add Element To queue: {nextNumber}");
                return nextNumber.ToString();
            }
            catch (Exception ex)
            {
                log.Error($"\n************* ERROR execute CRM {numberOfExecution} ******************", ex);
                return "0";
            }
        }

        [FunctionName("ExistFile")]
        public static bool ExistFile(
        [ActivityTrigger] string numberOfExecution,
        TraceWriter log)
        {
            try
            {
                log.Info($"\n************* START Verify File in Blob {numberOfExecution} ******************");
                var container = InitializeBlobContainer("bancsabadell");
                container.CreateIfNotExists(BlobContainerPublicAccessType.Container);

                var blockBlob = container.GetBlockBlobReference($"{numberOfExecution}.json");

                if (blockBlob.Exists())
                {
                    log.Info($"\n************* END EXISTS Verify File in Blob {numberOfExecution} ******************");
                    return true;
                }

                log.Info($"\n************* END NO EXISTS Verify File in Blob {numberOfExecution} ******************");
                return false;
            }
            catch (Exception ex)
            {
                log.Error($"\n************* ERROR Verify File in Blob {numberOfExecution} ******************", ex);
                throw;
            }
        }

        public static async Task<List<account>> DownloadBlob(string numberOfExecution, TraceWriter log)
        {
            List<account> accounts = new List<account>();

            try
            {
                log.Info($"**************Init DownloadBlob file {numberOfExecution}******************");

                var container = InitializeBlobContainer("bancsabadell");
                container.CreateIfNotExists(BlobContainerPublicAccessType.Container);

                var blockBlob = container.GetBlockBlobReference($"{numberOfExecution}.json");
                using (var memoryStream = new MemoryStream())
                {
                    await blockBlob.DownloadToStreamAsync(memoryStream);
                    var file = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                    accounts = JsonConvert.DeserializeObject<List<account>>(file);
                }
                log.Info($"****************END DownloadBlob file {numberOfExecution}********************");
                return accounts;
            }
            catch (Exception ex)
            {
                log.Error($"\n************* ERROR download blob ******************", ex);
                throw ex;
            }
        }

        private static CloudBlobContainer InitializeBlobContainer(string containerName)
        {
            var storageConnectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            return container;
        }

        private static async Task<EntityCollection> GetCollectionOfEntitiesToUpdate(string numberOfExecution, TraceWriter log)
        {
            EntityCollection collection = new EntityCollection()
            {
                EntityName = "account"
            };

            var accounts = await DownloadBlob(numberOfExecution, log);
            
            foreach (var item in accounts)
            {
                var entity = new Entity("account");
                entity["name"] = item.name;

                collection.Entities.Add(entity);
            }

            return collection;
        }

        private static OrganizationServiceProxy CreateOrganizationService()
        {           

            var discoveryUri = new Uri("https://xxxx.api.crm4.dynamics.com/XRMServices/2011/Organization.svc");
            var userCredentials = new ClientCredentials();
            userCredentials.UserName.UserName = "yourUsernam@xxx.onmicrosoft.com";
            userCredentials.UserName.Password = "xxxx";
            
            var discoveryConfiguration = ServiceConfigurationFactory.CreateConfiguration<IDiscoveryService>(discoveryUri);
            var userResponseWrapper = discoveryConfiguration.Authenticate(userCredentials);            
            var servConf = ServiceConfigurationFactory.CreateConfiguration<IOrganizationService>(discoveryUri);            
            var orgServiceProxy = new OrganizationServiceProxy(servConf, userResponseWrapper)
            {
                Timeout = new TimeSpan(24, 0, 0)              
            };

            return orgServiceProxy;
        }
    }
}
