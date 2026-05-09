using System.Collections.Generic;
using System.Threading.Tasks;
using AiAssistantDesktopDemo.Core.Models;

namespace AiAssistantDesktopDemo.Core.Interfaces
{
    public interface IAvatarController
    {
        bool IsConnected { get; }

        Task ConnectAsync(string unityUrl);
        Task SetExpressionAsync(Emotion emotion, float intensity = 1.0f);
        Task SetBlendshapesAsync(Dictionary<string, float> @params);
        Task PlayAnimationAsync(string animationName);
        Task SpeakAsync(string text, bool interruptCurrent = true);
        Task StopSpeakingAsync();
        Task SetLookAtAsync(float x, float y);
    }
}