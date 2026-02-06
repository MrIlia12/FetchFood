using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Categories
{
    /// <summary>
    /// Команда удаления категории.
    /// </summary>
    public class DeleteCategoryCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.DELETE_CATEGORY;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            // Если args = "select" - показать список для выбора
            if (ctx.Args == "select")
            {
                return await ShowCategorySelectionAsync(ctx);
            }

            // Если args = ID категории - удалить
            if (int.TryParse(ctx.Args, out var categoryId))
            {
                return await DeleteCategoryAsync(ctx, categoryId);
            }

            await SendMessageAsync(ctx, "Некорректный формат команды.");
            return true;
        }

        private async Task<bool> ShowCategorySelectionAsync(MenuCommandContext ctx)
        {
            var categories = await ctx.CategoryService.GetAllCategoriesAsync(ctx.CancellationToken);

            if (categories.Count == 0)
            {
                await SendMessageAsync(ctx,
                    "Нет категорий для удаления.",
                    new InlineKeyboardMarkup(new[] { new[] { BackToCategoriesButton() } }));
                return true;
            }

            var rows = categories
                .Select(c => new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"🗑 {c.Name ?? $"#{c.PositionCategoryId}"}",
                        $"{BotCommands.MENU}:{BotCommands.DELETE_CATEGORY}:{c.PositionCategoryId}")
                })
                .ToList();

            rows.Add(new[] { BackToCategoriesButton() });

            await SendMessageAsync(ctx, "Выберите категорию для удаления:", new InlineKeyboardMarkup(rows));
            return true;
        }

        private async Task<bool> DeleteCategoryAsync(MenuCommandContext ctx, int categoryId)
        {
            var category = await ctx.CategoryService.GetCategoryByIdAsync(categoryId, ctx.CancellationToken);

            if (category == null)
            {
                await SendMessageAsync(ctx, "Категория не найдена.");
                return true;
            }

            var result = await ctx.CategoryService.DeleteAsync(categoryId, ctx.CancellationToken);

            if (result)
            {
                await SendMessageAsync(ctx,
                    $"🗑 Категория «{category.Name}» удалена!",
                    new InlineKeyboardMarkup(new[] { new[] { BackToCategoriesButton() } }));
            }
            else
            {
                await SendMessageAsync(ctx, "❌ Не удалось удалить категорию.");
            }

            return true;
        }
    }
}
