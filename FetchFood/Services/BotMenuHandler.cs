using BusinessLogic.Services.Menu.Abstractions;
using BusinessLogic.Services.Authorization.Abstractions;
using FetchFood.Commands.Menu;
using FetchFood.Commands.Menu.Navigation;
using FetchFood.Commands.Menu.Categories;
using FetchFood.Commands.Menu.Positions;
using Telegram.Bot;
using Telegram.Bot.Types;
using FetchFood.States;
using System.Collections.Concurrent;

namespace FetchFood.Services
{
    // Обработчик команд меню
    class BotMenuHandler : BotCommandHandler
    {
        private readonly MenuCommandDispatcher _dispatcher;

        // Словарь для хранения ожидаемых команд от пользователей
        private static Dictionary<long, string> _pendingCommands = new Dictionary<long, string>();

        // Сохранить команду которую ждём от пользователя
        public static void SetPendingCommand(long chatId, string command)
        {
            _pendingCommands[chatId] = command;
        }

        // Получить сохранённую команду (и удалить её)
        public static string? GetPendingCommand(long chatId)
        {
            if (_pendingCommands.ContainsKey(chatId))
            {
                var command = _pendingCommands[chatId];
                _pendingCommands.Remove(chatId);
                return command;
            }
            return null;
        }

        // Проверить есть ли сохранённая команда
        public static bool HasPendingCommand(long chatId)
        {
            return _pendingCommands.ContainsKey(chatId);
        }

        // Удалить сохранённую команду
        public static void RemovePendingCommand(long chatId)
        {
            if (_pendingCommands.ContainsKey(chatId))
            {
                _pendingCommands.Remove(chatId);
            }
        }

        public BotMenuHandler(
            Update update,
            ITelegramBotClient botClient,
            IMenuService menuService,
            ICategoryService categoryService,
            IAuthorizationService authorizationService,
            ConcurrentDictionary<long, UserState> usersState) : base(update, botClient, usersState)
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

        public override async Task Invoke()
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

                // Получаем сохранённую команду (если есть)
                var pending = GetPendingCommand(chatId);

                // Обработка фото
                if (mssg.Photo != null && mssg.Photo.Length > 0)
                {
                    var fileId = mssg.Photo.Last().FileId;

                    if (pending != null)
                    {
                        data = pending + fileId;
                    }
                    else
                    {
                        // проверяем ReplyToMessage
                        var replyText = mssg.ReplyToMessage?.Text;
                        if (!string.IsNullOrEmpty(replyText) && replyText.Contains($"{BotCommands.MENU}:"))
                        {
                            var commandMatch = System.Text.RegularExpressions.Regex.Match(
                                replyText,
                                $@"{BotCommands.MENU}:(\w+):(\w+):");

                            if (commandMatch.Success)
                            {
                                var action = commandMatch.Groups[1].Value;
                                var arg = commandMatch.Groups[2].Value;
                                data = $"{BotCommands.MENU}:{action}:{arg}:{fileId}";
                            }
                        }
                    }
                }
                // Обработка текста
                else if (pending != null && !string.IsNullOrEmpty(mssg.Text))
                {
                    data = pending + mssg.Text;
                }
                else
                {
                    // проверяем ReplyToMessage
                    var replyText = mssg.ReplyToMessage?.Text;
                    if (!string.IsNullOrEmpty(replyText) && replyText.Contains($"{BotCommands.MENU}:") && !string.IsNullOrEmpty(mssg.Text))
                    {
                        var commandMatch = System.Text.RegularExpressions.Regex.Match(
                            replyText,
                            $@"{BotCommands.MENU}:(\w+):(\w+):");

                        if (commandMatch.Success)
                        {
                            var action = commandMatch.Groups[1].Value;
                            var arg = commandMatch.Groups[2].Value;
                            data = $"{BotCommands.MENU}:{action}:{arg}:{mssg.Text}";
                        }
                        else
                        {
                            data = mssg.Text;
                        }
                    }
                    else
                    {
                        data = mssg.Text;
                    }
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
