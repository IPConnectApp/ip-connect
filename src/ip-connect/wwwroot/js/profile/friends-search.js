// IP Connect - Friends Search

document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('friendSearchInput');
    const searchResults = document.getElementById('friendSearchResults');

    // Only run if search elements exist (own profile)
    if (!searchInput || !searchResults) return;

    let searchTimeout;

    // Search as user types
    searchInput.addEventListener('input', function () {
        const query = this.value.trim();

        clearTimeout(searchTimeout);

        if (query.length === 0) {
            searchResults.style.display = 'none';
            return;
        }

        searchTimeout = setTimeout(() => {
            searchUsers(query);
        }, 300);
    });

    // Close dropdown when clicking outside
    document.addEventListener('click', function (e) {
        if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
            searchResults.style.display = 'none';
        }
    });

    // Search users via API
    async function searchUsers(query) {
        try {
            const response = await fetch(`/api/chat/search?query=${encodeURIComponent(query)}`);

            if (!response.ok) {
                throw new Error('Search failed');
            }

            const users = await response.json();

            // Get friendship status for each user
            const usersWithStatus = await Promise.all(
                users.map(async user => {
                    const status = await getFriendshipStatus(user.id);
                    return { ...user, status };
                })
            );

            displaySearchResults(usersWithStatus);
        } catch (error) {
            console.error('Error searching users:', error);
            searchResults.innerHTML = '<div class="friends-search-error">Failed to search users</div>';
            searchResults.style.display = 'block';
        }
    }

    // Get friendship status for a user
    async function getFriendshipStatus(userId) {
        try {
            const response = await fetch(`/api/friendship/status/${userId}`);
            if (!response.ok) return null;
            return await response.json();
        } catch (error) {
            console.error('Error getting friendship status:', error);
            return null;
        }
    }

    // Display search results
    function displaySearchResults(users) {
        if (users.length === 0) {
            searchResults.innerHTML = '<div class="friends-search-no-results">No users found</div>';
            searchResults.style.display = 'block';
            return;
        }

        const resultsHtml = users.map(user => {
            return `
                <div class="friends-search-result" onclick="goToProfile('${user.username}')">
                    <img src="${user.profilePictureUrl || '/images/default-avatar.jpg'}" 
                         alt="${user.username}" 
                         class="friends-search-result-avatar">
                    <div class="friends-search-result-info">
                        <div class="friends-search-result-username">${escapeHtml(user.username)}</div>
                    </div>
                    ${getActionButton(user)}
                </div>
            `;
        }).join('');

        searchResults.innerHTML = resultsHtml;
        searchResults.style.display = 'block';
    }

    // Get the right action button based on friendship status
    function getActionButton(user) {
        const status = user.status;

        // Already friends → X button opens remove modal
        if (status && status.areFriends) {
            return `
                <button class="btn-remove-friend-small" onclick="event.stopPropagation(); openRemoveFriendModal('${status.friendshipId}', '${escapeHtml(user.username)}')">
                    <i class="fas fa-times"></i>
                </button>
            `;
        }

        // Pending - YOU sent the request → disabled Pending
        if (status && status.hasPendingRequest && status.isSentByMe) {
            return `
                <button class="btn-pending-friend" disabled>
                    <i class="fas fa-clock"></i> Pending
                </button>
            `;
        }

        // Pending - THEY sent you the request → Accept button
        if (status && status.hasPendingRequest && !status.isSentByMe) {
            return `
                <button class="btn-accept-friend" onclick="event.stopPropagation(); acceptFriend('${status.friendshipId}')">
                    <i class="fas fa-check"></i> Accept
                </button>
            `;
        }

        // Not friend → Add button
        return `
            <button class="btn-add-friend-small" onclick="event.stopPropagation(); addFriend('${user.id}')">
                <i class="fas fa-user-plus"></i> Add
            </button>
        `;
    }

    // Navigate to user profile
    function goToProfile(username) {
        window.location.href = `/profile/${username}/photos`;
    }

    // Send friend request
    async function addFriend(userId) {
        try {
            const response = await fetch('/api/friendship/send', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ friendUserId: userId })
            });

            if (!response.ok) {
                const error = await response.json();
                alert(error.error || 'Failed to send friend request');
                return;
            }

            // Re-search to update buttons
            const query = searchInput.value.trim();
            if (query.length > 0) {
                await searchUsers(query);
            }

            // Reload the friends grid
            await loadFriendsPage();
        } catch (error) {
            console.error('Error sending friend request:', error);
            alert('Failed to send friend request');
        }
    }

    // Accept friend request
    async function acceptFriend(friendshipId) {
        try {
            const response = await fetch(`/api/friendship/accept/${friendshipId}`, {
                method: 'POST'
            });

            if (!response.ok) {
                const error = await response.json();
                alert(error.error || 'Failed to accept friend request');
                return;
            }

            // Re-search to update buttons
            const query = searchInput.value.trim();
            if (query.length > 0) {
                await searchUsers(query);
            }

            // Reload the friends grid
            await loadFriendsPage();
        } catch (error) {
            console.error('Error accepting friend request:', error);
            alert('Failed to accept friend request');
        }
    }

    // Expose globally
    window.addFriend = addFriend;
    window.acceptFriend = acceptFriend;
    window.goToProfile = goToProfile;
});

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}