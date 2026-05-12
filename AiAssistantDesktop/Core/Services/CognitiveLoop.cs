using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Events;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models;
using AiAssistantDesktop.Core.Models.Memory;

namespace AiAssistantDesktop.Core.Services
{
    public class CognitiveLoop : IDisposable
    {
        private readonly ILLMProvider _llm;
        private readonly MemoryManager _memory;
        private readonly ProactivityManager _proactivity;
        private readonly IEventBus _eventBus;
        private readonly CancellationTokenSource _cts;
        private readonly Func<bool> _isAgentBusy;
        private Task? _loopTask;
        private bool _isRunning;

        // 🔥 Защита от повторов
        private readonly Queue<string> _recentThoughts = new();
        private const int MaxRecentThoughts = 5;
        private readonly Random _random = new();

        public CognitiveLoop(ILLMProvider llm, MemoryManager memory, ProactivityManager proactivity, IEventBus eventBus, Func<bool> isAgentBusy)
        {
            _llm = llm;
            _memory = memory;
            _proactivity = proactivity;
            _eventBus = eventBus;
            _isAgentBusy = isAgentBusy;
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
            _eventBus.Publish(new CognitiveLoopStartedEvent());
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _cts.Cancel();
            _isRunning = false;
            _eventBus.Publish(new CognitiveLoopStoppedEvent());
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // 🔥 Не думаем, если агент занят диалогом с пользователем
                    if (_isAgentBusy())
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), ct);
                        continue;
                    }

                    if (!_proactivity.ShouldTriggerThought())
                    {
                        await Task.Delay(TimeSpan.FromMinutes(2), ct);
                        continue;
                    }

                    // 2. Генерируем мысль с защитой от повторов
                    var thought = await GenerateInternalThoughtAsync(ct);

                    if (string.IsNullOrWhiteSpace(thought.Content) || _recentThoughts.Contains(thought.Content))
                    {
                        await Task.Delay(TimeSpan.FromMinutes(3), ct);
                        continue;
                    }

                    // Сохраняем в историю повторов
                    _recentThoughts.Enqueue(thought.Content);
                    if (_recentThoughts.Count > MaxRecentThoughts) _recentThoughts.Dequeue();

                    // 3. Обрабатываем
                    if (thought.ShouldSpeak)
                    {
                        _eventBus.Publish(new ProactiveSpeechEvent(thought.Content));
                    }
                    else
                    {
                        _eventBus.Publish(new InternalThoughtEvent(thought));
                        _memory.Add(new MemoryEntry(thought.Content, MemoryLevel.LongTerm, "cognitive_loop", TimeSpan.FromDays(7)));
                    }

                    // Пауза 4-9 мин (рандом для естественности)
                    var delay = TimeSpan.FromMinutes(_random.Next(4, 9));
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _eventBus.Publish(new AgentErrorEvent($"🧠 Цикл: {ex.Message}"));
                    await Task.Delay(TimeSpan.FromMinutes(2), ct);
                }
            }
        }

        private async Task<InternalThought> GenerateInternalThoughtAsync(CancellationToken ct)
        {
            var context = _proactivity.GetProactiveContext();
            var timeOfDay = DateTime.Now.Hour switch
            {
                < 6 => "ночь",
                < 12 => "утро",
                < 18 => "день",
                _ => "вечер"
            };

            var prompt = $@"Ты Michelle. Сейчас {timeOfDay}. Ты думаешь в фоне.
Контекст: {context}
Сгенерируй ОДНУ короткую внутреннюю мысль или наблюдение. 
Никогда не повторяй предыдущие мысли. Будь естественной, на русском.
Если мысль требует внимания пользователя, начни с [SPEAK]. Иначе просто мысль.";

            // 🔥 Используем специфичные параметры для фоновых задач
            var response = await _llm.GenerateAsync(prompt, new Dictionary<string, object>
            {
                { "temperature", 0.85f },
                { "repeat_penalty", 1.2f },
                { "num_predict", 64 }
            });

            var shouldSpeak = response.Contains("[SPEAK]");
            var cleanContent = response.Replace("[SPEAK]", "").Trim();

            return new InternalThought
            {
                Content = cleanContent,
                Type = shouldSpeak ? ThoughtType.Proactive : ThoughtType.Background,
                ShouldSpeak = shouldSpeak,
                Priority = shouldSpeak ? 0.9f : 0.4f
            };
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }

    // События
    public class CognitiveLoopStartedEvent { }
    public class CognitiveLoopStoppedEvent { }
    public class InternalThoughtEvent { public InternalThought Thought { get; } public InternalThoughtEvent(InternalThought t) => Thought = t; }
    public class ProactiveSpeechEvent { public string Text { get; } public ProactiveSpeechEvent(string t) => Text = t; }
}