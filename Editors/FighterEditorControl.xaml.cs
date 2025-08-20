using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ArenaFights.Models;

namespace ArenaFights.Editors
{
    public partial class FighterEditorControl : UserControl
    {
        private readonly string _portraitsFolderRel;
        private readonly string _skillsFolderRel;
        private readonly ObservableCollection<ArenaFights.Models.UmiejetnoscRef> _skillsView = new ObservableCollection<ArenaFights.Models.UmiejetnoscRef>();
        private readonly ObservableCollection<ArenaFights.Models.UmiejetnoscRef> _weaknessesView = new ObservableCollection<ArenaFights.Models.UmiejetnoscRef>();

        public Zawodnik Model { get; }
        public bool IsNew { get; }

        public event Action<FighterEditorControl> Saved;
        public event Action<FighterEditorControl> CloseRequested;

        public FighterEditorControl(Zawodnik model, bool isNew, string portraitsFolderRel, string skillsFolderRel)
        {
            InitializeComponent();
            Model = model;
            IsNew = isNew;
            _portraitsFolderRel = portraitsFolderRel;
            _skillsFolderRel = skillsFolderRel;

            HeaderText.Text = isNew ? "Nowy zawodnik" : ("Edycja zawodnika: " + (model.Imie ?? ""));

            // Dane -> pola
            NameBox.Text = model.Imie ?? "";
            EloBox.Text = model.Elo.ToString(CultureInfo.InvariantCulture);
            StrengthBox.Text = model.Sila.ToString(CultureInfo.InvariantCulture);
            FightBox.Text = model.Walka.ToString(CultureInfo.InvariantCulture);
            EnergyBox.Text = model.Energia.ToString(CultureInfo.InvariantCulture);
            EnduranceBox.Text = model.Wytrzymalosc.ToString(CultureInfo.InvariantCulture);
            SpeedBox.Text = model.Predkosc.ToString(CultureInfo.InvariantCulture);
            TonsBox.Text = model.Tony.ToString(CultureInfo.InvariantCulture);

            // Flagi L/R (co najmniej jedna)
            FlagL.IsChecked = model.PortretL;
            FlagR.IsChecked = model.PortretR;
            if (FlagL.IsChecked == false && FlagR.IsChecked == false) FlagL.IsChecked = true;

            // Portret
            if (!string.IsNullOrWhiteSpace(model.PortretPath))
            {
                var abs = Abs(model.PortretPath);
                if (File.Exists(abs))
                    PortraitPreview.Source = new BitmapImage(new Uri(abs));
            }

            // Umiejętności / Słabości
            foreach (var u in model.Umiejetnosci) _skillsView.Add(new ArenaFights.Models.UmiejetnoscRef { Nazwa = u.Nazwa, IkonaPath = u.IkonaPath });
            foreach (var w in model.Slabosci) _weaknessesView.Add(new ArenaFights.Models.UmiejetnoscRef { Nazwa = w.Nazwa, IkonaPath = w.IkonaPath });
            SkillsItems.ItemsSource = _skillsView;
            WeaknessItems.ItemsSource = _weaknessesView;
        }

        // --- Portret: z pliku / ze schowka / usuń ---

