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
using System.Windows.Controls;

namespace EEditor
{
    public class ConfigData
    {
        public string ServerUrl { get; set; } = "";
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
        private readonly MainWindow mainWindow;

        // === ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ (вынесены на уровень класса, так как в C# нельзя объявлять классы внутри методов) ===
        private class JsonFileData
        {
            public string RelativePath;
            public string NameWithoutExt;
            public string MapHash;
            public string ScenarioName;
            public int? ScenarioYear;
            public string ScenarioDesc;
        }

        private class MapInfo
        {
            public Dictionary<string, string> Versions { get; set; } = new Dictionary<string, string>();
            public List<ScenarioInfo> Scenarios { get; set; } = new List<ScenarioInfo>();
        }

        private class ScenarioInfo
        {
            public string File { get; set; }
            public string Name { get; set; }
            public int? Year { get; set; }
            public string Description { get; set; }
            public string MapVersion { get; set; }
        }

        private class MapPrefixInfo
        {
            public string CleanName;
            public string MapName;
            public string VersionId;
        }
        // =================================================================================

        public FileSystemBridge(MainWindow window)
        {
            mainWindow = window;
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
            const string MapHashProp = "map_hash";
            const string DisplayNameProp = "display_name";
            const string NameProp = "name";
            const string YearProp = "year";
            const string EditorProp = "editor";
            const string DescriptionProp = "description";
            const string DirtySuffix = "_!";
            const string VersionPrefix = "_v";
            const string OtherCategory = "other";

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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetGroupedMapsAsync] Error loading local cache: {ex.Message}");
                    mapsData = new Dictionary<string, JsonNode>();
                }
            }

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
            JsonDocument remoteDoc = null;
            bool mapsDataChanged = false;
            bool remoteFetchAttempted = false;

            var jsonFilesData = new List<JsonFileData>();

            foreach (var jsonFile in jsonFiles)
            {
                string mapHash = null;
                string scenarioName = null;
                int? scenarioYear = null;
                string scenarioDesc = null;

                try
                {
                    string fileContent = await File.ReadAllTextAsync(jsonFile.FullName);
                    using (var doc = JsonDocument.Parse(fileContent))
                    {
                        var root = doc.RootElement;

                        // Читаем map_hash, поддерживая и строку, и число
                        if (root.TryGetProperty(MapHashProp, out var hashProp))
                        {
                            if (hashProp.ValueKind == JsonValueKind.String)
                                mapHash = hashProp.GetString();
                            else if (hashProp.ValueKind == JsonValueKind.Number)
                                mapHash = hashProp.GetRawText(); // Число -> строка
                        }

                        if (root.TryGetProperty(NameProp, out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                            scenarioName = nameProp.GetString();
                        if (root.TryGetProperty(YearProp, out var yearProp) && yearProp.ValueKind == JsonValueKind.Number)
                            scenarioYear = yearProp.GetInt32();
                        if (root.TryGetProperty(EditorProp, out var editorProp) && editorProp.ValueKind == JsonValueKind.Object)
                        {
                            if (editorProp.TryGetProperty(DescriptionProp, out var descProp) && descProp.ValueKind == JsonValueKind.String)
                                scenarioDesc = descProp.GetString();
                        }
                    }

                    // Логируем найденный хеш
                    if (!string.IsNullOrEmpty(mapHash))
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetGroupedMapsAsync] Found map_hash '{mapHash}' in {jsonFile.NameWithoutExt}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetGroupedMapsAsync] Error parsing {jsonFile.FullName}: {ex.Message}");
                }

                jsonFilesData.Add(new JsonFileData
                {
                    RelativePath = jsonFile.RelativePath,
                    NameWithoutExt = jsonFile.NameWithoutExt,
                    MapHash = mapHash,
                    ScenarioName = scenarioName,
                    ScenarioYear = scenarioYear,
                    ScenarioDesc = scenarioDesc
                });

                if (!string.IsNullOrEmpty(mapHash) && !mapsData.ContainsKey(mapHash))
                {
                    if (remoteDoc == null && !remoteFetchAttempted)
                    {
                        remoteFetchAttempted = true;
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"[GetGroupedMapsAsync] Fetching remote maps from: {mapsJsonUrl}");
                            string remoteJson = await httpClient.GetStringAsync(mapsJsonUrl);
                            System.Diagnostics.Debug.WriteLine($"[GetGroupedMapsAsync] Remote maps fetched successfully. Length: {remoteJson.Length}");
                            remoteDoc = JsonDocument.Parse(remoteJson);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[GetGroupedMapsAsync] Error fetching remote maps: {ex.Message}");
                        }
                    }

                    if (remoteDoc != null)
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
                    else if (remoteFetchAttempted)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetGroupedMapsAsync] Warning: map_hash '{mapHash}' found in scenario, but remote maps could not be loaded.");
                    }
                }
            }

            remoteDoc?.Dispose();

            if (mapsDataChanged)
            {
                try
                {
                    string updatedJson = JsonSerializer.Serialize(mapsData, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(mapsDataPath, updatedJson);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetGroupedMapsAsync] Error saving local cache: {ex.Message}");
                }
            }
            else if (!File.Exists(mapsDataPath))
            {
                try { File.WriteAllText(mapsDataPath, "{}"); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetGroupedMapsAsync] Error creating empty cache: {ex.Message}");
                }
            }

            var priorityMapNames = new List<string> { "parkourcat_euro4", "zachary_world", "jalhund_europe", "eenot_asia" };
            var mapInfos = new Dictionary<string, MapInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var mapFile in mapFiles)
            {
                string fileName = mapFile.NameWithoutExt;
                string name = fileName.Replace(DirtySuffix, "");
                string version = "default";

                int lastVIndex = name.LastIndexOf(VersionPrefix, StringComparison.OrdinalIgnoreCase);
                if (lastVIndex != -1)
                {
                    string beforeV = name.Substring(0, lastVIndex);
                    string afterV = name.Substring(lastVIndex + VersionPrefix.Length);
                    int endIndex = afterV.IndexOf('_');
                    if (endIndex == -1) endIndex = afterV.Length;
                    string versionPart = afterV.Substring(0, endIndex);

                    if (!string.IsNullOrEmpty(versionPart))
                    {
                        version = "v" + versionPart;
                        name = beforeV;
                    }
                }

                string mapName = name;
                string versionId = string.IsNullOrEmpty(version) ? fileName.Replace(DirtySuffix, "") : version;

                if (!mapInfos.ContainsKey(mapName))
                    mapInfos[mapName] = new MapInfo();
                if (!mapInfos[mapName].Versions.ContainsKey(versionId))
                    mapInfos[mapName].Versions[versionId] = mapFile.RelativePath;
            }

            var mapPrefixLookup = new List<MapPrefixInfo>();
            foreach (var kvp in mapInfos)
            {
                foreach (var verKvp in kvp.Value.Versions)
                {
                    string clean = Path.GetFileNameWithoutExtension(verKvp.Value).Replace(DirtySuffix, "");
                    mapPrefixLookup.Add(new MapPrefixInfo { CleanName = clean, MapName = kvp.Key, VersionId = verKvp.Key });
                }
            }

            // Добавляем сами имена карт (без версий), чтобы префиксный поиск работал даже при отсутствии .map файлов
            foreach (var kvp in mapInfos)
            {
                if (!mapPrefixLookup.Any(p => p.CleanName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    mapPrefixLookup.Add(new MapPrefixInfo { CleanName = kvp.Key, MapName = kvp.Key, VersionId = null });
                }
            }
            mapPrefixLookup = mapPrefixLookup.OrderByDescending(x => x.CleanName.Length).ToList();

            var unmatchedJsonKeys = new List<string>();

            Func<string, string> GetMapNameFromHash = (hash) =>
            {
                if (string.IsNullOrEmpty(hash) || !mapsData.TryGetValue(hash, out var node)) return null;

                if (node is JsonObject obj)
                {
                    if (obj.TryGetPropertyValue(DisplayNameProp, out var disp) && disp != null && disp.GetValueKind() == JsonValueKind.String) return disp.ToString();
                    if (obj.TryGetPropertyValue(NameProp, out var name) && name != null && name.GetValueKind() == JsonValueKind.String) return name.ToString();
                }
                var possible = GetPossibleBaseNamesFromMapData(node);
                return possible.FirstOrDefault();
            };

            foreach (var jsonData in jsonFilesData)
            {
                string jsonPath = jsonData.RelativePath;
                string jsonName = jsonData.NameWithoutExt;
                string mapHash = jsonData.MapHash;
                string jsonKey = Path.ChangeExtension(jsonPath, null);

                string mapName = null;
                string versionId = null;

                // 2.1 Поиск по хешу
                if (!string.IsNullOrEmpty(mapHash))
                {
                    if (mapsData.TryGetValue(mapHash, out var mapNode) && mapNode is JsonObject mapObj)
                    {
                        string foundMapName = null;
                        string foundVersionId = null;

                        if (mapObj.TryGetPropertyValue("id", out var idNode) && idNode != null)
                        {
                            if (idNode is JsonArray idArray)
                            {
                                var idParts = new List<string>();
                                foreach (var part in idArray)
                                {
                                    if (part != null && part.GetValueKind() == JsonValueKind.String)
                                    {
                                        idParts.Add(part.ToString());
                                    }
                                }
                                foundMapName = string.Join("_", idParts);
                            }
                            else if (idNode.GetValueKind() == JsonValueKind.String)
                            {
                                foundMapName = idNode.ToString();
                            }
                        }

                        if (mapObj.TryGetPropertyValue("versions", out var versionsNode) && versionsNode is JsonArray versionsArray)
                        {
                            foreach (var verNode in versionsArray)
                            {
                                if (verNode is JsonArray verArray && verArray.Count >= 2)
                                {
                                    var verId = verArray[0]?.ToString();

                                    // Нормализуем хеш версии к строке
                                    string verHash = null;
                                    if (verArray[1] != null)
                                    {
                                        verHash = verArray[1].GetValueKind() == JsonValueKind.String
                                            ? verArray[1].ToString()
                                            : verArray[1].ToJsonString().Trim('"'); // Для чисел
                                    }

                                    if (verHash == mapHash && !string.IsNullOrEmpty(verId))
                                    {
                                        foundVersionId = verId;
                                        break;
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(foundMapName))
                        {
                            mapName = foundMapName;
                            versionId = string.IsNullOrEmpty(foundVersionId) ? mapHash : foundVersionId;

                            if (!mapInfos.ContainsKey(mapName))
                            {
                                mapInfos[mapName] = new MapInfo();
                            }

                            if (!mapInfos[mapName].Versions.ContainsKey(versionId))
                            {
                                mapInfos[mapName].Versions[versionId] = "";
                            }
                        }
                    }
                }

                // 2.2 Префиксный поиск с извлечением версии из имени сценария
                if (string.IsNullOrEmpty(mapName))
                {
                    int underscoreCount = jsonName.Count(c => c == '_');
                    if (underscoreCount >= 3)
                    {
                        foreach (var prefix in mapPrefixLookup)
                        {
                            if (jsonName.StartsWith(prefix.CleanName + "_", StringComparison.OrdinalIgnoreCase) ||
                                jsonName.Equals(prefix.CleanName, StringComparison.OrdinalIgnoreCase))
                            {
                                mapName = prefix.MapName;
                                versionId = prefix.VersionId;

                                // Если версия не определена (нет .map файла), попробуем извлечь из имени сценария
                                if (string.IsNullOrEmpty(versionId))
                                {
                                    int vIndex = jsonName.IndexOf("_v", StringComparison.OrdinalIgnoreCase);
                                    if (vIndex != -1)
                                    {
                                        string afterV = jsonName.Substring(vIndex + 2);
                                        int endIndex = afterV.IndexOf('_');
                                        if (endIndex == -1) endIndex = afterV.Length;
                                        string versionPart = afterV.Substring(0, endIndex);
                                        if (!string.IsNullOrEmpty(versionPart))
                                        {
                                            versionId = "v" + versionPart;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                // 2.3 Fallback по возможным именам из кэша
                if (string.IsNullOrEmpty(mapName) && !string.IsNullOrEmpty(mapHash) && mapsData.ContainsKey(mapHash))
                {
                    var possibleNames = GetPossibleBaseNamesFromMapData(mapsData[mapHash]);
                    foreach (var baseName in possibleNames)
                    {
                        if (mapInfos.ContainsKey(baseName))
                        {
                            mapName = baseName;
                            versionId = mapInfos[mapName].Versions.FirstOrDefault().Key;
                            break;
                        }
                    }
                }

                // 2.4 Поиск по приоритетным именам
                if (string.IsNullOrEmpty(mapName))
                {
                    foreach (var priorityName in priorityMapNames)
                    {
                        if (jsonName.StartsWith(priorityName + "_", StringComparison.OrdinalIgnoreCase) ||
                            jsonName.Equals(priorityName, StringComparison.OrdinalIgnoreCase))
                        {
                            mapName = priorityName;
                            if (mapInfos.ContainsKey(mapName))
                            {
                                versionId = mapInfos[mapName].Versions.FirstOrDefault().Key;
                            }
                            else
                            {
                                mapInfos[mapName] = new MapInfo();
                                versionId = "default";
                                mapInfos[mapName].Versions[versionId] = "";
                            }
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(mapName))
                {
                    unmatchedJsonKeys.Add(jsonKey);
                    continue;
                }

                var scenario = new ScenarioInfo
                {
                    File = jsonKey,
                    MapVersion = versionId,
                    Name = jsonData.ScenarioName,
                    Year = jsonData.ScenarioYear,
                    Description = jsonData.ScenarioDesc
                };

                if (!mapInfos.ContainsKey(mapName)) mapInfos[mapName] = new MapInfo();
                if (!mapInfos[mapName].Versions.ContainsKey(versionId)) mapInfos[mapName].Versions[versionId] = "";
                mapInfos[mapName].Scenarios.Add(scenario);
            }

            var otherScenarios = new List<ScenarioInfo>();
            foreach (var jsonKey in unmatchedJsonKeys)
            {
                bool assigned = false;
                string fileName = Path.GetFileNameWithoutExtension(jsonKey);
                foreach (var priorityName in priorityMapNames)
                {
                    if (fileName.StartsWith(priorityName + "_", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals(priorityName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!mapInfos.ContainsKey(priorityName)) mapInfos[priorityName] = new MapInfo();
                        string vId = mapInfos[priorityName].Versions.Count == 0 ? "default" : mapInfos[priorityName].Versions.First().Key;
                        if (!mapInfos[priorityName].Versions.ContainsKey(vId)) mapInfos[priorityName].Versions[vId] = "";

                        mapInfos[priorityName].Scenarios.Add(new ScenarioInfo { File = jsonKey, MapVersion = vId });
                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    otherScenarios.Add(new ScenarioInfo { File = jsonKey });
                }
            }

            var keysToRemove = mapInfos.Where(kvp => kvp.Value.Versions.Count == 0).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove) mapInfos.Remove(key);

            var orderedResult = new Dictionary<string, object>();

            foreach (var priorityName in priorityMapNames)
            {
                if (mapInfos.ContainsKey(priorityName))
                {
                    orderedResult[priorityName] = new
                    {
                        versions = mapInfos[priorityName].Versions.Select(kvp => new[] { kvp.Key, kvp.Value }).ToList(),
                        scenarios = mapInfos[priorityName].Scenarios
                            .OrderBy(s => s.File, StringComparer.OrdinalIgnoreCase)
                            .Select(s => new { file = s.File, name = s.Name, year = s.Year, description = s.Description, mapversion = s.MapVersion }).ToList()
                    };
                    mapInfos.Remove(priorityName);
                }
            }

            var rootKeys = mapInfos.Keys
                .Where(k => !k.Contains('/') && !k.Contains('\\'))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var key in rootKeys)
            {
                orderedResult[key] = new
                {
                    versions = mapInfos[key].Versions.Select(kvp => new[] { kvp.Key, kvp.Value }).ToList(),
                    scenarios = mapInfos[key].Scenarios
                        .OrderBy(s => s.File, StringComparer.OrdinalIgnoreCase)
                        .Select(s => new { file = s.File, name = s.Name, year = s.Year, description = s.Description, mapversion = s.MapVersion }).ToList()
                };
                mapInfos.Remove(key);
            }

            if (otherScenarios.Count > 0)
            {
                orderedResult[OtherCategory] = new
                {
                    versions = new List<string[]>(),
                    scenarios = otherScenarios
                        .OrderBy(s => s.File, StringComparer.OrdinalIgnoreCase)
                        .Select(s => new { file = s.File, name = (string)null, year = (int?)null, description = (string)null, mapversion = (string)null }).ToList()
                };
            }

            var nestedKeys = mapInfos.Keys
                .Where(k => k.Contains('/') || k.Contains('\\'))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var key in nestedKeys)
            {
                orderedResult[key] = new
                {
                    versions = mapInfos[key].Versions.Select(kvp => new[] { kvp.Key, kvp.Value }).ToList(),
                    scenarios = mapInfos[key].Scenarios
                        .OrderBy(s => s.File, StringComparer.OrdinalIgnoreCase)
                        .Select(s => new { file = s.File, name = s.Name, year = s.Year, description = s.Description, mapversion = s.MapVersion }).ToList()
                };
            }

            return JsonSerializer.Serialize(orderedResult, new JsonSerializerOptions { WriteIndented = true });
        }

        private bool IsHashMatch(JsonElement item, string targetHash)
        {
            if (string.IsNullOrEmpty(targetHash)) return false;

            // Проверяем поле "hash"
            if (item.TryGetProperty("hash", out var h))
            {
                string hashStr = h.ValueKind == JsonValueKind.String ? h.GetString() : h.GetRawText();
                if (hashStr == targetHash) return true;
            }

            // Проверяем поле "map_hash"
            if (item.TryGetProperty("map_hash", out var mh))
            {
                string mhStr = mh.ValueKind == JsonValueKind.String ? mh.GetString() : mh.GetRawText();
                if (mhStr == targetHash) return true;
            }

            // Проверяем массив versions
            if (item.TryGetProperty("versions", out var versions) && versions.ValueKind == JsonValueKind.Array)
            {
                foreach (var ver in versions.EnumerateArray())
                {
                    if (ver.ValueKind == JsonValueKind.Array && ver.GetArrayLength() >= 2)
                    {
                        string verHash = ver[1].ValueKind == JsonValueKind.String ? ver[1].GetString() : ver[1].GetRawText();
                        if (verHash == targetHash) return true;
                    }
                }
            }
            return false;
        }

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

                    result.Add(baseId);

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