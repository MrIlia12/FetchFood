using DataAccess.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Categories
{
    /// <summary>
    /// Команда добавления категории.
    /// </summary>
    public class AddCategoryCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.ADD_CATEGORY;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Args))
            {
                await SendMessageAsync(ctx,
                    $"Чтобы добавить категорию:\n{BotCommands.MENU}:{BotCommands.ADD_CATEGORY}:Название;Описание\n\n" +
                    $"Пример:\n{BotCommands.MENU}:{BotCommands.ADD_CATEGORY}:Напитки;Холодные и горячие напитки",
                    new ForceReplyMarkup { Selective = true });
                return true;
            }

            var parts = ctx.Args.Split(';', 2, StringSplitOptions.TrimEntries);
            string name = parts[0];
            string? description = parts.Length >= 2 ? parts[1] : null;

            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            {
                await SendMessageAsync(ctx, "Название категории обязательно и должно быть <= 100 символов.");
                return true;
            }

            var newCategory = new PositionCategory
            {
                Name = name.Trim(),
                Description = description?.Trim() ?? string.Empty
            };

            var result = await ctx.CategoryService.CreateAsync(newCategory, ctx.CancellationToken);

            if (result)
            {
                await SendMessageAsync(ctx,
                    $"✅ Категория «{newCategory.Name}» успешно создана!",
                    new InlineKeyboardMarkup(new[] { new[] { BackToCategoriesButton() } }));
            }
            else
            {
                await SendMessageAsync(ctx, "❌ Не удалось создать категорию. Попробуйте позже.");
            }

            return true;
        }
    }
}
