using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using wpf_projekt.Models;
using wpf_projekt.models;
using System.Windows.Input;
using wpf_projekt.Services;
using wpf_projekt.Repositories;
using wpf_projekt.Converters;



namespace wpf_projekt.ViewModels
{
    /// <summary>
    /// ViewModel dla zakładki Transakcje.
    /// Pobiera dane z MainViewModel (wspólna kolekcja Transactions).
    /// </summary>
    public partial class TransactionsViewModel : ObservableObject
    {
        private readonly MainViewModel _mainVm;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAccountRepository _accountRepository;


        //  Kolekcje filtrów 
        public ObservableCollection<string> AvailableYears { get; } = new();
        public ObservableCollection<MonthItem> AvailableMonths { get; } = new();
        public ObservableCollection<string> AvailableCategories { get; } = new();

        //  Wynik filtrowania 
        [ObservableProperty] private List<Transaction> _filteredTransactions = new();

        //  Filtry 
        [ObservableProperty] private string _selectedYear = "Wszystkie";
        [ObservableProperty] private MonthItem? _selectedMonth;
        [ObservableProperty] private string _selectedCategory = "Wszystkie";
        [ObservableProperty] private string _selectedType = "Wszystkie";   // Wszystkie / Wydatek / Przychód
        [ObservableProperty] private string _selectedSort = "Od najnowszej"; // Od najnowszej / Od najstarszej

        public TransactionsViewModel(MainViewModel mainVm,
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository)
        {
            _mainVm = mainVm;
            _transactionRepository = transactionRepository;
            _categoryRepository = categoryRepository;
            _accountRepository = accountRepository;
            _mainVm.Transactions.CollectionChanged += (_, _) => Refresh();
        }

        //  Wywołane przez widok po załadowaniu 
        public void Load()
        {
            BuildFilters();
            Apply();
        }

        private void BuildFilters()
        {
            var transactions = _mainVm.Transactions;

            // LATA
            AvailableYears.Clear();
            AvailableYears.Add("Wszystkie");
            foreach (var year in transactions.Select(t => t.Date.Year).Distinct().OrderByDescending(y => y))
                AvailableYears.Add(year.ToString());
            SelectedYear = "Wszystkie";

            // MIESIĄCE
            AvailableMonths.Clear();
            AvailableMonths.Add(new MonthItem(null, "Wszystkie"));
            for (int i = 1; i <= 12; i++)
            {
                var name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i);
                AvailableMonths.Add(new MonthItem(i, char.ToUpper(name[0]) + name[1..]));
            }
            SelectedMonth = AvailableMonths[0];

            // KATEGORIE
            AvailableCategories.Clear();
            AvailableCategories.Add("Wszystkie");
            foreach (var cat in transactions
                .Select(t => t.TransactionType?.Name)
                .Where(n => n != null)
                .Distinct()
                .OrderBy(c => c)!)
                AvailableCategories.Add(cat!);
            SelectedCategory = "Wszystkie";
        }

        // Wywoływane przez widok przy zmianie filtra (przez binding)
        partial void OnSelectedYearChanged(string value) => Apply();
        partial void OnSelectedMonthChanged(MonthItem? value) => Apply();
        partial void OnSelectedCategoryChanged(string value) => Apply();
        partial void OnSelectedTypeChanged(string value) => Apply();
        partial void OnSelectedSortChanged(string value) => Apply();

        private void Apply()
        {
            var data = _mainVm.Transactions.AsEnumerable();

            if (SelectedYear != "Wszystkie" && !string.IsNullOrEmpty(SelectedYear))
                data = data.Where(t => t.Date.Year.ToString() == SelectedYear);

            if (SelectedMonth?.Number != null)
                data = data.Where(t => t.Date.Month == SelectedMonth.Number);

            if (SelectedCategory != "Wszystkie" && !string.IsNullOrEmpty(SelectedCategory))
                data = data.Where(t => t.TransactionType?.Name == SelectedCategory);

            if (SelectedType == "Wydatek") data = data.Where(t => !t.IsPositive);
            else if (SelectedType == "Przychód") data = data.Where(t => t.IsPositive);

            data = SelectedSort == "Od najstarszej"
                ? data.OrderBy(t => t.Date)
                : data.OrderByDescending(t => t.Date);

            FilteredTransactions = data.ToList();
        }

        private void Refresh()
        {
            BuildFilters();
            Apply();
        }

        //  Eksport CSV 
        [RelayCommand]
        private void ExportToCsv()
        {
            if (!FilteredTransactions.Any())
            {
                MessageBox.Show("Brak danych do wyeksportowania.", "Informacja",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Plik CSV (*.csv)|*.csv",
                Title = "Eksportuj transakcje",
                FileName = $"Zestawienie_Transakcji_{DateTime.Now:yyyy_MM_dd}.csv"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var csv = new StringBuilder();
                csv.AppendLine("Data;Kategoria;Kwota;Typ;Opis");

                foreach (var t in FilteredTransactions)
                {
                    csv.AppendLine(
                        $"{t.Date:dd.MM.yyyy};" +
                        $"{t.TransactionType?.Name ?? "Brak"};" +
                        $"{t.Amount:F2};" +
                        $"{t.TypeName};" +
                        $"{t.Description?.Replace(";", ",") ?? ""}");
                }

                File.WriteAllText(dlg.FileName, csv.ToString(), Encoding.UTF8);
                MessageBox.Show("Dane zostały pomyślnie zapisane!", "Sukces",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu pliku: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        [RelayCommand]
        private async System.Threading.Tasks.Task ImportCsvAsync()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Pliki CSV (*.csv)|*.csv",
                Title = "Wybierz plik CSV z transakcjami"
            };

            if (dlg.ShowDialog() != true) return;

            if (!dlg.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Wybrano nieprawidłowy format. Proszę wybrać plik .csv.",
                    "Błąd pliku", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Wczytaj i sparsuj plik CSV
                var (headers, rows) = CsvImportService.ParseFile(dlg.FileName);

                // 2. Upewnij się że kategoria "Import" istnieje
                await _categoryRepository.EnsureExistsAsync("Import");

                // 3. Otwórz okno mapowania
                var mappingVm = new wpf_projekt.ViewModels.CsvMappingViewModel(
                    headers, rows,
                    _transactionRepository,
                    _categoryRepository,
                    _accountRepository,
                    _mainVm);

                var mappingWindow = new wpf_projekt.Views.CsvMappingWindow(mappingVm)
                {
                    Owner = Application.Current.MainWindow
                };

                mappingWindow.ShowDialog();
                // Po zamknięciu okna MainVm.LoadDataAsync() zostało już wywołane wewnątrz VM
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Błąd formatu pliku",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wczytywania pliku:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //Pomocniczy rekord reprezentujący miesiąc w filtrze
        public record MonthItem(int? Number, string Label)
        {
            public override string ToString() => Label;
        }
    }
}