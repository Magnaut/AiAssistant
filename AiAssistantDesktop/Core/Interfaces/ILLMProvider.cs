using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiAssistantDesktop.Core.Interfaces
{
    public interface ILLMProvider : IDisposable
    {
        string ModelName { get; }
        bool IsReady { get; }

        Task InitializeAsync();

        // Основной метод (без опций)
        Task<string> GenerateAsync(string prompt);

        // 🔥 Новый метод с кастомными опциями
        Task<string> GenerateAsync(string prompt, Dictionary<string, object>? customOptions = null);

        IAsyncEnumerable<string> GenerateStreamingAsync(string prompt);
        void SetGenerationParams(int? maxTokens = null, float? temperature = null);
    }
}