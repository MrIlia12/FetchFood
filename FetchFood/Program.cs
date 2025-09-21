using FetchFood;
using FetchFood.Services;
using System.Text.Json;
using FetchFood.Models;

Console.WriteLine("Запуск телеграм-бота FetchFood");

string json = await File.ReadAllTextAsync("settings.json");

Settings settings = JsonSerializer.Deserialize<Settings>(json)
    ?? throw new InvalidOperationException($"[{LogMessages.ERROR}]: структура settings.json некорректна."); // разбираем настройки из json

string token = settings.Telegram.BotToken;
if (string.IsNullOrEmpty(token))
throw new InvalidOperationException($"[{LogMessages.ERROR}]: не удалось получить токен из settings.json.");

// Создаем сервис заказов
IOrderService orderService = new OrderService();

// Создаем бота с передачей зависимости
TelegramBotService botService = new TelegramBotService(token, orderService);
await botService.StartAsync();

Console.WriteLine("Бот запущен. Нажмите любую клавишу для выхода...");
Console.ReadKey();
await botService.StopAsync();

Environment.Exit(0);
