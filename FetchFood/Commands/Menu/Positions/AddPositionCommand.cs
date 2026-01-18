using DataAccess.Entities;
using DataAccess.Entities.Models;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Команда добавления новой позиции.
    /// </summary>
    public class AddPositionCommand : BaseMenuCommand
    {
        public override string CommandKey => BotCommands.ADD_POSITION;

        public override async Task<bool> ExecuteAsync(MenuCommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Args))
            {
                await SendMessageAsync(ctx,
                    $"Формат: {BotCommands.MENU}:{BotCommands.ADD_POSITION}:Имя;Цена(руб.);Состав;Описание;[ImageUrl];[CategoryId]\n" +
                    $"Например:\n{BotCommands.MENU}:{BotCommands.ADD_POSITION}:Бургер;199.9;ингредиент1,ингредиент2;Пара слов о блюде.;https://img;1",
                    new ForceReplyMarkup { Selective = true });
                return true;
            }

            var parts = ctx.Args.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length < 4)
            {
                await SendMessageAsync(ctx,
                    "Нужно указать корректные значения полей Имя и Цена. Состав и описание являются опциональными параметрами.");
                return true;
            }

            string name = parts[0];
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            {
                await SendMessageAsync(ctx, "Имя обязательно и <= 100 символов.");
                return true;
            }

            // Проверка дубликата
            var existing = await ctx.MenuService.SearchPositionsAsync(name, false, ctx.CancellationToken);
            if (existing.Any(p => p.Status == PositionStatus.Active &&
                string.Equals(p.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                await SendMessageAsync(ctx, $"❌ Позиция «{name}» уже существует. Дубликаты не добавляю.");
                return true;
            }

            if (!decimal.TryParse(parts[1], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                await SendMessageAsync(ctx, "Цена некорректна. Используйте десятичную точку: 199.9");
                return true;
            }

            string? ingredients = parts.Length >= 3 ? parts[2] : null;
            string? description = parts.Length >= 4 ? parts[3] : null;
            string? image = parts.Length >= 5 ? parts[4] : null;

            int? categoryId = null;
            if (parts.Length >= 6 && !string.IsNullOrWhiteSpace(parts[5]) && int.TryParse(parts[5], out var parsedCatId))
            {
                var category = await ctx.CategoryService.GetCategoryByIdAsync(parsedCatId, ctx.CancellationToken);
                if (category != null)
                {
                    categoryId = parsedCatId;
                }
                else
                {
                    await SendMessageAsync(ctx, $"⚠️ Категория с ID {parsedCatId} не найдена. Позиция будет добавлена без категории.");
                }
            }

            var pos = new Position
            {
                Name = name.Trim(),
                Price = price,
                Status = PositionStatus.Active,
                Ingredients = string.IsNullOrEmpty(ingredients) ? null : ingredients.Trim(),
                Description = string.IsNullOrEmpty(description) ? null : description.Trim(),
                Image = string.IsNullOrWhiteSpace(image) ? null : image.Trim(),
                PositionCategoryId = categoryId
            };

            try
            {
                if (!await ctx.MenuService.CreateAsync(pos, ctx.CancellationToken))
                {
                    await SendMessageAsync(ctx, "❌ Не удалось добавить позицию. Попробуйте позже.",
                        new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                    return true;
                }

                string categoryInfo = categoryId.HasValue
                    ? $" (категория: {(await ctx.CategoryService.GetCategoryByIdAsync(categoryId.Value, ctx.CancellationToken))?.Name})"
                    : " (без категории)";

                await SendMessageAsync(ctx,
                    $"✅ Добавлено: #{pos.PositionId} • {pos.Name} — {pos.Price:0.##}{categoryInfo}",
                    new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
            }
            catch (Exception ex)
            {
                await SendMessageAsync(ctx, "❌ Не удалось добавить позицию. Попробуйте позже.",
                    new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                Console.WriteLine($"[AddPos ERROR]: {ex}");
            }

            return true;
        }
    }
}
