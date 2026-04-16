using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using wpf_projekt.models;
using wpf_projekt.Models;

namespace wpf_projekt.Views
{
    public partial class SummaryView : UserControl
    {
        public SummaryView()
        {
            InitializeComponent();

            this.Loaded += SummaryView_Loaded;
        }

        private void Transactions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Load();
        }

        private void LoadFilters()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;

            // KONTA
            AccountComboBox.Items.Clear();
            AccountComboBox.Items.Add("Wszystkie");

            foreach (var acc in mainWindow.Accounts)
            {
                AccountComboBox.Items.Add(acc);
            }

            AccountComboBox.SelectedIndex = 0;

            // ROK
            YearComboBox.Items.Clear();
            YearComboBox.Items.Add("Wszystkie");

            var years = mainWindow.Transactions
                .Select(x => x.Date.Year)
                .Distinct()
                .OrderByDescending(x => x);

            foreach (var y in years)
                YearComboBox.Items.Add(y.ToString());

            YearComboBox.SelectedIndex = 0;

            // MIESIĄC
            MonthComboBox.Items.Clear();
            MonthComboBox.Items.Add(new ComboBoxItem { Content = "Wszystkie", Tag = null });

            for (int i = 1; i <= 12; i++)
            {
                MonthComboBox.Items.Add(new ComboBoxItem
                {
                    Content = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i),
                    Tag = i
                });
            }

            MonthComboBox.SelectedIndex = 0;
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            Load();
        }

        private void Load()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            var data = mainWindow.Transactions.AsEnumerable();

            // FILTR KONTA
            if (AccountComboBox.SelectedItem != null && AccountComboBox.SelectedItem.ToString() != "Wszystkie")
            {
                var selected = AccountComboBox.SelectedItem as AccountListItem;

                if (selected != null)
                {
                    if (selected.Kind == AccountKind.Personal)
                        data = data.Where(t => t.PersonalAccountId == selected.Id);
                    else
                        data = data.Where(t => t.SharedAccountId == selected.Id);
                }
            }

            // FILTR ROKU
            var year = YearComboBox.SelectedItem?.ToString();
            if (year != "Wszystkie" && !string.IsNullOrEmpty(year))
            {
                data = data.Where(x => x.Date.Year.ToString() == year);
            }

            // FILTR MIESIĄCA
            if (MonthComboBox.SelectedItem is ComboBoxItem m && m.Tag != null)
            {
                int month = (int)m.Tag;
                data = data.Where(x => x.Date.Month == month);
            }

            var income = data.Where(x => x.IsPositive).Sum(x => x.Amount);
            var expense = data.Where(x => !x.IsPositive).Sum(x => x.Amount);
            var balance = income - expense;

            IncomeText.Text = $"{income:F2} zł";
            ExpenseText.Text = $"{expense:F2} zł";
            BalanceText.Text = $"{balance:F2} zł";

            BalanceText.Foreground = balance >= 0
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;
        }
        private void SummaryView_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;

            mainWindow.Transactions.CollectionChanged += Transactions_CollectionChanged;

            LoadFilters();
            Load();
        }
    }
}