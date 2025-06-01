// NovaTechManagement/NovaTechManagement/wwwroot/js/auth.js

const API_BASE_URL = 'https://localhost:44372/'; // Assuming API is at the same origin

/**
 * Checks for the authentication token in localStorage.
 * If not found or empty, redirects to index.html (login page).
 */
function checkAuthentication() {
    const token = localStorage.getItem('authToken');
    if (!token) {
        if (!window.location.pathname.endsWith('index.html') && window.location.pathname !== '/') {
            window.location.href = 'index.html';
        }
    }
}

/**
 * Removes authToken and user details from localStorage and redirects to index.html.
 */
function logout() {
    localStorage.removeItem('authToken');
    localStorage.removeItem('userEmail');
    localStorage.removeItem('userFirstName');
    localStorage.removeItem('userLastName');
    window.location.href = 'index.html';
}

/**
 * Returns the authentication token from localStorage.
 * @returns {string|null} The auth token, or null if not found.
 */
function getToken() {
    return localStorage.getItem('authToken');
}

/**
 * Returns stored user details from localStorage.
 * @returns {object|null} An object with user details, or null if not found.
 */
function getUserDetails() {
    const email = localStorage.getItem('userEmail');
    const firstName = localStorage.getItem('userFirstName');
    const lastName = localStorage.getItem('userLastName');

    if (email) {
        return {
            email: email,
            firstName: firstName,
            lastName: lastName,
        };
    }
    return null;
}

/**
 * Makes an authenticated API call.
 * @param {string} url - The API endpoint (e.g., '/api/clients').
 * @param {object} options - Standard fetch options.
 * @returns {Promise<Response>} The fetch Response object.
 * @throws {Error} If the request fails or returns an error status (after handling 401).
 */
async function fetchWithAuth(url, options = {}) {
    const token = getToken();
    const headers = {
        'Content-Type': 'application/json', // Default, can be overridden by options.headers
        ...options.headers,
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    try {
        const response = await fetch(API_BASE_URL + url, { ...options, headers });

        if (response.status === 401) {
            logout(); // Handles redirect to login
            // Throw an error to stop further processing in the calling function
            throw new Error('Unauthorized: Session expired or invalid. Logging out.');
        }
        
        // Do not throw for other non-ok statuses here, let the caller handle it.
        // This allows callers to parse error messages from the body if needed.
        return response; 
    } catch (error) {
        // Log network errors or errors from the fetch call itself (e.g. if server is down)
        // Errors from non-ok statuses (except 401) should be handled by the caller
        // The 401 error thrown above will also be caught here if not caught by caller.
        console.error(`Fetch error for ${url}:`, error.message); 
        throw error; // Re-throw to allow caller to handle if needed
    }
}


// Global event listener for logout button
document.addEventListener('DOMContentLoaded', function() {
    const logoutButton = document.getElementById('logoutButton');
    if (logoutButton) {
        logoutButton.addEventListener('click', function(event) {
            event.preventDefault(); 
            logout();
        });
    }
});
