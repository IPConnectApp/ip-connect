// Create connection to SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();

// Receive messages from server
connection.on("ReceiveMessage", function (senderName, message, timestamp) {
    const messagesList = document.getElementById("messagesList");
    const messageDiv = document.createElement("div");
    messageDiv.className = "mb-2";
    
    const time = new Date(timestamp).toLocaleTimeString('en-US', { 
        hour: '2-digit', 
        minute: '2-digit' 
    });
    
    messageDiv.innerHTML = `<strong>${senderName}:</strong> ${message} <small class="text-muted">(${time})</small>`;
    messagesList.appendChild(messageDiv);
    
    // Auto scroll to bottom
    messagesList.scrollTop = messagesList.scrollHeight;
});

// Start connection
connection.start()
    .then(() => console.log("✅ Connected to chat!"))
    .catch(err => console.error("Connection error:", err));

// Send message on button click
document.getElementById("sendButton").addEventListener("click", sendMessage);

// Send message on Enter key
document.getElementById("messageInput").addEventListener("keypress", function (e) {
    if (e.key === "Enter") {
        sendMessage();
    }
});

function sendMessage() {
    const user = document.getElementById("userInput").value;
    const message = document.getElementById("messageInput").value;
    
    if (user && message) {
        connection.invoke("SendMessage", user, message)
            .catch(err => console.error("Send error:", err));
        
        // Clear input
        document.getElementById("messageInput").value = "";
    } else {
        alert("Please enter your name and message!");
    }
}