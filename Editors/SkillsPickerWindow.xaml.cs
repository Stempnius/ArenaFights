using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ArenaFights.Models;

namespace ArenaFights
{
    public partial class SkillsPickerWindow : Window
    {
        private readonly List<SkillEntry> _all;
        public SkillEntry Selected { get; private set; }

        public SkillsPickerWindow(IEnumerable<SkillEntry> catalog)
        {
            InitializeComponent();
            _all = (catalog ?? Enumerable.Empty<SkillEntry>()).ToList();
            List.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var q = (SearchBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(q))
            {
                List.ItemsSource = _all;
            }
            else
            {
                List.ItemsSource = _all.Where(s => s.Nazwa != null &&
                                                   s.Nazwa.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                                       .ToList();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Selected = List.SelectedItem as SkillEntry;
            if (Selected == null)
            {
                MessageBox.Show("Wskaż umiejętność z listy.");
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
