using System.Collections.Generic;

namespace NovaTechManagement.DTOs.Dashboard
{
    public class OrderDistributionDataPoint
    {
        public string Category { get; set; } = string.Empty; // e.g., "Online", "In-Store" - for now, use Order Status
        public int Count { get; set; }
    }

    public class OrderDistributionDto
    {
        public List<OrderDistributionDataPoint> Data { get; set; } = new List<OrderDistributionDataPoint>();
        public string DistributionDescription { get; set; } = string.Empty; // e.g., "This Month"
    }
}
