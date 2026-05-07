using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using wpf_projekt.models;
using wpf_projekt.Models;
using wpf_projekt.Repositories;
using wpf_projekt.Services;

namespace wpf_projekt.ViewModels
{
    public partial class CsvMappingViewModel : ObservableObject
    {
        //  Zależności 
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly MainViewModel _mainVm;
        private readonly List<Dictionary<string, string>> _rows;

        //  Zdarzenia 
        public event Action<int>? ImportCompleted;   // int = liczba zapisanych
        public event Action? Cancelled;

        //  Kolumny wykryte z pliku 
        public ObservableCollection<string> AvailableColumns { get; } = new();

        //  Zapisane profile 
        public ObservableCollection<CsvMappingProfile> SavedProfiles { get; } = new();

        //  Pola mapowania 
        [ObservableProperty] private string? _selectedColumnDate;
        [ObservableProperty] private string? _selectedColumnAmount;
        [ObservableProperty] private string? _selectedColumnDescription;
        [ObservableProperty] private string? _selectedColumnIsPositive;
        [ObservableProperty] private bool _amountSignDeterminesDirection = true;
        [ObservableProperty] private string _dateFormat = "dd.MM.yyyy";
        [ObservableProperty] private string _profileName = "Mój bank";
        [ObservableProperty] private string _delimiter = ";";

        //  Konta 
        public ObservableCollection<AccountListItem> Accounts { get; } = new();
        [ObservableProperty] private AccountListItem? _selectedAccount;

        //  Kategorie 
        public ObservableCollection<TransactionType> Categories { get; } = new();
        [ObservableProperty] private TransactionType? _selectedCategory;

        //  Stan UI 
        [ObservableProperty] private string _previewText = string.Empty;
        [ObservableProperty] private int _totalRowCount;
        [ObservableProperty] private bool _isBusy;

        //  Błędy walidacji 
        [ObservableProperty] private string _validationMessage = string.Empty;
        [ObservableProperty] private bool _isValid;

        public CsvMappingViewModel(
            string[] headers,
            List<Dictionary<string, string>> rows,
            ITransactionRepository transactionRepository,
            ICategoryRepository categoryRepository,
            IAccountRepository accountRepository,
            MainViewModel mainVm)
        {
            _rows = rows;
            _transactionRepository = transactionRepository;
            _categoryRepository = categoryRepository;
            _accountRepository = accountRepository;
            _mainVm = mainVm;
            TotalRowCount = rows.Count;

            // Kolumny z pliku + opcja "—" (brak)
            AvailableColumns.Add("—");
            foreach (var h in headers) AvailableColumns.Add(h);

            // Wypełnij konta i kategorie z MainVm
            foreach (var a in mainVm.Accounts) Accounts.Add(a);
            foreach (var c in mainVm.Categories) Categories.Add(c);

            SelectedAccount = Accounts.FirstOrDefault();
            SelectedCategory = Categories.FirstOrDefault(c =>
                c.Name.Equals("Import", StringComparison.OrdinalIgnoreCase))
                ?? Categories.FirstOrDefault();

            // Załaduj zapisane profile
            foreach (var p in CsvImportService.LoadSavedProfiles())
                SavedProfiles.Add(p);

            // Auto-mapa: wykryj kolumny po popularnych nazwach
            AutoDetectColumns(headers);
            Validate();
        }

        //  Auto-detekcja nazw kolumn 

        private void AutoDetectColumns(string[] headers)
        {
            SelectedColumnDate = FindColumn(headers, "data", "date", "Data operacji", "Data transakcji");
            SelectedColumnAmount = FindColumn(headers, "kwota", "amount", "Kwota", "Wartość");
            SelectedColumnDescription = FindColumn(headers, "opis", "description", "Tytuł", "Tytul");
            SelectedColumnIsPositive = FindColumn(headers, "typ", "type", "Rodzaj transakcji");

            BuildPreview();
        }

