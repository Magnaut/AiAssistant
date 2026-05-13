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
using AiAssistantDesktop.Modules.Tools;

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

            services.AddSingleton<IEventBus, SimpleEventBus>();
            services.AddSingleton<ContentFilter>();
            services.AddSingleton<SessionFileManager>();
            services.AddSingleton<ThoughtPrioritizer>();
            services.AddSingleton<MemoryManager>();
            services.AddSingleton<ToolRegistry>();
            services.AddSingleton<ToolExecutor>();
            services.AddSingleton<PromptBuilder>();
            services.AddSingleton<ProactivityManager>();

            services.AddSingleton<CognitiveLoop>(sp =>
            {
                var llm = sp.GetRequiredService<ILLMProvider>();
                var mem = sp.GetRequiredService<MemoryManager>();
                var proact = sp.GetRequiredService<ProactivityManager>();
                var bus = sp.GetRequiredService<IEventBus>();
                return new CognitiveLoop(llm, mem, proact, bus, () => false);
            });

            services.AddSingleton<ILLMProvider, OllamaLlmProvider>();
            services.AddSingleton<ITTSService, SystemTtsService>();
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vosk-model-small-ru");
            services.AddSingleton<IASRService>(sp => new VoskAsrService(modelPath));

            services.AddSingleton<ToolRegistry>(sp =>
            {
                var registry = new ToolRegistry();
                registry.Register(new DateTimeTool());
                registry.Register(new CalculatorTool());
                registry.Register(new MockSearchTool());

                // 🔥 Новый инструмент: скриншот
                var vision = sp.GetRequiredService<IVisionService>();
                registry.Register(new ScreenshotTool(vision));

                return registry;
            });

            // 🔥 Vision Service
            services.AddSingleton<IVisionService, VisionService>();

            services.AddSingleton<ConversationAgent>();
            Services = services.BuildServiceProvider();

            // Загрузка плагинов
            var pluginLoader = new PluginLoader(Services);
            pluginLoader.LoadAllPlugins();
        }
    }
}