using BusinessLogic.Services.Authorization.Abstractions;
using BusinessLogic.Services.MakingOrders.Abstractions;
using DataAccess.Entities;
using DataAccess.Entities.Models;
using DataAccess.EntityFramework;
using DataAccess.Repositories.Abstractions;
using DataAccess.Repositories.Implementations;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types.ReplyMarkups;

namespace BusinessLogic.Services.MakingOrders.Implemenatation
{
    public class MakingOrdersService : IMakingOrdersService
    {
        private readonly ILogger<MakingOrdersService> _logger;
        private readonly IOrdersRepository _ordersRepository;
        private readonly IOrdersDataRepository _ordersDataRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly IUserRepository _userRepository;

        public MakingOrdersService(
            IOrdersRepository ordersRepository,
            IOrdersDataRepository ordersDataRepository,
            IAuthorizationService authorizationService,
            IUserRepository userRepository,
            ILogger<MakingOrdersService> logger)
        {
            _logger = logger;
            _ordersRepository = ordersRepository;
            _ordersDataRepository = ordersDataRepository;
            _authorizationService = authorizationService;
            _userRepository = userRepository;
        }

        public async Task<bool> StartOrderCreationAsync(long userId)
        {
                // Создаем временные данные для заказа
                UserOrderData orderData = new UserOrderData
                {
                    UserId = userId,
                    CurrentState = OrderStatus.WaitingForAddress,
                    CartItems = new List<CartItem>() // Здесь должна быть логика получения корзины
                };

                // Сохраняем временные данные в репозиторий черновиков
                await _ordersDataRepository.SaveOrderDataAsync(orderData);
                _logger.LogInformation($"Начат процесс оформления заказа для пользователя {userId}");
                return true;
        }

        public async Task<OrderProcessingResult> ProcessUserInputAsync(long userId, string message)
        {
                // Получаем текущие данные заказа из репозитория черновых заказов
                UserOrderData orderData = await _ordersDataRepository.GetOrderDataAsync(userId);
                if (orderData == null)
                {
                    return new OrderProcessingResult
                    {
                        Success = false,
                        Message = "❌ Сессия оформления заказа не найдена. Начните заново."
                    };
                }
                // Обрабатываем ввод в зависимости от текущего состояния
                OrderProcessingResult result = orderData.CurrentState switch
                {
                    OrderStatus.WaitingForAddress => await ProcessAddressInputAsync(userId, message, orderData),
                    OrderStatus.WaitingForComment => await ProcessCommentInputAsync(userId, message, orderData),
                    OrderStatus.WaitingForCommentText => await ProcessCommentInputAsync(userId, message, orderData),
                    OrderStatus.WaitingForConfirmation => await ProcessConfirmationInputAsync(userId, message, orderData),
                    _ => new OrderProcessingResult { Success = false, Message = "❌ Неизвестное состояние заказа" }
                };

                return result;
        }

        public async Task<bool> CancelOrderCreationAsync(long userId)
        {
                // Удаляем временные данные заказа из репозитория черновых заказов
                await _ordersDataRepository.DeleteOrderDataAsync(userId);
                _logger.LogInformation($"Оформление заказа отменено для пользователя {userId}");
                return true;
        }

        public async Task<Orders> GetCurrentOrderAsync(long userId)
        {
                // Получаем последний заказ пользователя из репозитория оформленных заказов
                return await _ordersRepository.GetUserCurrentOrderAsync(userId);
        }

        // Сохраняем оформленный заказ в бд
        public async Task<bool> CompleteOrderAsync(long userId)
        {
                // Получаем временные данные заказа
                UserOrderData orderData = await _ordersDataRepository.GetOrderDataAsync(userId);
                if (orderData == null)
                {
                    _logger.LogWarning($"Не найдены временные данные заказа для пользователя {userId}");
                    return false;
                }

                Orders order = new Orders
                {
                    IdUser = userId,
                    Address = orderData.Address,
                    PhoneNumber = orderData.PhoneNumber,
                    Status = OrderStatus.Created,
                    Price = (int)orderData.Price,
                    DateOrder = DateTime.UtcNow,
                    Comment = orderData.Comment
                };

                // Сохраняем заказ в базу данных
                Orders createdOrder = await _ordersRepository.CreateOrderAsync(order);

                // Очищаем временные данные
                await _ordersDataRepository.DeleteOrderDataAsync(userId);

                _logger.LogInformation($"Заказ №{createdOrder.OrderId} успешно создан для пользователя {userId}");
                return true;
        }

