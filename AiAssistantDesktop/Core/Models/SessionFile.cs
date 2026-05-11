using System;

namespace AiAssistantDesktop.Core.Models
{
    /// <summary>
    /// Метаданные файла сессии
    /// </summary>
    public class SessionFile
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public long SizeBytes { get; set; }
        public string? Summary { get; set; }

        public SessionFile() { }

        public SessionFile(string fileName, string content, string contentType = "text/plain")
        {
            FileName = fileName;
            Content = content;
            ContentType = contentType;
            SizeBytes = System.Text.Encoding.UTF8.GetByteCount(content);
            UploadedAt = DateTime.UtcNow;
        }
    }
}