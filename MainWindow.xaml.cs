using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ArenaFights.Models;     // Zawodnik, Arena, SkillEntry
using ArenaFights.Tabs;      // FightersTab, SkillsTab, ArenasTab
using ArenaFights.Editors;   // FighterEditorControl

namespace ArenaFights
{
    public partial class MainWindow : Window
    {
        // Ścieżki danych
        private const string DataFolderRel = "data";
        private const string PlayersJsonRel = DataFolderRel + "/players.json";
        private const string ArenasJsonRel = DataFolderRel + "/arenas.json";

        // Kolekcje współdzielone z kartami
        private readonly ObservableCollection<Zawodnik> _fighters = new();
        private readonly ObservableCollection<SkillEntry> _skills = new();
        private readonly ObservableCollection<Arena> _arenas = new();

        public MainWindow()
        {
            InitializeComponent();
            EnsureFolders();

            // Fighters tab
            FightersTabControl.Initialize("assets/portraits", "assets/skills");
            FightersTabControl.SetSource(_fighters);
            FightersTabControl.SelectedNameChanged += name =>
            {
                SelectedStatus.Text = string.IsNullOrWhiteSpace(name)
                    ? "Brak zaznaczenia"
                    : $"Zaznaczono: {name}";
            };
            // >>> NOWOŚĆ: prosimy MainWindow o otwarcie edytora w nowej karcie
            FightersTabControl.OpenEditorRequested += OpenEditorTab;

            // Skills tab
            SkillsTabControl.SetSource(_skills);

            // Arenas tab
            ArenasTabControl.SetSource(_arenas);

            // Start – wczytaj z dysku (jeżeli istnieją)
            LoadSkills();
            LoadPlayersIfExists();
            LoadArenasIfExists();
        }

        // ====== Otwieranie edytorów w kartach ======
        private void OpenEditorTab(UserControl editorControl, string header)
        {
            // Nagłówek karty z przyciskiem X
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0) };
            headerPanel.Children.Add(new TextBlock { Text = header, Margin = new Thickness(0, 0, 6, 0) });

            var closeBtn = new Button
            {
                Content = "×",
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                Cursor = Cursors.Hand
            };

            var tab = new TabItem { Content = editorControl };
            closeBtn.Click += (s, e) => MainTabs.Items.Remove(tab);

            headerPanel.Children.Add(closeBtn);
            tab.Header = headerPanel;

            // Podpinamy zdarzenia „edycyjnych” kontrolek, jeżeli mają Saved/CloseRequested
            if (editorControl is FighterEditorControl fec)
            {
                fec.Saved += ctrl =>
                {
                    // Jeżeli nowy – dodaj do listy i przelicz ranking
                    if (fec.IsNew)
                    {
                        // uniknij duplikatu po nazwie
                        if (_fighters.Any(f => string.Equals(f.Imie, fec.Model.Imie, StringComparison.OrdinalIgnoreCase)))
                        {
                            MessageBox.Show("Zawodnik o takiej nazwie już istnieje.");
                            return;
                        }
                        _fighters.Add(fec.Model);
                        RecalcRankingAndSort();
                    }
                    // odśwież kartę zawodników (filtry/kolumny, jeśli potrzeba)
                    FightersTabControl.RefreshAfterExternalChange();
                    // zamknij kartę edytora
                    MainTabs.Items.Remove(tab);
                };
                fec.CloseRequested += ctrl =>
                {
                    MainTabs.Items.Remove(tab);
                };
            }

            MainTabs.Items.Add(tab);
            tab.IsSelected = true;
        }

        private void RecalcRankingAndSort()
        {
            var ordered = _fighters
                .OrderByDescending(f => f.Elo)
                .ThenBy(f => f.Imie, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = 0; i < ordered.Count; i++) ordered[i].Miejsce = i + 1;

            _fighters.Clear();
            foreach (var f in ordered) _fighters.Add(f);
        }

        // ===== Umiejętności (katalog) =====
        private void LoadSkills()
        {
            _skills.Clear();
            foreach (var s in SkillsCatalogProvider.Load())
                _skills.Add(s);

            SkillsTabControl.RefreshAfterExternalChange();
        }

        // ===== Zapis/Wczyt – zawodnicy + areny =====
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(AbsDir(DataFolderRel));

                File.WriteAllText(
                    Abs(PlayersJsonRel),
                    JsonConvert.SerializeObject(_fighters.ToList(), Formatting.Indented)
                );

                File.WriteAllText(
                    Abs(ArenasJsonRel),
                    JsonConvert.SerializeObject(_arenas.ToList(), Formatting.Indented)
                );

                MessageBox.Show("Zapisano: zawodnicy + areny.", "OK");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu: " + ex.Message);
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(Abs(PlayersJsonRel)))
                {
                    var json = File.ReadAllText(Abs(PlayersJsonRel));
                    var list = JsonConvert.DeserializeObject<List<Zawodnik>>(json) ?? new List<Zawodnik>();
                    _fighters.Clear();
                    foreach (var f in list) _fighters.Add(f);
                    FightersTabControl.RefreshAfterExternalChange();
                }

                if (File.Exists(Abs(ArenasJsonRel)))
                {
                    var jsonA = File.ReadAllText(Abs(ArenasJsonRel));
                    var listA = JsonConvert.DeserializeObject<List<Arena>>(jsonA) ?? new List<Arena>();
                    _arenas.Clear();
                    foreach (var a in listA) _arenas.Add(a);
                    ArenasTabControl.RefreshAfterExternalChange();
                }

                MessageBox.Show("Wczytano: zawodnicy + areny.", "OK");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania: " + ex.Message);
            }
        }

        private void LoadPlayersIfExists()
        {
            try
            {
                if (!File.Exists(Abs(PlayersJsonRel))) return;
                var json = File.ReadAllText(Abs(PlayersJsonRel));
                var list = JsonConvert.DeserializeObject<List<Zawodnik>>(json) ?? new List<Zawodnik>();
                _fighters.Clear();
                foreach (var f in list) _fighters.Add(f);
                FightersTabControl.RefreshAfterExternalChange();
            }
            catch { }
        }

        private void LoadArenasIfExists()
        {
            try
            {
                if (!File.Exists(Abs(ArenasJsonRel))) return;
                var json = File.ReadAllText(Abs(ArenasJsonRel));
                var list = JsonConvert.DeserializeObject<List<Arena>>(json) ?? new List<Arena>();
                _arenas.Clear();
                foreach (var a in list) _arenas.Add(a);
                ArenasTabControl.RefreshAfterExternalChange();
            }
            catch { }
        }

        // ===== Foldery =====
        private void EnsureFolders()
        {
            Directory.CreateDirectory(AbsDir(DataFolderRel));
        }

        private static string Abs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }
        private static string AbsDir(string relDir) => Abs(relDir);
    }
}
