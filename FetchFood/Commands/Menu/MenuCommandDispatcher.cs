using BusinessLogic.Services.Authorization.Abstractions;
using BusinessLogic.Services.Menu.Abstractions;
using Telegram.Bot;

namespace FetchFood.Commands.Menu
{
    // Диспетчер команд меню
    // Хранит все команды и вызывает нужную по ключу
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

        // Зарегистрировать одну команду
        public MenuCommandDispatcher Register(IMenuCommand command)
        {
            _commands[command.CommandKey] = command;
            return this;
        }

        // Зарегистрировать несколько команд сразу
        public MenuCommandDispatcher RegisterAll(params IMenuCommand[] commands)
        {
            foreach (var command in commands)
            {
                Register(command);
            }
            return this;
        }

        // Выполнить команду по ключу
        public async Task<bool> DispatchAsync(
            ITelegramBotClient bot,
            long chatId,
            string commandKey,
            string args,
            CancellationToken ct = default)
        {
            if (!_commands.ContainsKey(commandKey))
            {
                return false;
            }

            var command = _commands[commandKey];

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
    }
}
