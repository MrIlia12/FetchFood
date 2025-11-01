using Telegram.Bot;

namespace FetchFood.Abstractions
{
    public interface ITelegramBotMenuService
    {
        /// <summary>
        /// Показать кнопку меню в тг чате
        /// </summary>
        /// <param name="bot">Экземпляр тг бота</param>
        /// <param name="_chatId">Id чата (тип long)</param>
        /// <param name="mggs">Сопроводительное сообщение для кнопки меню</param>
        /// <param name="ct">Токен отмены</param>
        /// <returns></returns>
        public Task ShowMenuButton(ITelegramBotClient bot, string mggs, long _chatId, CancellationToken ct);
        /// <summary>
        /// Обработка команд управления меню
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="_chatId"></param>
        /// <param name="message"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task HandleMenuCommandAsync(ITelegramBotClient bot, long _chatId, string message, CancellationToken ct);
    }
}
