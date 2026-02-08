using BusinessLogic.Services.Administration.Models;
using DataAccess.Entities;

namespace BusinessLogic.Services.Administration.Abstraction
{
    /// <summary>
    /// Сервис администрирования заказов.
    /// </summary>
    public interface IAdministrationService 
    {
        Task<List<Orders>> GetOrdersAsync(string status);

        Task<Orders> GetOrderAsync(int orderId);

        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);

        Task<long> GetOrdersUserIdAsync(int orderId);
    }
}
