using System;
using System.Windows;
using System.Windows.Media;
using AiAssistantDesktop.Core.Events;
using AiAssistantDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiAssistantDesktop
{
    public partial class MainWindow : Window
    {
        private readonly ConversationAgent _agent;
        private readonly IEventBus _eventBus;
        private bool _isListening = true;

        public MainWindow()
        {
            InitializeComponent();

            // 1. Получаем сервисы из контейнера (DI)
            _agent = App.Services!.GetRequiredService<ConversationAgent>();
            _eventBus = App.Services!.GetRequiredService<IEventBus>();

            // 2. Подписываемся на события Агентa (чтобы UI знал, что происходит)
            _eventBus.Subscribe<UserSpokeEvent>(OnUserSpoke);
            _eventBus.Subscribe<AgentThinkingEvent>(OnAgentThinking);
            _eventBus.Subscribe<AgentRespondedEvent>(OnAgentResponded);
            _eventBus.Subscribe<AgentErrorEvent>(OnAgentError);

            // 3. Начальная настройка UI
            UpdateListenButtonUI();
        }

        // --- Обработчики событий (работают в фоновом потоке!) ---

        private void OnUserSpoke(UserSpokeEvent e)
        {
            // Dispatcher.Invoke нужен, так как события приходят не из UI-потока
            Dispatcher.Invoke(() =>
            {
                Log($"👤 Пользователь: {e.Text}", "#4FC3F7"); // Голубой цвет
                lblStatus.Text = "🧠 Агент обрабатывает...";
            });
        }

        private void OnAgentThinking(AgentThinkingEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                Log("🤔 Агент думает...", "#FF9800"); // Оранжевый
            });
        }

        private void OnAgentResponded(AgentRespondedEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                Log($"🤖 Агент: {e.Text}", "#81C784"); // Зеленый
                lblStatus.Text = "🟢 Система активна";
            });
        }

        private void OnAgentError(AgentErrorEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                Log($"❌ Ошибка: {e.Message}", "#E57373"); // Красный
                lblStatus.Text = "⚠️ Ошибка";
            });
        }

        // --- Управление кнопками ---

        private async void BtnToggleListen_Click(object sender, RoutedEventArgs e)
        {
            _isListening = !_isListening;
            UpdateListenButtonUI();

            if (_isListening)
            {
                await _agent.StartListeningAsync(); // ✅ Исправлено
                Log("🎤 Микрофон включен.", "#AAAAAA");
            }
            else
            {
                await _agent.StopListeningAsync(); // ✅ Исправлено
                Log("🔇 Микрофон выключен.", "#AAAAAA");
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }

        // --- Вспомогательные методы ---

        private void UpdateListenButtonUI()
        {
            if (_isListening)
            {
                btnToggleListen.Content = "⏹ Остановить";
                btnToggleListen.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5252")); // Красный
            }
            else
            {
                btnToggleListen.Content = "▶️ Слушать";
                btnToggleListen.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")); // Зеленый
            }
        }

        private void Log(string message, string? hexColor = null)
        {
            Dispatcher.Invoke(() =>
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                txtLog.AppendText($"{time} {message}\n");
                txtLog.ScrollToEnd();
            });
        }
    }
}