using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        public ObservableCollection<TransactionType> Categories { get; set; } = new ObservableCollection<TransactionType>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // Pozwala na Binding w XAML

            InitializeDatabase();
        }

        private async void InitializeDatabase()
        {
            // Tworzy bazę danych, jeśli nie istnieje
            await _context.Database.EnsureCreatedAsync();

            // Uruchamia Seed danych
            await SeedInitialDataAsync();

            // Ładuje dane do UI
            await LoadDataFromDatabaseAsync();

            TransactionDatePicker.SelectedDate = DateTime.Now;
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
                new TransactionType { Name = "Rozrywka" }
            };
            _context.TransactionTypes.AddRange(types);

            // 2. Dodaj użytkownika i konto
            var testUser = new User { FirstName = "Jan", LastName = "Kowalski", Earnings = 5000 };
            _context.Users.Add(testUser);
            await _context.SaveChangesAsync(); // Zapisujemy, by dostać ID użytkownika

            var personalAcc = new PersonalAccount { Balance = 2500, UserId = testUser.Id };
            _context.PersonalAccounts.Add(personalAcc);

            await _context.SaveChangesAsync();
        }

        private async Task LoadDataFromDatabaseAsync()
        {
            // Czyścimy kolekcje lokalne
            Categories.Clear();
            PersonalAccounts.Clear();
            Transactions.Clear();

            // Pobieramy dane z bazy (Include ładuje relacje)
            var dbCategories = await _context.TransactionTypes.ToListAsync();
            var dbAccounts = await _context.PersonalAccounts.ToListAsync();
            var dbTransactions = await _context.Transactions
                .Include(t => t.TransactionType)
                .Include(t => t.PersonalAccount)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            // Przypisujemy do ObservableCollection
            dbCategories.ForEach(Categories.Add);
            dbAccounts.ForEach(PersonalAccounts.Add);
            dbTransactions.ForEach(Transactions.Add);

            // Ustawienie ComboBoxów
            CategoryComboBox.ItemsSource = Categories;
            AccountComboBox.ItemsSource = PersonalAccounts;

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
            var selectedAccount = AccountComboBox.SelectedItem as PersonalAccount;

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
                TransactionTypeId = selectedType.Id,
                PersonalAccountId = selectedAccount.Id
            };

            try
            {
                // 3. Logika biznesowa: Aktualizacja salda w bazie
                if (isIncome)
                    selectedAccount.Balance += amount;
                else
                    selectedAccount.Balance -= amount;

                // 4. Zapis do bazy
                _context.Transactions.Add(newTransaction);
                _context.PersonalAccounts.Update(selectedAccount);
                await _context.SaveChangesAsync();

                // 5. Aktualizacja UI (dodajemy na początek listy)
                Transactions.Insert(0, newTransaction);

                MessageBox.Show($"Zapisano! Aktualne saldo: {selectedAccount.Balance}");
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
            Transactions = new List<Transaction>(Transactions);
        }
    }
}