using Microsoft.Extensions.Configuration;

namespace TelegramBotApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Загрузка конфигурации из appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Получение настроек
            var telegramBotSettings = configuration.GetSection("TelegramBotSettings").Get<TelegramBotSettings>();
            var aiSettings = configuration.GetSection("AISettings").Get<AISettings>();

            if (telegramBotSettings == null || aiSettings == null)
            {
                Console.WriteLine("Ошибка: Не удалось загрузить настройки из конфигурационного файла.");
                return;
            }

            // Запуск бота
            var bot = new TelegramBot(telegramBotSettings, aiSettings);
            await bot.StartAsync();
        }
    }
}