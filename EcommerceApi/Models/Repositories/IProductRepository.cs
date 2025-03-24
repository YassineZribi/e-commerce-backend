namespace EcommerceApi.Models.Repositories
{
    public interface IProductRepository
    {
        IQueryable<Product> GetProducts();

        IQueryable<Product> GetProducts(string category);

        Task<Product> GetProduct(int id);

        Task<Product> CreateProduct(Product product);

        Task<Product> UpdateProduct(Product product);

        Task<Product> DeleteProduct(Product product);
    }
}
