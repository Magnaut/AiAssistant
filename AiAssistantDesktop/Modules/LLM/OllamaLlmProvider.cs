using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Interfaces;

namespace AiAssistantDesktop.Modules.LLM
{
    public class OllamaLlmProvider : ILLMProvider, ISwitchableProvider
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(5) };
        private List<Message> _history = new();

        private const string OllamaUrl = "http://localhost:11434/api/chat";
        private const string OllamaTagsUrl = "http://localhost:11434/api/tags";

        private string _currentModel;
        private List<string> _availableModels;
        private readonly string _logFile;

        public string ModelName => _currentModel;
        public string CurrentModel => _currentModel;
        public IEnumerable<string> AvailableModels => _availableModels.AsReadOnly();
        public bool IsReady { get; private set; }

        public OllamaLlmProvider()
        {
            _currentModel = "qwen2.5:1.5b";
            _availableModels = new List<string> { "qwen2.5:0.5b", "qwen2.5:1.5b", "tinyllama", "phi" };
            _logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ollama_debug.log");

            _history.Add(new Message
            {
                Role = "system",
                Content = "Ты Michelle. Отвечай ОЧЕНЬ кратко (1 предложение) на русском."
            });
            IsReady = true;

            // Загружаем доступные модели асинхронно
            _ = LoadAvailableModelsAsync();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        private async Task LoadAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(OllamaTagsUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OllamaTagsResponse>(json);
                    if (result?.Models != null)
                    {
                        _availableModels = result.Models.Select(m => m.Name).ToList();
                    }
                }
            }
            catch { /* Игнорируем, используем дефолтный список */ }
        }

        public async Task<bool> SwitchModelAsync(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                return false;

            // 🔥 Если модель уже активна — считаем это успехом (не ошибкой)
            if (modelName == _currentModel)
            {
                await LogAsync($"Model already active: {modelName}");
                return true; // ✅ Возвращаем true вместо false
            }

            // Проверяем, есть ли модель в списке
            if (!_availableModels.Contains(modelName))
            {
                // Пытаемся добавить (надеюсь, она есть в Ollama)
                _availableModels.Add(modelName);
            }

            _currentModel = modelName;
            _history.Clear();
            _history.Add(new Message
            {
                Role = "system",
                Content = "Ты Michelle. Отвечай ОЧЕНЬ кратко (1 предложение) на русском."
            });

            await LogAsync($"Switched to model: {modelName}");
            return true;
        }

        public async Task ReloadAsync()
        {
            _history.Clear();
            _history.Add(new Message
            {
                Role = "system",
                Content = "Ты Michelle. Отвечай ОЧЕНЬ кратко (1 предложение) на русском."
            });
            await LoadAvailableModelsAsync();
        }

        public Task<string> GenerateAsync(string prompt) => GenerateAsync(prompt, null);

        public async Task<string> GenerateAsync(string prompt, Dictionary<string, object>? customOptions = null)
        {
            if (_history.Count > 4) _history.RemoveAt(1);
            _history.Add(new Message { Role = "user", Content = prompt });

            var options = new Dictionary<string, object>
            {
                { "temperature", 0.7f },
                { "top_p", 0.9f },
                { "num_predict", 64 },
                { "num_ctx", 1024 },
                { "num_thread", 2 },
                { "repeat_penalty", 1.1f }
            };

            if (customOptions != null)
                foreach (var kvp in customOptions) options[kvp.Key] = kvp.Value;

            var request = new { model = _currentModel, messages = _history, stream = false, options };
            string json = JsonSerializer.Serialize(request);

            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(OllamaUrl, content);

                if (!response.IsSuccessStatusCode)
                    return $"Ошибка: {response.StatusCode}";

                string responseJson = await response.Content.ReadAsStringAsync();
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson, jsonOptions);

                if (result?.Message?.Content != null)
                {
                    _history.Add(result.Message);
                    return result.Message.Content;
                }
            }
            catch (Exception ex)
            {
                await LogAsync($"Generate error: {ex.Message}");
                return $"Ошибка: {ex.Message}";
            }
            return "Не удалось ответить.";
        }

        public async IAsyncEnumerable<string> GenerateStreamingAsync(string prompt)
        {
            yield return await GenerateAsync(prompt);
        }

        public void SetGenerationParams(int? maxTokens = null, float? temperature = null) { }

        private async Task LogAsync(string message)
        {
            try
            {
                await File.AppendAllTextAsync(_logFile, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
            }
            catch { }
        }

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

    public class OllamaTagsResponse
    {
        [JsonPropertyName("models")] public List<ModelInfo> Models { get; set; } = new();
    }

    public class ModelInfo
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
    }
}