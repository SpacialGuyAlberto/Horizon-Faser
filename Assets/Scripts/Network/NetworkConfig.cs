using UnityEngine;
using System;
using System.IO;

namespace HorizonFaser.Network
{
    /// <summary>
    /// Centralized network and environment configuration for Unity Horizon-faser.
    /// Reads .env files (project root or Resources/env.txt) and supports runtime overrides via PlayerPrefs.
    /// </summary>
    public static class NetworkConfig
    {
        private const string PREF_SERVER_URL = "ServerBaseUrl";

        public static string LaptopIp { get; private set; } = "172.20.10.2";
        public static int ServerPort { get; private set; } = 8080;
        public static string GlassesIp { get; private set; } = "172.20.10.5";
        public static int UdpListenPort { get; private set; } = 8000;
        public static string ConfiguredServerUrl { get; private set; } = "http://172.20.10.2:8080";

        private static bool _envLoaded = false;

        static NetworkConfig()
        {
            LoadEnv();
        }

        public static void LoadEnv()
        {
            if (_envLoaded) return;
            _envLoaded = true;

            string content = null;

            // 1. Check persistentDataPath (custom pushed .env on Quest/device)
            try
            {
                string path = Path.Combine(Application.persistentDataPath, ".env");
                if (File.Exists(path))
                {
                    content = File.ReadAllText(path);
                }
            }
            catch (Exception) {}

            // 2. Check project root or current working directory (Unity Editor / Standalone)
            if (string.IsNullOrEmpty(content))
            {
                try
                {
                    string rootPath = Path.Combine(Application.dataPath, "../.env");
                    if (File.Exists(rootPath))
                    {
                        content = File.ReadAllText(rootPath);
                    }
                    else if (File.Exists(".env"))
                    {
                        content = File.ReadAllText(".env");
                    }
                }
                catch (Exception) {}
            }

            // 3. Fallback to bundled Resources/env.txt (inside Quest APK)
            if (string.IsNullOrEmpty(content))
            {
                try
                {
                    TextAsset textAsset = Resources.Load<TextAsset>("env");
                    if (textAsset != null && !string.IsNullOrEmpty(textAsset.text))
                    {
                        content = textAsset.text;
                    }
                }
                catch (Exception) {}
            }

            if (!string.IsNullOrEmpty(content))
            {
                ParseEnv(content);
            }
        }

        private static void ParseEnv(string content)
        {
            bool hasExplicitServerUrl = false;
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                int eqIdx = line.IndexOf('=');
                if (eqIdx == -1) continue;

                string key = line.Substring(0, eqIdx).Trim().ToUpperInvariant();
                string val = line.Substring(eqIdx + 1).Trim();

                switch (key)
                {
                    case "LAPTOP_IP":
                        LaptopIp = val;
                        break;
                    case "SERVER_PORT":
                        if (int.TryParse(val, out int sp)) ServerPort = sp;
                        break;
                    case "SERVER_URL":
                        ConfiguredServerUrl = val.TrimEnd('/');
                        hasExplicitServerUrl = true;
                        break;
                    case "GLASSES_IP":
                    case "HEADSET_IP":
                        GlassesIp = val;
                        break;
                    case "UDP_LISTEN_PORT":
                        if (int.TryParse(val, out int lp)) UdpListenPort = lp;
                        break;
                }
            }

            if (!hasExplicitServerUrl && !string.IsNullOrEmpty(LaptopIp))
            {
                ConfiguredServerUrl = $"http://{LaptopIp}:{ServerPort}";
            }
        }

        public static string GetDefaultUrl()
        {
            LoadEnv();
#if UNITY_EDITOR
            // In editor, if LaptopIp is 127.0.0.1 or localhost, use it; otherwise ConfiguredServerUrl
            return ConfiguredServerUrl;
#else
            return ConfiguredServerUrl;
#endif
        }

        public static string BaseUrl
        {
            get
            {
                LoadEnv();
                string saved = PlayerPrefs.GetString(PREF_SERVER_URL, "");
                if (!string.IsNullOrEmpty(saved))
                {
                    if (saved.Contains("fedora.fritz.box") || saved.Contains("tu-servidor.com") || saved.Contains("example.com"))
                    {
                        PlayerPrefs.DeleteKey(PREF_SERVER_URL);
                        PlayerPrefs.Save();
                        return GetDefaultUrl();
                    }
                    return saved.TrimEnd('/');
                }
                return GetDefaultUrl();
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    string cleaned = value.Trim().TrimEnd('/');
                    if (cleaned.Contains("fedora.fritz.box") || cleaned.Contains("tu-servidor.com") || cleaned.Contains("example.com"))
                    {
                        PlayerPrefs.DeleteKey(PREF_SERVER_URL);
                        PlayerPrefs.Save();
                        return;
                    }
                    PlayerPrefs.SetString(PREF_SERVER_URL, cleaned);
                    PlayerPrefs.Save();
                }
            }
        }

        public static string SseStreamUrl => $"{BaseUrl}/api/notifications/stream";

        public static string Host
        {
            get
            {
                try
                {
                    Uri uri = new Uri(BaseUrl);
                    return uri.Authority;
                }
                catch
                {
                    return $"{LaptopIp}:{ServerPort}";
                }
            }
        }

        public static string HostWithoutPort
        {
            get
            {
                try
                {
                    Uri uri = new Uri(BaseUrl);
                    return uri.Host;
                }
                catch
                {
                    return LaptopIp;
                }
            }
        }

        public static string ResolveUrl(string rawUrl)
        {
            if (string.IsNullOrEmpty(rawUrl)) return rawUrl;

            string currentHost = Host;
            return rawUrl.Replace("fedora.fritz.box:8080", currentHost)
                         .Replace("localhost:8080", currentHost)
                         .Replace("127.0.0.1:8080", currentHost);
        }
    }
}
