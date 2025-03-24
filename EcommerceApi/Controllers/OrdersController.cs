using EcommerceApi.Models;
using EcommerceApi.Models.Repositories;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IUserRepository userRepository;
        private readonly IProductRepository productRepository;
        private readonly IOrderRepository orderRepository;

        public OrdersController(IUserRepository userRepository, IProductRepository productRepository, IOrderRepository orderRepository)
        {
            this.userRepository = userRepository;
            this.productRepository = productRepository;
            this.orderRepository = orderRepository;
        }

        [HttpGet("PaymentStatuses")]
        public IActionResult PaymentStatuses()
        {
            return Ok(OrderHelper.PaymentStatuses);
        }

        [HttpGet("OrderStatuses")]
        public IActionResult OrderStatuses()
        {
            return Ok(OrderHelper.OrderStatuses);
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetOrders(int? page)
        {
            int userId = JwtReader.GetUserId(User);
            string role = JwtReader.GetUserRole(User);
            
            IQueryable <Order> query = orderRepository.GetOrders().Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product);

            if (role != "admin")
            {
                query = query.Where(o => o.UserId == userId);
            }

            query = query.OrderByDescending(o => o.Id);


            // implement the pagination functionality
            if (page == null || page < 1)
            {
                page = 1;
            }

            int pageSize = 5;
            int totalPages = 0;

            decimal count = query.Count();
            totalPages = (int)Math.Ceiling(count / pageSize);

            query = query.Skip((int)(page - 1) * pageSize)
                .Take(pageSize);


            // read the orders
            var orders = query.ToList();


            foreach(var order in orders)
            {
                // get rid of the object cycle
                foreach (var item in order.OrderItems)
                {
                    item.Order = null;
                }

                order.User.Password = "";
            }


            var response = new
            {
                Orders = orders,
                TotalPages = totalPages,
                PageSize = pageSize,
                Page = page
            };

            return Ok(response);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("Count")]
        public IActionResult GetOrdersCount()
        {
            decimal count = orderRepository.GetOrders().Count();

            var response = new
            {
                Count = count,
            };

            return Ok(response);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("TotalSales")]
        public IActionResult GetTotalSales()
        {
            IQueryable<Order> query = orderRepository.GetOrders()
                .Include(o => o.OrderItems);

            // read the orders
            var orders = query.ToList();

            decimal totalSales = 0;

            foreach (var order in orders)
            {
                totalSales += order.ShippingFee;

                foreach (var item in order.OrderItems)
                {
                    totalSales += item.UnitPrice * item.Quantity;
                }
            }


            var response = new
            {
                TotalSales = totalSales,
            };

            return Ok(response);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("CountByStatuses")]
        public IActionResult GetOrdersCountByOrderStatuses()
        {
            var orderStatuses = OrderHelper.OrderStatuses;

            var response = orderStatuses.ToDictionary(key => key, key => orderRepository.GetOrders(key).Count());

            return Ok(response);
        }


        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetOrder(int id)
        {
            int userId = JwtReader.GetUserId(User);
            string role = JwtReader.GetUserRole(User);

            Order? order = null;

            if (role == "admin")
            {
                order = orderRepository.GetOrders().Include(o => o.User)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .FirstOrDefault(o => o.Id == id);
            }
            else
            {
                order = orderRepository.GetOrders().Include(o => o.User)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .FirstOrDefault(o => o.Id == id && o.UserId == userId);
            }

            if (order == null)
            {
                return NotFound();
            }


            // get rid of the object cycle
            foreach(var item in order.OrderItems)
            {
                item.Order = null;
            }


            // hide the user password
            order.User.Password = "";


            return Ok(order);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> CreateOrder(OrderDto orderDto)
        {
            // check if the payment method is valid or not
            if (!OrderHelper.PaymentMethods.ContainsKey(orderDto.PaymentMethod))
            {
                ModelState.AddModelError("Payment Method", "Please select a valid payment method");
                return BadRequest(ModelState);
            }

            int userId = JwtReader.GetUserId(User);
            var user = await userRepository.GetUserById(userId);
            if (user == null)
            {
                ModelState.AddModelError("Order", "Unable to create the order");
                return BadRequest(ModelState);
            }

            var productDictionary = OrderHelper.GetProductDictionary(orderDto.ProductIdentifiers);


            // create a new order
            Order order = new Order();
            order.UserId = userId;
            order.CreatedAt = DateTime.Now;
            order.ShippingFee = OrderHelper.ShippingFee;
            order.DeliveryAddress = orderDto.DeliveryAddress;
            order.PaymentMethod = orderDto.PaymentMethod;
            order.PaymentStatus = OrderHelper.PaymentStatuses[0]; // Pending
            order.OrderStatus = OrderHelper.OrderStatuses[0]; // Created


            foreach(var pair in productDictionary)
            {
                int productId = pair.Key;
                var product = await productRepository.GetProduct(productId);
                if (product == null)
                {
                    ModelState.AddModelError("Product", "Product with id " + productId + " is not available");
                    return BadRequest(ModelState);
                }

                var orderItem = new OrderItem();
                orderItem.ProductId = productId;
                orderItem.Quantity = pair.Value;
                orderItem.UnitPrice = product.Price;


                order.OrderItems.Add(orderItem);
            }


            if (order.OrderItems.Count < 1)
            {
                ModelState.AddModelError("Order", "Unable to create the order");
                return BadRequest(ModelState);
            }


            // save the order in the database
            var createdOrder = await orderRepository.CreateOrder(order);



            // get rid of the object cycle
            foreach(var item in createdOrder.OrderItems)
            {
                item.Order = null;
            }

            // hide the user password
            createdOrder.User.Password = "";

            return Ok(createdOrder);
        }


        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateOrder(int id, string? paymentStatus, string? orderStatus)
        {
            if (paymentStatus == null && orderStatus == null)
            {
                // we have nothing to do
                ModelState.AddModelError("Update Order", "There is nothing to update");
                return BadRequest(ModelState);
            }


            if (paymentStatus != null && !OrderHelper.PaymentStatuses.Contains(paymentStatus))
            {
                // the payment status is not valid
                ModelState.AddModelError("Payment Status", "The Payment Status is not valid");
                return BadRequest(ModelState);
            }


            if (orderStatus != null && !OrderHelper.OrderStatuses.Contains(orderStatus))
            {
                // the order status is not valid
                ModelState.AddModelError("Order Status", "The Order Status is not valid");
                return BadRequest(ModelState);
            }


            var order = await orderRepository.FindOrderById(id);
            if (order == null)
            {
                return NotFound();
            }

            if (paymentStatus != null)
            {
                order.PaymentStatus = paymentStatus;
            }

            if (orderStatus != null)
            {
                order.OrderStatus = orderStatus;
            }

            var updatedOrder = await orderRepository.UpdateOrder(order);

            return Ok(updatedOrder);
        }



        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteOrder(int id)
        {
            var order = await orderRepository.FindOrderById(id);
            if (order == null)
            {
                return NotFound();
            }

            await orderRepository.DeleteOrder(order);

            return Ok();
        }
    }
}
