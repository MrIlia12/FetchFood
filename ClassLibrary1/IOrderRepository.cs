using DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Abstractions
{
    /// <summary>
    /// Интерфейс репозитория, связанного с таблицей заказов.
    /// </summary>
    public interface IOrderRepository
    {
        Task<bool> AddOrderAsync(Order order);

        Task<Order[]> GetOrdersAsync(int firstIndex, int lastIndex);
    }
}
