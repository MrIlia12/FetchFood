using DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Abstractions
{
    /// <summary>
    /// Репозиторий для работы с заказами в базе данных (интерфейс)
    /// </summary>
    public interface IOrdersRepository
    {
        /// <summary>
        /// Создает новый заказ в базе данных
        /// </summary>
        Task<Orders> CreateOrderAsync(Orders order);

        /// <summary>
        /// Получает заказ по его уникальному идентификатору
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        Task<Orders> GetOrderByIdAsync(int orderId);

        /// <summary>
        /// Получает текущий (последний) заказ пользователя
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<Orders> GetUserCurrentOrderAsync(long userId);

        /// <summary>
        /// Обновляет информацию о заказе в базе данных
        /// </summary>
        Task<bool> UpdateOrderAsync(Orders order);

        /// <summary>
        /// Удаляет заказ из базы данных по его ID
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        Task<bool> DeleteOrderAsync(int orderId);

        /// <summary>
        /// Получает все заказы пользователя отсортированные по дате (сначала новые)
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<List<Orders>> GetUserOrdersAsync(long userId);

        /// <summary>
        /// Получает все активные заказы, закреплённые за курьером и отсортированные по дате (сначала новые)
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<List<Orders>> GetCourierOrdersAsync(long courierId);

        /// <summary>
        /// Получает заказы по статусу
        /// </summary>
        /// <param name="status">Статус заказа</param>
        Task<List<Orders>> GetOrdersByStatusAsync(string status);
    }

    /// <summary>
    /// Репозиторий для временных данных заказа во время процесса оформления
    /// </summary>
    public interface IOrdersDataRepository
    {
        /// <summary>
        /// Получает временные данные заказа
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<UserOrderData> GetOrderDataAsync(long userId);

        /// <summary>
        /// Сохраняет временные данные заказа
        /// </summary>
        /// <param name="orderData">Данные заказа для сохранения</param>
        Task<bool> SaveOrderDataAsync(UserOrderData orderData);

        /// <summary>
        /// Удаляет временные данные заказа
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<bool> DeleteOrderDataAsync(long userId);
    }
}
