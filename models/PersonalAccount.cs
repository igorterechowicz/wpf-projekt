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
        public decimal Balance { get; set; }
        public int UserId { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<wpf_projekt.Models.Transaction> Transactions { get; set; } = new List<wpf_projekt.Models.Transaction>();
    }
}
