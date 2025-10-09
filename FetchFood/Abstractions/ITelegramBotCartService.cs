using Telegram.Bot;
using Telegram.Bot.Types;

namespace FetchFood.Abstractions
{
    public interface ITelegramBotCartService
    {
        Task ShowMainMenuAsync(ITelegramBotClient bot, long chatId, CancellationToken ct);
        Task HandleMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct);

    }
}
