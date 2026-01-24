using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Categories
{
    /// <summary>
    /// Базовый класс для команд редактирования отдельных полей категории.
    /// </summary>
    public abstract class EditCategoryFieldCommand : AdminMenuCommand
    {
        /// <summary>
        /// Название поля на русском (для подсказки пользователю)
        /// </summary>
        protected abstract string FieldNameRu { get; }

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            var (categoryId, newValue) = ParseIdAndValue(ctx.Args);

            if (categoryId == null)
            {
                await SendMessageAsync(ctx, "Некорректный формат команды.");
                return true;
            }

            if (string.IsNullOrWhiteSpace(newValue))
            {
                await SendMessageAsync(ctx,
                    $"Введите новое {FieldNameRu}:\n{BotCommands.MENU}:{CommandKey}:{categoryId}:НовоеЗначение",
                    new ForceReplyMarkup { Selective = true });
                return true;
            }

            var category = await ctx.CategoryService.GetCategoryByIdAsync(categoryId.Value, ctx.CancellationToken);
            if (category == null)
            {
                await SendMessageAsync(ctx, "Категория не найдена.");
                return true;
            }

            if (!await ApplyValueAsync(ctx, category, newValue.Trim()))
            {
                return true;
            }

            var result = await ctx.CategoryService.UpdateAsync(category, ctx.CancellationToken);

            if (result)
            {
                await SendMessageAsync(ctx,
                    $"✅ {char.ToUpper(FieldNameRu[0])}{FieldNameRu[1..]} категории обновлено!",
                    new InlineKeyboardMarkup(new[] { new[] { BackToCategoriesButton() } }));
            }
            else
            {
                await SendMessageAsync(ctx, "❌ Не удалось обновить категорию.");
            }

            return true;
        }

        /// <summary>
        /// Применить новое значение к категории. Возвращает false если значение некорректно.
        /// </summary>
        protected abstract Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.PositionCategory category, string value);
    }

    /// <summary>
    /// Команда изменения названия категории.
    /// </summary>
    public class EditCategoryNameCommand : EditCategoryFieldCommand
    {
        public override string CommandKey => BotCommands.EDIT_CATEGORY_NAME;
        protected override string FieldNameRu => "название";

        protected override Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.PositionCategory category, string value)
        {
            if (value.Length > 100)
            {
                SendMessageAsync(ctx, "Название должно быть <= 100 символов.");
                return Task.FromResult(false);
            }
            category.Name = value;
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Команда изменения описания категории.
    /// </summary>
    public class EditCategoryDescCommand : EditCategoryFieldCommand
    {
        public override string CommandKey => BotCommands.EDIT_CATEGORY_DESC;
        protected override string FieldNameRu => "описание";

        protected override Task<bool> ApplyValueAsync(MenuCommandContext ctx, DataAccess.Entities.PositionCategory category, string value)
        {
            category.Description = value;
            return Task.FromResult(true);
        }
    }
}
