using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ArenaFights.Tabs
{
    public partial class SkillsTab : UserControl
    {
        private ObservableCollection<SkillEntry> _source = new();

        public SkillsTab()
        {
            InitializeComponent();
        }

        public void SetSource(ObservableCollection<SkillEntry> skills)
        {
            _source = skills ?? new ObservableCollection<SkillEntry>();
            SkillsList.ItemsSource = _source;
            SkillsList.Items.Refresh();
        }

        public void RefreshAfterExternalChange()
        {
            SkillsList.Items.Refresh();
        }

        private void BtnSkillNew_Click(object sender, RoutedEventArgs e)
        {
            var model = new SkillEntry { Nazwa = "", IkonaPath = null };
            var editor = new SkillsEditor(model, "assets/skills") { Owner = Application.Current.MainWindow };
            if (editor.ShowDialog() == true)
            {
                if (_source.Any(x => string.Equals(x.Nazwa, model.Nazwa, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Umiejętność o tej nazwie już istnieje.");
                    return;
                }
                _source.Add(model);
                SkillsList.Items.Refresh();
            }
        }

        private void BtnSkillEdit_Click(object sender, RoutedEventArgs e)
        {
            if (SkillsList.SelectedItem is not SkillEntry selected)
            {
                MessageBox.Show("Zaznacz umiejętność do edycji.");
                return;
            }

            var copy = new SkillEntry { Nazwa = selected.Nazwa, IkonaPath = selected.IkonaPath };
            var editor = new SkillsEditor(copy, "assets/skills") { Owner = Application.Current.MainWindow };
            if (editor.ShowDialog() == true)
            {
                var nameChanged = !string.Equals(copy.Nazwa, selected.Nazwa, StringComparison.OrdinalIgnoreCase);
                if (nameChanged && _source.Any(x => string.Equals(x.Nazwa, copy.Nazwa, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Umiejętność o tej nazwie już istnieje.");
                    return;
                }

                selected.Nazwa = copy.Nazwa;
                selected.IkonaPath = copy.IkonaPath;
                SkillsList.Items.Refresh();
            }
        }

        private void BtnSkillDelete_Click(object sender, RoutedEventArgs e)
        {
            if (SkillsList.SelectedItem is not SkillEntry selected)
            {
                MessageBox.Show("Zaznacz umiejętność do usunięcia.");
                return;
            }

            if (MessageBox.Show($"Usunąć umiejętność „{selected.Nazwa}”?",
                "Potwierdź", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            _source.Remove(selected);
            SkillsList.Items.Refresh();
        }
    }
}
