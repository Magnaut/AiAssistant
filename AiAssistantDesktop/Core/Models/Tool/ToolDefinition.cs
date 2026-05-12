using System.Collections.Generic;

namespace AiAssistantDesktop.Core.Models.Tool
{
    public class ToolDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> ParametersSchema { get; set; } = new();
    }
}