        #region Methods
        private async Task<OrderProcessingResult> ProcessAddressInputAsync(long userId, string message, UserOrderData orderData)
        {
            try
            {
                var validationResult = ValidateAddress(message);
                if (!validationResult.IsValid)
                {
                    string errorMessage = $"❌ {validationResult.ErrorMessage}\n\n" +
                                      $"Правильный формат: ул. <улица>, д. <номер дома>, кв. <номер квартиры>\n" +
                                      $"Пример правильного адреса: ул. Ленина, д. 15а, кв. 42\n" +
                                      $"Допустимые форматы дома: 15, 15а, 15/1, 15/1а\n\n" +
                                      $"Повторите попытку ввода адреса заново";

                    return new OrderProcessingResult
                    {
                        Success = false,
                        Message = errorMessage,
                        NextState = OrderStatus.WaitingForAddress
                    };
                }

                // Сохраняем адрес во временные данные
                orderData.Address = FormatAddress(message);
                // Меняем состояние
                orderData.CurrentState = OrderStatus.WaitingForComment;

                // Обновляем временные данные в репозитории черновиков
                await _ordersDataRepository.SaveOrderDataAsync(orderData);

                return new OrderProcessingResult
                {
                    Success = true,
                    Message = $"✅ Адрес сохранен!\n📍 {orderData.Address}\n\nХотите добавить комментарий к заказу?\n\n",
                    NextState = OrderStatus.WaitingForComment,
                    HasInlineKeyboard = true,
                    InlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✅ Да, добавить", "add_comment"),
                            InlineKeyboardButton.WithCallbackData("❌ Нет, продолжить", "skip_comment")
                        }
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка во время обработки адреса ({userId}): {ex}");
                throw;
            }
        }        

        // Обрабатываем выбранный ответ касательно выбора ввода комментария
        private async Task<OrderProcessingResult> ProcessCommentInputAsync(long userId, string message, UserOrderData orderData)
        {
            // Если пользователь отправил комментарий вместо нажатия кнопки
            if (orderData.CurrentState == OrderStatus.WaitingForComment && message != "skip_comment" && message != "add_comment")
            {                
                return new OrderProcessingResult
                {
                    Success = false,
                    Message = "❌ Пожалуйста, используйте кнопки для выбора варианта.",
                    NextState = OrderStatus.WaitingForComment
                };
            }

            // Обрабатываем команды от кнопок
            if (message == "skip_comment")
            {
                // Пользователь выбрал "нет" - пропускаем комментарий
                orderData.Comment = null;
                return await ProceedToConfirmation(userId, orderData);
            }
            else if (message == "add_comment")
            {
                // Пользователь выбрал "да" - просим ввести комментарий
                orderData.CurrentState = OrderStatus.WaitingForCommentText;
                await _ordersDataRepository.SaveOrderDataAsync(orderData);

                return new OrderProcessingResult
                {
                    Success = true,
                    Message = "💬 Введите ваш комментарий к заказу:\n\n" +
                             "Например: 'Позвонить за час до доставки', 'Оставить у двери', 'Без лука'",
                    NextState = OrderStatus.WaitingForCommentText
                };
            }

            // Если это текст комментария (в состоянии WaitingForCommentText)
            else if (orderData.CurrentState == OrderStatus.WaitingForCommentText)
            {
                // Пользователь ввел текст комментария
                // Если длиннее 1000 символов, то обрезаем строку до 1000, убираем лишние пробелы
                orderData.Comment = message.Trim().Length > 1000
                    ? message.Trim().Substring(0, 1000)
                    : message.Trim();

                return await ProceedToConfirmation(userId, orderData);
            }
            else
            {
                // Неизвестная команда или состояние
                return new OrderProcessingResult
                {
                    Success = false,
                    Message = "❌ Непонятная команда. Пожалуйста, используйте кнопки для выбора.",
                    NextState = orderData.CurrentState
                };
            }
        }

