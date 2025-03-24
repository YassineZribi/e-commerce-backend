namespace EcommerceApi.Models.Repositories
{
    public interface IOrderRepository
    {
        IQueryable<Order> GetOrders();

        IQueryable<Order> GetOrders(string orderStatus);

        Task<Order> CreateOrder(Order order);

        Task<Order> FindOrderById(int id);

        Task<Order> UpdateOrder(Order order);

        Task<Order> DeleteOrder(Order order);
    }
}
