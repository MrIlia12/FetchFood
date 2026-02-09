using DataAccess.Entities;
using DataAccess.EntityFramework;
using DataAccess.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;

namespace DataAccess.Repositories.Implementations
{
    public class CourierRepository : ICourierRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CourierRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> AddCourierAsync(Courier courier) 
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            await dbContext.Couriers.AddAsync(courier);
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<Courier> GetCourierByUserIdAsync(long userId)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var courier = await dbContext.Couriers.FirstOrDefaultAsync(x => x.IdUser == userId);
            return courier;
        }
    }
}
