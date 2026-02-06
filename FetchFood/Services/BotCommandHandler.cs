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

        /// <summary>
        /// ctor.
        /// </summary>
        public BotCommandHandler(Update update, ITelegramBotClient botClient)
        {
            Update = update;
            _bot = botClient;
        }

        /// <summary>
        /// Метод вызова обработчика.
        /// </summary>
        abstract public void Invoke();
    }
}
