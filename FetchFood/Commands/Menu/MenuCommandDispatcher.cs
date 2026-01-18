using BusinessLogic.Services.Authorization.Abstractions;
using BusinessLogic.Services.Menu.Abstractions;
using Telegram.Bot;

namespace FetchFood.Commands.Menu
{
    /// <summary>
    /// Диспетчер команд меню.
    /// Регистрирует и маршрутизирует команды по их ключам.
    /// </summary>
    public class MenuCommandDispatcher
    {
        private readonly Dictionary<string, IMenuCommand> _commands = new();
        private readonly IMenuService _menuService;
        private readonly ICategoryService _categoryService;
        private readonly IAuthorizationService _authorizationService;

        public MenuCommandDispatcher(
            IMenuService menuService,
            ICategoryService categoryService,
            IAuthorizationService authorizationService)
        {
            _menuService = menuService;
            _categoryService = categoryService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Зарегистрировать команду
        /// </summary>
        public MenuCommandDispatcher Register(IMenuCommand command)
        {
            _commands[command.CommandKey] = command;
            return this;
        }

        /// <summary>
        /// Зарегистрировать несколько команд
        /// </summary>
        public MenuCommandDispatcher RegisterAll(params IMenuCommand[] commands)
        {
            foreach (var command in commands)
            {
                Register(command);
            }
            return this;
        }

        /// <summary>
        /// Выполнить команду по ключу
        /// </summary>
        /// <param name="bot">Telegram Bot Client</param>
        /// <param name="chatId">ID чата</param>
        /// <param name="commandKey">Ключ команды</param>
        /// <param name="args">Аргументы команды</param>
        /// <param name="ct">Токен отмены</param>
        /// <returns>True если команда обработана</returns>
        public async Task<bool> DispatchAsync(
            ITelegramBotClient bot,
            long chatId,
            string commandKey,
            string args,
            CancellationToken ct = default)
        {
            if (!_commands.TryGetValue(commandKey, out var command))
            {
                return false;
            }

            var context = new MenuCommandContext(
                bot,
                chatId,
                args,
                _menuService,
                _categoryService,
                _authorizationService,
                ct);

            return await command.ExecuteAsync(context);
        }

        /// <summary>
        /// Проверить, зарегистрирована ли команда
        /// </summary>
        public bool HasCommand(string commandKey) => _commands.ContainsKey(commandKey);

        /// <summary>
        /// Получить все зарегистрированные ключи команд
        /// </summary>
        public IEnumerable<string> GetRegisteredKeys() => _commands.Keys;
    }
}
