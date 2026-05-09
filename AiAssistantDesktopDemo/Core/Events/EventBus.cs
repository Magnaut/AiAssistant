using System;
using System.Collections.Generic;

namespace AiAssistantDesktopDemo.Core.Events
{
    /// <summary>
    /// Интерфейс шины событий
    /// </summary>
    public interface IEventBus
    {
        void Publish<T>(T @event) where T : class;
        void Subscribe<T>(Action<T> handler) where T : class;
        void Unsubscribe<T>(Action<T> handler) where T : class;
    }

    /// <summary>
    /// Реализация шины событий (потокобезопасная)
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private readonly object _lock = new();

        public void Publish<T>(T @event) where T : class
        {
            if (@event == null) return;

            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(T), out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        try
                        {
                            // Вызываем обработчик события
                            handler.DynamicInvoke(@event);
                        }
                        catch (Exception ex)
                        {
                            // В будущем здесь будет логгер
                            Console.WriteLine($"[EventBus Error] {ex.Message}");
                        }
                    }
                }
            }
        }

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            lock (_lock)
            {
                if (!_handlers.ContainsKey(typeof(T)))
                    _handlers[typeof(T)] = new List<Delegate>();

                _handlers[typeof(T)].Add(handler);
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(T), out var handlers))
                    handlers.Remove(handler);
            }
        }
    }
}