using System;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Interfaces;

namespace AiAssistantDesktop.Modules.TTS
{
    public class SystemTtsService : ITTSService, IDisposable
    {
        private SpeechSynthesizer? _synthesizer;
        private bool _isSpeaking;

        public bool IsSpeaking => _isSpeaking;

        public SystemTtsService()
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
            _synthesizer.Rate = 0;
        }

        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            if (_synthesizer == null || string.IsNullOrWhiteSpace(text)) return;

            _isSpeaking = true;
            try
            {
                await Task.Run(() => _synthesizer.Speak(text), ct);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isSpeaking = false;
            }
        }

        public Task StopAsync()
        {
            _synthesizer?.SpeakAsyncCancelAll();
            _isSpeaking = false;
            return Task.CompletedTask;
        }

        public Task<bool> IsVoiceLoadedAsync() => Task.FromResult(true);

        public void Dispose()
        {
            _synthesizer?.Dispose();
        }
    }
}