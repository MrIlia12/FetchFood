using DataAccess.Entities;

namespace DataAccess.Repositories.Abstractions
{
    public interface IUserRepository
    {
        bool AddUser(User user);

        User GetUserById(long TelegramUserId);

        bool RemoveUserById(long TelegramUserId);
    }
}
