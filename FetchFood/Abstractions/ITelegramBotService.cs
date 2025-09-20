using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FetchFood.Abstractions
{
    public interface ITelegramBotService
    {
        public async Task StartAsync(string token)
        { }

        public Task StopAsync();
    }
}
