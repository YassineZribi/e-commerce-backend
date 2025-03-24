namespace EcommerceApi.Models.Repositories
{
    public interface IAccountRepository
    {
        Task<User> Register(User user);

        Task<PasswordReset> FindPasswordResetByEmail(string email);

        Task<PasswordReset> FindPasswordResetByToken(string token);

        Task<PasswordReset> RemovePasswordReset(PasswordReset passwordReset);

        Task<PasswordReset> AddPasswordReset(PasswordReset passwordReset);

        Task<User> UpdateProfile(User user);
    }
}
