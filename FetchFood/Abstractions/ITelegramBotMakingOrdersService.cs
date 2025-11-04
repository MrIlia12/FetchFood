using DataAccess.Entities;
using DataAccess.Entities.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FetchFood.Abstractions
{
    public interface ITelegramBotMakingOrdersService
    {
        /// <summary>
        /// Обработка callback-запросов
        /// </summary>
        Task HandleOrderCallbackAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct);

        /// <summary>
        /// Отправка сообщения с результатом обработки заказа
        /// </summary>
        Task SendOrderResultMessageAsync(ITelegramBotClient bot, long chatId, OrderProcessingResult result, CancellationToken ct);

        /// <summary>
        /// Проверка является ли callbackData кнопкой сервиса оформления заказа
        /// </summary>
        bool IsOrderButton(string callbackData);
    }
}