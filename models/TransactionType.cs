using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf_projekt.models
{
    public class TransactionType
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } // np. "Zakupy", "Wypłata"

        public virtual ICollection<wpf_projekt.Models.Transaction> Transactions { get; set; }
    }
}
