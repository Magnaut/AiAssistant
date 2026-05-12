using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiAssistantDesktop.Core.Interfaces
{
    public interface ISwitchableProvider
    {
        string CurrentModel { get; }
        IEnumerable<string> AvailableModels { get; }
        Task<bool> SwitchModelAsync(string modelName);
        Task ReloadAsync();
    }
}