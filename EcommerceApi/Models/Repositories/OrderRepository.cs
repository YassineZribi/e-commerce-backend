using EcommerceApi.Services;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Models.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext context;

        public OrderRepository(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task<Order> CreateOrder(Order order)
        {
            var result = await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();
            return result.Entity;
        }

        public IQueryable<Order> GetOrders()
        {
            return context.Orders;
        }

        public IQueryable<Order> GetOrders(string orderStatus)
        {
            return context.Orders.Where(o => o.OrderStatus.ToLower().Equals(orderStatus.ToLower()));
        }

        public async Task<Order> FindOrderById(int id)
        {
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            return order;
        }

        public async Task<Order> UpdateOrder(Order order)
        {
            var Order = context.Orders.Attach(order);
            Order.State = EntityState.Modified;
            await context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> DeleteOrder(Order order)
        {
            context.Orders.Remove(order);
            await context.SaveChangesAsync();
            return order;
        }
    }
}
