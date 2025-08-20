using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ArenaFights.Editors;   // FighterEditorControl
using ArenaFights.Models;

namespace ArenaFights.Tabs
{
    public partial class FightersTab : UserControl
    {
        private ObservableCollection<Zawodnik> _source = new();

        private string _portraitsFolderRel = "assets/portraits";
        private string _skillsFolderRel = "assets/skills";

        private bool _filterMode = false;
        private readonly ObservableCollection<FighterFilterRow> _filterRows = new();
        private readonly IntToBlankConverter _intToBlank = new();

        // Do statusbara
        public event Action<string> SelectedNameChanged;

        // >>> NOWOŚĆ: prosimy MainWindow o otwarcie edytora w KARcie
        public event Action<UserControl, string> OpenEditorRequested;

        public FightersTab()
        {
            InitializeComponent();
            BuildPrettyColumns();
            FightersGrid.SelectionChanged += FightersGrid_SelectionChanged;
        }

        public void Initialize(string portraitsFolderRel, string skillsFolderRel)
        {
            _portraitsFolderRel = portraitsFolderRel ?? _portraitsFolderRel;
            _skillsFolderRel = skillsFolderRel ?? _skillsFolderRel;
        }

        public void SetSource(ObservableCollection<Zawodnik> fighters)
        {
            _source = fighters ?? new ObservableCollection<Zawodnik>();
            FightersGrid.ItemsSource = _source;
            FightersGrid.Items.Refresh();
        }

        public void RefreshAfterExternalChange()
        {
            if (_filterMode)
            {
                RebuildFilterRows();
                FightersGrid.ItemsSource = _filterRows;
            }
            FightersGrid.Items.Refresh();
        }

        // ===== UI „ładne” =====
        private void BuildPrettyColumns()
        {
            FightersGrid.Columns.Clear();

            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Miejsce", Binding = new Binding("Miejsce"), Width = 60 });

            var colPortrait = new DataGridTemplateColumn { Header = "Portret", Width = 70, IsReadOnly = true };
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.WidthProperty, 48.0);
            borderFactory.SetValue(Border.HeightProperty, 48.0);
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);
            borderFactory.SetValue(Border.ClipToBoundsProperty, true);

