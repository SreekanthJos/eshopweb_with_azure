using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.Extensions.Configuration;

namespace OrderDeliveryApp
{
    public static class OrderDelivery
    {
        private static Database database;
        private static CosmosClient cosmosClient;
        private static Container container;
        private static string databaseId = "OrderDeliverDB";
        private static string containerId = "OrderDeliverContainer";

        [FunctionName("OrderDelivery")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log,ExecutionContext executionContext)
        {
            try
            {
                var config = new ConfigurationBuilder()
                         .SetBasePath(executionContext.FunctionAppDirectory)
                         .AddJsonFile("local.settings.json", true, true)
                         .AddEnvironmentVariables().Build();

                cosmosClient = new CosmosClient(config["CosmosDBEndpoint"]);
                log.LogInformation("Order Delivery function execution started");
                await CreateDataBaseAsync();
                log.LogInformation("Database created or connected");
                await CreateContainerAsync();
                log.LogInformation("Container created or connected");

                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var res = JsonConvert.DeserializeObject<Order>(body);
                log.LogInformation("Order Delivery data deserialized");
                res.TotalPrice = res.Total();
                ItemResponse<Order> response = await container.CreateItemAsync<Order>(res, new PartitionKey(res.BuyerId));
                log.LogInformation("Order Delivery record created successfully");

                string responseMessage = $"Delivery record for Order {res.Id} created successfully";

                return new OkObjectResult(responseMessage);
            }
            catch(Exception ex)
            {
                throw (ex);
            }
        }
        private static async Task CreateDataBaseAsync()
        {
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);

        }
        private static async Task CreateContainerAsync()
        {
            container = await database.CreateContainerIfNotExistsAsync(containerId, "/BuyerId");

        }
        private static async Task<bool> AddItemstoContainerAsync(HttpRequest req)
        {
           
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var res = JsonConvert.DeserializeObject<Order>(body);
                ItemResponse<Order> response = await container.CreateItemAsync<Order>(res, new PartitionKey(res.BuyerId));
                return true;
            }
            catch(Exception ex)
            {
                throw (ex);
            }

        }
    }
}