        private async Task<OrderProcessingResult> ProceedToConfirmation(long userId, UserOrderData orderData)
        {
            // Получаем телефон пользователя из базы данных
            User user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new OrderProcessingResult
                {
                    Success = false,
                    Message = "❌ Не удалось получить данные пользователя. Попробуйте позже."
                };
            }

            orderData.PhoneNumber = user.PhoneNumber;
            orderData.Name = user.Name;
            orderData.CurrentState = OrderStatus.WaitingForConfirmation;

            await _ordersDataRepository.SaveOrderDataAsync(orderData);

            string confirmationMessage = GetConfirmationMessage(orderData);

            return new OrderProcessingResult
            {
                Success = true,
                Message = confirmationMessage,
                NextState = OrderStatus.WaitingForConfirmation,
                HasInlineKeyboard = true,
                InlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Подтвердить заказ", "confirm_order"),
                        InlineKeyboardButton.WithCallbackData("❌ Отменить", "cancel_order")
                    }
                })
            };
        }

        // Формируем сообщение с подтверждением заказа
        private string GetConfirmationMessage(UserOrderData orderData)
        {
            StringBuilder confirmationText = new StringBuilder();

            confirmationText.AppendLine("📋 Подтвердите заказ:\n");
            confirmationText.AppendLine($"👤 Имя: {orderData.Name}");
            confirmationText.AppendLine($"📞 Телефон: {orderData.PhoneNumber}");
            confirmationText.AppendLine($"📍 Адрес: {orderData.Address}");

            // Добавляем комментарий, если он есть
            if (!string.IsNullOrEmpty(orderData.Comment))
            {
                confirmationText.AppendLine($"💬 Комментарий: {orderData.Comment}");
            }

            confirmationText.AppendLine($"💰 Сумма: {orderData.Price} руб.\n");
            confirmationText.AppendLine("Всё верно?\n");

            return confirmationText.ToString();
        }

        private async Task<OrderProcessingResult> ProcessConfirmationInputAsync(long userId, string message, UserOrderData orderData)
        {
            if (message == "confirm_order")
            {
                // Завершаем оформление заказа
                bool success = await CompleteOrderAsync(userId);
                if (success)
                {
                    return new OrderProcessingResult
                    {
                        Success = true,
                        Message = "🎉 Заказ успешно оформлен! Ожидайте доставку.\n\nДля нового заказа начните оформление заново.",
                        IsCompleted = true
                    };
                }
                else
                {
                    return new OrderProcessingResult
                    {
                        Success = false,
                        Message = "❌ Произошла ошибка при создании заказа. Попробуйте еще раз.",
                        NextState = OrderStatus.WaitingForConfirmation
                    };
                }
            }
            else if (message == "cancel_order")
            {
                // Отменяем оформление заказа
                await CancelOrderCreationAsync(userId);
                return new OrderProcessingResult
                {
                    Success = true,
                    Message = "❌ Заказ отменен.\n\nДля нового заказа начните оформление заново.",
                    IsCompleted = true
                };
            }
            else
            {
                return new OrderProcessingResult
                {
                    Success = false,
                    Message = "❌ Пожалуйста, используйте кнопки для подтверждения или отмены заказа.",
                    NextState = OrderStatus.WaitingForConfirmation
                };
            }
        }
        #endregion

        #region Validation Adress
        // Проверка адреса на корретность
        private (bool IsValid, string ErrorMessage) ValidateAddress(string address)
        {
            // Пустой
            if (string.IsNullOrWhiteSpace(address))
                return (false, "Адрес не может быть пустым.");

            // Короткий
            if (address.Length < 5)
                return (false, "Адрес слишком короткий.");

            var startsWithValidation = ValidateAddressStartsWithStreet(address);
            if (!startsWithValidation.IsValid)
                return startsWithValidation;

            bool hasApartment = address.Contains("кв.") || address.Contains("квартира") || ContainsApartmentNumber(address);
            // Проверяем структуру адреса
            string[] parts = address.Split(',');
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

        // Проверка на мусор в начале адреса
        private (bool IsValid, string ErrorMessage) ValidateAddressStartsWithStreet(string address)
        {
            // Приводим к нижнему регистру для проверки
            string lowerAddress = address.Trim().ToLower();

            // Паттерны для проверки
            string[] validStartPatterns = {
                @"^\s*ул\.",                    // ул.
                @"^\s*улица",                   // улица
            };

            // Проверяем начинается ли адрес с любого из паттернов
            bool isValidStart = validStartPatterns.Any(pattern => Regex.IsMatch(lowerAddress, pattern));

            if (!isValidStart)
            {
                return (false, "Адрес должен начинаться с указания улицы.");
            }

            string addressWithoutStreet = lowerAddress;

            // Убираем допустимые префиксы улиц для проверки остатка
            foreach (string pattern in validStartPatterns)
            {
                Match match = Regex.Match(lowerAddress, pattern);
                if (match.Success)
                {
                    addressWithoutStreet = lowerAddress.Substring(match.Length).Trim();
                    break;
                }
            }

            // Проверяем есть ли мусор (цифры/непонятные символы) перед названием улицы
            if (addressWithoutStreet.Length > 0)
            {
                // Проверяем, что после префикса улицы идет нормальное название (не цифры и не мусор)
                if (Regex.IsMatch(addressWithoutStreet, @"^\s*\d") ||
                    Regex.IsMatch(addressWithoutStreet, @"^\s*[^а-яa-z\s]"))
                {
                    return (false, "Неверный формат адреса. После указания улицы должно идти её название.");
                }
            }

            return (true, string.Empty);
        }

        // Проверка номера дома на корретность
        private (bool IsValid, string ErrorMessage) ValidateHouseNumber(string housePart)
        {
            if (string.IsNullOrWhiteSpace(housePart))
                return (false, "Не указан номер дома.");

            // Извлекаем номер дома из строки
            Match houseMatch = Regex.Match(housePart, @"д\.?\s*([^,]+)");
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
            string pattern = @"^\d+[а-яa-z]?$|^\d+/\d+[а-яa-z]?$";
            return Regex.IsMatch(houseNumber, pattern, RegexOptions.IgnoreCase);
        }

        // Проверка номера квартиры на корретность
        private (bool IsValid, string ErrorMessage) ValidateApartmentNumber(string apartmentPart)
        {
            if (string.IsNullOrWhiteSpace(apartmentPart))
                return (false, "Не указан номер квартиры.");

            // Извлекаем номер квартиры из строки
            Match apartmentMatch = Regex.Match(apartmentPart, @"(кв\.?\s*|квартира\s*)?([^,]+)");
            if (!apartmentMatch.Success)
                return (false, "Не удалось определить номер квартиры.");

            string apartmentNumber = apartmentMatch.Groups[2].Value.Trim();

            // Номер квартиры должен содержать только цифры или кв.
            if (!Regex.IsMatch(apartmentNumber, @"^(кв\.?\s*)?\d+$"))
                return (false, $"Некорректный номер квартиры: '{apartmentNumber}'. Должен содержать цифры (можно с 'кв.').");

            return (true, string.Empty);
        }

        private bool ContainsApartmentNumber(string address)
        {
            // Есть ли номер квартиры в конце адреса (без префикса "кв.")
            string[] parts = address.Split(',');
            if (parts.Length >= 3)
                return true;

            // Проверка последней части на наличие цифр (квартира без префикса)
            string lastPart = parts[parts.Length - 1].Trim();
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
                    string houseNumber = houseMatch.Groups[1].Value.Trim();
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
                    string apartmentNumber = apartmentMatch.Groups[2].Value.Trim();
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
        #endregion

    }
}
