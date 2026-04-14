using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using wpf_projekt.models;
using wpf_projekt.Models;

namespace wpf_projekt
{
    public partial class MainWindow : Window
    {
        private readonly AppDbContext _context = new AppDbContext();

        // Kolekcje powiązane z UI
        public ObservableCollection<Transaction> Transactions { get; set; } = new ObservableCollection<Transaction>();
        public ObservableCollection<PersonalAccount> PersonalAccounts { get; set; } = new ObservableCollection<PersonalAccount>();
        public ObservableCollection<SharedAccount> SharedAccounts { get; set; } = new ObservableCollection<SharedAccount>();
        public ObservableCollection<AccountListItem> Accounts { get; set; } = new ObservableCollection<AccountListItem>();
        public ObservableCollection<TransactionType> Categories { get; set; } = new ObservableCollection<TransactionType>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // Pozwala na Binding w XAML

            InitializeDatabase();
        }

        private async void InitializeDatabase()
        {
            await _context.Database.EnsureCreatedAsync();
            await ApplyPendingSchemaUpdatesAsync();

            // Uruchamia Seed danych
            await SeedInitialDataAsync();
            await EnsureTransferCategoryExistsAsync();

            // Ładuje dane do UI
            await LoadDataFromDatabaseAsync();

            TransactionDatePicker.SelectedDate = DateTime.Now;
        }

        private async Task EnsureTransferCategoryExistsAsync()
        {
            bool exists = await _context.TransactionTypes.AnyAsync(t => t.Name == "Transfer");
            if (!exists)
            {
                _context.TransactionTypes.Add(new TransactionType { Name = "Transfer" });
                await _context.SaveChangesAsync();
            }
        }

        private async Task ApplyPendingSchemaUpdatesAsync()
        {
            await TryAddColumnAsync("PersonalAccounts", "Name", "TEXT NOT NULL DEFAULT ''");
            await TryAddColumnAsync("SharedAccounts", "Name", "TEXT NOT NULL DEFAULT ''");
            await TryAddColumnAsync("Transactions", "TransferGroupId", "TEXT NULL");
        }

