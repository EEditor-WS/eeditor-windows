using Microsoft.Web.WebView2.Core;
using NetDiscordRpc;
using NetDiscordRpc.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;

namespace EEditor
{
    public class ConfigData
    {
        public string ServerUrl { get; set; } = "";
        // 1. Добавляем поле для хранения пути к игре в конфиге
        public string GameFolderPath { get; set; } = "";
    }

    [ComVisible(true)]
    public class BackupManagerBridge
    {
        private readonly string backupFolder;

        public BackupManagerBridge()
        {
            backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
            if (!Directory.Exists(backupFolder))
                Directory.CreateDirectory(backupFolder);
        }

        public string[] GetBackupFiles()
        {
            return Directory.GetFiles(backupFolder)
                            .Select(path => Path.GetFileName(path))
                            .ToArray();
        }

        public void SaveBackupFile(string fileName, string content)
        {
            string path = Path.Combine(backupFolder, fileName);
            File.WriteAllText(path, content);
        }

        public string ReadBackupFile(string fileName)
        {
            string path = Path.Combine(backupFolder, fileName);
            if (File.Exists(path))
                return File.ReadAllText(path);
            return null;
        }
    }

    [ComVisible(true)]
    public class FileSystemBridge
    {
        private string selectedFolderPath;
        private static readonly HttpClient httpClient = new HttpClient();
        // 2. Добавляем ссылку на главное окно, чтобы обновлять и сохранять конфиг
        private readonly MainWindow mainWindow;

        public FileSystemBridge(MainWindow window)
        {
            mainWindow = window;
            // Инициализируем путь из уже загруженного конфига
            selectedFolderPath = mainWindow.Config?.GameFolderPath;
        }

        private string GetRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(selectedFolderPath))
                return fullPath;

            var basePath = selectedFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var path = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                var relative = path.Substring(basePath.Length);
                if (relative.StartsWith(Path.DirectorySeparatorChar.ToString()) ||
                    relative.StartsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    relative = relative.Substring(1);
                }
                return relative;
            }

            return fullPath;
        }

        private string GetAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(selectedFolderPath))
                return relativePath;

            return Path.Combine(selectedFolderPath, relativePath);
        }

        public async Task<string> SelectFolderAsync()
        {
            string result = null;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
#if NET8_0_OR_GREATER
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Выберите папку игры"
                };
                if (dialog.ShowDialog() == true)
                {
                    selectedFolderPath = dialog.FolderName;
                    result = selectedFolderPath;
                }
#else
                using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    fbd.Description = "Выберите папку игры";
                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        selectedFolderPath = fbd.SelectedPath;
                        result = selectedFolderPath;
                    }
                }
