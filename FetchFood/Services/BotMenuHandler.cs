using BusinessLogic.Services.Menu.Abstractions;
using BusinessLogic.Services.Authorization.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using FetchFood.Commands.Menu;
using FetchFood.Commands.Menu.Navigation;
using FetchFood.Commands.Menu.Categories;
using FetchFood.Commands.Menu.Positions;

namespace FetchFood.Services
{
    /// <summary>
    /// Обработчик команд меню.
    /// Использует Command Pattern для маршрутизации команд.
    /// </summary>
    class BotMenuHandler : BotCommandHandler
    {
        private readonly MenuCommandDispatcher _dispatcher;

        public BotMenuHandler(
            Update update,
            ITelegramBotClient botClient,
            IMenuService menuService,
            ICategoryService categoryService,
            IAuthorizationService authorizationService) : base(update, botClient)
        {
            _dispatcher = new MenuCommandDispatcher(menuService, categoryService, authorizationService);
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            _dispatcher.RegisterAll(
                // Навигация
                new PageCommand(),
                new BackCommand(),
                new PositionCommand(),
                new CategoriesCommand(),
                new CategoryPositionsCommand(),
                new FindCommand(),

                // CRUD позиций
                new AddPositionHandler(),
                new ConfirmDeletePositionCommand(),
                new DeletePositionCommand(),

                // Редактирование позиций
                new EditPositionMenuCommand(),
                new EditNameCommand(),
                new EditPriceCommand(),
                new EditIngredientsCommand(),
                new EditDescriptionCommand(),
                new EditImageCommand(),
                new EditPositionCategoryCommand(),
                new QuickEditPositionCommand(),

                // CRUD категорий
                new AddCategoryCommand(),
                new EditCategoryCommand(),
                new EditCategoryNameCommand(),
                new EditCategoryDescCommand(),
                new DeleteCategoryCommand(),
                new QuickEditCategoryCommand()
            );
        }

        public override async void Invoke()
        {
            string? data = string.Empty;
            long chatId;

            if (Update.CallbackQuery != null)
            {
                var callbackQuery = Update.CallbackQuery;
                chatId = callbackQuery.Message!.Chat.Id;
                data = callbackQuery.Data;
            }
            else if (Update.Message != null)
            {
                var mssg = Update.Message;
                chatId = mssg.Chat.Id;

                // Обработка фото: извлекаем FileId и формируем команду из ReplyToMessage
                if (mssg.Photo != null && mssg.Photo.Length > 0)
                {
                    var fileId = mssg.Photo.Last().FileId;
                    var replyText = mssg.ReplyToMessage?.Text;

                    // Проверяем активную сессию создания позиции (шаг image)
                    if (AddPositionHandler.HasActiveSession(chatId) &&
                        AddPositionHandler.GetCurrentStep(chatId) == "image")
                    {
                        data = $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:photo:{fileId}";
                    }
                    else if (!string.IsNullOrEmpty(replyText) && replyText.Contains($"{BotCommands.MENU}:"))
                    {
                        // Извлекаем команду из текста промпта (например "menu:edit_image:5:")
                        var commandMatch = System.Text.RegularExpressions.Regex.Match(
                            replyText,
                            $@"{BotCommands.MENU}:(\w+):(\d+):");

                        if (commandMatch.Success)
                        {
                            var action = commandMatch.Groups[1].Value;
                            var id = commandMatch.Groups[2].Value;
                            data = $"{BotCommands.MENU}:{action}:{id}:{fileId}";
                        }
                    }
                }
                // Проверяем активную сессию создания позиции для текстового ввода
                else if (AddPositionHandler.HasActiveSession(chatId) && !string.IsNullOrEmpty(mssg.Text))
                {
                    data = $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:{mssg.Text}";
                }
                else
                {
                    data = mssg.Text;
                }
            }
            else
            {
                return;
            }

            await HandleMenuCommandAsync(_bot, chatId, data ?? string.Empty);
        }

        private async Task HandleMenuCommandAsync(ITelegramBotClient bot, long chatId, string message, CancellationToken ct = default)
        {
            var messageSplitted = message.Split(':', 3, StringSplitOptions.TrimEntries);

            // Если пришла просто команда "menu" - показать первую страницу
            if (messageSplitted.Length <= 1)
            {
                await _dispatcher.DispatchAsync(bot, chatId, BotCommands.PAGE, "0", ct);
                return;
            }

            string action = messageSplitted[1];
            string args = messageSplitted.Length >= 3 ? messageSplitted[2] : BotCommands.EMPTY;

            // Пытаемся выполнить команду через диспетчер
            var handled = await _dispatcher.DispatchAsync(bot, chatId, action, args, ct);

            if (!handled)
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: "Неизвестная команда управления меню.",
                    cancellationToken: ct);
            }
        }
    }
}
