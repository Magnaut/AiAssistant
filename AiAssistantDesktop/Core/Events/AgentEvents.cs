using AiAssistantDesktop.Core.Models;

namespace AiAssistantDesktop.Core.Events
{
    public class UserSpokeEvent
    {
        public string Text { get; }
        public UserSpokeEvent(string text) => Text = text;
    }

    public class AgentThinkingEvent { }

    public class AgentRespondedEvent
    {
        public string Text { get; }
        public Emotion Emotion { get; }
        public AgentRespondedEvent(string text, Emotion emotion) => (Text, Emotion) = (text, emotion);
    }

    public class AgentErrorEvent
    {
        public string Message { get; }
        public AgentErrorEvent(string message) => Message = message;
    }
}