using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiAssistantDesktop.Core.Interfaces
{
    /// <summary>
    /// Интерфейс для провайдеров, поддерживающих переключение моделей на лету
    /// </summary>
    public interface ISwitchableProvider
    {
        string CurrentModel { get; }
        IEnumerable<string> AvailableModels { get; }

        Task<bool> SwitchModelAsync(string modelName);
        Task ReloadAsync();
    }
}