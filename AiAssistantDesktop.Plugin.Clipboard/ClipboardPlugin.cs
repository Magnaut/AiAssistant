using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models.Tool;
using AiAssistantDesktop.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace AiAssistantDesktop.Modules.Plugins
{
    public class ClipboardPlugin : IPlugin, ITool
    {
        public string Name => "ClipboardPlugin";
        public string Description => "Управление буфером обмена.";

        public string ToolName => "clipboard";
        public string ToolDescription => "Читает или записывает в буфер обмена. Параметры: action (get/set), text (если set).";
        public Dictionary<string, string> ParametersSchema => new()
        {
            { "action", "get или set" },
            { "text", "Текст для записи (если action=set)" }
        };

        public void Initialize(IServiceProvider serviceProvider)
        {
            var registry = serviceProvider.GetService(typeof(ToolRegistry)) as ToolRegistry;
            registry?.Register(this);
        }

        public Task<ToolResult> ExecuteAsync(Dictionary<string, string> parameters)
        {
            try
            {
                // 🔥 ИСПРАВЛЕНО: используем разные переменные или out параметры
                if (parameters.TryGetValue("action", out var action) && action.ToLower() == "get")
                {
                    var clipboardText = Clipboard.GetText(); // 🔥 Переименовали
                    return Task.FromResult(ToolResult.Ok(string.IsNullOrEmpty(clipboardText) ? "Буфер пуст." : clipboardText));
                }

                if (parameters.TryGetValue("action", out action) && action.ToLower() == "set")
                {
                    // 🔥 ИСПРАВЛЕНО: проверяем наличие text в параметрах
                    if (parameters.TryGetValue("text", out var textToCopy))
                    {
                        Clipboard.SetText(textToCopy);
                        return Task.FromResult(ToolResult.Ok("Текст скопирован в буфер."));
                    }
                    else
                    {
                        return Task.FromResult(ToolResult.Fail("Не указан параметр 'text'"));
                    }
                }

                return Task.FromResult(ToolResult.Fail("Неверные параметры. Используй: action=get или action=set&text=..."));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Fail($"Ошибка буфера: {ex.Message}"));
            }
        }

        public void Dispose() { }
    }
}