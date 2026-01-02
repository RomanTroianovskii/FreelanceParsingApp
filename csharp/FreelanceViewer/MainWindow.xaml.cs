using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FreelanceViewer
{
    public partial class MainWindow : Window
    {
        // controls (will be found after loading XAML)
        private Avalonia.Controls.Button RefreshButton = null!;
        private Avalonia.Controls.Button OpenUrlButton = null!;
        private Avalonia.Controls.TextBox SearchBox = null!;
        private object OffersGrid = null!; // typed as object to avoid compile-time dependency on DataGrid type

        private ObservableCollection<OfferViewModel> _offers = new();
        private string _dbPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "python", "data", "offers.db");

        public MainWindow()
        {
            InitializeComponent();

            // Bind controls
            RefreshButton = this.FindControl<Avalonia.Controls.Button>("RefreshButton");
            OpenUrlButton = this.FindControl<Avalonia.Controls.Button>("OpenUrlButton");
            SearchBox = this.FindControl<Avalonia.Controls.TextBox>("SearchBox");
            OffersGrid = this.FindControl<Control>("OffersGrid");

            // Set Items via reflection to avoid DataGrid compile-time dependency
            SetGridItems(_offers);

            RefreshButton.Click += RefreshButton_Click;
            OpenUrlButton.Click += OpenUrlButton_Click;
            SearchBox.KeyUp += (s, e) => ApplyFilter();

            LoadOffers();
        }

        private void SetGridItems(object items)
        {
            var prop = OffersGrid?.GetType().GetProperty("Items");
            prop?.SetValue(OffersGrid, items);
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        private void RefreshButton_Click(object? sender, RoutedEventArgs e) => LoadOffers();

        private void OpenUrlButton_Click(object? sender, RoutedEventArgs e)
        {
            // Get SelectedItem via reflection
            var selObj = OffersGrid?.GetType().GetProperty("SelectedItem")?.GetValue(OffersGrid);
            if (selObj is OfferViewModel sel && !string.IsNullOrEmpty(sel.Url))
            {
                try { Process.Start(new ProcessStartInfo(sel.Url) { UseShellExecute = true }); } catch { }
            }
        }

        private void ApplyFilter()
        {
            var q = SearchBox.Text?.ToLowerInvariant() ?? string.Empty;
            foreach (var item in _offers)
            {
                item.IsVisible = string.IsNullOrEmpty(q) || (item.Title?.ToLowerInvariant().Contains(q) ?? false);
            }
            SetGridItems(_offers.Where(x => x.IsVisible));
        }

        private void LoadOffers()
        {
            _offers.Clear();
            if (!File.Exists(_dbPath)) return;

            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT site, title, url, budget, posted_at FROM offers ORDER BY scraped_at DESC LIMIT 1000";
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                _offers.Add(new OfferViewModel
                {
                    Site = rdr.GetString(0),
                    Title = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1),
                    Url = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2),
                    Budget = rdr.IsDBNull(3) ? string.Empty : rdr.GetString(3),
                    PostedAt = rdr.IsDBNull(4) ? string.Empty : rdr.GetString(4),
                    IsVisible = true
                });
            }

            SetGridItems(_offers);
        }
    }

    public class OfferViewModel
    {
        public string Site { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Budget { get; set; } = string.Empty;
        public string PostedAt { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
    }
}