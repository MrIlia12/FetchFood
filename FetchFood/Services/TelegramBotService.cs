using BusinessLogic.Services.Administration.Abstraction;
using BusinessLogic.Services.Administration.Models;
using BusinessLogic.Services.Authorization.Abstractions;
using BusinessLogic.Services.MakingOrders.Abstractions;
using BusinessLogic.Services.Menu.Abstractions;
using DataAccess.Entities.Models;
using FetchFood.Abstractions;
using FetchFood.Commands;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;


namespace FetchFood.Services
{
    // Версия библиотеки Telegram.Bot: 22.6.2
    internal class TelegramBotService : ITelegramBotService
    {
        private TelegramBotClient _bot;
        private readonly CancellationTokenSource _cts = new();
        private readonly IAuthorizationService _authorizationService;
        private readonly IMenuService _menuService;
        private readonly IAdministrationService _administrationService;
        private readonly IMakingOrdersService _makingOrdersService;

        private readonly ITelegramBotCartService _cartService;

        public TelegramBotService(IAuthorizationService authorizationService, ITelegramBotCartService cartService, IAdministrationService administrationService, IMenuService menuService, IMakingOrdersService makingOrdersService)
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
                // Указываем, что мы хотим получать ВСЕ типы обновлений (включая CallbackQuery)
                AllowedUpdates = { }
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
            // Обработка текстовых сообщений от пользователя
            if (update.Type is UpdateType.Message)
            {
                string? command = update.Message.Text.Split(' ')[0];
                switch (command)
                {
                    case BotCommands.START:
                        await bot.SendMessage(update.Message.Chat.Id, "Привет! Меня зовут FetchFood. \nСейчас я проверю, знакомы ли мы. \nТакже Вы можете написать /help, чтобы узнать, что я могу!", cancellationToken: ct);

                        // Лия (2025-12-03): договорились, что будем показывать кнопку Меню сразу после старта бота.
                        // Пока не очень понимаю, как сделать это правильно, поэтому вынесла показ кнопки в публичный метод.
                        var menuCommandHandler = new BotMenuHandler(update, bot, _menuService);
                        await menuCommandHandler.ShowMenuButton(bot, string.Empty, update.Message.Chat.Id, ct);
                        //

                        var isAdministrator = await _authorizationService.IsUserAdministratorAsync(update.Message.From.Id);

                        // TODO: ПЕРЕНЕСТИ ЭТОТ ВЫЗОВ КНОПКИ В СЕРВИС КОРЗИНЫ
                        // ЗДЕСЬ НУЖЕН ТОЛЬКО ДЛЯ ПРОВЕРКИ
                        if (isAdministrator)
                        {
                            return;
                        }
                        else
                        {
                            // Если авторизован - предлагаем начать оформление заказа
                            await ShowOrderSuggestion(bot, update.Message.Chat.Id, ct);
                        }

                        break;

                    case BotCommands.HELP:
                        await bot.SendMessage(update.Message.Chat.Id, "Всем привет! Я - бот доставки еды. \r\nПока я ещё совсем молодой и почти ничего не умею, но в будущем смогу отображать меню, помогать с оформлением и отслеживанием заказов.\r\nПожелайте мне успехов в развитии!♥️", cancellationToken: ct);
                        break;

                    default:
                        
                        // Проверка находится ли пользователь в процессе оформления заказа
                        bool isInOrderProcess = await _makingOrdersService.IsUserInOrderProcessAsync(update.Message.From.Id);
                        if (isInOrderProcess)
                        {
                            var commandHandler = new BotMakingOrdersHandler(update, bot, _makingOrdersService);
                            commandHandler.Invoke();
                            return;
                        }
                        else
                        {
                            // Проверка на авторизацию пользователя
                            var commandHandler = new BotAuthorizationHandler(update, bot, _authorizationService);
                            commandHandler.Invoke();
                            // если была подана текстовая команда управления меню
                            if (update.Message.Text.StartsWith(BotCommands.MENU))
                            {
                                var menuMessageCommandHandler = new BotMenuHandler(update, bot, _menuService);
                                menuMessageCommandHandler.Invoke();
                                return;
                            }
                            // // Лия (2025-12-03): если подано сообщение ни для сервиса меню, ни для сервиса заказо, проверяем сервис корзины.
                            else if (await _cartService.HandleMessageAsync(bot, update.Message, ct))
                            {
                                return;
                            }
                            // // Лия (2025-12-03): если ни один сервис не принял сообщение, переспрашиваем пользователя.
                            await bot.SendMessage(update.Message.Chat.Id, "Вас не понял... Попробуйте команду /help.", cancellationToken: ct);
                        }

                        break;
                }
            }

