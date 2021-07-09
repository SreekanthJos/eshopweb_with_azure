namespace Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate
{
    public class OrderItem 
    {
        public int Id { get; set; }

        public CatalogItemOrdered ItemOrdered { get; set; }
        public decimal UnitPrice { get; set; }
        public int Units { get; set; }

       
       
    }
}
