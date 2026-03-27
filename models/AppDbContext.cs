using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf_projekt.models
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<PersonalAccount> PersonalAccounts { get; set; }
        public DbSet<SharedAccount> SharedAccounts { get; set; }
        public DbSet<wpf_projekt.Models.Transaction> Transactions { get; set; }
        public DbSet<TransactionType> TransactionTypes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=finance_manager.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SharedAccount>()
                .HasOne(s => s.User1)
                .WithMany()
                .HasForeignKey(s => s.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SharedAccount>()
                .HasOne(s => s.User2)
                .WithMany()
                .HasForeignKey(s => s.User2Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
