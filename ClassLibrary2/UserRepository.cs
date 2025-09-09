using DataAccess.Entities;
using DataAccess.EntityFramework;
using DataAccess.Repositories.Abstractions;

namespace DataAccess.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext dbContext;

        public UserRepository(DataContext context)
        {
            dbContext = context;
        }

        public User GetUserById(long telegramId)
        {
            return dbContext.Users.FirstOrDefault(x => x.TelegramUserId == telegramId);
        }

        public bool AddUser(User user) 
        {
            dbContext.Users.Add(user);
            dbContext.SaveChanges();
            return true;
        }

        public bool RemoveUserById(long telegramId)
        {
            var user = dbContext.Users.FirstOrDefault(x => x.TelegramUserId == telegramId);
            dbContext.Users.Remove(user);
            dbContext.SaveChanges();
            return true;
        }
    }
}
