using AiAssistantDesktop.Core.Events;
using AiAssistantDesktop.Core.Models;
using AiAssistantDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input; // 🔥 Добавлено
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

            // 1. Получаем сервисы из контейнера
            _agent = App.Services!.GetRequiredService<ConversationAgent>();
            _eventBus = App.Services!.GetRequiredService<IEventBus>();
            _themeManager = new ThemeManager();

            // 2. Подписываемся на события ядра
            _eventBus.Subscribe<UserSpokeEvent>(OnUserSpoke);
            _eventBus.Subscribe<AgentThinkingEvent>(OnAgentThinking);
            _eventBus.Subscribe<AgentRespondedEvent>(OnAgentResponded);
            _eventBus.Subscribe<AgentErrorEvent>(OnAgentError);

            // 3. Подписываемся на события Фазы 3 (когнитивный цикл)
            _eventBus.Subscribe<InternalThoughtEvent>(OnInternalThought);
            _eventBus.Subscribe<CognitiveLoopStartedEvent>(_ => Log("🧠 Когнитивный цикл запущен"));
            _eventBus.Subscribe<CognitiveLoopStoppedEvent>(_ => Log("🛑 Когнитивный цикл остановлен"));

            // 🔥 4. Подписываемся на событие прерывания (Фаза 5)
            _eventBus.Subscribe<AgentInterruptedEvent>(OnAgentInterrupted);

            // 5. Инициализация тем оформления
            cmbTheme.ItemsSource = _themeManager.AvailableThemes;
            cmbTheme.SelectedItem = _themeManager.GetCurrentTheme();
            _themeManager.ApplyTheme(cmbTheme.SelectedItem?.ToString() ?? "Dark");

            //  6. Инициализация списка моделей
            LoadAvailableModels();

            // 7. Начальная настройка UI
            UpdateListenButtonUI();
            UpdateStatusLabel();
        }

        // ==================== ОБРАБОТЧИКИ СОБЫТИЙ ====================

        private void OnUserSpoke(UserSpokeEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                Log($"👤 Пользователь: {e.Text}");
                lblStatus.Text = "🧠 Агент обрабатывает...";
                lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
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
                lblStatus.Text = " Система активна";
                lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                UpdateStatusLabel();
            });
        }

        private void OnAgentError(AgentErrorEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                Log($"❌ Ошибка: {e.Message}");
                lblStatus.Text = "⚠️ Ошибка";
                lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E57373"));
            });
        }

        private void OnInternalThought(InternalThoughtEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                var icon = e.Thought.Type switch
                {
                    ThoughtType.Proactive => "💡",
                    ThoughtType.MemoryConsolidation => "🗃️",
                    ThoughtType.ToolReflection => "🛠️",
                    _ => "🌫️"
                };
                Log($"{icon} Мысль: {e.Thought.Content}");
            });
        }

        // 🔥 Обработчик прерывания (Barge-in)
        private void OnAgentInterrupted(AgentInterruptedEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                Log($"⚡ Прервано! {(string.IsNullOrWhiteSpace(e.InterruptReason) ? "" : $"Причина: {e.InterruptReason}")}");
                lblStatus.Text = "👂 Слушаю...";
                lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
            });
        }

        // ==================== УПРАВЛЕНИЕ КНОПКАМИ ====================

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
            UpdateStatusLabel();
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e) => txtLog.Clear();

        private void CmbTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbTheme.SelectedItem != null)
                _themeManager.ApplyTheme(cmbTheme.SelectedItem.ToString());
        }

        // 🔥 Обработчик смены модели
        private async void CmbModel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbModel?.SelectedItem is string newModel && !string.IsNullOrWhiteSpace(newModel))
            {
                // 🔥 Не пытаемся переключиться, если модель уже активна
                if (newModel == _agent.GetCurrentModel())
                {
                    // Просто обновляем статус без ошибки
                    UpdateStatusLabel();
                    return;
                }

                lblStatus.Text = "🔄 Переключение модели...";
                var success = await _agent.SwitchLlmModelAsync(newModel);

                if (success)
                    Log($"✅ Модель: {newModel}");
                else
                    Log($"❌ Ошибка переключения на {newModel}");

                UpdateStatusLabel();
            }
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

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

        private void UpdateStatusLabel()
        {
            var status = _agent.GetStatus();
            var filesInfo = status.SessionFilesCount > 0 ? $" 📎{status.SessionFilesCount}" : "";
            var modelInfo = $" [{_agent.GetCurrentModel()}]";

            lblStatus.Text = status.IsListening
                ? $"🟢 Слушаю{filesInfo}{modelInfo}"
                : $" Пауза{filesInfo}{modelInfo}";
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                txtLog.AppendText($"{time} {message}\n");
                txtLog.ScrollToEnd();
            });
        }

        // 🔥 Загружает доступные модели в ComboBox
        private void LoadAvailableModels()
        {
            try
            {
                var models = _agent.GetAvailableModels();

                if (cmbModel != null)
                {
                    cmbModel.ItemsSource = models;

                    // 🔥 Устанавливаем текущую модель БЕЗ вызова события SelectionChanged
                    var currentModel = _agent.GetCurrentModel();
                    cmbModel.SelectedItem = currentModel;

                    Log($"📚 Доступные модели: {string.Join(", ", models)}");
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️ Не удалось загрузить модели: {ex.Message}");
            }
        }

        // 🔥 Горячие клавиши
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Ctrl+M: переключить микрофон
            if (e.Key == Key.M && Keyboard.Modifiers == ModifierKeys.Control)
            {
                BtnToggleListen_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }

            // Ctrl+D: перезапуск когнитивного цикла
            if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Log("🔄 Перезапуск когнитивного цикла...");
                e.Handled = true;
            }

            // Ctrl+L: очистить лог
            if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
            {
                BtnClearLog_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}