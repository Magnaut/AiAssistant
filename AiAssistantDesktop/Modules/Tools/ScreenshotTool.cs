using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models.Tool;

namespace AiAssistantDesktop.Modules.Tools
{
    public class ScreenshotTool : ITool
    {
        private readonly IVisionService _vision;

        public ScreenshotTool(IVisionService vision)
        {
            _vision = vision;
        }

        public string Name => "screenshot";
        public string Description => "Делает скриншот экрана и анализирует его. Параметры: target (screen/window), question (что найти).";

        public Dictionary<string, string> ParametersSchema => new()
        {
            { "target", "screen (весь экран) или window (активное окно)" },
            { "question", "Вопрос о содержимом экрана, например: 'Что открыто?', 'Какой сайт?', 'Есть ли ошибка?'" }
        };

        public async Task<ToolResult> ExecuteAsync(Dictionary<string, string> parameters)
        {
            try
            {
                Debug.WriteLine($"🛠 ScreenshotTool: params={string.Join(", ", parameters)}");

                // 1. Делаем скриншот
                string? base64Image = parameters.TryGetValue("target", out var target) && target.ToLower() == "window"
                    ? await _vision.CaptureActiveWindowAsync()
                    : await _vision.CaptureScreenAsync();

                if (string.IsNullOrWhiteSpace(base64Image))
                    return ToolResult.Fail("Не удалось сделать скриншот.");

                // 2. Формируем вопрос для vision-модели
                var question = parameters.TryGetValue("question", out var q) && !string.IsNullOrWhiteSpace(q)
                    ? q
                    : "Опиши кратко, что видно на этом скриншоте.";

                // 3. Анализируем через vision-модель
                var analysis = await _vision.AnalyzeImageAsync(base64Image, question);

                Debug.WriteLine($"👁️ Анализ: {analysis}");

                return ToolResult.Ok(analysis);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ScreenshotTool error: {ex.Message}");
                return ToolResult.Fail($"Ошибка: {ex.Message}");
            }
        }
    }
}