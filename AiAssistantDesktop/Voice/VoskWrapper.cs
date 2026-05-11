using System;
using System.Diagnostics;
using System.Text.Json;
using NAudio.Wave;
using Vosk;
using AiAssistantDesktopDemo.Core.Events;

namespace AiAssistantDesktopDemo.Voice
{
    public class VoskWrapper : IDisposable
    {
        private readonly Model _model;
        private readonly VoskRecognizer _recognizer;
        private readonly WaveInEvent _waveIn;
        private readonly IEventBus _eventBus;
        private bool _isRecording = false;

        public VoskWrapper(string modelPath, IEventBus eventBus)
        {
            _eventBus = eventBus;
            Debug.WriteLine($"[Vosk] Загрузка модели: {modelPath}");

            _model = new Model(modelPath);
            _recognizer = new VoskRecognizer(_model, 16000f);

            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(16000, 1),
                BufferMilliseconds = 100
            };

            _waveIn.DataAvailable += WaveIn_DataAvailable;
            Debug.WriteLine("[Vosk] ✅ Готов");
        }

        public void Start()
        {
            if (_isRecording) return;
            try
            {
                _waveIn.StartRecording();
                _isRecording = true;
                Debug.WriteLine("[Vosk] 🎤 Запись началась");
            }
            catch (InvalidOperationException)
            {
                _isRecording = true;
            }
        }

        public void Stop()
        {
            if (!_isRecording) return;
            try
            {
                _waveIn.StopRecording();
                _isRecording = false;
                Debug.WriteLine("[Vosk] 🛑 Запись остановлена");
            }
            catch { }
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                string result = _recognizer.Result();
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(result))
                    {
                        if (doc.RootElement.TryGetProperty("text", out JsonElement textElement))
                        {
                            string text = textElement.GetString()?.Trim() ?? "";
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                Debug.WriteLine($"[Vosk] 🎯 Распознано: {text}");
                                // 🔥 Публикуем событие в шину
                                _eventBus.Publish(new TextRecognizedEvent(text, DateTime.Now));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Vosk] ⚠️ Ошибка парсинга: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _waveIn?.Dispose();
            _recognizer?.Dispose();
            _model?.Dispose();
        }
    }
}