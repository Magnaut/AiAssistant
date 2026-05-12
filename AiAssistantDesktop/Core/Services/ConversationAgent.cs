using System;
using System.Linq;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Events;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models;
using AiAssistantDesktop.Core.Models.Memory;

namespace AiAssistantDesktop.Core.Services
{
    public class ConversationAgent
    {
        private readonly IASRService _asr;
        private readonly ILLMProvider _llm;
        private readonly ITTSService _tts;
        private readonly IEventBus _eventBus;

        private readonly ContentFilter _contentFilter;
        private readonly SessionFileManager _sessionManager;
        private readonly ThoughtPrioritizer _prioritizer;
        private readonly MemoryManager _memoryManager;
        private readonly PromptBuilder _promptBuilder;
        private readonly ToolExecutor _toolExecutor;
        private readonly CognitiveLoop _cognitiveLoop;
        private readonly ProactivityManager _proactivity;

        private bool _isThinking;
        private bool _isInitialized;

        public ConversationAgent(
            IASRService asr,
            ILLMProvider llm,
            ITTSService tts,
            IEventBus eventBus,
            ContentFilter contentFilter,
            SessionFileManager sessionManager,
            ThoughtPrioritizer prioritizer,
            MemoryManager memoryManager,
            PromptBuilder promptBuilder,
            ToolExecutor toolExecutor,
            CognitiveLoop cognitiveLoop,
            ProactivityManager proactivity)
        {
            _asr = asr;
            _llm = llm;
            _tts = tts;
            _eventBus = eventBus;
            _contentFilter = contentFilter;
            _sessionManager = sessionManager;
            _prioritizer = prioritizer;
            _memoryManager = memoryManager;
            _promptBuilder = promptBuilder;
            _toolExecutor = toolExecutor;
            _cognitiveLoop = cognitiveLoop;
            _proactivity = proactivity;

            _asr.OnTextRecognized += OnTextRecognized;
            _eventBus.Subscribe<ProactiveSpeechEvent>(OnProactiveSpeech);

            _isInitialized = true;
        }

        private async void OnTextRecognized(string rawText)
        {
            if (_isThinking || !_isInitialized) return;

            var filteredInput = _contentFilter.FilterInput(rawText);
            if (string.IsNullOrWhiteSpace(filteredInput)) return;

            var thought = _prioritizer.CreateThought(filteredInput, "user");
            if (thought.CanBeIgnored)
            {
                _eventBus.Publish(new UserSpokeEvent($"[Low] {filteredInput}"));
                return;
            }

            await ProcessInputAsync(filteredInput, "user");
        }

        private async void OnProactiveSpeech(ProactiveSpeechEvent e)
        {
            if (_isThinking || !_isInitialized) return;
            await ProcessInputAsync(e.Text, "proactive");
        }

        private async Task ProcessInputAsync(string input, string source)
        {
            _isThinking = true;
            if (source == "user") await _asr.StopAsync();

            _eventBus.Publish(new UserSpokeEvent(source == "proactive" ? $"🧠 Michelle: {input}" : input));
            _proactivity.RecordInteraction();

            try
            {
                _eventBus.Publish(new AgentThinkingEvent());

                var userPrompt = _promptBuilder.BuildUserPrompt(input);
                string response = await _llm.GenerateAsync(userPrompt);
                response = _contentFilter.FilterOutput(response);

                var (finalResponse, usedTool) = await _toolExecutor.TryExecuteToolAsync(response);
                if (usedTool)
                {
                    _eventBus.Publish(new AgentThinkingEvent());
                    var toolPrompt = $"Результат: {finalResponse}. Ответь пользователю.";
                    response = await _llm.GenerateAsync(toolPrompt);
                    response = _contentFilter.FilterOutput(response);
                }

                _memoryManager.ExtractFacts(input, response);
                _memoryManager.Add(new MemoryEntry(response, MemoryLevel.ShortTerm, "agent", TimeSpan.FromMinutes(30)));

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
                if (source == "user") await _asr.StartAsync();
            }
        }

        public async Task StartAsync()
        {
            if (!_isInitialized) return;
            await _asr.StartAsync();
            _cognitiveLoop.Start();
        }

        public async Task StopListeningAsync() { await _asr.StopAsync(); }
        public async Task StartListeningAsync() { await _asr.StartAsync(); }

        public void ToggleProactivity()
        {
            // Простая проверка через Reflection или можно вынести статус в интерфейс. 
            // Для краткости используем try/catch обёртку или оставим как есть, так как цикл сам проверяет _isAgentBusy
        }

        public bool AddSessionFile(string fileName, string content, string contentType = "text/plain") => _sessionManager.AddFile(fileName, content, contentType);
        public SessionFile[] SearchSessionFiles(string keyword) => _sessionManager.SearchByKeyword(keyword).ToArray();
        public void ClearSessionFiles() => _sessionManager.ClearAll();
        public void AddBlockedWord(string word) => _contentFilter.AddBlockedWord(word);
        public void AddCriticalKeyword(string keyword) => _prioritizer.AddCriticalKeyword(keyword);

        public AgentStatus GetStatus() => new()
        {
            IsListening = _asr.IsListening,
            IsThinking = _isThinking,
            SessionFilesCount = _sessionManager.GetFileNames().Count(),
            IsInitialized = _isInitialized
        };
    }

    public class AgentStatus
    {
        public bool IsListening { get; set; }
        public bool IsThinking { get; set; }
        public int SessionFilesCount { get; set; }
        public bool IsInitialized { get; set; }
    }

    public class UserSpokeEvent { public string Text { get; } public UserSpokeEvent(string text) => Text = text; }
    public class AgentThinkingEvent { }
    public class AgentRespondedEvent { public string Text { get; } public AgentRespondedEvent(string text) => Text = text; }
    public class AgentErrorEvent { public string Message { get; } public AgentErrorEvent(string message) => Message = message; }
}