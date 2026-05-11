using System;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Events;
using AiAssistantDesktop.Core.Interfaces;

namespace AiAssistantDesktop.Core.Services
{
    public class ConversationAgent
    {
        private readonly IASRService _asr;
        private readonly ILLMProvider _llm;
        private readonly ITTSService _tts;
        private readonly IEventBus _eventBus;

        private bool _isThinking;

        public ConversationAgent(
            IASRService asr,
            ILLMProvider llm,
            ITTSService tts,
            IEventBus eventBus)
        {
            _asr = asr;
            _llm = llm;
            _tts = tts;
            _eventBus = eventBus;

            _asr.OnTextRecognized += OnTextRecognized;
        }

        private async void OnTextRecognized(string text)
        {
            if (_isThinking) return;

            _isThinking = true;
            await _asr.StopAsync();
            _eventBus.Publish(new UserSpokeEvent(text));

            try
            {
                _eventBus.Publish(new AgentThinkingEvent());
                string response = await _llm.GenerateAsync(text);
                await _tts.SpeakAsync(response);
                _eventBus.Publish(new AgentRespondedEvent(response));
            }
            catch (Exception ex)
            {
                _eventBus.Publish(new AgentErrorEvent(ex.Message));
            }
            finally
            {
                _isThinking = false;
                await _asr.StartAsync();
            }
        }

        public async Task StartAsync()
        {
            await _asr.StartAsync();
        }

        public async Task StopListeningAsync()
        {
            await _asr.StopAsync();
        }

        public async Task StartListeningAsync()
        {
            await _asr.StartAsync();
        }
    }

    // События
    public class UserSpokeEvent { public string Text { get; } public UserSpokeEvent(string text) => Text = text; }
    public class AgentThinkingEvent { }
    public class AgentRespondedEvent { public string Text { get; } public AgentRespondedEvent(string text) => Text = text; }
    public class AgentErrorEvent { public string Message { get; } public AgentErrorEvent(string message) => Message = message; }
}