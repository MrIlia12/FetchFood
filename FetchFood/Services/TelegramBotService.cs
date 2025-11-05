using BusinessLogic.Services.Authorization.Abstractions;
using BusinessLogic.Services.MakingOrders.Abstractions;
using BusinessLogic.Services.MakingOrders.Implemenatation;
using DataAccess.Entities;
using FetchFood.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using BusinessLogic.Services.Administration.Abstraction;
using BusinessLogic.Services.Administration.Models;
using DataAccess.Entities.Models;
using BusinessLogic.Services.Menu.Abstractions;
using DataAccess.Entities;

namespace FetchFood.Services
{
    // Версия библиотеки Telegram.Bot: 22.6.2
    internal class TelegramBotService : ITelegramBotService
    {
        private TelegramBotClient _bot;
        private readonly CancellationTokenSource _cts = new();
        private readonly ILogger<MakingOrdersService> _logger;
        private readonly IAuthorizationService _authorizationService;
        private readonly ITelegramBotMenuService _menuService;
        private readonly IAdministrationService _administrationService;
        private readonly IMakingOrdersService _makingOrdersService;

        private readonly ITelegramBotCartService _cartService;

        public TelegramBotService(IAuthorizationService authorizationService, ITelegramBotCartService cartService, IAdministrationService administrationService, ITelegramBotMenuService menuService, IMakingOrdersService makingOrdersService)
        {
            _authorizationService = authorizationService;
            _administrationService = administrationService;
            _cartService = cartService;
            _menuService = menuService;
            _makingOrdersService = makingOrdersService;
        }

