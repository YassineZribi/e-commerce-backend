using EcommerceApi.Services;
using Microsoft.EntityFrameworkCore;
using System;

namespace EcommerceApi.Models.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext context;

        public AccountRepository(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task<User> Register(User user)
        {
            var result = await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<PasswordReset> FindPasswordResetByEmail(string email)
        {
            var oldPwdReset = await context.PasswordResets.FirstOrDefaultAsync(r => r.Email == email);
            return oldPwdReset;
        }

        public async Task<PasswordReset> RemovePasswordReset(PasswordReset passwordReset)
        {
            context.PasswordResets.Remove(passwordReset);
            await context.SaveChangesAsync();
            return passwordReset;
        }

        public async Task<PasswordReset> AddPasswordReset(PasswordReset passwordReset)
        {
            var result = await context.PasswordResets.AddAsync(passwordReset);
            await context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<PasswordReset> FindPasswordResetByToken(string token)
        {
            var pwdReset = await context.PasswordResets.FirstOrDefaultAsync(r => r.Token == token);
            return pwdReset;
        }

        public async Task<User> UpdateProfile(User user)
        {
            var User = context.Users.Attach(user);
            User.State = EntityState.Modified;
            await context.SaveChangesAsync();
            return user;
        }
    }
}