        private static string? FindColumn(string[] headers, params string[] candidates)
        {
            foreach (var c in candidates)
            {
                var match = headers.FirstOrDefault(h =>
                    h.Contains(c, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            return null;
        }

        //  Walidacja 

        partial void OnSelectedColumnDateChanged(string? value) { Validate(); BuildPreview(); }
        partial void OnSelectedColumnAmountChanged(string? value) { Validate(); BuildPreview(); }
        partial void OnSelectedColumnDescriptionChanged(string? value) { BuildPreview(); }
        partial void OnAmountSignDeterminesDirectionChanged(bool value) { Validate(); }

        private void Validate()
        {
            var msgs = new List<string>();
            if (string.IsNullOrWhiteSpace(SelectedColumnDate) || SelectedColumnDate == "—")
                msgs.Add("• Kolumna Data jest wymagana.");
            if (string.IsNullOrWhiteSpace(SelectedColumnAmount) || SelectedColumnAmount == "—")
                msgs.Add("• Kolumna Kwota jest wymagana.");
            if (SelectedAccount == null)
                msgs.Add("• Wybierz konto docelowe.");
            if (SelectedCategory == null)
                msgs.Add("• Wybierz kategorię.");

            ValidationMessage = msgs.Count > 0 ? string.Join("\n", msgs) : string.Empty;
            IsValid = msgs.Count == 0;
        }

        //  Podgląd pierwszego wiersza 

        private void BuildPreview()
        {
            if (_rows.Count == 0) { PreviewText = "Brak danych."; return; }
            var first = _rows.First();

            string Get(string? col) =>
                !string.IsNullOrEmpty(col) && col != "—" && first.TryGetValue(col, out var v)
                    ? v : "—";

            PreviewText =
                $"Data:  {Get(SelectedColumnDate)}\n" +
                $"Kwota: {Get(SelectedColumnAmount)}\n" +
                $"Opis:  {Get(SelectedColumnDescription)}";
        }

        //  Wczytaj zapisany profil 

        [RelayCommand]
        private void LoadProfile(CsvMappingProfile profile)
        {
            ProfileName = profile.ProfileName;
            DateFormat = profile.DateFormat;
            AmountSignDeterminesDirection = profile.AmountSignDeterminesDirection;

            SelectedColumnDate = AvailableColumns.Contains(profile.ColumnDate ?? "") ? profile.ColumnDate : null;
            SelectedColumnAmount = AvailableColumns.Contains(profile.ColumnAmount ?? "") ? profile.ColumnAmount : null;
            SelectedColumnDescription = AvailableColumns.Contains(profile.ColumnDescription ?? "") ? profile.ColumnDescription : null;
            SelectedColumnIsPositive = AvailableColumns.Contains(profile.ColumnIsPositive ?? "") ? profile.ColumnIsPositive : null;

            var cat = Categories.FirstOrDefault(c =>
                c.Name.Equals(profile.DefaultCategoryName, StringComparison.OrdinalIgnoreCase));
            if (cat != null) SelectedCategory = cat;

            BuildPreview();
            Validate();
        }

        //  Zapisz profil 

        [RelayCommand]
        private void SaveProfile()
        {
            var profile = BuildProfile();
            try
            {
                CsvImportService.SaveProfile(profile);
                // Odśwież listę
                SavedProfiles.Clear();
                foreach (var p in CsvImportService.LoadSavedProfiles()) SavedProfiles.Add(p);
                MessageBox.Show("Schemat mapowania został zapisany.", "Sukces",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu profilu: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //  Import 

        [RelayCommand]
        private async Task ImportAsync()
        {
            Validate();
            if (!IsValid)
            {
                MessageBox.Show(ValidationMessage, "Uzupełnij wymagane pola",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsBusy = true;
            try
            {
                var profile = BuildProfile();
                var existing = await _transactionRepository.GetAllWithDetailsAsync();

                int? personalId = SelectedAccount!.Kind == AccountKind.Personal ? SelectedAccount.Id : null;
                int? sharedId = SelectedAccount.Kind == AccountKind.Shared ? SelectedAccount.Id : null;

                var result = CsvImportService.MapToTransactions(
                    _rows, profile, SelectedCategory!, personalId, sharedId, existing);

                if (result.Errors.Count > 0 && result.Imported.Count == 0)
                {
                    MessageBox.Show(
                        $"Wystąpiły błędy – żadna transakcja nie została zaimportowana:\n\n" +
                        string.Join("\n", result.Errors.Take(10)),
                        "Błąd importu", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (result.Imported.Count == 0)
                {
                    MessageBox.Show(
                        $"Wszystkie {result.Duplicates} transakcji już istnieją w bazie.",
                        "Brak nowych danych", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await _transactionRepository.AddRangeAsync(result.Imported);

                // Aktualizacja salda konta
                await UpdateAccountBalanceAsync(result.Imported);

                await _mainVm.LoadDataAsync();

                string summary =
                    $"Zaimportowano: {result.Imported.Count} transakcji\n" +
                    (result.Duplicates > 0 ? $"⏭ Pominięto duplikatów: {result.Duplicates}\n" : "") +
                    (result.Errors.Count > 0 ? $"⚠ Błędnych wierszy: {result.Errors.Count}" : "");

                MessageBox.Show(summary, "Import zakończony",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ImportCompleted?.Invoke(result.Imported.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd importu: {ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void Cancel() => Cancelled?.Invoke();

        //  Helpery 

        private CsvMappingProfile BuildProfile() => new()
        {
            ProfileName = ProfileName.Trim(),
            Delimiter = Delimiter == "," ? ',' : ';',
            ColumnDate = SelectedColumnDate == "—" ? null : SelectedColumnDate,
            ColumnAmount = SelectedColumnAmount == "—" ? null : SelectedColumnAmount,
            ColumnDescription = SelectedColumnDescription == "—" ? null : SelectedColumnDescription,
            ColumnIsPositive = SelectedColumnIsPositive == "—" ? null : SelectedColumnIsPositive,
            AmountSignDeterminesDirection = AmountSignDeterminesDirection,
            DateFormat = DateFormat,
            DefaultCategoryName = SelectedCategory?.Name ?? "Import"
        };

        private async Task UpdateAccountBalanceAsync(List<Transaction> imported)
        {
            if (SelectedAccount == null) return;

            decimal delta = imported.Sum(t => t.IsPositive ? t.Amount : -t.Amount);

            if (delta == 0) return;

            if (SelectedAccount.Kind == AccountKind.Personal)
            {
                var acc = await _accountRepository.GetPersonalAccountByIdAsync(SelectedAccount.Id);
                if (acc != null)
                {
                    acc.Balance += delta;
                    await _accountRepository.UpdatePersonalAccountAsync(acc);
                }
            }
            else if (SelectedAccount.Kind == AccountKind.Shared)
            {
                var acc = await _accountRepository.GetSharedAccountByIdAsync(SelectedAccount.Id);
                if (acc != null)
                {
                    acc.Balance += delta;
                    await _accountRepository.UpdateSharedAccountAsync(acc);
                }
            }
        }
    }
}
