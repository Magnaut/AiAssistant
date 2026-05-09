using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AiAssistantDesktopDemo.Core.Interfaces;
using AiAssistantDesktopDemo.Core.Models;

namespace AiAssistantDesktopDemo.Core.Services
{
    public class ConversationManager : IDisposable
    {
        private readonly IAgent _agent;
        private readonly IASRService? _asr;
        private readonly ITTSService? _tts;
        private readonly IAvatarController? _avatar;

        private CancellationTokenSource? _currentCts;
        private bool _isProcessing;

        private Action<string>? _asrHandler;

        public event Action<string>? OnUserSpoke;
        public event Action<AgentResponse>? OnAgentResponded;
        public event Action<string>? OnStatusChanged;
        public event Action<bool>? OnThinkingChanged;

        public ConversationManager(
            IAgent agent,
            IASRService? asr = null,
            ITTSService? tts = null,
            IAvatarController? avatar = null)
        {
            _agent = agent;
            _asr = asr;
            _tts = tts;
            _avatar = avatar;

            _agent.OnResponseGenerated += HandleAgentResponse;
            _agent.OnThinkingStarted += () => OnThinkingChanged?.Invoke(true);
            _agent.OnThinkingCompleted += () => OnThinkingChanged?.Invoke(false);
        }

        public async Task ProcessInputAsync(string userText)
        {
            if (_isProcessing)
            {
                await _agent.InterruptAsync();
                await _tts?.StopAsync();
                await _avatar?.StopSpeakingAsync();
            }

            _isProcessing = true;
            OnStatusChanged?.Invoke("Думаю...");

            try
            {
                OnUserSpoke?.Invoke(userText);
                _currentCts = new CancellationTokenSource();

                var context = new ConversationContext();
                var response = await _agent.ProcessInputAsync(userText, context);

                await ExecuteResponseAsync(response, _currentCts.Token);
            }
            catch (OperationCanceledException)
            {
                OnStatusChanged?.Invoke("Прервано");
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Ошибка: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
                _currentCts?.Dispose();
            }
        }

        private async Task ExecuteResponseAsync(AgentResponse response, CancellationToken ct)
        {
            if (_avatar != null && response.Emotion != Emotion.Neutral)
                await _avatar.SetExpressionAsync(response.Emotion);

            if (!string.IsNullOrEmpty(response.Animation) && _avatar != null)
                await _avatar.PlayAnimationAsync(response.Animation);

            if (!string.IsNullOrWhiteSpace(response.Text) && _tts != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_avatar != null)
                            await _avatar.SpeakAsync(response.Text);
                        else
                            await _tts.SpeakAsync(response.Text, ct);
                    }
                    catch (OperationCanceledException) { }
                }, ct);
            }

            OnAgentResponded?.Invoke(response);
            OnStatusChanged?.Invoke("Готово");
        }

        private void HandleAgentResponse(AgentResponse response)
        {
            OnAgentResponded?.Invoke(response);
        }

        public async Task StartListeningAsync()
        {
            if (_asr != null)
            {
                _asrHandler = (text) => _ = ProcessInputAsync(text);
                _asr.OnTextRecognized += _asrHandler;
                await _asr.StartAsync();
                OnStatusChanged?.Invoke("Слушаю...");
            }
            // 🔥 Если _asr == null — просто ничего не делаем, не падаем
        }

        public async Task StopListeningAsync()
        {
            if (_asr != null)
            {
                await _asr.StopAsync();

                if (_asrHandler != null)
                {
                    _asr.OnTextRecognized -= _asrHandler;
                    _asrHandler = null;
                }

                OnStatusChanged?.Invoke("Остановлено");
            }
        }

        public void Dispose()
        {
            _currentCts?.Cancel();
            _currentCts?.Dispose();
            _agent?.Dispose();
            _asr?.Dispose();
            _tts?.Dispose();
        }
    }
}