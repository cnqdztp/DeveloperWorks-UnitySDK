using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Developerworks_SDK.Provider.AI
{
    /// <summary>
    /// Provider for the platform AI image endpoint (/ai/{gameId}/v1/image)
    /// Uses platform-hosted image models with game-based routing
    /// </summary>
    internal class AIImageProvider : IImageProvider
    {
        private readonly Auth.DW_AuthManager _authManager;
        private readonly bool _useOversea = false;
        public AIImageProvider(Auth.DW_AuthManager authManager, bool useOversea = false)
        {
            _authManager = authManager;
            _useOversea = useOversea;
        }

        private string GetImageUrl()
        {
            if (_authManager == null || string.IsNullOrEmpty(_authManager.PublishableKey))
            {
                throw new InvalidOperationException("PublishableKey (GameId) is not available from AuthManager.");
            }
            if(_useOversea)
            {
                return $"https://dwoversea.agentlandlab.com/ai/{_authManager.PublishableKey}/v1/image";
            }
            return $"https://developerworks.agentlandlab.com/ai/{_authManager.PublishableKey}/v1/image";
        }

        private string GetAuthToken()
        {
            if (_authManager == null || string.IsNullOrEmpty(_authManager.AuthToken))
            {
                throw new InvalidOperationException("Authentication token is not available.");
            }
            return _authManager.AuthToken;
        }

        public async UniTask<ImageGenerationResponse> GenerateImageAsync(
            ImageGenerationRequest request, 
            System.Threading.CancellationToken cancellationToken = default)
        {
            Debug.Log("[AIImageProvider] GenerateImageAsync");
            
            // Validate request
            if (string.IsNullOrEmpty(request.Model))
            {
                throw new ArgumentException("Model is required for image generation");
            }
            
            if (string.IsNullOrEmpty(request.Prompt))
            {
                throw new ArgumentException("Prompt is required for image generation");
            }
            
            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            
            using (var webRequest = new UnityWebRequest(GetImageUrl(), "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes(json));
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {GetAuthToken()}");
                
                try 
                { 
                    await webRequest.SendWebRequest().ToUniTask(cancellationToken: cancellationToken); 
                }
                catch (Exception ex) when (!(ex is OperationCanceledException)) 
                { 
                    Debug.LogError($"[AIImageProvider] API request failed: {ex.Message}"); 
                    return null; 
                }
                
                if (webRequest.result != UnityWebRequest.Result.Success) 
                { 
                    Debug.LogError($"[AIImageProvider] API Error: {webRequest.responseCode} - {webRequest.error}\n{webRequest.downloadHandler.text}"); 
                    return null; 
                }
                
                // Parse response
                try
                {
                    var response = JsonConvert.DeserializeObject<ImageGenerationResponse>(webRequest.downloadHandler.text);
                    Debug.Log($"[AIImageProvider] Successfully generated {response?.Data?.Count ?? 0} images");
                    return response;
                }
                catch (JsonException ex)
                {
                    Debug.LogError($"[AIImageProvider] Failed to parse response: {ex.Message}\nResponse: {webRequest.downloadHandler.text}");
                    return null;
                }
            }
        }
    }
}