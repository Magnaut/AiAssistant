using System;
using System.Threading.Tasks;
using AiAssistantDesktopDemo.Core.Models;

namespace AiAssistantDesktopDemo.Core.Interfaces
{
    public interface IAgent : IDisposable
    {
        string Name { get; }
        bool IsReady { get; }

        event Action<AgentResponse>? OnResponseGenerated;
        event Action<string>? OnError;
        event Action? OnThinkingStarted;
        event Action? OnThinkingCompleted;

        Task InitializeAsync();
        Task<AgentResponse> ProcessInputAsync(string userText, ConversationContext context);
        Task InterruptAsync();
        Task SaveMemoryAsync();
        Task LoadMemoryAsync();
    }
}