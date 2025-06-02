using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaTechManagement.Data;
using NovaTechManagement.DTOs.Dashboard;
using NovaTechManagement.DTOs.Order; // For OrderDto used in RecentActivity
using NovaTechManagement.DTOs.Client; // For ClientDto nested in OrderDto
using NovaTechManagement.Models; // If direct model access is needed, though usually through DTOs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NovaTechManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,User")] // Apply to all actions in this controller
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/dashboard/stats
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // TotalRevenue: Sum of TotalAmount from Invoice where status is "Paid"
            var totalRevenue = await _context.Invoices
                .Where(i => i.Status == "Paid")
                .SumAsync(i => i.TotalAmount);

            // OrdersProcessed: Count of Order where status is "Completed" or "Shipped"
            var ordersProcessed = await _context.Orders
                .CountAsync(o => o.Status == "Completed" || o.Status == "Shipped");

            // NewClientsThisMonth: Count of Client where DateAdded is within the current month
            var newClientsThisMonth = await _context.Clients
                .CountAsync(c => c.DateAdded >= firstDayOfMonth && c.DateAdded <= lastDayOfMonth);

            var stats = new DashboardStatsDto
            {
                TotalRevenue = totalRevenue,
                OrdersProcessed = ordersProcessed,
                NewClientsThisMonth = newClientsThisMonth
            };

            return Ok(stats);
        }

        // GET: api/dashboard/revenue-trend
        [HttpGet("revenue-trend")]
        public async Task<ActionResult<RevenueTrendDto>> GetRevenueTrend()
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            
            var monthlyRevenue = await _context.Invoices
                .Where(i => i.Status == "Paid" && i.InvoiceDate >= sixMonthsAgo)
                .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Select(g => new RevenueDataPoint
                {
                    Period = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"), // e.g., "Jan 2023"
                    Revenue = g.Sum(i => i.TotalAmount)
                })
                .OrderBy(dp => DateTime.ParseExact(dp.Period, "MMM yyyy", CultureInfo.InvariantCulture))
                .ToListAsync();
            
            // Ensure all months in the last 6 months are present, even if revenue is zero
            var trendData = new List<RevenueDataPoint>();
            for (int i = 5; i >= 0; i--) // Iterate from 5 months ago to current month
            {
                var targetMonthDate = DateTime.UtcNow.AddMonths(-i);
                var periodName = targetMonthDate.ToString("MMM yyyy");
                var existingDataPoint = monthlyRevenue.FirstOrDefault(dp => dp.Period == periodName);
                if (existingDataPoint != null)
                {
                    trendData.Add(existingDataPoint);
                }
                else
                {
                    trendData.Add(new RevenueDataPoint { Period = periodName, Revenue = 0 });
                }
            }


            var revenueTrend = new RevenueTrendDto
            {
                Data = trendData,
                TrendDescription = "Revenue from Paid Invoices - Last 6 Months"
            };

            return Ok(revenueTrend);
        }

        // GET: api/dashboard/order-distribution
        [HttpGet("order-distribution")]
        public async Task<ActionResult<OrderDistributionDto>> GetOrderDistribution()
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            var orderDistribution = await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfMonth) // Orders this month
                .GroupBy(o => o.Status)
                .Select(g => new OrderDistributionDataPoint
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var distribution = new OrderDistributionDto
            {
                Data = orderDistribution,
                DistributionDescription = "Order Status Distribution - This Month"
            };

            return Ok(distribution);
        }

        // GET: api/dashboard/recent-activity
        [HttpGet("recent-activity")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetRecentActivity([FromQuery] int count = 5)
        {
            if (count <= 0) count = 5; // Default to 5 if invalid count provided
            if (count > 20) count = 20; // Max limit

            var recentOrders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    ClientId = o.ClientId,
                    Client = o.Client == null ? null : new ClientDto
                    {
                        Id = o.Client.Id,
                        ClientName = o.Client.ClientName,
                        Email = o.Client.Email,
                        Phone = o.Client.Phone,
                        Status = o.Client.Status,
                        DateAdded = o.Client.DateAdded
                    },
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    OrderItems = o.OrderItems.Select(oi => new ReturnOrderItemDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product != null ? oi.Product.ProductName : "N/A",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList()
                })
                .ToListAsync();

            return Ok(recentOrders);
        }
    }
}
