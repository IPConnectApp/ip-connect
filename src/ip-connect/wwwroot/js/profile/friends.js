// Make sendFriendRequest available globally for restricted view
window.sendFriendRequest = async function (friendUserId) {
    try {
        const response = await fetch('/api/friendship/send', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ friendUserId: friendUserId })
        });

        if (!response.ok) {
            const error = await response.json();
            alert(error.error || 'Failed to send friend request');
            return;
        }

        alert('Friend request sent successfully!');
        location.reload();
    } catch (error) {
        console.error('Error sending friend request:', error);
        alert('Failed to send friend request');
    }
};

// Friends Page JavaScript
document.addEventListener('DOMContentLoaded', async function () {
    if (!document.getElementById('profileUserId')) return;

    await loadFriendsPage();
});

async function loadFriendsPage() {
    const profileUserId = document.getElementById('profileUserId').value;
    const isOwnProfile = document.getElementById('isOwnProfile').value === 'true';
    const friendsContent = document.getElementById('friendsContent');

    try {
        if (isOwnProfile) {
            // Viewing own profile - show my friends
            await loadMyFriends(friendsContent);
        } else {
            // Viewing friend's profile - show their friends
            // (We can only get here if we're friends, thanks to ProfileController check)
            await loadUserFriends(profileUserId, friendsContent);
        }
    } catch (error) {
        console.error('Error loading friends:', error);
        friendsContent.innerHTML = `
            <div class="empty-friends">
                <i class="fas fa-exclamation-triangle"></i>
                <h3>Error Loading Friends</h3>
                <p>Unable to load friends at this time.</p>
            </div>
        `;
    }
}

async function loadMyFriends(container) {
    const response = await fetch('/api/friendship/friends');
    if (!response.ok) throw new Error('Failed to load friends');

    const friends = await response.json();

    if (friends.length === 0) {
        container.innerHTML = `
            <div class="empty-friends">
                <i class="fas fa-user-friends"></i>
                <h3>No Friends Yet</h3>
                <p>Start connecting with people to see them here!</p>
            </div>
        `;
        return;
    }

    displayFriendsList(friends, container);
}

async function loadUserFriends(userId, container) {
    try {
        const response = await fetch(`/api/friendship/friends/${userId}`);
        if (!response.ok) throw new Error('Failed to load friends');

        const friends = await response.json();

        if (friends.length === 0) {
            container.innerHTML = `
                <div class="empty-friends">
                    <i class="fas fa-user-friends"></i>
                    <h3>No Friends Yet</h3>
                    <p>This user hasn't added any friends yet.</p>
                </div>
            `;
            return;
        }

        displayFriendsList(friends, container);
    } catch (error) {
        console.error('Error loading user friends:', error);
        container.innerHTML = `
            <div class="empty-friends">
                <i class="fas fa-exclamation-triangle"></i>
                <h3>Error Loading Friends</h3>
                <p>Unable to load friends at this time.</p>
            </div>
        `;
    }
}

function displayFriendsList(friends, container) {
    const isOwnProfile = document.getElementById('isOwnProfile')?.value === 'true';

    const friendsHtml = friends.map(friend => `
        <div class="friend-card" onclick="window.location.href='/profile/${friend.username}/albums'">
            ${isOwnProfile ? `
                <button class="btn-remove-friend" onclick="event.stopPropagation(); openRemoveFriendModal('${friend.friendshipId}', '${escapeHtml(friend.username)}')">
                    <i class="fas fa-times"></i>
                </button>
            ` : ''}
            <img src="${friend.profilePictureUrl || '/images/default-avatar.jpg'}" 
                 alt="${friend.username}" 
                 class="friend-avatar" />
            <div class="friend-name">${escapeHtml(friend.username)}</div>
            <div class="friend-since">Friends since ${formatDate(friend.friendsSince)}</div>
            <div class="friend-actions" onclick="event.stopPropagation()">
                <button class="btn-view-profile" onclick="window.location.href='/profile/${friend.username}/albums'">
                    <i class="fas fa-user"></i> Profile
                </button>
                ${isOwnProfile ? `
                    <button class="btn-message" onclick="startChat('${friend.userId}', '${escapeHtml(friend.username)}', '${friend.profilePictureUrl || '/images/default-avatar.jpg'}')">
                        <i class="fas fa-comment"></i> Message
                    </button>
                ` : ''}
            </div>
        </div>
    `).join('');

    container.innerHTML = `<div class="friends-grid">${friendsHtml}</div>`;
}

function formatDate(dateString) {
    const date = new Date(dateString);
    const options = { year: 'numeric', month: 'short' };
    return date.toLocaleDateString('en-US', options);
}

//Remove friend
let friendToRemove = null;

function openRemoveFriendModal(friendshipId, username) {
    friendToRemove = friendshipId;
    document.getElementById('removeFriendUsername').textContent = username;
    document.getElementById('removeFriendModal').style.display = 'flex';
}

function closeRemoveFriendModal() {
    friendToRemove = null;
    document.getElementById('removeFriendModal').style.display = 'none';
}

async function confirmRemoveFriend() {
    if (!friendToRemove) return;

    try {
        const response = await fetch(`/api/friendship/remove/${friendToRemove}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            const error = await response.json();
            alert(error.error || 'Failed to remove friend');
            return;
        }

        // Close modal and reload friends list
        closeRemoveFriendModal();
        await loadFriendsPage();
    } catch (error) {
        console.error('Error removing friend:', error);
        alert('Failed to remove friend');
    }
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