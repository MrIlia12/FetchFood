using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Categories
{
    /// <summary>
    /// Команда изменения описания категории.
    /// </summary>
    public class EditCategoryDescCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.EDIT_CATEGORY_DESC;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            var parts = ctx.Args.Split(':', 2, StringSplitOptions.TrimEntries);

            if (parts.Length < 1 || !int.TryParse(parts[0], out var categoryId))
            {
                await SendMessageAsync(ctx, "Некорректный формат команды.");
                return true;
            }

            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                await SendMessageAsync(ctx,
                    $"Введите новое описание категории:\n{BotCommands.MENU}:{BotCommands.EDIT_CATEGORY_DESC}:{categoryId}:НовоеОписание",
                    new ForceReplyMarkup { Selective = true });
                return true;
            }

            var newDesc = parts[1].Trim();

            var category = await ctx.CategoryService.GetCategoryByIdAsync(categoryId, ctx.CancellationToken);
            if (category == null)
            {
                await SendMessageAsync(ctx, "Категория не найдена.");
                return true;
            }

            category.Description = newDesc;
            var result = await ctx.CategoryService.UpdateAsync(category, ctx.CancellationToken);

            if (result)
            {
                await SendMessageAsync(ctx,
                    "✅ Описание категории обновлено!",
                    new InlineKeyboardMarkup(new[] { new[] { BackToCategoriesButton() } }));
            }
            else
            {
                await SendMessageAsync(ctx, "❌ Не удалось обновить категорию.");
            }

            return true;
        }
    }
}
