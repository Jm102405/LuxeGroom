/*
 * ReportViewModels.cs
 * View models for the LuxeGroom Reports feature.
 * Located at: Models/ReportViewModels.cs
 */

namespace LuxeGroom.Models
{
    public class RevenueMonthItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PaymentCount { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }

    public class GroomingStyleItem
    {
        public int Rank { get; set; }
        public string GroomingStyle { get; set; } = string.Empty;
        public int ReservationCount { get; set; }
    }

    public class MonthlySummaryItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int NewCustomers { get; set; }
        public int TotalReservations { get; set; }
        public int ApprovedReservations { get; set; }
        public decimal Revenue { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }
}