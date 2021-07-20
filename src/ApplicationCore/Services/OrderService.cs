using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Services
{
    public class OrderService : IOrderService
    {
        private readonly IAsyncRepository<Order> _orderRepository;
        private readonly IUriComposer _uriComposer;
        private readonly IAsyncRepository<Basket> _basketRepository;
        private readonly IAsyncRepository<CatalogItem> _itemRepository;

        public OrderService(IAsyncRepository<Basket> basketRepository,
            IAsyncRepository<CatalogItem> itemRepository,
            IAsyncRepository<Order> orderRepository,
            IUriComposer uriComposer)
        {
            _orderRepository = orderRepository;
            _uriComposer = uriComposer;
            _basketRepository = basketRepository;
            _itemRepository = itemRepository;
        }

        public async Task CreateOrderAsync(int basketId, Address shippingAddress)
        {
            var basketSpec = new BasketWithItemsSpecification(basketId);
            var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

            Guard.Against.NullBasket(basketId, basket);
            Guard.Against.EmptyBasketOnCheckout(basket.Items);

            var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
            var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

            var items = basket.Items.Select(basketItem =>
            {
                var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
                var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
                var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
                return orderItem;
            }).ToList();

            var order = new Order(basket.BuyerId, shippingAddress, items);
          
            await _orderRepository.AddAsync(order);
             await UploadOrders(items);
            //await CreateDeliveryForOrder(order);
        }
               
        private async Task CreateDeliveryForOrder(Order order)
        {
            //var json = JsonConvert.SerializeObject(order);
            var functionUrl = "https://orderdeliveryfun.azurewebsites.net/api/OrderDelivery?code=EEdYxvD3WZIRJJBNhWxGUR0AAf10WTb15aZoLRTiaaqrvz/aLKPUoQ==";
             
            using (HttpClient client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, functionUrl))
            using (HttpContent content = CreateHttpContent(order))
            {
                request.Content = content;
                using (var resp = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    var message = await resp.Content.ReadAsStringAsync();
                }
            }
        }
        private async Task UploadOrders(List<OrderItem> items)
        {
            List<OrderedItem> orderedItems = new List<OrderedItem>();
            foreach (var item in items)
            {
                orderedItems.Add(new OrderedItem { ItemId = item.ItemOrdered.CatalogItemId, Quantity = item.Units });
            }
            var json = JsonConvert.SerializeObject(orderedItems);
            var functionUrl = "https://sjorderreserver.azurewebsites.net/api/OrderItemsReserver?code=iXzDgYSevq0A3tuaRbJgcagLZHlFJs/HIEoxNgumPKMTnEbCSKVPSg==";
                //"https://sjorderreserver.azurewebsites.net/api/OrderItemsReserver?code=6cP9es/k5XF9TJvr2PRgZmuvPRZUx9DZUauS8pa8WNUUuNQJJi6UoQ==";
                //var requestData = new StringContent(json, Encoding.UTF8, "application/json");
            using (HttpClient client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, functionUrl))
            using (HttpContent content = CreateHttpContent(orderedItems))
            {
                request.Content = content;
                using (var resp = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    var resualtList = await resp.Content.ReadAsStringAsync();
                }
            }
        }

        public static void SerializeJsonIntoStream(object value, Stream stream)
        {
            using (var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            using (var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                var js = new JsonSerializer();
                js.Serialize(jtw, value);
                jtw.Flush();
            }
        }
        private static HttpContent CreateHttpContent(object content)
        {
            HttpContent httpContent = null;

            if (content != null)
            {
                var ms = new MemoryStream();
                SerializeJsonIntoStream(content, ms);
                ms.Seek(0, SeekOrigin.Begin);
                httpContent = new StreamContent(ms);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            return httpContent;
        }

        public class OrderedItem
        {
            public int ItemId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
