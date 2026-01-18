using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Categories
{
    /// <summary>
    /// Команда быстрого редактирования категории (все параметры одной командой).
    /// Формат: menu:qcat:ID:name=Название;desc=Описание
    /// </summary>
    public class QuickEditCategoryCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.QUICK_EDIT_CAT;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            var parts = ctx.Args.Split(':', 2, StringSplitOptions.TrimEntries);

            if (parts.Length < 1 || !int.TryParse(parts[0], out var categoryId))
            {
                await SendHelpAsync(ctx);
                return true;
            }

            var category = await ctx.CategoryService.GetCategoryByIdAsync(categoryId, ctx.CancellationToken);
            if (category == null)
            {
                await SendMessageAsync(ctx, $"Категория с ID {categoryId} не найдена.");
                return true;
            }

            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                await SendCurrentValuesAsync(ctx, category);
                return true;
            }

            // Парсим параметры
            var paramPairs = parts[1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var changes = new List<string>();
            var errors = new List<string>();

            foreach (var pair in paramPairs)
            {
                var kv = pair.Split('=', 2);
                if (kv.Length != 2) continue;

                var key = kv[0].Trim().ToLower();
                var value = kv[1].Trim();

                switch (key)
                {
                    case "name":
                        if (value.Length > 100)
                            errors.Add("Название должно быть <= 100 символов");
                        else if (string.IsNullOrWhiteSpace(value))
                            errors.Add("Название не может быть пустым");
                        else
                        {
                            category.Name = value;
                            changes.Add($"название → «{value}»");
                        }
                        break;

                    case "desc":
                        category.Description = value;
                        changes.Add($"описание → «{value}»");
                        break;

                    default:
                        errors.Add($"Неизвестный параметр: {key}");
                        break;
                }
            }

            if (changes.Count == 0)
            {
                var errorMsg = errors.Count > 0 ? $"\n\nОшибки:\n• {string.Join("\n• ", errors)}" : "";
                await SendMessageAsync(ctx, $"Нет параметров для изменения.{errorMsg}");
                return true;
            }

            var result = await ctx.CategoryService.UpdateAsync(category, ctx.CancellationToken);

            if (result)
            {
                var changeList = string.Join("\n• ", changes);
                var errorList = errors.Count > 0 ? $"\n\nПропущено (ошибки):\n• {string.Join("\n• ", errors)}" : "";

                await SendMessageAsync(ctx,
                    $"✅ Категория #{categoryId} обновлена:\n• {changeList}{errorList}",
                    new InlineKeyboardMarkup(new[] { new[] { BackToCategoriesButton() } }));
            }
            else
            {
                await SendMessageAsync(ctx, "❌ Не удалось обновить категорию.");
            }

            return true;
        }

        private static Task SendHelpAsync(MenuCommandContext ctx)
        {
            return SendMessageAsync(ctx,
                $"Быстрое редактирование категории:\n" +
                $"{BotCommands.MENU}:{BotCommands.QUICK_EDIT_CAT}:ID:параметры\n\n" +
                "Доступные параметры:\n" +
                "• name=Название\n" +
                "• desc=Описание\n\n" +
                "Пример:\n" +
                $"{BotCommands.MENU}:{BotCommands.QUICK_EDIT_CAT}:1:name=Новое название;desc=Новое описание");
        }

        private static Task SendCurrentValuesAsync(MenuCommandContext ctx, DataAccess.Entities.PositionCategory category)
        {
            return SendMessageAsync(ctx,
                $"Текущие значения категории #{category.PositionCategoryId}:\n" +
                $"• name={category.Name}\n" +
                $"• desc={category.Description ?? ""}\n\n" +
                $"Введите параметры для изменения:\n" +
                $"{BotCommands.MENU}:{BotCommands.QUICK_EDIT_CAT}:{category.PositionCategoryId}:name=Новое название",
                new ForceReplyMarkup { Selective = true });
        }
    }
}
