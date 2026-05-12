namespace AiAssistantDesktop.Core.Events
{
    /// <summary>
    /// Событие: агента прервали (пользователь перебил)
    /// </summary>
    public class AgentInterruptedEvent
    {
        public string? InterruptReason { get; set; }
        public AgentInterruptedEvent(string? reason = null) => InterruptReason = reason;
    }
}