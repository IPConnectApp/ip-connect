//import { initializeEmojiPicker } from '../utils/emoji.js';

// Global variables
let currentConversationId = null;
let currentChatUsername = null;
let currentChatUserId = null;
let connection = null;
let typingTimeout = null;
let typingUsers = {};
let isSending = false;

// Initialize chat-specific SignalR handlers
async function initializeChatSignalR() {
    // Wait for global connection to be ready
    let attempts = 0;
    while (!window.globalConnection && attempts < 50) {
        await new Promise(resolve => setTimeout(resolve, 100));
        attempts++;
    }

    if (!window.globalConnection) {
        return;
    }

    connection = window.globalConnection;

    // Remove any existing handlers to prevent duplicates
    connection.off("ReceiveMessage");
    connection.off("NewMessageNotification");
    connection.off("NewConversationCreated");

    // Chat-specific message handler
    connection.on("ReceiveMessage", function (message) {
        // If we're viewing this conversation, add message to UI
        if (currentConversationId === message.conversationId) {
            if (message.senderUsername === window.currentUsername) {
                return;
            }

            const isSent = false;
            addMessageToUI(message, isSent);
            markAsRead(message.conversationId);
            return;
        }

        // Modal is closed - reload list and update badge
        loadConversations();

        if (typeof updateProfileUnreadBadge === 'function') {
            updateProfileUnreadBadge();
        }
    });

    // Badge notification handler for chat list updates
    connection.on("NewMessageNotification", function (message) {
        if (!currentConversationId || currentConversationId !== message.conversationId) {
            loadConversations();
        }
    });

    // Chat-specific new conversation handler
    connection.on("NewConversationCreated", function (data) {
        loadConversations();
        joinConversation(data.conversationId);
    });

    connection.on("UserTyping", function (data) {
        if (data.conversationId === currentConversationId && data.username !== window.currentUsername) {
            showTypingIndicator(data.username);
        }

        // Track typing for chat list
        if (data.username !== window.currentUsername) {
            typingUsers[data.conversationId] = data.username;
            updateChatListTyping(data.conversationId, data.username);
        }
    });

    connection.on("UserStoppedTyping", function (data) {
        // Hide modal typing indicator
        if (data.conversationId === currentConversationId) {
            hideTypingIndicator();
        }

        // Remove from typing list
        delete typingUsers[data.conversationId];
        updateChatListTyping(data.conversationId, null);
    });
}

// Initialize chat SignalR
initializeChatSignalR();

// Join all user's conversation groups
async function joinAllConversations() {
    try {
        const response = await fetch('/api/chat/conversations');

        if (!response.ok) {
            return;
        }

        const conversations = await response.json();

        // Join each conversation group
        for (const conv of conversations) {
            await joinConversation(conv.id);
            console.log(`Joined conversation group: ${conv.id}`);
        }

    } catch (error) {
        console.error('Error joining conversation groups:', error);
    }
}

// Join conversation group
async function joinConversation(conversationId) {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        try {
            await connection.invoke("JoinConversation", conversationId);
        } catch (err) {
            console.error("Error joining conversation:", err);
        }
    } else {
        console.error(`Cannot join conversation ${conversationId} - connection not ready. State:`, connection?.state);
    }
}

// Leave conversation group
async function leaveConversation(conversationId) {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        try {
            await connection.invoke("LeaveConversation", conversationId);
        } catch (err) {
            console.error("Error leaving conversation:", err);
        }
    }
}

// Chat Search Functionality
const searchInput = document.getElementById('userSearchInput');
const searchResults = document.getElementById('searchResults');

let searchTimeout;

// Search users as user types
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

// Close search results when clicking outside
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
        displaySearchResults(users);

    } catch (error) {
        console.error('Error searching users:', error);
        searchResults.innerHTML = '<div class="search-error">Failed to search users</div>';
        searchResults.style.display = 'block';
    }
}

// Display search results
function displaySearchResults(users) {
    if (users.length === 0) {
        searchResults.innerHTML = '<div class="search-no-results">No users found</div>';
        searchResults.style.display = 'block';
        return;
    }

    const resultsHtml = users.map(user => `
        <div class="search-result-item" onclick="startChat('${user.id}', '${user.username}', '${user.profilePictureUrl || '/images/default-avatar.jpg'}')">
            <img src="${user.profilePictureUrl || '/images/default-avatar.jpg'}" 
                 alt="${user.username}" 
                 class="search-result-avatar">
            <div class="search-result-info">
                <div class="search-result-username">${user.username}</div>
            </div>
        </div>
    `).join('');

    searchResults.innerHTML = resultsHtml;
    searchResults.style.display = 'block';
}

