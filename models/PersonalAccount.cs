using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf_projekt.models
{
    public class PersonalAccount : ObservableModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal _balance;
        public decimal Balance
        {
            get => _balance;
            set
            {
                if (_balance != value)
                {
                    _balance = value;
                    OnPropertyChanged();
                }
            }
        }
        public int UserId { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<wpf_projekt.Models.Transaction> Transactions { get; set; } = new List<wpf_projekt.Models.Transaction>();
    }
}
