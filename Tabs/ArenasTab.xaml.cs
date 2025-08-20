using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ArenaFights.Models;
using ArenaFights.Services;

namespace ArenaFights.Tabs
{
    public partial class ArenasTab : UserControl
    {
        private ObservableCollection<Arena> _source = new();

        public ArenasTab()
        {
            InitializeComponent();
        }

        /// <summary>Podłącz istniejącą kolekcję aren (współdzieloną z MainWindow).</summary>
        public void SetSource(ObservableCollection<Arena> arenas)
        {
            _source = arenas ?? new ObservableCollection<Arena>();
            ArenyGrid.ItemsSource = _source;
            ArenyGrid.Items.Refresh();
        }

        /// <summary>Odśwież widok po zewnętrznych zmianach (np. po zapisie/odczycie/migracji).</summary>
        public void RefreshAfterExternalChange()
        {
            ArenyGrid.Items.Refresh();
        }

        // === Przyciski ===

        private void ArenaNew_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Wybierz obraz areny",
                Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.gif",
                Multiselect = false
            };
            if (ofd.ShowDialog() != true) return;

            // 1) Import do Assets (originals + miniatura 200px do thumbs)
            try
            {
                AssetsService.ImportArenaImage(ofd.FileName, out var relOriginal, out var relThumb);

                var arena = new Arena
                {
                    Nazwa = Path.GetFileNameWithoutExtension(ofd.FileName),
                    ObrazPath = relOriginal,                             // REL -> assets/arenas/originals/....
                    ObrazPathAbsolute = ToAbs(relOriginal),             // ABS do UI
                    MiniaturaPathAbsolute = ToAbs(relThumb),            // ABS miniatury 200px
                    Cechy = new System.Collections.Generic.List<SkillRef>()
                };

                // Unikaj duplikatów po nazwie
                if (_source.Any(a => string.Equals(a.Nazwa, arena.Nazwa, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Arena o takiej nazwie już istnieje.");
                    return;
                }

                _source.Add(arena);
                ArenyGrid.Items.Refresh();

                // Opcjonalnie: jeśli masz edytor Aren z gifem – otwórz
                TryOpenEditor(arena);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd importu areny: " + ex.Message, "Arena", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ArenaEdit_Click(object sender, RoutedEventArgs e)
        {
            if (ArenyGrid.SelectedItem is not Arena a)
            {
                MessageBox.Show("Zaznacz arenę na liście.", "Edycja areny",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            TryOpenEditor(a);
            ArenyGrid.Items.Refresh();
        }

        private void ArenaDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ArenyGrid.SelectedItem is not Arena a)
            {
                MessageBox.Show("Zaznacz arenę na liście.", "Usuń arenę",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"Usunąć arenę „{a.Nazwa}”? (pliki w Assets pozostaną)",
                "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            _source.Remove(a);
            ArenyGrid.Items.Refresh();
        }

        // === Helper: pełny edytor aren (jeśli istnieje w projekcie) ===
        private void TryOpenEditor(Arena arena)
        {
            try
            {
                var owner = Window.GetWindow(this);
                var catalog = SkillsCatalogProvider.Load(); // katalog umiejętności (zapis/odczyt w data/skills.json)

                var editorType = Type.GetType("ArenaFights.ArenaEditor");
                if (editorType != null)
                {
                    var editor = Activator.CreateInstance(editorType, new object[] { arena, catalog }) as Window;
                    if (editor != null)
                    {
                        if (owner != null) editor.Owner = owner;
                        editor.ShowDialog();
                    }
                }
            }
            catch
            {
                // brak edytora – pomijamy cicho
            }
        }

        private static string ToAbs(string relOrAbs)
        {
            if (string.IsNullOrWhiteSpace(relOrAbs)) return null;
            if (Path.IsPathRooted(relOrAbs)) return relOrAbs;
            // Twój AssetsService używa BaseDirectory jako bazy – korzystamy z jego Abs(...)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, relOrAbs.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
