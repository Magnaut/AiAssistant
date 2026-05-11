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

            _agent = App.Services!.GetRequiredService<ConversationAgent>();
            _eventBus = App.Services!.GetRequiredService<IEventBus>();

            _eventBus.Subscribe<UserSpokeEvent>(OnUserSpoke);
            _eventBus.Subscribe<AgentThinkingEvent>(OnAgentThinking);
            _eventBus.Subscribe<AgentRespondedEvent>(OnAgentResponded);
            _eventBus.Subscribe<AgentErrorEvent>(OnAgentError);

            UpdateListenButtonUI();
        }

        private void OnUserSpoke(UserSpokeEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                Log($"👤 Пользователь: {e.Text}");
                lblStatus.Text = "🧠 Агент обрабатывает...";
            });
        }

        private void OnAgentThinking(AgentThinkingEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                Log("🤔 Агент думает...");
            });
        }

        private void OnAgentResponded(AgentRespondedEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                Log($"🤖 Агент: {e.Text}");
                lblStatus.Text = "🟢 Система активна";
            });
        }

        private void OnAgentError(AgentErrorEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                Log($"❌ Ошибка: {e.Message}");
                lblStatus.Text = "⚠️ Ошибка";
            });
        }

        private async void BtnToggleListen_Click(object sender, RoutedEventArgs e)
        {
            _isListening = !_isListening;
            UpdateListenButtonUI();

            if (_isListening)
            {
                await _agent.StartListeningAsync();
                Log("🎤 Микрофон включен.");
            }
            else
            {
                await _agent.StopListeningAsync();
                Log("🔇 Микрофон выключен.");
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }

        private void UpdateListenButtonUI()
        {
            if (_isListening)
            {
                btnToggleListen.Content = "⏹ Остановить";
                btnToggleListen.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5252"));
            }
            else
            {
                btnToggleListen.Content = "▶️ Слушать";
                btnToggleListen.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
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