using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ArenaFights.Services
{
    public static class ImageHelpers
    {
        private const string ArenasFolderRel = "assets/arenas";
        private const string ArenasThumbsRel = "assets/arenas/thumbs";

        public sealed class ImportedArenaImage
        {
            public string RelOriginal { get; set; } = string.Empty;
            public string AbsOriginal { get; set; } = string.Empty;
            public string RelThumb { get; set; } = string.Empty;
            public string AbsThumb { get; set; } = string.Empty;
        }

        public static void EnsureArenaFolders()
        {
            Directory.CreateDirectory(Abs(ArenasFolderRel));
            Directory.CreateDirectory(Abs(ArenasThumbsRel));
        }

        public static ImportedArenaImage ImportArenaFromFile(string sourcePath)
        {
            EnsureArenaFolders();

            // Rozpoznanie rozszerzenia
            var ext = Path.GetExtension(sourcePath)?.ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".gif")
                throw new InvalidOperationException("Obsługiwane formaty: PNG/JPG/GIF.");

            var id = Guid.NewGuid().ToString("N");
            var relOriginal = $"{ArenasFolderRel}/{id}{ext}";
            var absOriginal = Abs(relOriginal);

            // Kopiuj oryginał do assets/arenas
            File.Copy(sourcePath, absOriginal, true);

            // Utwórz miniaturę 80px wysokości z pierwszej klatki (dla GIF) lub całego obrazu
            var relThumb = $"{ArenasThumbsRel}/{id}.png";
            var absThumb = Abs(relThumb);
            CreateThumbFromFile(absOriginal, absThumb, 80);

            return new ImportedArenaImage
            {
                RelOriginal = relOriginal,
                AbsOriginal = absOriginal,
                RelThumb = relThumb,
                AbsThumb = absThumb
            };
        }

        public static ImportedArenaImage ImportArenaFromClipboard()
        {
            EnsureArenaFolders();

            if (!Clipboard.ContainsImage())
                throw new InvalidOperationException("Schowek nie zawiera obrazu.");

            var bmp = Clipboard.GetImage();
            if (bmp == null) throw new InvalidOperationException("Nie udało się pobrać obrazu ze schowka.");

            var id = Guid.NewGuid().ToString("N");

            // Oryginał zapisujemy jako PNG
            var relOriginal = $"{ArenasFolderRel}/{id}.png";
            var absOriginal = Abs(relOriginal);
            SaveBitmapSourceAsPng(bmp, absOriginal);

            // Miniatura 80 px
            var relThumb = $"{ArenasThumbsRel}/{id}.png";
            var absThumb = Abs(relThumb);
            CreateThumbFromBitmapSource(bmp, absThumb, 80);

            return new ImportedArenaImage
            {
                RelOriginal = relOriginal,
                AbsOriginal = absOriginal,
                RelThumb = relThumb,
                AbsThumb = absThumb
            };
        }

        private static void CreateThumbFromFile(string absSource, string absThumbPng, int targetHeightPx)
        {
            BitmapSource frame;

            var ext = Path.GetExtension(absSource)?.ToLowerInvariant();
            if (ext == ".gif")
            {
                // Pierwsza klatka GIF-a
                var decoder = new GifBitmapDecoder(new Uri(absSource, UriKind.Absolute), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                frame = decoder.Frames.Count > 0 ? decoder.Frames[0] : null;
            }
            else
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(absSource, UriKind.Absolute);
                bi.EndInit();
                frame = bi;
            }

            if (frame == null) throw new InvalidOperationException("Nie udało się odczytać obrazu.");

            CreateThumbFromBitmapSource(frame, absThumbPng, targetHeightPx);
        }

        private static void CreateThumbFromBitmapSource(BitmapSource src, string absThumbPng, int targetHeightPx)
        {
            // Skala do żądanej wysokości, szerokość proporcjonalnie
            double scale = (double)targetHeightPx / Math.Max(1, src.PixelHeight);
            var transform = new ScaleTransform(scale, scale);

            var tb = new TransformedBitmap(src, transform);
            tb.Freeze();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(tb));

            var dir = Path.GetDirectoryName(absThumbPng);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using (var fs = new FileStream(absThumbPng, FileMode.Create, FileAccess.Write))
                encoder.Save(fs);
        }

        private static void SaveBitmapSourceAsPng(BitmapSource src, string absPath)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(src));
            var dir = Path.GetDirectoryName(absPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using (var fs = new FileStream(absPath, FileMode.Create, FileAccess.Write))
                encoder.Save(fs);
        }

        private static string Abs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
