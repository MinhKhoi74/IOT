using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.DTOs.ElectronicTicket;
using SmartParking.Models;
using SmartParking.Models.Enums;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    public class ElectronicTicketService : IElectronicTicketService
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<ElectronicTicketService> _logger;

        public ElectronicTicketService(ApplicationDBContext context, ILogger<ElectronicTicketService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ElectronicTicketDetailDto> CreateTicketAsync(CreateElectronicTicketDto dto)
        {
            try
            {
                var ticketCode = GenerateTicketCode();

                var ticket = new ElectronicTicket
                {
                    Id = Guid.NewGuid(),
                    LicensePlate = dto.LicensePlate.ToUpper(),
                    TicketCode = ticketCode,
                    CheckInDateTime = dto.CheckInDateTime,
                    ParkingLotName = dto.ParkingLotName,
                    BranchName = dto.BranchName,
                    Status = ElectronicTicketStatus.Created,
                    CreatedAt = DateTime.UtcNow,
                    AttemptCount = 0
                };

                _context.ElectronicTickets.Add(ticket);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Electronic ticket created: {ticketCode} for license plate: {dto.LicensePlate}");

                return MapToDetailDto(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating electronic ticket: {ex.Message}");
                throw;
            }
        }

        public async Task<ElectronicTicketDetailDto> GetTicketByIdAsync(string userId, Guid ticketId)
        {
            var ticket = await _context.ElectronicTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket not found: {ticketId}");
            }

            // Kiểm tra quyền: user chỉ được xem vé của mình hoặc admin
            if (ticket.UserId != userId && !IsAdmin(userId))
            {
                throw new UnauthorizedAccessException("You don't have permission to view this ticket");
            }

            return MapToDetailDto(ticket);
        }

        public async Task<List<ElectronicTicketSummaryDto>> GetMyTicketsAsync(string userId)
        {
            var tickets = await _context.ElectronicTickets
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CheckInDateTime)
                .ToListAsync();

            return tickets.Select(MapToSummaryDto).ToList();
        }

        public async Task<ElectronicTicketDetailDto> GetTicketByCodeAsync(string ticketCode)
        {
            var ticket = await _context.ElectronicTickets
                .FirstOrDefaultAsync(t => t.TicketCode == ticketCode);

            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket not found: {ticketCode}");
            }

            return MapToDetailDto(ticket);
        }

        public async Task<List<ElectronicTicketDetailDto>> GetAllTicketsAsync(string? status = null, string? parkingLotName = null)
        {
            var query = _context.ElectronicTickets.AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ElectronicTicketStatus>(status, out var ticketStatus))
            {
                query = query.Where(t => t.Status == ticketStatus);
            }

            if (!string.IsNullOrEmpty(parkingLotName))
            {
                query = query.Where(t => t.ParkingLotName.Contains(parkingLotName));
            }

            var tickets = await query
                .OrderByDescending(t => t.CheckInDateTime)
                .ToListAsync();

            return tickets.Select(MapToDetailDto).ToList();
        }

        public async Task<bool> UpdatePaymentStatusAsync(Guid ticketId, PaymentConfirmationDto confirmationDto)
        {
            var ticket = await _context.ElectronicTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket not found: {ticketId}");
            }

            ticket.FeeAmount = confirmationDto.PaymentAmount;
            ticket.PaymentMethod = confirmationDto.PaymentMethod;

            if (confirmationDto.PaymentMethod == PaymentMethod.Cash)
            {
                ticket.Status = ElectronicTicketStatus.PaidCash;
            }
            else if (confirmationDto.PaymentMethod == PaymentMethod.Wallet)
            {
                ticket.Status = ElectronicTicketStatus.PaidWallet;
            }

            _context.ElectronicTickets.Update(ticket);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Payment status updated for ticket: {ticket.TicketCode}");
            return true;
        }

        public async Task<bool> UpdateCheckOutInfoAsync(Guid ticketId, DateTime checkOutTime, decimal feeAmount)
        {
            var ticket = await _context.ElectronicTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket not found: {ticketId}");
            }

            ticket.CheckOutDateTime = checkOutTime;
            ticket.FeeAmount = feeAmount;
            ticket.Status = ElectronicTicketStatus.Completed;

            _context.ElectronicTickets.Update(ticket);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Check-out info updated for ticket: {ticket.TicketCode}");
            return true;
        }

        public async Task<bool> ConfirmCashPaymentAsync(Guid ticketId, decimal amount, string staffId)
        {
            var ticket = await _context.ElectronicTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket not found: {ticketId}");
            }

            ticket.FeeAmount = amount;
            ticket.PaymentMethod = PaymentMethod.Cash;
            ticket.Status = ElectronicTicketStatus.PaidCash;

            _context.ElectronicTickets.Update(ticket);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Cash payment confirmed by staff {staffId} for ticket: {ticket.TicketCode}");
            return true;
        }

        public async Task<ElectronicTicketDetailDto> GetTicketByLicensePlateAsync(string licensePlate)
        {
            var ticket = await _context.ElectronicTickets
                .Where(t => t.LicensePlate == licensePlate.ToUpper())
                .OrderByDescending(t => t.CheckInDateTime)
                .FirstOrDefaultAsync();

            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket not found for license plate: {licensePlate}");
            }

            return MapToDetailDto(ticket);
        }

        public async Task<List<ElectronicTicket>> GetUnsentTicketsAsync(int maxAttempts = 3)
        {
            return await _context.ElectronicTickets
                .Where(t => t.Status == ElectronicTicketStatus.Created && t.AttemptCount < maxAttempts)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateTicketUserAsync(Guid ticketId, string userId)
        {
            var ticket = await _context.ElectronicTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket not found: {ticketId}");
            }

            ticket.UserId = userId;
            ticket.AttemptCount++;

            _context.ElectronicTickets.Update(ticket);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Ticket {ticket.TicketCode} matched to user: {userId}");
            return true;
        }

        public async Task<bool> MarkTicketAsSentAsync(Guid ticketId)
        {
            var ticket = await _context.ElectronicTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket not found: {ticketId}");
            }

            ticket.Status = ElectronicTicketStatus.Sent;
            ticket.SentToUserAt = DateTime.UtcNow;

            _context.ElectronicTickets.Update(ticket);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Ticket {ticket.TicketCode} marked as sent to user");
            return true;
        }

        public async Task<bool> ConfirmCashPaymentAsync(Guid ticketId, Guid checkInOutId, decimal feeAmount)
        {
            try
            {
                var ticket = await _context.ElectronicTickets
                    .FirstOrDefaultAsync(t => t.Id == ticketId);

                if (ticket == null)
                {
                    throw new KeyNotFoundException($"Ticket not found: {ticketId}");
                }

                // Cập nhật vé: đánh dấu đã thanh toán tiền mặt
                ticket.Status = ElectronicTicketStatus.PaidCash;
                ticket.PaymentMethod = PaymentMethod.Cash;
                ticket.FeeAmount = feeAmount;

                _context.ElectronicTickets.Update(ticket);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cash payment confirmed for ticket {ticket.TicketCode}: {feeAmount}đ");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error confirming cash payment for ticket {ticketId}: {ex.Message}");
                throw;
            }
        }

        // ==================== PRIVATE HELPER METHODS ====================

        private string GenerateTicketCode()
        {
            // Format: ETICKET-20260425-123456
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(100000, 999999);
            return $"ETICKET-{timestamp}-{random}";
        }

        private ElectronicTicketDetailDto MapToDetailDto(ElectronicTicket ticket)
        {
            var duration = ticket.CheckOutDateTime.HasValue && ticket.CheckInDateTime != default
                ? (decimal?)(ticket.CheckOutDateTime.Value - ticket.CheckInDateTime).TotalHours
                : (decimal?)null;

            return new ElectronicTicketDetailDto
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                LicensePlate = ticket.LicensePlate,
                CheckInDateTime = ticket.CheckInDateTime,
                CheckOutDateTime = ticket.CheckOutDateTime,
                ParkingLotName = ticket.ParkingLotName,
                BranchName = ticket.BranchName,
                UserId = ticket.UserId,
                FeeAmount = ticket.FeeAmount,
                Status = ticket.Status,
                PaymentMethod = ticket.PaymentMethod,
                CreatedAt = ticket.CreatedAt,
                SentToUserAt = ticket.SentToUserAt,
                DurationHours = duration.HasValue ? (decimal)duration : null
            };
        }

        private ElectronicTicketSummaryDto MapToSummaryDto(ElectronicTicket ticket)
        {
            return new ElectronicTicketSummaryDto
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                LicensePlate = ticket.LicensePlate,
                CheckInDateTime = ticket.CheckInDateTime,
                ParkingLotName = ticket.ParkingLotName,
                Status = ticket.Status,
                PaymentMethod = ticket.PaymentMethod,
                FeeAmount = ticket.FeeAmount
            };
        }

        private bool IsAdmin(string userId)
        {
            // TODO: Implement admin role check from user claims or database
            // For now, return false - implement based on your auth system
            return false;
        }
    }
}
