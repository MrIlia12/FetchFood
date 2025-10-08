using BusinessLogic.Services.Authorization.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using FetchFood.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using BusinessLogic.Services.Administration.Abstraction;
using BusinessLogic.Services.Administration.Models;
using DataAccess.Entities.Models;

namespace FetchFood.Services
{
    // Версия библиотеки Telegram.Bot: 22.6.2
    internal class TelegramBotService : ITelegramBotService
    {
        private TelegramBotClient _bot;
        private readonly CancellationTokenSource _cts = new();
        private readonly IAuthorizationService _authorizationService;
        private readonly IAdministrationService _administrationService;

        public TelegramBotService(IAuthorizationService authorizationService, IAdministrationService administrationService)
        {
            _authorizationService = authorizationService;
            _administrationService = administrationService;
        }

        public async Task StartAsync(string token)
        {
            _bot = new TelegramBotClient(token);
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
            // Обработка событий по callback системе.
            if (update.Type is UpdateType.CallbackQuery)
            {
                if (update.CallbackQuery is not { } callBack) return;

                var callBackData = callBack.Data.Split(' ');
                var number = callBackData.Length > 1 ? Convert.ToInt32(callBackData[1]) : 0;

                switch (callBackData[0])
                {
                    case BotCommands.GETORDERS:
                        var order = await _administrationService.GetOrderInformationAsync(number);
                        var keyboard = new InlineKeyboardMarkup();
                        var keyBoardButtons = new List<InlineKeyboardButton>();

                        if (order.OrderPosition != OrderPosition.First)
                        {
                            keyBoardButtons.Add(new InlineKeyboardButton("⬅", $"GetOrder {number - 1}"));
                        }

                        keyBoardButtons.Add(new InlineKeyboardButton("Выбрать", $"ToOrderMenu {order.Id} {order.Status}"));

                        if (order.OrderPosition != OrderPosition.Last)
                        {
                            keyBoardButtons.Add(new InlineKeyboardButton("➡", $"GetOrder {number + 1}"));
                        }

                        keyboard.AddButtons(keyBoardButtons.ToArray());

                        await _bot.SendMessage(
                            chatId: callBack.Message.Chat.Id,
                            text: "Заказ: " + order.Id + "\n" +
                                    "Пользователь:" + order.UserName + "\n" +
                                    "Статус:" + order.Status + "\n" +
                                    "Цена:" + order.Price + "\n" +
                                    "Дата заказа:" + order.DateOrder,
                            replyMarkup: keyboard);


                        break;

                    case BotCommands.TOORDERMENU:
                        var menuKeyboard = new InlineKeyboardMarkup();
                        var menuKeyboardButtons = new List<InlineKeyboardButton>();

                        if (callBackData[2] != OrderStatus.Complete.ToString())
                        {
                            menuKeyboardButtons.Add(new InlineKeyboardButton("Перевести заказ на следующий этап.", $"NextStep {number}"));
                        }

                        menuKeyboardButtons.Add(new InlineKeyboardButton("Удалить заказ", $"Delete {number}"));

                        menuKeyboard.AddButtons(menuKeyboardButtons.ToArray());

                        await _bot.SendMessage(
                            chatId: callBack.Message.Chat.Id,
                            text: "Меню заказа: " + number,
                            replyMarkup: menuKeyboard);

                        break;

                    case BotCommands.ORDERNEXTSTEP:
                        if (await _administrationService.ChangeOrderStatus(number))
                        {
                            var afterStepKeyboard = new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    new InlineKeyboardButton("В меню.", "GetOrder")
                                }
                            });

                            await _bot.SendMessage(
                                chatId: callBack.Message.Chat.Id,
                                text: "Операция успешно выполнена",
                                replyMarkup: afterStepKeyboard);
                        }
                        break;
                }
            }

            if (update.Message is not { } msg) return;

            if (msg.Type == MessageType.Contact)
            {
                await HandleContactMessage(msg);
                return;
            }

            if (msg.Text is not { } text) return;

            string? command = text.Split(' ')[0];

            switch (command)
            {
                case BotCommands.START:
                    await bot.SendMessage(msg.Chat.Id, "Привет! Меня зовут FetchFood. \nСейчас я проверю, знакомы ли мы. \nТакже Вы можете написать /help, чтобы узнать, что я могу!", cancellationToken: ct);

                    var isAuthorized = await _authorizationService.IsUserAuthorizedAsync(msg.From.Id);
                    if (!isAuthorized)
                    {
                        await RequestContactAsync(msg.Chat.Id);
                        return;
                    }

                    var isAdministrator = await _authorizationService.IsUserAdministratorAsync(msg.From.Id);
                    if (isAdministrator)
                    {
                        GetAdministratorConsoleAsync(msg.Chat.Id);
                        return;
                    }

                    break;

                case BotCommands.HELP:
                    await bot.SendMessage(msg.Chat.Id, "Всем привет! Я - бот доставки еды. \r\nПока я ещё совсем молодой и почти ничего не умею, но в будущем смогу отображать меню, помогать с оформлением и отслеживанием заказов.\r\nПожелайте мне успехов в развитии!♥️", cancellationToken: ct);
                    break;

                default:
                    await bot.SendMessage(msg.Chat.Id, "Вас не понял... Попробуйте команду /help.", cancellationToken: ct);
                    break;
            }
        }

        /// <summary>
        /// Метод запроса на предоставления контакта.
        /// </summary>
        /// <param name="chatId">Id чата.</param>
        private async Task RequestContactAsync(long chatId)
        {
            var requestContactKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    KeyboardButton.WithRequestContact("📞 Поделиться контактом.")
                }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await _bot.SendMessage(
                chatId: chatId,
                text: "👋 Добро пожаловать! Чтобы использовать бота, пожалуйста поделитесь контактом.",
                replyMarkup: requestContactKeyboard);
        }

        private async Task GetAdministratorConsoleAsync(long chatId)
        {
            var requestContactKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    new InlineKeyboardButton("Перейти к списку заказов", "GetOrder")
                }
            });

            await _bot.SendMessage(
                chatId: chatId,
                text: "Ваша роль - администратор.",
                replyMarkup: requestContactKeyboard);
        }

        private async Task HandleContactMessage(Message message)
        {
            var user = new DataAccess.Entities.User
            {
                TelegramUserId = message.From.Id,
                Name = message.Contact.FirstName,
                PhoneNumber = message.Contact.PhoneNumber,
                Role = DataAccess.Entities.Models.UserRole.User,
            };

            var result = await _authorizationService.AuthorizeUserAsync(user);

            if (result)
            {
                // Remove the contact request keyboard
                var removeKeyboard = new ReplyKeyboardRemove();

                await _bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: "✅ Спасибо! Ваша контактная информация успешно сохранена.",
                    replyMarkup: removeKeyboard);
            }
            else
            {
                await _bot.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Произошла ошибка, пожалуйста попробуйте позже.");
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"[{LogMessages.ERROR}]: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
