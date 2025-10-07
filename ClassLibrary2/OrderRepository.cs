using DataAccess.Entities;
using DataAccess.EntityFramework;
using DataAccess.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /// <summary>
        /// Получает указанное количество заказов, начиная с указанного индекса.
        /// </summary>
        /// <param name="firstIndex">Первый индекс.</param>
        /// <param name="count">Число записей.</param>
        public async Task<Order[]> GetOrdersAsync(int firstIndex, int count)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var result = await dbContext.Orders.ToArrayAsync();

            return result;
        }
    }
}
