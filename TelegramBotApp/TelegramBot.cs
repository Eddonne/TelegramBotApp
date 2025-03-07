using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotApp
{
    public class TelegramBot
    {
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly AISettings _aiSettings;

        public TelegramBot(TelegramBotSettings telegramBotSettings, AISettings aiSettings)
        {
            _botClient = new TelegramBotClient(telegramBotSettings.BotToken);
            _httpClient = new HttpClient();
            _aiSettings = aiSettings;
        }

        public async Task StartAsync()
        {
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"[Лог] Бот {me.Username} запущен!");

            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);

            Console.WriteLine("Нажмите Enter для выхода...");
            Console.ReadLine();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Проверяем, что сообщение является текстовым и новым
            if (update.Type == UpdateType.Message &&
                update.Message.Type == MessageType.Text &&
                !string.IsNullOrEmpty(update.Message.Text))
            {
                var chatId = update.Message.Chat.Id;
                var userMessage = update.Message.Text;

                Console.WriteLine($"[Лог] Получено новое текстовое сообщение от {update.Message.Chat.Username}: {userMessage}");

                // Сообщение пользователю о том, что ответ готовится
                await botClient.SendTextMessageAsync(chatId, "Ожидайте, ответ готовится...");

                // Отправка сообщения в обработчик AI
                Console.WriteLine($"[Лог] Отправка запроса к AI модели: {userMessage}");
                var aiResponse = await GetAIResponseAsync(userMessage);

                // Логирование ответа от AI
                Console.WriteLine($"[Лог] Получен ответ от AI модели: {aiResponse}");

                // Отправка ответа пользователю
                await botClient.SendTextMessageAsync(chatId, aiResponse);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Обработка ошибок
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"[Ошибка] Ошибка Telegram API: {apiRequestException.ErrorCode} - {apiRequestException.Message}",
                _ => $"[Ошибка] Неизвестная ошибка: {exception}"
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private async Task<string> GetAIResponseAsync(string userMessage)
        {
            var requestBody = new
            {
                model = _aiSettings.Model,
                messages = new[]
                {
                    // Настройки системного промта заданы в LM Studio
                    // new { role = "system", content = _aiSettings.SystemMessage },
                    new { role = "user", content = userMessage }
                },
                temperature = _aiSettings.Temperature,
                max_tokens = _aiSettings.MaxTokens,
                stream = _aiSettings.Stream
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                Console.WriteLine($"[Лог] Отправка HTTP запроса к AI модели...");
                var response = await _httpClient.PostAsync(_aiSettings.ApiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);

                // Извлечение ответа AI модели
                var aiResponse = responseObject.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                return aiResponse ?? "Не удалось получить ответ от AI модели.";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[Ошибка] Ошибка HTTP запроса: {ex.Message}");
                return "Произошла ошибка при запросе к AI модели.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Ошибка] Неизвестная ошибка: {ex.Message}");
                return "Произошла неизвестная ошибка.";
            }
        }
    }
}