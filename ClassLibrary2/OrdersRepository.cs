using DataAccess.Entities;
using DataAccess.EntityFramework;
using DataAccess.Repositories.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;

namespace DataAccess.Repositories.Implementations
{
    /// <summary>
    /// Интерфейс оформленных заказов
    /// </summary>
    public class OrdersRepository : IOrdersRepository
    {
        public const string Completed = "Completed";

        // Фабрика для создания областей видимости (scopes) зависимостей
        private readonly IServiceScopeFactory _scopeFactory;

        // Конструктор, куда передается фабрика областей видимости
        public OrdersRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // Создание нового заказа в бд
        // Бот обрабатывает много пользователей одновременно !!!
        // Без scope factory все потоки используют один DbContext !!!
        public async Task<Orders> CreateOrderAsync(Orders order)
        {
            // Каждый раз создаем НОВУЮ область видимости для изоляции работы с БД            
            using IServiceScope scope = _scopeFactory.CreateScope();
            // Каждый раз получаем НОВЫЙ DbContext бд из контейнера зависимостей
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Изолированная работа с БД
            // Добавляем заказ в контекст бд
            await dbContext.Orders.AddAsync(order);
            // Сохраняем изменения в бд
            await dbContext.SaveChangesAsync();
            return order;
        }

        // Получение заказа по его ID
        public async Task<Orders> GetOrderByIdAsync(int orderId)
        {
            // Каждый раз создаем НОВУЮ область видимости для изоляции работы с БД            
            using IServiceScope scope = _scopeFactory.CreateScope();
            // Каждый раз получаем НОВЫЙ DbContext бд из контейнера зависимостей
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Ищем заказ по ID и включаем связанные данные о пользователе
            return await dbContext.Orders
                .Include(o => o.User)                             // Загружаем данные пользователя вместе с заказом
                .FirstOrDefaultAsync(x => x.OrderId == orderId);  // Находим первый заказ с указанным ID или null
        }

        // Получение текущего (последнего) заказа пользователя
        public async Task<Orders> GetUserCurrentOrderAsync(long userId)
        {
            // Каждый раз создаем НОВУЮ область видимости для изоляции работы с БД            
            using IServiceScope scope = _scopeFactory.CreateScope();
            // Каждый раз получаем НОВЫЙ DbContext бд из контейнера зависимостей
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            return await dbContext.Orders
                .Where(o => o.IdUser == userId)           // Фильтруем заказы по ID пользователя
                .OrderByDescending(o => o.DateOrder)      // Сортируем по дате (сначала новые)
                .FirstOrDefaultAsync();                   // Берем самый свежий заказ
        }

        // Обновление существующего заказа
        public async Task<bool> UpdateOrderAsync(Orders order)
        {
            // Каждый раз создаем НОВУЮ область видимости для изоляции работы с БД            
            using IServiceScope scope = _scopeFactory.CreateScope();
            // Каждый раз получаем НОВЫЙ DbContext бд из контейнера зависимостей
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Помечаем заказ как измененный в контексте БД
            dbContext.Orders.Update(order);
            // Сохраняем изменения в базе
            await dbContext.SaveChangesAsync();
            return true;
        }

        // Удаление заказа по ID
        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            // Каждый раз создаем НОВУЮ область видимости для изоляции работы с БД            
            using IServiceScope scope = _scopeFactory.CreateScope();
            // Каждый раз получаем НОВЫЙ DbContext бд из контейнера зависимостей
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            // Находим заказ по ID
            Orders order = await dbContext.Orders.FindAsync(orderId);
            // Если заказ существует - удаляем его
            if (order != null)
            {
                dbContext.Orders.Remove(order);
                await dbContext.SaveChangesAsync();
            }
            return true;
        }

        // Получение всех заказов пользователя
        public async Task<List<Orders>> GetUserOrdersAsync(long userId)
        {
            // Каждый раз создаем НОВУЮ область видимости для изоляции работы с БД            
            using IServiceScope scope = _scopeFactory.CreateScope();
            // Каждый раз получаем НОВЫЙ DbContext бд из контейнера зависимостей
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            return await dbContext.Orders
                .Where(o => o.IdUser == userId)           // Фильтруем по пользователю
                .OrderByDescending(o => o.DateOrder)      // Сортируем по дате (сначала новые)
                .ToListAsync();                           // Преобразуем в список
        }

        // Получение заказов по статусу
        public async Task<List<Orders>> GetOrdersByStatusAsync(string status)
        {
            // Каждый раз создаем НОВУЮ область видимости для изоляции работы с БД            
            using IServiceScope scope = _scopeFactory.CreateScope();
            // Каждый раз получаем НОВЫЙ DbContext бд из контейнера зависимостей
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            return await dbContext.Orders
                .Include(o => o.User)                     // Включаем данные пользователя
                .Where(o => o.Status == status)           // Фильтруем по статусу
                .OrderByDescending(o => o.DateOrder)      // Сортируем по дате (сначала новые)
                .ToListAsync();                           // Преобразуем в список
        }

        public async Task<List<Orders>> GetCourierOrdersAsync(long courierId)
        {
            // Каждый раз создаем НОВУЮ область видимости для изоляции работы с БД            
            using IServiceScope scope = _scopeFactory.CreateScope();
            // Каждый раз получаем НОВЫЙ DbContext бд из контейнера зависимостей
            DataContext dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

            return await dbContext.Orders
                .Where(o => o.IdCourier == courierId)
                .Where(o => o.Status != Completed)
                .OrderByDescending(o => o.DateOrder)
                .ToListAsync();
        }
    }

    /// <summary>
    /// Черновики заказов в процессе
    /// </summary>
    public class OrderDataRepository : IOrdersDataRepository
    {
        // Словарь для хранения временных данных заказов в памяти приложения
        // Key: UserId
        // Value: данные заказа во время оформления
        private readonly Dictionary<long, UserOrderData> _orderDataStorage = new();

        // Объект для синхронизации доступа к словарю
        private readonly object _lockObject = new();

        // Получение временных данных заказа для пользователя
        public Task<UserOrderData> GetOrderDataAsync(long userId)
        {
            // Блокируем доступ к словарю (попытка потокобезопасности)
            lock (_lockObject)
            {
                // Пытаемся получить данные заказа по ID пользователя
                if (_orderDataStorage.TryGetValue(userId, out UserOrderData orderData))
                {
                    return Task.FromResult(orderData);
                }
                return Task.FromResult<UserOrderData>(null);
            }
        }

        // Сохранение временных данных заказа
        public Task<bool> SaveOrderDataAsync(UserOrderData orderData)
        {
            lock (_lockObject)
            {
                _orderDataStorage[orderData.UserId] = orderData;
                return Task.FromResult(true);
            }
        }

        // Удаление временных данных заказа
        public Task<bool> DeleteOrderDataAsync(long userId)
        {
            lock (_lockObject)
            {
                return Task.FromResult(_orderDataStorage.Remove(userId));
            }
        }
    }
}
