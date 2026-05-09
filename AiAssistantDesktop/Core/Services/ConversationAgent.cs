using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Events;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models;
using AiAssistantDesktop.Core.Services;
using AiAssistantDesktop.Modules.Memory;

namespace AiAssistantDesktop.Core.Services
{
    /// <summary>
    /// Агент, управляющий диалогом.
    /// Связывает ASR, LLM, TTS, Память и Автономность.
    /// </summary>
    public class ConversationAgent : IDisposable
    {
        private readonly IASRService _asr;
        private readonly ILLMProvider _llm;
        private readonly ITTSService _tts;
        private readonly IEventBus _eventBus;
        private readonly MemoryManager _memory;
        private readonly AutonomyController _autonomy;

        private bool _isThinking;
        private CancellationTokenSource? _cts;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public ConversationAgent(
            IASRService asr,
            ILLMProvider llm,
            ITTSService tts,
            IEventBus eventBus,
            MemoryManager memory,
            AutonomyController autonomy)
        {
            _asr = asr;
            _llm = llm;
            _tts = tts;
            _eventBus = eventBus;
            _memory = memory;
            _autonomy = autonomy;

            // Подписки на события
            _asr.OnTextRecognized += OnTextRecognized;
            _asr.OnError += OnAsrError;
            _tts.OnSpeakingFinished += OnSpeakingFinished;
            _autonomy.OnAutonomousSpeechRequested += OnTriggerAutonomous;

            Log("[Agent] ✅ Инициализация завершена. Агент готов к работе.");
        }

        private void OnAsrError(string error)
        {
            Log($"[Agent] ❌ ASR ошибка: {error}");
            _eventBus.Publish(new AgentErrorEvent($"ASR: {error}"));
        }

        private async void OnTextRecognized(string text)
        {
            if (!_lock.Wait(0))
            {
                Log($"[Agent] ⏳ Пропуск ввода (занято): \"{text}\"");
                return;
            }

            try
            {
                if (_isThinking)
                {
                    Log($"[Agent] ️ Игнорируем (думаю/говорю): \"{text}\"");
                    return;
                }

                _isThinking = true;
                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                Log($"[Agent] 👤 Ввод пользователя: \"{text}\"");
                Console.WriteLine($"👤 Пользователь: {text}");

                await _asr.StopAsync();
                _eventBus.Publish(new UserSpokeEvent(text));

                // Сохраняем реплику в краткосрочную память
                _memory.AddShortTerm("user", text);

                try
                {
                    _eventBus.Publish(new AgentThinkingEvent());
                    Log("[Agent] 🧠 Запрос к LLM...");

                    // Формируем промпт с контекстом
                    var context = _memory.BuildContext();
                    var contextText = string.Join("\n", context);
                    var prompt = $"Контекст диалога:\n{contextText}\n\nПользователь: {text}\nМишель:";

                    string response = await _llm.GenerateAsync(prompt);

                    Log($"[Agent] 🤖 LLM ответ: \"{response}\"");
                    Console.WriteLine($"🤖 Агент: {response}");

                    var emotion = ExtractEmotion(response);

                    // Сохраняем ответ в память
                    _memory.AddShortTerm("assistant", response);

                    _eventBus.Publish(new AgentRespondedEvent(response, emotion));

                    Log("[Agent] 🔊 Отправка в TTS...");
                    await _tts.SpeakAsync(response, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Log("[Agent] ⚠️ Операция отменена (перебивание)");
                }
                catch (Exception ex)
                {
                    Log($"[Agent] ❌ Ошибка в пайплайне: {ex.Message}");
                    Console.WriteLine($"❌ Ошибка: {ex.Message}");
                    _eventBus.Publish(new AgentErrorEvent(ex.Message));
                }
            }
            finally
            {
                _isThinking = false;
                await _asr.StartAsync();
                Log("[Agent] ✅ Цикл завершён, микрофон включён");
                _lock.Release();
            }
        }

        private async void OnTriggerAutonomous(string prompt)
        {
            await ProcessSpeechAsync(prompt, isUserInput: false);
        }

        private async Task ProcessSpeechAsync(string input, bool isUserInput)
        {
            if (!_lock.Wait(0)) return;

            try
            {
                _isThinking = true;
                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                await _asr.StopAsync();
                _eventBus.Publish(new AgentThinkingEvent());

                var context = _memory.BuildContext();
                var contextText = string.Join("\n", context);

                var finalPrompt = isUserInput
                    ? $"Контекст:\n{contextText}\n\nПользователь: {input}\nМишель:"
                    : $"Контекст:\n{contextText}\n\n{input}\nМишель:";

                string response = await _llm.GenerateAsync(finalPrompt);
                var emotion = ExtractEmotion(response);

                if (isUserInput) _memory.AddShortTerm("user", input);
                _memory.AddShortTerm("assistant", response);

                _eventBus.Publish(new AgentRespondedEvent(response, emotion));
                await _tts.SpeakAsync(response, _cts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log($"[Agent] ❌ Ошибка автономного ответа: {ex.Message}");
                _eventBus.Publish(new AgentErrorEvent(ex.Message));
            }
            finally
            {
                _isThinking = false;
                await _asr.StartAsync();
                _lock.Release();
            }
        }

        private void OnSpeakingFinished()
        {
            _autonomy.ResetIdleTimer();
        }

        private Emotion ExtractEmotion(string text)
        {
            if (text.Contains("!") || text.Contains("заебись") || text.Contains("круто") || text.Contains("🎉"))
                return Emotion.Excited;
            if (text.Contains("?"))
                return Emotion.Confused;
            if (text.Contains("бля") || text.Contains("херня") || text.Contains("пиздец") || text.Contains("😡"))
                return Emotion.Angry;
            if (text.Contains("😊") || text.Contains("рад") || text.Contains("счастлив"))
                return Emotion.Happy;
            if (text.Contains("😔") || text.Contains("груст") || text.Contains("устал"))
                return Emotion.Sad;

            return Emotion.Neutral;
        }

        public async Task StartAsync()
        {
            Log("[Agent] ▶️ StartAsync");
            await _asr.StartAsync();
        }

        public async Task StopListeningAsync()
        {
            Log("[Agent] ⏹ StopListeningAsync");
            await _asr.StopAsync();
        }

        public async Task StartListeningAsync()
        {
            Log("[Agent] ▶️ StartListeningAsync");
            await _asr.StartAsync();
        }

        private void Log(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        public void Dispose()
        {
            Log("[Agent] 🗑 Dispose");
            _cts?.Cancel();
            _cts?.Dispose();
            _lock?.Dispose();

            // Отписка от событий
            _asr.OnTextRecognized -= OnTextRecognized;
            _asr.OnError -= OnAsrError;
            _tts.OnSpeakingFinished -= OnSpeakingFinished;
            _autonomy.OnAutonomousSpeechRequested -= OnTriggerAutonomous;

            _autonomy.Dispose();
        }
    }
}