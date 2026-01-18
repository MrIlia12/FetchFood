using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Navigation
{
    /// <summary>
    /// Команда отображения списка категорий.
    /// </summary>
    public class CategoriesCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.CATEGORIES;

        public override async Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            var categories = await ctx.CategoryService.GetAllCategoriesAsync(ctx.CancellationToken);
            var isAdmin = await ctx.IsAdminAsync();

            var rows = new List<InlineKeyboardButton[]>();

            if (categories.Count == 0)
            {
                if (isAdmin)
                {
                    rows.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "➕ Добавить категорию",
                            $"{BotCommands.MENU}:{BotCommands.ADD_CATEGORY}:")
                    });
                }
                rows.Add(new[] { BackToMenuButton() });

                await SendMessageAsync(ctx,
                    "Пока нет категорий. Все позиции отображаются в общем меню.",
                    new InlineKeyboardMarkup(rows));
                return true;
            }

            var categoryButtons = categories
                .Select(c => InlineKeyboardButton.WithCallbackData(
                    c.Name ?? $"Категория #{c.PositionCategoryId}",
                    $"{BotCommands.MENU}:{BotCommands.CATEGORY_POSITIONS}:{c.PositionCategoryId}"))
                .Chunk(2)
                .Select(r => r.ToArray())
                .ToList();

            rows.AddRange(categoryButtons);

            if (isAdmin)
            {
                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Добавить", $"{BotCommands.MENU}:{BotCommands.ADD_CATEGORY}:"),
                    InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"{BotCommands.MENU}:{BotCommands.EDIT_CATEGORY}:select"),
                    InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"{BotCommands.MENU}:{BotCommands.DELETE_CATEGORY}:select")
                });
            }

            rows.Add(new[] { BackToMenuButton() });

            await SendMessageAsync(ctx, "Выберите категорию:", new InlineKeyboardMarkup(rows));
            return true;
        }
    }
}
