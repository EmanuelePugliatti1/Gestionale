// NovaTechManagement/NovaTechManagement/wwwroot/js/dashboard-data.js

document.addEventListener('DOMContentLoaded', function () {
    if (typeof getToken !== 'function' || typeof fetchWithAuth !== 'function') { // Check for global fetchWithAuth
        console.error("auth.js not loaded or core functions (getToken, fetchWithAuth) are not defined.");
        // Display a user-friendly error on the page as critical functions are missing
        const body = document.querySelector('body');
        if (body) {
            body.innerHTML = '<div style="color: red; text-align: center; padding: 20px;">Critical error: Authentication module not loaded. Please contact support.</div>';
        }
        return;
    }

    if (!getToken()) {
        console.error("No auth token found, dashboard data will not be loaded.");
        // checkAuthentication() in auth.js should handle redirection if not on login page.
        return;
    }

    // API_BASE_URL is now defined in auth.js, so no need to redefine here.

    // Fetch and display dashboard stats
    async function loadDashboardStats() {
        try {
            const response = await fetchWithAuth('/api/dashboard/stats'); // Uses global fetchWithAuth
            if (!response.ok) {
                // Error is already logged by fetchWithAuth for network/401, 
                // but here we handle other non-ok statuses if needed.
                const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                throw new Error(errorData.message);
            }
            const stats = await response.json();
            
            document.getElementById('totalRevenue').textContent = `$${stats.totalRevenue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
            document.getElementById('ordersProcessed').textContent = stats.ordersProcessed.toLocaleString();
            document.getElementById('newClients').textContent = stats.newClientsThisMonth.toLocaleString();
        } catch (error) {
            console.error('Failed to load dashboard stats:', error.message);
            document.getElementById('totalRevenue').textContent = 'Error';
            document.getElementById('ordersProcessed').textContent = 'Error';
            document.getElementById('newClients').textContent = 'Error';
        }
    }

    // Fetch and display recent activity
    async function loadRecentActivity() {
        const tableBody = document.getElementById('recentActivityTableBody');
        if (!tableBody) {
            console.error("Element with ID 'recentActivityTableBody' not found.");
            return;
        }
        try {
            const response = await fetchWithAuth('/api/dashboard/recent-activity?count=5'); // Uses global fetchWithAuth
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                throw new Error(errorData.message);
            }
            const activities = await response.json();
            
            tableBody.innerHTML = ''; 

            if (activities && activities.length > 0) {
                activities.forEach(activity => {
                    const row = tableBody.insertRow();
                    row.insertCell().textContent = `#${activity.id}`;
                    row.insertCell().textContent = activity.client ? activity.client.clientName : 'N/A';
                    row.insertCell().textContent = new Date(activity.orderDate).toLocaleDateString();
                    
                    const statusCell = row.insertCell();
                    const statusButton = document.createElement('button');
                    statusButton.className = "flex min-w-[84px] max-w-[480px] cursor-default items-center justify-center overflow-hidden rounded-xl h-8 px-4 bg-[#223649] text-white text-sm font-medium leading-normal w-full";
                    statusButton.innerHTML = `<span class="truncate">${activity.status}</span>`;
                    statusCell.appendChild(statusButton);
                    
                    row.insertCell().textContent = `$${activity.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
                });
            } else {
                tableBody.innerHTML = '<tr><td colspan="5" class="text-center text-[#90adcb] py-4">No recent activity found.</td></tr>';
            }
        } catch (error) {
            console.error('Failed to load recent activity:', error.message);
            if (tableBody) {
                 tableBody.innerHTML = `<tr><td colspan="5" class="text-center text-red-500 py-4">Error loading recent activity: ${error.message}</td></tr>`;
            }
        }
    }

    // Fetch and display revenue trend
    async function loadRevenueTrend() {
        const revenueTrendTotalEl = document.getElementById('revenueTrendTotal');
        const revenueTrendDescEl = document.getElementById('revenueTrendDescription');
        const chartContainer = document.getElementById('revenueChartContainer');
        const chartLabelsContainer = document.getElementById('revenueChartLabels');

        try {
            const response = await fetchWithAuth('/api/dashboard/revenue-trend'); // Uses global fetchWithAuth
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                throw new Error(errorData.message);
            }
            const trend = await response.json();
            
            const totalRevenueFromTrend = trend.data.reduce((sum, dp) => sum + dp.revenue, 0);
            if (revenueTrendTotalEl) revenueTrendTotalEl.textContent = `$${totalRevenueFromTrend.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
            if (revenueTrendDescEl && trend.trendDescription) {
                 revenueTrendDescEl.textContent = trend.trendDescription;
            }

            if(chartContainer && chartLabelsContainer) {
                chartLabelsContainer.innerHTML = ''; 
                chartContainer.innerHTML = ''; 

                if (trend.data && trend.data.length > 0) {
                    const maxRevenue = Math.max(...trend.data.map(dp => dp.revenue), 0);
                    
                    const chartBarsHtml = trend.data.map(dp => {
                        const barHeightPercentage = maxRevenue > 0 ? (dp.revenue / maxRevenue) * 100 : 0;
                        return `<div class="flex flex-col items-center h-full justify-end">
                                    <div class="bg-[#607afb] w-8 md:w-10" style="height: ${barHeightPercentage}%;"></div>
                                </div>`;
                    }).join('');
                    chartContainer.innerHTML = `<div class="flex justify-around items-end h-full">${chartBarsHtml}</div>`;
                    
                    const chartLabelsHtml = trend.data.map(dp => 
                        `<p class="text-[#90adcb] text-[13px] font-bold leading-normal tracking-[0.015em]">${dp.period.split(' ')[0]}</p>`
                    ).join(''); 
                    chartLabelsContainer.innerHTML = chartLabelsHtml;
                } else {
                    chartContainer.innerHTML = '<p class="text-center text-[#90adcb]">No revenue data available for the period.</p>';
                }
            }

        } catch (error) {
            console.error('Failed to load revenue trend:', error.message);
            if (revenueTrendTotalEl) revenueTrendTotalEl.textContent = 'Error';
            if (chartContainer) chartContainer.innerHTML = `<p class="text-center text-red-500">Error loading chart: ${error.message}</p>`;
        }
    }

    // Fetch and display order distribution
    async function loadOrderDistribution() {
        const orderDistTotalEl = document.getElementById('orderDistributionTotal');
        const orderDistDescEl = document.getElementById('orderDistributionDescription');
        const chartContainer = document.getElementById('orderDistributionChartContainer');
        try {
            const response = await fetchWithAuth('/api/dashboard/order-distribution'); // Uses global fetchWithAuth
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                throw new Error(errorData.message);
            }
            const distribution = await response.json();

            const totalOrdersInDistribution = distribution.data.reduce((sum, dp) => sum + dp.count, 0);
            if (orderDistTotalEl) orderDistTotalEl.textContent = totalOrdersInDistribution.toLocaleString();
            if (orderDistDescEl && distribution.distributionDescription) {
                 orderDistDescEl.textContent = distribution.distributionDescription;
            }

            if(chartContainer) {
                chartContainer.innerHTML = ''; 
                 if (distribution.data && distribution.data.length > 0) {
                    const maxCount = Math.max(...distribution.data.map(dp => dp.count), 0);
                    
                    const chartBarsHtml = distribution.data.map(dp => {
                        const barHeightPercentage = maxCount > 0 ? (dp.count / maxCount) * 100 : 0;
                        return `<div class="flex flex-col items-center h-full justify-end">
                                    <div class="bg-[#607afb] w-10 md:w-12" style="height: ${barHeightPercentage}%;"></div> <!-- Changed color and removed border -->
                                    <p class="text-[#90adcb] text-[13px] font-bold leading-normal tracking-[0.015em] mt-1">${dp.category}</p>
                                </div>`;
                    }).join('');
                    chartContainer.className = "flex min-h-[180px] items-end justify-around px-3"; 
                    chartContainer.innerHTML = chartBarsHtml;

                } else {
                    chartContainer.innerHTML = '<p class="text-center text-[#90adcb]">No order distribution data available for the period.</p>';
                }
            }

        } catch (error) {
            console.error('Failed to load order distribution:', error.message);
            if (orderDistTotalEl) orderDistTotalEl.textContent = 'Error';
            if (chartContainer) chartContainer.innerHTML = `<p class="text-center text-red-500">Error loading chart: ${error.message}</p>`;
        }
    }

    loadDashboardStats();
    loadRecentActivity();
    loadRevenueTrend();
    loadOrderDistribution();
});
