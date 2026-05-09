namespace AiAssistantDesktopDemo.Core.Events
{
    /// <summary>
    /// Событие: текст успешно распознан модулем ASR
    /// </summary>
    public record TextRecognizedEvent(string Text, DateTime Timestamp);
}