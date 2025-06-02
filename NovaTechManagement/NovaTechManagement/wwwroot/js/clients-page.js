// NovaTechManagement/NovaTechManagement/wwwroot/js/clients-page.js

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
        console.error("No auth token found, clients data will not be loaded.");
        return;
    }

    // API_BASE_URL is defined in auth.js
    const clientTableBody = document.getElementById('clientTableBody');
    const searchInput = document.getElementById('searchInput');
    const addClientButton = document.getElementById('addClientButton');
    const addClientModal = document.getElementById('addClientModal');
    const closeClientModalButton = document.getElementById('closeClientModalButton');
    const addClientForm = document.getElementById('addClientForm');
    const addClientErrorMessage = document.getElementById('addClientErrorMessage');
    const saveClientButton = document.getElementById('saveClientButton');

    // Function to display clients in the table
    function displayClients(clients) {
        if (!clientTableBody) {
            console.error("Client table body not found.");
            return;
        }
        clientTableBody.innerHTML = '';

        if (!clients || clients.length === 0) {
            clientTableBody.innerHTML = '<tr><td colspan="5" class="text-center text-[#90adcb] py-4">No clients found.</td></tr>';
            return;
        }

        clients.forEach(client => {
            const row = clientTableBody.insertRow();
            row.className = "border-t border-t-[#314d68]";

            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[25%] text-white text-sm font-normal leading-normal">${client.clientName}</td>`;
            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[30%] text-[#90adcb] text-sm font-normal leading-normal">${client.email}</td>`;
            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[20%] text-[#90adcb] text-sm font-normal leading-normal">${client.phone || 'N/A'}</td>`;

            const statusCell = row.insertCell();
            statusCell.className = "h-[72px] px-4 py-2 w-[10%] text-sm font-normal leading-normal";
            const statusButton = document.createElement('button');
            statusButton.className = "flex min-w-[84px] max-w-[480px] cursor-default items-center justify-center overflow-hidden rounded-xl h-8 px-4 bg-[#223649] text-white text-sm font-medium leading-normal w-full";
            statusButton.innerHTML = `<span class="truncate">${client.status}</span>`;
            statusCell.appendChild(statusButton);

            const actionsCell = row.insertCell();
            actionsCell.className = "h-[72px] px-4 py-2 w-[15%] text-[#90adcb] text-sm font-bold leading-normal tracking-[0.015em]";
            actionsCell.innerHTML = `<button class="hover:underline view-client-button" data-client-id="${client.id}">View</button>`;
        });
    }

    // Function to fetch clients from the API
    async function fetchClients(searchTerm = '') {
        let url = '/api/clients';
        if (searchTerm) {
            url += `?search=${encodeURIComponent(searchTerm)}`;
        }
        try {
            if (clientTableBody) clientTableBody.innerHTML = '<tr><td colspan="5" class="text-center text-[#90adcb] py-4">Loading clients...</td></tr>';
            const response = await fetchWithAuth(url); // Uses global fetchWithAuth
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                throw new Error(errorData.message);
            }
            const clients = await response.json();
            displayClients(clients);
        } catch (error) {
            console.error('Failed to load clients:', error.message);
            if (clientTableBody) clientTableBody.innerHTML = `<tr><td colspan="5" class="text-center text-red-500 py-4">Error loading clients: ${error.message}</td></tr>`;
        }
    }

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            fetchClients(this.value.trim());
        });
    }

    if (addClientButton && addClientModal && closeClientModalButton && addClientForm && saveClientButton) {
        addClientButton.addEventListener('click', () => {
            addClientModal.style.display = 'block';
            addClientErrorMessage.textContent = '';
            addClientForm.reset();
        });

        closeClientModalButton.addEventListener('click', () => {
            addClientModal.style.display = 'none';
        });

        window.addEventListener('click', (event) => {
            if (event.target === addClientModal) {
                addClientModal.style.display = 'none';
            }
        });

        addClientForm.addEventListener('submit', async function (event) {
            event.preventDefault();
            addClientErrorMessage.textContent = '';

            const clientName = document.getElementById('addClientName').value.trim();
            const email = document.getElementById('addClientEmail').value.trim();
            const phone = document.getElementById('addClientPhone').value.trim();
            const status = document.getElementById('addClientStatus').value;

            if (!clientName || !email || !status) {
                addClientErrorMessage.textContent = 'Client Name, Email, and Status are required.';
                return;
            }

            const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailPattern.test(email)) {
                addClientErrorMessage.textContent = 'Please enter a valid email address.';
                return;
            }

            const clientData = { clientName, email, phone: phone || null, status };

            try {
                saveClientButton.disabled = true;
                saveClientButton.textContent = 'Saving...';
                const response = await fetchWithAuth('/api/clients', { // Uses global fetchWithAuth
                    method: 'POST',
                    body: JSON.stringify(clientData),
                });

                if (!response.ok) {
                    const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                    throw new Error(errorData.message);
                }
                // No need to parse response.json() if API returns 201 with location or 204

                addClientModal.style.display = 'none';
                fetchClients();
            } catch (error) {
                addClientErrorMessage.textContent = error.message || 'Failed to add client. Please try again.';
            } finally {
                saveClientButton.disabled = false;
                saveClientButton.textContent = 'Save Client';
            }
        });
    } else {
        console.error("One or more modal elements for 'Add Client' not found.");
    }

    fetchClients();

    // Conditionally show/hide "Add Client" button based on role
    if (addClientButton) {
        if (!hasRole('Admin')) {
            addClientButton.style.display = 'none';
        } else {
            addClientButton.style.display = 'flex'; // Assuming it's a flex item, or use '' for default
        }
    }
});
