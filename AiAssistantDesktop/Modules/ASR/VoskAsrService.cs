using System;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Interfaces;
using NAudio.Wave;
using Vosk;
using System.Text.Json;

namespace AiAssistantDesktop.Modules.ASR
{
    public class VoskAsrService : IASRService, IDisposable
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
            if (!System.IO.Directory.Exists(modelPath))
                throw new System.IO.DirectoryNotFoundException($"Model path not found: {modelPath}");

            _model = new Model(modelPath);
            _recognizer = new VoskRecognizer(_model, 16000f);

            _waveIn = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(16000, 1),
                BufferMilliseconds = 100
            };

            _waveIn.DataAvailable += WaveIn_DataAvailable;
        }

        public Task StartAsync()
        {
            try
            {
                _waveIn?.StartRecording();
                _isListening = true;
                return Task.CompletedTask;
            }
            catch (InvalidOperationException)
            {
                _isListening = true;
                return Task.CompletedTask;
            }
        }

        public Task StopAsync()
        {
            try
            {
                _waveIn?.StopRecording();
                _isListening = false;
                return Task.CompletedTask;
            }
            catch { return Task.CompletedTask; }
        }

        public Task<bool> IsModelLoadedAsync() => Task.FromResult(_model != null);

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_recognizer == null) return;

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
                                OnTextRecognized?.Invoke(text);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        public void Dispose()
        {
            StopAsync().Wait();
            _waveIn?.Dispose();
            _recognizer?.Dispose();
            _model?.Dispose();
        }
    }
}