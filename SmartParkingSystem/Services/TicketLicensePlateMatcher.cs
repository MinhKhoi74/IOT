using Microsoft.EntityFrameworkCore;
using SmartParking.Data;
using SmartParking.Models.Enums;
using SmartParking.Services.Interfaces;

namespace SmartParking.Services
{
    /// <summary>
    /// Background service để tự động match license plate với user đã đăng ký xe
    /// Chạy mỗi 10 giây và kiểm tra các vé chưa được gửi cho user
    /// </summary>
    public class TicketLicensePlateMatcher : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TicketLicensePlateMatcher> _logger;
        private readonly int _intervalSeconds = 10;
        private readonly int _maxRetryAttempts = 3;

        public TicketLicensePlateMatcher(
            IServiceProvider serviceProvider,
            ILogger<TicketLicensePlateMatcher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TicketLicensePlateMatcher service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MatchUnsentTicketsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in TicketLicensePlateMatcher: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
            }

            _logger.LogInformation("TicketLicensePlateMatcher service stopped");
        }

        private async Task MatchUnsentTicketsAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                var ticketService = scope.ServiceProvider.GetRequiredService<IElectronicTicketService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<IElectronicTicketNotificationService>();

                // Lấy danh sách vé chưa được gửi
                var unsentTickets = await context.ElectronicTickets
                    .Where(t => t.Status == ElectronicTicketStatus.Created && t.AttemptCount < _maxRetryAttempts)
                    .ToListAsync();

                if (!unsentTickets.Any())
                {
                    return;
                }

                _logger.LogInformation($"Processing {unsentTickets.Count} unsent tickets");

                foreach (var ticket in unsentTickets)
                {
                    try
                    {
                        // Tìm xe đã đăng ký với biển số này
                        var vehicle = await context.Vehicle
                            .FirstOrDefaultAsync(v => v.LicensePlate == ticket.LicensePlate);

                        if (vehicle != null)
                        {
                            // Match thành công - cập nhật UserId
                            await ticketService.UpdateTicketUserAsync(ticket.Id, vehicle.UserId);

                            // Gửi vé cho user qua SignalR
                            var ticketDto = await ticketService.GetTicketByIdAsync(vehicle.UserId, ticket.Id);
                            await notificationService.SendTicketNotificationAsync(vehicle.UserId, ticketDto);

                            // Đánh dấu vé đã gửi
                            await ticketService.MarkTicketAsSentAsync(ticket.Id);

                            _logger.LogInformation(
                                $"Ticket {ticket.TicketCode} matched to user {vehicle.UserId} for license plate {ticket.LicensePlate}");
                        }
                        else
                        {
                            // Không tìm thấy, tăng attempt count
                            ticket.AttemptCount++;
                            context.ElectronicTickets.Update(ticket);
                            await context.SaveChangesAsync();

                            _logger.LogWarning(
                                $"No matching vehicle found for license plate {ticket.LicensePlate}. Attempt: {ticket.AttemptCount}");

                            // Nếu vượt quá max retries, đánh dấu là Pending
                            if (ticket.AttemptCount >= _maxRetryAttempts)
                            {
                                ticket.Status = ElectronicTicketStatus.Pending;
                                context.ElectronicTickets.Update(ticket);
                                await context.SaveChangesAsync();

                                _logger.LogInformation(
                                    $"Ticket {ticket.TicketCode} marked as Pending after {ticket.AttemptCount} attempts");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing ticket {ticket.TicketCode}: {ex.Message}");
                    }
                }
            }
        }
    }
}
