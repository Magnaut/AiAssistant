using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistantDesktop.Core.Interfaces
{
    public interface ITTSService : IDisposable
    {
        bool IsSpeaking { get; }

        Task SpeakAsync(string text, CancellationToken ct = default);
        Task StopAsync();
        Task<bool> IsVoiceLoadedAsync();
    }
}