#endif
            });

            // 3. Если папка успешно выбрана, сохраняем её в конфигурацию
            if (!string.IsNullOrEmpty(result) && mainWindow.Config != null)
            {
                mainWindow.Config.GameFolderPath = result;
                mainWindow.SaveConfig();
            }

            return result;
        }

        public async Task<string[]> GetMapAndJsonFilesAsync()
        {
            if (string.IsNullOrEmpty(selectedFolderPath) || !Directory.Exists(selectedFolderPath))
            {
                await SelectFolderAsync();
                if (string.IsNullOrEmpty(selectedFolderPath))
                {
                    return new string[0];
                }
            }

            try
            {
                return Directory.EnumerateFiles(selectedFolderPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".map", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    .Select(f => GetRelativePath(f))
                    .ToArray();
            }
            catch
            {
                return new string[0];
            }
        }

        public async Task<bool> DeleteFileAsync(string relativePath)
        {
            try
            {
                string fullPath = GetAbsolutePath(relativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DownloadFileAsync(string url, string relativePath)
        {
            try
            {
                string fullPath = GetAbsolutePath(relativePath);
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (var stream = await httpClient.GetStreamAsync(url))
                {
                    using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> ReadFileAsync(string relativePath)
        {
            try
            {
                string fullPath = GetAbsolutePath(relativePath);
                if (File.Exists(fullPath))
                {
                    return await Task.Run(() => File.ReadAllText(fullPath, Encoding.UTF8));
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> WriteFileAsync(string relativePath, string content)
        {
            try
            {
                string fullPath = GetAbsolutePath(relativePath);
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await Task.Run(() => File.WriteAllText(fullPath, content, Encoding.UTF8));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetGroupedMapsAsync(string libLink)
        {
            if (string.IsNullOrEmpty(selectedFolderPath) || !Directory.Exists(selectedFolderPath))
            {
                await SelectFolderAsync();
                if (string.IsNullOrEmpty(selectedFolderPath)) return "{}";
            }

            string mapsDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mapsData.json");
            Dictionary<string, JsonNode> mapsData = new Dictionary<string, JsonNode>();
            if (File.Exists(mapsDataPath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(mapsDataPath);
                    mapsData = JsonSerializer.Deserialize<Dictionary<string, JsonNode>>(json) ?? new Dictionary<string, JsonNode>();
                }
                catch { mapsData = new Dictionary<string, JsonNode>(); }
            }

            // Находим все .map и .json файлы с относительными путями
            var mapFiles = Directory.EnumerateFiles(selectedFolderPath, "*.map", SearchOption.AllDirectories)
                .Select(f => new {
                    FullName = f,
                    RelativePath = GetRelativePath(f),
                    NameWithoutExt = Path.GetFileNameWithoutExtension(f)
                })
                .OrderByDescending(m => m.NameWithoutExt.Length)
                .ToList();

            var jsonFiles = Directory.EnumerateFiles(selectedFolderPath, "*.json", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).Equals("mapsData.json", StringComparison.OrdinalIgnoreCase))
                .Select(f => new {
                    FullName = f,
                    RelativePath = GetRelativePath(f),
                    NameWithoutExt = Path.GetFileNameWithoutExtension(f)
                })
                .ToList();

            string mapsJsonUrl = libLink.TrimEnd('/') + "/maps.json";
            string remoteMapsJson = null;
            bool mapsDataChanged = false;

            var jsonFilesData = new List<(string RelativePath, string NameWithoutExt, string MapHash)>();

            foreach (var jsonFile in jsonFiles)
            {
                string mapHash = null;
                try
                {
                    string content = await File.ReadAllTextAsync(jsonFile.FullName);
                    using (JsonDocument doc = JsonDocument.Parse(content))
                    {
                        if (doc.RootElement.TryGetProperty("map_hash", out var hashProp))
                        {
                            mapHash = hashProp.ToString();
                        }
                    }
                }
                catch { }

                jsonFilesData.Add((jsonFile.RelativePath, jsonFile.NameWithoutExt, mapHash));

                if (!string.IsNullOrEmpty(mapHash) && !mapsData.ContainsKey(mapHash))
                {
                    if (remoteMapsJson == null)
                    {
                        try { remoteMapsJson = await httpClient.GetStringAsync(mapsJsonUrl); }
                        catch { remoteMapsJson = "[]"; }
                    }

                    using (JsonDocument remoteDoc = JsonDocument.Parse(remoteMapsJson))
                    {
                        JsonElement? foundData = null;

                        if (remoteDoc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in remoteDoc.RootElement.EnumerateArray())
                            {
                                if (IsHashMatch(item, mapHash)) { foundData = item; break; }
                            }
                        }
                        else if (remoteDoc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            if (remoteDoc.RootElement.TryGetProperty(mapHash, out var directMatch)) foundData = directMatch;
                            else
                            {
                                foreach (var prop in remoteDoc.RootElement.EnumerateObject())
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.Object && IsHashMatch(prop.Value, mapHash))
                                    { foundData = prop.Value; break; }
                                }
                            }
                        }

                        if (foundData.HasValue)
                        {
                            mapsData[mapHash] = JsonNode.Parse(foundData.Value.GetRawText());
                            mapsDataChanged = true;
                        }
                    }
                }
            }

            if (mapsDataChanged)
            {
                try
                {
                    string updatedJson = JsonSerializer.Serialize(mapsData, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(mapsDataPath, updatedJson);
                }
                catch { }
            }
            else if (!File.Exists(mapsDataPath))
            {
                try { File.WriteAllText(mapsDataPath, "{}"); } catch { }
            }

            // Группировка файлов
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var mapFile in mapFiles)
            {
                // Используем относительный путь без расширения как ключ
                string mapKey = Path.ChangeExtension(mapFile.RelativePath, null);
                result[mapKey] = new List<string>();
            }

            foreach (var jsonData in jsonFilesData)
            {
                string jsonRelativePath = jsonData.RelativePath;
                string jsonName = jsonData.NameWithoutExt;
                string mapHash = jsonData.MapHash;
                bool matched = false;

                int underscoreCount = jsonName.Count(c => c == '_');
                bool useNameSearch = underscoreCount >= 3;

                if (useNameSearch)
                {
                    // Ищем .map файл, чьё имя (без расширения) является префиксом имени .json файла
                    var matchedMap = mapFiles.FirstOrDefault(m =>
                        jsonName.StartsWith(m.NameWithoutExt + "_", StringComparison.OrdinalIgnoreCase) ||
                        jsonName.StartsWith(m.NameWithoutExt.Replace("_!", "") + "_", StringComparison.OrdinalIgnoreCase));

                    if (matchedMap != null)
                    {
                        string mapKey = Path.ChangeExtension(matchedMap.RelativePath, null);
                        // Используем относительный путь .json файла (без расширения)
                        string jsonKey = Path.ChangeExtension(jsonRelativePath, null);
                        result[mapKey].Add(jsonKey);
                        matched = true;
                    }
                }

                if (!matched && !string.IsNullOrEmpty(mapHash))
                {
                    if (mapsData.TryGetValue(mapHash, out JsonNode rawMapData))
                    {
                        // Получаем список возможных имён карт (с учётом версий)
                        var possibleBaseNames = GetPossibleBaseNamesFromMapData(rawMapData);

                        foreach (var baseName in possibleBaseNames)
                        {
                            // Ищем .map файл с таким именем (с учётом суффикса _!)
                            var mapByHash = mapFiles.FirstOrDefault(m =>
                                string.Equals(m.NameWithoutExt, baseName, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(m.NameWithoutExt, baseName + "_!", StringComparison.OrdinalIgnoreCase));

                            if (mapByHash != null)
                            {
                                string mapKey = Path.ChangeExtension(mapByHash.RelativePath, null);
                                string jsonKey = Path.ChangeExtension(jsonRelativePath, null);
                                result[mapKey].Add(jsonKey);
                                break;
                            }
                        }
                    }
                }
            }

            var filteredResult = result.Where(kvp => kvp.Value.Count > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            foreach (var key in filteredResult.Keys.ToList())
            {
                filteredResult[key] = filteredResult[key].OrderBy(x => x).ToList();
            }

            return JsonSerializer.Serialize(filteredResult);
        }

        private bool IsHashMatch(JsonElement item, string targetHash)
        {
            if (item.TryGetProperty("hash", out var h) && h.ToString() == targetHash) return true;
            if (item.TryGetProperty("map_hash", out var mh) && mh.ToString() == targetHash) return true;

            if (item.TryGetProperty("versions", out var versions) && versions.ValueKind == JsonValueKind.Array)
            {
                foreach (var ver in versions.EnumerateArray())
                {
                    if (ver.ValueKind == JsonValueKind.Array && ver.GetArrayLength() >= 2)
                    {
                        if (ver[1].ToString() == targetHash) return true;
                    }
                }
            }
            return false;
        }

        // Возвращает список возможных имён карты (с учётом всех версий)
        private List<string> GetPossibleBaseNamesFromMapData(JsonNode node)
        {
            var result = new List<string>();
            try
            {
                if (node["id"] is JsonArray idArray)
                {
                    var idParts = new List<string>();
                    foreach (var part in idArray)
                    {
                        if (part != null) idParts.Add(part.ToString());
                    }
                    string baseId = string.Join("_", idParts);

                    // Добавляем базовое имя (без версии)
                    result.Add(baseId);

                    // Если есть versions, добавляем варианты с версиями
                    if (node["versions"] is JsonArray versionsArray)
                    {
                        foreach (var ver in versionsArray)
                        {
                            if (ver is JsonArray verArray && verArray.Count > 0)
                            {
                                string version = verArray[0]?.ToString();
                                if (!string.IsNullOrEmpty(version))
                                {
                                    result.Add(baseId + "_" + version);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return result;
        }
    }

    [ComVisible(true)]
    public partial class MainWindow : Window
    {
        private const string applicationId = "1333948751919972434";
        private DiscordRPC client;
        private string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        // 4. Делаем свойство Config публичным/внутренним для доступа из FileSystemBridge
        public ConfigData Config { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            InitializeDiscordRPC();
            InitializeWebView();
            this.Closed += Window_Closed;
        }

        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    Config = JsonSerializer.Deserialize<ConfigData>(json);
                }
                catch
                {
                    Config = new ConfigData();
                }
            }
            else
            {
                Config = new ConfigData();
            }

            // Защита на случай, если файл конфигурации существовал, но поле GameFolderPath в нем отсутствовало (было null)
            if (Config.GameFolderPath == null)
            {
                Config.GameFolderPath = "";
            }

            if (string.IsNullOrEmpty(Config.ServerUrl))
            {
                string message =
@"Выберите сервер / Choise server:
1. Россия
2. International 1";

                var input = Microsoft.VisualBasic.Interaction.InputBox(message, "Выбор сервера", "2");

                switch (input.Trim())
                {
                    case "1":
                        Config.ServerUrl = "https://eeditor-ws.github.io/";
                        break;
                    case "2":
                        Config.ServerUrl = "https://eeditor-ws.vercel.app/";
                        break;
                    default:
                        Config.ServerUrl = "https://eeditor-ws.vercel.app/";
                        break;
                }

                SaveConfig();
            }
        }

        // 5. Делаем метод публичным, чтобы мост мог вызывать сохранение файла
        public void SaveConfig()
        {
            string json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }

        private void InitializeDiscordRPC()
        {
            client = new DiscordRPC(applicationId);
            client.Initialize();

            client.SetPresence(new RichPresence
            {
                Details = "Better Warnament's scenario editor",
                State = "Загрузка...",
                Assets = new Assets
                {
                    LargeImageKey = "icob",
                    LargeImageText = "EEditor"
                },
                Timestamps = Timestamps.Now
            });
        }

        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async(null);

            string remoteUrl = Config.ServerUrl;

            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "index.html");
            string localUri = new Uri(localPath).AbsoluteUri;

            bool isRemoteAvailable = await CheckRemoteAvailable(remoteUrl);
            string targetUrl = isRemoteAvailable ? remoteUrl : localUri;

            webView.CoreWebView2.AddHostObjectToScript("backupManager", new BackupManagerBridge());
            // 6. Передаем `this` (текущее окно) в конструктор FileSystemBridge
            webView.CoreWebView2.AddHostObjectToScript("fileSystem", new FileSystemBridge(this));

            webView.CoreWebView2.Navigate(targetUrl);
            webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            client?.SetPresence(new RichPresence
            {
                Details = "Better Warnament's scenario editor",
                State = isRemoteAvailable ? "Онлайн редактор" : "Локальная версия",
                Assets = new Assets
                {
                    LargeImageKey = "icob",
                    LargeImageText = "EEditor"
                },
                Timestamps = Timestamps.Now
            });
        }

        private async Task<bool> CheckRemoteAvailable(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    var response = await client.GetAsync(url);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            if (!string.IsNullOrEmpty(message))
            {
                UpdateDiscordStatus(message);
            }
        }

        private void UpdateDiscordStatus(string status)
        {
            client?.SetPresence(new RichPresence
            {
                Details = "Better Warnament's scenario editor",
                State = status,
                Assets = new Assets
                {
                    LargeImageKey = "icob",
                    LargeImageText = "EEditor"
                },
                Timestamps = Timestamps.Now
            });
        }

        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Console.WriteLine($"Ошибка загрузки страницы: {e.WebErrorStatus}");
                client?.SetPresence(new RichPresence
                {
                    Details = "Ошибка загрузки",
                    State = "Проблема с редактором"
                });
            }
            else
            {
                string script = @"
                    window.updateDiscordStatus = function(status) {
                        window.chrome.webview.postMessage(status);
                    };
                ";
                webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            client?.Dispose();
        }
    }
}