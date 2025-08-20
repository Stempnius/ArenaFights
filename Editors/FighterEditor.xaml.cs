using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ArenaFights.Models;

namespace ArenaFights
{
    public partial class FighterEditor : Window
    {
        private readonly Zawodnik _model;
        private readonly string _portraitsFolderRel;
        private readonly string _skillsFolderRel;

        private readonly ObservableCollection<UmiejetnoscRef> _skillsView = new();
        private readonly ObservableCollection<UmiejetnoscRef> _weaknessesView = new();

        public FighterEditor(Zawodnik model, string portraitsFolderRel, string skillsFolderRel)
        {
            InitializeComponent();
            _model = model;
            _portraitsFolderRel = portraitsFolderRel;
            _skillsFolderRel = skillsFolderRel;

            // ===== Dane podstawowe =====
            NameBox.Text = _model.Imie ?? "";
            StrengthBox.Text = _model.Sila.ToString(CultureInfo.InvariantCulture);
            FightBox.Text = _model.Walka.ToString(CultureInfo.InvariantCulture);
            EnergyBox.Text = _model.Energia.ToString(CultureInfo.InvariantCulture);
            EnduranceBox.Text = _model.Wytrzymalosc.ToString(CultureInfo.InvariantCulture);
            SpeedBox.Text = _model.Predkosc.ToString(CultureInfo.InvariantCulture);
            TonsBox.Text = _model.Tony.ToString(CultureInfo.InvariantCulture);
            EloBox.Text = _model.Elo.ToString(CultureInfo.InvariantCulture);

            // ===== DRUŻYNA – ustaw od razu widoczny tekst =====
            TeamNameText.Text = string.IsNullOrWhiteSpace(_model.Druzyna) ? "(brak)" : _model.Druzyna;

            // ===== Portret =====
            if (!string.IsNullOrWhiteSpace(_model.PortretPath))
            {
                var abs = Abs(_model.PortretPath);
                if (File.Exists(abs))
                    PortraitPreview.Source = new BitmapImage(new Uri(abs));
            }

            // ===== Umiejętności / Słabości =====
            if (_model.Umiejetnosci != null)
                foreach (var u in _model.Umiejetnosci) _skillsView.Add(u);
            SkillsItems.ItemsSource = _skillsView;

            if (_model.Slabosci != null)
                foreach (var w in _model.Slabosci) _weaknessesView.Add(w);
            WeaknessItems.ItemsSource = _weaknessesView;
        }

        // ---- DRUŻYNA: wybór z okna ----
        private void PickTeam_Click(object sender, RoutedEventArgs e)
        {
            var picker = new TeamPickerWindow { Owner = this };
            if (picker.ShowDialog() == true)
            {
                var name = picker.SelectedTeam;
                TeamNameText.Text = string.IsNullOrWhiteSpace(name) ? "(brak)" : name;
            }
        }