// Start chat with selected user
async function startChat(userId, username, profilePicture) {
    // Hide search results
    if (searchResults) {
        searchResults.style.display = 'none';
    }
    if (searchInput) {
        searchInput.value = '';
    }

    try {
        // Call API to get or create conversation
        const response = await fetch(`/api/chat/start/${userId}`, {
            method: 'POST'
        });

        if (!response.ok) {
            throw new Error('Failed to start conversation');
        }

        const conversation = await response.json();

        // Store current chat info
        currentConversationId = conversation.id;
        currentChatUsername = username;
        currentChatUserId = userId;

        // Join SignalR conversation group
        await joinConversation(conversation.id);

        // Open modal
        openChatModal(username, profilePicture);

        // Load messages
        await loadMessages(conversation.id);

        // Reload conversations list to show new conversation
        await loadConversations();
    } catch (error) {
        console.error('Error starting chat:', error);
        alert('Failed to start chat. Please try again.');
    }
}

// Open chat modal
function openChatModal(username, profilePicture) {
    const modal = document.getElementById('chatModal');
    const modalUsername = document.getElementById('modalUsername');
    const modalAvatar = document.getElementById('modalUserAvatar');

    modalUsername.textContent = username;
    modalAvatar.src = profilePicture;
    modalAvatar.alt = username;

    modal.style.display = 'flex';

    // Initialize emoji picker
    setTimeout(() => {
        initializeEmojiPicker();
    }, 100);

    // Focus on input and attach typing listeners
    const messageInput = document.getElementById('messageInput');
    if (messageInput) {
        messageInput.focus();

        // Attach typing event listeners (if not already attached)
        if (!messageInput.hasAttribute('data-typing-listeners')) {
            messageInput.addEventListener('input', function () {
                notifyTyping();
                clearTimeout(typingTimeout);
                typingTimeout = setTimeout(() => {
                    notifyStoppedTyping();
                }, 2000);
            });

            messageInput.addEventListener('keypress', function (e) {
                if (e.key === 'Enter') {
                    sendMessage();
                    notifyStoppedTyping();
                }
            });

            // Mark as having listeners
            messageInput.setAttribute('data-typing-listeners', 'true');
        }
    }

    // Close modal when clicking on backdrop (outside modal content)
    modal.addEventListener('click', function handleModalClick(e) {
        if (e.target === modal) {
            closeChatModal();
            // Remove listener after closing
            modal.removeEventListener('click', handleModalClick);
        }
    });
}

// Close chat modal
async function closeChatModal() {
    const modal = document.getElementById('chatModal');
    modal.style.display = 'none';

    // Clear messages
    document.getElementById('chatMessages').innerHTML = '';

    if (currentConversationId) {
        await markAsRead(currentConversationId);
        await leaveConversation(currentConversationId);
    }

    // Reset current chat info
    currentConversationId = null;
    currentChatUsername = null;
    currentChatUserId = null;

    //Reload conversations to update the list
    await loadConversations();

    // Update profile tab badge
    if (typeof updateProfileUnreadBadge === 'function') {
        updateProfileUnreadBadge();
    }
}

// Load messages for conversation
async function loadMessages(conversationId) {
    try {
        const response = await fetch(`/api/chat/messages/${conversationId}`);

        if (!response.ok) {
            throw new Error('Failed to load messages');
        }

        const messages = await response.json();
        displayMessages(messages);

        // Mark as read
        await markAsRead(conversationId);

    } catch (error) {
        console.error('Error loading messages:', error);
        document.getElementById('chatMessages').innerHTML = '<div class="chat-empty">Failed to load messages</div>';
    }
}

