using SmartParking.Models.Identity;

namespace SmartParking.Models
{
    public class Wallet
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public ApplicationUser User { get; set; }
        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}
