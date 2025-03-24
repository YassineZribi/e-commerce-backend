namespace EcommerceApi.Models.Repositories
{
    public interface ISupplierRepository
    {
        IQueryable<Supplier> GetSuppliers();

        Task<Supplier> GetSupplierById(int id);

        Task<Supplier> GetSupplierByEmail(string email);

        Task<Supplier> CreateSupplier(Supplier supplier);

        Task<Supplier> UpdateSupplier(Supplier supplier);

        Task<Supplier> DeleteSupplier(Supplier supplier);
    }
}
