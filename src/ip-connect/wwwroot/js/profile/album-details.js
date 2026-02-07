// ========================================
// Global State
// ========================================
let currentPhotos = [];
let currentLightboxIndex = 0;
let photoToDelete = null;

// ========================================
// Initialize on Page Load
// ========================================
document.addEventListener('DOMContentLoaded', function () {
    console.log('Album Details page loaded');
    loadAlbumData();

    if (window.albumConfig.isOwnProfile) {
        initializeUpload();
    }

    // Keyboard navigation for lightbox
    document.addEventListener('keydown', handleKeyboardNavigation);
});

// ========================================
// Load Album Data
// ========================================
async function loadAlbumData() {
    try {
        const response = await fetch(`/api/album/${window.albumConfig.albumId}`);

        if (!response.ok) {
            throw new Error('Failed to load album');
        }

        const album = await response.json();

        // Update header info
        document.getElementById('albumName').textContent = album.name;
        document.getElementById('albumDescription').textContent = album.description || '';
        document.getElementById('albumDate').textContent = formatDate(album.createdAt);

        // Load photos
        await loadPhotos();

    } catch (error) {
        console.error('Error loading album:', error);
        showError('Failed to load album details');
    }
}

// ========================================
// Load Photos
// ========================================
async function loadPhotos() {
    try {
        const response = await fetch(`/api/photo/album/${window.albumConfig.albumId}`);

        if (!response.ok) {
            throw new Error('Failed to load photos');
        }

        currentPhotos = await response.json();
        renderPhotos();

    } catch (error) {
        console.error('Error loading photos:', error);
        showError('Failed to load photos');
    }
}

// ========================================
// Render Photos Grid
// ========================================
function renderPhotos() {
    const grid = document.getElementById('photosGrid');

    if (currentPhotos.length === 0) {
        grid.innerHTML = `
            <div class="empty-photos">
                <i class="fas fa-images"></i>
                <h3>No Photos Yet</h3>
                <p>${window.albumConfig.isOwnProfile ? 'Upload your first photo to this album!' : 'This album is empty.'}</p>
            </div>
        `;
        return;
    }

    // Sort by display order
    currentPhotos.sort((a, b) => a.displayOrder - b.displayOrder);

    grid.innerHTML = currentPhotos.map((photo, index) => `
        <div class="photo-item ${window.albumConfig.isOwnProfile ? 'draggable' : ''}" 
             data-photo-id="${photo.id}"
             data-index="${index}"
             ${window.albumConfig.isOwnProfile ? 'draggable="true"' : ''}
             onclick="openLightbox(${index})">
            
            ${window.albumConfig.isOwnProfile ? `
                <div class="photo-drag-handle" title="Drag to reorder">
                    <i class="fas fa-grip-vertical"></i>
                </div>
                <button class="photo-delete-btn" onclick="event.stopPropagation(); deletePhoto(${photo.id})" title="Delete photo">
                    <i class="fas fa-trash"></i>
                </button>
            ` : ''}
            
            <img src="${photo.photoUrl}" alt="Photo" loading="lazy">
        </div>
    `).join('');

    // Add drag & drop events if owner
    if (window.albumConfig.isOwnProfile) {
        initializeDragAndDrop();
    }
}

// ========================================
// Drag & Drop for Reordering (Owner Only)
// ========================================
function initializeDragAndDrop() {
    const photoItems = document.querySelectorAll('.photo-item.draggable');

    photoItems.forEach(item => {
        item.addEventListener('dragstart', handleDragStart);
        item.addEventListener('dragover', handleDragOver);
        item.addEventListener('drop', handleDrop);
        item.addEventListener('dragend', handleDragEnd);
        item.addEventListener('dragenter', handleDragEnter);
        item.addEventListener('dragleave', handleDragLeave);
    });
}

let draggedElement = null;

function handleDragStart(e) {
    draggedElement = this;
    this.classList.add('dragging');
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/html', this.innerHTML);
}

function handleDragOver(e) {
    if (e.preventDefault) {
        e.preventDefault();
    }
    e.dataTransfer.dropEffect = 'move';
    return false;
}

function handleDragEnter(e) {
    if (this !== draggedElement) {
        this.classList.add('drag-over');
    }
}

function handleDragLeave(e) {
    this.classList.remove('drag-over');
}

function handleDrop(e) {
    if (e.stopPropagation) {
        e.stopPropagation();
    }

    if (draggedElement !== this) {
        const draggedIndex = parseInt(draggedElement.dataset.index);
        const targetIndex = parseInt(this.dataset.index);

        // Reorder photos array
        const [movedPhoto] = currentPhotos.splice(draggedIndex, 1);
        currentPhotos.splice(targetIndex, 0, movedPhoto);

        // Update display order
        updatePhotoOrder();
    }

    return false;
}

function handleDragEnd(e) {
    this.classList.remove('dragging');

    document.querySelectorAll('.photo-item').forEach(item => {
        item.classList.remove('drag-over');
    });
}

async function updatePhotoOrder() {
    try {
        // Update display order for each photo
        const updates = currentPhotos.map((photo, index) => ({
            id: photo.id,
            displayOrder: index
        }));

        const response = await fetch(`/api/photo/reorder`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(updates)
        });

        if (!response.ok) {
            throw new Error('Failed to update order');
        }

        // Re-render with new order
        renderPhotos();

    } catch (error) {
        console.error('Error updating photo order:', error);
        showError('Failed to save new order');
        loadPhotos(); // Reload original order
    }
}

