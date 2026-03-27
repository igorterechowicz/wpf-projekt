using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace wpf_projekt.models
{
    public class SharedAccount : ObservableModel
    {
        public int Id { get; set; }
        public decimal Balance { get; set; }
        [Required]
        public int User1Id { get; set; }
        [Required]
        public int User2Id { get; set; }

        [Required]
        public virtual User User1 { get; set; }
        [Required]
        public virtual User User2 { get; set; }
        public virtual ICollection<wpf_projekt.Models.Transaction> Transactions { get; set; } = new List<wpf_projekt.Models.Transaction>();
    }
}
