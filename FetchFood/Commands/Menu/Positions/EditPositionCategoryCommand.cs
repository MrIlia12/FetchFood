using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Команда изменения категории позиции.
    /// </summary>
    public class EditPositionCategoryCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.EDIT_POS_CATEGORY;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            var parts = ctx.Args.Split(':', 2, StringSplitOptions.TrimEntries);

            if (parts.Length < 1 || !int.TryParse(parts[0], out var positionId))
            {
                await SendMessageAsync(ctx, "Некорректный формат команды.");
                return true;
            }

            var position = await ctx.MenuService.GetPositionAsync(positionId, ctx.CancellationToken);
            if (position == null)
            {
                await SendMessageAsync(ctx, "Позиция не найдена.");
                return true;
            }

            // Если категория не указана - показать список для выбора
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                return await ShowCategorySelectionAsync(ctx, position);
            }

            // Установка категории
            if (!int.TryParse(parts[1], out var categoryId))
            {
                await SendMessageAsync(ctx, "Некорректный ID категории.");
                return true;
            }

            if (categoryId == 0)
            {
                position.PositionCategoryId = null;
            }
            else
            {
                var category = await ctx.CategoryService.GetCategoryByIdAsync(categoryId, ctx.CancellationToken);
                if (category == null)
                {
                    await SendMessageAsync(ctx, "Категория не найдена.");
                    return true;
                }
                position.PositionCategoryId = categoryId;
            }

            var result = await ctx.MenuService.UpdateAsync(position, ctx.CancellationToken);

            if (result)
            {
                var categoryName = categoryId == 0
                    ? "Без категории"
                    : (await ctx.CategoryService.GetCategoryByIdAsync(categoryId, ctx.CancellationToken))?.Name ?? "Неизвестная";

                await SendMessageAsync(ctx,
                    $"✅ Категория позиции изменена на «{categoryName}»!",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("✏️ Продолжить редактирование", $"{BotCommands.MENU}:{BotCommands.EDIT}:{positionId}") },
                        new[] { InlineKeyboardButton.WithCallbackData("⬅️ К позиции", $"{BotCommands.MENU}:{BotCommands.POSITION}:{positionId}") }
                    }));
            }
            else
            {
                await SendMessageAsync(ctx, "❌ Не удалось обновить позицию.");
            }

            return true;
        }

        private async Task<bool> ShowCategorySelectionAsync(MenuCommandContext ctx, DataAccess.Entities.Position position)
        {
            var categories = await ctx.CategoryService.GetAllCategoriesAsync(ctx.CancellationToken);
            var rows = new List<InlineKeyboardButton[]>();

            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("❌ Без категории", $"{BotCommands.MENU}:{BotCommands.EDIT_POS_CATEGORY}:{position.PositionId}:0")
            });

            var categoryButtons = categories
                .Select(c => InlineKeyboardButton.WithCallbackData(
                    c.Name ?? $"#{c.PositionCategoryId}",
                    $"{BotCommands.MENU}:{BotCommands.EDIT_POS_CATEGORY}:{position.PositionId}:{c.PositionCategoryId}"))
                .Chunk(2)
                .Select(r => r.ToArray())
                .ToList();

            rows.AddRange(categoryButtons);
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"{BotCommands.MENU}:{BotCommands.EDIT}:{position.PositionId}")
            });

            await SendMessageAsync(ctx,
                $"Выберите категорию для позиции «{position.Name}»:",
                new InlineKeyboardMarkup(rows));

            return true;
        }
    }
}
