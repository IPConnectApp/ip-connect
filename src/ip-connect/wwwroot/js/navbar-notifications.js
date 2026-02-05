// Navbar Notifications JavaScript
let notificationDropdownOpen = false;
let notificationConnection = null;

console.log('Navbar notifications script loaded!');

document.addEventListener('DOMContentLoaded', async function () {
    await initializeNotificationHub();
    await loadNotificationCount();
    await loadNotifications();

    // Toggle dropdown on bell click
    document.getElementById('notificationBell').addEventListener('click', function (e) {
        e.preventDefault();
        toggleNotificationDropdown();
    });

    // Mark all as read button
    document.getElementById('markAllReadBtn').addEventListener('click', async function (e) {
        e.stopPropagation();
        await markAllAsRead();
    });

    // Close dropdown when clicking outside
    document.addEventListener('click', function (e) {
        const container = document.querySelector('.navbar-notification-container');
        if (!container.contains(e.target) && notificationDropdownOpen) {
            toggleNotificationDropdown();
        }
    });
});

async function initializeNotificationHub() {
    try {
        // Create SignalR connection
        notificationConnection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect()
            .build();

        // Listen for new notifications
        notificationConnection.on("ReceiveNotification", function (notification) {
            console.log("New notification received:", notification);

            // Add notification to dropdown
            addNotificationToDropdown(notification);

            // Play sound or show toast (optional)
            // showNotificationToast(notification.message);
        });

        // Listen for notification count updates
        notificationConnection.on("UpdateNotificationCount", function (count) {
            console.log("Notification count updated:", count);
            updateBadgeCount(count);
        });

        // Start connection
        await notificationConnection.start();
        console.log("NotificationHub connected!");

    } catch (error) {
        console.error("Error connecting to NotificationHub:", error);
    }
}

function toggleNotificationDropdown() {
    const dropdown = document.getElementById('notificationDropdown');
    notificationDropdownOpen = !notificationDropdownOpen;

    if (notificationDropdownOpen) {
        dropdown.style.display = 'flex';
        loadNotifications(); // Refresh notifications when opening
    } else {
        dropdown.style.display = 'none';
    }
}

async function loadNotificationCount() {
    try {
        const response = await fetch('/api/notification/unread-count');
        if (!response.ok) return;

        const { count } = await response.json();

        const badge = document.getElementById('notificationBadge');
        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : count;
            badge.style.display = 'inline';
        } else {
            badge.style.display = 'none';
        }
    } catch (error) {
        console.error('Error loading notification count:', error);
    }
}

function updateBadgeCount(count) {
    const badge = document.getElementById('notificationBadge');

    if (!badge) {
        console.error("Badge element not found!");
        return;
    }

    if (count > 0) {
        badge.textContent = count > 99 ? '99+' : count;
        badge.style.display = 'inline';
    } else {
        badge.style.display = 'none';
    }
}

async function loadNotifications() {
    const listElement = document.getElementById('notificationList');

    try {
        const response = await fetch('/api/notification/unread');
        if (!response.ok) throw new Error('Failed to load notifications');

        const notifications = await response.json();

        if (notifications.length === 0) {
            listElement.innerHTML = `
                <div class="notification-empty">
                    <i class="fas fa-bell-slash"></i>
                    <p>No new notifications</p>
                </div>
            `;
            return;
        }

        listElement.innerHTML = notifications.map(n => createNotificationHTML(n)).join('');
    } catch (error) {
        console.error('Error loading notifications:', error);
        listElement.innerHTML = `
            <div class="notification-empty">
                <i class="fas fa-exclamation-triangle"></i>
                <p>Failed to load notifications</p>
            </div>
        `;
    }
}

function addNotificationToDropdown(notification) {
    const listElement = document.getElementById('notificationList');

    // Remove "no notifications" message if it exists
    const emptyMessage = listElement.querySelector('.notification-empty');
    if (emptyMessage) {
        listElement.innerHTML = '';
    }

    // Add new notification at the top
    const notificationHTML = createNotificationHTML(notification);
    listElement.insertAdjacentHTML('afterbegin', notificationHTML);
}

function createNotificationHTML(notification) {
    const timeAgo = formatTimeAgo(notification.createdAt);
    const avatar = notification.relatedUserProfilePicture || '/images/default-avatar.jpg';
    const unreadDot = notification.isRead ? '' : '<span class="notification-unread-dot"></span>';

    let actionsHtml = '';

    // Friend Request notification - show Accept/Reject buttons
    if (notification.type === 1 && !notification.isRead) {
        actionsHtml = `
        <div class="notification-actions" onclick="event.stopPropagation()">
            <button class="btn-accept" onclick="acceptFriendRequest(${notification.relatedEntityId})">
                <i class="fas fa-check"></i> Accept
            </button>
            <button class="btn-reject" onclick="rejectFriendRequest(${notification.relatedEntityId})">
                <i class="fas fa-times"></i> Reject
            </button>
        </div>
    `;
    }

    return `
        <div class="notification-item ${notification.isRead ? '' : 'unread'}" 
             onclick="markNotificationAsRead(${notification.id})"
             data-notification-id="${notification.id}">
            <img src="${avatar}" alt="User" class="notification-avatar" />
            <div class="notification-content">
                <div class="notification-message">${escapeHtml(notification.message)}</div>
                <div class="notification-time">${timeAgo}</div>
                ${actionsHtml}
            </div>
            ${unreadDot}
        </div>
    `;
}

async function acceptFriendRequest(friendshipId) {
    try {
        const response = await fetch(`/api/friendship/accept/${friendshipId}`, {
            method: 'POST'
        });

        if (!response.ok) throw new Error('Failed to accept');

        // Reload notifications (this will show only unread, so accepted one disappears)
        await loadNotifications();
        await loadNotificationCount();

        showToast('Friend request accepted!', 'success');
    } catch (error) {
        console.error('Error accepting friend request:', error);
        showToast('Failed to accept friend request', 'error');
    }
}

async function rejectFriendRequest(friendshipId) {
    try {
        const response = await fetch(`/api/friendship/reject/${friendshipId}`, {
            method: 'POST'
        });

        if (!response.ok) throw new Error('Failed to reject');

        // Reload notifications
        await loadNotifications();
        await loadNotificationCount();

        showToast('Friend request rejected', 'info');
    } catch (error) {
        console.error('Error rejecting friend request:', error);
        showToast('Failed to reject friend request', 'error');
    }
}

async function markNotificationAsRead(notificationId) {
    try {
        await fetch(`/api/notification/mark-read/${notificationId}`, {
            method: 'POST'
        });

        await loadNotifications();
        await loadNotificationCount();
    } catch (error) {
        console.error('Error marking notification as read:', error);
    }
}

async function markAllAsRead() {
    try {
        const response = await fetch('/api/notification/mark-all-read', {
            method: 'POST'
        });

        if (!response.ok) throw new Error('Failed to mark all as read');

        await loadNotifications();
        await loadNotificationCount();

        showToast('All notifications marked as read', 'success');
    } catch (error) {
        console.error('Error marking all as read:', error);
        showToast('Failed to mark all as read', 'error');
    }
}

function formatTimeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now - date) / 1000);

    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;

    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

function showToast(message, type = 'info') {
    // Simple alert for now - you can enhance this later with a proper toast library
    alert(message);
}