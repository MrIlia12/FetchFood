using DataAccess.Entities;
using DataAccess.EntityFramework;
using DataAccess.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        //private readonly DataContext dbContext;
        private readonly IServiceScopeFactory _scopeFactory;

        public UserRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<User> GetUserByIdAsync(long telegramId)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.TelegramUserId == telegramId);
            return user;
        }

        public async Task<bool> AddUserAsync(User user) 
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveUserByIdAsync(long telegramId)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            var user = dbContext.Users.FirstOrDefault(x => x.TelegramUserId == telegramId);
            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync();
            return true;
        }
    }
}
