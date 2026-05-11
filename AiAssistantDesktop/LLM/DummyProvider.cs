
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AiAssistantDesktopDemo.Core.Interfaces;

namespace AiAssistantDesktopDemo.LLM
{
    /// <summary>
    /// Заглушка для LLM — возвращает фиксированные ответы без сети
    /// </summary>
    public class DummyProvider : ILLMProvider
    {
        public string ModelName => "Dummy";
        public bool IsReady => true;

        public Task InitializeAsync()
        {
            Debug.WriteLine("[Dummy] Инициализирован");
            return Task.CompletedTask;
        }

        public Task<string> GenerateAsync(string prompt)
        {
            Debug.WriteLine($"[Dummy] Получен промпт: {prompt}");

            // Простые ответы для теста
            string response = prompt.ToLower().Contains("привет")
                ? "Привет! Я слышу тебя! 🎤"
                : $"Ты сказал: \"{prompt}\" — я пока учусь отвечать!";

            Debug.WriteLine($"[Dummy] Ответ: {response}");
            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<string> GenerateStreamingAsync(string prompt)
        {
            var response = await GenerateAsync(prompt);
            yield return response;
        }

        public void SetGenerationParams(int? maxTokens = null, float? temperature = null) { }
        public void Dispose() { }
    }
}
