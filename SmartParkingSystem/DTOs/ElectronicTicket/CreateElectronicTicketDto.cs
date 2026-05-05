namespace SmartParking.DTOs.ElectronicTicket
{
    public class CreateElectronicTicketDto
    {
        /// <summary>
        /// Biển số xe
        /// </summary>
        public string LicensePlate { get; set; }

        /// <summary>
        /// Tên bãi đỗ xe
        /// </summary>
        public string ParkingLotName { get; set; }

        /// <summary>
        /// Tên chi nhánh
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Thời gian check-in
        /// </summary>
        public DateTime CheckInDateTime { get; set; }
    }
}
