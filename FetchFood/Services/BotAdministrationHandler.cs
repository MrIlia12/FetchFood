using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using BusinessLogic.Services.Administration.Abstraction;
using Telegram.Bot;

namespace FetchFood.Services
{
    class BotAdministrationHandler : BotCommandHandler
    {
        private readonly IAdministrationService _administrationnService;

        /// <summary>
        /// ctor.
        /// </summary>
        public BotAdministrationHandler(Update update, ITelegramBotClient botClient, IAdministrationService administrationnService) : base(update, botClient)
        {
            _administrationnService = administrationnService;
        }

        public override async void Invoke()
        {

        }
    }
}
