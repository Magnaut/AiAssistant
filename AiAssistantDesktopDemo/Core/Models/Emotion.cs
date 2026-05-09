using System.Collections.Generic;

namespace AiAssistantDesktopDemo.Core.Models
{
    /// <summary>
    /// Система эмоций (совместима с VRM blendshapes)
    /// </summary>
    public enum Emotion
    {
        Neutral = 0,
        Happy = 1,
        Sad = 2,
        Angry = 3,
        Surprised = 4,
        Fearful = 5,
        Disgusted = 6,
        Confused = 7,
        Thinking = 8,
        Excited = 9
    }

    public static class EmotionExtensions
    {
        public static Dictionary<string, float> ToBlendshapes(this Emotion emotion)
        {
            return emotion switch
            {
                Emotion.Happy => new() { { "joy", 1.0f }, { "mouthSmile", 0.8f }, { "eyeSmile", 0.6f } },
                Emotion.Sad => new() { { "sorrow", 1.0f }, { "mouthSad", 0.7f }, { "eyeSad", 0.5f } },
                Emotion.Angry => new() { { "angry", 1.0f }, { "browDown", 0.8f }, { "mouthFrown", 0.6f } },
                Emotion.Surprised => new() { { "surprised", 1.0f }, { "eyeOpen", 1.2f }, { "mouthOpen", 0.9f } },
                Emotion.Thinking => new() { { "neutral", 0.5f }, { "browUp", 0.4f } },
                Emotion.Excited => new() { { "joy", 1.0f }, { "eyeOpen", 1.1f }, { "mouthOpen", 0.7f } },
                _ => new() { { "neutral", 1.0f } }
            };
        }
    }
}