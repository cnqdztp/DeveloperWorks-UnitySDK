using UnityEditor;
using UnityEngine;

namespace Developerworks_SDK.Auth
{
    /// <summary>
    /// Adds a menu item to the Unity Editor to help with debugging authentication.
    /// </summary>
    public static class DW_AuthMenu
    {
        /// <summary>
        /// Clears the locally stored Player Token using PlayerPrefs.
        /// </summary>
        [MenuItem("Developerworks SDK/Clear Local Player Token")]
        private static void ClearLocalPlayerToken()
        {
            // Call the static method from your existing AuthManager
            DW_AuthManager.ClearPlayerToken();
            
            // Log a confirmation message to the Unity Console
            Debug.Log("[Developerworks SDK] Local player token and expiry have been cleared from PlayerPrefs.");
        }
    }
}