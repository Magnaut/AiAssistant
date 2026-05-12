using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AiAssistantDesktop.Core.Interfaces;
using AiAssistantDesktop.Core.Models.Tool;

namespace AiAssistantDesktop.Core.Services
{
    public class ToolRegistry
    {
        private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

        public void Register(ITool tool)
        {
            if (tool != null && !string.IsNullOrWhiteSpace(tool.Name))
            {
                _tools[tool.Name] = tool;
                Debug.WriteLine($"✅ Зарегистрирован инструмент: {tool.Name}");
            }
        }

        public ITool? GetTool(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            _tools.TryGetValue(name, out var tool);
            return tool;
        }

        public List<ToolDefinition> GetAllTools()
        {
            return _tools.Values.Select(t => new ToolDefinition
            {
                Name = t.Name,
                Description = t.Description,
                ParametersSchema = t.ParametersSchema
            }).ToList();
        }
    }
}