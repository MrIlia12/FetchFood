namespace FetchFood.Models
{
    public class Settings
    {
        public TelegramSettings Telegram { get; set; } = new();
    }
    public class TelegramSettings
    {
        public string BotToken { get; set; } = String.Empty;
    }
}
