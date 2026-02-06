using BusinessLogic.Services.Authorization.Abstractions;
using BusinessLogic.Services.Menu.Abstractions;
using Telegram.Bot;

namespace FetchFood.Commands.Menu
{
    /// <summary>
    /// Контекст выполнения команды меню.
    /// Содержит все необходимые зависимости и данные для выполнения команды.
    /// </summary>
    public class MenuCommandContext
    {
        /// <summary>
        /// Telegram Bot Client
        /// </summary>
        public ITelegramBotClient Bot { get; }

        /// <summary>
        /// ID чата (пользователя)
        /// </summary>
        public long ChatId { get; }

        /// <summary>
        /// Аргументы команды (часть после второго ':')
        /// </summary>
        public string Args { get; }

        /// <summary>
        /// Токен отмены
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Сервис меню
        /// </summary>
        public IMenuService MenuService { get; }

        /// <summary>
        /// Сервис категорий
        /// </summary>
        public ICategoryService CategoryService { get; }

        /// <summary>
        /// Сервис авторизации
        /// </summary>
        public IAuthorizationService AuthorizationService { get; }

        /// <summary>
        /// Кэшированный статус админа (null = ещё не проверялось)
        /// </summary>
        private bool? _isAdminCached;

        public MenuCommandContext(
            ITelegramBotClient bot,
            long chatId,
            string args,
            IMenuService menuService,
            ICategoryService categoryService,
            IAuthorizationService authorizationService,
            CancellationToken cancellationToken = default)
        {
            Bot = bot;
            ChatId = chatId;
            Args = args;
            MenuService = menuService;
            CategoryService = categoryService;
            AuthorizationService = authorizationService;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Проверить, является ли пользователь администратором.
        /// Результат кэшируется на время жизни контекста.
        /// </summary>
        public async Task<bool> IsAdminAsync()
        {
            // Если уже проверяли - возвращаем кэшированное значение
            if (_isAdminCached.HasValue)
                return _isAdminCached.Value;

            // Первая проверка - запрашиваем у сервиса и кэшируем
            _isAdminCached = await AuthorizationService.IsUserAdministratorAsync(ChatId);
            return _isAdminCached.Value;
        }
    }
}