        // ---- Portret ----
        private void PortraitFromFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Wybierz obraz portretu"
            };
            if (ofd.ShowDialog(this) == true)
            {
                var bmp = new BitmapImage(new Uri(ofd.FileName));

                var fileName = Guid.NewGuid().ToString("N") + ".png";
                var rel = _portraitsFolderRel + "/" + fileName;
                var abs = Abs(rel);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
                using (var fs = new FileStream(abs, FileMode.Create, FileAccess.Write))
                    encoder.Save(fs);

                _model.PortretPath = rel;
                PortraitPreview.Source = new BitmapImage(new Uri(abs));
            }
        }

        private void PortraitPaste_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsImage()) { MessageBox.Show("Schowek nie zawiera obrazu."); return; }
            var bmp = Clipboard.GetImage(); if (bmp == null) { MessageBox.Show("Nie udało się pobrać obrazu."); return; }

            var fileName = Guid.NewGuid().ToString("N") + ".png";
            var rel = _portraitsFolderRel + "/" + fileName;
            var abs = Abs(rel);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
            using (var fs = new FileStream(abs, FileMode.Create, FileAccess.Write))
                encoder.Save(fs);

            _model.PortretPath = rel;
            PortraitPreview.Source = new BitmapImage(new Uri(abs));
        }

        private void PortraitRemove_Click(object sender, RoutedEventArgs e)
        {
            _model.PortretPath = string.Empty;
            PortraitPreview.Source = null;
        }

        // ---- Umiejętności/Słabości z katalogu ----
        private void AddSkillFromCatalog_Click(object sender, RoutedEventArgs e)
        {
            var catalog = SkillsCatalogProvider.Load();
            if (catalog.Count == 0)
            {
                MessageBox.Show("Katalog umiejętności jest pusty. Dodaj najpierw umiejętność w zakładce 'Umiejętności'.");
                return;
            }

            var picker = new SkillsPickerWindow(catalog) { Owner = this };
            if (picker.ShowDialog() == true && picker.Selected != null)
            {
                _skillsView.Add(new UmiejetnoscRef
                {
                    Nazwa = picker.Selected.Nazwa,
                    IkonaPath = picker.Selected.IkonaPath
                });
            }
        }

        private void RemoveSkill_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is UmiejetnoscRef s)
                _skillsView.Remove(s);
        }

        private void AddWeaknessFromCatalog_Click(object sender, RoutedEventArgs e)
        {
            var catalog = SkillsCatalogProvider.Load();
            if (catalog.Count == 0)
            {
                MessageBox.Show("Katalog umiejętności jest pusty. Dodaj najpierw umiejętność w zakładce 'Umiejętności'.");
                return;
            }

            var picker = new SkillsPickerWindow(catalog) { Owner = this };
            if (picker.ShowDialog() == true && picker.Selected != null)
            {
                _weaknessesView.Add(new UmiejetnoscRef
                {
                    Nazwa = picker.Selected.Nazwa,
                    IkonaPath = picker.Selected.IkonaPath
                });
            }
        }

        private void RemoveWeakness_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is UmiejetnoscRef w)
                _weaknessesView.Remove(w);
        }

        // ---- OK / Anuluj ----
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text)) { MessageBox.Show("Imię nie może być puste."); return; }

            if (!TryParseInt(StrengthBox.Text, 1, 10, out var strength)) { FieldError("Siła (1..10)"); return; }
            if (!TryParseInt(FightBox.Text, 1, 10, out var fight)) { FieldError("Walka (1..10)"); return; }
            if (!TryParseInt(EnergyBox.Text, 1, 10, out var energy)) { FieldError("Energia (1..10)"); return; }
            if (!TryParseInt(EnduranceBox.Text, 1, 10, out var endur)) { FieldError("Wytrzymałość (1..10)"); return; }
            if (!TryParseInt(SpeedBox.Text, 1, 10, out var speed)) { FieldError("Prędkość (1..10)"); return; }
            if (!TryParseDecimal(TonsBox.Text, 0.01m, 999m, out var tons)) { FieldError("Tony (0,01..999)"); return; }
            if (!TryParseDecimal(EloBox.Text, 0m, 9999m, out var elo)) { FieldError("ELO (0..9999)"); return; }

            _model.Imie = NameBox.Text.Trim();
            _model.Sila = strength;
            _model.Walka = fight;
            _model.Energia = energy;
            _model.Wytrzymalosc = endur;
            _model.Predkosc = speed;
            _model.Tony = tons;
            _model.Elo = elo;

            // >>> ZAPIS DRUŻYNY DO MODELU <<<
            var teamText = TeamNameText.Text;
            _model.Druzyna = string.Equals(teamText, "(brak)", StringComparison.OrdinalIgnoreCase)
                             ? null
                             : (teamText?.Trim() ?? null);

            _model.Umiejetnosci = new System.Collections.Generic.List<UmiejetnoscRef>(_skillsView);
            _model.Slabosci = new System.Collections.Generic.List<UmiejetnoscRef>(_weaknessesView);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Helpers
        private static string Abs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool TryParseInt(string text, int min, int max, out int value)
        {
            value = 0;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tmp))
                return false;
            if (tmp < min) tmp = min;
            if (tmp > max) tmp = max;
            value = tmp;
            return true;
        }

        private static bool TryParseDecimal(string text, decimal min, decimal max, out decimal value)
        {
            value = 0m;
            var n = (text ?? "").Replace(',', '.');
            if (!decimal.TryParse(n, NumberStyles.Number, CultureInfo.InvariantCulture, out var tmp))
                return false;
            if (tmp < min) tmp = min;
            if (tmp > max) tmp = max;
            value = tmp;
            return true;
        }

        private static void FieldError(string field)
        {
            MessageBox.Show("Nieprawidłowa wartość pola: " + field, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
