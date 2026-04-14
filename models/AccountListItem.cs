namespace wpf_projekt.models
{
    public enum AccountKind
    {
        Personal = 0,
        Shared = 1
    }

    public class AccountListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public AccountKind Kind { get; set; }
        public string KindLabel => Kind == AccountKind.Personal ? "Osobiste" : "Wspolne";
        public string DisplayName => $"{Name} ({KindLabel}) - saldo: {Balance} zl";
    }
}
