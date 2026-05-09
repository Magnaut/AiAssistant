using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using AiAssistantDesktop.Core.Interfaces;
using NAudio.Wave;
using Vosk;

namespace AiAssistantDesktop.Modules.ASR
{
    public class VoskAsrService : IASRService
    {
        private Model? _model;
        private VoskRecognizer? _recognizer;
        private WaveInEvent? _waveIn;
        private bool _isListening;

        public bool IsListening => _isListening;
        public event Action<string>? OnTextRecognized;
        public event Action<string>? OnError;

        public VoskAsrService(string modelPath)
        {
            Log($"[Vosk] Инициализация, путь: {modelPath}");

            if (!System.IO.Directory.Exists(modelPath))
            {
                var msg = $"Model path not found: {modelPath}";
                Log($"[Vosk] ❌ Ошибка: {msg}");
                throw new System.IO.DirectoryNotFoundException(msg);
            }

            try
            {
                _model = new Model(modelPath);
                Log("[Vosk] ✅ Модель загружена");

                _recognizer = new VoskRecognizer(_model, 16000f);
                Log("[Vosk] ✅ Распознаватель создан");

                _waveIn = new WaveInEvent
                {
                    DeviceNumber = 0,
                    WaveFormat = new WaveFormat(16000, 1),
                    BufferMilliseconds = 100
                };

                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.RecordingStopped += (s, e) => Log("[Vosk] ⏹ Запись остановлена");

                Log($"[Vosk] ✅ WaveIn настроен (устройство #{_waveIn.DeviceNumber})");
            }
            catch (Exception ex)
            {
                Log($"[Vosk] ❌ Критическая ошибка инициализации: {ex.Message}");
                OnError?.Invoke($"Vosk init error: {ex.Message}");
                throw;
            }
        }

        public Task StartAsync()
        {
            try
            {
                Log("[Vosk] ▶️ StartAsync вызван");
                _waveIn?.StartRecording();
                _isListening = true;
                Log("[Vosk] ✅ Микрофон запущен");
                return Task.CompletedTask;
            }
            catch (InvalidOperationException ex)
            {
                Log($"[Vosk] ⚠️ Микрофон уже запущен или занят: {ex.Message}");
                _isListening = true;
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log($"[Vosk] ❌ Ошибка StartAsync: {ex.Message}");
                OnError?.Invoke($"Start error: {ex.Message}");
                return Task.FromException(ex);
            }
        }

        public Task StopAsync()
        {
            try
            {
                Log("[Vosk] ⏹ StopAsync вызван");
                _waveIn?.StopRecording();
                _isListening = false;
                Log("[Vosk] ✅ Микрофон остановлен");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log($"[Vosk] ⚠️ Ошибка StopAsync (игнорируется): {ex.Message}");
                return Task.CompletedTask;
            }
        }

        public Task<bool> IsModelLoadedAsync() => Task.FromResult(_model != null);

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_recognizer == null) return;

            if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                string result = _recognizer.Result();
                Log($"[Vosk] 📦 Raw JSON: {result}");

                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(result))
                    {
                        if (doc.RootElement.TryGetProperty("text", out JsonElement textElement))
                        {
                            string text = textElement.GetString()?.Trim() ?? "";

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                Log($"[Vosk] 🎤 РАСПОЗНАНО: \"{text}\"");
                                Console.WriteLine($"🎤 VOICE: {text}");
                                OnTextRecognized?.Invoke(text);
                            }
                            else
                            {
                                Log("[Vosk] ⚪ Пустой текст, игнорируем");
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Log($"[Vosk] ❌ JSON parse error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Log($"[Vosk] ❌ Ошибка обработки: {ex.Message}");
                }
            }
        }

        private void Log(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        public void Dispose()
        {
            Log("[Vosk] 🗑 Dispose вызван");
            StopAsync().Wait();
            _waveIn?.Dispose();
            _recognizer?.Dispose();
            _model?.Dispose();
            Log("[Vosk] ✅ Ресурсы освобождены");
        }
    }
}