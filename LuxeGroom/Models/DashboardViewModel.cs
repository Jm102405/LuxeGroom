namespace LuxeGroom.ViewModels
{
    public class DashboardViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public int AppointmentsToday { get; set; }
        public int TotalClients { get; set; }
        public decimal EstimatedRevenueThisMonth { get; set; }

        public List<RecentAppointmentItem> RecentAppointments { get; set; } = new();
    }

    public class RecentAppointmentItem
    {
        public string OwnerName { get; set; } = string.Empty;
        public string PetName { get; set; } = string.Empty;
        public string GroomingStyle { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
