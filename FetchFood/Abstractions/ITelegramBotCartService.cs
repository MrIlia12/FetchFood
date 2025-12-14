using Telegram.Bot;
using Telegram.Bot.Types;
using System.Threading;
using System.Threading.Tasks; 

namespace FetchFood.Abstractions
{
    /// <summary>
    /// Определяет контракт для сервиса, управляющего взаимодействием пользователя с корзиной
    /// и главным меню в Telegram-боте.
    /// </summary>
    public interface ITelegramBotCartService
    {
        /// <summary>
        /// Асинхронно обрабатывает входящее текстовое сообщение от пользователя.
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="msg"></param>
        /// <param name="ct"></param>
        /// 
        // Лия (2025-12-13): добавляю возврат значения, чтобы понимать - сообщение было принято сервисом или нет
        // (если бот не находится в состоянии, когда пользователь добавляет или удалят позицию из корзины, будет возвращено false)
        Task<bool> HandleMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct);

        /// <summary>
        /// Асинхронно отображает главное меню пользователю.
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="chatId"></param>
        /// <param name="ct"></param>
        Task ShowMainMenuAsync(ITelegramBotClient bot, long chatId, CancellationToken ct);

        /// <summary>
        /// Асинхронно обрабатывает нажатие inline-кнопки (CallbackQuery).
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        Task HandleCallbackQueryAsync(ITelegramBotClient bot, CallbackQuery query, CancellationToken ct);
    }
}
