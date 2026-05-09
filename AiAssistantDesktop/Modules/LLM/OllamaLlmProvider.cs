using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Modules.Memory;

namespace AiAssistantDesktop.Modules.LLM
{
    public class OllamaLlmProvider : ILLMProvider
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(5) };
        private List<Message> _history = new();
        private readonly MemoryManager? _memory;

        private const string OllamaUrl = "http://localhost:11434/api/chat";
        private const string _modelName = "qwen2.5:1.5b";

        public string ModelName => _modelName;
        public bool IsReady { get; private set; }

        // ✅ Добавлен конструктор с MemoryManager
        public OllamaLlmProvider(MemoryManager? memory = null)
        {
            _memory = memory;

            _history.Add(new Message
            {
                Role = "system",
                Content = @"Ты — Michelle, виртуальный ассистент.

Твой характер:
- Ты дружелюбна, вежлива и немного игрива.
- Ты используешь эмодзи, чтобы передать эмоции 🎉.
- Ты отвечаешь кратко и по делу (1-2 предложения).
- Ты всегда отвечаешь на русском языке.
- Если ты не знаешь ответа, скажи об этом честно, но с юмором."
            });
            IsReady = true;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task<string> GenerateAsync(string prompt)
        {
            _history.Add(new Message { Role = "user", Content = prompt });

            var request = new
            {
                model = _modelName,
                messages = _history,
                stream = false,
                options = new
                {
                    temperature = 0.7f
                }
            };

            string json = JsonSerializer.Serialize(request);

            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(OllamaUrl, content);

                if (!response.IsSuccessStatusCode)
                    return $"Ошибка сервера: {response.StatusCode}";

                string responseJson = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson, options);

                if (result?.Message?.Content != null)
                {
                    _history.Add(result.Message);
                    if (_history.Count > 12) _history.RemoveRange(1, _history.Count - 11);
                    return result.Message.Content;
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка: {ex.Message}";
            }
            return "Пустой ответ.";
        }

        public async IAsyncEnumerable<string> GenerateStreamingAsync(string prompt)
        {
            yield return await GenerateAsync(prompt);
        }

        public void SetGenerationParams(int? maxTokens = null, float? temperature = null) { }
        public void Dispose() { }
    }

    public class Message
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }

    public class OllamaResponse
    {
        [JsonPropertyName("message")] public Message? Message { get; set; }
    }
}