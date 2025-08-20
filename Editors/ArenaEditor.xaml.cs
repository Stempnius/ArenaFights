using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using ArenaFights.Models;
using WpfAnimatedGif; // NUget: WpfAnimatedGif

namespace ArenaFights
{
    public partial class ArenaEditor : Window
    {
        private const string ArenasFolderRel = "assets/arenas";
        private const string ThumbsFolderRel = "assets/arenas/thumbs";

        private readonly Models.Arena _model;
        private readonly List<SkillEntry> _skillsCatalog;
        private readonly ObservableCollection<Models.SkillRef> _cechyView = new ObservableCollection<Models.SkillRef>();

        public ArenaEditor(Models.Arena model, List<SkillEntry> skillsCatalog)
        {
            InitializeComponent();
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _skillsCatalog = skillsCatalog ?? new List<SkillEntry>();

            NameBox.Text = _model.Nazwa ?? "";

            // Podgląd obrazu, jeśli już jest
            if (!string.IsNullOrWhiteSpace(_model.ObrazPathAbsolute) && File.Exists(_model.ObrazPathAbsolute))
                SetPreviewAnimated(_model.ObrazPathAbsolute);

            // Cechy -> widok
            _cechyView.Clear();
            if (_model.Cechy != null)
                foreach (var c in _model.Cechy)
                    _cechyView.Add(new Models.SkillRef { Nazwa = c.Nazwa, IkonaPathAbsolute = c.IkonaPathAbsolute });
            CechyList.ItemsSource = _cechyView;

            // Katalog umiejętności do ComboBoxa
            SkillsCombo.ItemsSource = _skillsCatalog
                .OrderBy(s => s.Nazwa, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // foldery
            Directory.CreateDirectory(AbsDir(ArenasFolderRel));
            Directory.CreateDirectory(AbsDir(ThumbsFolderRel));
        }

        // ============ Obraz ============

        private void UploadFromFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Wybierz obraz areny",
                Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.gif",
                Multiselect = false
            };
            if (ofd.ShowDialog(this) != true) return;

            try
            {
                var srcPath = ofd.FileName;
                var ext = Path.GetExtension(srcPath)?.ToLowerInvariant() ?? ".png";
                var fileName = Guid.NewGuid().ToString("N") + ext;

                var rel = $"{ArenasFolderRel}/{fileName}";
                var abs = Abs(rel);

                Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
                File.Copy(srcPath, abs, overwrite: true);

                _model.ObrazPath = rel;
                _model.ObrazPathAbsolute = abs;

                // Miniatura (dla listy):
                if (ext == ".gif")
                {
                    // Dla GIF użyjemy oryginału jako miniatury (lista i tak pokazuje statycznie)
                    _model.MiniaturaPathAbsolute = abs;
                }
                else
                {
                    var thumbAbs = Abs($"{ThumbsFolderRel}/{Path.GetFileNameWithoutExtension(fileName)}.png");
                    GenerateThumbnail(abs, thumbAbs, targetHeight: 250); // skala pod listę aren
                    _model.MiniaturaPathAbsolute = thumbAbs;
                }

                // Podgląd z animacją (GIF)
                SetPreviewAnimated(abs);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu obrazu: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PasteFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsImage())
            {
                MessageBox.Show("Schowek nie zawiera obrazu.", "Informacja");
                return;
            }

            try
            {
                var img = Clipboard.GetImage();
                if (img == null)
                {
                    MessageBox.Show("Nie udało się odczytać obrazu ze schowka.", "Błąd");
                    return;
                }

                var fileName = Guid.NewGuid().ToString("N") + ".png";
                var rel = $"{ArenasFolderRel}/{fileName}";
                var abs = Abs(rel);

                SaveBitmapSourceToPng(img, abs);

                _model.ObrazPath = rel;
                _model.ObrazPathAbsolute = abs;

                var thumbAbs = Abs($"{ThumbsFolderRel}/{Path.GetFileNameWithoutExtension(fileName)}.png");
                GenerateThumbnail(abs, thumbAbs, targetHeight: 250);
                _model.MiniaturaPathAbsolute = thumbAbs;

                SetPreviewAnimated(abs);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd przy wklejaniu obrazu: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            _model.ObrazPath = "";
            _model.ObrazPathAbsolute = "";
            _model.MiniaturaPathAbsolute = "";
            ImageBehavior.SetAnimatedSource(ArenaPreview, null);
            ArenaPreview.Source = null;
        }

        private void SetPreviewAnimated(string absPath)
        {
            // Jeżeli GIF – ustaw AnimatedSource; jeżeli PNG/JPG – zwykłe źródło
            var ext = Path.GetExtension(absPath)?.ToLowerInvariant();
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(absPath);
            bi.CacheOption = BitmapCacheOption.OnLoad;  // zwolnij blokadę pliku
            bi.EndInit();

            if (ext == ".gif")
            {
                // animowany GIF
                ImageBehavior.SetAnimatedSource(ArenaPreview, bi);
            }
            else
            {
                // zwykły obraz
                ImageBehavior.SetAnimatedSource(ArenaPreview, null);
                ArenaPreview.Source = bi;
            }
        }

        // ============ Cechy ============

        private void AddSkill_Click(object sender, RoutedEventArgs e)
        {
            if (SkillsCombo.SelectedItem is not SkillEntry sel)
            {
                MessageBox.Show("Wybierz cechę z listy.");
                return;
            }

            if (_cechyView.Any(x => string.Equals(x.Nazwa, sel.Nazwa, StringComparison.OrdinalIgnoreCase)))
                return;

            _cechyView.Add(new Models.SkillRef
            {
                Nazwa = sel.Nazwa,
                IkonaPathAbsolute = sel.IkonaPathAbsolute
            });
        }

        private void RemoveSkill_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is Models.SkillRef s)
                _cechyView.Remove(s);
        }

        // ============ OK / Anuluj ============

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var name = (NameBox.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Nazwa areny nie może być pusta.");
                return;
            }

            _model.Nazwa = name;
            _model.Cechy = _cechyView.ToList();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ============ Helpers ============

        private static string Abs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string AbsDir(string rel) => Abs(rel);

        private static void SaveBitmapSourceToPng(BitmapSource bmp, string absPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using var fs = new FileStream(absPath, FileMode.Create, FileAccess.Write);
            encoder.Save(fs);
        }

        private static void GenerateThumbnail(string srcAbs, string dstAbs, int targetHeight)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(srcAbs);
            if (targetHeight > 0) bi.DecodePixelHeight = targetHeight; // skala do żądanej wysokości
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();

            SaveBitmapSourceToPng(bi, dstAbs);
        }
    }
}
