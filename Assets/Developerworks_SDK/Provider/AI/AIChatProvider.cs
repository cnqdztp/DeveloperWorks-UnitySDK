using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Developerworks_SDK.Provider.AI
{
    /// <summary>
    /// Provider for the platform AI endpoint (/ai/{gameId}/v1/chat)
    /// Uses platform-hosted AI models with game-based routing
    /// </summary>
    internal class AIChatProvider : IChatProvider
    {
        private readonly Auth.DW_AuthManager _authManager;
        private readonly bool _useOversea;

        public AIChatProvider(Auth.DW_AuthManager authManager,bool useOversea = false)
        {
            _authManager = authManager;
            _useOversea = useOversea;
        }

        private string GetChatUrl()
        {
            if (_authManager == null || string.IsNullOrEmpty(_authManager.PublishableKey))
            {
                throw new InvalidOperationException("PublishableKey (GameId) is not available from AuthManager.");
            }
            if(_useOversea){
                return $"https://dwoversea.agentlandlab.com/ai/{_authManager.PublishableKey}/v1/chat";
            }
            return $"https://developerworks.agentlandlab.com/ai/{_authManager.PublishableKey}/v1/chat";
        }

        private string GetAuthToken()
        {
            if (_authManager == null || string.IsNullOrEmpty(_authManager.AuthToken))
            {
                throw new InvalidOperationException("Authentication token is not available.");
            }
            return _authManager.AuthToken;
        }

        public async UniTask<ChatCompletionResponse> ChatCompletionAsync(
            ChatCompletionRequest request, 
            System.Threading.CancellationToken cancellationToken = default)
        {
            // Debug.Log("[AIChatProvider] ChatCompletionAsync");
            
            // Convert to AI endpoint format if needed (currently same as OpenAI format)
            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            
            using (var webRequest = new UnityWebRequest(GetChatUrl(), "POST"))
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
                    Debug.LogError($"[AIChatProvider] API request failed: {ex.Message}"); 
                    return null; 
                }
                
                if (webRequest.result != UnityWebRequest.Result.Success) 
                { 
                    Debug.LogError($"[AIChatProvider] API Error: {webRequest.responseCode} - {webRequest.error}\n{webRequest.downloadHandler.text}"); 
                    return null; 
                }
                
                // Parse response - AI endpoint returns OpenAI compatible format for non-streaming
                return JsonConvert.DeserializeObject<ChatCompletionResponse>(webRequest.downloadHandler.text);
            }
        }

        public async UniTask ChatCompletionStreamAsync(
            ChatCompletionRequest request, 
            Action<string> onTextDelta,
            Action<StreamCompletionResponse> onLegacyResponse, 
            Action onFinally, 
            System.Threading.CancellationToken cancellationToken = default)
        {
            // Debug.Log("[AIChatProvider] ChatCompletionStreamAsync");
            
            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            
            using (var webRequest = new UnityWebRequest(GetChatUrl(), "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(new UTF8Encoding().GetBytes(json));
                webRequest.downloadHandler = new StreamingDownloadHandler(onTextDelta, onLegacyResponse);
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {GetAuthToken()}");
                
                try 
                { 
                    await webRequest.SendWebRequest().ToUniTask(cancellationToken: cancellationToken); 
                }
                catch (Exception ex) when (!(ex is OperationCanceledException)) 
                { 
                    Debug.LogError($"[AIChatProvider] API stream request failed: {ex.Message}"); 
                }
                finally 
                { 
                    onFinally?.Invoke(); 
                }
            }
        }

        private class StreamingDownloadHandler : DownloadHandlerScript
        {
            private readonly Action<string> _onTextDelta;
            private readonly Action<StreamCompletionResponse> _onLegacyResponse;
            
            public StreamingDownloadHandler(Action<string> onTextDelta, Action<StreamCompletionResponse> onLegacyResponse) 
            { 
                _onTextDelta = onTextDelta;
                _onLegacyResponse = onLegacyResponse;
            }
            
            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength == 0) return true;
                
                var lines = Encoding.UTF8.GetString(data, 0, dataLength).Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("data: "))
                    {
                        var jsonData = line.Substring(6);
                        if (jsonData.Trim() == "[DONE]") continue;
                        
                        try 
                        { 
                            // Try to parse as UI Message Stream format first
                            var uiMessage = JsonConvert.DeserializeObject<UIMessageStreamResponse>(jsonData);
                            if (uiMessage != null && !string.IsNullOrEmpty(uiMessage.Type))
                            {
                                // Handle UI Message Stream format
                                if (uiMessage.Type == "text-delta" && !string.IsNullOrEmpty(uiMessage.Delta))
                                {
                                    _onTextDelta?.Invoke(uiMessage.Delta);
                                }
                                // Handle other types like "start", "finish", etc. if needed
                                continue;
                            }
                        }
                        catch (JsonException)
                        {
                            // Not UI Message Stream format, try legacy format
                        }
                        
                        try
                        {
                            // Fallback to legacy OpenAI compatible format
                            var legacyResponse = JsonConvert.DeserializeObject<StreamCompletionResponse>(jsonData);
                            _onLegacyResponse?.Invoke(legacyResponse);
                        }
                        catch (JsonException ex) 
                        { 
                            Debug.LogWarning($"[AIChatProvider] Failed to parse streaming response: {ex.Message}\nData: {jsonData}");
                        }
                    }
                }
                return true;
            }
        }
    }
}