using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using BusinessLogic.Services.Administration.Abstraction;
using Telegram.Bot;
using FetchFood.States;
using System.Collections.Concurrent;

namespace FetchFood.Services
{
    class BotAdministrationHandler : BotCommandHandler
    {
        private readonly IAdministrationService _administrationnService;

        /// <summary>
        /// ctor.
        /// </summary>
        public BotAdministrationHandler(
            Update update,
            ITelegramBotClient botClient, 
            IAdministrationService administrationnService,
            ConcurrentDictionary<long, UserState> userState) 
            : base(update, botClient, userState)
        {
            _administrationnService = administrationnService;
        }

        public override async Task Invoke()
        {
           throw new NotImplementedException();
        }
    }
}
