namespace SmartParking.Models
{
    public class WalletTransaction
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public string Type { get; set; } = "Unknown"; // 'TopUp', 'ParkingFee', 'Refund'
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? ReferenceType { get; set; } // 'CheckInOut', 'TopUp'
        public string? ReferenceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Wallet Wallet { get; set; }
        public ICollection<CheckInOut> ParkingSessions { get; set; } = new List<CheckInOut>();
    }
}
