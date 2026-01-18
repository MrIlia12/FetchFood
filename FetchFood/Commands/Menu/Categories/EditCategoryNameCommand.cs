using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Categories
{
    /// <summary>
    /// Команда изменения названия категории.
    /// </summary>
    public class EditCategoryNameCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.EDIT_CATEGORY_NAME;

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
                    $"Введите новое название категории:\n{BotCommands.MENU}:{BotCommands.EDIT_CATEGORY_NAME}:{categoryId}:НовоеНазвание",
                    new ForceReplyMarkup { Selective = true });
                return true;
            }

            var newName = parts[1].Trim();
            if (newName.Length > 100)
            {
                await SendMessageAsync(ctx, "Название должно быть <= 100 символов.");
                return true;
            }

            var category = await ctx.CategoryService.GetCategoryByIdAsync(categoryId, ctx.CancellationToken);
            if (category == null)
            {
                await SendMessageAsync(ctx, "Категория не найдена.");
                return true;
            }

            category.Name = newName;
            var result = await ctx.CategoryService.UpdateAsync(category, ctx.CancellationToken);

            if (result)
            {
                await SendMessageAsync(ctx,
                    $"✅ Название категории изменено на «{newName}»!",
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
