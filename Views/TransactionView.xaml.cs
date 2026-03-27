using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace wpf_projekt.Views
{
    public partial class TransactionsView : UserControl
    {
        public TransactionsView()
        {
            InitializeComponent();
            this.Loaded += TransactionsView_Loaded;
        }

        private void TransactionsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            // 🔹 KATEGORIE
            CategoryFilterComboBox.Items.Clear();
            CategoryFilterComboBox.Items.Add("Wszystkie");

            var transactions = ((MainWindow)Application.Current.MainWindow).Transactions;

            var categories = transactions
                .Select(t => t.TransactionType.Name)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var c in categories)
                CategoryFilterComboBox.Items.Add(c);

            CategoryFilterComboBox.SelectedIndex = 0;

            // 🔹 SORTOWANIE - domyślnie od najnowszej
            DateSortComboBox.SelectedIndex = 0;

            // 🔹 RODZAJ - domyślnie wszystkie
            TypeFilterComboBox.SelectedIndex = 0;

            Apply();
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            Apply();
        }

        private void Apply()
        {
            var data = ((MainWindow)Application.Current.MainWindow).Transactions.AsEnumerable();

            // 🔹 FILTR KATEGORII
            var selectedCategory = CategoryFilterComboBox.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "Wszystkie")
            {
                data = data.Where(t => t.TransactionType.Name == selectedCategory);
            }

            // 🔹 FILTR RODZAJU (wydatek/przychód)
            var selectedType = (TypeFilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (selectedType == "Wydatek")
            {
                data = data.Where(t => !t.IsPositive);
            }
            else if (selectedType == "Przychód")
            {
                data = data.Where(t => t.IsPositive);
            }

            // 🔹 SORTOWANIE
            var selectedSort = (DateSortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (selectedSort == "Od najnowszej")
            {
                data = data.OrderByDescending(t => t.Date);
            }
            else if (selectedSort == "Od najstarszej")
            {
                data = data.OrderBy(t => t.Date);
            }

            TransactionsGrid.ItemsSource = data.ToList();
        }
    }
}