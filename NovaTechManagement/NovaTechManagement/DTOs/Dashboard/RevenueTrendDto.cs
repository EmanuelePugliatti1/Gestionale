using System;
using System.Collections.Generic;

namespace NovaTechManagement.DTOs.Dashboard
{
    public class RevenueDataPoint
    {
        public string Period { get; set; } = string.Empty; // e.g., "Jan", "Feb" or "Week 1"
        public decimal Revenue { get; set; }
    }

    public class RevenueTrendDto
    {
        public List<RevenueDataPoint> Data { get; set; } = new List<RevenueDataPoint>();
        public string TrendDescription { get; set; } = string.Empty; // e.g., "Last 6 Months"
    }
}
