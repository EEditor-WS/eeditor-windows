using System.Text;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using System;
using NetDiscordRpc;
using NetDiscordRpc.RPC;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace EEditor
{
    public class ConfigData
    {
        public string ServerUrl { get; set; } = "";
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
    public partial class MainWindow : Window
    {
        private const string applicationId = "1333948751919972434";
        private DiscordRPC client;
        private string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private ConfigData config;

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
                    config = JsonSerializer.Deserialize<ConfigData>(json);
                }
                catch
                {
                    config = new ConfigData();
                }
            }
            else
            {
                config = new ConfigData();
            }

            if (string.IsNullOrEmpty(config.ServerUrl))
            {
                string message =
@"Выберите сервер / Choise server:
1. Россия
2. International 1";
//3. International 2";

                //var result = MessageBox.Show(message, "Выбор сервера", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                // Тут MessageBox не позволяет три кнопки кастомные, поэтому проще спросить через InputBox/свой Window.
                // Для простоты: используем стандартный InputBox (вариант через WPF окно).

                var input = Microsoft.VisualBasic.Interaction.InputBox(message, "Выбор сервера", "2");

                switch (input.Trim())
                {
                    case "1":
                        config.ServerUrl = "https://eeditor-ws.github.io/";
                        break;
                    case "2":
                        config.ServerUrl = "https://eeditor-ws.vercel.app/";
                        break;
                    //case "3":
                    //    config.ServerUrl = "https://eeditor-warnament.netlify.app/";
                    //    break;
                    default:
                        config.ServerUrl = "https://eeditor-ws.vercel.app/"; // значение по умолчанию
                        break;
                }

                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
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

            string remoteUrl = config.ServerUrl;

            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "index.html");
            string localUri = new Uri(localPath).AbsoluteUri;

            bool isRemoteAvailable = await CheckRemoteAvailable(remoteUrl);
            string targetUrl = isRemoteAvailable ? remoteUrl : localUri;

            webView.CoreWebView2.Navigate(targetUrl);
            webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            webView.CoreWebView2.AddHostObjectToScript("backupManager", new BackupManagerBridge());

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
