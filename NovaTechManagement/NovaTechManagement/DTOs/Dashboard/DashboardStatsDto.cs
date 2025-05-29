namespace NovaTechManagement.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public int OrdersProcessed { get; set; } // Consider which statuses count as "processed"
        public int NewClientsThisMonth { get; set; } // Or define a specific period
    }
}
