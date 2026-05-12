using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models.Tool;
using AiAssistantDesktop.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiAssistantDesktop.Modules.Plugins
{
    public class HelloWorldPlugin : IPlugin, ITool
    {
        public string Name => "HelloWorldPlugin";
        public string Description => "Пример плагина, который просто здоровается.";

        public string ToolName => "say_hello";
        public string ToolDescription => "Говорит 'Привет' и текущее время.";
        public Dictionary<string, string> ParametersSchema => new();

        public void Initialize(IServiceProvider serviceProvider)
        {
            // Регистрируем этот плагин как инструмент
            var registry = serviceProvider.GetService(typeof(ToolRegistry)) as ToolRegistry;
            registry?.Register(this);
        }

        public Task<ToolResult> ExecuteAsync(Dictionary<string, string> parameters)
        {
            return Task.FromResult(ToolResult.Ok($"Привет! Сейчас {DateTime.Now:HH:mm}. Плагин работает!"));
        }

        public void Dispose() { }
    }
}