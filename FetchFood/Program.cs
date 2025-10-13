using FetchFood;
using FetchFood.Services;
using System.Text.Json;
using FetchFood.Models;
using BusinessLogic.Services.Authorization;
using DataAccess.Repositories.Abstractions;
using DataAccess.Repositories.Implementations;
using BusinessLogic.Services.Authorization.Abstractions;
using FetchFood.Abstractions;
using DataAccess.EntityFramework;
using BusinessLogic.Services.MakingOrders.Abstractions;
using BusinessLogic.Services.MakingOrders.Implemenatation;

public static class Program
{
    public static void Main(string[] args)
    {
        string connectionString = "Server=localhost;port=9432;database=FetchFood;User ID=postgres;password=1882320;";

        WebApplication app = ConfigureApp(args, connectionString);
        
        app.Run( async (context) =>
        {
            Console.WriteLine("Hello, World!");
            Console.WriteLine("Ilya Yaryshev!");
            Console.WriteLine("Привет, это Лия!");
            Console.WriteLine("Литвинова Анна");
            Console.WriteLine("Рахмонов Файзирахмон");

            string json = await File.ReadAllTextAsync("settings.json");

            Settings settings = JsonSerializer.Deserialize<Settings>(json)
                ?? throw new InvalidOperationException($"[{LogMessages.ERROR}]: структура settings.json некорректна."); // разбираем настройки из json

            string token = settings.Telegram.BotToken;
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException($"[{LogMessages.ERROR}]: не удалось получить токен из settings.json.");

            ITelegramBotService botService = app.Services.GetRequiredService<ITelegramBotService>();
            
            await botService.StartAsync(token);

            Console.WriteLine("Бот запущен. Нажмите любую клавишу для выхода...");
            Console.ReadKey();
            await botService.StopAsync();

            Environment.Exit(0);
        });

        app.Run();
    }

    public static WebApplication ConfigureApp(string[] args, string connectionString)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.ConfigureContext(connectionString);
        builder.Services.InstallServices();
        builder.Services.InstallRepositories();

        return builder.Build();
    }

    private static void InstallServices(this IServiceCollection serviceCollection)
    {
        serviceCollection
        .AddTransient<IAuthorizationService, AuthorizationService>()
        .AddTransient<ITelegramBotService, TelegramBotService>()
        .AddTransient<IMakingOrdersService, MakingOrdersService>();
    }

    private static void InstallRepositories(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddTransient<IUserRepository, UserRepository>()
            .AddTransient<IOrdersDataRepository, OrderDataRepository>()
            .AddTransient<IOrdersRepository, OrdersRepository>();
    }

}