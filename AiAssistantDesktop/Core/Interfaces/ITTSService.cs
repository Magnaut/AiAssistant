using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistantDesktop.Core.Interfaces
{
    /// <summary>
    /// Интерфейс сервиса синтеза речи
    /// </summary>
    public interface ITTSService : IDisposable
    {
        bool IsSpeaking { get; }
        event Action? OnSpeakingFinished;
        Task SpeakAsync(string text, CancellationToken ct = default);
        Task StopAsync();
        Task<bool> IsVoiceLoadedAsync();
    }
}