namespace TelegramBotApp
{
    public class TelegramBotSettings
    {
        public string BotToken { get; set; }
    }

    public class AISettings
    {
        public string ApiUrl { get; set; }
        public string Model { get; set; }
        public string SystemMessage { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public bool Stream { get; set; }
    }
}