// ========================================
// Upload Functions (Owner Only)
// ========================================
function initializeUpload() {
    const dropZone = document.getElementById('dropZone');
    const photoInput = document.getElementById('photoInput');

    // Click to browse
    dropZone.addEventListener('click', () => photoInput.click());

    // Prevent default drag behaviors
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    // Highlight drop zone
    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => dropZone.classList.add('drag-over'), false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => dropZone.classList.remove('drag-over'), false);
    });

    // Handle dropped files
    dropZone.addEventListener('drop', handleFileDrop, false);

    // Handle selected files
    photoInput.addEventListener('change', (e) => {
        handleFiles(e.target.files);
    });
}

function preventDefaults(e) {
    e.preventDefault();
    e.stopPropagation();
}

function handleFileDrop(e) {
    const dt = e.dataTransfer;
    const files = dt.files;
    handleFiles(files);
}

async function handleFiles(files) {
    if (!files || files.length === 0) return;

    const validFiles = Array.from(files).filter(file => {
        if (!file.type.startsWith('image/')) {
            showError(`${file.name} is not an image`);
            return false;
        }
        if (file.size > 10 * 1024 * 1024) { // 10MB limit
            showError(`${file.name} is too large (max 10MB)`);
            return false;
        }
        return true;
    });

    if (validFiles.length === 0) return;

    await uploadPhotos(validFiles);
}

async function uploadPhotos(files) {
    const progressDiv = document.getElementById('uploadProgress');
    const progressBar = document.getElementById('progressBar');
    const progressText = document.getElementById('progressText');

    progressDiv.style.display = 'block';

    let uploaded = 0;
    const total = files.length;

    for (const file of files) {
        try {
            const formData = new FormData();
            formData.append('file', file);
            formData.append('albumId', window.albumConfig.albumId);

            const response = await fetch('/api/photo', {
                method: 'POST',
                body: formData
            });

            if (!response.ok) {
                throw new Error('Upload failed');
            }

            uploaded++;
            const progress = (uploaded / total) * 100;
            progressBar.style.width = `${progress}%`;
            progressText.textContent = `Uploading... ${uploaded}/${total}`;

        } catch (error) {
            console.error('Error uploading file:', error);
            showError(`Failed to upload ${file.name}`);
        }
    }

    // Hide progress and reload photos
    setTimeout(() => {
        progressDiv.style.display = 'none';
        progressBar.style.width = '0%';
        loadPhotos();
    }, 500);
}

// ========================================
// Delete Photo (Owner Only)
// ========================================
function deletePhoto(photoId) {
    photoToDelete = photoId;
    document.getElementById('deletePhotoModal').style.display = 'flex';
}

function closeDeletePhotoModal() {
    document.getElementById('deletePhotoModal').style.display = 'none';
    photoToDelete = null;
}

async function confirmDeletePhoto() {
    if (!photoToDelete) return;

    try {
        const response = await fetch(`/api/photo/${photoToDelete}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error('Failed to delete photo');
        }

        closeDeletePhotoModal();
        showSuccess('Photo deleted successfully');
        loadPhotos();

    } catch (error) {
        console.error('Error deleting photo:', error);
        showError('Failed to delete photo');
    }
}

// ========================================
// Lightbox Functions
// ========================================
function openLightbox(index) {
    currentLightboxIndex = index;
    const modal = document.getElementById('lightboxModal');
    const img = document.getElementById('lightboxImage');
    const counter = document.getElementById('lightboxCounter');

    img.src = currentPhotos[index].photoUrl;
    counter.textContent = `${index + 1} / ${currentPhotos.length}`;

    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';

    updateLightboxButtons();
}

function closeLightbox() {
    const modal = document.getElementById('lightboxModal');
    modal.style.display = 'none';
    document.body.style.overflow = 'auto';
}

function navigateLightbox(direction) {
    currentLightboxIndex += direction;

    // Loop around
    if (currentLightboxIndex < 0) {
        currentLightboxIndex = currentPhotos.length - 1;
    } else if (currentLightboxIndex >= currentPhotos.length) {
        currentLightboxIndex = 0;
    }

    const img = document.getElementById('lightboxImage');
    const counter = document.getElementById('lightboxCounter');

    img.src = currentPhotos[currentLightboxIndex].photoUrl;
    counter.textContent = `${currentLightboxIndex + 1} / ${currentPhotos.length}`;

    updateLightboxButtons();
}

function updateLightboxButtons() {
    const prevBtn = document.querySelector('.lightbox-prev');
    const nextBtn = document.querySelector('.lightbox-next');

    // Always enable buttons (we loop around)
    prevBtn.disabled = false;
    nextBtn.disabled = false;
}

function handleKeyboardNavigation(e) {
    const modal = document.getElementById('lightboxModal');

    if (modal.style.display !== 'flex') return;

    switch (e.key) {
        case 'Escape':
            closeLightbox();
            break;
        case 'ArrowLeft':
            navigateLightbox(-1);
            break;
        case 'ArrowRight':
            navigateLightbox(1);
            break;
    }
}

// ========================================
// Utility Functions
// ========================================
function formatDate(dateString) {
    const date = new Date(dateString);
    const options = { year: 'numeric', month: 'long', day: 'numeric' };
    return date.toLocaleDateString('en-US', options);
}

function showError(message) {
    // You can replace this with a toast notification library
    alert(message);
}

function showSuccess(message) {
    // You can replace this with a toast notification library
    alert(message);
}

// ========================================
// Close modals on outside click
// ========================================
document.addEventListener('click', function (e) {
    if (e.target.classList.contains('modal-overlay')) {
        e.target.style.display = 'none';
    }

    if (e.target.id === 'lightboxModal') {
        closeLightbox();
    }
});
