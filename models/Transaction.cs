using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using wpf_projekt.models;

namespace wpf_projekt.Models
{
    public class Transaction : ObservableModel // Zakładając, że dziedziczysz po klasie powiadomień
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")] // Określenie precyzji dla bazy danych
        public decimal Amount { get; set; }

        [Required]
        public bool IsPositive { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string Description { get; set; }

        // --- POWIĄZANIA Z DIAGRAMU ---

        // Klucz obcy do PersonalAccount (może być nullable, jeśli transakcja może należeć do konta współdzielonego)
        public int? PersonalAccountId { get; set; }

        [ForeignKey("PersonalAccountId")]
        public virtual PersonalAccount PersonalAccount { get; set; }

        // Klucz obcy do SharedAccount
        public int? SharedAccountId { get; set; }

        [ForeignKey("SharedAccountId")]
        public virtual SharedAccount SharedAccount { get; set; }

        // Powiązanie z typem transakcji (z diagramu: TransactionType type_id FK)
        [Required]
        public int TransactionTypeId { get; set; }

        [ForeignKey("TransactionTypeId")]
        public virtual TransactionType TransactionType { get; set; }

        public Guid? TransferGroupId { get; set; }

        // Właściwość pomocnicza
        [NotMapped] // Nie twórz kolumny w bazie dla tej właściwości obliczeniowej
        public string TypeName => IsPositive ? "Przychód" : "Wydatek";
    }
}