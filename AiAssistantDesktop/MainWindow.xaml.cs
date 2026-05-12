using AiAssistantDesktop.Core.Events;
using AiAssistantDesktop.Core.Models;
using AiAssistantDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AiAssistantDesktop
{
    public partial class MainWindow : Window
    {
        private readonly ConversationAgent _agent;
        private readonly IEventBus _eventBus;
        private readonly ThemeManager _themeManager;
        private bool _isListening = true;

        public MainWindow()
        {
            InitializeComponent();
            _agent = App.Services!.GetRequiredService<ConversationAgent>();
            _eventBus = App.Services!.GetRequiredService<IEventBus>();
            _themeManager = new ThemeManager();

            _eventBus.Subscribe<UserSpokeEvent>(OnUserSpoke);
            _eventBus.Subscribe<AgentThinkingEvent>(OnAgentThinking);
            _eventBus.Subscribe<AgentRespondedEvent>(OnAgentResponded);
            _eventBus.Subscribe<AgentErrorEvent>(OnAgentError);
            _eventBus.Subscribe<InternalThoughtEvent>(OnInternalThought);
            _eventBus.Subscribe<CognitiveLoopStartedEvent>(_ => Log("🧠 Цикл запущен"));
            _eventBus.Subscribe<CognitiveLoopStoppedEvent>(_ => Log("🛑 Цикл остановлен"));
            _eventBus.Subscribe<AgentInterruptedEvent>(OnAgentInterrupted);

            cmbTheme.ItemsSource = _themeManager.AvailableThemes;
            cmbTheme.SelectedItem = _themeManager.GetCurrentTheme();
            _themeManager.ApplyTheme(cmbTheme.SelectedItem?.ToString() ?? "Dark");
            LoadAvailableModels();
            UpdateListenButtonUI();
            UpdateStatusLabel();
        }

        private void OnUserSpoke(UserSpokeEvent e) => Dispatcher.Invoke(() => { Log($"👤 Пользователь: {e.Text}"); lblStatus.Text = "🧠 Обрабатывает..."; });
        private void OnAgentThinking(AgentThinkingEvent e) => Dispatcher.Invoke(() => Log("🤔 Думает..."));
        private void OnAgentResponded(AgentRespondedEvent e) => Dispatcher.Invoke(() => { Log($"🤖 Агент: {e.Text}"); lblStatus.Text = "🟢 Активна"; UpdateStatusLabel(); });
        private void OnAgentError(AgentErrorEvent e) => Dispatcher.Invoke(() => { Log($"❌ Ошибка: {e.Message}"); lblStatus.Text = "⚠️ Ошибка"; });
        private void OnInternalThought(InternalThoughtEvent e) => Dispatcher.Invoke(() => Log($"{(e.Thought.Type == ThoughtType.Proactive ? "💡" : "🌫️")} Мысль: {e.Thought.Content}"));
        private void OnAgentInterrupted(AgentInterruptedEvent e) => Dispatcher.Invoke(() => { Log($"⚡ Прервано! {e.Reason}"); lblStatus.Text = "👂 Слушаю..."; });

        private async void BtnToggleListen_Click(object sender, RoutedEventArgs e)
        {
            _isListening = !_isListening;
            UpdateListenButtonUI();
            if (_isListening) { await _agent.StartListeningAsync(); Log("🎤 Включен"); }
            else { await _agent.StopListeningAsync(); Log("🔇 Выключен"); }
            UpdateStatusLabel();
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e) => txtLog.Clear();
        private void CmbTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        { if (cmbTheme.SelectedItem != null) _themeManager.ApplyTheme(cmbTheme.SelectedItem.ToString()); }

        private async void CmbModel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbModel?.SelectedItem is string newModel && !string.IsNullOrWhiteSpace(newModel) && newModel != _agent.GetCurrentModel())
            {
                lblStatus.Text = "🔄 Переключение...";
                var success = await _agent.SwitchLlmModelAsync(newModel);
                Log(success ? $"✅ Модель: {newModel}" : $"❌ Ошибка: {newModel}");
                UpdateStatusLabel();
            }
        }

        private void UpdateListenButtonUI()
        {
            btnToggleListen.Content = _isListening ? "⏹ Остановить" : "▶️ Слушать";
            btnToggleListen.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_isListening ? "#FF5252" : "#4CAF50"));
        }

        private void UpdateStatusLabel()
        {
            var s = _agent.GetStatus();
            lblStatus.Text = s.IsListening ? $"🟢 Слушаю{(_agent.GetStatus().SessionFilesCount > 0 ? $" 📎{_agent.GetStatus().SessionFilesCount}" : "")} [{_agent.GetCurrentModel()}]" : $"🔴 Пауза [{_agent.GetCurrentModel()}]";
        }

        private void Log(string message) => Dispatcher.Invoke(() => { txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {message}\n"); txtLog.ScrollToEnd(); });
        private void LoadAvailableModels() { try { var models = _agent.GetAvailableModels(); if (cmbModel != null) { cmbModel.ItemsSource = models; cmbModel.SelectedItem = _agent.GetCurrentModel(); } } catch (Exception ex) { Log($"⚠️ Модели: {ex.Message}"); } }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.M && Keyboard.Modifiers == ModifierKeys.Control) { BtnToggleListen_Click(this, new RoutedEventArgs()); e.Handled = true; }
            if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control) { BtnClearLog_Click(this, new RoutedEventArgs()); e.Handled = true; }
        }
    }
}