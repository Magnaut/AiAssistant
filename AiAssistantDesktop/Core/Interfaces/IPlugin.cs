using System;
using AiAssistantDesktop.Core.Interfaces;

namespace AiAssistantDesktop.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для модульных плагинов.
    /// Плагин может предоставлять инструменты (Tools) для агента.
    /// </summary>
    public interface IPlugin : IDisposable
    {
        string Name { get; }
        string Description { get; }

        /// <summary>
        /// Вызывается при загрузке. Здесь можно зарегистрировать инструменты.
        /// </summary>
        void Initialize(IServiceProvider serviceProvider);

        /// <summary>
        /// Вызывается при выгрузке плагина.
        /// </summary>
        new void Dispose();
    }
}