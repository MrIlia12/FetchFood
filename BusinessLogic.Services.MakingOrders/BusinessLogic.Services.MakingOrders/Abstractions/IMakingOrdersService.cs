using DataAccess.Entities;

namespace BusinessLogic.Services.MakingOrders.Abstractions
{
    /// <summary>
    /// Интерфейс сервиса оформления заказов
    /// </summary>
    public interface IMakingOrdersService
    {
        /// <summary>
        /// Начало процесса создания заказа для пользователя
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<bool> StartOrderCreationAsync(long userId);

        /// <summary>
        /// Обработка ввода пользователя на текущем шаге оформления заказа
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <param name="message">Сообщение от пользователя (адрес, комментарий)</param>
        Task<OrderProcessingResult> ProcessUserInputAsync(long userId, string message);

        /// <summary>
        /// Отмена процесса создания заказа
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<bool> CancelOrderCreationAsync(long userId);

        /// <summary>
        /// Получение текущего активного заказа пользователя (последний созданный)
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<Orders> GetCurrentOrderAsync(long userId);

        /// <summary>
        /// Завершение процесса оформления заказа
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<bool> CompleteOrderAsync(long userId);

        /// <summary>
        /// Проверка находится ли пользователь в процессе оформления заказа
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        Task<bool> IsUserInOrderProcessAsync(long userId);
    }
}
