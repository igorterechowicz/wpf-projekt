using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace wpf_projekt.Views
{
    public partial class SummaryView : UserControl
    {
        public SummaryView()
        {
            InitializeComponent();
            Load();
        }

        private void Load()
        {
            var t = ((MainWindow)Application.Current.MainWindow).Transactions;

            var income = t.Where(x => x.IsPositive).Sum(x => x.Amount);
            var expense = t.Where(x => !x.IsPositive).Sum(x => x.Amount);

            BalanceText.Text = $"Bilans: {income - expense} zł";
            IncomeText.Text = $"Przychody: {income} zł";
            ExpenseText.Text = $"Wydatki: {expense} zł";
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Load();
        }
    }

}