using FetchFood.States;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FetchFood.Services
{
    /// <summary>
    /// Абстрактный класс обработчика комманд (паттерн Фабричного метода)
    /// </summary>
    abstract class BotCommandHandler
    {
        /// <summary>
        /// Данные акта взаимодействия с ботом.
        /// </summary>
        protected Update Update { get; set; }

        /// <summary>
        /// Клиент бота.
        /// </summary>
        protected ITelegramBotClient _bot { get; set; }

        // Отслеживает, какого ответа бот ждет от пользователя
        // Статический словарь, сохраняет состояние между всеми экземплярами обработчика
        protected readonly ConcurrentDictionary<long, UserState> _userState;

        /// <summary>
        /// ctor.
        /// </summary>
        public BotCommandHandler(Update update, ITelegramBotClient botClient, ConcurrentDictionary<long, UserState> userState)
        {
            Update = update;
            _bot = botClient;
            _userState = userState;
        }

        /// <summary>
        /// Метод вызова обработчика.
        /// </summary>
        abstract public Task Invoke();
    }
}
