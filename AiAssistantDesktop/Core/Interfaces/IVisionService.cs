using System.Threading.Tasks;

namespace AiAssistantDesktop.Core.Interfaces
{
    public interface IVisionService
    {
        /// <summary>
        /// Делает скриншот всего экрана и возвращает base64
        /// </summary>
        Task<string?> CaptureScreenAsync();

        /// <summary>
        /// Делает скриншот активного окна
        /// </summary>
        Task<string?> CaptureActiveWindowAsync();

        /// <summary>
        /// Анализирует изображение через vision-модель
        /// </summary>
        Task<string> AnalyzeImageAsync(string base64Image, string prompt);
    }
}