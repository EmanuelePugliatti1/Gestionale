// NovaTechManagement/NovaTechManagement/wwwroot/js/invoices-page.js

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
        console.error("No auth token found, invoices data will not be loaded.");
        return;
    }

    // API_BASE_URL is defined in auth.js
    const invoiceTableBody = document.getElementById('invoiceTableBody');
    const searchInput = document.getElementById('invoiceSearchInput');
    const addInvoiceButton = document.getElementById('addInvoiceButton');
    const addInvoiceModal = document.getElementById('addInvoiceModal');
    const closeInvoiceModalButton = document.getElementById('closeInvoiceModalButton');
    const addInvoiceForm = document.getElementById('addInvoiceForm');
    const addInvoiceErrorMessage = document.getElementById('addInvoiceErrorMessage');
    const saveInvoiceButton = document.getElementById('saveInvoiceButton');

    function displayInvoices(invoices) {
        if (!invoiceTableBody) {
            console.error("Invoice table body not found.");
            return;
        }
        invoiceTableBody.innerHTML = ''; 

        if (!invoices || invoices.length === 0) {
            invoiceTableBody.innerHTML = '<tr><td colspan="7" class="text-center text-[#90adcb] py-4">No invoices found.</td></tr>';
            return;
        }

        invoices.forEach(invoice => {
            const row = invoiceTableBody.insertRow();
            row.className = "border-t border-t-[#314d68]";

            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[15%] text-white text-sm font-normal leading-normal">#${invoice.id}</td>`;
            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[20%] text-[#90adcb] text-sm font-normal leading-normal">${invoice.client ? invoice.client.clientName : 'N/A'}</td>`;
            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[15%] text-[#90adcb] text-sm font-normal leading-normal">#${invoice.orderId}</td>`;
            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[15%] text-[#90adcb] text-sm font-normal leading-normal">${new Date(invoice.invoiceDate).toLocaleDateString()}</td>`;
            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[15%] text-[#90adcb] text-sm font-normal leading-normal">$${parseFloat(invoice.totalAmount).toFixed(2)}</td>`;
            
            const statusCell = row.insertCell();
            statusCell.className = "h-[72px] px-4 py-2 w-[10%] text-sm font-normal leading-normal";
            const statusButton = document.createElement('button');
            statusButton.className = `flex min-w-[84px] max-w-[480px] cursor-default items-center justify-center overflow-hidden rounded-xl h-8 px-4 text-sm font-medium leading-normal w-full`;
            if (invoice.status === 'Paid') statusButton.classList.add('bg-green-700', 'text-green-100');
            else if (invoice.status === 'Pending') statusButton.classList.add('bg-yellow-600', 'text-yellow-100');
            else if (invoice.status === 'Overdue') statusButton.classList.add('bg-red-700', 'text-red-100');
            else statusButton.classList.add('bg-gray-600', 'text-gray-100');
            statusButton.innerHTML = `<span class="truncate">${invoice.status}</span>`;
            statusCell.appendChild(statusButton);

            const actionsCell = row.insertCell();
            actionsCell.className = "h-[72px] px-4 py-2 w-[10%] text-[#90adcb] text-sm font-bold leading-normal tracking-[0.015em]";
            actionsCell.innerHTML = `<button class="hover:underline view-invoice-button" data-invoice-id="${invoice.id}">View</button>`;
        });
    }

    async function fetchInvoices(searchTerm = '') {
        let url = '/api/invoices';
        const params = new URLSearchParams();
        if(searchTerm){
            if(!isNaN(searchTerm) && searchTerm.length > 0) {
                // Assuming a numeric search term could be Order ID or Client ID. 
                // For simplicity, let's assume it's Order ID if numeric.
                // A more robust solution might involve a dropdown to select search field.
                // params.append('orderId', searchTerm); 
                 // For now, let's stick to status search for simplicity with single input
            } else if (searchTerm.length > 0) {
                 params.append('status', searchTerm);
            }
        }
        
        url += (params.toString() ? '?' + params.toString() : '');

        try {
            if (invoiceTableBody) invoiceTableBody.innerHTML = '<tr><td colspan="7" class="text-center text-[#90adcb] py-4">Loading invoices...</td></tr>';
            const response = await fetchWithAuth(url); // Uses global fetchWithAuth
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                throw new Error(errorData.message);
            }
            const invoices = await response.json();
            displayInvoices(invoices);
        } catch (error) {
            console.error('Failed to load invoices:', error.message);
            if (invoiceTableBody) invoiceTableBody.innerHTML = `<tr><td colspan="7" class="text-center text-red-500 py-4">Error loading invoices: ${error.message}</td></tr>`;
        }
    }

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            // For invoices, the API supports clientId, orderId, or status.
            // This simple search input will just filter by status for non-numeric, or try orderId for numeric.
             const term = this.value.trim();
             fetchInvoices(term);
        });
    }

    if (addInvoiceButton && addInvoiceModal && closeInvoiceModalButton && addInvoiceForm && saveInvoiceButton) {
        addInvoiceButton.addEventListener('click', () => {
            addInvoiceModal.style.display = 'block';
            addInvoiceErrorMessage.textContent = '';
            addInvoiceForm.reset();
            document.getElementById('addInvoiceStatus').value = 'Pending';
        });

        closeInvoiceModalButton.addEventListener('click', () => {
            addInvoiceModal.style.display = 'none';
        });

        window.addEventListener('click', (event) => {
            if (event.target === addInvoiceModal) {
                addInvoiceModal.style.display = 'none';
            }
        });

        addInvoiceForm.addEventListener('submit', async function (event) {
            event.preventDefault();
            addInvoiceErrorMessage.textContent = '';

            const orderId = parseInt(document.getElementById('addInvoiceOrderId').value, 10);
            const status = document.getElementById('addInvoiceStatus').value;
            const dueDateInput = document.getElementById('addInvoiceDueDate').value;
            const dueDate = dueDateInput ? dueDateInput : null; 

            if (isNaN(orderId)) {
                addInvoiceErrorMessage.textContent = 'Order ID must be a valid number.';
                return;
            }
             if (!status) {
                addInvoiceErrorMessage.textContent = 'Status is required.';
                return;
            }

            const invoiceData = { orderId, status, dueDate };

            try {
                saveInvoiceButton.disabled = true;
                saveInvoiceButton.textContent = 'Saving...';
                const response = await fetchWithAuth('/api/invoices', { // Uses global fetchWithAuth
                    method: 'POST',
                    body: JSON.stringify(invoiceData),
                });
                if (!response.ok) {
                    const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                    throw new Error(errorData.message);
                }
                addInvoiceModal.style.display = 'none';
                fetchInvoices(); 
            } catch (error) {
                addInvoiceErrorMessage.textContent = error.message || 'Failed to add invoice. Please try again.';
            } finally {
                saveInvoiceButton.disabled = false;
                saveInvoiceButton.textContent = 'Save Invoice';
            }
        });
    } else {
        console.error("One or more modal elements for 'Add Invoice' not found.");
    }

    fetchInvoices(); 
});
