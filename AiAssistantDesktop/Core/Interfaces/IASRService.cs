using System;
using System.Threading.Tasks;

namespace AiAssistantDesktop.Core.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса распознавания речи
    /// </summary>
    public interface IASRService : IDisposable
    {
        bool IsListening { get; }
        event Action<string>? OnTextRecognized;
        event Action<string>? OnError;

        Task StartAsync();
        Task StopAsync();
        Task<bool> IsModelLoadedAsync();
    }
}