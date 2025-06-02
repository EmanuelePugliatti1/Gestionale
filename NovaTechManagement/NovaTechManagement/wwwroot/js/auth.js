// NovaTechManagement/NovaTechManagement/wwwroot/js/auth.js

const API_BASE_URL = 'https://localhost:44372/'; // Ensure trailing slash

/**
 * Checks for the authentication token in localStorage.
 * If not found or empty, redirects to index.html (login page).
 */
function checkAuthentication() {
    const token = localStorage.getItem('authToken');
    if (!token) {
        if (!window.location.pathname.endsWith('index.html') && window.location.pathname !== '/') {
            window.location.href = 'index.html'; // Assumes index.html is at the root
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
    localStorage.removeItem('userRoles'); // Remove userRoles on logout
    window.location.href = 'index.html'; // Assumes index.html is at the root
}

/**
 * Returns the array of roles for the current user from localStorage.
 * @returns {string[]} Array of role names, or an empty array if not found/invalid.
 */
function getUserRoles() {
    const rolesString = localStorage.getItem('userRoles');
    try {
        if (rolesString) {
            const roles = JSON.parse(rolesString);
            return Array.isArray(roles) ? roles : [];
        }
    } catch (e) {
        console.error("Error parsing userRoles from localStorage", e);
    }
    return [];
}

/**
 * Checks if the current user has a specific role.
 * @param {string} roleName - The name of the role to check for.
 * @returns {boolean} True if the user has the role, false otherwise.
 */
function hasRole(roleName) {
    const roles = getUserRoles();
    return roles.includes(roleName);
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
 * @param {string} relativeUrl - The API endpoint (e.g., '/api/clients' or 'api/clients').
 * @param {object} options - Standard fetch options.
 * @returns {Promise<Response>} The fetch Response object.
 * @throws {Error} If the request fails or returns an error status (after handling 401).
 */
async function fetchWithAuth(relativeUrl, options = {}) {
    const token = getToken();
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers,
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    // Construct absolute URL
    // Ensure API_BASE_URL ends with a slash and relativeUrl doesn't start with one, or handle both cases.
    const cleanBase = API_BASE_URL.endsWith('/') ? API_BASE_URL : API_BASE_URL + '/';
    const cleanRelativeUrl = relativeUrl.startsWith('/') ? relativeUrl.substring(1) : relativeUrl;
    const absoluteUrl = cleanBase + cleanRelativeUrl;

    try {
        const response = await fetch(absoluteUrl, { ...options, headers });

        if (response.status === 401) {
            logout();
            throw new Error('Unauthorized: Session expired or invalid. Logging out.');
        }

        return response;
    } catch (error) {
        console.error(`Fetch error for ${absoluteUrl}:`, error.message);
        throw error;
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
