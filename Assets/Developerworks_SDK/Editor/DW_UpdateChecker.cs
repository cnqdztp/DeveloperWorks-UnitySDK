using System;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Developerworks_SDK.Editor
{
    /// <summary>
    /// Automatically checks for SDK updates on Unity Editor startup
    /// </summary>
    [InitializeOnLoad]
    public static class DW_UpdateChecker
    {
        private const string VERSION_API_URL = "https://developerworks.agentlandlab.com/api/sdk/version/unity";
        private const string DOWNLOAD_URL = "https://developerworks.agentlandlab.com";
        private const string LAST_CHECK_KEY = "DW_SDK_LastUpdateCheck";
        private const string SKIP_VERSION_KEY = "DW_SDK_SkipVersion";

        static DW_UpdateChecker()
        {
            // Delay the check slightly to avoid interfering with Unity startup
            EditorApplication.delayCall += () => CheckForUpdatesAuto();
        }

        [MenuItem("Developerworks SDK/Check for Updates")]
        private static void CheckForUpdatesManual()
        {
            CheckForUpdates(true);
        }

        private static void CheckForUpdatesAuto()
        {
            // Check if we should auto-check (once per day)
            string lastCheckStr = EditorPrefs.GetString(LAST_CHECK_KEY, "");
            if (!string.IsNullOrEmpty(lastCheckStr))
            {
                if (DateTime.TryParse(lastCheckStr, out DateTime lastCheck))
                {
                    if ((DateTime.Now - lastCheck).TotalHours < 24)
                    {
                        return; // Already checked today
                    }
                }
            }

            CheckForUpdates(false);
        }

        private static async UniTaskVoid CheckForUpdates(bool isManual)
        {
            using (var webRequest = UnityWebRequest.Get(VERSION_API_URL))
            {
                var operation = webRequest.SendWebRequest();

                // Wait for completion
                while (!operation.isDone)
                {
                    await System.Threading.Tasks.Task.Delay(100);
                }

                // Update last check time
                EditorPrefs.SetString(LAST_CHECK_KEY, DateTime.Now.ToString());

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    if (isManual)
                    {
                        EditorUtility.DisplayDialog(
                            "Update Check Failed",
                            $"Failed to check for updates:\n{webRequest.error}",
                            "OK"
                        );
                    }
                    return;
                }

                try
                {
                    var response = JsonUtility.FromJson<VersionResponse>(webRequest.downloadHandler.text);

                    if (response == null || string.IsNullOrEmpty(response.version))
                    {
                        if (isManual)
                        {
                            EditorUtility.DisplayDialog(
                                "Update Check Failed",
                                "Invalid response from version server.",
                                "OK"
                            );
                        }
                        return;
                    }

                    string currentVersion = DW_SDK.VERSION;
                    string latestVersion = response.version;

                    // Check if user has chosen to skip this version
                    string skipVersion = EditorPrefs.GetString(SKIP_VERSION_KEY, "");
                    if (!isManual && skipVersion == latestVersion)
                    {
                        return; // User chose to skip this version
                    }

                    int comparison = CompareVersions(currentVersion, latestVersion);

                    if (comparison < 0)
                    {
                        // New version available
                        string message = $"A new version of Developerworks SDK is available!\n\n" +
                                       $"Current Version: {currentVersion}\n" +
                                       $"Latest Version: {latestVersion}\n";

                        if (!string.IsNullOrEmpty(response.name))
                        {
                            message += $"Release Name: {response.name}\n";
                        }

                        if (!string.IsNullOrEmpty(response.publishedAt))
                        {
                            message += $"Published: {response.publishedAt}\n";
                        }

                        if (!string.IsNullOrEmpty(response.body))
                        {
                            message += $"\n{response.body}\n";
                        }

                        message += $"\nWould you like to visit Developerworks to download the latest version?";

                        int option = EditorUtility.DisplayDialogComplex(
                            "SDK Update Available",
                            message,
                            "Download Now",
                            "Skip This Version",
                            "Remind Me Later"
                        );

                        switch (option)
                        {
                            case 0: // Download Now
                                Application.OpenURL(DOWNLOAD_URL);
                                break;
                            case 1: // Skip This Version
                                EditorPrefs.SetString(SKIP_VERSION_KEY, latestVersion);
                                break;
                            case 2: // Remind Me Later
                                // Do nothing, will check again tomorrow
                                break;
                        }
                    }
                    else if (isManual)
                    {
                        // Only show "up to date" message for manual checks
                        EditorUtility.DisplayDialog(
                            "No Updates Available",
                            $"You are using the latest version ({currentVersion}).",
                            "OK"
                        );
                    }
                }
                catch (Exception ex)
                {
                    if (isManual)
                    {
                        EditorUtility.DisplayDialog(
                            "Update Check Failed",
                            $"Failed to parse version information:\n{ex.Message}",
                            "OK"
                        );
                    }
                    Debug.LogError($"[Developerworks SDK] Update check failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Compares two version strings. Supports semantic versioning with optional prefixes and suffixes.
        /// </summary>
        /// <returns>-1 if v1 < v2, 0 if equal, 1 if v1 > v2</returns>
        private static int CompareVersions(string v1, string v2)
        {
            var parsed1 = ParseVersion(v1);
            var parsed2 = ParseVersion(v2);

            // Compare major.minor.patch.build
            for (int i = 0; i < 4; i++)
            {
                if (parsed1.numbers[i] < parsed2.numbers[i]) return -1;
                if (parsed1.numbers[i] > parsed2.numbers[i]) return 1;
            }

            // If numbers are equal, compare suffixes (beta < alpha < stable)
            return CompareSuffixes(parsed1.suffix, parsed2.suffix);
        }

        private static (int[] numbers, string suffix) ParseVersion(string version)
        {
            // Remove 'v' prefix if present
            version = version.TrimStart('v', 'V');

            // Extract suffix (e.g., "-beta", "-alpha")
            string suffix = "";
            var match = Regex.Match(version, @"-(.+)$");
            if (match.Success)
            {
                suffix = match.Groups[1].Value.ToLower();
                version = version.Substring(0, match.Index);
            }

            // Parse version numbers
            var parts = version.Split('.');
            int[] numbers = new int[4]; // major.minor.patch.build

            for (int i = 0; i < Math.Min(parts.Length, 4); i++)
            {
                if (int.TryParse(parts[i], out int num))
                {
                    numbers[i] = num;
                }
            }

            return (numbers, suffix);
        }

        private static int CompareSuffixes(string s1, string s2)
        {
            // Empty suffix (stable) > alpha/beta
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 0;
            if (string.IsNullOrEmpty(s1)) return 1;  // stable > prerelease
            if (string.IsNullOrEmpty(s2)) return -1; // prerelease < stable

            // beta > alpha
            if (s1.Contains("beta") && s2.Contains("alpha")) return 1;
            if (s1.Contains("alpha") && s2.Contains("beta")) return -1;

            // Otherwise compare alphabetically
            return string.Compare(s1, s2, StringComparison.Ordinal);
        }

        [Serializable]
        private class VersionResponse
        {
            public string version;
            public string publishedAt;
            public string name;
            public string body;
        }
    }
}