        private async Task TryAddColumnAsync(string tableName, string columnName, string columnDefinition)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("duplicate column name"))
            {
                // Kolumna juz istnieje - nic nie robimy.
            }
        }

        private async Task SeedInitialDataAsync()
        {
            // Jeśli mamy już jakichś użytkowników, nie dodajemy danych testowych
            if (await _context.Users.AnyAsync()) return;

            // 1. Dodaj typy transakcji
            var types = new[]
            {
                new TransactionType { Name = "Jedzenie" },
                new TransactionType { Name = "Transport" },
                new TransactionType { Name = "Wypłata" },
                new TransactionType { Name = "Rozrywka" },
                new TransactionType { Name = "Transfer" }
            };
            _context.TransactionTypes.AddRange(types);

            // 2. Dodaj użytkownika i konto
            var testUser = new User { FirstName = "Jan", LastName = "Kowalski", Earnings = 5000 };
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync(); // Zapisujemy, by dostać ID użytkownika

            var personalAcc = new PersonalAccount { Name = "Konto glowne", Balance = 2500, UserId = testUser.Id };
            _context.PersonalAccounts.Add(personalAcc);
            var sharedAcc = new SharedAccount
            {
                Name = "Konto wspolne",
                Balance = 1200,
                User1Id = testUser.Id,
                User2Id = testUser.Id
            };
            _context.SharedAccounts.Add(sharedAcc);

            await _context.SaveChangesAsync();
        }

        private async Task LoadDataFromDatabaseAsync()
        {
            // Czyścimy kolekcje lokalne
            Categories.Clear();
            PersonalAccounts.Clear();
            SharedAccounts.Clear();
            Accounts.Clear();
            Transactions.Clear();

            // Pobieramy dane z bazy (Include ładuje relacje)
            var dbCategories = await _context.TransactionTypes.ToListAsync();
            var dbAccounts = await _context.PersonalAccounts.ToListAsync();
            var dbSharedAccounts = await _context.SharedAccounts.ToListAsync();
            var dbTransactions = await _context.Transactions
                .Include(t => t.TransactionType)
                .Include(t => t.PersonalAccount)
                .Include(t => t.SharedAccount)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            // Przypisujemy do ObservableCollection
            dbCategories.ForEach(Categories.Add);
            dbAccounts.ForEach(PersonalAccounts.Add);
            dbSharedAccounts.ForEach(SharedAccounts.Add);
            foreach (var account in dbAccounts)
            {
                Accounts.Add(new AccountListItem
                {
                    Id = account.Id,
                    Name = string.IsNullOrWhiteSpace(account.Name) ? $"Konto osobiste #{account.Id}" : account.Name,
                    Balance = account.Balance,
                    Kind = AccountKind.Personal
                });
            }
            foreach (var account in dbSharedAccounts)
            {
                Accounts.Add(new AccountListItem
                {
                    Id = account.Id,
                    Name = string.IsNullOrWhiteSpace(account.Name) ? $"Konto wspolne #{account.Id}" : account.Name,
                    Balance = account.Balance,
                    Kind = AccountKind.Shared
                });
            }
            dbTransactions.ForEach(Transactions.Add);

            // Ustawienie ComboBoxów
            CategoryComboBox.ItemsSource = Categories;
            AccountComboBox.ItemsSource = Accounts;
            TransferFromComboBox.ItemsSource = Accounts.ToList();
            TransferToComboBox.ItemsSource = Accounts.ToList();

            // Wyświetlanie formatu w ComboBox (jeśli nie ustawiono w XAML)
            CategoryComboBox.DisplayMemberPath = "Name";
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Walidacja
            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount))
            {
                MessageBox.Show("Wprowadź poprawną kwotę.");
                return;
            }

            var selectedType = CategoryComboBox.SelectedItem as TransactionType;
            var selectedAccount = AccountComboBox.SelectedItem as AccountListItem;

            if (selectedType == null || selectedAccount == null)
            {
                MessageBox.Show("Wybierz kategorię i konto!");
                return;
            }

            bool isIncome = IncomeRadio.IsChecked == true;

            // 2. Tworzenie obiektu
            var newTransaction = new Transaction
            {
                Amount = amount,
                IsPositive = isIncome,
                Date = TransactionDatePicker.SelectedDate ?? DateTime.Now,
                Description = DescriptionTextBox.Text,
                TransactionTypeId = selectedType.Id
            };

            try
            {
                decimal updatedBalance;
                if (selectedAccount.Kind == AccountKind.Personal)
                {
                    var account = await _context.PersonalAccounts.FirstOrDefaultAsync(a => a.Id == selectedAccount.Id);
                    if (account == null)
                    {
                        MessageBox.Show("Nie znaleziono konta osobistego.");
                        return;
                    }

                    if (isIncome) account.Balance += amount;
                    else account.Balance -= amount;

                    newTransaction.PersonalAccountId = account.Id;
                    _context.PersonalAccounts.Update(account);
                    updatedBalance = account.Balance;
                }
                else
                {
                    var account = await _context.SharedAccounts.FirstOrDefaultAsync(a => a.Id == selectedAccount.Id);
                    if (account == null)
                    {
                        MessageBox.Show("Nie znaleziono konta wspolnego.");
                        return;
                    }

                    if (isIncome) account.Balance += amount;
                    else account.Balance -= amount;

                    newTransaction.SharedAccountId = account.Id;
                    _context.SharedAccounts.Update(account);
                    updatedBalance = account.Balance;
                }

                // 4. Zapis do bazy
                _context.Transactions.Add(newTransaction);
                await _context.SaveChangesAsync();

                await LoadDataFromDatabaseAsync();
                MessageBox.Show($"Zapisano! Aktualne saldo: {updatedBalance}");
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu: {ex.Message}");
            }
        }

        private void ClearForm()
        {
            AmountTextBox.Clear();
            DescriptionTextBox.Clear();
            TransactionDatePicker.SelectedDate = DateTime.Now;
        }

        private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var accountName = NewAccountNameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(accountName))
            {
                MessageBox.Show("Podaj nazwe konta.");
                return;
            }

            var firstUser = await _context.Users.FirstOrDefaultAsync();
            if (firstUser == null)
            {
                MessageBox.Show("Brak uzytkownika w bazie.");
                return;
            }

            var selectedType = (NewAccountTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedType == "Wspolne")
            {
                _context.SharedAccounts.Add(new SharedAccount
                {
                    Name = accountName,
                    Balance = 0m,
                    User1Id = firstUser.Id,
                    User2Id = firstUser.Id
                });
            }
            else
            {
                _context.PersonalAccounts.Add(new PersonalAccount
                {
                    Name = accountName,
                    Balance = 0m,
                    UserId = firstUser.Id
                });
            }

            await _context.SaveChangesAsync();
            await LoadDataFromDatabaseAsync();
            NewAccountNameTextBox.Clear();
        }

        private async void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TransferAmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Podaj poprawna kwote transferu.");
                return;
            }

            var source = TransferFromComboBox.SelectedItem as AccountListItem;
            var target = TransferToComboBox.SelectedItem as AccountListItem;
            if (source == null || target == null)
            {
                MessageBox.Show("Wybierz konto źródłowe i docelowe.");
                return;
            }
            if (source.Kind == target.Kind && source.Id == target.Id)
            {
                MessageBox.Show("Wybierz dwa rozne konta.");
                return;
            }
            if (source.Balance < amount)
            {
                var missingAmount = amount - source.Balance;
                MessageBox.Show(
                    $"Niewystarczajace srodki na koncie źródłowym.\n" +
                    $"Dostepne saldo: {source.Balance:F2} zl\n" +
                    $"Brakuje: {missingAmount:F2} zl");
                return;
            }

            var transferType = await _context.TransactionTypes.FirstOrDefaultAsync(t => t.Name == "Transfer");
            if (transferType == null)
            {
                MessageBox.Show("Nie mozna wykonac transferu.");
                return;
            }

            var outgoingTransaction = new Transaction
            {
                Amount = amount,
                IsPositive = false,
                Date = DateTime.Now,
                TransactionTypeId = transferType.Id
            };
            var incomingTransaction = new Transaction
            {
                Amount = amount,
                IsPositive = true,
                Date = DateTime.Now,
                TransactionTypeId = transferType.Id
            };

            if (source.Kind == AccountKind.Personal)
            {
                var sourceAccount = await _context.PersonalAccounts.FirstOrDefaultAsync(a => a.Id == source.Id);
                if (sourceAccount == null)
                {
                    MessageBox.Show("Nie mozna wykonac transferu.");
                    return;
                }
                sourceAccount.Balance -= amount;
                outgoingTransaction.PersonalAccountId = sourceAccount.Id;
                _context.PersonalAccounts.Update(sourceAccount);
            }
            else
            {
                var sourceAccount = await _context.SharedAccounts.FirstOrDefaultAsync(a => a.Id == source.Id);
                if (sourceAccount == null)
                {
                    MessageBox.Show("Nie mozna wykonac transferu.");
                    return;
                }
                sourceAccount.Balance -= amount;
                outgoingTransaction.SharedAccountId = sourceAccount.Id;
                _context.SharedAccounts.Update(sourceAccount);
            }

            if (target.Kind == AccountKind.Personal)
            {
                var targetAccount = await _context.PersonalAccounts.FirstOrDefaultAsync(a => a.Id == target.Id);
                if (targetAccount == null)
                {
                    MessageBox.Show("Nie mozna wykonac transferu.");
                    return;
                }
                targetAccount.Balance += amount;
                incomingTransaction.PersonalAccountId = targetAccount.Id;
                _context.PersonalAccounts.Update(targetAccount);
            }
            else
            {
                var targetAccount = await _context.SharedAccounts.FirstOrDefaultAsync(a => a.Id == target.Id);
                if (targetAccount == null)
                {
                    MessageBox.Show("Nie mozna wykonac transferu.");
                    return;
                }
                targetAccount.Balance += amount;
                incomingTransaction.SharedAccountId = targetAccount.Id;
                _context.SharedAccounts.Update(targetAccount);
            }

            var groupId = Guid.NewGuid();
            var description = string.IsNullOrWhiteSpace(TransferDescriptionTextBox.Text)
                ? "Transfer miedzy kontami"
                : TransferDescriptionTextBox.Text.Trim();

            outgoingTransaction.Description = $"{description} (wyjście)";
            outgoingTransaction.TransferGroupId = groupId;
            incomingTransaction.Description = $"{description} (wejćcie)";
            incomingTransaction.TransferGroupId = groupId;

            _context.Transactions.Add(outgoingTransaction);
            _context.Transactions.Add(incomingTransaction);
            await _context.SaveChangesAsync();

            await LoadDataFromDatabaseAsync();
            TransferAmountTextBox.Clear();
            TransferDescriptionTextBox.Clear();
            MessageBox.Show("Transfer wykonany.");
        }

        private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Otwieramy okno z Twojego folderu Views
            var addCategoryWindow = new wpf_projekt.Views.AddCategoryWindow();
            addCategoryWindow.Owner = this; // Środkuje okienko na głównym oknie

            // 2. Czekamy na zamknięcie okienka
            if (addCategoryWindow.ShowDialog() == true)
            {
                // 3. Jeśli użytkownik kliknął "Zapisz", odświeżamy kolekcję używając TWOJEJ metody!
                // To automatycznie zaktualizuje CategoryComboBox dzięki ObservableCollection
                await LoadDataFromDatabaseAsync();
            }
        }

    }
}