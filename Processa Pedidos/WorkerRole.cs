using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using ClassLibrary1;
using Microsoft.Azure.NotificationHubs;

namespace Processa_Pedidos
{
    public class WorkerRole : RoleEntryPoint
    {
        static CloudQueue cloudQueue;
        private static NotificationHubClient _hub;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("Processa Pedidos is running");

            //var connectionString = ConfigurationManager.ConnectionStrings["Azure Storage Account Demo Primary"].ConnectionString;
            //var connectionString = "UseDevelopmentStorage=true";
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=pucblackfriday;AccountKey=wp2XRtwddph4Xe1mMetQoWGDRUbVfk2iM/BaaIMcJ9jdenHohX+3mtfvDPNaRKI2MuscLCADZVf5AhgIfVgl5A==";
            CloudStorageAccount cloudStorageAccount;

            if (!CloudStorageAccount.TryParse(connectionString, out cloudStorageAccount))
            {
                //Assert.Fail("Expected connection string 'Azure Storage Account Demo Primary' to be a valid Azure Storage Connection String.");
            }

            var cloudQueueClient = cloudStorageAccount.CreateCloudQueueClient();
            cloudQueue = cloudQueueClient.GetQueueReference("demoqueue");

            // Note: Usually this statement can be executed once during application startup or maybe even never in the application.
            //       A queue in Azure Storage is often considered a persistent item which exists over a long time.
            //       Every time .CreateIfNotExists() is executed a storage transaction and a bit of latency for the call occurs.
            cloudQueue.CreateIfNotExists();

            string defaultFullSharedAccessSignature = "Endpoint=sb://pucblackfridayapp.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=Fah+JgdJe+IddFD3Gk7NdbkJH0tEYdZKpsMgDSW6OOk=";
            string hubName = "pedidos";
            _hub = NotificationHubClient.CreateClientFromConnectionString(defaultFullSharedAccessSignature, hubName);


            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("Processa Pedidos has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Processa Pedidos is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("Processa Pedidos has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                var cloudQueueMessage = cloudQueue.GetMessage();

                if (cloudQueueMessage != null)
                {
                    Pedido p = JsonConvert.DeserializeObject<Pedido>(cloudQueueMessage.AsString);

                    await SendNotificationAsync($"{p.Id}:realizado com sucesso", "lucasmdo");

                    //Assert.AreEqual(MessageText + AdditionalMessage, cloudQueueMessage.AsString);
                    //Assert.AreEqual(2, cloudQueueMessage.DequeueCount);

                    cloudQueue.DeleteMessage(cloudQueueMessage);
                }
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }

        public async Task<bool> SendNotificationAsync(string message, string to_tag)
        {
            var user = "lucasmdo";
            string[] userTag = new string[1];
            userTag[0] = to_tag;

            NotificationOutcome outcome = null;

            // Android
            var notif = "{ \"data\" : {\"message\":\"" + "From " + user + ": " + message + "\"}}";
                    outcome = await _hub.SendGcmNativeNotificationAsync(notif, userTag);

            if (outcome != null)
            {
                if (!((outcome.State == NotificationOutcomeState.Abandoned) ||
                    (outcome.State == NotificationOutcomeState.Unknown)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
