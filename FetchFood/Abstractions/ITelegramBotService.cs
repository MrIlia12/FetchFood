namespace FetchFood.Abstractions
{
    public interface ITelegramBotService
    {
        public async Task StartAsync(string token)
        { }

        public Task StopAsync();
    }
}
