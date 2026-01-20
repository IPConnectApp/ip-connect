// Update profile tab unread badge
async function updateProfileUnreadBadge() {
    try {
        const response = await fetch('/api/chat/unread-count');
        
        if (!response.ok) {
            return;
        }
        
        const data = await response.json();
        const badge = document.getElementById('profileUnreadBadge');
        
        if (badge) {
            if (data.count > 0) {
                badge.textContent = data.count > 99 ? '99+' : data.count;
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        }
        
    } catch (error) {
        console.error('Error updating profile unread badge:', error);
    }
}

// Update badge on page load
document.addEventListener('DOMContentLoaded', function() {
    updateProfileUnreadBadge();
});

// Update badge when window gets focus
window.addEventListener('focus', function() {
    updateProfileUnreadBadge();
});

// Export function so chat.js can call it
window.updateProfileUnreadBadge = updateProfileUnreadBadge;