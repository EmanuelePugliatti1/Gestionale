// NovaTechManagement/NovaTechManagement/wwwroot/js/products-page.js

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
        console.error("No auth token found, products data will not be loaded.");
        return;
    }

    // API_BASE_URL is defined in auth.js
    const productTableBody = document.getElementById('productTableBody');
    const searchInput = document.getElementById('productSearchInput');
    const addProductButton = document.getElementById('addProductButton');
    const addProductModal = document.getElementById('addProductModal');
    const closeProductModalButton = document.getElementById('closeProductModalButton');
    const addProductForm = document.getElementById('addProductForm');
    const addProductErrorMessage = document.getElementById('addProductErrorMessage');
    const saveProductButton = document.getElementById('saveProductButton');

    function displayProducts(products) {
        if (!productTableBody) {
            console.error("Product table body not found.");
            return;
        }
        productTableBody.innerHTML = '';

        if (!products || products.length === 0) {
            productTableBody.innerHTML = '<tr><td colspan="6" class="text-center text-[#90adcb] py-4">No products found.</td></tr>';
            return;
        }

        products.forEach(product => {
            const row = productTableBody.insertRow();
            row.className = "border-t border-t-[#314d68]";

            const imgCell = row.insertCell();
            imgCell.className = "h-[72px] px-4 py-2 w-[10%] text-sm font-normal leading-normal";
            if (product.imageUrl) {
                imgCell.innerHTML = `<div class="bg-center bg-no-repeat aspect-square bg-cover rounded-md w-10 h-10" style='background-image: url("${product.imageUrl}");'></div>`;
            } else {
                imgCell.innerHTML = `<div class="bg-[#223649] rounded-md w-10 h-10 flex items-center justify-center text-white text-xs">No Image</div>`;
            }

            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[35%] text-white text-sm font-normal leading-normal">${product.productName}</td>`;
            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[15%] text-[#90adcb] text-sm font-normal leading-normal">$${parseFloat(product.price).toFixed(2)}</td>`;
            row.insertCell().outerHTML = `<td class="h-[72px] px-4 py-2 w-[15%] text-[#90adcb] text-sm font-normal leading-normal">${product.quantityInStock}</td>`;

            const statusCell = row.insertCell();
            statusCell.className = "h-[72px] px-4 py-2 w-[15%] text-sm font-normal leading-normal";
            const statusButton = document.createElement('button');
            statusButton.className = `flex min-w-[84px] max-w-[480px] cursor-default items-center justify-center overflow-hidden rounded-xl h-8 px-4 text-sm font-medium leading-normal w-full ${product.status === 'Active' ? 'bg-green-700 text-green-100' : 'bg-red-700 text-red-100'}`;
            statusButton.innerHTML = `<span class="truncate">${product.status}</span>`;
            statusCell.appendChild(statusButton);

            const actionsCell = row.insertCell();
            actionsCell.className = "h-[72px] px-4 py-2 w-[10%] text-[#90adcb] text-sm font-bold leading-normal tracking-[0.015em]";
            actionsCell.innerHTML = `<button class="hover:underline view-product-button" data-product-id="${product.id}">View</button>`;
        });
    }

    async function fetchProducts(searchTerm = '') {
        let url = '/api/products';
        if (searchTerm) {
            url += `?search=${encodeURIComponent(searchTerm)}`;
        }
        try {
            if (productTableBody) productTableBody.innerHTML = '<tr><td colspan="6" class="text-center text-[#90adcb] py-4">Loading products...</td></tr>';
            const response = await fetchWithAuth(url); // Uses global fetchWithAuth
             if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: `HTTP error ${response.status}` }));
                throw new Error(errorData.message);
            }
            const products = await response.json();
            displayProducts(products);
        } catch (error) {
            console.error('Failed to load products:', error.message);
            if (productTableBody) productTableBody.innerHTML = `<tr><td colspan="6" class="text-center text-red-500 py-4">Error loading products: ${error.message}</td></tr>`;
        }
    }

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            fetchProducts(this.value.trim());
        });
    }

    if (addProductButton && addProductModal && closeProductModalButton && addProductForm && saveProductButton) {
        addProductButton.addEventListener('click', () => {
            addProductModal.style.display = 'block';
            addProductErrorMessage.textContent = '';
            addProductForm.reset();
        });

        closeProductModalButton.addEventListener('click', () => {
            addProductModal.style.display = 'none';
        });

        window.addEventListener('click', (event) => {
            if (event.target === addProductModal) {
                addProductModal.style.display = 'none';
            }
        });

        addProductForm.addEventListener('submit', async function (event) {
            event.preventDefault();
            addProductErrorMessage.textContent = '';

            const productName = document.getElementById('addProductName').value.trim();
            const description = document.getElementById('addProductDescription').value.trim();
            const price = parseFloat(document.getElementById('addProductPrice').value);
            const quantityInStock = parseInt(document.getElementById('addProductQuantity').value, 10);
            const status = document.getElementById('addProductStatus').value;
            const imageUrl = document.getElementById('addProductImageUrl').value.trim();

            if (!productName || !price || !status || isNaN(price) || isNaN(quantityInStock)) {
                addProductErrorMessage.textContent = 'Product Name, Price, Quantity, and Status are required and must be valid numbers for price/quantity.';
                return;
            }
            if (price <= 0) {
                 addProductErrorMessage.textContent = 'Price must be greater than 0.';
                 return;
            }
             if (quantityInStock < 0) {
                 addProductErrorMessage.textContent = 'Quantity must be a non-negative number.';
                 return;
            }

            const productData = { productName, description, price, quantityInStock, status, imageUrl: imageUrl || null };

            try {
                saveProductButton.disabled = true;
                saveProductButton.textContent = 'Saving...';
                const response = await fetchWithAuth('/api/products', { // Uses global fetchWithAuth
                    method: 'POST',
                    body: JSON.stringify(productData),
                });
                if (!response.ok) {
                    // Try to parse error from backend if available
                    const errorData = await response.json().catch(() => ({ message: `Failed to add product. Server responded with ${response.status}` }));
                    throw new Error(errorData.message);
                }
                // If response.ok, no need to parse JSON for a POST if API returns 201/204
                addProductModal.style.display = 'none';
                fetchProducts();
            } catch (error) {
                addProductErrorMessage.textContent = error.message || 'Failed to add product. Please try again.';
            } finally {
                saveProductButton.disabled = false;
                saveProductButton.textContent = 'Save Product';
            }
        });
    } else {
        console.error("One or more modal elements for 'Add Product' not found.");
    }

    fetchProducts();
});
