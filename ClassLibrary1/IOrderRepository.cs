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
        Task<bool> AddOrderAsync(Orders order);

        Task<Orders> GetOrderByIdAsync(int id);

        Task<bool> UpdateOrderAsync(Orders order);

        Task<Orders[]> GetOrdersAsync();

        Task<bool> RemoveOrderByIdAsync(long orderId);
    }
}
