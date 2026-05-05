using System;

namespace SmartParking.Models
{
    public class CheckInOut
    {
        public int Id { get; set; }
        public Guid? VehicleId { get; set; }
        public string? UserId { get; set; }

        // Plate info
        public string LicensePlate { get; set; }
        public string PlateImagePath { get; set; }
        public float Confidence { get; set; }

        // Checkin info
        public DateTime CheckInTime { get; set; }
        public string CheckInStationId { get; set; } // "STATION_01"
        public string CheckInImageBase64 { get; set; }

        // Checkout info
        public DateTime? CheckOutTime { get; set; }
        public string CheckOutStationId { get; set; } // "STATION_02"
        public string CheckOutImageBase64 { get; set; }

        // Fee calculation
        public int? DurationMinutes { get; set; }
        public decimal FeeAmount { get; set; }
        public DateTime? FeeCalculatedAt { get; set; }
        public string FeeStatus { get; set; } = "Pending"; // 'Pending', 'Calculated', 'Paid'
        public string PaymentStatus { get; set; } = "Pending"; // 'Pending', 'Paid', 'Failed'
        public string? PaymentMethod { get; set; } // 'Wallet', 'Momo'
        public Guid? WalletTransactionId { get; set; }
        public DateTime? PaidAt { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Active"; // 'Active', 'Completed', 'Cancelled'

        public Vehicle? Vehicle { get; set; }
        public Identity.ApplicationUser? User { get; set; }
        public WalletTransaction? WalletTransaction { get; set; }
    }
}
