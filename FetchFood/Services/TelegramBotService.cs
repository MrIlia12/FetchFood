using BusinessLogic.Services.Administration.Abstraction;
using BusinessLogic.Services.Administration.Models;
using BusinessLogic.Services.Authorization.Abstractions;
using BusinessLogic.Services.MakingOrders.Abstractions;
using BusinessLogic.Services.Menu.Abstractions;
using BusinessLogic.Services.Cart.Abstractions;
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
        private readonly ICartService _cartService;
        private readonly BusinessLogic.Services.Menu.Abstractions.ICategoryService _categoryService;

        public TelegramBotService(IAuthorizationService authorizationService, ICartService cartService, IAdministrationService administrationService, IMenuService menuService, IMakingOrdersService makingOrdersService, BusinessLogic.Services.Menu.Abstractions.ICategoryService categoryService)
        {
            _authorizationService = authorizationService;
            _administrationService = administrationService;
            _cartService = cartService;
            _menuService = menuService;
            _makingOrdersService = makingOrdersService;
            _categoryService = categoryService;
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


            BotCommandHandler? handler = update.Type is UpdateType.CallbackQuery
                ? await GetHandlerAsync(update, update.CallbackQuery.Data)
                : update.Message.Text == BotCommands.START || update.Message.Type is MessageType.Contact
                    ? new BotAuthorizationHandler(update, this._bot, this._authorizationService)
                    : await HandleReplyMessage(update);

            handler?.Invoke();
        }

        private async Task<BotCommandHandler> GetHandlerAsync(Update update, string command)
        {
            string commandPrefix;
            try
            {
                commandPrefix = command.Split(CommandsBase.Separator)[0];
            }
            catch 
            {
                throw new ArgumentException("Неверный формат команды.");
            }

            BotCommandHandler handler = commandPrefix switch
            {
                MakingOrdersCommand.ORDER => new BotMakingOrdersHandler(update, this._bot, this._makingOrdersService),
                MenuCommand.MENU => new BotMenuHandler(update, this._bot, this._menuService, this._categoryService),
                AdministrationCommands.ADMIN => new BotAdministrationHandler(update, this._bot, this._administrationService),
                BotCommands.CART => new BotCartHandler(update, this._bot, this._cartService)
            };

            return handler;
        }

        private async Task<BotCommandHandler> HandleReplyMessage(Update update)
        {
            var replyMessage = update.Message.ReplyToMessage.Text;

            BotCommandHandler handler = replyMessage switch
            {
                BotCommands.MENU1 => new BotMenuHandler(update, this._bot, this._menuService, this._categoryService),
                BotCommands.ORDER1 => new BotMakingOrdersHandler(update, this._bot, this._makingOrdersService),
            };

            return handler;
        }

        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"[{LogMessages.ERROR}]: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}

