using System.Windows;
using wpf_projekt.models;
using wpf_projekt.Models; // Przestrzeń nazw Twoich modeli i AppDbContext

namespace wpf_projekt.Views
{
    public partial class AddCategoryWindow : Window
    {
        public AddCategoryWindow()
        {
            InitializeComponent();
            CategoryNameTextBox.Focus();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string categoryName = CategoryNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                MessageBox.Show("Nazwa kategorii nie może być pusta!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Zapisujemy nową kategorię do bazy za pomocą Twojego AppDbContext
                using (var db = new AppDbContext())
                {
                    var newCategory = new TransactionType
                    {
                        Name = categoryName
                        // UWAGA: Jeśli Twoja klasa TransactionType ma jeszcze jakieś wymagane pola 
                        // (np. typ string Icon, albo bool IsIncome), musisz je tutaj też uzupełnić!
                    };

                    db.TransactionTypes.Add(newCategory);
                    db.SaveChanges();
                }

                // Sukces
                this.DialogResult = true;
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas zapisu: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}