using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FreelanceViewer
{
    public partial class MainWindow : Window
    {
        // controls (will be found after loading XAML)
        private Avalonia.Controls.Button FetchButton = null!;
        private Avalonia.Controls.Button RefreshButton = null!;
        private Avalonia.Controls.Button OpenUrlButton = null!;
        private Avalonia.Controls.TextBox SearchBox = null!;
        private Avalonia.Controls.DataGrid OffersGrid = null!;

        private List<OfferViewModel> _allOffers = new();
        public ObservableCollection<OfferViewModel> FilteredOffers { get; } = new();
        private string _dbPath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            // Bind controls
            FetchButton = this.FindControl<Avalonia.Controls.Button>("FetchButton");
            RefreshButton = this.FindControl<Avalonia.Controls.Button>("RefreshButton");
            OpenUrlButton = this.FindControl<Avalonia.Controls.Button>("OpenUrlButton");
            SearchBox = this.FindControl<Avalonia.Controls.TextBox>("SearchBox");
            OffersGrid = this.FindControl<Avalonia.Controls.DataGrid>("OffersGrid");

            // Set data context and bind collections
            this.DataContext = this;
            // Items are bound in XAML: Items="{Binding FilteredOffers}"
            OffersGrid.DoubleTapped += OffersGrid_DoubleTapped;

            FetchButton.Click += async (s, e) => await FetchButton_Click(s, e);
            RefreshButton.Click += async (s, e) => await RefreshButton_Click(s, e);
            OpenUrlButton.Click += OpenUrlButton_Click;
            SearchBox.KeyUp += (s, e) => ApplyFilter();

            _dbPath = FindDbPath();
            _ = LoadOffersAsync();

            // debug
            System.Diagnostics.Debug.WriteLine($"Using DB path: {_dbPath}");
            Console.WriteLine($"Using DB path: {_dbPath}");
        }


        private void OffersGrid_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var sel = OffersGrid?.SelectedItem as OfferViewModel;
            if (sel != null && !string.IsNullOrEmpty(sel.Url))
            {
                try { Process.Start(new ProcessStartInfo(sel.Url) { UseShellExecute = true }); } catch { }
            }
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        private async Task RefreshButton_Click(object? sender, RoutedEventArgs e) => await LoadOffersAsync();

        private void OpenUrlButton_Click(object? sender, RoutedEventArgs e)
        {
            var sel = OffersGrid?.SelectedItem as OfferViewModel;
            if (sel != null && !string.IsNullOrEmpty(sel.Url))
            {
                try { Process.Start(new ProcessStartInfo(sel.Url) { UseShellExecute = true }); } catch { }
            }
        }

        private void ApplyFilter()
        {
            var q = SearchBox.Text?.ToLowerInvariant() ?? string.Empty;
            FilteredOffers.Clear();
            foreach (var item in _allOffers)
            {
                if (string.IsNullOrEmpty(q) || (item.Title?.ToLowerInvariant().Contains(q) ?? false))
                    FilteredOffers.Add(item);
            }
        }

        private string FindPythonDir()
        {
            // Respect explicit override if present (useful for Windows testing)
            var overrideDir = System.Environment.GetEnvironmentVariable("FREELANCE_PYTHON_DIR");
            if (!string.IsNullOrEmpty(overrideDir) && Directory.Exists(overrideDir))
                return overrideDir;

            var assemblyDir = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location) ?? Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(assemblyDir);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "python");
                if (Directory.Exists(candidate) && Directory.Exists(Path.Combine(candidate, "freelance_parser")))
                    return candidate;
                dir = dir.Parent;
            }
            // fallback to current directory search
            dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "python");
                if (Directory.Exists(candidate) && Directory.Exists(Path.Combine(candidate, "freelance_parser")))
                    return candidate;
                dir = dir.Parent;
            }
            // final fallback guess
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "python"));
        }

        private async Task<bool> RunParserAsync(string site, int pages)
        {
            var pythonDir = FindPythonDir();
            if (string.IsNullOrEmpty(pythonDir) || !Directory.Exists(pythonDir))
            {
                await ShowMessageAsync("Python не найден", "Не удалось найти папку с Python-проектом. Убедитесь, что в репозитории есть папка 'python' с модулем 'freelance_parser'.");
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"-m freelance_parser.cli fetch --site {site} --pages {pages}",
                WorkingDirectory = pythonDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                var stderrTask = proc.StandardError.ReadToEndAsync();
                await Task.WhenAll(stdoutTask, stderrTask);
                await proc.WaitForExitAsync();
                var ok = proc.ExitCode == 0;
                // simple logging
                System.Diagnostics.Debug.WriteLine($"Parser {site} exit={proc.ExitCode}\n{stdoutTask.Result}\n{stderrTask.Result}");
                return ok;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to start parser: " + ex);
                return false;
            }
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            var win = new Window
            {
                Title = title,
                Width = 480,
                Height = 240,
                Content = new ScrollViewer
                {
                    Content = new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap }
                }
            };
            await win.ShowDialog(this);
        }

        private string FindDbPath()
        {
            // Allow explicit override for testing (FREELANCE_DB_PATH)
            var overridePath = System.Environment.GetEnvironmentVariable("FREELANCE_DB_PATH");
            if (!string.IsNullOrEmpty(overridePath) && File.Exists(overridePath))
            {
                Console.WriteLine($"Using DB override: {overridePath}");
                return overridePath;
            }

            var assemblyDir = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location) ?? Directory.GetCurrentDirectory();
            var dir = new DirectoryInfo(assemblyDir);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "python", "data", "offers.db");
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }

            dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "python", "data", "offers.db");
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }

            var fallback = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "python", "data", "offers.db"));
            Console.WriteLine($"DB not found by search; using fallback path: {fallback}");
            return fallback;
        }

        private async Task FetchButton_Click(object? sender, RoutedEventArgs e)
        {
            FetchButton.IsEnabled = false;
            RefreshButton.IsEnabled = false;
            try
            {
                var ok1 = await RunParserAsync("flru", 5);
                var ok2 = await RunParserAsync("kwork", 5);
                await LoadOffersAsync();
                await ShowMessageAsync("Парсинг завершён", $"FL.ru: {(ok1 ? "OK" : "Failed")}\nKwork: {(ok2 ? "OK" : "Failed")}");
            }
            finally
            {
                FetchButton.IsEnabled = true;
                RefreshButton.IsEnabled = true;
            }
        }
        private async Task LoadOffersAsync()
        {
            _allOffers.Clear();
            FilteredOffers.Clear();
            if (string.IsNullOrEmpty(_dbPath) || !File.Exists(_dbPath))
            {
                Console.WriteLine($"Файл базы данных не найден:\n{_dbPath}");
                await ShowMessageAsync("БД не найдена", $"Файл базы данных не найден:\n{_dbPath}\nЗапустите парсер или проверьте путь.");
                return;
            }

            Console.WriteLine($"Reading DB: {_dbPath}");
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT site, title, url, budget, posted_at FROM offers ORDER BY scraped_at DESC LIMIT 1000";
            using var rdr = cmd.ExecuteReader();
            int i = 0;
            while (rdr.Read())
            {
                var item = new OfferViewModel
                {
                    Site = rdr.GetString(0),
                    Title = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1),
                    Url = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2),
                    Budget = rdr.IsDBNull(3) ? string.Empty : rdr.GetString(3),
                    PostedAt = rdr.IsDBNull(4) ? string.Empty : rdr.GetString(4)
                };
                _allOffers.Add(item);
                Console.WriteLine($"    Site: {item.Site}\n   Title: {item.Title} \n   Url: {item.Url}\n   Budget: {item.Budget}\n    PostedAt: {item.PostedAt}\n");
                i++;
            }

            ApplyFilter();
        }
    }

    public class OfferViewModel
    {
        public string Site { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Budget { get; set; } = string.Empty;
        public string PostedAt { get; set; } = string.Empty;
    }
}