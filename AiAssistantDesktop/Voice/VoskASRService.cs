
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AiAssistantDesktopDemo.Core.Interfaces;

namespace AiAssistantDesktopDemo.Voice
{
    public class VoskASRService : IASRService
    {
        private readonly VoskWrapper _vosk;
        private bool _isListening;

        public bool IsListening => _isListening;
        public event Action<string>? OnTextRecognized;
        public event Action<string>? OnError;

        public VoskASRService(string modelPath)
        {
            Debug.WriteLine($"[ASR] Создание: {modelPath}");
            _vosk = new VoskWrapper(modelPath);
            _vosk.OnTextRecognized += text => OnTextRecognized?.Invoke(text);
        }

        public Task StartAsync()
        {
            Debug.WriteLine("[ASR] Запуск");
            _vosk.Start();
            _isListening = true;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Debug.WriteLine("[ASR] Остановка");
            _vosk.Stop();
            _isListening = false;
            return Task.CompletedTask;
        }

        public Task<bool> IsModelLoadedAsync() => Task.FromResult(true);
        public void Dispose() => _vosk.Dispose();
    }
}