        public async Task StartAsync(string token)
        {
            _bot = new TelegramBotClient(token);
            Telegram.Bot.Types.User user = await _bot.GetMe(_cts.Token);
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
            if (update.Type is UpdateType.Message)
            {
                var commandHandler = new BotAuthorizationHandler(update, bot, _authorizationService);
                commandHandler.Invoke();
            }

            // Обработка событий по callback системе.
            if (update.Type is UpdateType.CallbackQuery)
            {
                if (update.CallbackQuery is not { } callBack) return;

                var callBackData = callBack.Data.Split(' ');
				
				// обработка ответов на команды меню
                if (callBackData[0].Contains(BotCommands.MENU))
                {
                    await _menuService.HandleMenuCommandAsync(bot, callBack.Message.Chat.Id, callBack.Data, ct);
                    await _bot.AnswerCallbackQuery(callBack.Id, cancellationToken: ct);
                    return;
                }
                //
				
                var number = callBackData.Length > 1 ? Convert.ToInt32(callBackData[1]) : 0;

                switch (callBackData[0])
                {
                    case BotCommands.GETORDERS:
                        var order = await _administrationService.GetOrderInformationAsync(number);
                        var keyboard = new InlineKeyboardMarkup();
                        var keyBoardButtons = new List<InlineKeyboardButton>();

                        if (order.OrderPosition is not OrderPosition.First and not OrderPosition.Lonely)
                        {
                            keyBoardButtons.Add(new InlineKeyboardButton("⬅", $"GetOrder {number - 1}"));
                        }

                        keyBoardButtons.Add(new InlineKeyboardButton("Выбрать", $"ToOrderMenu {order.Id} {order.Status}"));

                        if (order.OrderPosition is not OrderPosition.Last and not OrderPosition.Lonely)
                        {
                            keyBoardButtons.Add(new InlineKeyboardButton("➡", $"GetOrder {number + 1}"));
                        }

                        keyboard.AddButtons(keyBoardButtons.ToArray());

                        await _bot.SendMessage(
                            chatId: callBack.Message.Chat.Id,
                            text: "Заказ: " + order.Id + "\n" +
                                    "Пользователь: " + order.UserName + "\n" +
                                    "Статус: " + order.Status + "\n" +
                                    "Цена: " + order.Price + "\n" +
                                    "Дата заказа: " + order.DateOrder,
                            replyMarkup: keyboard);


                        break;

                    case BotCommands.TOORDERMENU:
                        var menuKeyboard = new InlineKeyboardMarkup();
                        var menuKeyboardButtons = new List<InlineKeyboardButton>();

                        if (callBackData[2] != OrderStatus.Delivered.ToString())
                        {
                            menuKeyboardButtons.Add(new InlineKeyboardButton("Перевести заказ на следующий этап.", $"NextStep {number}"));
                        }

                        menuKeyboardButtons.Add(new InlineKeyboardButton("Удалить заказ", $"DeleteOrder {number}"));

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

                    case BotCommands.ORDERDELETE:
                        if (await _administrationService.DeleteOrderAsync(number))
                        {
                            var afterDeleteKeyboard = new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    new InlineKeyboardButton("В меню.", "GetOrder")
                                }
                            });

                            await _bot.SendMessage(
                                chatId: callBack.Message.Chat.Id,
                                text: "Операция успешно выполнена",
                                replyMarkup: afterDeleteKeyboard);
                        }
                        break;
                }
            }

            if (update.CallbackQuery is { } callbackQuery)
            {
                await HandleCallbackQueryAsync(bot, callbackQuery, ct);
                return;
            }

            if (update.Message is not { } msg) return;

            if (msg.Text is not { } text) return;
			
			// если была подана текстовая команда управления меню
            if (msg.Text.StartsWith(BotCommands.MENU))
            {
                await _menuService.HandleMenuCommandAsync(bot, msg.Chat.Id, msg.Text, ct);
                return;
            }

            string? command = text.Split(' ')[0];
            switch (command)
            {
                case BotCommands.START:
                    await bot.SendMessage(msg.Chat.Id, "Привет! Меня зовут FetchFood. \nСейчас я проверю, знакомы ли мы. \nТакже Вы можете написать /help, чтобы узнать, что я могу!", cancellationToken: ct);

                    var isAdministrator = await _authorizationService.IsUserAdministratorAsync(msg.From.Id);
                    if (isAdministrator)
                    {
                        return;
                    }
                    else
                    {
                        // Если авторизован - предлагаем начать оформление заказа
                        await ShowOrderSuggestion(bot, msg.Chat.Id, ct);
                    }
					// Показываем кнопку меню
                    await _menuService.ShowMenuButton(bot, string.Empty, msg.Chat.Id, ct);
					// Показываем меню управления корзиной 
                    await _cartService.ShowMainMenuAsync(bot, msg.Chat.Id, ct);

                    break;

                case BotCommands.HELP:
                    await bot.SendMessage(msg.Chat.Id, "Всем привет! Я - бот доставки еды. \r\nПока я ещё совсем молодой и почти ничего не умею, но в будущем смогу отображать меню, помогать с оформлением и отслеживанием заказов.\r\nПожелайте мне успехов в развитии!♥️", cancellationToken: ct);
                    break;

                default:
                    // проверка на то, может ли быть пользователь в процессе оформления заказа
                    OrderProcessingResult orderResult = await _makingOrdersService.ProcessUserInputAsync(msg.From.Id, text);

                    if (orderResult.Success || !string.IsNullOrEmpty(orderResult.Message))
                    {
                        await SendOrderResultMessage(bot, msg.Chat.Id, orderResult, ct);
                    }
                    else
                    {
                        await bot.SendMessage(msg.Chat.Id, "Вас не понял... Попробуйте команду /help.", cancellationToken: ct);
                    }
                    await _cartService.HandleMessageAsync(bot, msg, ct);
                    break;
            }
        }
        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"[{LogMessages.ERROR}]: {ex.Message}");
            return Task.CompletedTask;
        }

        // Показываем предложение оформить заказ с инлайн-кнопкой
        private async Task ShowOrderSuggestion(ITelegramBotClient bot, long chatId, CancellationToken ct)
        {
            string message = "✅ Вы авторизованы!\n\n" +
                          "🎉 Отлично! Теперь вы можете оформить свой заказ!\n\n" +
                          "Готовы начать?";

            // Создаем инлайн-кнопку
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🛍️ Оформить заказ", "start_order")
                }
            });

            await bot.SendMessage(chatId,
                message,
                replyMarkup: inlineKeyboard,
                cancellationToken: ct);
        }

        // Обработка нажатий на инлайн-кнопки
        private async Task HandleCallbackQueryAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
        {
            try
            {
                await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

                if (callbackQuery.Data == "start_order")
                {
                    // Нажата кнопка "Оформить заказ"
                    await bot.AnswerCallbackQuery(callbackQuery.Id, "Начинаем оформление заказа!", cancellationToken: ct);
                    Message message = new Message
                    {
                        Chat = callbackQuery.Message.Chat,
                        From = callbackQuery.From,
                        Text = callbackQuery.Data,
                    };
                    await HandleOrderCommand(bot, message, ct);
                }
                else if (IsOrderButton(callbackQuery.Data)) // Если нажата кнопка, связанная с оформлением заказа
                {
                    OrderProcessingResult orderResult = await _makingOrdersService.ProcessUserInputAsync(callbackQuery.From.Id, callbackQuery.Data);
                    await SendOrderResultMessage(bot, callbackQuery.Message.Chat.Id, orderResult, ct);
                }
                else
                {
                    await bot.AnswerCallbackQuery(callbackQuery.Id, "Неизвестная команда", cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка в HandleCallbackQueryAsync: {ex}");
                await bot.AnswerCallbackQuery(callbackQuery.Id, "Произошла ошибка", cancellationToken: ct);
            }
        }

        // Проверка является ли callbackQuery кнопкой заказа
        private bool IsOrderButton(string callbackData)
        {
            string[] orderButtons = { "add_comment", "skip_comment", "confirm_order", "cancel_order" };
            return orderButtons.Contains(callbackData);
        }

        // Отправка результата обработки заказа
        private async Task SendOrderResultMessage(ITelegramBotClient bot, long chatId, OrderProcessingResult result, CancellationToken ct)
        {
            if (result.Success)
            {
                if (result.HasInlineKeyboard && result.InlineKeyboard != null)
                {
                    await bot.SendMessage(chatId,
                        result.Message,
                        replyMarkup: result.InlineKeyboard,
                        cancellationToken: ct);
                }
                else
                {
                    await bot.SendMessage(chatId,
                        result.Message,
                        cancellationToken: ct);
                }
            }
            else
            {
                await bot.SendMessage(chatId,
                    result.Message,
                    cancellationToken: ct);
            }
        }

        // Начало оформления заказа
        private async Task HandleOrderCommand(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            // Проверяем авторизацию
            bool isAuthorized = await _authorizationService.IsUserAuthorizedAsync(msg.From.Id);
            if (!isAuthorized)
            {
                await bot.SendMessage(msg.Chat.Id,
                    "❌ Сначала необходимо авторизоваться! Напишите /start",
                    cancellationToken: ct);
                return;
            }

            // TODO: Здесь будет проверка, что корзина не пуста
            // Пока что имитируем, что корзина с товарами пуста
            // Заглушка - потом заменить на реальную проверку
            bool isCartEmpty = false;

            if (isCartEmpty)
            {
                await bot.SendMessage(msg.Chat.Id,
                    "🛒 Ваша корзина пуста!\n\n" +
                    "Добавьте товары в корзину, чтобы оформить заказ.",
                    cancellationToken: ct);
                return;
            }

            // Начинаем процесс оформления заказа
            bool success = await _makingOrdersService.StartOrderCreationAsync(msg.From.Id);
            if (success)
            {
                await bot.SendMessage(msg.Chat.Id,
                    "📝 Введите адрес доставки в формате:\nул. <улица>, д. <номер дома>, кв. <номер квартиры>\n\n" +
                    "Пример: ул. Ленина, д. 15, кв. 42\n\n" +
                    "Допустимые форматы дома: 15, 15а, 15/1, 15/1а",
                    cancellationToken: ct);
            }
            else
            {
                await bot.SendMessage(msg.Chat.Id,
                    "❌ Не удалось начать оформление заказа. Попробуйте позже.",
                    cancellationToken: ct);
            }
        }
    }
}
