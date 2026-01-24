// Site-wide JavaScript

// Set active navigation item based on current URL
document.addEventListener('DOMContentLoaded', function() {
    const currentPath = window.location.pathname.toLowerCase();
    const moduleLinks = document.querySelectorAll('.module-nav a');
    
    moduleLinks.forEach(link => {
        const linkPath = link.getAttribute('href').toLowerCase();
        if (currentPath === linkPath || currentPath.startsWith(linkPath + '/')) {
            link.classList.add('active');
        }
    });
});
