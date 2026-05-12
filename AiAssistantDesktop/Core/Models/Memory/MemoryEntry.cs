using System;

namespace AiAssistantDesktop.Core.Models.Memory
{
    public class MemoryEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; } = string.Empty;
        public MemoryLevel Level { get; set; } = MemoryLevel.ShortTerm;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public string? Source { get; set; } // "user", "agent", "tool", "system"
        public Dictionary<string, string> Metadata { get; set; } = new();

        public MemoryEntry() { }

        public MemoryEntry(string content, MemoryLevel level, string? source = null, TimeSpan? ttl = null)
        {
            Content = content;
            Level = level;
            Source = source;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null;
        }

        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }
}