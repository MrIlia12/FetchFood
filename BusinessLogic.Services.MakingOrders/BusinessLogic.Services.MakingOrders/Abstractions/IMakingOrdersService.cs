using DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.MakingOrders.Abstractions
{
    /// <summary>
    /// Сервис оформления заказов (интерфейс).
    /// </summary>
    public interface IMakingOrdersService
    {
        /// <summary>
        /// Начинает процесс создания заказа для пользователя
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <returns>True, если процесс начат успешно, false если пользователь не авторизован или произошла ошибка</returns>
        Task<bool> StartOrderCreationAsync(long userId);

        /// <summary>
        /// Обрабатывает ввод пользователя на текущем шаге оформления заказа
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <param name="message">Сообщение от пользователя (адрес, комментарий и т.д.)</param>
        /// <returns>Результат обработки с сообщением для пользователя и следующим шагом</returns>
        Task<OrderProcessingResult> ProcessUserInputAsync(long userId, string message);

        /// <summary>
        /// Отменяет процесс создания заказа и очищает временные данные
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <returns>True, если отмена выполнена успешно</returns>
        Task<bool> CancelOrderCreationAsync(long userId);

        /// <summary>
        /// Получает текущий активный заказ пользователя (последний созданный)
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <returns>Объект заказа или null, если заказ не найден</returns>
        Task<Orders> GetCurrentOrderAsync(long userId);

        /// <summary>
        /// Завершает процесс оформления заказа и сохраняет его в базу данных
        /// </summary>
        /// <param name="userId">ID пользователя в Telegram</param>
        /// <returns>True, если заказ успешно завершен</returns>
        Task<bool> CompleteOrderAsync(long userId);

        Task<bool> IsUserInOrderProcessAsync(long userId);
    }
}
