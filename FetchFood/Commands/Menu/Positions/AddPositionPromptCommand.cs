using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Команда показа инструкции для добавления позиции.
    /// </summary>
    public class AddPositionPromptCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.ADD;

        public override async Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            await SendMessageAsync(ctx,
                BotCommands.MENU1,
                new ForceReplyMarkup { Selective = true });
            return true;
        }
    }
}
