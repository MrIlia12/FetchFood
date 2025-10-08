using DataAccess.Entities;
using DataAccess.EntityFramework;
using DataAccess.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.Repositories.Implementations
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OrderRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> AddOrderAsync(Order order)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            await dbContext.Orders.AddAsync(order);
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            var result = await dbContext.Orders.FirstOrDefaultAsync(x => x.Id == id);
            return result;
        }

        public async Task<bool> UpdateOrderAsync(Order order)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            var oldOrder = await dbContext.Orders.FirstOrDefaultAsync(x =>x.Id == order.Id);
            oldOrder.UserId = order.UserId;
            oldOrder.CourierId = order.CourierId;
            oldOrder.Status = order.Status;
            oldOrder.Price = order.Price;
            oldOrder.DateOrder = order.DateOrder;

            dbContext.SaveChanges();

            return true;
        }

        /// <summary>
        /// Получает заказ по порядковому номеру.
        /// </summary>
        public async Task<Order[]> GetOrdersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var result = await dbContext.Orders.ToArrayAsync();

            return result;
        }
    }
}