// Display messages in modal
function displayMessages(messages) {
    const messagesContainer = document.getElementById('chatMessages');

    if (messages.length === 0) {
        messagesContainer.innerHTML = '<div class="chat-empty">No messages yet. Start the conversation!</div>';
        return;
    }

    const messagesHtml = messages.map(message => {
        const isSent = message.senderUsername !== currentChatUsername;
        const messageClass = isSent ? 'sent' : 'received';

        //Parse as UTC and format with date + 24h time
        const timestamp = message.timestamp.endsWith('Z') ? message.timestamp : message.timestamp + 'Z';
        const messageDate = new Date(timestamp);
        const now = new Date();
        const isToday = messageDate.toDateString() === now.toDateString();

        const time = isToday
            ? messageDate.toLocaleString('en-US', {
                hour: '2-digit',
                minute: '2-digit',
                hour12: false  //
            })
            : messageDate.toLocaleString('en-US', {
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
                hour12: false  //
            });

        return `
            <div class="chat-message ${messageClass}">
                <div class="chat-message-bubble">
                    <div class="chat-message-text">${escapeHtml(message.text)}</div>
                    <div class="chat-message-time">${time}</div>
                </div>
            </div>
        `;
    }).join('');

    messagesContainer.innerHTML = messagesHtml;

    // Scroll to bottom
    scrollToBottom();
}

// Send message
async function sendMessage() {
    if (isSending) return;

    const input = document.getElementById('messageInput');
    const text = input.value.trim();

    if (!text || !currentConversationId) {
        return;
    }

    isSending = true;

    try {
        const response = await fetch('/api/chat/send', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                conversationId: currentConversationId,
                text: text
            })
        });

        if (!response.ok) {
            throw new Error('Failed to send message');
        }

        const message = await response.json();

        // Clear input
        input.value = '';

        // Add message to UI
        addMessageToUI(message, true);

        // Mark as read after sending (update LastReadAt)
        await markAsRead(currentConversationId);
    } catch (error) {
        console.error('Error sending message:', error);
        alert('Failed to send message. Please try again.');
    } finally {
        isSending = false;
    }
}

// Add message to UI
function addMessageToUI(message, isSent) {
    const messagesContainer = document.getElementById('chatMessages');

    // Remove empty state if exists
    const emptyState = messagesContainer.querySelector('.chat-empty');
    if (emptyState) {
        emptyState.remove();
    }

    const messageClass = isSent ? 'sent' : 'received';

    //Parse as UTC and format with 24h time
    const timestamp = message.timestamp.endsWith('Z') ? message.timestamp : message.timestamp + 'Z';
    const messageDate = new Date(timestamp);
    const now = new Date();
    const isToday = messageDate.toDateString() === now.toDateString();

    const time = isToday
        ? messageDate.toLocaleString('en-US', {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        })
        : messageDate.toLocaleString('en-US', {
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        });

    const messageHtml = `
        <div class="chat-message ${messageClass}">
            <div class="chat-message-bubble">
                <div class="chat-message-text">${escapeHtml(message.text)}</div>
                <div class="chat-message-time">${time}</div>
            </div>
        </div>
    `;

    messagesContainer.insertAdjacentHTML('beforeend', messageHtml);
    scrollToBottom();
}

// Mark conversation as read
async function markAsRead(conversationId) {
    try {
        const response = await fetch(`/api/chat/mark-read/${conversationId}`, {
            method: 'POST'
        });

        if (response.ok) {
            console.log(`Successfully marked conversation ${conversationId} as read`);
        } else {
            console.error(`Failed to mark as read. Status: ${response.status}`);
        }
    } catch (error) {
        console.error('Error marking as read:', error);
    }
}

// Scroll messages to bottom
function scrollToBottom() {
    const messagesContainer = document.getElementById('chatMessages');
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Send message on Enter key
document.addEventListener('DOMContentLoaded', function () {
    const messageInput = document.getElementById('messageInput');

    if (messageInput) {
        messageInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                sendMessage();
            }
        });
    }
});

// Close modal on Escape key
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        e.preventDefault();
        closeChatModal();
    }
});

// Load user's conversations on page load
async function loadConversations() {
    try {
        const response = await fetch('/api/chat/conversations');

        if (!response.ok) {
            throw new Error('Failed to load conversations');
        }

        const conversations = await response.json();
        displayConversations(conversations);

    } catch (error) {
        console.error('Error loading conversations:', error);
    }
}

