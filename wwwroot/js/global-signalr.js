// Global SignalR connection for the entire site
let globalConnection = null;

async function initializeGlobalSignalR() {
    if (globalConnection) {
        return;
    }

    globalConnection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")
        .withAutomaticReconnect()
        .build();

    // Listen for new messages globally
    globalConnection.on("ReceiveMessage", function(message) {
        if (typeof updateProfileUnreadBadge === 'function') {
            updateProfileUnreadBadge();
        }
    });

    // Listen for new conversations
    globalConnection.on("NewConversationCreated", function(data) {
        if (typeof updateProfileUnreadBadge === 'function') {
            updateProfileUnreadBadge();
        }
    });

    // Start connection
    try {
        await globalConnection.start();
    } catch (err) {
        console.error("Global SignalR Connection Error:", err);
        setTimeout(initializeGlobalSignalR, 5000);
    }

    // Log connection errors
    globalConnection.onclose((error) => {
        if (error) {
            console.error("Global SignalR Connection Closed:", error);
        }
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    initializeGlobalSignalR();
});

// Export for use in other scripts
window.globalConnection = globalConnection;