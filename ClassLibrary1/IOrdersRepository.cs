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
        /// <param name="order">Объект заказа для создания</param>
        /// <returns>Созданный заказ с присвоенным ID</returns>
        Task<Orders> CreateOrderAsync(Orders order);

        /// <summary>
        /// Получает заказ по его уникальному идентификатору
        /// </summary>
        /// <param name="orderId">ID заказа в базе данных</param>
        /// <returns>Объект заказа или null, если не найден</returns>
        Task<Orders> GetOrderByIdAsync(int orderId);

        /// <summary>
        /// Получает текущий (последний) заказ пользователя
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <returns>Последний заказ пользователя или null, если заказов нет</returns>
        Task<Orders> GetUserCurrentOrderAsync(long userId);

        /// <summary>
        /// Обновляет информацию о заказе в базе данных
        /// </summary>
        /// <param name="order">Объект заказа с обновленными данными</param>
        /// <returns>True, если обновление выполнено успешно</returns>
        Task<bool> UpdateOrderAsync(Orders order);

        /// <summary>
        /// Удаляет заказ из базы данных по его ID
        /// </summary>
        /// <param name="orderId">ID заказа для удаления</param>
        /// <returns>True, если удаление выполнено успешно</returns>
        Task<bool> DeleteOrderAsync(int orderId);

        /// <summary>
        /// Получает все заказы пользователя отсортированные по дате (сначала новые)
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <returns>Список заказов пользователя</returns>
        Task<List<Orders>> GetUserOrdersAsync(long userId);
    }

    /// <summary>
    /// Репозиторий для временных данных заказа во время процесса оформления
    /// </summary>
    public interface IOrdersDataRepository
    {
        /// <summary>
        /// Получает временные данные заказа для пользователя
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <returns>Временные данные заказа или null, если процесс не начат</returns>
        Task<UserOrderData> GetOrderDataAsync(long userId);

        /// <summary>
        /// Сохраняет временные данные заказа для пользователя
        /// </summary>
        /// <param name="orderData">Данные заказа для сохранения</param>
        /// <returns>True, если сохранение выполнено успешно</returns>
        Task<bool> SaveOrderDataAsync(UserOrderData orderData);

        /// <summary>
        /// Удаляет временные данные заказа для пользователя
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <returns>True, если удаление выполнено успешно</returns>
        Task<bool> DeleteOrderDataAsync(long userId);
    }
}
