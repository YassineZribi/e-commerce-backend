namespace EcommerceApi.Models.Repositories
{
    public interface IUserRepository
    {
        IQueryable<User> GetUsers();

        IQueryable<User> GetUsers(string role);

        Task<User> GetUserByEmail(string email);

        Task<User> GetUserById(int id);
    }
}
