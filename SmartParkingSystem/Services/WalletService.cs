using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.Wallet;
using SmartParking.Models;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDBContext _context;

        public WalletService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<Wallet> EnsureWalletAsync(string userId)
        {
            var wallet = await _context.Wallets
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (wallet != null)
            {
                return wallet;
            }

            wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = 0m,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return wallet;
        }

        public async Task<WalletResponseDto> GetMyWalletAsync(string userId)
        {
            var wallet = await EnsureWalletAsync(userId);
            return MapWallet(wallet);
        }

        public async Task<List<WalletTransactionDto>> GetTransactionsAsync(string userId, int take = 20)
        {
            var wallet = await EnsureWalletAsync(userId);

            return await _context.WalletTransactions
                .Where(x => x.WalletId == wallet.Id)
                .OrderByDescending(x => x.CreatedAt)
                .Take(Math.Clamp(take, 1, 100))
                .Select(x => new WalletTransactionDto
                {
                    Id = x.Id,
                    Type = x.Type,
                    Amount = x.Amount,
                    BalanceBefore = x.BalanceBefore,
                    BalanceAfter = x.BalanceAfter,
                    ReferenceType = x.ReferenceType,
                    ReferenceId = x.ReferenceId,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<WalletTransaction> CreditAsync(string userId, decimal amount, string type, string description, string? referenceType = null, string? referenceId = null)
        {
            if (amount <= 0)
            {
                throw new Exception("Amount must be greater than 0");
            }

            var wallet = await EnsureWalletAsync(userId);
            var balanceBefore = wallet.Balance;

            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.Now;

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = type,
                Amount = amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Description = description,
                CreatedAt = DateTime.Now
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<WalletTransaction> DebitAsync(string userId, decimal amount, string type, string description, string? referenceType = null, string? referenceId = null)
        {
            if (amount <= 0)
            {
                throw new Exception("Amount must be greater than 0");
            }

            var wallet = await EnsureWalletAsync(userId);

            if (wallet.Balance < amount)
            {
                throw new Exception("Insufficient wallet balance");
            }

            var balanceBefore = wallet.Balance;
            wallet.Balance -= amount;
            wallet.UpdatedAt = DateTime.Now;

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = type,
                Amount = -amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Description = description,
                CreatedAt = DateTime.Now
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> HasSufficientBalanceAsync(string userId, decimal amount)
        {
            var wallet = await EnsureWalletAsync(userId);
            return wallet.Balance >= amount;
        }

        public WalletResponseDto MapWallet(Wallet wallet)
        {
            return new WalletResponseDto
            {
                WalletId = wallet.Id,
                Balance = wallet.Balance,
                UpdatedAt = wallet.UpdatedAt
            };
        }

        public WalletTransactionDto MapTransaction(WalletTransaction transaction)
        {
            return new WalletTransactionDto
            {
                Id = transaction.Id,
                Type = transaction.Type,
                Amount = transaction.Amount,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter,
                ReferenceType = transaction.ReferenceType,
                ReferenceId = transaction.ReferenceId,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt
            };
        }
    }
}
