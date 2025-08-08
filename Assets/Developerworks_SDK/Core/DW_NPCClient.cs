using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Developerworks_SDK.Public;
using UnityEngine;

namespace Developerworks_SDK
{
    /// <summary>
    /// A simplified NPC chat client that automatically manages conversation history.
    /// This is a "sugar" wrapper around DW_AIChatClient for easier usage.
    /// </summary>
    public class DW_NPCClient : MonoBehaviour
    {
        [SerializeField] private string characterDesign;
        [SerializeField] private string chatModelName;
        private DW_AIChatClient _chatClient;
        private List<ChatMessage> _conversationHistory;
        private string _currentPrompt;
        private bool _isTalking;
        public bool IsTalking { get { return _isTalking; } }
        private bool _isReady;
        public bool IsReady { get { return _isReady; } }

        public void Setup (DW_AIChatClient chatClient)
        {
            _chatClient = chatClient;
            _isReady = true;
        }
        
        private void Start()
        {
            _conversationHistory = new List<ChatMessage>();
            Initialize().Forget();
        }

        private async UniTask Initialize()
        {
            await UniTask.WaitUntil(() => DW_SDK.IsReady());
            if(string.IsNullOrEmpty(characterDesign))
                SetSystemPrompt(characterDesign);
            DW_SDK.Populate.CreateNpc(this,chatModelName);
        }

        /// <summary>
        /// Send a message to the NPC and get a response.
        /// The conversation history is automatically managed.
        /// </summary>
        /// <param name="message">The message to send to the NPC</param>
        /// <returns>The NPC's response</returns>
        public async UniTask<string> Talk(string message)
        {
            _isTalking = true;
            await UniTask.WaitUntil(() => IsReady);
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogError("NPC client is not active");
                return null;
            }
            if (string.IsNullOrEmpty(message))
            {
                return null;
            }

            // Add user message to history
            _conversationHistory.Add(new ChatMessage
            {
                Role = "user",
                Content = message
            });

            try
            {
                // Create chat config with full conversation history
                var config = new ChatConfig(_conversationHistory.ToList());
                var result = await _chatClient.TextGenerationAsync(config);

                if (result.Success && !string.IsNullOrEmpty(result.Response))
                {
                    // Add assistant response to history
                    _conversationHistory.Add(new ChatMessage
                    {
                        Role = "assistant",
                        Content = result.Response
                    });
                    _isTalking = false;
                    return result.Response;
                }
                else
                {
                    _isTalking = false;
                    return null;
                }
            }
            catch (Exception ex)
            {
                _isTalking = false;

                UnityEngine.Debug.LogError($"[NPCClient] Error in Talk: {ex.Message}");
                return null;
            }

        }

        /// <summary>
        /// Send a message to the NPC and get a streaming response.
        /// The conversation history is automatically managed.
        /// </summary>
        /// <param name="message">The message to send to the NPC</param>
        /// <param name="onChunk">Called for each piece of the response as it streams in</param>
        /// <param name="onComplete">Called when the complete response is ready</param>
        public async UniTask TalkStream(string message, Action<string> onChunk, Action<string> onComplete)
        {
            _isTalking = true;
            await UniTask.WaitUntil(() => IsReady);
            if (string.IsNullOrEmpty(message))
            {
                onChunk?.Invoke(null);
                onComplete?.Invoke(null);
                return;
            }

            // Add user message to history
            _conversationHistory.Add(new ChatMessage
            {
                Role = "user",
                Content = message
            });

            try
            {
                var config = new ChatStreamConfig(_conversationHistory.ToList());
                
                await _chatClient.TextChatStreamAsync(config, 
                    chunk =>
                    {
                        // Forward each chunk to the caller
                        onChunk?.Invoke(chunk);
                    },
                    completeResponse =>
                    {
                        _isTalking = false;
                        // Add the complete response to conversation history
                        if (!string.IsNullOrEmpty(completeResponse))
                        {
                            _conversationHistory.Add(new ChatMessage
                            {
                                Role = "assistant",
                                Content = completeResponse
                            });
                        }
                        
                        // Notify caller that response is complete
                        onComplete?.Invoke(completeResponse);
                    }
                );
            }
            catch (Exception ex)
            {
                _isTalking = false;
                UnityEngine.Debug.LogError($"[NPCClient] Error in streaming Talk: {ex.Message}");
                onChunk?.Invoke(null);
                onComplete?.Invoke(null);
            }
        }

