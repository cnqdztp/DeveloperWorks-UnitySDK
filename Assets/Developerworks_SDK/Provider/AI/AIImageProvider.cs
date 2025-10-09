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
            if (_authManager == null || string.IsNullOrEmpty(_authManager.gameId))
            {
                throw new InvalidOperationException("PublishableKey (GameId) is not available from AuthManager.");
            }
            if(_useOversea)
            {
                return $"https://dwoversea.agentlandlab.com/ai/{_authManager.gameId}/v1/image";
            }
            return $"https://developerworks.agentlandlab.com/ai/{_authManager.gameId}/v1/image";
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
            // Debug.Log("[AIImageProvider] GenerateImageAsync");
            
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
                catch (UnityWebRequestException ex) when (!(ex is OperationCanceledException)) 
                { 
                    // Check if we have response data to parse
                    if (webRequest.downloadHandler != null && !string.IsNullOrEmpty(webRequest.downloadHandler.text))
                    {
                        // Try to parse error response
                        try
                        {
                            var errorResponse = JsonConvert.DeserializeObject<DW_ApiErrorResponse>(webRequest.downloadHandler.text);
                            if (errorResponse?.error != null)
                            {
                                // Check for specific image size validation errors
                                if (errorResponse.error.code == DW_ErrorCodes.INVALID_SIZE_FORMAT ||
                                    errorResponse.error.code == DW_ErrorCodes.INVALID_SIZE_VALUE ||
                                    errorResponse.error.code == DW_ErrorCodes.SIZE_EXCEEDS_LIMIT ||
                                    errorResponse.error.code == DW_ErrorCodes.SIZE_NOT_MULTIPLE ||
                                    errorResponse.error.code == DW_ErrorCodes.SIZE_NOT_ALLOWED)
                                {
                                    // Don't log here, let the caller handle it
                                    throw new DW_ImageSizeValidationException(
                                        errorResponse.error.message,
                                        errorResponse.error.code,
                                        request.Size
                                    );
                                }

                                // Throw general API error
                                throw new DW_ApiErrorException(
                                    errorResponse.error.message,
                                    errorResponse.error.code,
                                    (int)webRequest.responseCode
                                );
                            }
                        }
                        catch (JsonException)
                        {
                            // If error response parsing fails, log and throw generic error
                            Debug.LogError($"[AIImageProvider] Failed to parse error response: {webRequest.downloadHandler.text}");
                        }
                    }
                    
                    // Only log for actual network/unknown errors
                    Debug.LogError($"[AIImageProvider] API request failed: {ex.Message}");
                    throw new DWException($"Network request failed: {ex.Message}", ex);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException) && !(ex is DW_ImageSizeValidationException) && !(ex is DW_ApiErrorException) && !(ex is DWException))
                {
                    Debug.LogError($"[AIImageProvider] Unexpected error: {ex.Message}");
                    throw new DWException($"Unexpected error: {ex.Message}", ex);
                }
                
                if (webRequest.result != UnityWebRequest.Result.Success) 
                { 
                    Debug.LogError($"[AIImageProvider] API Error: {webRequest.responseCode} - {webRequest.error}\n{webRequest.downloadHandler.text}");
                    
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<DW_ApiErrorResponse>(webRequest.downloadHandler.text);
                        if (errorResponse?.error != null)
                        {
                            // Check for specific image size validation errors
                            if (errorResponse.error.code == DW_ErrorCodes.INVALID_SIZE_FORMAT ||
                                errorResponse.error.code == DW_ErrorCodes.INVALID_SIZE_VALUE ||
                                errorResponse.error.code == DW_ErrorCodes.SIZE_EXCEEDS_LIMIT ||
                                errorResponse.error.code == DW_ErrorCodes.SIZE_NOT_MULTIPLE ||
                                errorResponse.error.code == DW_ErrorCodes.SIZE_NOT_ALLOWED)
                            {
                                throw new DW_ImageSizeValidationException(
                                    errorResponse.error.message,
                                    errorResponse.error.code,
                                    request.Size
                                );
                            }

                            // Throw general API error
                            throw new DW_ApiErrorException(
                                errorResponse.error.message,
                                errorResponse.error.code,
                                (int)webRequest.responseCode
                            );
                        }
                    }
                    catch (JsonException)
                    {
                        // If error response parsing fails, continue to throw generic error below
                    }
                    catch (DW_ImageSizeValidationException)
                    {
                        // Re-throw image size validation exceptions
                        throw;
                    }
                    catch (DW_ApiErrorException)
                    {
                        // Re-throw API error exceptions
                        throw;
                    }

                    throw new DWException(
                        $"API request failed with status {webRequest.responseCode}: {webRequest.error}",
                        null,
                        (int)webRequest.responseCode
                    );
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