// Display conversations in the list
function displayConversations(conversations) {
    const chatList = document.getElementById('privateChats');

    // If chat list doesn't exist (not on Chats page), skip
    if (!chatList) {
        return;
    }

    if (conversations.length === 0) {
        chatList.innerHTML = '<p class="no-chats-message">No conversations yet. Search for users to start chatting!</p>';
        return;
    }

    const conversationsHtml = conversations.map(conv => {
        const otherUser = conv.otherUserName || 'Unknown User';
        const profilePic = conv.otherUserProfilePicture || '/images/default-avatar.jpg';

        const timeToDisplay = conv.lastMessageTime || conv.createdAt;

        const timestamp = timeToDisplay.endsWith('Z') ? timeToDisplay : timeToDisplay + 'Z';
        const timeAgo = getTimeAgo(timestamp);

        const lastMessage = conv.lastMessageText || 'No messages yet';

        // Truncate long messages
        const displayMessage = lastMessage.length > 50
            ? lastMessage.substring(0, 50) + '...'
            : lastMessage;

        // Unread badge
        const unreadBadge = conv.unreadCount > 0
            ? `<span class="unread-badge">${conv.unreadCount}</span>`
            : '';

        return `
            <div class="chat-item" onclick="openConversationFromList('${conv.id}', '${otherUser}', '${profilePic}')">
                <img src="${profilePic}" alt="${otherUser}" class="chat-avatar">
                <div class="chat-info">
                    <div class="chat-username">${otherUser}</div>
                    <div class="chat-last-message">${escapeHtml(displayMessage)}</div>
                </div>
                <div class="chat-meta">
                    <div class="chat-time">${timeAgo}</div>
                    ${unreadBadge}
                </div>
            </div>
        `;
    }).join('');

    chatList.innerHTML = conversationsHtml;
}

// Open conversation from the chat list
async function openConversationFromList(conversationId, username, profilePicture) {
    try {
        // Leave previous conversation if any
        if (currentConversationId) {
            await leaveConversation(currentConversationId);
        }

        // Store current chat info
        currentConversationId = parseInt(conversationId);
        currentChatUsername = username;

        // Join SignalR conversation group
        await joinConversation(currentConversationId);

        // Open modal
        openChatModal(username, profilePicture);

        // Load messages
        await loadMessages(currentConversationId);

    } catch (error) {
        console.error('Error opening conversation:', error);
        alert('Failed to open chat. Please try again.');
    }
}

// Format time ago
function getTimeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now - date) / 1000);

    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;

    return date.toLocaleDateString();
}

function showTypingIndicator(username) {
    const indicator = document.getElementById('typingIndicator');
    const usernameSpan = document.getElementById('typingUsername');

    if (indicator && usernameSpan) {
        usernameSpan.textContent = username;
        indicator.style.display = 'flex';
        scrollToBottom();
    }
}

function hideTypingIndicator() {
    const indicator = document.getElementById('typingIndicator');
    if (indicator) {
        indicator.style.display = 'none';
    }
}

function notifyTyping() {
    if (connection && currentConversationId) {
        connection.invoke("UserTyping", currentConversationId, window.currentUsername)
            .catch(err => console.error('Error sending typing notification:', err));
    }
}

function notifyStoppedTyping() {
    if (connection && currentConversationId) {
        connection.invoke("UserStoppedTyping", currentConversationId, window.currentUsername)
            .catch(err => console.error('Error sending stopped typing notification:', err));
    }
}

function updateChatListTyping(conversationId, username) {
    const chatItem = document.querySelector(`[onclick*="openConversationFromList('${conversationId}'"]`);

    if (chatItem) {
        const lastMessageElement = chatItem.querySelector('.chat-last-message');

        if (lastMessageElement) {
            if (username) {
                lastMessageElement.innerHTML = `<em style="color: #008080;">${escapeHtml(username)} is typing...</em>`;
            } else {
                // Reload conversations to restore original last message
                loadConversations();
            }
        }
    }
}

// Load conversations when page loads
document.addEventListener('DOMContentLoaded', async function () {
    await initializeChatSignalR();

    await loadConversations();
    await joinAllConversations();

    const messageInput = document.getElementById('messageInput');
    if (messageInput) {
        messageInput.addEventListener('input', function () {
            notifyTyping();

            clearTimeout(typingTimeout);

            typingTimeout = setTimeout(() => {
                notifyStoppedTyping();
            }, 2000);
        });

        messageInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                sendMessage();
                notifyStoppedTyping();
            }
        });
    }
});

// Reload conversations when window gets focus
window.addEventListener('focus', function () {
    loadConversations();
    joinAllConversations();
});

// Export functions to window for onclick handlers and global access
window.openConversationFromList = openConversationFromList;
window.startChat = startChat;
window.closeChatModal = closeChatModal;
window.sendMessage = sendMessage;