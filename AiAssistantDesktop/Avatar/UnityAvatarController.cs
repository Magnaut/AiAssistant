/*
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AiAssistantDesktopDemo.Core.Interfaces;
using AiAssistantDesktopDemo.Core.Models;

namespace AiAssistantDesktopDemo.Avatar
{
    public class UnityAvatarController : IAvatarController
    {
        private readonly HttpClient _httpClient;
        private string _unityUrl;

        public bool IsConnected { get; private set; }

        public UnityAvatarController()
        {
            _httpClient = new HttpClient();
            _unityUrl = "http://localhost:5000/api/unity";
        }

        public async Task ConnectAsync(string unityUrl)
        {
            _unityUrl = unityUrl.TrimEnd('/');

            try
            {
                var response = await _httpClient.GetAsync($"{_unityUrl}/health");
                IsConnected = response.IsSuccessStatusCode;
            }
            catch
            {
                IsConnected = false;
            }
        }

        public async Task SetExpressionAsync(Emotion emotion, float intensity = 1.0f)
        {
            if (!IsConnected) return;

            var blendshapes = emotion.ToBlendshapes();
            foreach (var kvp in blendshapes)
                blendshapes[kvp.Key] *= intensity;

            await SetBlendshapesAsync(blendshapes);
        }

        public async Task SetBlendshapesAsync(Dictionary<string, float> @params)
        {
            if (!IsConnected) return;

            var payload = new { blendshapes = @params };
            await _httpClient.PostAsJsonAsync($"{_unityUrl}/expression", payload);
        }

        public async Task PlayAnimationAsync(string animationName)
        {
            if (!IsConnected) return;

            var payload = new { animation = animationName };
            await _httpClient.PostAsJsonAsync($"{_unityUrl}/animate", payload);
        }

        public async Task SpeakAsync(string text, bool interruptCurrent = true)
        {
            if (!IsConnected) return;

            var payload = new { text = text, interrupt = interruptCurrent };
            await _httpClient.PostAsJsonAsync($"{_unityUrl}/speak", payload);
        }

        public Task StopSpeakingAsync()
        {
            if (!IsConnected) return Task.CompletedTask;
            return _httpClient.PostAsync($"{_unityUrl}/speak/stop", null);
        }

        public async Task SetLookAtAsync(float x, float y)
        {
            if (!IsConnected) return;

            var payload = new { x = x, y = y };
            await _httpClient.PostAsJsonAsync($"{_unityUrl}/lookat", payload);
        }
    }
}
*/