        /// <summary>
        /// Set the system prompt for the NPC character.
        /// This will update the conversation history with the new prompt.
        /// </summary>
        /// <param name="prompt">The new system prompt</param>
        public void SetSystemPrompt(string prompt)
        {
            _currentPrompt = prompt;
            
            // Remove existing system message if any
            for (int i = _conversationHistory.Count - 1; i >= 0; i--)
            {
                if (_conversationHistory[i].Role == "system")
                {
                    _conversationHistory.RemoveAt(i);
                }
            }
            
            // Add new system message if we have a prompt
            if (!string.IsNullOrEmpty(_currentPrompt))
            {
                _conversationHistory.Insert(0, new ChatMessage
                {
                    Role = "system",
                    Content = _currentPrompt
                });
            }
        }

        /// <summary>
        /// Revert the last exchange (user message and assistant response) from history.
        /// </summary>
        /// <returns>True if successfully reverted, false if no history to revert</returns>
        public bool RevertHistory()
        {
            // Find the last assistant message and the user message before it
            int lastAssistantIndex = -1;
            int lastUserIndex = -1;
            
            for (int i = _conversationHistory.Count - 1; i >= 0; i--)
            {
                if (_conversationHistory[i].Role == "assistant" && lastAssistantIndex == -1)
                {
                    lastAssistantIndex = i;
                }
                else if (_conversationHistory[i].Role == "user" && lastAssistantIndex != -1 && lastUserIndex == -1)
                {
                    lastUserIndex = i;
                    break;
                }
            }
            
            if (lastAssistantIndex != -1 && lastUserIndex != -1)
            {
                // Remove both messages (assistant first, then user)
                _conversationHistory.RemoveAt(lastAssistantIndex);
                _conversationHistory.RemoveAt(lastUserIndex);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Save the current conversation history to a serializable format.
        /// </summary>
        /// <returns>Serialized conversation data</returns>
        public string SaveHistory()
        {
            var saveData = new ConversationSaveData
            {
                Prompt = _currentPrompt,
                History = _conversationHistory.ToArray()
            };
            
            return UnityEngine.JsonUtility.ToJson(saveData);
        }

        /// <summary>
        /// Load conversation history from serialized data.
        /// </summary>
        /// <param name="saveData">Serialized conversation data</param>
        /// <returns>True if successfully loaded, false if data is invalid</returns>
        public bool LoadHistory(string saveData)
        {
            try
            {
                var data = UnityEngine.JsonUtility.FromJson<ConversationSaveData>(saveData);
                if (data == null) return false;
                
                _conversationHistory.Clear();
                
                // Set the prompt first
                SetSystemPrompt(data.Prompt);
                
                // Add all non-system messages (system message is already added by SetPrompt)
                foreach (var message in data.History)
                {
                    if (message.Role != "system")
                    {
                        _conversationHistory.Add(message);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NPCClient] Failed to load history: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear the conversation history, starting fresh.
        /// The system prompt (character) will be preserved.
        /// </summary>
        public void ClearHistory()
        {
            _conversationHistory.Clear();
            
            // Re-add system message if we have a prompt
            if (!string.IsNullOrEmpty(_currentPrompt))
            {
                _conversationHistory.Add(new ChatMessage
                {
                    Role = "system", 
                    Content = _currentPrompt
                });
            }
        }

        /// <summary>
        /// Get the current conversation history
        /// </summary>
        public ChatMessage[] GetHistory()
        {
            return _conversationHistory.ToArray();
        }

        /// <summary>
        /// Get the number of messages in the conversation history
        /// </summary>
        public int GetHistoryLength()
        {
            return _conversationHistory.Count;
        }

    }

    /// <summary>
    /// Data structure for saving and loading conversation history
    /// </summary>
    [System.Serializable]
    public class ConversationSaveData
    {
        public string Prompt;
        public ChatMessage[] History;
    }
}