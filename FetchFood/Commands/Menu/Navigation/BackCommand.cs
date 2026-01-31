using FetchFood.Services;

namespace FetchFood.Commands.Menu.Navigation
{
    // Команда возврата в главное меню
    public class BackCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.BACK;

        private readonly PageCommand _pageCommand = new();

        public override Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            // Удаляем сохранённую команду при возврате в меню
            BotMenuHandler.RemovePendingCommand(ctx.ChatId);

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
