// Global SignalR connection for the entire site
window.globalConnection = null;

async function initializeGlobalSignalR() {
    if (window.globalConnection) {
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")
        .withAutomaticReconnect()
        .build();

    // Listen for new messages globally
    connection.on("NewMessageNotification", function(message) {
        //Update badge on ALL pages (Photos, Friends, Settings, etc.)
        if (typeof window.updateProfileUnreadBadge === 'function') {
            window.updateProfileUnreadBadge();
        }
    });

    // Listen for new conversations
    connection.on("NewConversationCreated", function(data) {
        //Update badge when new conversation created
        if (typeof window.updateProfileUnreadBadge === 'function') {
            window.updateProfileUnreadBadge();
        }
    });

    // Start connection
    try {
        await connection.start();
        // Export AFTER successful connection
        window.globalConnection = connection;
        
    } catch (err) {
        console.error("Global SignalR Connection Error:", err);
        setTimeout(initializeGlobalSignalR, 5000);
    }

    // Log connection errors
    connection.onclose((error) => {
        if (error) {
            console.error("Global SignalR Connection Closed:", error);
        }
        window.globalConnection = null;
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    initializeGlobalSignalR();
});