            // Обработка нажатий на кнопки
            if (update.Type is UpdateType.CallbackQuery)
            {
                if (update.CallbackQuery is not { } callBack) return;

                var callBackData = callBack.Data.Split(' ');

                // обработка ответов на команды меню
                if (callBackData[0].Contains(BotCommands.MENU))
                {
                    var menuCommandHandler = new BotMenuHandler(update, bot, _menuService);
                    menuCommandHandler.Invoke();
                    return;
                }
                //
                // Проверяем, начинается ли callback_data с префикса "cart_
                // Лия (2025-12-13): заменяю множественные проверки на одну, используя новую константу.
                //if (callBackData[0] == BotCommands.CART_SHOW ||
                //    callBackData[0] == BotCommands.CART_ADD ||
                //    callBackData[0] == BotCommands.CART_REMOVE ||
                //    callBackData[0] == BotCommands.CART_CLEAR)
                if (callBackData[0].StartsWith(BotCommands.CART_PREFIX))
                {
                    // Если да, передаем *весь* объект callBack
                    // в HandleCallbackQueryAsync нашего TelegramBotCartService
                    await _cartService.HandleCallbackQueryAsync(bot, callBack, ct);

                    return;
                }

                // обработка ответов на команды сервиса оформления заказов
                if (callBackData[0].Contains(MakingOrdersCommand.ORDER))
                {
                    var commandHandler = new BotMakingOrdersHandler(update, bot, _makingOrdersService);
                    commandHandler.Invoke();
                    await _bot.AnswerCallbackQuery(callBack.Id, cancellationToken: ct);
                    return;
                }

                var number = callBackData.Length > 1 ? Convert.ToInt32(callBackData[1]) : 0;

                switch (callBackData[0])
                {
                    case BotCommands.GETORDERS:
                        var order = await _administrationService.GetOrderInformationAsync(number);

                        // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
                        var keyboard = new InlineKeyboardMarkup();
                        var keyBoardButtons = new List<InlineKeyboardButton>();

                        if (order.OrderPosition is not OrderPosition.First and not OrderPosition.Lonely)
                        {
                            // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
                            keyBoardButtons.Add(new InlineKeyboardButton("⬅", $"GetOrder {number - 1}"));
                        }

                        // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
                        keyBoardButtons.Add(new InlineKeyboardButton("Выбрать", $"ToOrderMenu {order.Id} {order.Status}"));

                        if (order.OrderPosition is not OrderPosition.Last and not OrderPosition.Lonely)
                        {
                            // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
                            keyBoardButtons.Add(new InlineKeyboardButton("➡", $"GetOrder {number + 1}"));
                        }

                        // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
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
                        // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
                        var menuKeyboard = new InlineKeyboardMarkup();
                        var menuKeyboardButtons = new List<InlineKeyboardButton>();

                        if (callBackData[2] != OrderStatus.Delivered.ToString())
                        {
                            // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
                            menuKeyboardButtons.Add(new InlineKeyboardButton("Перевести заказ на следующий этап.", $"NextStep {number}"));
                        }

                        // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
                        menuKeyboardButtons.Add(new InlineKeyboardButton("Удалить заказ", $"DeleteOrder {number}"));

                        // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
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
                                    // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
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
                                    // --- ВОЗВРАЩЕНО (Изменение 4 отменено) ---
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
        }
        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"[{LogMessages.ERROR}]: {ex.Message}");
            return Task.CompletedTask;
        }


        // TODO: ПЕРЕНЕСТИ ЭТОТ ВЫЗОВ КНОПКИ В СЕРВИС КОРЗИНЫ
        // ЗДЕСЬ НУЖЕН ТОЛЬКО ДЛЯ ПРОВЕРКИ
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
                     InlineKeyboardButton.WithCallbackData("🛍️ Оформить заказ", MakingOrdersCommand.StartOrder.Command)
                 }
             });

            await bot.SendMessage(chatId,
                message,
                replyMarkup: inlineKeyboard,
                cancellationToken: ct);
        }
    }
}

