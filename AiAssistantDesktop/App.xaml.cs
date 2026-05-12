using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AiAssistantDesktop.Core.Events;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Services;
using AiAssistantDesktop.Modules.ASR;
using AiAssistantDesktop.Modules.LLM;
using AiAssistantDesktop.Modules.TTS;
using AiAssistantDesktop.Modules.Tools; // 🔥 Фаза 2

namespace AiAssistantDesktop
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ConfigureServices();
            var agent = Services?.GetService<ConversationAgent>();
            agent?.StartAsync();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Core
            services.AddSingleton<IEventBus, SimpleEventBus>();
            services.AddSingleton<ContentFilter>();
            services.AddSingleton<SessionFileManager>();
            services.AddSingleton<ThoughtPrioritizer>();

            // 🔥 Фаза 2: Память и Инструменты
            services.AddSingleton<MemoryManager>();
            services.AddSingleton<ToolRegistry>();
            services.AddSingleton<ToolExecutor>();
            services.AddSingleton<PromptBuilder>();

            // Modules
            services.AddSingleton<ILLMProvider, OllamaLlmProvider>();
            services.AddSingleton<ITTSService, SystemTtsService>();
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vosk-model-small-ru");
            services.AddSingleton<IASRService>(sp => new VoskAsrService(modelPath));

            // Регистрация базовых инструментов
            services.AddSingleton<ToolRegistry>(sp =>
            {
                var registry = new ToolRegistry();
                registry.Register(new DateTimeTool());
                registry.Register(new CalculatorTool());
                registry.Register(new MockSearchTool());
                return registry;
            });

            // Agent
            services.AddSingleton<ConversationAgent>();

            Services = services.BuildServiceProvider();
        }
    }
}