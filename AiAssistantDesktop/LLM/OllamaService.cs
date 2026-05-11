using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AiAssistantDesktopDemo
{
    public class OllamaService
    {
        private static readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        private List<Message> conversationHistory = new();
        private const string OllamaUrl = "http://localhost:11434/api/chat";

        // 🔹 Основная (русская):
        //private const string ModelName = "lakomoor/vikhr-llama-3.2-1b-instruct:1b";

        // 🔹 Резервная (универсальная):
        private const string ModelName = "qwen2.5:1.5b";

        private static readonly string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ollama_debug.log");

        public OllamaService()
        {
            File.AppendAllText(logFile, $"\n\n=== [{DateTime.Now}] Инициализация ===\n");
            File.AppendAllText(logFile, $"Модель: {ModelName}\n");

            // 🔥 Системный промпт, оптимизированный для Vikhr
            conversationHistory.Add(new Message
            {
                Role = "system",
                Content = "Ты — Michelle, дружелюбный русскоязычный ассистент. Отвечай кратко (1-2 предложения), по делу, на чистом русском. Будь полезной, вежливой и естественной в общении."
            });
        }

        public async Task<string> GetResponseAsync(string userMessage)
        {
            File.AppendAllText(logFile, $"\n[{DateTime.Now}] Запрос: '{userMessage}'\n");

            try
            {
                var ping = await httpClient.GetAsync("http://localhost:11434");
                File.AppendAllText(logFile, $"[Ping] Статус: {ping.StatusCode}\n");
            }
            catch (Exception pingEx)
            {
                File.AppendAllText(logFile, $"[Ping] ❌ ОШИБКА: {pingEx.Message}\n");
                return "⚠️ Ollama не запущена. Открой терминал: ollama serve";
            }

            try
            {
                conversationHistory.Add(new Message { Role = "user", Content = userMessage });

                // 🔥 Добавляем параметры для лучшего качества ответов
                var request = new
                {
                    model = ModelName,
                    messages = conversationHistory,
                    stream = false,
                    options = new
                    {
                        temperature = 0.3f,   // 🔥 Рекомендовано для Vikhr: меньше "воды"
                        top_p = 0.95f,        // Баланс креативности/точности
                        num_predict = 256     // Ограничиваем длину ответа
                    }
                };

                string json = JsonSerializer.Serialize(request);
                File.AppendAllText(logFile, $"[Request] JSON length: {json.Length}\n");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(OllamaUrl, content);

                string responseJson = await response.Content.ReadAsStringAsync();
                File.AppendAllText(logFile, $"[Response] Статус: {response.StatusCode}\n");

                if (!response.IsSuccessStatusCode)
                {
                    File.AppendAllText(logFile, $"[Error] HTTP {response.StatusCode}\n");
                    return $"Ошибка сервера: {response.StatusCode}";
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson, options);

                if (result?.Message?.Content != null && !string.IsNullOrEmpty(result.Message.Content))
                {
                    conversationHistory.Add(result.Message);
                    if (conversationHistory.Count > 12)
                        conversationHistory.RemoveRange(1, conversationHistory.Count - 11);

                    File.AppendAllText(logFile, $"[✅ Success] Ответ: {result.Message.Content}\n");
                    return result.Message.Content;
                }
                else
                {
                    File.AppendAllText(logFile, $"[⚠️ Warning] Пустой ответ.\n");
                    return "ИИ вернул пустой ответ.";
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"[❌ Exception] {ex.GetType().Name}: {ex.Message}\n");
                return $"Ошибка: {ex.Message}";
            }
        }

        public void ClearHistory()
        {
            conversationHistory.Clear();
            conversationHistory.Add(new Message
            {
                Role = "system",
                Content = "Ты — Michelle, дружелюбный русскоязычный ассистент."
            });
        }
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    public class OllamaResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}