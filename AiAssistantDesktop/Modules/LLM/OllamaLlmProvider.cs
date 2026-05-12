using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private string _currentModel = "qwen2.5:1.5b";
        private List<string> _availableModels = new() { "qwen2.5:1.5b", "qwen2.5:0.5b", "tinyllama", "phi" };
        private readonly string _logFile;

        public string ModelName => _currentModel;
        public string CurrentModel => _currentModel;
        public IEnumerable<string> AvailableModels => _availableModels.AsReadOnly();
        public bool IsReady { get; private set; } = true;

        public OllamaLlmProvider()
        {
            _logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ollama_debug.log");
            _history.Add(new Message { Role = "system", Content = "Ты Michelle. Отвечай кратко на русском." });
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
                        _availableModels = result.Models.Select(m => m.Name).ToList();
                }
            }
            catch { }
        }

        public async Task<bool> SwitchModelAsync(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName)) return false;
            if (modelName == _currentModel) { Debug.WriteLine($"✅ Уже активна: {modelName}"); return true; }
            if (!_availableModels.Contains(modelName)) _availableModels.Add(modelName);
            _currentModel = modelName;
            _history.Clear();
            _history.Add(new Message { Role = "system", Content = "Ты Michelle. Отвечай кратко на русском." });
            await LogAsync($"Switched to: {modelName}");
            return true;
        }

        public Task ReloadAsync() { _history.Clear(); _history.Add(new Message { Role = "system", Content = "Ты Michelle. Отвечай кратко на русском." }); return LoadAvailableModelsAsync(); }

        public Task<string> GenerateAsync(string prompt) => GenerateAsync(prompt, null);

        public async Task<string> GenerateAsync(string prompt, Dictionary<string, object>? customOptions = null)
        {
            if (_history.Count > 4) _history.RemoveAt(1);
            _history.Add(new Message { Role = "user", Content = prompt });

            var options = new Dictionary<string, object>
            {
                { "temperature", 0.7f }, { "top_p", 0.9f }, { "num_predict", 128 },
                { "num_ctx", 2048 }, { "num_thread", 2 }, { "repeat_penalty", 1.1f }
            };
            if (customOptions != null) foreach (var kvp in customOptions) options[kvp.Key] = kvp.Value;

            var request = new { model = _currentModel, messages = _history, stream = false, options };
            var json = JsonSerializer.Serialize(request);

            Debug.WriteLine($"📤 Ollama запрос (модель: {_currentModel}):\n{json.Substring(0, Math.Min(400, json.Length))}...");

            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(OllamaUrl, content);
                if (!response.IsSuccessStatusCode) return $"Ошибка: {response.StatusCode}";

                var responseJson = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"📥 Ollama ответ:\n{responseJson.Substring(0, Math.Min(300, responseJson.Length))}...");

                var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result?.Message?.Content != null) { _history.Add(result.Message); return result.Message.Content; }
            }
            catch (Exception ex) { await LogAsync($"Error: {ex.Message}"); return $"Ошибка: {ex.Message}"; }
            return "Не удалось ответить.";
        }

        public async IAsyncEnumerable<string> GenerateStreamingAsync(string prompt) { yield return await GenerateAsync(prompt); }
        public void SetGenerationParams(int? maxTokens = null, float? temperature = null) { }

        private async Task LogAsync(string message) { try { await File.AppendAllTextAsync(_logFile, $"[{DateTime.Now:HH:mm:ss}] {message}\n"); } catch { } }
        public void Dispose() { }
    }

    public class Message { [JsonPropertyName("role")] public string Role { get; set; } = ""; [JsonPropertyName("content")] public string Content { get; set; } = ""; }
    public class OllamaResponse { [JsonPropertyName("message")] public Message? Message { get; set; } }
    public class OllamaTagsResponse { [JsonPropertyName("models")] public List<ModelInfo> Models { get; set; } = new(); }
    public class ModelInfo { [JsonPropertyName("name")] public string Name { get; set; } = ""; }
}