using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AiAssistantDesktop.Core.Events;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Services;
using AiAssistantDesktop.Modules.ASR;
using AiAssistantDesktop.Modules.LLM;
using AiAssistantDesktop.Modules.Memory;
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
            var agent = Services?.GetRequiredService<ConversationAgent>();
            agent?.StartAsync();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // 1. Event Bus
            services.AddSingleton<IEventBus, SimpleEventBus>();

            // 2. Memory Manager
            services.AddSingleton<MemoryManager>();

            // 3. Autonomy Controller (теперь требует IASRService для проверки состояния)
            services.AddSingleton<AutonomyController>(sp => new AutonomyController(
                sp.GetRequiredService<ILLMProvider>(),
                sp.GetRequiredService<MemoryManager>(),
                sp.GetRequiredService<IASRService>()
            ));

            // 4. ASR - Vosk
            string modelPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Models",
                "vosk-model-small-ru"
            );
            services.AddSingleton<IASRService>(sp => new VoskAsrService(modelPath));

            // 5. TTS - System Speech
            services.AddSingleton<ITTSService, SystemTtsService>();

            // 6. LLM - Ollama с Qwen2.5:0.5b
            services.AddSingleton<ILLMProvider>(sp =>
                new OllamaLlmProvider(sp.GetRequiredService<MemoryManager>()));

            // 7. Agent (Orchestrator)
            services.AddSingleton<ConversationAgent>();

            Services = services.BuildServiceProvider();

            // Запуск автономности с кастомными настройками
            var autonomy = Services.GetRequiredService<AutonomyController>();
            autonomy.AutonomyChancePercent = 75; // 75% шанс
            autonomy.WorkEvenIfMicOff = true;    // Работает даже при выключенном микрофоне
            autonomy.Start();
        }
    }
}