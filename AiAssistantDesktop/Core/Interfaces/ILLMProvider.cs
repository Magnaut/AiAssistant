using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiAssistantDesktop.Core.Interfaces
{
    /// <summary>
    /// Интерфейс провайдера LLM
    /// </summary>
    public interface ILLMProvider : IDisposable
    {
        string ModelName { get; }
        bool IsReady { get; }

        Task InitializeAsync();
        Task<string> GenerateAsync(string prompt);
        IAsyncEnumerable<string> GenerateStreamingAsync(string prompt);
        void SetGenerationParams(int? maxTokens = null, float? temperature = null);
    }
}