using System;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Models;

namespace AiAssistantDesktop.Core.Interfaces
{
    /// <summary>
    /// Интерфейс контроллера аватара
    /// </summary>
    public interface IAvatarController : IDisposable
    {
        bool IsConnected { get; }

        Task ConnectAsync(string endpoint);
        Task DisconnectAsync();
        Task SetExpressionAsync(Emotion emotion);
        Task PlayAnimationAsync(string animationName);
        Task SpeakAsync(string text);
        Task StopSpeakingAsync();
    }
}