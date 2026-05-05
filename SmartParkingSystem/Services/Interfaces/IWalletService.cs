using SmartParking.DTOs.Wallet;
using SmartParking.Models;

namespace SmartParking.Services.Interfaces
{
    public interface IWalletService
    {
        Task<Wallet> EnsureWalletAsync(string userId);
        Task<WalletResponseDto> GetMyWalletAsync(string userId);
        Task<List<WalletTransactionDto>> GetTransactionsAsync(string userId, int take = 20);
        Task<WalletTransaction> CreditAsync(string userId, decimal amount, string type, string description, string? referenceType = null, string? referenceId = null);
        Task<WalletTransaction> DebitAsync(string userId, decimal amount, string type, string description, string? referenceType = null, string? referenceId = null);
        Task<bool> HasSufficientBalanceAsync(string userId, decimal amount);
        WalletResponseDto MapWallet(Wallet wallet);
        WalletTransactionDto MapTransaction(WalletTransaction transaction);
    }
}
