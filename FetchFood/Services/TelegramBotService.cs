using BusinessLogic.Services.Administration.Abstraction;
using BusinessLogic.Services.Administration.Models;
using BusinessLogic.Services.Authorization.Abstractions;
using BusinessLogic.Services.MakingOrders.Abstractions;
using BusinessLogic.Services.Menu.Abstractions;
using BusinessLogic.Services.Cart.Abstractions;
using BusinessLogic.Services.Courier.Abstractions;
using DataAccess.Entities.Models;
using FetchFood.Abstractions;
using FetchFood.Commands;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Concurrent;
using FetchFood.States;


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
        private readonly ICourierService _courierService;
        private readonly ICartService _cartService;
        private readonly ConcurrentDictionary<long, UserState> _usersState = new();
        private readonly BusinessLogic.Services.Menu.Abstractions.ICategoryService _categoryService;

        public TelegramBotService(
            IAuthorizationService authorizationService,
            ICartService cartService,
            IAdministrationService administrationService,
            IMenuService menuService,
            IMakingOrdersService makingOrdersService,
            ICourierService courierService,
            ICategoryService categoryService)
        {
            _authorizationService = authorizationService;
            _administrationService = administrationService;
            _cartService = cartService;
            _menuService = menuService;
            _makingOrdersService = makingOrdersService;
            _categoryService = categoryService;
            _courierService = courierService,
            _courierService = courierService;
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
            var userId = update.Type is UpdateType.CallbackQuery
                ? update.CallbackQuery.From.Id
                : update.Message.From.Id;

            var userState = _usersState.GetOrAdd(userId, new UserState(new NonAuthorizedUser()));

            BotCommandHandler? handler = await GetHandlerAsync(update, userState);

            handler?.Invoke();
            return;
        }

        private async Task<BotCommandHandler> GetHandlerAsync(Update update, UserState userState)
        {
            string commandPrefix;
            BotCommandHandler handler;
            switch (userState.State)
            {
                case NonAuthorizedUser:
                    return new BotAuthorizationHandler(update, this._bot, this._authorizationService, this._usersState);

                case AuthorizedUser:
                    if (update.CallbackQuery.Data == MakingOrdersCommand.StartOrder.Command)
                    {
                        var userId = update.CallbackQuery.From.Id;
                        var state = this._usersState[userId];
                        this._usersState[userId].State.ToNextState(state);
                        return new BotMakingOrdersHandler(update, this._bot, this._makingOrdersService, this._usersState);
                    }

                    try
                    {
                        commandPrefix = update.CallbackQuery.Data.Split(CommandsBase.Separator)[0];
                    }
                    catch
                    {
                        throw new ArgumentException("Неверный формат команды.");
                    }

                    handler = commandPrefix switch
                    {
                        MenuCommand.MENU => new BotMenuHandler(update, this._bot, this._menuService, this._usersState),
                        AdministrationCommands.ADMIN => new BotAdministrationHandler(update, this._bot, this._administrationService, this._usersState),
                        BotCommands.CART => new BotCartHandler(update, this._bot, this._cartService, this._usersState),
                    };

                    if (handler is null)
                    {
                            await _bot.SendMessage(
                                    chatId: update.CallbackQuery.Message.Chat.Id,
                                    text: "В данный момент Вы не можете сделать это.");
                    }

                    return handler;

                case IsMakingOrder:
                    return new BotMakingOrdersHandler(update, this._bot, this._makingOrdersService, this._usersState);

                default:
                    throw new NotImplementedException();
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"[{LogMessages.ERROR}]: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
