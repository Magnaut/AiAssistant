using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Models;

namespace AiAssistantDesktop.Core.Services
{
    /// <summary>
    /// Менеджер временных файлов сессии
    /// Файлы хранятся в памяти и очищаются при завершении сессии
    /// </summary>
    public class SessionFileManager
    {
        private readonly Dictionary<string, SessionFile> _files;
        private readonly string _sessionDataPath;
        private readonly int _maxFiles;
        private readonly long _maxTotalSizeBytes;

        public SessionFileManager(string? sessionDataPath = null, int maxFiles = 10, long maxTotalSizeBytes = 10 * 1024 * 1024)
        {
            _files = new Dictionary<string, SessionFile>();
            _sessionDataPath = sessionDataPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "session");
            _maxFiles = maxFiles;
            _maxTotalSizeBytes = maxTotalSizeBytes;

            // Создаём папку, если не существует
            if (!Directory.Exists(_sessionDataPath))
                Directory.CreateDirectory(_sessionDataPath);
        }

        /// <summary>
        /// Добавляет файл в сессию
        /// </summary>
        public bool AddFile(string fileName, string content, string contentType = "text/plain")
        {
            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(content))
                return false;

            // Проверка лимитов
            if (_files.Count >= _maxFiles)
                RemoveOldestFile();

            var size = System.Text.Encoding.UTF8.GetByteCount(content);
            if (GetTotalSize() + size > _maxTotalSizeBytes)
                return false; // Не хватает места

            var sessionFile = new SessionFile(fileName, content, contentType);
            _files[fileName.ToLowerInvariant()] = sessionFile;

            // Опционально: сохраняем на диск для восстановления
            SaveToFile(sessionFile);

            return true;
        }

        /// <summary>
        /// Получает файл по имени
        /// </summary>
        public SessionFile? GetFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            _files.TryGetValue(fileName.ToLowerInvariant(), out var file);
            return file;
        }

        /// <summary>
        /// Ищет контент по ключевому слову (простой семантический поиск)
        /// </summary>
        public List<SessionFile> SearchByKeyword(string keyword, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<SessionFile>();

            var lowerKeyword = keyword.ToLowerInvariant();
            return _files.Values
                .Where(f => f.Content.ToLowerInvariant().Contains(lowerKeyword) ||
                           f.FileName.ToLowerInvariant().Contains(lowerKeyword))
                .OrderByDescending(f => f.UploadedAt)
                .Take(maxResults)
                .ToList();
        }

        /// <summary>
        /// Удаляет файл
        /// </summary>
        public bool RemoveFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            var key = fileName.ToLowerInvariant();
            if (_files.Remove(key, out var file))
            {
                TryDeleteFromFileSystem(file.FileName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Очищает все файлы сессии
        /// </summary>
        public void ClearAll()
        {
            foreach (var file in _files.Values)
            {
                TryDeleteFromFileSystem(file.FileName);
            }
            _files.Clear();
        }

        /// <summary>
        /// Получает контекст для LLM из всех файлов
        /// </summary>
        public string GetContextForLLM(int maxChars = 2000)
        {
            if (_files.Count == 0) return string.Empty;

            var context = new System.Text.StringBuilder();
            context.AppendLine("📎 Прикреплённые файлы сессии:");

            foreach (var file in _files.Values.OrderBy(f => f.UploadedAt))
            {
                var preview = file.Content.Length > 200
                    ? file.Content.Substring(0, 200) + "..."
                    : file.Content;

                context.AppendLine($"• {file.FileName} ({file.ContentType}): {preview}");
                if (context.Length > maxChars) break;
            }

            return context.ToString();
        }

        /// <summary>
        /// Сохраняет файл на диск (опционально)
        /// </summary>
        private void SaveToFile(SessionFile file)
        {
            try
            {
                var filePath = Path.Combine(_sessionDataPath, $"{file.FileName}.session");
                File.WriteAllText(filePath, file.Content, System.Text.Encoding.UTF8);
            }
            catch { /* Игнорируем ошибки сохранения */ }
        }

        /// <summary>
        /// Удаляет файл с диска
        /// </summary>
        private void TryDeleteFromFileSystem(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_sessionDataPath, $"{fileName}.session");
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch { /* Игнорируем */ }
        }

        /// <summary>
        /// Удаляет самый старый файл при переполнении
        /// </summary>
        private void RemoveOldestFile()
        {
            var oldest = _files.Values.OrderBy(f => f.UploadedAt).FirstOrDefault();
            if (oldest != null)
            {
                RemoveFile(oldest.FileName);
            }
        }

        /// <summary>
        /// Получает общий размер всех файлов
        /// </summary>
        private long GetTotalSize() => _files.Values.Sum(f => f.SizeBytes);

        /// <summary>
        /// Получает список имён файлов
        /// </summary>
        public IEnumerable<string> GetFileNames() => _files.Values.Select(f => f.FileName);
    }
}