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

namespace AiAssistantDesktop
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ConfigureServices();

            // Запускаем агента
            var agent = Services?.GetService<ConversationAgent>();
            agent?.StartAsync();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // 1. Event Bus
            services.AddSingleton<IEventBus, SimpleEventBus>();

            // 🔥 2. Новые сервисы Фазы 1
            services.AddSingleton<ContentFilter>();
            services.AddSingleton<SessionFileManager>();
            services.AddSingleton<ThoughtPrioritizer>();

            // 3. Модули
            services.AddSingleton<ILLMProvider, OllamaLlmProvider>();
            services.AddSingleton<ITTSService, SystemTtsService>();

            // ASR - Vosk
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vosk-model-small-ru");
            services.AddSingleton<IASRService>(sp => new VoskAsrService(modelPath));

            // 4. Агент (с инъекцией новых зависимостей)
            services.AddSingleton<ConversationAgent>();

            Services = services.BuildServiceProvider();
        }
    }
}