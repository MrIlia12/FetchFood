namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Команда показа инструкции для удаления позиции.
    /// </summary>
    public class DeletePositionPromptCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.DELETE;

        public override async Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            await SendMessageAsync(ctx,
                $"Чтобы удалить позицию:\n" +
                $"{BotCommands.MENU}:{BotCommands.DELETE_POSITION}:<название>\n\n" +
                $"Пример:\n" +
                $"{BotCommands.MENU}:{BotCommands.DELETE_POSITION}:Бургер");
            return true;
        }
    }
}
