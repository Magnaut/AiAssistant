namespace AiAssistantDesktop.Core.Models.Memory
{
    public enum MemoryLevel
    {
        ShortTerm,  // Последние 5-10 сообщений сессии
        MediumTerm, // Факты и предпочтения пользователя (хранятся днями)
        LongTerm    // Глубокие выводы, summary дней, постоянные знания
    }
}