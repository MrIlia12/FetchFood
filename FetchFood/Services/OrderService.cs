using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FetchFood.Services
{
    public interface IOrderService
    {
        Task StartOrderAsync(long chatId, ITelegramBotClient bot, CancellationToken ct);
        Task ProcessMessageAsync(long chatId, string message, ITelegramBotClient bot, CancellationToken ct);
        void CancelOrder(long chatId);
    }
    public class OrderService : IOrderService
    {
        private readonly Dictionary<long, UserOrderData> _userOrders = new();

        public async Task StartOrderAsync(long chatId, ITelegramBotClient bot, CancellationToken ct)
        {
            _userOrders[chatId] = new UserOrderData
            {
                ChatId = chatId,
                CurrentState = OrderState.WaitingForAddress
            };

            await bot.SendMessage(
                chatId,
                "📝 Введите адрес доставки в формате:\nул. <улица>, д. <номер дома>, кв. <номер квартиры>\n\nПример: ул. Ленина, д. 15, кв. 42",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct
            );
        }

        public async Task ProcessMessageAsync(long chatId, string message, ITelegramBotClient bot, CancellationToken ct)
        {
            if (!_userOrders.TryGetValue(chatId, out var orderData))
                return;

            switch (orderData.CurrentState)
            {
                case OrderState.WaitingForAddress:
                    await ProcessAddressInputAsync(chatId, message, orderData, bot, ct);
                    break;

                case OrderState.WaitingForFullName:
                    await ProcessFullNameInputAsync(chatId, message, orderData, bot, ct);
                    break;

                case OrderState.Confirmation:
                    await ProcessConfirmationInputAsync(chatId, message, orderData, bot, ct);
                    break;
            }
        }
        private async Task ProcessAddressInputAsync(long chatId, string message, UserOrderData orderData, ITelegramBotClient bot, CancellationToken ct)
        {
            // Проверяем, соответствует ли адрес формату
            var validationResult = ValidateAddress(message);

            if (!validationResult.IsValid)
            {
                var errorMessage = $"❌ {validationResult.ErrorMessage}\n\n" +
                                          $"**Правильный формат:** ул. <улица>, д. <номер дома>, кв. <номер квартиры>\n" +
                                          $"**Пример правильного адреса:** ул. Ленина, д. 15а, кв. 42\n" +
                                          $"**Допустимые форматы дома:** 15, 15а, 15/1, 15/1а\n\n" +
                                          $"Убедитесь, что:\n" +
                                          $"• Указана улица, дом и квартира\n" +
                                          $"• Номер дома содержит цифры\n" +
                                          $"• Номер квартиры содержит цифры";

                await bot.SendMessage(
                    chatId,
                    errorMessage,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct
                );
                return;
            }

            // Форматируем адрес
            orderData.Address = FormatAddress(message);
            orderData.CurrentState = OrderState.WaitingForFullName;

            await bot.SendMessage(
                chatId,
                $"✅ Адрес сохранен!\n📍 {orderData.Address}\n\nНапишите Ваши полные ФИО:",
                cancellationToken: ct
            );
        }

        // Проверка адреса на корретность
        private (bool IsValid, string ErrorMessage) ValidateAddress(string address)
        {
            // Пустой
            if (string.IsNullOrWhiteSpace(address))
                return (false, "Адрес не может быть пустым.");

            // Короткий
            if (address.Length < 5)
                return (false, "Адрес слишком короткий.");

            bool hasApartment = address.Contains("кв.") || address.Contains("квартира") || ContainsApartmentNumber(address);
            // Проверяем структуру адреса
            var parts = address.Split(',');
            if (parts.Length < 3 && !hasApartment)
                return (false, "Адрес должен содержать улицу, номер дома и номер квартиры, разделенные запятой.");

            // Ключевые элементы адреса
            if (!(address.Contains("ул.") || address.Contains("улица")))
                return (false, "Не указана улица (используйте 'ул.' или 'улица').");

            if (!(address.Contains("д.") || address.Contains("дом")))
                return (false, "Не указан номер дома (используйте 'д.' или 'дом').");

            if (!hasApartment)
                return (false, "Не указан номер квартиры (используйте 'кв.', 'квартира' или просто число).");

            // Проверяем часть с улицей
            string streetPart = parts[0].ToLower();
            if (streetPart.Replace("ул.", "").Replace("улица", "").Trim().Length < 3)
                return (false, "Название улицы слишком короткое или отсутствует.");

            // Проверяем часть с домом
            string housePart = parts.Length > 1 ? parts[1].ToLower() : "";
            var houseValidation = ValidateHouseNumber(housePart);
            if (!houseValidation.IsValid)
                return (false, houseValidation.ErrorMessage);

            // Проверяем часть с квартирой
            string apartmentPart = parts.Length > 2 ? parts[2].ToLower() : "";
            var apartmentValidation = ValidateApartmentNumber(apartmentPart);
            if (!apartmentValidation.IsValid)
                return (false, apartmentValidation.ErrorMessage);


            return (true, string.Empty);
        }

        // Проверка номера дома на корретность
        private (bool IsValid, string ErrorMessage) ValidateHouseNumber(string housePart)
        {
            if (string.IsNullOrWhiteSpace(housePart))
                return (false, "Не указан номер дома.");

            // Извлекаем номер дома из строки
            var houseMatch = Regex.Match(housePart, @"д\.?\s*([^,]+)");
            if (!houseMatch.Success)
                return (false, "Не удалось определить номер дома.");

            string houseNumber = houseMatch.Groups[1].Value.Trim();

            // Проверяем формат номера дома
            if (!IsValidHouseNumber(houseNumber))
                return (false, $"Некорректный формат номера дома: '{houseNumber}'. Допустимые форматы: 15, 15а, 15/1, 15/1а");

            return (true, string.Empty);
        }

        // Допустимые значения для поля "дом"
        // 15 - только цифры
        // 15а - цифры + одна буква
        // 15/1 - цифры/цифры
        // 15/1а - цифры/цифры + буква
        private bool IsValidHouseNumber(string houseNumber)
        {
            var pattern = @"^\d+[а-яa-z]?$|^\d+/\d+[а-яa-z]?$";
            return Regex.IsMatch(houseNumber, pattern, RegexOptions.IgnoreCase);
        }

        // Проверка номера квартиры на корретность
        private (bool IsValid, string ErrorMessage) ValidateApartmentNumber(string apartmentPart)
        {
            if (string.IsNullOrWhiteSpace(apartmentPart))
                return (false, "Не указан номер квартиры.");

            // Извлекаем номер квартиры из строки
            var apartmentMatch = Regex.Match(apartmentPart, @"(кв\.?\s*|квартира\s*)?([^,]+)");
            if (!apartmentMatch.Success)
                return (false, "Не удалось определить номер квартиры.");

            var apartmentNumber = apartmentMatch.Groups[2].Value.Trim();

            // Номер квартиры должен содержать только цифры
            if (!Regex.IsMatch(apartmentNumber, @"^\d+$"))
                return (false, $"Некорректный номер квартиры: '{apartmentNumber}'. Должен содержать только цифры.");

            return (true, string.Empty);
        }

        private bool ContainsApartmentNumber(string address)
        {
            // Есть ли номер квартиры в конце адреса (без префикса "кв.")
            var parts = address.Split(',');
            if (parts.Length >= 3)
                return true;

            // Проверка последней части на наличие цифр (квартира без префикса)
            var lastPart = parts[parts.Length - 1].Trim();
            return Regex.IsMatch(lastPart, @"^\d+$");
        }

        private string FormatAddress(string rawAddress)
        {
            // Приводим к стандартному формату
            string formatted = rawAddress.Replace("улица", "ул.").Replace("дом", "д.").Replace("квартира", "кв.");

            // Убираем лишние пробелы
            formatted = Regex.Replace(formatted, @"\s+", " ").Trim();

            // Разбираем на части адреса
            string[] parts = formatted.Split(',');
            List<string> resultParts = new List<string>();

            // Обрабатываем улицу
            if (parts.Length > 0)
            {
                string streetPart = parts[0].Trim();
                if (!streetPart.Contains("ул."))
                {
                    streetPart = streetPart.Replace("ул", "ул.");
                }
                resultParts.Add(streetPart);
            }

            // Обрабатываем дом
            if (parts.Length > 1)
            {
                string housePart = parts[1].Trim();
                Match houseMatch = Regex.Match(housePart, @"д\.?\s*([^,]+)");
                if (houseMatch.Success)
                {
                    var houseNumber = houseMatch.Groups[1].Value.Trim();
                    resultParts.Add($"д. {houseNumber}");
                }
                else
                {
                    resultParts.Add($"д. {housePart}");
                }
            }

            // Обрабатываем квартиру
            if (parts.Length > 2)
            {
                string apartmentPart = parts[2].Trim();
                Match apartmentMatch = Regex.Match(apartmentPart, @"(кв\.?\s*|квартира\s*)?([^,]+)");
                if (apartmentMatch.Success)
                {
                    var apartmentNumber = apartmentMatch.Groups[2].Value.Trim();
                    resultParts.Add($"кв. {apartmentNumber}");
                }
                else
                {
                    resultParts.Add($"кв. {apartmentPart}");
                }
            }
            else if (ContainsApartmentNumber(rawAddress))
            {
                // Если квартира указана без префикса в последней части
                string lastPart = parts[parts.Length - 1].Trim();
                Match apartmentMatch = Regex.Match(lastPart, @"(\d+)$");
                if (apartmentMatch.Success)
                {
                    resultParts[resultParts.Count - 1] = resultParts[resultParts.Count - 1].Replace(apartmentMatch.Value, "").Trim();
                    resultParts.Add($"кв. {apartmentMatch.Value}");
                }
            }

            return string.Join(", ", resultParts);
        }

        private async Task ProcessFullNameInputAsync(long chatId, string message, UserOrderData orderData, ITelegramBotClient bot, CancellationToken ct)
        {
            // Валидация ФИО
            var validationResult = ValidateFullName(message);

            if (!validationResult.IsValid)
            {
                await bot.SendMessage(
                    chatId,
                    $"❌ {validationResult.ErrorMessage}\n\nПожалуйста, введите полные ФИО",
                    cancellationToken: ct
                );
                return;
            }

            orderData.FullName = FormatFullName(message);
            orderData.CurrentState = OrderState.Confirmation;

            await SendConfirmationMessageAsync(chatId, orderData, bot, ct);
        }

        private (bool IsValid, string ErrorMessage) ValidateFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "ФИО не может быть пустым.");

            if (fullName.Length < 5)
                return (false, "ФИО слишком короткое.");

            // Должно быть как минимум 2 слова (имя и фамилия)
            var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2)
                return (false, "Пожалуйста, укажите как минимум фамилию и имя.");

            // Проверяем, что нет цифр и специальных символов
            if (Regex.IsMatch(fullName, @"[0-9]"))
                return (false, "ФИО не должно содержать цифры.");

            if (Regex.IsMatch(fullName, @"[^\p{L}\s\-]"))
                return (false, "ФИО содержит недопустимые символы.");

            return (true, string.Empty);
        }

        private string FormatFullName(string rawFullName)
        {
            // Приводим к стандартному формату: каждое слово с заглавной буквы
            var words = rawFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }

            return string.Join(" ", words);
        }

        private async Task ProcessConfirmationInputAsync(long chatId, string message, UserOrderData orderData, ITelegramBotClient bot, CancellationToken ct)
        {
            if (message.ToLower().Contains("подтвердить") || message == "✅")
            {
                await CompleteOrderAsync(chatId, orderData, bot, ct);
            }
            else if (message.ToLower().Contains("отменить") || message == "❌")
            {
                await CancelOrderAsync(chatId, bot, ct);
            }
            else
            {
                await bot.SendMessage(
                    chatId,
                    "❌ Непонятная команда. Пожалуйста, выберите 'Подтвердить' или 'Отменить'",
                    cancellationToken: ct
                );
            }
        }

        private async Task SendConfirmationMessageAsync(long chatId, UserOrderData orderData, ITelegramBotClient bot, CancellationToken ct)
        {
            var confirmationText = $"📋 **Подтвердите заказ:**\n\n" +
                                  $"📍 **Адрес:** {orderData.Address}\n" +
                                  $"👤 **ФИО:** {orderData.FullName}\n\n" +
                                  $"Всё верно?";

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "✅ Подтвердить" },
                new KeyboardButton[] { "❌ Отменить" },
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await bot.SendMessage(
                chatId,
                confirmationText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: ct
            );
        }

        private async Task CompleteOrderAsync(long chatId, UserOrderData orderData, ITelegramBotClient bot, CancellationToken ct)
        {
            // Сохраняем заказ
            Console.WriteLine($"✅ Заказ оформлен: {orderData.FullName}, {orderData.Address}");

            _userOrders.Remove(chatId);

            await bot.SendMessage(
                chatId,
                "🎉 Заказ успешно оформлен! Ожидайте доставку.\n\nДля нового заказа напишите /order",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct
            );
        }

        private async Task CancelOrderAsync(long chatId, ITelegramBotClient bot, CancellationToken ct)
        {
            _userOrders.Remove(chatId);

            await bot.SendMessage(
                chatId,
                "❌ Заказ отменен.\n\nДля нового заказа напишите /order",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct
            );
        }

        public void CancelOrder(long chatId)
        {
            _userOrders.Remove(chatId);
        }
    }
}