        private void PortraitFromFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Wybierz obraz portretu"
            };
            if (ofd.ShowDialog() == true)
            {
                SaveBitmapToPortrait(new BitmapImage(new Uri(ofd.FileName)));
            }
        }

        private void PortraitPaste_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsImage()) { MessageBox.Show("Schowek nie zawiera obrazu."); return; }
            var bmp = Clipboard.GetImage(); if (bmp == null) { MessageBox.Show("Nie udało się pobrać obrazu."); return; }
            SaveBitmapToPortrait(bmp);
        }

        private void PortraitRemove_Click(object sender, RoutedEventArgs e)
        {
            Model.PortretPath = null;
            PortraitPreview.Source = null;
        }

        private void SaveBitmapToPortrait(BitmapSource bmp)
        {
            var fileName = Guid.NewGuid().ToString("N") + ".png";
            var rel = _portraitsFolderRel + "/" + fileName;
            var abs = Abs(rel);

            var dir = Path.GetDirectoryName(abs);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = new FileStream(abs, FileMode.Create, FileAccess.Write))
                encoder.Save(fs);

            Model.PortretPath = rel;
            PortraitPreview.Source = new BitmapImage(new Uri(abs));
        }

        // --- Umiejętności / Słabości ---

        private void AddSkillFromCatalog_Click(object sender, RoutedEventArgs e)
        {
            var catalog = SkillsCatalogProvider.Load();
            if (catalog.Count == 0) { MessageBox.Show("Katalog umiejętności jest pusty."); return; }

            var picker = new SkillsPickerWindow(catalog) { Owner = Application.Current.MainWindow };
            if (picker.ShowDialog() == true && picker.Selected != null)
            {
                _skillsView.Add(new ArenaFights.Models.UmiejetnoscRef { Nazwa = picker.Selected.Nazwa, IkonaPath = picker.Selected.IkonaPath });
            }
        }

        private void RemoveSkill_Click(object sender, RoutedEventArgs e)
        {
            if (_skillsView.Any())
                _skillsView.RemoveAt(_skillsView.Count - 1);
        }

        private void AddWeaknessFromCatalog_Click(object sender, RoutedEventArgs e)
        {
            var catalog = SkillsCatalogProvider.Load();
            if (catalog.Count == 0) { MessageBox.Show("Katalog umiejętności jest pusty."); return; }

            var picker = new SkillsPickerWindow(catalog) { Owner = Application.Current.MainWindow };
            if (picker.ShowDialog() == true && picker.Selected != null)
            {
                _weaknessesView.Add(new ArenaFights.Models.UmiejetnoscRef { Nazwa = picker.Selected.Nazwa, IkonaPath = picker.Selected.IkonaPath });
            }
        }

        private void RemoveWeakness_Click(object sender, RoutedEventArgs e)
        {
            if (_weaknessesView.Any())
                _weaknessesView.RemoveAt(_weaknessesView.Count - 1);
        }

        // --- Zapis / Zamknięcie karty ---

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text)) { FieldError("Imię"); return; }

            int strength, fight, energy, endur, speed;
            decimal tons, elo;

            if (!TryParseInt(StrengthBox.Text, 1, 10, out strength)) { FieldError("Siła (1..10)"); return; }
            if (!TryParseInt(FightBox.Text, 1, 10, out fight)) { FieldError("Walka (1..10)"); return; }
            if (!TryParseInt(EnergyBox.Text, 1, 10, out energy)) { FieldError("Energia (1..10)"); return; }
            if (!TryParseInt(EnduranceBox.Text, 1, 10, out endur)) { FieldError("Wytrzymałość (1..10)"); return; }
            if (!TryParseInt(SpeedBox.Text, 1, 10, out speed)) { FieldError("Prędkość (1..10)"); return; }
            if (!TryParseDecimal(TonsBox.Text, 0.01m, 999m, out tons)) { FieldError("Tony (0,01..999)"); return; }
            if (!TryParseDecimal(EloBox.Text, 0m, 999999m, out elo)) { FieldError("ELO (0..999999)"); return; }

            Model.Imie = NameBox.Text.Trim();
            Model.Sila = strength;
            Model.Walka = fight;
            Model.Energia = energy;
            Model.Wytrzymalosc = endur;
            Model.Predkosc = speed;
            Model.Tony = tons;
            Model.Elo = elo;

            // Flagi L/R – przynajmniej jedna
            var l = FlagL.IsChecked == true;
            var r = FlagR.IsChecked == true;
            if (!l && !r) l = true;
            Model.PortretL = l;
            Model.PortretR = r;

            Model.Umiejetnosci = _skillsView.Select(s => new ArenaFights.Models.UmiejetnoscRef { Nazwa = s.Nazwa, IkonaPath = s.IkonaPath }).ToList();
            Model.Slabosci = _weaknessesView.Select(s => new ArenaFights.Models.UmiejetnoscRef { Nazwa = s.Nazwa, IkonaPath = s.IkonaPath }).ToList();

            if (Saved != null) Saved(this);
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (CloseRequested != null) CloseRequested(this);
        }

        // --- Helpers ---

        private static string Abs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool TryParseInt(string text, int min, int max, out int value)
        {
            value = 0;
            int tmp;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out tmp))
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
            decimal tmp;
            if (!decimal.TryParse(n, NumberStyles.Number, CultureInfo.InvariantCulture, out tmp))
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
