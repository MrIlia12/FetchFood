using System.Collections.Concurrent;
using System.Text;
using DataAccess.Entities;
using DataAccess.Entities.Models;
using FetchFood.Services;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Commands.Menu.Positions
{
    /// <summary>
    /// Пошаговый обработчик добавления новой позиции.
    /// </summary>
    public class AddPositionHandler : AdminMenuCommand
    {
        public override string CommandKey => BotCommands.ADD_POSITION;

        /// <summary>
        /// Данные создаваемой позиции.
        /// </summary>
        private class CreationData
        {
            public string CurrentStep { get; set; } = "name";
            public string? Name { get; set; }
            public decimal? Price { get; set; }
            public int? CategoryId { get; set; }
            public string? CategoryName { get; set; }
            public string? Ingredients { get; set; }
            public string? Description { get; set; }
            public string? ImageFileId { get; set; }
        }

        /// <summary>
        /// In-memory хранилище сессий создания позиций.
        /// </summary>
        private static readonly ConcurrentDictionary<long, CreationData> _sessions = new();

        protected override async Task<bool> ExecuteAdminAsync(MenuCommandContext ctx)
        {
            _sessions.TryGetValue(ctx.ChatId, out var data);
            var action = ctx.Args?.Split(':')[0] ?? "start";

            return action switch
            {
                "start" => await StartAsync(ctx),
                "skip" => await SkipCurrentStepAsync(ctx, data),
                "cancel" => await CancelAsync(ctx),
                "confirm" => await CreatePositionAsync(ctx, data),
                "cat" => await HandleCategorySelectionAsync(ctx, data),
                "back" => await GoBackAsync(ctx, data),
                "input" => await HandleTextInputAsync(ctx, data),
                _ => await HandleTextInputAsync(ctx, data)
            };
        }

        /// <summary>
        /// Начать процесс создания позиции.
        /// </summary>
        private async Task<bool> StartAsync(MenuCommandContext ctx)
        {
            var data = new CreationData { CurrentStep = "name" };
            _sessions[ctx.ChatId] = data;

            await SendStepMessageAsync(ctx, data);
            return true;
        }

        /// <summary>
        /// Пропустить текущий шаг (для опциональных полей).
        /// </summary>
        private async Task<bool> SkipCurrentStepAsync(MenuCommandContext ctx, CreationData? data)
        {
            if (data == null)
            {
                return await StartAsync(ctx);
            }

            // Переход к следующему шагу без сохранения значения
            MoveToNextStep(data);
            await SendStepMessageAsync(ctx, data);
            return true;
        }

        /// <summary>
        /// Отмена создания позиции.
        /// </summary>
        private async Task<bool> CancelAsync(MenuCommandContext ctx)
        {
            _sessions.TryRemove(ctx.ChatId, out _);
            BotMenuHandler.RemovePendingCommand(ctx.ChatId);

            await SendMessageAsync(ctx,
                "❌ Создание позиции отменено.",
                new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
            return true;
        }

        /// <summary>
        /// Создать позицию.
        /// </summary>
        private async Task<bool> CreatePositionAsync(MenuCommandContext ctx, CreationData? data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Name) || !data.Price.HasValue)
            {
                await SendMessageAsync(ctx, "❌ Недостаточно данных для создания позиции.");
                return await StartAsync(ctx);
            }

            // Проверка дубликата
            var existing = await ctx.MenuService.SearchPositionsAsync(data.Name, false, ctx.CancellationToken);
            if (existing.Any(p => p.Status == PositionStatus.Active &&
                string.Equals(p.Name?.Trim(), data.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                await SendMessageAsync(ctx,
                    $"❌ Позиция «{data.Name}» уже существует.",
                    new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("🔄 Изменить название", $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:start") },
                        new[] { BackToMenuButton() }
                    }));
                _sessions.TryRemove(ctx.ChatId, out _);
                return true;
            }

            var pos = new Position
            {
                Name = data.Name.Trim(),
                Price = data.Price.Value,
                Status = PositionStatus.Active,
                Ingredients = string.IsNullOrEmpty(data.Ingredients) ? null : data.Ingredients.Trim(),
                Description = string.IsNullOrEmpty(data.Description) ? null : data.Description.Trim(),
                Image = data.ImageFileId,
                PositionCategoryId = data.CategoryId
            };

            try
            {
                if (!await ctx.MenuService.CreateAsync(pos, ctx.CancellationToken))
                {
                    await SendMessageAsync(ctx,
                        "❌ Не удалось добавить позицию. Попробуйте позже.",
                        new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                    return true;
                }

                _sessions.TryRemove(ctx.ChatId, out _);

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("📄 К позиции", $"{BotCommands.MENU}:{BotCommands.POSITION}:{pos.PositionId}") },
                    new[] { BackToMenuButton() }
                });

                await SendMessageAsync(ctx,
                    $"🎉 Позиция создана!\n\n#{pos.PositionId} • {pos.Name} — {pos.Price:0.##} ₽",
                    keyboard);
            }
            catch (Exception ex)
            {
                await SendMessageAsync(ctx,
                    "❌ Не удалось добавить позицию. Попробуйте позже.",
                    new InlineKeyboardMarkup(new[] { new[] { BackToMenuButton() } }));
                Console.WriteLine($"[AddPos ERROR]: {ex}");
            }

            return true;
        }

        /// <summary>
        /// Обработка выбора категории.
        /// </summary>
        private async Task<bool> HandleCategorySelectionAsync(MenuCommandContext ctx, CreationData? data)
        {
            if (data == null)
            {
                return await StartAsync(ctx);
            }

            var parts = ctx.Args?.Split(':');
            if (parts?.Length >= 2 && int.TryParse(parts[1], out var categoryId))
            {
                if (categoryId == 0)
                {
                    // Без категории
                    data.CategoryId = null;
                    data.CategoryName = null;
                }
                else
                {
                    var category = await ctx.CategoryService.GetCategoryByIdAsync(categoryId, ctx.CancellationToken);
                    if (category != null)
                    {
                        data.CategoryId = categoryId;
                        data.CategoryName = category.Name;
                    }
                }
            }

            MoveToNextStep(data);
            await SendStepMessageAsync(ctx, data);
            return true;
        }

        /// <summary>
        /// Вернуться на предыдущий шаг.
        /// </summary>
        private async Task<bool> GoBackAsync(MenuCommandContext ctx, CreationData? data)
        {
            if (data == null)
            {
                return await StartAsync(ctx);
            }

            MoveToPreviousStep(data);
            await SendStepMessageAsync(ctx, data);
            return true;
        }

        /// <summary>
        /// Обработка текстового ввода.
        /// </summary>
        private async Task<bool> HandleTextInputAsync(MenuCommandContext ctx, CreationData? data)
        {
            if (data == null)
            {
                return await StartAsync(ctx);
            }

            // Извлекаем значение из формата "{step}:{value}"
            var input = ctx.Args;
            var colonIndex = input?.IndexOf(':') ?? -1;
            if (colonIndex > 0 && input != null)
            {
                input = input.Substring(colonIndex + 1);
            }

            // Проверяем, не является ли это фото (file_id)
            if (data.CurrentStep == "image" && !string.IsNullOrWhiteSpace(input))
            {
                data.ImageFileId = input.Trim();
                MoveToNextStep(data);
                await SendStepMessageAsync(ctx, data);
                return true;
            }

            switch (data.CurrentStep)
            {
                case "name":
                    if (string.IsNullOrWhiteSpace(input) || input.Length > 100)
                    {
                        await SendMessageAsync(ctx,
                            "❌ Название обязательно и должно быть не длиннее 100 символов.\n\nВведите название:");
                        return true;
                    }
                    data.Name = input.Trim();
                    break;

                case "price":
                    // Пробуем парсить цену (поддержка запятой и точки)
                    var priceStr = input?.Replace(',', '.') ?? "";
                    if (!decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var price) || price <= 0)
                    {
                        await SendMessageAsync(ctx,
                            "❌ Некорректная цена. Введите положительное число.\n\nВведите цену (в рублях):");
                        return true;
                    }
                    data.Price = price;
                    break;

                case "ingredients":
                    data.Ingredients = input?.Trim();
                    break;

                case "description":
                    data.Description = input?.Trim();
                    break;

                case "image":
                    // Если это просто текст - считаем как file_id
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        data.ImageFileId = input.Trim();
                    }
                    break;
            }

            MoveToNextStep(data);
            await SendStepMessageAsync(ctx, data);
            return true;
        }

        /// <summary>
        /// Отправить сообщение текущего шага.
        /// </summary>
        private async Task SendStepMessageAsync(MenuCommandContext ctx, CreationData data)
        {
            // Шаги с текстовым вводом используют ForceReply
            var textInputSteps = new[] { "name", "price", "ingredients", "description", "image" };

            if (textInputSteps.Contains(data.CurrentStep))
            {
                // Сначала отправляем статус с кнопками
                var (statusText, buttons) = await BuildStatusWithButtonsAsync(ctx, data);
                if (!string.IsNullOrEmpty(statusText))
                {
                    await SendMessageAsync(ctx, statusText, buttons);
                }

                // Затем отправляем запрос с ForceReply
                var promptText = BuildForceReplyPrompt(ctx.ChatId, data);
                await SendMessageAsync(ctx, promptText);
            }
            else
            {
                // Шаги с кнопками (category, confirm)
                var (text, keyboard) = await BuildButtonStepContentAsync(ctx, data);
                await SendMessageAsync(ctx, text, keyboard);
            }
        }

        // Построить текст запроса для ForceReply
        private static string BuildForceReplyPrompt(long chatId, CreationData data)
        {
            var step = data.CurrentStep;
            var prompt = step switch
            {
                "name" => "Введите название:",
                "price" => "Введите цену (в рублях):",
                "ingredients" => "Введите состав:",
                "description" => "Введите описание:",
                "image" => "Отправьте фото:",
                _ => "Введите значение:"
            };

            // Сохраняем команду которую ждём
            BotMenuHandler.SetPendingCommand(chatId, $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:{step}:");

            return prompt;
        }

        /// <summary>
        /// Построить статус и кнопки для шагов с текстовым вводом.
        /// </summary>
        private async Task<(string text, InlineKeyboardMarkup keyboard)> BuildStatusWithButtonsAsync(MenuCommandContext ctx, CreationData data)
        {
            var sb = new StringBuilder();
            var rows = new List<InlineKeyboardButton[]>();

            switch (data.CurrentStep)
            {
                case "name":
                    sb.AppendLine("📝 Добавление новой позиции");
                    sb.AppendLine();
                    sb.AppendLine("Шаг 1/6: Введите название");
                    rows.Add(new[] { CancelButton() });
                    break;

                case "price":
                    sb.AppendLine($"✅ Название: {data.Name}");
                    sb.AppendLine();
                    sb.AppendLine("Шаг 2/6: Введите цену (в рублях)");
                    rows.Add(new[] { BackButton(), CancelButton() });
                    break;

                case "ingredients":
                    sb.AppendLine($"✅ Название: {data.Name}");
                    sb.AppendLine($"✅ Цена: {data.Price:0.##} ₽");
                    sb.AppendLine($"✅ Категория: {data.CategoryName ?? "без категории"}");
                    sb.AppendLine();
                    sb.AppendLine("Шаг 4/6: Введите состав (или пропустите)");
                    rows.Add(new[] { SkipButton(), BackButton(), CancelButton() });
                    break;

                case "description":
                    sb.AppendLine($"✅ Название: {data.Name}");
                    sb.AppendLine($"✅ Цена: {data.Price:0.##} ₽");
                    sb.AppendLine($"✅ Категория: {data.CategoryName ?? "без категории"}");
                    if (!string.IsNullOrWhiteSpace(data.Ingredients))
                        sb.AppendLine($"✅ Состав: {data.Ingredients}");
                    sb.AppendLine();
                    sb.AppendLine("Шаг 5/6: Введите описание (или пропустите)");
                    rows.Add(new[] { SkipButton(), BackButton(), CancelButton() });
                    break;

                case "image":
                    sb.AppendLine($"✅ Название: {data.Name}");
                    sb.AppendLine($"✅ Цена: {data.Price:0.##} ₽");
                    sb.AppendLine($"✅ Категория: {data.CategoryName ?? "без категории"}");
                    if (!string.IsNullOrWhiteSpace(data.Ingredients))
                        sb.AppendLine($"✅ Состав: {data.Ingredients}");
                    if (!string.IsNullOrWhiteSpace(data.Description))
                        sb.AppendLine($"✅ Описание: {data.Description}");
                    sb.AppendLine();
                    sb.AppendLine("Шаг 6/6: Отправьте фото (или пропустите)");
                    rows.Add(new[] { SkipButton(), BackButton(), CancelButton() });
                    break;
            }

            return (sb.ToString(), new InlineKeyboardMarkup(rows));
        }

        /// <summary>
        /// Построить содержимое для шагов с кнопками.
        /// </summary>
        private async Task<(string text, InlineKeyboardMarkup keyboard)> BuildButtonStepContentAsync(MenuCommandContext ctx, CreationData data)
        {
            var sb = new StringBuilder();
            var rows = new List<InlineKeyboardButton[]>();

            switch (data.CurrentStep)
            {
                case "category":
                    sb.AppendLine($"✅ Название: {data.Name}");
                    sb.AppendLine($"✅ Цена: {data.Price:0.##} ₽");
                    sb.AppendLine();
                    sb.AppendLine("Шаг 3/6: Выберите категорию:");

                    rows.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ Без категории", $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:cat:0") });

                    var categories = await ctx.CategoryService.GetAllCategoriesAsync(ctx.CancellationToken);
                    var catButtons = categories
                        .Select(c => InlineKeyboardButton.WithCallbackData(
                            $"📂 {c.Name}",
                            $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:cat:{c.PositionCategoryId}"))
                        .Chunk(2)
                        .Select(chunk => chunk.ToArray());
                    rows.AddRange(catButtons);

                    rows.Add(new[] { BackButton(), CancelButton() });
                    break;

                case "confirm":
                    sb.AppendLine("📋 Проверьте данные:");
                    sb.AppendLine();
                    sb.AppendLine($"📝 {data.Name}");
                    sb.AppendLine($"💰 {data.Price:0.##} ₽");
                    sb.AppendLine($"📂 {data.CategoryName ?? "без категории"}");
                    if (!string.IsNullOrWhiteSpace(data.Ingredients))
                        sb.AppendLine($"🥗 {data.Ingredients}");
                    if (!string.IsNullOrWhiteSpace(data.Description))
                        sb.AppendLine($"📄 {data.Description}");
                    sb.AppendLine(!string.IsNullOrWhiteSpace(data.ImageFileId) ? "🖼 Фото добавлено" : "🖼 Без фото");
                    rows.Add(new[] { ConfirmButton(), CancelButton() });
                    rows.Add(new[] { BackButton() });
                    break;
            }

            return (sb.ToString(), new InlineKeyboardMarkup(rows));
        }

        /// <summary>
        /// Перейти к следующему шагу.
        /// </summary>
        private static void MoveToNextStep(CreationData data)
        {
            data.CurrentStep = data.CurrentStep switch
            {
                "name" => "price",
                "price" => "category",
                "category" => "ingredients",
                "ingredients" => "description",
                "description" => "image",
                "image" => "confirm",
                _ => data.CurrentStep
            };
        }

        /// <summary>
        /// Вернуться к предыдущему шагу.
        /// </summary>
        private static void MoveToPreviousStep(CreationData data)
        {
            data.CurrentStep = data.CurrentStep switch
            {
                "price" => "name",
                "category" => "price",
                "ingredients" => "category",
                "description" => "ingredients",
                "image" => "description",
                "confirm" => "image",
                _ => data.CurrentStep
            };
        }

        // Вспомогательные кнопки
        private static InlineKeyboardButton CancelButton() =>
            InlineKeyboardButton.WithCallbackData("❌ Отмена", $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:cancel");

        private static InlineKeyboardButton BackButton() =>
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:back");

        private static InlineKeyboardButton SkipButton() =>
            InlineKeyboardButton.WithCallbackData("⏭ Пропустить", $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:skip");

        private static InlineKeyboardButton ConfirmButton() =>
            InlineKeyboardButton.WithCallbackData("✅ Создать", $"{BotCommands.MENU}:{BotCommands.ADD_POSITION}:confirm");

        /// <summary>
        /// Проверяет, есть ли активная сессия создания у пользователя.
        /// </summary>
        public static bool HasActiveSession(long chatId) => _sessions.ContainsKey(chatId);

        /// <summary>
        /// Получить текущий шаг сессии.
        /// </summary>
        public static string? GetCurrentStep(long chatId) =>
            _sessions.TryGetValue(chatId, out var data) ? data.CurrentStep : null;

        /// <summary>
        /// Обработка фото для сессии создания.
        /// </summary>
        public static void SetPhotoFileId(long chatId, string fileId)
        {
            if (_sessions.TryGetValue(chatId, out var data) && data.CurrentStep == "image")
            {
                data.ImageFileId = fileId;
            }
        }
    }
}
