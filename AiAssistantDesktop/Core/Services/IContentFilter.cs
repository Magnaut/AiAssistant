using System;
using System.Collections.Generic;

namespace AiAssistantDesktop.Core.Services
{
    /// <summary>
    /// Интерфейс фильтра контента
    /// </summary>
    public interface IContentFilter
    {
        string FilterInput(string input);
        string FilterOutput(string output);
        bool IsBlocked(string text);
        void AddBlockedWord(string word);
        void LoadBlockedWords(IEnumerable<string> words);
    }
}