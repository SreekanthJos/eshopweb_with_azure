using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace OrderItemsReserver
{
    public static class OrderItemsReserver
    {
        [FunctionName("OrderItemsReserver")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext executionContext)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var fileName = "order_" + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".json";
            var filePath = executionContext.FunctionAppDirectory +"/" + fileName;

            File.WriteAllText(filePath, body);
            //using (var fs = File.Create(fileName))
            //{
            //    //JsonSerializer serializer = new JsonSerializer();
            //    //serializer.Formatting = Formatting.Indented;
            //    //serializer.Serialize(file, body);
            //}
            await CreateBlob(filePath, executionContext);
            log.LogInformation($"{fileName} uploaded successfully..");

            //string name = req.Query["name"];



            string responseMessage =
                 $"This HTTP triggered function executed successfully. {fileName} uploaded successfully..";
               
            return new OkObjectResult(responseMessage);
        }
        private static async Task<string> CreateBlob(string filePath,ExecutionContext executionContext)
        {
            try
            {
                var config = new ConfigurationBuilder()
                           .SetBasePath(executionContext.FunctionAppDirectory)
                           .AddJsonFile("local.settings.json", true, true)
                           .AddEnvironmentVariables().Build();

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config["AzureWebJobsStorage"]);
                CloudBlobClient client = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = client.GetContainerReference("orderscontainer");
                await cloudBlobContainer.CreateIfNotExistsAsync();
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference("orders" + DateTime.Now.ToString("yyyyMMddHHmmssffff"));

                await cloudBlockBlob.UploadFromFileAsync(filePath);

                return cloudBlockBlob.Uri.AbsoluteUri;
            }
            catch(Exception ex)
            {
                throw (ex);
            }
        }

        

    }
}
