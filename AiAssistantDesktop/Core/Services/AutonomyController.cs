using System;
using System.Linq;
using System.Threading;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Modules.Memory;

namespace AiAssistantDesktop.Core.Services
{
    public class AutonomyController : IDisposable
    {
        private Timer? _timer;
        private readonly ILLMProvider _llm;
        private readonly MemoryManager _memory;
        private readonly IASRService _asr;
        private readonly Random _rnd = new();
        private DateTime _lastInteraction = DateTime.Now;

        // ⚙️ Настройки автономности
        public int AutonomyChancePercent { get; set; } = 75; // Шанс срабатывания (0-100)
        public bool WorkEvenIfMicOff { get; set; } = true;   // true = игнорирует состояние микрофона
        public TimeSpan IdleThreshold { get; set; } = TimeSpan.FromSeconds(20);
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(20);

        public event Action<string>? OnAutonomousSpeechRequested;

        public AutonomyController(ILLMProvider llm, MemoryManager memory, IASRService asr)
        {
            _llm = llm;
            _memory = memory;
            _asr = asr;
        }

        public void Start()
        {
            _timer = new Timer(CheckIdle, null, CheckInterval, CheckInterval);
        }

        private async void CheckIdle(object? state)
        {
            // 1. Если микрофон выключен И мы не разрешили работу в этом режиме → выходим
            if (!WorkEvenIfMicOff && !_asr.IsListening) return;

            // 2. Проверка кулдауна (не спамим чаще чем каждые N сек)
            if (DateTime.Now - _lastInteraction < IdleThreshold) return;

            // 3. Вероятность срабатывания
            if (_rnd.Next(0, 100) >= AutonomyChancePercent) return;

            try
            {
                // 4. Один оптимизированный запрос вместо двух
                var context = string.Join("\n", _memory.BuildContext().TakeLast(3));
                var prompt = $"Контекст диалога:\n{context}\n\nПользователь молчит 20+ сек. " +
                             $"Либо напиши короткую живую фразу (1-2 предложения), чтобы начать разговор, " +
                             $"либо строго ответь НЕТ. Если решил говорить — не добавляй пояснений, только реплику.";

                var response = await _llm.GenerateAsync(prompt);

                // Если LLM не ответил "НЕТ" и выдал осмысленный текст → триггерим
                if (!response.Contains("НЕТ", StringComparison.OrdinalIgnoreCase) && response.Length > 4)
                {
                    _lastInteraction = DateTime.Now;
                    OnAutonomousSpeechRequested?.Invoke(response.Trim());
                }
            }
            catch
            {
                // Игнорируем ошибки фона (сеть, таймаут LLM и т.д.)
            }
        }

        public void ResetIdleTimer() => _lastInteraction = DateTime.Now;
        public void Dispose() => _timer?.Dispose();
    }
}