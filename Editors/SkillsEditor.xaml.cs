using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ArenaFights.Models;

namespace ArenaFights
{
    public partial class SkillsEditor : Window
    {
        private readonly SkillEntry _model;
        private readonly string _skillsFolderRel;

        public SkillsEditor(SkillEntry model, string skillsFolderRel)
        {
            InitializeComponent();
            _model = model;
            _skillsFolderRel = skillsFolderRel;

            NameBox.Text = _model.Nazwa ?? "";

            // Załaduj podgląd jeśli jest ikona
            if (!string.IsNullOrWhiteSpace(_model.IkonaPath))
            {
                var abs = ToAbs(_model.IkonaPath);
                if (File.Exists(abs))
                    PreviewImage.Source = new BitmapImage(new Uri(abs));
            }
        }

        // === Handlery obrazka ===
        private void UploadFromFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Wybierz ikonę umiejętności",
                Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp"
            };
            if (ofd.ShowDialog(this) != true) return;

            var bmp = new BitmapImage(new Uri(ofd.FileName));
            SavePngToCatalog(bmp);
        }

        private void PasteFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsImage())
            {
                MessageBox.Show("Schowek nie zawiera obrazu.");
                return;
            }
            var bmp = Clipboard.GetImage();
            if (bmp == null)
            {
                MessageBox.Show("Nie udało się pobrać obrazu ze schowka.");
                return;
            }
            SavePngToCatalog(bmp);
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            _model.IkonaPath = null;        // <-- USTAWIAMY TYLKO IkonaPath (NIE IkonaPathAbsolute!)
            PreviewImage.Source = null;
        }

        // Zapisuje bitmapę jako PNG do katalogu assets/skills i wpisuje ŚCIEŻKĘ WZGLĘDNĄ do modelu
        private void SavePngToCatalog(BitmapSource bmp)
        {
            var fileName = Guid.NewGuid().ToString("N") + ".png";
            var rel = _skillsFolderRel.TrimEnd('/') + "/" + fileName;
            var abs = ToAbs(rel);

            Directory.CreateDirectory(Path.GetDirectoryName(abs));
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = new FileStream(abs, FileMode.Create, FileAccess.Write))
                encoder.Save(fs);

            _model.IkonaPath = rel;         // <-- zapis REL, bez grzebania w IkonaPathAbsolute
            PreviewImage.Source = new BitmapImage(new Uri(abs));
        }

        // === OK / Anuluj ===
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var name = (NameBox.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Nazwa nie może być pusta.");
                return;
            }
            _model.Nazwa = name;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // === Helpers ===
        private static string ToAbs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
