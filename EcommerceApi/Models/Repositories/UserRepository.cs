using EcommerceApi.Services;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Models.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext context;

        public UserRepository(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task<User> GetUserByEmail(string email)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user;
        }

        public async Task<User> GetUserById(int id)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }

        public IQueryable<User> GetUsers()
        {
            return context.Users;
        }

        public IQueryable<User> GetUsers(string role)
        {
            return context.Users.Where(u => u.Role.ToLower().Equals(role.ToLower()));
        }
    }
}
