using Telegram.Bot;

namespace FetchFood.Commands.Menu
{
    /// <summary>
    /// Интерфейс команды меню.
    /// Каждая команда обрабатывает определённое действие в меню.
    /// </summary>
    public interface IMenuCommand
    {
        /// <summary>
        /// Ключ команды (например, "page", "pos", "addcat").
        /// Используется для маршрутизации.
        /// </summary>
        string CommandKey { get; }

        /// <summary>
        /// Выполнить команду.
        /// </summary>
        /// <param name="context">Контекст выполнения команды</param>
        /// <returns>True если команда обработана, False если нужно передать дальше</returns>
        Task<bool> ExecuteAsync(MenuCommandContext context);
    }
}
