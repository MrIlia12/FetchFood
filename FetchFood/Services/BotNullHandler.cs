using Telegram.Bot;
using Telegram.Bot.Types;
using FetchFood.States;
using System.Collections.Concurrent;


namespace FetchFood.Services
{
    /// <summary>
    /// Обработчик для сервиса оформления заказов
    /// </summary>
    class BotNullHandler : BotCommandHandler
    {
        public BotNullHandler(Update update, ITelegramBotClient botClient, ConcurrentDictionary<long, UserState> userState) : base(update, botClient, userState)
        {
        }

        public override async Task Invoke()
        {
            await _bot.SendMessage(
                chatId: Update.Message.Chat.Id,
                text: "Вы не можете сейчас это сделать.");
        }
    }
}
