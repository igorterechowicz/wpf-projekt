using System;
using System.Collections.Generic;
using System.Windows;
using wpf_projekt.Models;

namespace wpf_projekt
{
    public partial class MainWindow : Window
    {
        public static List<Transaction> Transactions { get; set; } = new List<Transaction>();

        // Dwie oddzielne listy dla lepszej logiki!
        private readonly List<string> ExpenseCategories = new List<string> { "Jedzenie", "Transport", "Rachunki", "Rozrywka", "Zdrowie", "Inne" };
        private readonly List<string> IncomeCategories = new List<string> { "Wynagrodzenie", "Premia", "Zwrot", "Prezent", "Inne" };

        public MainWindow()
        {
            InitializeComponent();

            TransactionDatePicker.SelectedDate = DateTime.Now;
            UpdateCategories(); // Wczytuje kategorie przy starcie (domyślnie wydatki)
        }

        // Metoda wywoływana automatycznie, gdy klikniesz "Przychód" lub "Wydatek"
        private void TransactionType_Changed(object sender, RoutedEventArgs e)
        {
            // Warunek zapobiega błędowi, zanim okno do końca się załaduje
            if (CategoryComboBox != null)
            {
                UpdateCategories();
            }
        }

        // Logika podmieniania kategorii
        private void UpdateCategories()
        {
            CategoryComboBox.Items.Clear(); // Czyści starą listę

            if (ExpenseRadio.IsChecked == true)
            {
                foreach (var cat in ExpenseCategories) CategoryComboBox.Items.Add(cat);
            }
            else if (IncomeRadio.IsChecked == true)
            {
                foreach (var cat in IncomeCategories) CategoryComboBox.Items.Add(cat);
            }

            // Automatycznie zaznacza pierwszą pozycję z nowej listy
            if (CategoryComboBox.Items.Count > 0)
                CategoryComboBox.SelectedIndex = 0;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Walidacja: czy wpisano kwotę i czy jest większa od 0
            if (!decimal.TryParse(AmountTextBox.Text, out decimal parsedAmount) || parsedAmount <= 0)
            {
                MessageBox.Show("Wprowadź poprawną kwotę większą od zera (np. 150,50).", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool isIncome = IncomeRadio.IsChecked == true;

            Transaction newTransaction = new Transaction
            {
                Id = Transactions.Count + 1,
                Amount = parsedAmount,
                Date = TransactionDatePicker.SelectedDate ?? DateTime.Now,
                Description = DescriptionTextBox.Text,
                Category = CategoryComboBox.SelectedItem?.ToString() ?? "Inne",
                IsPositive = isIncome
            };

            Transactions.Add(newTransaction);

            MessageBox.Show($"Dodano pomyślnie!\n\nTyp: {newTransaction.TypeName}\nKategoria: {newTransaction.Category}\nKwota: {newTransaction.Amount} zł\nOpis: {newTransaction.Description}",
                            "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

            // Wyczyszczenie formularza na następny raz
            AmountTextBox.Clear();
            DescriptionTextBox.Clear();
            TransactionDatePicker.SelectedDate = DateTime.Now;
            Transactions = new List<Transaction>(Transactions);
        }
    }
}