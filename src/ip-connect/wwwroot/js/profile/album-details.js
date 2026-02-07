document.addEventListener('DOMContentLoaded', function () {
    loadAlbumInfo();
    loadPhotos();
    setupUpload();
});

const config = window.albumConfig;
let currentOpenPhotoId = null;

// 1. Load Album Meta Info
async function loadAlbumInfo() {
    try {
        const response = await fetch(`/api/album/${config.albumId}`);
        if (!response.ok) throw new Error('Failed to load album info');

        const album = await response.json();

        document.getElementById('albumName').textContent = album.name;
        document.getElementById('albumDescription').textContent = album.description || '';

        const date = new Date(album.createdAt).toLocaleDateString('en-US', {
            year: 'numeric', month: 'long', day: 'numeric'
        });
        document.getElementById('albumDate').textContent = `Created ${date}`;
    } catch (error) {
        console.error(error);
        alert('Could not load album details.');
    }
}

// 2. Load Photos
async function loadPhotos() {
    const grid = document.getElementById('photosGrid');

    try {
        // NOTE: You need to implement this endpoint in C#
        const response = await fetch(`/api/photo/album/${config.albumId}`);

        if (!response.ok) {
            if (response.status === 404) {
                renderEmptyState();
                return;
            }
            throw new Error('Failed to load photos');
        }

        const photos = await response.json();
        renderPhotos(photos);

    } catch (error) {
        console.error(error);
        grid.innerHTML = '<div class="album-empty">Failed to load photos.</div>';
    }
}

function renderPhotos(photos) {
    const grid = document.getElementById('photosGrid');
    grid.innerHTML = '';

    if (photos.length === 0) {
        renderEmptyState();
        return;
    }

    photos.forEach(photo => {
        const card = document.createElement('div');
        card.className = 'photo-card';
        card.onclick = () => openLightbox(photo.url, photo.id);

        const img = document.createElement('img');
        img.src = photo.thumbnailUrl || photo.url; // Use thumb if available
        img.alt = "Album photo";
        img.loading = "lazy";

        card.appendChild(img);
        grid.appendChild(card);
    });
}

function renderEmptyState() {
    const grid = document.getElementById('photosGrid');
    grid.innerHTML = `
        <div class="album-empty">
            <i class="fas fa-images" style="font-size: 48px; margin-bottom: 15px;"></i>
            <h3>No photos yet</h3>
            <p>${config.isOwnProfile ? 'Upload photos to get started!' : 'This album is empty.'}</p>
        </div>
    `;
}

// 3. Upload Logic
function setupUpload() {
    const dropZone = document.getElementById('dropZone');
    const input = document.getElementById('photoInput');

    if (!dropZone || !input) return; // Not owner

    // Click trigger
    dropZone.addEventListener('click', () => input.click());

    // Drag & Drop events
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => dropZone.classList.add('drag-over'), false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => dropZone.classList.remove('drag-over'), false);
    });

    dropZone.addEventListener('drop', handleDrop, false);
    input.addEventListener('change', handleFiles, false);
}

function handleDrop(e) {
    const dt = e.dataTransfer;
    const files = dt.files;
    handleFiles({ target: { files: files } });
}

async function handleFiles(e) {
    const files = [...e.target.files];
    if (files.length === 0) return;

    uploadFiles(files);
}

async function uploadFiles(files) {
    const progressBar = document.getElementById('progressBar');
    const progressContainer = document.getElementById('uploadProgress');

    progressContainer.style.display = 'block';
    progressBar.style.width = '0%';

    const formData = new FormData();
    files.forEach(file => {
        formData.append('files', file);
    });
    formData.append('albumId', config.albumId);

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        // NOTE: You need to implement this endpoint
        const response = await fetch('/api/photo/upload', {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': token || ''
            }
        });

        progressBar.style.width = '100%';

        if (!response.ok) throw new Error('Upload failed');

        // Reload grid to show new photos
        setTimeout(() => {
            progressContainer.style.display = 'none';
            loadPhotos();
        }, 500);

    } catch (error) {
        console.error(error);
        alert('Error uploading photos.');
        progressContainer.style.display = 'none';
    }
}

// 4. Lightbox Logic
function openLightbox(url, photoId) {
    const modal = document.getElementById('lightboxModal');
    const img = document.getElementById('lightboxImage');
    const deleteBtn = document.getElementById('lightboxDeleteBtn');

    img.src = url;
    currentOpenPhotoId = photoId;

    modal.style.display = 'flex';

    if (deleteBtn) {
        deleteBtn.onclick = () => deletePhoto(photoId);
    }
}

function closeLightbox() {
    document.getElementById('lightboxModal').style.display = 'none';
    currentOpenPhotoId = null;
}

// Close on outside click
window.onclick = function (event) {
    const modal = document.getElementById('lightboxModal');
    if (event.target == modal) {
        closeLightbox();
    }
}

// 5. Delete Logic
async function deletePhoto(photoId) {
    if (!confirm("Are you sure you want to delete this photo?")) return;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        // NOTE: You need to implement this endpoint
        const response = await fetch(`/api/photo/${photoId}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': token || ''
            }
        });

        if (!response.ok) throw new Error('Delete failed');

        closeLightbox();
        loadPhotos(); // Refresh grid

    } catch (error) {
        console.error(error);
        alert('Failed to delete photo.');
    }
}