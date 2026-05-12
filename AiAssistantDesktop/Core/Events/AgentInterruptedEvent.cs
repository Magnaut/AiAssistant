namespace AiAssistantDesktop.Core.Events
{
    /// <summary>
    /// Событие: агента прервали (пользователь перебил)
    /// </summary>
    public class AgentInterruptedEvent
    {
        public string? Reason { get; set; }
        public AgentInterruptedEvent(string? reason = null) => Reason = reason;
    }
}