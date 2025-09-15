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

        public async Task<User> GetUserByIdAsync(long telegramId)
        {
            return await new Task<User>(() => 
            {
                return dbContext.Users.FirstOrDefault(x => x.TelegramUserId == telegramId);
            });
        }

        public async Task<bool> AddUserAsync(User user) 
        {
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();
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
