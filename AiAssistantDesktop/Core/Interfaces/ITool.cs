using System.Collections.Generic;
using System.Threading.Tasks;
using AiAssistantDesktop.Core.Models.Tool;

namespace AiAssistantDesktop.Core.Interfaces
{
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        Dictionary<string, string> ParametersSchema { get; } // name -> description

        Task<ToolResult> ExecuteAsync(Dictionary<string, string> parameters);
    }
}