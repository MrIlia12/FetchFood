using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FetchFood.Services
{
    // Версия библиотеки Telegram.Bot: 22.6.2
    internal class TelegramBotService
    {
        private readonly TelegramBotClient _bot;
        private readonly CancellationTokenSource _cts = new();
        private readonly IOrderService _orderService;

        public TelegramBotService(string token, IOrderService orderService)
        {
            _bot = new TelegramBotClient(token);
            _orderService = orderService;
        }

        public async Task StartAsync()
        {
            User user = await _bot.GetMe(_cts.Token);
            Console.WriteLine($"@{user.Username} готов к работе.");

            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cts.Token
            );
        }

        public Task StopAsync()
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Message is not { } msg) return;

            // Обработка текстовых сообщений
            if (msg.Text is not { } text) return;

            string? command = text.Split(' ')[0];

            switch (command)
            {
                case BotCommands.START:
                    await bot.SendMessage(msg.Chat.Id, "Привет! Меня зовут FetchFood.\nНапишите /help, чтобы узнать, что я могу!", cancellationToken: ct);
                    break;

                case BotCommands.HELP:
                    await bot.SendMessage(msg.Chat.Id, "Всем привет! Я - бот доставки еды. \r\nПока я ещё совсем молодой и почти ничего не умею, но в будущем смогу отображать меню, помогать с оформлением и отслеживанием заказов.\r\nПожелайте мне успехов в развитии!♥️", cancellationToken: ct);
                    break;

                case BotCommands.ORDER:
                    await _orderService.StartOrderAsync(msg.Chat.Id, bot, ct);
                    break;

                default:
                    // Если это не команда, передаем сообщение в сервис заказов
                    await _orderService.ProcessMessageAsync(msg.Chat.Id, text, bot, ct);
                    break;

                //default:
                //    await bot.SendMessage(msg.Chat.Id, "Вас не понял... Попробуйте команду /help.", cancellationToken: ct);
                //    break;
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"[{LogMessages.ERROR}]: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
