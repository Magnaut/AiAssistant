using System;
using System.Linq;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Events;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models;

namespace AiAssistantDesktop.Core.Services
{
    /// <summary>
    /// Агент, управляющий диалогом с интеграцией фильтра, сессии и приоритезации
    /// </summary>
    public class ConversationAgent
    {
        private readonly IASRService _asr;
        private readonly ILLMProvider _llm;
        private readonly ITTSService _tts;
        private readonly IEventBus _eventBus;

        // 🔥 Новые компоненты Фазы 1
        private readonly ContentFilter _contentFilter;
        private readonly SessionFileManager _sessionManager;
        private readonly ThoughtPrioritizer _prioritizer;

        private bool _isThinking;
        private bool _isInitialized;

        public ConversationAgent(
            IASRService asr,
            ILLMProvider llm,
            ITTSService tts,
            IEventBus eventBus,
            ContentFilter? contentFilter = null,
            SessionFileManager? sessionManager = null,
            ThoughtPrioritizer? prioritizer = null)
        {
            _asr = asr;
            _llm = llm;
            _tts = tts;
            _eventBus = eventBus;

            // 🔥 Инициализация новых компонентов
            _contentFilter = contentFilter ?? new ContentFilter();
            _sessionManager = sessionManager ?? new SessionFileManager();
            _prioritizer = prioritizer ?? new ThoughtPrioritizer("Michelle");

            _asr.OnTextRecognized += OnTextRecognized;
            _isInitialized = true;
        }

        /// <summary>
        /// Обработчик распознанного текста от ASR
        /// </summary>
        private async void OnTextRecognized(string rawText)
        {
            if (_isThinking || !_isInitialized) return;

            // 🔥 1. Фильтрация входящего текста
            var filteredInput = _contentFilter.FilterInput(rawText);
            if (string.IsNullOrWhiteSpace(filteredInput))
            {
                _eventBus.Publish(new AgentErrorEvent("Входной текст отфильтрован"));
                return;
            }

            // 🔥 2. Приоритезация мысли
            var thought = _prioritizer.CreateThought(filteredInput, "user");

            // 🔥 3. Пропуск низкоприоритетных мыслей (опционально)
            if (thought.CanBeIgnored)
            {
                // Логируем, но не отвечаем
                _eventBus.Publish(new UserSpokeEvent($"[Low] {filteredInput}"));
                return;
            }

            _isThinking = true;
            await _asr.StopAsync();

            _eventBus.Publish(new UserSpokeEvent(filteredInput));

            try
            {
                _eventBus.Publish(new AgentThinkingEvent());

                // 🔥 4. Добавляем контекст сессии к промпту
                var sessionContext = _sessionManager.GetContextForLLM();
                var enhancedPrompt = string.IsNullOrWhiteSpace(sessionContext)
                    ? filteredInput
                    : $"{sessionContext}\n\nПользователь: {filteredInput}";

                // 🔥 5. Запрос к LLM
                string response = await _llm.GenerateAsync(enhancedPrompt);

                // 🔥 6. Фильтрация исходящего ответа
                var filteredResponse = _contentFilter.FilterOutput(response);

                // 🔥 7. Озвучка
                await _tts.SpeakAsync(filteredResponse);

                _eventBus.Publish(new AgentRespondedEvent(filteredResponse));
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

        /// <summary>
        /// Запуск агента
        /// </summary>
        public async Task StartAsync()
        {
            if (!_isInitialized) return;
            await _asr.StartAsync();
        }

        /// <summary>
        /// Остановка прослушивания
        /// </summary>
        public async Task StopListeningAsync()
        {
            await _asr.StopAsync();
        }

        /// <summary>
        /// Возобновление прослушивания
        /// </summary>
        public async Task StartListeningAsync()
        {
            await _asr.StartAsync();
        }

        // 🔥 Публичные методы для управления новыми компонентами

        /// <summary>
        /// Добавляет файл в сессию
        /// </summary>
        public bool AddSessionFile(string fileName, string content, string contentType = "text/plain")
        {
            return _sessionManager.AddFile(fileName, content, contentType);
        }

        /// <summary>
        /// Ищет файлы по ключевому слову
        /// </summary>
        public SessionFile[] SearchSessionFiles(string keyword)
        {
            return _sessionManager.SearchByKeyword(keyword).ToArray();
        }

        /// <summary>
        /// Очищает все файлы сессии
        /// </summary>
        public void ClearSessionFiles()
        {
            _sessionManager.ClearAll();
        }

        /// <summary>
        /// Добавляет слово в чёрный список фильтра
        /// </summary>
        public void AddBlockedWord(string word)
        {
            _contentFilter.AddBlockedWord(word);
        }

        /// <summary>
        /// Добавляет ключевое слово для критического приоритета
        /// </summary>
        public void AddCriticalKeyword(string keyword)
        {
            _prioritizer.AddCriticalKeyword(keyword);
        }

        /// <summary>
        /// Получает статус компонентов
        /// </summary>
        public AgentStatus GetStatus()
        {
            return new AgentStatus
            {
                IsListening = _asr.IsListening,
                IsThinking = _isThinking,
                SessionFilesCount = _sessionManager.GetFileNames().Count(),
                IsInitialized = _isInitialized
            };
        }
    }

    /// <summary>
    /// Статус агента для мониторинга
    /// </summary>
    public class AgentStatus
    {
        public bool IsListening { get; set; }
        public bool IsThinking { get; set; }
        public int SessionFilesCount { get; set; }
        public bool IsInitialized { get; set; }
    }

    // ========================================
    // 🔥 КЛАССЫ СОБЫТИЙ (обязательно!)
    // ========================================

    /// <summary>
    /// Пользователь сказал что-то
    /// </summary>
    public class UserSpokeEvent
    {
        public string Text { get; }
        public UserSpokeEvent(string text) => Text = text;
    }

    /// <summary>
    /// Агент начал думать
    /// </summary>
    public class AgentThinkingEvent
    {
    }

    /// <summary>
    /// Агент ответил
    /// </summary>
    public class AgentRespondedEvent
    {
        public string Text { get; }
        public AgentRespondedEvent(string text) => Text = text;
    }

    /// <summary>
    /// Произошла ошибка
    /// </summary>
    public class AgentErrorEvent
    {
        public string Message { get; }
        public AgentErrorEvent(string message) => Message = message;
    }
}