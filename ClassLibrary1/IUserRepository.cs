using DataAccess.Entities;

namespace DataAccess.Repositories.Abstractions
{
    public interface IUserRepository
    {
        Task<bool> AddUserAsync(User user);

        Task<User> GetUserByIdAsync(long TelegramUserId);

        bool RemoveUserById(long TelegramUserId);
    }
}
