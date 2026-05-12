using System;
using System.Diagnostics; // 🔥 Добавлено
using System.IO;
using System.Linq;
using System.Reflection;
using AiAssistantDesktop.Core.Interfaces;

namespace AiAssistantDesktop.Core.Services
{
    public class PluginLoader
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _pluginsPath;

        public PluginLoader(IServiceProvider serviceProvider, string? pluginsPath = null)
        {
            _serviceProvider = serviceProvider;
            _pluginsPath = pluginsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

            Debug.WriteLine($"🔍 PluginLoader: Ищу плагины в {_pluginsPath}");

            if (!Directory.Exists(_pluginsPath))
            {
                Debug.WriteLine($"⚠️ Папка Plugins не найдена, создаю...");
                Directory.CreateDirectory(_pluginsPath);
            }
        }

        public void LoadAllPlugins()
        {
            Debug.WriteLine($"📂 Сканирую папку: {_pluginsPath}");

            var dlls = Directory.GetFiles(_pluginsPath, "*.dll");
            Debug.WriteLine($"📦 Найдено DLL: {dlls.Length}");

            if (dlls.Length == 0)
            {
                Debug.WriteLine("❌ Плагины не найдены!");
                return;
            }

            foreach (var dll in dlls)
            {
                try
                {
                    Debug.WriteLine($"🔄 Загрузка: {Path.GetFileName(dll)}");
                    var assembly = Assembly.LoadFrom(dll);
                    var types = assembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);

                    Debug.WriteLine($"  Найдено типов IPlugin: {types.Count()}");

                    foreach (var type in types)
                    {
                        var plugin = Activator.CreateInstance(type) as IPlugin;
                        if (plugin != null)
                        {
                            plugin.Initialize(_serviceProvider);
                            Debug.WriteLine($"✅ Загружен плагин: {plugin.Name}");
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Debug.WriteLine($"❌ Ошибка загрузки типов из {dll}:");
                    foreach (var loaderEx in ex.LoaderExceptions)
                    {
                        Debug.WriteLine($"   - {loaderEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Ошибка загрузки плагина {dll}: {ex.Message}");
                    Debug.WriteLine($"   Stack: {ex.StackTrace}");
                }
            }
        }
    }
}