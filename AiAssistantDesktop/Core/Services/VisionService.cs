using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Modules.LLM;

namespace AiAssistantDesktop.Core.Services
{
    public class VisionService : IVisionService
    {
        private readonly ILLMProvider _llm;
        private readonly string _visionModel;

        public VisionService(ILLMProvider llm, string visionModel = "llava:7b")
        {
            _llm = llm;
            _visionModel = visionModel;
        }

        public async Task<string?> CaptureScreenAsync()
        {
            try
            {
                Debug.WriteLine("📸 Делаю скриншот экрана...");

                // Используем WPF для захвата экрана
                var screen = System.Windows.Forms.Screen.PrimaryScreen;
                if (screen == null) return null;

                using var bitmap = new System.Drawing.Bitmap(screen.Bounds.Width, screen.Bounds.Height);
                using var graphics = System.Drawing.Graphics.FromImage(bitmap);

                graphics.CopyFromScreen(
                    screen.Bounds.X, screen.Bounds.Y,
                    0, 0,
                    screen.Bounds.Size,
                    System.Drawing.CopyPixelOperation.SourceCopy);

                // Конвертируем в base64
                using var ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                var base64 = Convert.ToBase64String(ms.ToArray());

                Debug.WriteLine($"✅ Скриншот готов: {base64.Length} симв.");
                return base64;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка скриншота: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> CaptureActiveWindowAsync()
        {
            try
            {
                Debug.WriteLine("📸 Делаю скриншот активного окна...");

                var hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (hwnd == IntPtr.Zero) return await CaptureScreenAsync();

                using var bitmap = new System.Drawing.Bitmap(
                    System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Width ?? 1920,
                    System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Height ?? 1080);

                using var graphics = System.Drawing.Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);

                using var ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка: {ex.Message}");
                return await CaptureScreenAsync();
            }
        }

        public async Task<string> AnalyzeImageAsync(string base64Image, string prompt)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
                return "❌ Не удалось получить изображение.";

            Debug.WriteLine($"👁️ Анализирую изображение: {prompt}");

            // Формат для Ollama vision-моделей
            var messages = new[]
            {
                new { role = "user", content = prompt, images = new[] { base64Image } }
            };

            var request = new
            {
                model = _visionModel,
                messages,
                stream = false,
                options = new
                {
                    temperature = 0.3f,
                    num_predict = 256
                }
            };

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(request);
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("http://localhost:11434/api/chat", content);
                if (!response.IsSuccessStatusCode)
                    return $"❌ Ошибка vision: {response.StatusCode}";

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<OllamaVisionResponse>(responseJson, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result?.Message?.Content ?? "❌ Не удалось проанализировать изображение.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Vision error: {ex.Message}");
                return $"❌ Ошибка: {ex.Message}";
            }
        }
    }

    // Вспомогательный класс для парсинга ответа Ollama
    internal class OllamaVisionResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public OllamaVisionMessage? Message { get; set; }
    }

    internal class OllamaVisionMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}