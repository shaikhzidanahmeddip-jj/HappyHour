using MelonLoader;
using Steamworks;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace HappyHour.Features
{
    public static class UpdateNotifier
    {
        private const string RepoOwner = "shaikhzidanahmeddip-jj";
        private const string RepoName = "HappyHour";
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/shaikhzidanahmeddip-jj/HappyHour/releases/latest";
        private const string FallbackReleaseUrl = "https://github.com/shaikhzidanahmeddip-jj/HappyHour/releases/latest";

        private static bool initialized;
        private static bool updateAvailable;
        private static bool dismissed;
        private static bool checkCompleted;
        private static bool checkFailed;
        private static string currentVersionLabel = "unknown";
        private static string latestVersionLabel = "unknown";
        private static string latestReleaseUrl = FallbackReleaseUrl;

        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            currentVersionLabel = GetCurrentVersionLabel();
            MelonCoroutines.Start(CheckLatestRelease());
        }

        public static void DrawNotification()
        {
            if (!updateAvailable || dismissed)
                return;

            const float width = 430f;
            const float height = 96f;
            float x = Screen.width - width - 16f;
            float y = 16f;

            Rect boxRect = new Rect(x, y, width, height);
            GUI.Box(boxRect, GUIContent.none);

            GUI.Label(new Rect(x + 10f, y + 8f, width - 20f, 20f), "HappyHour update available");
            GUI.Label(new Rect(x + 10f, y + 30f, width - 20f, 20f), $"Current: {currentVersionLabel}   Latest: {latestVersionLabel}");

            if (GUI.Button(new Rect(x + 10f, y + 58f, 120f, 28f), "View release"))
            {
                SteamFriends.ActivateGameOverlayToWebPage(latestReleaseUrl);
            }

            if (GUI.Button(new Rect(x + width - 90f, y + 58f, 80f, 28f), "Dismiss"))
            {
                dismissed = true;
            }
        }

        public static string GetSettingsStatusText()
        {
            if (!checkCompleted)
                return $"Checking... (Current: {currentVersionLabel})";

            if (checkFailed)
                return $"Check failed (Current: {currentVersionLabel})";

            if (updateAvailable)
                return $"New version available ({currentVersionLabel} -> {latestVersionLabel})";

            return $"Up to date ({currentVersionLabel})";
        }

        private static IEnumerator CheckLatestRelease()
        {
            using var request = UnityWebRequest.Get(LatestReleaseApiUrl);
            request.SetRequestHeader("User-Agent", "HappyHour-Mod-Updater");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                checkCompleted = true;
                checkFailed = true;
                yield break;
            }

            string responseBody = request.downloadHandler.text;
            string tagName = ExtractJsonString(responseBody, "tag_name");
            if (string.IsNullOrWhiteSpace(tagName))
            {
                checkCompleted = true;
                checkFailed = true;
                yield break;
            }

            latestVersionLabel = tagName;

            string htmlUrl = ExtractJsonString(responseBody, "html_url");
            latestReleaseUrl = string.IsNullOrWhiteSpace(htmlUrl) ? FallbackReleaseUrl : htmlUrl;

            if (!TryParseVersion(currentVersionLabel, out System.Version currentVersion))
            {
                checkCompleted = true;
                checkFailed = true;
                yield break;
            }

            if (!TryParseVersion(tagName, out System.Version latestVersion))
            {
                checkCompleted = true;
                checkFailed = true;
                yield break;
            }

            updateAvailable = latestVersion > currentVersion;
            checkCompleted = true;
            checkFailed = false;
        }

        private static string GetCurrentVersionLabel()
        {
            System.Version version = typeof(Core).Assembly.GetName().Version;
            if (version == null)
                return "0.0.0";

            return version.ToString(3);
        }

        private static bool TryParseVersion(string raw, out System.Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string cleaned = raw.Trim();
            if (cleaned.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(1);

            int dash = cleaned.IndexOf('-');
            if (dash > 0)
                cleaned = cleaned.Substring(0, dash);

            return System.Version.TryParse(cleaned, out version);
        }

        private static string ExtractJsonString(string json, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(fieldName))
                return null;

            string token = $"\"{fieldName}\"";
            int keyIndex = json.IndexOf(token, StringComparison.Ordinal);
            if (keyIndex < 0)
                return null;

            int colonIndex = json.IndexOf(':', keyIndex + token.Length);
            if (colonIndex < 0)
                return null;

            int firstQuote = json.IndexOf('"', colonIndex + 1);
            if (firstQuote < 0)
                return null;

            int secondQuote = json.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0)
                return null;

            return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }
    }
}