using ClassLibrary1;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api.Controllers
{

    public class ValuesController : ApiController
    {
        static CloudQueue cloudQueue;
        //static int count = 0;

        public ValuesController()
        {
            //var connectionString = ConfigurationManager.ConnectionStrings["Azure Storage Account Demo Primary"].ConnectionString;
            //var connectionString = "UseDevelopmentStorage=true";
            var connectionString = "DefaultEndpointsProtocol=https;AccountName=pucblackfriday;AccountKey=wp2XRtwddph4Xe1mMetQoWGDRUbVfk2iM/BaaIMcJ9jdenHohX+3mtfvDPNaRKI2MuscLCADZVf5AhgIfVgl5A==";
            CloudStorageAccount cloudStorageAccount;

            if (!CloudStorageAccount.TryParse(connectionString, out cloudStorageAccount))
            {
                Trace.TraceError("Erro");
                //Assert.Fail("Expected connection string 'Azure Storage Account Demo Primary' to be a valid Azure Storage Connection String.");
            }

            var cloudQueueClient = cloudStorageAccount.CreateCloudQueueClient();
            cloudQueue = cloudQueueClient.GetQueueReference("demoqueue");

            // Note: Usually this statement can be executed once during application startup or maybe even never in the application.
            //       A queue in Azure Storage is often considered a persistent item which exists over a long time.
            //       Every time .CreateIfNotExists() is executed a storage transaction and a bit of latency for the call occurs.
            cloudQueue.CreateIfNotExists();
        }
        // GET api/values
        //public /*IEnumerable<string>*/ string Get()
        //{
        //    //return new string[] { "value1", "value2" };
        //    return $"LucasMDO:{++count}";

        //}

        public Pedido Get()
        {
            try
            {
                var cloudQueueMessage = cloudQueue.GetMessage();
                return JsonConvert.DeserializeObject<Pedido>(cloudQueueMessage.AsString);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"GET error - 1 - {ex.Message}");
                return null;
            }           
        }

        // GET api/values/5
        //public string Get(int id)
        //{
        //    return "value";
        //}
        public Pedido Get(string id)
        {
            Pedido p = new Pedido();
            CloudQueueMessage cloudQueueMessage;

            do
            {
                cloudQueueMessage = cloudQueue.GetMessage(TimeSpan.FromSeconds(2));

                if (cloudQueueMessage != null)
                    p = JsonConvert.DeserializeObject<Pedido>(cloudQueueMessage.AsString);
            }
            while (p != null && p.Id != id);

            return p;
        }

        // POST api/values
        //public void Post([FromBody]string value)
        //{
        //    var message = new CloudQueueMessage(value);

        //    cloudQueue.AddMessage(message);
        //}

        // POST api/values
        public string Post([FromBody]Pedido value)
        {
            try
            {
                Pedido p = new Pedido();
                p.Customer = value.Customer;
                p.Items = value.Items;
                p.ShippingFee = value.ShippingFee;

                var json = JsonConvert.SerializeObject(p);

                var message = new CloudQueueMessage(json);

                cloudQueue.AddMessage(message);

                return value.Id;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"POST error - 3 - {ex.Message}");
                return $"POST error - 3 - {ex.Message}";
            }
        }

        // PUT api/values/5
        //public void Put(int id, [FromBody]string value)
        //{
        //}
        public void Put(string id, [FromBody]Pedido value)
        {
            Pedido p = new Pedido();
            CloudQueueMessage cloudQueueMessage;

            //GET message by id
            do
            {
                cloudQueueMessage = cloudQueue.GetMessage(TimeSpan.FromSeconds(2));

                p = JsonConvert.DeserializeObject<Pedido>(cloudQueueMessage.AsString);
            }
            while (p.Id != id || cloudQueueMessage != null);

            //Found message, update fields that are valid (in memory)
            if (p != null)
            {
                if (value.Customer != null)
                    p.Customer = value.Customer;

                p.ShippingFee = value.ShippingFee;

                if (value.Items != null)
                {
                    p.Items.Clear();
                    foreach (string item in value.Items)
                    {
                        p.Items.Add(item);
                    }
                }
            }

            //Serialize it back
            var json = JsonConvert.SerializeObject(p);

            //Actually update it
            cloudQueueMessage.SetMessageContent(json);
            cloudQueue.UpdateMessage(cloudQueueMessage, TimeSpan.FromSeconds(1), MessageUpdateFields.Visibility | MessageUpdateFields.Content);

        }

        // DELETE api/values/5
        //public void Delete(int id)
        //{
        //}

        public void Delete(string id)
        {
            Pedido p = new Pedido();
            CloudQueueMessage cloudQueueMessage;

            //GET message by id
            do
            {
                cloudQueueMessage = cloudQueue.GetMessage(TimeSpan.FromSeconds(2));

                p = JsonConvert.DeserializeObject<Pedido>(cloudQueueMessage.AsString);
            }
            while (p.Id != id || cloudQueueMessage != null);

            //Found message, delete it
            if (p != null)
                cloudQueue.DeleteMessage(cloudQueueMessage);
        }
    }
}
