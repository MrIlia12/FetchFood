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
            BotCommandHandler? handler = update.Type is UpdateType.CallbackQuery
                ? await GetHandlerAsync(update, update.CallbackQuery.Data)
                : update.Message.Text == BotCommands.START || update.Message.Type is MessageType.Contact
                    ? new BotAuthorizationHandler(update, this._bot, this._authorizationService)
                    : await HandleReplyMessage(update);

            handler?.Invoke();
            return;
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
                MenuCommand.MENU => new BotMenuHandler(update, this._bot, this._menuService, this._categoryService, this._authorizationService),
                AdministrationCommands.ADMIN => new BotAdministrationHandler(update, this._bot, this._administrationService),
                BotCommands.CART => new BotCartHandler(update, this._bot, this._cartService),
                CourierCommands.COURIER => new BotCourierHandler(update, this._bot, this._courierService)
            };

            return handler;
        }

        private Task<BotCommandHandler?> HandleReplyMessage(Update update)
        {
            var chatId = update.Message?.Chat.Id ?? 0;

            // Проверяем есть ли сохранённая команда для меню
            if (BotMenuHandler.HasPendingCommand(chatId))
            {
                return Task.FromResult<BotCommandHandler?>(
                    new BotMenuHandler(update, this._bot, this._menuService, this._categoryService, this._authorizationService));
            }

            // Если нет reply - вернуть null
            if (update.Message?.ReplyToMessage?.Text is not { } replyMessage)
                return Task.FromResult<BotCommandHandler?>(null);

            // Проверяем по префиксу команды в тексте промпта
            if (replyMessage.Contains($"{MenuCommand.MENU}:"))
            {
                return Task.FromResult<BotCommandHandler?>(
                    new BotMenuHandler(update, this._bot, this._menuService, this._categoryService, this._authorizationService));
            }

            if (replyMessage.Contains($"{MakingOrdersCommand.ORDER}:"))
            {
                return Task.FromResult<BotCommandHandler?>(
                    new BotMakingOrdersHandler(update, this._bot, this._makingOrdersService));
            }

            // Неизвестный reply - игнорируем
            return Task.FromResult<BotCommandHandler?>(null);
        }

        private static Task HandleErrorAsync(ITelegramBotClient _, Exception ex, CancellationToken __)
        {
            Console.WriteLine($"[{LogMessages.ERROR}]: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
