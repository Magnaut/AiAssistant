using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiAssistantDesktopDemo.Core.Interfaces
{
    public interface ITTSService : IDisposable
    {
        bool IsSpeaking { get; }

        Task SpeakAsync(string text, CancellationToken ct = default);
        Task StopAsync();
        void SetVoiceParams(int? rate = null, int? volume = null);
    }
}