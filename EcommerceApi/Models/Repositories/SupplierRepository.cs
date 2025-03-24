using EcommerceApi.Services;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Models.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly ApplicationDbContext context;

        public SupplierRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IQueryable<Supplier> GetSuppliers()
        {
            return context.Suppliers;
        }

        public async Task<Supplier> GetSupplierById(int id)
        {
            return await context.Suppliers.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Supplier> GetSupplierByEmail(string email)
        {
            return await context.Suppliers.FirstOrDefaultAsync(s => s.Email == email);
        }

        public async Task<Supplier> CreateSupplier(Supplier supplier)
        {
            var result = await context.Suppliers.AddAsync(supplier);
            await context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<Supplier> UpdateSupplier(Supplier supplier)
        {
            var Supplier = context.Suppliers.Attach(supplier);
            Supplier.State = EntityState.Modified;
            await context.SaveChangesAsync();
            return supplier;
        }

        public async Task<Supplier> DeleteSupplier(Supplier supplier)
        {
            context.Suppliers.Remove(supplier);
            await context.SaveChangesAsync();
            return supplier;
        }
    }
}
