using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ArenaFights.Models;
using ArenaFights.Services;

namespace ArenaFights
{
    public partial class TeamPickerWindow : Window
    {
        private readonly ObservableCollection<TeamRow> _rows = new();
        private List<Team> _allTeams = new();

        /// <summary>Wybrana nazwa drużyny (null/empty = brak).</summary>
        public string SelectedTeam { get; private set; }

        public TeamPickerWindow()
        {
            InitializeComponent();

            TeamsList.ItemsSource = _rows;

            LoadTeams();
            RefreshList();
        }

        private void LoadTeams()
        {
            _allTeams = TeamsService.Load();
        }

        private void SaveTeams()
        {
            TeamsService.Save(_allTeams);
        }

        private void RefreshList()
        {
            var filter = (SearchBox.Text ?? "").Trim();
            IEnumerable<Team> q = _allTeams;

            if (!string.IsNullOrWhiteSpace(filter))
                q = q.Where(t => t.Nazwa.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

            // Licznik członków zostawiamy na 0 (podłączymy później do faktycznej listy zawodników)
            var view = q.OrderBy(t => t.Nazwa, StringComparer.OrdinalIgnoreCase)
                        .Select(t => new TeamRow { Nazwa = t.Nazwa, MembersCount = 0 })
                        .ToList();

            _rows.Clear();
            foreach (var r in view) _rows.Add(r);
        }

        // === UI handlers ===

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshList();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var name = Prompt("Podaj nazwę nowej drużyny:");
            if (string.IsNullOrWhiteSpace(name)) return;

            if (_allTeams.Any(t => string.Equals(t.Nazwa, name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Taka drużyna już istnieje.", "Uwaga");
                return;
            }
            _allTeams.Add(new Team { Nazwa = name.Trim() });
            SaveTeams();
            RefreshList();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (TeamsList.SelectedItem is not TeamRow row)
            {
                MessageBox.Show("Zaznacz drużynę do edycji.");
                return;
            }
            var newName = Prompt($"Zmień nazwę drużyny („{row.Nazwa}”):", row.Nazwa);
            if (string.IsNullOrWhiteSpace(newName)) return;

            if (_allTeams.Any(t => !t.Nazwa.Equals(row.Nazwa, StringComparison.OrdinalIgnoreCase) &&
                                   t.Nazwa.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Drużyna o tej nazwie już istnieje.");
                return;
            }

            // Zmień nazwę
            var team = _allTeams.First(t => t.Nazwa.Equals(row.Nazwa, StringComparison.OrdinalIgnoreCase));
            team.Nazwa = newName.Trim();
            SaveTeams();
            RefreshList();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (TeamsList.SelectedItem is not TeamRow row)
            {
                MessageBox.Show("Zaznacz drużynę do usunięcia.");
                return;
            }
            if (MessageBox.Show($"Usunąć drużynę „{row.Nazwa}”?", "Potwierdź",
                                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            var team = _allTeams.FirstOrDefault(t => t.Nazwa.Equals(row.Nazwa, StringComparison.OrdinalIgnoreCase));
            if (team != null) _allTeams.Remove(team);
            SaveTeams();
            RefreshList();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (TeamsList.SelectedItem is not TeamRow row)
            {
                MessageBox.Show("Zaznacz drużynę z listy.");
                return;
            }
            SelectedTeam = row.Nazwa;
            DialogResult = true;
            Close();
        }

        private void None_Click(object sender, RoutedEventArgs e)
        {
            SelectedTeam = null; // brak
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // === Helpers ===
        private sealed class TeamRow
        {
            public string Nazwa { get; set; } = "";
            public int MembersCount { get; set; }
        }

        private static string Prompt(string caption, string initial = "")
        {
            // prościutkie okno prompt – bez mnożenia plików
            var w = new Window
            {
                Title = caption,
                Width = 420,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };
            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var tb = new TextBox { Text = initial ?? "" };
            Grid.SetRow(tb, 2);
            grid.Children.Add(tb);

            var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 90, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "Anuluj", Width = 90 };
            ok.Click += (_, __) => { w.Tag = tb.Text; w.DialogResult = true; };
            cancel.Click += (_, __) => { w.DialogResult = false; };
            Grid.SetRow(panel, 4);
            panel.Children.Add(ok);
            panel.Children.Add(cancel);
            grid.Children.Add(panel);

            w.Content = grid;
            var owner = Application.Current?.Windows?.OfType<Window>()?.FirstOrDefault(x => x.IsActive);
            if (owner != null) w.Owner = owner;
            return w.ShowDialog() == true ? (w.Tag as string) : null;
        }
    }
}