            var imgFactory = new FrameworkElementFactory(typeof(Image));
            imgFactory.SetBinding(Image.SourceProperty, new Binding("PortretPathAbsolute"));
            imgFactory.SetValue(Image.StretchProperty, Stretch.Uniform);
            imgFactory.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);

            borderFactory.AppendChild(imgFactory);
            colPortrait.CellTemplate = new DataTemplate { VisualTree = borderFactory };
            FightersGrid.Columns.Add(colPortrait);

            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Zawodnik", Binding = new Binding("Imie"), Width = 180 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "ELO", Binding = new Binding("Elo") { StringFormat = "F0" }, Width = 80 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Siła", Binding = new Binding("Sila"), Width = 60 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Walka", Binding = new Binding("Walka"), Width = 60 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Energia", Binding = new Binding("Energia"), Width = 70 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Wytrzymałość", Binding = new Binding("Wytrzymalosc"), Width = 100 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Prędkość", Binding = new Binding("Predkosc"), Width = 80 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Tony", Binding = new Binding("Tony") { StringFormat = "0.##" }, Width = 70 });

            FightersGrid.Columns.Add(MakeIconsColumn("Umiejętności", "Umiejetnosci"));
            FightersGrid.Columns.Add(MakeIconsColumn("Słabości", "Slabosci", weakBorder: true));
        }

        private DataGridTemplateColumn MakeIconsColumn(string header, string itemsPath, bool weakBorder = false)
        {
            var col = new DataGridTemplateColumn { Header = header, Width = 220, IsReadOnly = true };

            var itemsFactory = new FrameworkElementFactory(typeof(ItemsControl));
            itemsFactory.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(itemsPath));
            var panelTemplate = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)));
            itemsFactory.SetValue(ItemsControl.ItemsPanelProperty, panelTemplate);

            var imageFactory = new FrameworkElementFactory(typeof(Image));
            imageFactory.SetBinding(Image.SourceProperty, new Binding("IkonaPathAbsolute"));
            imageFactory.SetValue(Image.WidthProperty, 24.0);
            imageFactory.SetValue(Image.HeightProperty, 24.0);
            imageFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(2));
            imageFactory.SetValue(Image.StretchProperty, Stretch.Uniform);
            imageFactory.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);

            FrameworkElementFactory root;
            if (weakBorder)
            {
                var border = new FrameworkElementFactory(typeof(Border));
                border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
                border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(0xAA, 0xE5, 0x73, 0x73)));
                border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
                border.SetValue(FrameworkElement.MarginProperty, new Thickness(2));
                border.AppendChild(imageFactory);
                root = border;
            }
            else
            {
                var wrapper = new FrameworkElementFactory(typeof(Border));
                wrapper.SetValue(Border.BackgroundProperty, Brushes.Transparent);
                wrapper.SetValue(FrameworkElement.MarginProperty, new Thickness(2));
                wrapper.AppendChild(imageFactory);
                root = wrapper;
            }

            var dataTemplate = new DataTemplate { VisualTree = root };
            itemsFactory.SetValue(ItemsControl.ItemTemplateProperty, dataTemplate);
            col.CellTemplate = new DataTemplate { VisualTree = itemsFactory };
            return col;
        }

        // ===== Filtry =====
        private void BuildFilterColumns()
        {
            FightersGrid.Columns.Clear();

            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Miejsce", Binding = new Binding("Miejsce"), Width = 60 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Zawodnik", Binding = new Binding("Imie"), Width = 180 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "ELO", Binding = new Binding("Elo") { StringFormat = "F0" }, Width = 80 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Siła", Binding = new Binding("Sila"), Width = 60 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Walka", Binding = new Binding("Walka"), Width = 60 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Energia", Binding = new Binding("Energia"), Width = 70 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Wytrzymałość", Binding = new Binding("Wytrzymalosc"), Width = 100 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Prędkość", Binding = new Binding("Predkosc"), Width = 80 });
            FightersGrid.Columns.Add(new DataGridTextColumn { Header = "Tony", Binding = new Binding("Tony") { StringFormat = "0.##" }, Width = 70 });

            var catalog = SkillsCatalogProvider.Load();
            var allSkillNames = catalog.Select(s => s.Nazwa)
                                       .Distinct(StringComparer.OrdinalIgnoreCase)
                                       .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                                       .ToList();

            foreach (var name in allSkillNames)
            {
                FightersGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = name,
                    Binding = new Binding($"Flags[{name}]") { Converter = _intToBlank },
                    Width = 80
                });
            }
            foreach (var name in allSkillNames)
            {
                var key = "S: " + name;
                FightersGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = key,
                    Binding = new Binding($"Flags[{key}]") { Converter = _intToBlank },
                    Width = 80
                });
            }
        }

        private void RebuildFilterRows()
        {
            _filterRows.Clear();

            var allNames = SkillsCatalogProvider.Load()
                .Select(s => s.Nazwa)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var f in _source)
            {
                var row = new FighterFilterRow(f);
                foreach (var n in allNames)
                {
                    row.Flags[n] = 0;
                    row.Flags["S: " + n] = 0;
                }

                if (f.Umiejetnosci != null)
                    foreach (var u in f.Umiejetnosci)
                        if (!string.IsNullOrWhiteSpace(u.Nazwa))
                            row.Flags[u.Nazwa] = 1;

                if (f.Slabosci != null)
                    foreach (var s in f.Slabosci)
                        if (!string.IsNullOrWhiteSpace(s.Nazwa))
                            row.Flags["S: " + s.Nazwa] = 1;

                _filterRows.Add(row);
            }
        }

        // ===== Handlery =====
        private void BtnNewFighter_Click(object sender, RoutedEventArgs e)
        {
            var model = new Zawodnik
            {
                Imie = "",
                Sila = 5,
                Walka = 5,
                Energia = 5,
                Wytrzymalosc = 5,
                Predkosc = 5,
                Tony = 1.00m,
                Elo = 1000.0000000000m,
                Miejsce = 0
            };

            // >>> zamiast okna – UserControl edytora i prośba o otwarcie karty
            var editor = new FighterEditorControl(model, isNew: true, _portraitsFolderRel, _skillsFolderRel);
            OpenEditorRequested?.Invoke(editor, "Nowy zawodnik");
        }

        private void BtnEditFighter_Click(object sender, RoutedEventArgs e)
        {
            Zawodnik selected = FightersGrid.SelectedItem as Zawodnik;
            if (_filterMode && FightersGrid.SelectedItem is FighterFilterRow r) selected = r.Source;

            if (selected == null)
            {
                MessageBox.Show("Zaznacz zawodnika w tabeli, aby edytować.");
                return;
            }

            var editor = new FighterEditorControl(selected, isNew: false, _portraitsFolderRel, _skillsFolderRel);
            OpenEditorRequested?.Invoke(editor, $"Edycja: {selected.Imie}");
        }

        private void FightersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string name = null;
            if (!_filterMode && FightersGrid.SelectedItem is Zawodnik z) name = z.Imie;
            if (_filterMode && FightersGrid.SelectedItem is FighterFilterRow r) name = r.Imie;
            SelectedNameChanged?.Invoke(name ?? string.Empty);
        }

        private void FilterToggle_Checked(object sender, RoutedEventArgs e)
        {
            _filterMode = true;
            RebuildFilterRows();
            BuildFilterColumns();
            FightersGrid.ItemsSource = _filterRows;
            FightersGrid.Items.Refresh();
        }

        private void FilterToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _filterMode = false;
            BuildPrettyColumns();
            FightersGrid.ItemsSource = _source;
            FightersGrid.Items.Refresh();
        }
    }

    // Modele/konwerter dla filtrów — bez zmian
    public sealed class FighterFilterRow
    {
        public Zawodnik Source { get; }
        public int Miejsce { get; set; }
        public string Imie { get; set; } = "";
        public decimal Elo { get; set; }
        public int Sila { get; set; }
        public int Walka { get; set; }
        public int Energia { get; set; }
        public int Wytrzymalosc { get; set; }
        public int Predkosc { get; set; }
        public decimal Tony { get; set; }
        public System.Collections.Generic.Dictionary<string, int> Flags { get; } =
            new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public FighterFilterRow(Zawodnik src)
        {
            Source = src;
            Imie = src.Imie ?? "";
            Elo = src.Elo;
            Sila = src.Sila;
            Walka = src.Walka;
            Energia = src.Energia;
            Wytrzymalosc = src.Wytrzymalosc;
            Predkosc = src.Predkosc;
            Tony = src.Tony;
            Miejsce = src.Miejsce;
        }
    }

    public sealed class IntToBlankConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return "";
                if (value is int i) return i == 0 ? "" : "1";
                if (int.TryParse(value.ToString(), out var v)) return v == 0 ? "" : "1";
                return "";
            }
            catch { return ""; }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
