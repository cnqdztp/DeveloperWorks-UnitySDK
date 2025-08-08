using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Developerworks_SDK.Auth
{
    public class DW_AuthManager : MonoBehaviour
    {
        // CHANGED: Keys are now more specific to "PlayerToken"
        private const string PlayerTokenKey = "DW_SDK_PlayerToken";
        private const string TokenExpiryKey = "DW_SDK_TokenExpiry";
        private string _publishableKey;
        public string PublishableKey { get=>_publishableKey; }
        public string AuthToken { get; private set; }
        public bool IsDeveloperToken { get; private set; }

        [SerializeField]private DW_PlayerClient _playerClient;
        public DW_PlayerClient PlayerClient { get => _playerClient; }

        public void Setup(string publishableKey, string developerToken = null)
        {
            _publishableKey = publishableKey;
            Debug.Log("[Developerworks SDK] Initializing authentication with the following game id: "+_publishableKey);
            if (!string.IsNullOrEmpty(developerToken))
            {
                AuthToken = developerToken;
                IsDeveloperToken = true;
            }
            
            
        }

        public async UniTask<bool> AuthenticateAsync()
        {
            // If using a developer token, authentication is always considered successful.
            if (IsDeveloperToken)
            {
                Debug.Log("[Developerworks SDK] Using developer token. Authentication successful.");
                return true;
            }

            LoadPlayerToken();

            if (IsTokenValid())
            {
                Debug.Log("[Developerworks SDK] Existing valid player token found.");
                return true;
            }

            Debug.Log("[Developerworks SDK] No valid player token found. Initiating login process.");
            return await ShowLoginWebAsync();
        }

        private async UniTask<bool> ShowLoginWebAsync()
        {
            var loginWebPrefab = Resources.Load<GameObject>("LoginWeb");
            if (loginWebPrefab == null)
            {
                Debug.LogError("[Developerworks SDK] LoginWeb prefab not found in Resources folder!");
                return false;
            }

            var loginWebInstance = GameObject.Instantiate(loginWebPrefab);
            var authFlowManager = loginWebInstance.GetComponent<DW_AuthFlowManager>();
            if (authFlowManager == null)
            {
                Debug.LogError("[Developerworks SDK] AuthFlowManager component not found on the LoginWeb prefab!");
                GameObject.Destroy(loginWebInstance);
                return false;
            }
            
            // ADDED: Pass the AuthManager reference so the flow can use our PlayerClient
            authFlowManager.AuthManager = this;

            // Wait until the AuthFlowManager reports success (i.e., it has acquired and saved a player token).
            await UniTask.WaitUntil(() => authFlowManager.IsSuccess, cancellationToken: loginWebInstance.GetCancellationTokenOnDestroy());
            
            bool success = authFlowManager.IsSuccess;

            // Clean up the login UI.
            Destroy(loginWebInstance);

            if (success)
            {
                // The flow was successful, so a new token should have been saved. Load it.
                LoadPlayerToken();
                return IsTokenValid();
            }

            Debug.LogError("[Developerworks SDK] Login flow did not complete successfully.");
            return false;
        }

        private void LoadPlayerToken()
        {
            // Do not overwrite a developer token.
            if (IsDeveloperToken) return;

            AuthToken = PlayerPrefs.GetString(PlayerTokenKey, null);
        }

        private bool IsTokenValid()
        {
            if (string.IsNullOrEmpty(AuthToken))
            {
                return false;
            }

            // Developer tokens are always considered valid.
            if (IsDeveloperToken)
            {
                return true;
            }

            // Check expiry for Player Tokens.
            string expiryString = PlayerPrefs.GetString(TokenExpiryKey, "0");
            if (long.TryParse(expiryString, out long expiryTicks))
            {
                 if (DateTime.UtcNow.Ticks > expiryTicks)
                 {
                    Debug.Log("[Developerworks SDK] Player token has expired.");
                    ClearPlayerToken(); // Clean up expired token
                    return false;
                 }
            }
            else
            {
                // If expiry date is not a valid long, treat it as invalid.
                return false;
            }

            return true;
        }
        
        // CHANGED: Renamed and updated to handle the Player Token and its specific expiry format.
        public static void SavePlayerToken(string token, string expiresAtString)
        {
            PlayerPrefs.SetString(PlayerTokenKey, token);

            // The API returns null for never-expiring tokens. We'll store a far-future date.
            // Otherwise, we parse the ISO 8601 date string.
            DateTime expiryDate = string.IsNullOrEmpty(expiresAtString) 
                ? DateTime.MaxValue 
                : DateTime.Parse(expiresAtString, null, System.Globalization.DateTimeStyles.RoundtripKind);
            
            PlayerPrefs.SetString(TokenExpiryKey, expiryDate.ToUniversalTime().Ticks.ToString());
            PlayerPrefs.Save();
            Debug.Log("[Developerworks SDK] New player token saved successfully.");
        }
        
        public static void ClearPlayerToken()
        {
            PlayerPrefs.DeleteKey(PlayerTokenKey);
            PlayerPrefs.DeleteKey(TokenExpiryKey);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Get access to the PlayerClient for querying user information.
        /// This should be called after successful authentication.
        /// </summary>
        /// <returns>The PlayerClient instance, or null if not authenticated</returns>
        public DW_PlayerClient GetPlayerClient()
        {
            // Only return the PlayerClient if we have a valid token
            if (IsTokenValid())
            {
                // If we have a saved player token but the PlayerClient doesn't have it, 
                // we should initialize it with the current token
                if (PlayerClient != null && !PlayerClient.HasValidPlayerToken() && !IsDeveloperToken)
                {
                    // Set the player token directly since we already have it
                    SetPlayerTokenInClient(AuthToken);
                }
                return PlayerClient;
            }
            return null;
        }
        
        /// <summary>
        /// Internal method to set the player token in the client when we load it from storage
        /// </summary>
        private void SetPlayerTokenInClient(string token)
        {
            if (PlayerClient != null && !string.IsNullOrEmpty(token))
            {
                PlayerClient.SetPlayerToken(token);
                Debug.Log($"[DW_AuthManager] Player token loaded from storage and set in PlayerClient");
            }
        }
    }
}