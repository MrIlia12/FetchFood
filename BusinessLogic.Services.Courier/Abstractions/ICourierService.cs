using DataAccess.Entities;

namespace BusinessLogic.Services.Courier.Abstractions
{
    /// <summary>
    /// Интерфейс сервиса курьера
    /// </summary>
    public interface ICourierService
    {
        /// <summary>
        /// Получает список активных заказов, назначенных курьеру
        /// </summary>
        /// <param name="courierId">ID курьера в Telegram</param>
        Task<List<Orders>> GetAvailableOrdersAsync(long courierId);

        /// <summary>
        /// Получает список активных заказов, назначенных курьеру
        /// </summary>
        /// <param name="courierId">ID курьера в Telegram</param>
        Task<List<Orders>> GetCourierOrdersAsync(long courierId);

        /// <summary>
        /// Получает информацию о конкретном заказе для курьера
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        Task<Orders> GetOrderDetailsAsync(long orderId);

        /// <summary>
        /// Курьер прибыл на место - отправляет уведомление пользователю
        /// </summary>
        /// <param name="courierId">ID курьера в Telegram</param>
        /// <param name="orderId">ID заказа</param>
        /// <returns>Результат с ID пользователя для отправки уведомления</returns>
        Task<CourierArrivalResult> NotifyArrivalAsync(long courierId, long orderId);

        /// <summary>
        /// Курьер завершил доставку заказа
        /// </summary>
        /// <param name="courierId">ID курьера в Telegram</param>
        /// <param name="orderId">ID заказа</param>
        Task<bool> CompleteDeliveryAsync(long courierId, long orderId);

        /// <summary>
        /// Курьер берёт заказ в доставку
        /// </summary>
        /// <param name="courierId">ID курьера в Telegram</param>
        /// <param name="orderId">ID заказа</param>
        Task<bool> TakeOrderInDeliveryAsync(long courierId, int orderId);

        /// <summary>
        /// Проверяет, является ли пользователь курьером
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<bool> IsCourierAsync(long userId);
    }

    /// <summary>
    /// Результат уведомления о прибытии курьера
    /// </summary>
    public class CourierArrivalResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long? UserIdToNotify { get; set; }
        public string UserNotificationMessage { get; set; }
    }
}
