using BusinessLogic.Services.Authorization.Abstractions;
using BusinessLogic.Services.Cart.Abstractions;
using BusinessLogic.Services.MakingOrders.Abstractions;
using BusinessLogic.Services.MakingOrders.States;
using DataAccess.Entities;
using DataAccess.Entities.Models;
using DataAccess.Repositories.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types.ReplyMarkups;

namespace BusinessLogic.Services.MakingOrders.Implemenatation
{
    // Команды сервиса оформления заказа
    public static class MakingOrdersCommands
    {
        public const string AddComment = "order:add_comment";
        public const string SkipComment = "order:skip_comment";
        public const string ConfirmOrder = "order:confirm_order";
        public const string CancelOrder = "order:cancel_order";
    }

    // Логика сервиса оформления заказа
    public class MakingOrdersService : IMakingOrdersService
    {
        private readonly ILogger<MakingOrdersService> _logger;
        private readonly IOrdersRepository _ordersRepository;
        private readonly IOrdersDataRepository _ordersDataRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICartService _cartService;
        private Dictionary<string, OrderState> _states;

        public MakingOrdersService(
            IOrdersRepository ordersRepository,
            IOrdersDataRepository ordersDataRepository,
            IAuthorizationService authorizationService,
            IUserRepository userRepository,
            ICartService cartService,
            ILogger<MakingOrdersService> logger)
        {
            _logger = logger;
            _ordersRepository = ordersRepository;
            _ordersDataRepository = ordersDataRepository;
            _userRepository = userRepository;
            _cartService = cartService;
            _states = new Dictionary<string, OrderState>
            {
                { typeof(WaitingForAddressState).Name, new WaitingForAddressState(this) },
                { typeof(WaitingForCommentState).Name, new WaitingForCommentState(this) },
                { typeof(WaitingForCommentTextState).Name, new WaitingForCommentTextState(this) },
                { typeof(WaitingForConfirmationState).Name, new WaitingForConfirmationState(this) }
            };
        }

        public async Task<bool> StartOrderCreationAsync(long userId)
        {
            try
            {
                // Создаем временные данные для заказа
                UserOrderData orderData = new UserOrderData
                {
                    UserId = userId,
                    CurrentState = typeof(WaitingForAddressState).Name,
                    CartItems = new List<CartItem>() // TODO: Здесь должна быть логика получения корзины
                };

                // Сохраняем временные данные в репозиторий временных заказов
                await _ordersDataRepository.SaveOrderDataAsync(orderData);
                _logger.LogInformation($"Начат процесс оформления заказа для пользователя {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка в начале оформления заказа для пользователя {userId}");
                return false;
            }
        }

        public async Task<OrderProcessingResult> ProcessUserInputAsync(long userId, string message)
        {
            try
            {
                // Получаем текущие данные заказа из временных заказов
                UserOrderData orderData = await _ordersDataRepository.GetOrderDataAsync(userId);
                if (orderData == null)
                {
                    return new OrderProcessingResult
                    {
                        Success = false,
                        Message = "❌ Сессия оформления заказа не найдена. Начните заново."
                    };
                }

                // Получаем текущее состояние
                if (!_states.TryGetValue(orderData.CurrentState, out var currentState))
                {
                    // если состояние не найдено
                    orderData.CurrentState = typeof(WaitingForAddressState).Name;
                    currentState = _states[orderData.CurrentState];
                    await SaveOrderDataAsync(orderData);
                }                

                // Обрабатываем ввод данных через текущее состояние
                var result = await currentState.HandleInputAsync(userId, message, orderData);

                // Обновляем состояние, если нужно
                if (result.NextState != null)
                {
                    var nextStateName = result.NextState;
                    if (orderData.CurrentState != nextStateName)
                    {
                        orderData.CurrentState = nextStateName;
                        await SaveOrderDataAsync(orderData);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обработке ввода пользователя {userId}");
                return new OrderProcessingResult
                {
                    Success = false,
                    Message = "❌ Произошла ошибка при обработке данных. Попробуйте еще раз."
                };
            }
        }

        public async Task<bool> CancelOrderCreationAsync(long userId)
        {
            try
            {
                // Удаляем временные данные заказа из репозитория
                await _ordersDataRepository.DeleteOrderDataAsync(userId);
                _logger.LogInformation($"Оформление заказа отменено для пользователя {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отмене оформления заказа для пользователя {userId}");
                return false;
            }
        }

        public async Task<Orders> GetCurrentOrderAsync(long userId)
        {
            try
            {
                // Получаем текущаий (последний созданный) заказ пользователя из репозитория оформленных заказов
                return await _ordersRepository.GetUserCurrentOrderAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении текущего заказа для пользователя {userId}");
                return null;
            }
        }

        // Сохраняем оформленный заказ в бд
        public async Task<bool> CompleteOrderAsync(long userId)
        {
            try
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
                    Status = "Created",
                    Price = orderData.Price,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при завершении заказа для пользователя {userId}");
                return false;
            }
        }

        // ВАЖНО! Чтобы не пропустить текстовые сообщения в другие сервисы
        public async Task<bool> IsUserInOrderProcessAsync(long userId)
        {
            try
            {
                // Проверяем наличие временных данных заказа
                var orderData = await _ordersDataRepository.GetOrderDataAsync(userId);
                return orderData != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке процесса оформления заказа для пользователя {userId}");
                return false;
            }
        }

        #region Methods

        // Проблемно "дотянуться" до репозиториев, поэтому через три класса будем делать(
        public async Task SaveOrderDataAsync(UserOrderData orderData)
        {
            await _ordersDataRepository.SaveOrderDataAsync(orderData);
        }

        public async Task<OrderProcessingResult> ProceedToConfirmation(long userId, UserOrderData orderData)
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
            // Получение данных корзины пользователя
            var cart = await _cartService.GetCartAsync(userId);
            orderData.Price = cart.Price;
            orderData.CartItems = cart.CartItems;


            return new OrderProcessingResult
            {
                Success = true,
                Message = GetConfirmationMessage(orderData),
                NextState = typeof(WaitingForConfirmationState).Name,
                HasInlineKeyboard = true,
                InlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Подтвердить заказ", MakingOrdersCommands.ConfirmOrder),
                        InlineKeyboardButton.WithCallbackData("❌ Отменить", MakingOrdersCommands.CancelOrder)
                    }
                })
            };
        }

        // Формируем сообщение с подтверждением заказа
        private string GetConfirmationMessage(UserOrderData orderData)
        {
            string result = $"📋 Подтвердите заказ:\n\n" +
                            $"👤 Имя: {orderData.Name}\n" +
                            $"📞 Телефон: {orderData.PhoneNumber}\n" +
                            $"📍 Адрес: {orderData.Address}\n";

            // Добавляем комментарий, если он есть
            if (!string.IsNullOrEmpty(orderData.Comment))
            {
                result = string.Concat(result, $"💬 Комментарий: {orderData.Comment}\n");
            }

            result = string.Concat(result, $"💰 Сумма: {orderData.Price} руб.\n\n");
            result = string.Concat(result, "Всё верно?\n");

            return result;
        }

        #endregion

        

    }
}
