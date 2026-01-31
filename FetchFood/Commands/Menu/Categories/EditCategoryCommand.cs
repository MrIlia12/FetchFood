using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Categories
{
    /// <summary>
    /// Команда редактирования категории (показ меню или выбор категории).
    /// </summary>
    public class EditCategoryCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.EDIT_CATEGORY;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            // Если args = "select" - показать список для выбора
            if (ctx.Args == "select")
            {
                return await ShowCategorySelectionAsync(ctx);
            }

            // Если args = ID категории - показать меню редактирования
            if (int.TryParse(ctx.Args, out var categoryId))
            {
                return await ShowEditMenuAsync(ctx, categoryId);
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
                    "Нет категорий для редактирования.",
                    new InlineKeyboardMarkup(new[] { new[] { BackToCategoriesButton() } }));
                return true;
            }

            var rows = categories
                .Select(c => new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        c.Name ?? $"#{c.PositionCategoryId}",
                        $"{BotCommands.MENU}:{BotCommands.EDIT_CATEGORY}:{c.PositionCategoryId}")
                })
                .ToList();

            rows.Add(new[] { BackToCategoriesButton() });

            await SendMessageAsync(ctx, "Выберите категорию для редактирования:", new InlineKeyboardMarkup(rows));
            return true;
        }

        private async Task<bool> ShowEditMenuAsync(MenuCommandContext ctx, int categoryId)
        {
            var category = await ctx.CategoryService.GetCategoryByIdAsync(categoryId, ctx.CancellationToken);

            if (category == null)
            {
                await SendMessageAsync(ctx, "Категория не найдена.");
                return true;
            }

            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("📝 Изменить название", $"{BotCommands.MENU}:{BotCommands.EDIT_CATEGORY_NAME}:{categoryId}:") },
                new[] { InlineKeyboardButton.WithCallbackData("📄 Изменить описание", $"{BotCommands.MENU}:{BotCommands.EDIT_CATEGORY_DESC}:{categoryId}:") },
                new[] { BackToCategoriesButton() }
            };

            await SendMessageAsync(ctx,
                $"Редактирование категории «{category.Name}»:\n\n" +
                $"📝 Название: {category.Name}\n" +
                $"📄 Описание: {category.Description ?? "—"}",
                new InlineKeyboardMarkup(rows));

            return true;
        }
    }
}
