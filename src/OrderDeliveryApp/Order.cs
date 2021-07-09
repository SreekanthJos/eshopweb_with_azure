
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate
{
    public class Order 
    {
        [JsonProperty(PropertyName = "id")]
        public Guid id;
        public int Id { get; set; }
        public string BuyerId { get; set; }
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.Now;
        public Address ShipToAddress { get; set; }
        
        public List<OrderItem> OrderItems { get; set; }
         public decimal TotalPrice { get; set; }
        public decimal Total()
        {
            var total = 0m;
            foreach (var item in OrderItems)
            {
                total += item.UnitPrice * item.Units;
            }
            return total;
        }
    }
}
