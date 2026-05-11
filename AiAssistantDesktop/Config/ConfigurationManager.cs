using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace AiAssistantDesktopDemo.Config
{
    public static class ConfigurationManager
    {
        public static AppSettings Load(string fileName = "appsettings.json")
        {
            var config = new ConfigurationBuilder()
                // AppContext.BaseDirectory всегда указывает на папку exe при запуске
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(fileName, optional: false, reloadOnChange: true)
                .Build();

            var settings = new AppSettings();
            config.Bind(settings);
            return settings;
        }
    }

    public class AppSettings
    {
        public AgentSettings Agent { get; set; } = new();
        public LLMSettings LLM { get; set; } = new();
        public VoiceSettings Voice { get; set; } = new();
        public AvatarSettings Avatar { get; set; } = new();
    }

    public class AgentSettings
    {
        public string Type { get; set; } = "BasicMemoryAgent";
        public string Name { get; set; } = "Michelle";
        public string Persona { get; set; } = "";
        public string MemoryPath { get; set; } = "./Data/memory.json";
        public bool FasterFirstResponse { get; set; } = true;
    }

    public class LLMSettings
    {
        public string Provider { get; set; } = "Ollama";
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "qwen2.5:1.5b";
        public LLMParams DefaultParams { get; set; } = new();
    }

    public class LLMParams
    {
        public int MaxTokens { get; set; } = 200;
        public float Temperature { get; set; } = 0.7f;
    }

    public class VoiceSettings
    {
        public ASRSettings ASR { get; set; } = new();
        public TTSSettings TTS { get; set; } = new();
    }

    public class ASRSettings
    {
        public bool Enabled { get; set; } = true;
        public string Provider { get; set; } = "Vosk";
        public string ModelPath { get; set; } = "./Models/vosk-model-small-ru";
    }

    public class TTSSettings
    {
        public bool Enabled { get; set; } = true;
        public string Provider { get; set; } = "SystemSpeech";
        public string VoiceGender { get; set; } = "Female";
        public int Rate { get; set; } = 0;
        public int Volume { get; set; } = 100;
    }

    public class AvatarSettings
    {
        public bool Enabled { get; set; } = true;
        public string Provider { get; set; } = "Unity";
        public string UnityUrl { get; set; } = "http://localhost:5000/api/unity";
    }
}