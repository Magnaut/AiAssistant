namespace AiAssistantDesktop.Core.Models.Tool
{
    public class ToolResult
    {
        public bool Success { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Error { get; set; }

        public static ToolResult Ok(string content) => new() { Success = true, Content = content };
        public static ToolResult Fail(string error) => new() { Success = false, Error = error };
    }
}