// Emoji Picker Utilities
let isInitialized = false;
let clickListenerAdded = false;

window.initializeEmojiPicker = function () {
    const emojiButton = document.getElementById('emojiButton');
    const emojiPickerContainer = document.getElementById('emojiPickerContainer');
    const messageInput = document.getElementById('messageInput');

    if (!emojiButton || !emojiPickerContainer) return;

    // Only initialize once
    if (isInitialized) return;

    // Toggle emoji picker
    emojiButton.addEventListener('click', function (e) {
        e.stopPropagation();
        const isVisible = emojiPickerContainer.style.display === 'block';

        if (isVisible) {
            closeEmojiPicker();
        } else {
            openEmojiPicker();
        }
    });

    // Handle emoji selection
    const picker = emojiPickerContainer.querySelector('emoji-picker');
    if (picker) {
        picker.addEventListener('emoji-click', function (event) {
            insertEmoji(event.detail.unicode);
        });

        // Prevent clicks inside picker from propagating
        picker.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    }

    // Prevent clicks on container from propagating
    emojiPickerContainer.addEventListener('click', function (e) {
        e.stopPropagation();
    });

    // Close picker when clicking outside (add only once)
    if (!clickListenerAdded) {
        document.addEventListener('click', function (e) {
            const container = document.getElementById('emojiPickerContainer');
            const button = document.getElementById('emojiButton');

            if (container && button) {
                if (!container.contains(e.target) && !button.contains(e.target)) {
                    closeEmojiPicker();
                }
            }
        });
        clickListenerAdded = true;
    }

    isInitialized = true;
};

window.resetEmojiPicker = function () {
    isInitialized = false;
};

window.openEmojiPicker = function () {
    const emojiPickerContainer = document.getElementById('emojiPickerContainer');
    const emojiButton = document.getElementById('emojiButton');

    if (emojiPickerContainer) {
        emojiPickerContainer.style.display = 'block';
    }

    if (emojiButton) {
        emojiButton.classList.add('active');
    }
};

window.closeEmojiPicker = function () {
    const emojiPickerContainer = document.getElementById('emojiPickerContainer');
    const emojiButton = document.getElementById('emojiButton');

    if (emojiPickerContainer) {
        emojiPickerContainer.style.display = 'none';
    }

    if (emojiButton) {
        emojiButton.classList.remove('active');
    }
};

window.insertEmoji = function (emoji) {
    const messageInput = document.getElementById('messageInput');

    if (!messageInput) return;

    const cursorPos = messageInput.selectionStart || messageInput.value.length;
    const textBefore = messageInput.value.substring(0, cursorPos);
    const textAfter = messageInput.value.substring(cursorPos);

    messageInput.value = textBefore + emoji + textAfter;

    const newPos = cursorPos + emoji.length;
    messageInput.selectionStart = newPos;
    messageInput.selectionEnd = newPos;

    messageInput.focus();
    closeEmojiPicker();
};

// Suppress harmless IndexedDB errors from emoji-picker-element
window.addEventListener('unhandledrejection', function (event) {
    if (event.reason?.message?.includes('IDBDatabase') ||
        event.reason?.message?.includes('transaction') ||
        event.reason?.message?.includes('database.js')) {
        event.preventDefault();
    }
});