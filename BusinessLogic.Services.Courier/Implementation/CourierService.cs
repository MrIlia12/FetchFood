using BusinessLogic.Services.Courier.Abstractions;
using DataAccess.Entities;
using DataAccess.Entities.Models;
using DataAccess.Repositories.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace BusinessLogic.Services.Courier.Implementation
{
    /// <summary>
    /// Сервис курьера - управляет доставкой заказов
    /// </summary>
    public class CourierService : ICourierService
    {
        private readonly ILogger<CourierService> _logger;
        private readonly IOrdersRepository _ordersRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICourierRepository _courierRepository;

        /// <summary>
        /// Статусы заказа
        /// </summary>
        public static class OrderStatuses
        {
            public const string Created = "Created";
            public const string ToDelivery = "ToDelivery";
            public const string InDelivery = "InDelivery";
            public const string CourierArrived = "CourierArrived";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
        }

        public CourierService(
            IOrdersRepository ordersRepository,
            IUserRepository userRepository,
            ICourierRepository courierRepository,
            ILogger<CourierService> logger)
        {
            _ordersRepository = ordersRepository;
            _userRepository = userRepository;
            _courierRepository = courierRepository;
            _logger = logger;
        }

        /// <summary>
        /// Проверяет, является ли пользователь курьером
        /// </summary>
        public async Task<bool> IsCourierAsync(long userId)
        {
            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                return user?.Role == UserRole.Courier;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке роли курьера для пользователя {userId}");
                return false;
            }
        }

        /// <summary>
        /// Получает список активных заказов для курьера
        /// </summary>
        public async Task<List<Orders>> GetAvailableOrdersAsync(long courierId)
        {
            try
            {
                // Получаем заказы в статусе "ToDelivery"
                var inDeliveryOrders = await _ordersRepository.GetOrdersByStatusAsync(OrderStatuses.ToDelivery);
                
                var allOrders = new List<Orders>();
                if (inDeliveryOrders != null) allOrders.AddRange(inDeliveryOrders);
                
                return allOrders.OrderByDescending(o => o.DateOrder).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении заказов курьера {courierId}");
                return new List<Orders>();
            }
        }

        /// <summary>
        /// Получает список заказов данного курьера.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Orders>> GetCourierOrdersAsync(long userId)
        {
            try
            {
                var courier = await _courierRepository.GetCourierByUserIdAsync(userId);
                var inDeliveryOrders = await _ordersRepository.GetCourierOrdersAsync(courier.CourierId);

                var allOrders = new List<Orders>();
                if (inDeliveryOrders != null) allOrders.AddRange(inDeliveryOrders);

                return allOrders.OrderByDescending(o => o.DateOrder).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении заказов курьера {userId}");
                return new List<Orders>();
            }
        }

        /// <summary>
        /// Получает детали заказа
        /// </summary>
        public async Task<Orders> GetOrderDetailsAsync(long orderId)
        {
            try
            {
                return await _ordersRepository.GetOrderByIdAsync((int)orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении деталей заказа {orderId}");
                return null;
            }
        }

        /// <summary>
        /// Курьер прибыл на место - уведомляем пользователя
        /// </summary>
        public async Task<CourierArrivalResult> NotifyArrivalAsync(long courierId, long orderId)
        {
            try
            {
                // Получаем заказ
                var order = await _ordersRepository.GetOrderByIdAsync((int)orderId);
                
                if (order == null)
                {
                    return new CourierArrivalResult
                    {
                        Success = false,
                        Message = "❌ Заказ не найден."
                    };
                }

                // Проверяем статус заказа - разрешаем для Created и InDelivery
                if (order.Status != OrderStatuses.Created && order.Status != OrderStatuses.InDelivery)
                {
                    return new CourierArrivalResult
                    {
                        Success = false,
                        Message = $"❌ Невозможно отметить прибытие. Текущий статус заказа: {order.Status}"
                    };
                }

                // Обновляем статус заказа на "Курьер прибыл"
                order.Status = OrderStatuses.CourierArrived;
                await _ordersRepository.UpdateOrderAsync(order);

                _logger.LogInformation($"Курьер {courierId} прибыл по заказу {orderId} к пользователю {order.IdUser}");

                return new CourierArrivalResult
                {
                    Success = true,
                    Message = $"✅ Статус заказа #{orderId} обновлен!\n\nПользователь получит уведомление о вашем прибытии.",
                    UserIdToNotify = order.IdUser,
                    UserNotificationMessage = $"🚗 Курьер прибыл!\n\n" +
                                              $"Ваш заказ #{orderId} доставлен по адресу:\n" +
                                              $"📍 {order.Address}\n\n" +
                                              $"Пожалуйста, встретьте курьера."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отметке прибытия курьера {courierId} по заказу {orderId}");
                return new CourierArrivalResult
                {
                    Success = false,
                    Message = "❌ Произошла ошибка. Попробуйте позже."
                };
            }
        }

        /// <summary>
        /// Берём в доставку заказ
        /// </summary>
        /// <returns></returns>
        public async Task<bool> TakeOrderInDeliveryAsync(long courierId, int orderId)
        {
            try
            {
                var courier = await _courierRepository.GetCourierByUserIdAsync(courierId);
                var order = await _ordersRepository.GetOrderByIdAsync(orderId);

                order.IdCourier = courier.CourierId;
                order.Status = OrderStatuses.InDelivery;

                return await _ordersRepository.UpdateOrderAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обновлении заказа {orderId} курьером {courierId}");
                return false;
            }
        }

        /// <summary>
        /// Курьер завершил доставку
        /// </summary>
        public async Task<bool> CompleteDeliveryAsync(long courierId, long orderId)
        {
            try
            {
                var order = await _ordersRepository.GetOrderByIdAsync((int)orderId);
                
                if (order == null)
                {
                    _logger.LogWarning($"Попытка завершить несуществующий заказ {orderId}");
                    return false;
                }

                order.Status = OrderStatuses.Completed;
                await _ordersRepository.UpdateOrderAsync(order);

                _logger.LogInformation($"Заказ {orderId} завершен курьером {courierId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при завершении заказа {orderId} курьером {courierId}");
                return false;
            }
        }
    }
}
