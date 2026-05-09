using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AiAssistantDesktopDemo.Core.Interfaces;
using AiAssistantDesktopDemo.Core.Models;

namespace AiAssistantDesktopDemo.Agents
{
    public abstract class BaseAgent : IAgent
    {
        protected readonly ILLMProvider _llmProvider;
        protected ConversationContext _context;

        public string Name { get; set; } = "Assistant";
        public virtual bool IsReady => _llmProvider?.IsReady == true;

        public event Action<AgentResponse>? OnResponseGenerated;
        public event Action<string>? OnError;
        public event Action? OnThinkingStarted;
        public event Action? OnThinkingCompleted;

        protected BaseAgent(ILLMProvider llmProvider, ConversationContext? context = null)
        {
            _llmProvider = llmProvider;
            _context = context ?? new ConversationContext();
        }

        public virtual Task InitializeAsync() => _llmProvider.InitializeAsync();

        public virtual async Task<AgentResponse> ProcessInputAsync(
            string userText, ConversationContext context)
        {
            try
            {
                OnThinkingStarted?.Invoke();

                context.AddMessage("user", userText);
                var prompt = context.ExportForPrompt("qwen");

                var rawResponse = await _llmProvider.GenerateAsync(prompt);
                var response = ParseAgentResponse(rawResponse, userText);

                context.AddMessage("assistant", response.Text);

                OnThinkingCompleted?.Invoke();
                OnResponseGenerated?.Invoke(response);

                return response;
            }
            catch (Exception ex)
            {
                RaiseError($"Ошибка агента: {ex.Message}");
                return new AgentResponse { Text = $"Ошибка: {ex.Message}" };
            }
        }

        protected virtual AgentResponse ParseAgentResponse(string rawText, string userPrompt)
        {
            var response = new AgentResponse { Text = rawText };

            var emotionMatch = Regex.Match(rawText, @"\[EMOTION:(\w+)\]");
            if (emotionMatch.Success && Enum.TryParse<Emotion>(emotionMatch.Groups[1].Value, true, out var emotion))
            {
                response.Emotion = emotion;
                response.ExpressionParams = emotion.ToBlendshapes();
                response.Text = Regex.Replace(rawText, @"\[EMOTION:\w+\]", "").Trim();
            }

            var animMatch = Regex.Match(rawText, @"\[ANIM:(\w+)\]");
            if (animMatch.Success)
            {
                response.Animation = animMatch.Groups[1].Value;
                response.Text = Regex.Replace(response.Text, @"\[ANIM:\w+\]", "").Trim();
            }

            var thoughtMatch = Regex.Match(rawText, @"\(\((.*?)\)\)");
            if (thoughtMatch.Success)
            {
                response.Thought = thoughtMatch.Groups[1].Value;
                response.Text = Regex.Replace(response.Text, @"\(\(.*?\)\)", "").Trim();
            }

            if (response.Emotion == Emotion.Neutral)
            {
                response.Emotion = DetectEmotionFromText(response.Text);
                response.ExpressionParams = response.Emotion.ToBlendshapes();
            }

            return response;
        }

        protected virtual Emotion DetectEmotionFromText(string text)
        {
            var lower = text.ToLower();

            if (lower.Contains("рад") || lower.Contains("счастлив") || lower.Contains("отлично") || lower.Contains("привет"))
                return Emotion.Happy;
            if (lower.Contains("груст") || lower.Contains("печаль") || lower.Contains("жаль"))
                return Emotion.Sad;
            if (lower.Contains("зл") || lower.Contains("раздраж") || lower.Contains("бесит"))
                return Emotion.Angry;
            if (lower.Contains("удив") || lower.Contains("вау") || lower.Contains("неожидан"))
                return Emotion.Surprised;
            if (lower.Contains("дума") || lower.Contains("хм") || lower.Contains("интересно"))
                return Emotion.Thinking;

            return Emotion.Neutral;
        }

        protected void RaiseError(string message)
        {
            OnError?.Invoke(message);
        }

        public virtual Task InterruptAsync() => Task.CompletedTask;
        public virtual Task SaveMemoryAsync() => Task.CompletedTask;
        public virtual Task LoadMemoryAsync() => Task.CompletedTask;

        public virtual void Dispose() => _llmProvider?.Dispose();
    }
}