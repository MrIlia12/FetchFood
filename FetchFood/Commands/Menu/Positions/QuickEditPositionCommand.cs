using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Команда быстрого редактирования позиции (все параметры одной командой).
    /// Формат: menu:qpos:ID:name=Название;price=199.9;ing=Состав;desc=Описание;img=URL;cat=CategoryId
    /// </summary>
    public class QuickEditPositionCommand : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.QUICK_EDIT_POS;

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            var parts = ctx.Args.Split(':', 2, StringSplitOptions.TrimEntries);

            if (parts.Length < 1 || !int.TryParse(parts[0], out var positionId))
            {
                await SendHelpAsync(ctx);
                return true;
            }

            var position = await ctx.MenuService.GetPositionAsync(positionId, ctx.CancellationToken);
            if (position == null)
            {
                await SendMessageAsync(ctx, $"Позиция с ID {positionId} не найдена.");
                return true;
            }

            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                await SendCurrentValuesAsync(ctx, position);
                return true;
            }

            // Парсим и применяем параметры
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
                        else
                        {
                            position.Name = value;
                            changes.Add($"название → «{value}»");
                        }
                        break;

                    case "price":
                        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var price) && price > 0)
                        {
                            position.Price = price;
                            changes.Add($"цена → {price:0.##}");
                        }
                        else
                            errors.Add("Некорректная цена (используйте точку: 199.9)");
                        break;

                    case "ing":
                        position.Ingredients = string.IsNullOrEmpty(value) ? null : value;
                        changes.Add($"состав → «{value}»");
                        break;

                    case "desc":
                        position.Description = string.IsNullOrEmpty(value) ? null : value;
                        changes.Add($"описание → «{value}»");
                        break;

                    case "img":
                        position.Image = string.IsNullOrEmpty(value) ? null : value;
                        changes.Add("изображение обновлено");
                        break;

                    case "cat":
                        if (int.TryParse(value, out var catId))
                        {
                            if (catId == 0)
                            {
                                position.PositionCategoryId = null;
                                changes.Add("категория → без категории");
                            }
                            else
                            {
                                var cat = await ctx.CategoryService.GetCategoryByIdAsync(catId, ctx.CancellationToken);
                                if (cat != null)
                                {
                                    position.PositionCategoryId = catId;
                                    changes.Add($"категория → «{cat.Name}»");
                                }
                                else
                                    errors.Add($"Категория с ID {catId} не найдена");
                            }
                        }
                        else
                            errors.Add("Некорректный ID категории");
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

            var result = await ctx.MenuService.UpdateAsync(position, ctx.CancellationToken);

            if (result)
            {
                var changeList = string.Join("\n• ", changes);
                var errorList = errors.Count > 0 ? $"\n\nПропущено (ошибки):\n• {string.Join("\n• ", errors)}" : "";

                await SendMessageAsync(ctx,
                    $"✅ Позиция #{positionId} обновлена:\n• {changeList}{errorList}",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("📋 К позиции", $"{BotCommands.MENU}:{BotCommands.POSITION}:{positionId}") },
                        new[] { BackToMenuButton() }
                    }));
            }
            else
            {
                await SendMessageAsync(ctx, "❌ Не удалось обновить позицию.");
            }

            return true;
        }

        private static Task SendHelpAsync(MenuCommandContext ctx)
        {
            return SendMessageAsync(ctx,
                $"Быстрое редактирование позиции:\n" +
                $"{BotCommands.MENU}:{BotCommands.QUICK_EDIT_POS}:ID:параметры\n\n" +
                "Доступные параметры:\n" +
                "• name=Название\n" +
                "• price=199.9\n" +
                "• ing=Состав\n" +
                "• desc=Описание\n" +
                "• img=URL картинки\n" +
                "• cat=ID категории (0 = без категории)\n\n" +
                "Пример:\n" +
                $"{BotCommands.MENU}:{BotCommands.QUICK_EDIT_POS}:1:name=Новый бургер;price=299.9;cat=2");
        }

        private static Task SendCurrentValuesAsync(MenuCommandContext ctx, DataAccess.Entities.Position position)
        {
            return SendMessageAsync(ctx,
                $"Текущие значения позиции #{position.PositionId}:\n" +
                $"• name={position.Name}\n" +
                $"• price={position.Price}\n" +
                $"• ing={position.Ingredients ?? ""}\n" +
                $"• desc={position.Description ?? ""}\n" +
                $"• img={position.Image ?? ""}\n" +
                $"• cat={position.PositionCategoryId?.ToString() ?? "0"}\n\n" +
                $"Введите параметры для изменения:\n" +
                $"{BotCommands.MENU}:{BotCommands.QUICK_EDIT_POS}:{position.PositionId}:name=Новое название;price=299",
                new ForceReplyMarkup { Selective = true });
        }
    }
}
