using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.DTOs.ElectronicTicket;
using SmartParking.Models.Identity;
using SmartParking.Services.Interfaces;

namespace SmartParking.Controllers
{
    [ApiController]
    [Route("api/electronic-tickets")]
    public class ElectronicTicketsController : ControllerBase
    {
        private readonly IElectronicTicketService _ticketService;
        private readonly IElectronicTicketNotificationService _notificationService;
        private readonly ILogger<ElectronicTicketsController> _logger;

        public ElectronicTicketsController(
            IElectronicTicketService ticketService,
            IElectronicTicketNotificationService notificationService,
            ILogger<ElectronicTicketsController> logger)
        {
            _ticketService = ticketService;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách vé của user hiện tại
        /// </summary>
        [Authorize]
        [HttpGet("my-tickets")]
        public async Task<IActionResult> GetMyTickets()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found");
                }

                var tickets = await _ticketService.GetMyTicketsAsync(userId);
                return Ok(new
                {
                    success = true,
                    data = tickets,
                    count = tickets.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting my tickets: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy chi tiết vé theo ID
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicketDetail(Guid id)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found");
                }

                var ticket = await _ticketService.GetTicketByIdAsync(userId, id);
                return Ok(new
                {
                    success = true,
                    data = ticket
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Ticket not found" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting ticket detail: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Tìm vé theo mã code
        /// </summary>
        [Authorize]
        [HttpGet("by-code/{code}")]
        public async Task<IActionResult> GetTicketByCode(string code)
        {
            try
            {
                var ticket = await _ticketService.GetTicketByCodeAsync(code);
                return Ok(new
                {
                    success = true,
                    data = ticket
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Ticket not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting ticket by code: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Xác nhận thanh toán tiền mặt (Staff only)
        /// </summary>
        [Authorize(Roles = "Staff,Admin")]
        [HttpPost("confirm-payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] PaymentConfirmationDto confirmationDto)
        {
            try
            {
                var staffId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                if (string.IsNullOrEmpty(staffId))
                {
                    return Unauthorized("User ID not found");
                }

                var result = await _ticketService.UpdatePaymentStatusAsync(confirmationDto.TicketId, confirmationDto);

                if (result)
                {
                    var ticket = await _ticketService.GetTicketByIdAsync(staffId, confirmationDto.TicketId);
                    
                    // Gửi thông báo cho user nếu có
                    if (!string.IsNullOrEmpty(ticket.UserId))
                    {
                        await _notificationService.SendPaymentConfirmationAsync(
                            ticket.UserId,
                            confirmationDto.TicketId,
                            $"Thanh toán {confirmationDto.PaymentAmount} đồng bằng {confirmationDto.PaymentMethod} thành công");
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Payment confirmed successfully",
                        data = ticket
                    });
                }

                return BadRequest(new { success = false, message = "Failed to confirm payment" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Ticket not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error confirming payment: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả vé (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllTickets([FromQuery] string? status = null, [FromQuery] string? parkingLot = null)
        {
            try
            {
                var tickets = await _ticketService.GetAllTicketsAsync(status, parkingLot);
                return Ok(new
                {
                    success = true,
                    data = tickets,
                    count = tickets.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all tickets: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Danh sách gửi xe (delivery list) - tất cả vé đã hoàn thành
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("delivery-list")]
        public async Task<IActionResult> GetDeliveryList([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var tickets = await _ticketService.GetAllTicketsAsync("6", null); // Status = Completed (6)

                // Filter by date range if provided
                if (fromDate.HasValue || toDate.HasValue)
                {
                    tickets = tickets.Where(t =>
                    {
                        if (fromDate.HasValue && t.CheckInDateTime < fromDate) return false;
                        if (toDate.HasValue && t.CheckOutDateTime.HasValue && t.CheckOutDateTime > toDate) return false;
                        return true;
                    }).ToList();
                }

                return Ok(new
                {
                    success = true,
                    data = tickets,
                    count = tickets.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting delivery list: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gửi lại vé cho user
        /// </summary>
        [Authorize]
        [HttpPost("resend/{id}")]
        public async Task<IActionResult> ResendTicket(Guid id)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found");
                }

                var ticket = await _ticketService.GetTicketByIdAsync(userId, id);
                await _notificationService.SendTicketNotificationAsync(userId, ticket);

                return Ok(new
                {
                    success = true,
                    message = "Ticket resent successfully"
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Ticket not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error resending ticket: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Thống kê vé (Admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/statistics")]
        public async Task<IActionResult> GetTicketStatistics([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var allTickets = await _ticketService.GetAllTicketsAsync();

                // Filter by date if provided
                if (fromDate.HasValue || toDate.HasValue)
                {
                    allTickets = allTickets.Where(t =>
                    {
                        if (fromDate.HasValue && t.CheckInDateTime < fromDate) return false;
                        if (toDate.HasValue && t.CheckOutDateTime.HasValue && t.CheckOutDateTime > toDate) return false;
                        return true;
                    }).ToList();
                }

                var statistics = new
                {
                    totalTickets = allTickets.Count,
                    completedTickets = allTickets.Count(t => t.Status.ToString() == "Completed"),
                    paidCashTickets = allTickets.Count(t => t.Status.ToString() == "PaidCash"),
                    paidWalletTickets = allTickets.Count(t => t.Status.ToString() == "PaidWallet"),
                    pendingTickets = allTickets.Count(t => t.Status.ToString() == "Pending"),
                    totalRevenue = allTickets.Where(t => t.FeeAmount.HasValue).Sum(t => t.FeeAmount) ?? 0,
                    averageFee = allTickets.Where(t => t.FeeAmount.HasValue).Average(t => t.FeeAmount) ?? 0
                };

                return Ok(new
                {
                    success = true,
                    data = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting statistics: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Xác nhận thanh toán tiền mặt - Hoàn thành checkout (Staff only)
        /// </summary>
        [Authorize(Roles = "Staff,Admin")]
        [HttpPost("confirm-cash-payment")]
        public async Task<IActionResult> ConfirmCashPayment([FromBody] ConfirmCashPaymentRequestDto request)
        {
            try
            {
                var staffId = User.FindFirst("sub")?.Value ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                
                if (string.IsNullOrEmpty(staffId))
                {
                    return Unauthorized("User ID not found");
                }

                // Cập nhật vé: thanh toán tiền mặt
                var cashPaymentSuccess = await _ticketService.ConfirmCashPaymentAsync(
                    request.TicketId,
                    request.CheckInOutId,
                    request.FeeAmount);

                if (cashPaymentSuccess)
                {
                    var ticket = await _ticketService.GetTicketByIdAsync(staffId, request.TicketId);
                    
                    // Gửi thông báo cho user
                    if (!string.IsNullOrEmpty(ticket.UserId))
                    {
                        await _notificationService.SendPaymentConfirmationAsync(
                            ticket.UserId,
                            request.TicketId,
                            $"Thanh toán {request.FeeAmount:N0}đ tiền mặt được xác nhận");
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Thanh toán tiền mặt được xác nhận thành công",
                        data = new
                        {
                            ticketId = request.TicketId,
                            feeAmount = request.FeeAmount,
                            status = "PaidCash",
                            paymentMethod = "Cash"
                        }
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Không thể xác nhận thanh toán"
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Ticket not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error confirming cash payment: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
