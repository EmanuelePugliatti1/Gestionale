// NovaTechManagement/NovaTechManagement/wwwroot/js/orders-page.js

document.addEventListener('DOMContentLoaded', function () {
    if (typeof getToken !== 'function' || typeof fetchWithAuth !== 'function') {
        console.error("auth.js not loaded or core functions are not defined.");
        const body = document.querySelector('body');
        if (body) {
            body.innerHTML = '<div style="color: red; text-align: center; padding: 20px;">Critical error: Authentication module not loaded. Please contact support.</div>';
        }
        return;
    }
    if (!getToken()) {
        console.error("No auth token found, orders data will not be loaded.");
        return;
    }

    // API_BASE_URL is defined in auth.js
    const orderTableBody = document.getElementById('orderTableBody');
    const searchInput = document.getElementById('orderSearchInput');
    const addOrderButton = document.getElementById('addOrderButton');
    const addOrderModal = document.getElementById('addOrderModal');
    const closeOrderModalButton = document.getElementById('closeOrderModalButton');
    const addOrderForm = document.getElementById('addOrderForm');
    const addOrderErrorMessage = document.getElementById('addOrderErrorMessage');
    const saveOrderButton = document.getElementById('saveOrderButton');
    const statusFiltersContainer = document.getElementById('orderStatusFilters');

    let currentStatusFilter = ''; 

    function displayOrders(orders) {
        if (!orderTableBody) {
            console.error("Order table body not found.");
            return;
        }
        orderTableBody.innerHTML = ''; 

        if (!orders || orders.length === 0) {
            orderTableBody.innerHTML = '<tr><td colspan="6" class="text-center text-[#90adcb] py-4">No orders found.</td></tr>';
            return;
        }

        orders.forEach(order => {
            const row = orderTableBody.insertRow();
            row.className = "border-t border-t-[#314d68]";

            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[15%] text-white text-sm font-normal leading-normal">#${order.id}</td>`;
            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[25%] text-[#90adcb] text-sm font-normal leading-normal">${order.client ? order.client.clientName : 'N/A'} (ID: ${order.clientId})</td>`;
            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[15%] text-[#90adcb] text-sm font-normal leading-normal">${new Date(order.orderDate).toLocaleDateString()}</td>`;
            
            const statusCell = row.insertCell();
            statusCell.className = "h-[72px] px-4 py-2 w-[15%] text-sm font-normal leading-normal";
            const statusButton = document.createElement('button');
            statusButton.className = `flex min-w-[84px] max-w-[480px] cursor-default items-center justify-center overflow-hidden rounded-xl h-8 px-4 text-sm font-medium leading-normal w-full`;
            if (order.status === 'Completed' || order.status === 'Shipped') statusButton.classList.add('bg-green-700', 'text-green-100');
            else if (order.status === 'Open' || order.status === 'Processing') statusButton.classList.add('bg-yellow-600', 'text-yellow-100');
            else statusButton.classList.add('bg-gray-600', 'text-gray-100');
            statusButton.innerHTML = `<span class="truncate">${order.status}</span>`;
            statusCell.appendChild(statusButton);

            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[15%] text-[#90adcb] text-sm font-normal leading-normal">$${parseFloat(order.totalAmount).toFixed(2)}</td>`;
            
            const actionsCell = row.insertCell();
            actionsCell.className = "h-[72px] px-4 py-2 w-[15%] text-[#90adcb] text-sm font-bold leading-normal tracking-[0.015em]";
            actionsCell.innerHTML = `<button class="hover:underline view-order-button" data-order-id="${order.id}">View Details</button>`;
        });
    }

    async function fetchOrders(clientId = '', status = '') {
        let url = '/api/orders?';
        const params = new URLSearchParams();
        if (clientId) params.append('clientId', clientId);
        if (status) params.append('status', status);
        
        url += params.toString();

        try {
            if (orderTableBody) orderTableBody.innerHTML = '<tr><td colspan="6" class="text-center text-[#90adcb] py-4">Loading orders...</td></tr>';
            const response = await fetchWithAuth(url); // Uses global fetchWithAuth
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                throw new Error(errorData.message);
            }
            const orders = await response.json();
            displayOrders(orders);
        } catch (error) {
            console.error('Failed to load orders:', error.message);
            if (orderTableBody) orderTableBody.innerHTML = `<tr><td colspan="6" class="text-center text-red-500 py-4">Error loading orders: ${error.message}</td></tr>`;
        }
    }
    
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const searchTerm = this.value.trim();
            if (!isNaN(searchTerm) && searchTerm !== '') { 
                fetchOrders(searchTerm, currentStatusFilter);
            } else { 
                 fetchOrders('', searchTerm || currentStatusFilter);
            }
        });
    }

    if (statusFiltersContainer) {
        statusFiltersContainer.addEventListener('click', function(event) {
            const target = event.target.closest('.order-status-filter');
            if (target) {
                event.preventDefault();
                currentStatusFilter = target.dataset.status || '';
                
                statusFiltersContainer.querySelectorAll('.order-status-filter').forEach(btn => {
                    btn.classList.remove('border-b-[#3d98f4]', 'text-white');
                    btn.classList.add('border-b-transparent', 'text-[#90adcb]');
                    btn.querySelector('p').classList.remove('text-white');
                    btn.querySelector('p').classList.add('text-[#90adcb]');
                });
                target.classList.remove('border-b-transparent', 'text-[#90adcb]');
                target.classList.add('border-b-[#3d98f4]', 'text-white');
                target.querySelector('p').classList.add('text-white');
                target.querySelector('p').classList.remove('text-[#90adcb]');

                const searchTerm = searchInput ? searchInput.value.trim() : '';
                 if (!isNaN(searchTerm) && searchTerm !== '') { 
                    fetchOrders(searchTerm, currentStatusFilter);
                } else { 
                    fetchOrders('', currentStatusFilter); 
                }
            }
        });
    }

    if (addOrderButton && addOrderModal && closeOrderModalButton && addOrderForm && saveOrderButton) {
        addOrderButton.addEventListener('click', () => {
            addOrderModal.style.display = 'block';
            addOrderErrorMessage.textContent = '';
            addOrderForm.reset();
            document.getElementById('addOrderStatus').value = 'Open'; 
            document.getElementById('addOrderItemQuantity').value = 1; 
        });

        closeOrderModalButton.addEventListener('click', () => {
            addOrderModal.style.display = 'none';
        });

        window.addEventListener('click', (event) => {
            if (event.target === addOrderModal) {
                addOrderModal.style.display = 'none';
            }
        });

        addOrderForm.addEventListener('submit', async function (event) {
            event.preventDefault();
            addOrderErrorMessage.textContent = '';

            const clientId = parseInt(document.getElementById('addOrderClientId').value, 10);
            const status = document.getElementById('addOrderStatus').value;
            const productId = parseInt(document.getElementById('addOrderItemProductId').value, 10);
            const quantity = parseInt(document.getElementById('addOrderItemQuantity').value, 10);

            if (isNaN(clientId) || isNaN(productId) || isNaN(quantity)) {
                addOrderErrorMessage.textContent = 'Client ID, Product ID, and Quantity must be valid numbers.';
                return;
            }
            if (quantity <= 0) {
                addOrderErrorMessage.textContent = 'Quantity must be at least 1.';
                return;
            }
            if (!status) {
                addOrderErrorMessage.textContent = 'Status is required.';
                return;
            }

            const orderData = {
                clientId,
                status,
                orderItems: [{ productId, quantity }]
            };

            try {
                saveOrderButton.disabled = true;
                saveOrderButton.textContent = 'Saving...';
                const response = await fetchWithAuth('/api/orders', { // Uses global fetchWithAuth
                    method: 'POST',
                    body: JSON.stringify(orderData),
                });
                 if (!response.ok) {
                    const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                    throw new Error(errorData.message);
                }
                addOrderModal.style.display = 'none';
                fetchOrders('', currentStatusFilter); 
            } catch (error) {
                addOrderErrorMessage.textContent = error.message || 'Failed to add order. Please try again.';
            } finally {
                saveOrderButton.disabled = false;
                saveOrderButton.textContent = 'Save Order';
            }
        });
    } else {
        console.error("One or more modal elements for 'Add Order' not found.");
    }

    fetchOrders(); 
});
