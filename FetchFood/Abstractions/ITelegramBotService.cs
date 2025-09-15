using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FetchFood.Abstractions
{
    public interface ITelegramBotService
    {
        public async Task StartAsync()
        { }

        public Task StopAsync();

        private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        { }

    }
}
