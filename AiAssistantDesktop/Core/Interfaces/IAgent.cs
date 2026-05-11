using System;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Models;

namespace AiAssistantDesktop.Core.Interfaces
{
    public interface IAgent : IDisposable
    {
        string Name { get; }
        bool IsReady { get; }

        Task InitializeAsync();
        Task<AgentResponse> ProcessInputAsync(string input, AgentContext context);
        Task InterruptAsync();
    }
}