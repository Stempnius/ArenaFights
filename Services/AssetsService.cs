using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using ArenaFights.Models;

namespace ArenaFights.Services
{
    public static class AssetsService
    {
        public const string PortraitsFolderRel = "assets/portraits";
        public const string SkillsFolderRel = "assets/skills";
        public const string ArenasFolderRel = "assets/arenas";
        private const string ArenasOriginals = "assets/arenas/originals";
        private const string ArenasThumbs = "assets/arenas/thumbs";

        public static void EnsureFolders()
        {
            Directory.CreateDirectory(Abs(PortraitsFolderRel));
            Directory.CreateDirectory(Abs(SkillsFolderRel));
            Directory.CreateDirectory(Abs(ArenasFolderRel));
            Directory.CreateDirectory(Abs(ArenasOriginals));
            Directory.CreateDirectory(Abs(ArenasThumbs));
        }

        public static void MigrateAll(
            IEnumerable<Zawodnik> fighters,
            IEnumerable<SkillEntry> skills,
            IEnumerable<Arena> arenas)
        {
            if (fighters != null) foreach (var f in fighters) MigratePortrait(f);
            if (skills != null) foreach (var s in skills) MigrateSkillIcon(s);
            if (arenas != null) foreach (var a in arenas) MigrateArenaImage(a);
        }

        public static void MigratePortrait(Zawodnik f)
        {
            if (!string.IsNullOrWhiteSpace(f.PortretPath) &&
                f.PortretPath.Replace('\\', '/').StartsWith(PortraitsFolderRel, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var abs = !string.IsNullOrWhiteSpace(f.PortretPathAbsolute)
                        ? f.PortretPathAbsolute
                        : (!string.IsNullOrWhiteSpace(f.PortretPath) ? Abs(f.PortretPath) : null);

            if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs)) return;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(abs, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            var rel = $"{PortraitsFolderRel}/{Guid.NewGuid():N}.png";
            SaveBitmapPng(bmp, Abs(rel));

            f.PortretPath = rel;
        }

        public static void MigrateSkillIcon(SkillEntry s)
        {
            if (!string.IsNullOrWhiteSpace(s.IkonaPath) &&
                s.IkonaPath.Replace('\\', '/').StartsWith(SkillsFolderRel, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var abs = s.IkonaPathAbsolute;
            if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
            {
                if (!string.IsNullOrWhiteSpace(s.IkonaPath))
                {
                    var tryAbs = Abs(s.IkonaPath);
                    if (File.Exists(tryAbs)) abs = tryAbs;
                }
            }
            if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs)) return;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(abs, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            var rel = $"{SkillsFolderRel}/{Guid.NewGuid():N}.png";
            SaveBitmapPng(bmp, Abs(rel));

            s.IkonaPath = rel;
        }

        public static void MigrateArenaImage(Arena a)
        {
            bool thumbsOk = !string.IsNullOrWhiteSpace(a.MiniaturaPathAbsolute) &&
                            a.MiniaturaPathAbsolute.Replace('\\', '/').IndexOf(ArenasThumbs, StringComparison.OrdinalIgnoreCase) >= 0;

            bool originalsOk = !string.IsNullOrWhiteSpace(a.ObrazPath) &&
                               a.ObrazPath.Replace('\\', '/').StartsWith(ArenasOriginals, StringComparison.OrdinalIgnoreCase);

            if (thumbsOk && originalsOk) return;

            string srcAbs = FirstExisting(
                a.ObrazPathAbsolute,
                a.ObrazPath,
                a.MiniaturaPathAbsolute
            );

            if (string.IsNullOrWhiteSpace(srcAbs)) return;
            if (!Path.IsPathRooted(srcAbs)) srcAbs = Abs(srcAbs);
            if (!File.Exists(srcAbs)) return;

            ImportArenaImage(srcAbs, out var relOriginal, out var relThumb);

            a.ObrazPath = relOriginal;
            a.ObrazPathAbsolute = Abs(relOriginal);
            a.MiniaturaPathAbsolute = Abs(relThumb);
        }

        public static void ImportArenaImage(string sourceAbs, out string relOriginal, out string relThumb)
        {
            relOriginal = null;
            relThumb = null;

            EnsureFolders();

            var id = Guid.NewGuid().ToString("N");
            var ext = SafeExtension(sourceAbs);
            var relOrig = $"{ArenasOriginals}/{id}{ext}";
            var absOrig = Abs(relOrig);

            File.Copy(sourceAbs, absOrig, overwrite: true);

            var relTh = $"{ArenasThumbs}/{id}.png";
            var absTh = Abs(relTh);

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(absOrig, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bmp.EndInit();
            bmp.Freeze();

            var (w, h) = (bmp.PixelWidth, bmp.PixelHeight);
            if (w <= 0 || h <= 0) throw new InvalidDataException("Nieprawidłowe wymiary obrazu areny.");

            double targetH = 200.0;
            double scale = targetH / h;

            var scaled = new TransformedBitmap(bmp, new System.Windows.Media.ScaleTransform(scale, scale));
            SaveBitmapPng(scaled, absTh);

            relOriginal = relOrig;
            relThumb = relTh;
        }

        private static void SaveBitmapPng(BitmapSource src, string absPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absPath));
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(src));
            using var fs = new FileStream(absPath, FileMode.Create, FileAccess.Write);
            encoder.Save(fs);
        }

        private static string SafeExtension(string path)
        {
            var ext = (Path.GetExtension(path) ?? "").ToLowerInvariant();
            return ext is ".png" or ".jpg" or ".jpeg" or ".gif" ? ext : ".png";
        }

        private static string FirstExisting(params string[] candidates)
        {
            foreach (var c in candidates.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var p = c;
                if (!Path.IsPathRooted(p)) p = Abs(p);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static string Abs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
