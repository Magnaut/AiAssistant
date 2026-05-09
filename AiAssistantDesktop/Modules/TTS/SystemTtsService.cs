using System;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Interfaces;

namespace AiAssistantDesktop.Modules.TTS
{
    public class SystemTtsService : ITTSService
    {
        private SpeechSynthesizer? _synthesizer;
        private bool _isSpeaking;

        public bool IsSpeaking => _isSpeaking;
        public event Action? OnSpeakingFinished;

        public SystemTtsService()
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
            _synthesizer.Rate = 0;
            _synthesizer.Volume = 100;
        }

        public async Task SpeakAsync(string text, CancellationToken ct = default)
        {
            if (_synthesizer == null || string.IsNullOrWhiteSpace(text)) return;

            _isSpeaking = true;
            try
            {
                await Task.Run(() => _synthesizer.Speak(text), ct);
            }
            catch (OperationCanceledException)
            {
                // Игнорируем отмену
            }
            finally
            {
                _isSpeaking = false;
                OnSpeakingFinished?.Invoke();
            }
        }

        public Task StopAsync()
        {
            _synthesizer?.SpeakAsyncCancelAll();
            _isSpeaking = false;
            OnSpeakingFinished?.Invoke();
            return Task.CompletedTask;
        }

        public Task<bool> IsVoiceLoadedAsync() => Task.FromResult(true);

        public void Dispose()
        {
            _synthesizer?.Dispose();
        }
    }
}