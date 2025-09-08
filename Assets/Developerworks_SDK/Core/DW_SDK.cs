using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Developerworks_SDK
{
    public class DW_SDK : MonoBehaviour
    {
        [SerializeField] private string gameId, defaultChatModel, defaultImageModel;
        [SerializeField] private Auth.DW_AuthManager authManager;
        [SerializeField] private bool ignoreDeveloperToken;
        public static DW_SDK Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private static bool _isInitialized = false;
        private static Auth.DW_AuthManager _dwAuthManager => Instance.authManager;
        private static Provider.IChatProvider _chatProvider;
        private static Provider.IImageProvider _imageProvider;
        private static Provider.AI.IObjectProvider _objectProvider;

        /// <summary>
        /// Asynchronously initializes the SDK. This must complete successfully before creating clients.
        /// It handles configuration loading and user authentication.
        /// </summary>
        /// <returns>True if initialization and authentication were successful, otherwise false.</returns>
        public static async UniTask<bool> InitializeAsync(string developerToken = null)
        {
            if (!Instance)
            {
                Debug.LogError("Please place DW_SDK object in your FIRST scene.");
            }
            
            Debug.Log("[Developerworks SDK] Initializing...");
            if (_isInitialized) return true;



            if (developerToken != null && !Instance.ignoreDeveloperToken)
            {
                Debug.Log("[Developerworks SDK] You are loading a developer token, this can cost you money, fine for development...");
                _dwAuthManager.Setup(Instance.gameId, developerToken);
            }
            else
            {
                
                _dwAuthManager.Setup(Instance.gameId);

            }
            bool authSuccess = await _dwAuthManager.AuthenticateAsync();

            if (!authSuccess)
            {
                Debug.LogError("[Developerworks SDK] SDK Authentication Failed. Cannot proceed.");
                return false;
            }

            _chatProvider = new Provider.AI.AIChatProvider(_dwAuthManager);
            _imageProvider = new Provider.AI.AIImageProvider(_dwAuthManager);
            _objectProvider = new Provider.AI.AIObjectProvider(_dwAuthManager);
            _isInitialized = true;
            Debug.Log("[Developerworks SDK] Developerworks_SDK Initialized Successfully");
            return true;
        }

        /// <summary>
        /// Gets the PlayerClient for querying user information and managing player data.
        /// This can be used to check user credits, get user info, etc.
        /// </summary>
        /// <returns>The PlayerClient instance, or null if SDK not initialized or user not authenticated</returns>
        public static DW_PlayerClient GetPlayerClient()
        {
            if (!_isInitialized || _dwAuthManager == null)
            {
                Debug.LogWarning("SDK not initialized. Please call DW_SDK.InitializeAsync() first.");
                return null;
            }

            return _dwAuthManager.GetPlayerClient();
        }

        /// <summary>
        /// Checks if the SDK is initialized and the user is authenticated
        /// </summary>
        /// <returns>True if ready to use, false otherwise</returns>
        public static bool IsReady()
        {
            return _isInitialized && _dwAuthManager != null;
        }

        public static class Factory
        {
            /// <summary>
            /// Creates a standard chat client with both text and structured output capabilities
            /// </summary>
            public static DW_AIChatClient CreateChatClient(string modelName = null)
            {
                if (!Instance)
                {
                    Debug.LogError("Please place DW_SDK object in your FIRST scene.");
                }
                if (!_isInitialized)
                {
                    Debug.LogError("SDK not initialized. Please call DW_SDK.InitializeAsync() and wait for it to complete first.");
                    return null;
                }
                
                string model = modelName ?? Instance.defaultChatModel;
                var chatService = new Services.ChatService(_chatProvider);
                return new DW_AIChatClient(model, chatService, _objectProvider);
            }

            /// <summary>
            /// Creates an image generation client for AI-powered image creation
            /// </summary>
            public static DW_AIImageClient CreateImageClient(string modelName = null)
            {
                if (!Instance)
                {
                    Debug.LogError("Please place DW_SDK object in your FIRST scene.");
                    return null;
                }
                if (!_isInitialized)
                {
                    Debug.LogError("SDK not initialized. Please call DW_SDK.InitializeAsync() and wait for it to complete first.");
                    return null;
                }
                
                string model = modelName ?? Instance.defaultImageModel;
                if (string.IsNullOrEmpty(model))
                {
                    Debug.LogError("No image model specified. Please set defaultImageModel in DW_SDK or provide a model name.");
                    return null;
                }
                
                return new DW_AIImageClient(model, _imageProvider);
            }

        }

        public static class Populate
        {
            /// <summary>
            /// Set up a NPC client that automatically manages conversation history.
            /// This is a simplified interface perfect for game NPCs and characters.
            /// </summary>
            /// <param name="recipient">The NPC Object</param>
            /// <param name="modelName">Optional specific model to use</param>
            /// <returns>An NPC client ready for conversation</returns>
            public static void CreateNpc(DW_NPCClient recipient, string modelName = null)
            {
                if (!Instance)
                {
                    Debug.LogError("Please place DW_SDK object in your FIRST scene.");
                }
                if (!_isInitialized)
                {
                    Debug.LogError("SDK not initialized. Please call DW_SDK.InitializeAsync() and wait for it to complete first.");
                    return;
                }

                // Create underlying chat client
                var chatClient = Factory.CreateChatClient(modelName);
                if (chatClient == null)
                {
                    return;
                }

                recipient.Setup(chatClient);
            }
        }
    }
}
