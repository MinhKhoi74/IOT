using System.ComponentModel.DataAnnotations;

namespace SmartParking.DTOs.Wallet
{
    public class WalletResponseDto
    {
        public Guid WalletId { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "VND";
        public DateTime UpdatedAt { get; set; }
    }

    public class WalletTransactionDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class DemoTopUpRequestDto
    {
        [Range(1000, 10000000)]
        public decimal Amount { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }
    }

    public class MomoTopUpRequestDto
    {
        [Range(1000, 10000000)]
        public decimal Amount { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        /// <summary>
        /// Payment method: "Wallet" (default) or "ATM"
        /// </summary>
        [StringLength(50)]
        public string? PaymentMethod { get; set; }
    }

    public class WalletTopUpResponseDto
    {
        public WalletResponseDto Wallet { get; set; }
        public WalletTransactionDto Transaction { get; set; }
        public MomoSandboxInfoDto MomoSandboxInfo { get; set; }
    }

    public class MomoSandboxInfoDto
    {
        public string Provider { get; set; } = "MoMo";
        public string Environment { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string PartnerCode { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}
