using System;
using System.Threading.Tasks;

namespace AiAssistantDesktopDemo.Core.Interfaces
{
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