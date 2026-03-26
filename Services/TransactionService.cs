using System.Collections.ObjectModel;
using wpf_projekt.Models;

namespace wpf_projekt.Services
{
    public static class TransactionService
    {
        public static ObservableCollection<Transaction> Transactions { get; set; }
            = new ObservableCollection<Transaction>();
    }
}