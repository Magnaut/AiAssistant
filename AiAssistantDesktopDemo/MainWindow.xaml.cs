using AiAssistantDesktopDemo.Core.Events;
using AiAssistantDesktopDemo.Voice;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Speech.Synthesis;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AiAssistantDesktopDemo
{
    public partial class MainWindow : Window
    {
        private VoskWrapper? vosk;
        private SpeechSynthesizer? synthesizer;
        private OllamaService? aiService;
        private bool isListening = false;
        private static readonly HttpClient httpClient = new();
        private const string UnityApiUrl = "http://localhost:5000/api/unity/command/";

        // 🔥 Создаем экземпляр шины событий
        private readonly IEventBus _eventBus = new EventBus();

        public MainWindow()
        {
            InitializeComponent();
            InitializeSystem();
        }

        private void InitializeSystem()
        {
            try
            {
                // 1. Подписываемся на события ДО инициализации модулей
                _eventBus.Subscribe<TextRecognizedEvent>(OnTextRecognizedHandler);

                // 2. Настройка озвучки (TTS)
                synthesizer = new SpeechSynthesizer();
                synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
                synthesizer.Rate = 0;

                // 3. Настройка распознавания (Vosk)
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vosk-model-small-ru");
                if (!Directory.Exists(modelPath))
                {
                    string? solutionDir = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName;
                    modelPath = Path.Combine(solutionDir ?? "", "Models", "vosk-model-small-ru");
                }

                // 🔥 Передаем EventBus в конструктор
                vosk = new VoskWrapper(modelPath, _eventBus);

                // 4. Настройка локального ИИ (Ollama)
                aiService = new OllamaService();

                Log("✅ Система готова! Говори!");
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка инициализации: {ex.Message}");
                btnToggleListen.IsEnabled = false;
            }
        }

        // 🔥 Обработчик события из шины
        private void OnTextRecognizedHandler(TextRecognizedEvent e)
        {
            Log($"👂 Я слышу: {e.Text}");
            ProcessInput(e.Text);
        }

        private async void ProcessInput(string userText)
        {
            vosk?.Stop();

            string response;
            if (aiService != null)
            {
                Log("🤔 Думаю...");
                response = await aiService.GetResponseAsync(userText);
            }
            else
            {
                response = "ИИ не подключен";
            }

            Log($"💬 Ответ: {response}");

            if (synthesizer != null)
            {
                await Task.Run(() => synthesizer.Speak(response));
            }

            if (isListening)
            {
                vosk?.Start();
            }

            await SendToUnityAsync(response, "Talking");
        }

        private async Task SendToUnityAsync(string text, string animation)
        {
            try
            {
                var payload = new { text = "", animation = animation };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(UnityApiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("[Unity] ✅ Команда принята");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Unity] ⚠️ Ошибка: {ex.Message}");
            }
        }

        private void BtnToggleListen_Click(object sender, RoutedEventArgs e)
        {
            if (vosk == null) return;

            if (!isListening)
            {
                vosk.Start();
                isListening = true;
                btnToggleListen.Content = "⏹ Остановить";
                btnToggleListen.Background = System.Windows.Media.Brushes.DarkRed;
                Log("🎤 Слушаю...");
            }
            else
            {
                vosk.Stop();
                isListening = false;
                btnToggleListen.Content = "🎤 Начать слушать";
                btnToggleListen.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#0078D4");
                Log("🔴 Остановлено");
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            var text = txtInput.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                txtInput.Clear();
                Log($"⌨️ Ты: {text}");
                ProcessInput(text);
            }
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var text = txtInput.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    txtInput.Clear();
                    Log($"⌨️ Ты: {text}");
                    ProcessInput(text);
                }
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {message}\n");
                txtLog.ScrollToEnd();
            });
        }
    }
}