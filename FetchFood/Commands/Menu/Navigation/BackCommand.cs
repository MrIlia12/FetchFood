namespace FetchFood.Commands.Menu.Navigation
{
    /// <summary>
    /// Команда возврата в главное меню (перенаправляет на PageCommand).
    /// </summary>
    public class BackCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.BACK;

        private readonly PageCommand _pageCommand = new();

        public override Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            // Создаём новый контекст с args = "0" для первой страницы
            var newContext = new MenuCommandContext(
                ctx.Bot,
                ctx.ChatId,
                "0",
                ctx.MenuService,
                ctx.CategoryService,
                ctx.AuthorizationService,
                ctx.CancellationToken);

            return _pageCommand.ExecuteAsync(newContext);
        }
    }
}
