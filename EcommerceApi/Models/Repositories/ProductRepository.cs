using EcommerceApi.Services;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Models.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext context;

        public ProductRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IQueryable<Product> GetProducts()
        {
            return context.Products;
        }

        public IQueryable<Product> GetProducts(string category)
        {
            return context.Products.Where(p => p.Category.ToLower().Equals(category.ToLower()));
        }

        public async Task<Product> GetProduct(int id)
        {
            return await context.Products.FirstOrDefaultAsync(p => p.Id == id);

        }

        public async Task<Product> CreateProduct(Product product)
        {
            var result = await context.Products.AddAsync(product);
            await context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Product> UpdateProduct(Product product)
        {
            var Product = context.Products.Attach(product);
            Product.State = EntityState.Modified;
            await context.SaveChangesAsync();
            return product;
        }

        public async Task<Product> DeleteProduct(Product product)
        {
            context.Products.Remove(product);
            await context.SaveChangesAsync();
            return product;
        }
    }
}
