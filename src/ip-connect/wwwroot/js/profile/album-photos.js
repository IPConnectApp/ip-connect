// Album Photo Manager - Enhanced with Drag & Drop Reordering
class AlbumPhotoManager {
    constructor(albumId, isOwnProfile) {
        console.log('=== AlbumPhotoManager Constructor ===');
        console.log('albumId:', albumId, 'type:', typeof albumId);
        console.log('isOwnProfile:', isOwnProfile, 'type:', typeof isOwnProfile);

        this.albumId = albumId;
        this.isOwnProfile = isOwnProfile;
        this.photos = [];
        this.isUploading = false;
        this.currentLightboxIndex = 0;
        this.draggedElement = null;
        this.draggedPhotoId = null;

        // Validate inputs
        if (!this.albumId) {
            console.error('❌ Album ID is missing!');
            this.showError('Configuration error: Album ID is missing');
            return;
        }

        this.init();
    }

    init() {
        console.log('📌 Initializing AlbumPhotoManager...');
        this.setupEventListeners();
        this.loadPhotos();
    }

    setupEventListeners() {
        // File input change
        const fileInput = document.getElementById('photoFileInput');
        if (fileInput) {
            fileInput.addEventListener('change', (e) => this.handleFileSelect(e));
            console.log('✅ File input listener attached');
        }

        // Browse files button
        const browseBtn = document.getElementById('browseFilesBtn');
        if (browseBtn) {
            browseBtn.addEventListener('click', () => fileInput?.click());
            console.log('✅ Browse button listener attached');
        }

        // Drag and drop area
        const dropArea = document.getElementById('photoDropArea');
        if (dropArea) {
            ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
                dropArea.addEventListener(eventName, (e) => this.preventDefaults(e), false);
            });

            ['dragenter', 'dragover'].forEach(eventName => {
                dropArea.addEventListener(eventName, () => this.highlight(dropArea), false);
            });

            ['dragleave', 'drop'].forEach(eventName => {
                dropArea.addEventListener(eventName, () => this.unhighlight(dropArea), false);
            });

            dropArea.addEventListener('drop', (e) => this.handleDrop(e), false);
            console.log('✅ Drag & drop listeners attached');
        }

        // Edit Album button
        const btnEdit = document.getElementById('btnEditAlbum');
        if (btnEdit) {
            btnEdit.addEventListener('click', () => {
                console.log('📝 Edit Album clicked');
                // Placeholder for edit functionality
                alert('Edit Album functionality will be implemented');
            });
        }

        // Delete Album button
        const btnDelete = document.getElementById('btnDeleteAlbum');
        if (btnDelete) {
            btnDelete.addEventListener('click', () => {
                console.log('🗑️ Delete Album clicked');
                if (confirm('Are you sure you want to delete this album? All photos will be lost.')) {
                    // Placeholder for delete functionality
                    alert('Delete Album functionality will be implemented');
                }
            });
        }

        // Lightbox controls
        this.setupLightbox();
    }

    setupLightbox() {
        const lightboxClose = document.getElementById('lightboxClose');
        const lightboxPrev = document.getElementById('lightboxPrev');
        const lightboxNext = document.getElementById('lightboxNext');
        const lightboxModal = document.getElementById('lightboxModal');

        if (lightboxClose) {
            lightboxClose.addEventListener('click', () => this.closeLightbox());
        }

        if (lightboxPrev) {
            lightboxPrev.addEventListener('click', () => this.navigateLightbox(-1));
        }

        if (lightboxNext) {
            lightboxNext.addEventListener('click', () => this.navigateLightbox(1));
        }

        if (lightboxModal) {
            lightboxModal.addEventListener('click', (e) => {
                if (e.target === lightboxModal) {
                    this.closeLightbox();
                }
            });
        }

        // Keyboard navigation
        document.addEventListener('keydown', (e) => {
            const modal = document.getElementById('lightboxModal');
            if (modal && modal.classList.contains('active')) {
                if (e.key === 'Escape') this.closeLightbox();
                if (e.key === 'ArrowLeft') this.navigateLightbox(-1);
                if (e.key === 'ArrowRight') this.navigateLightbox(1);
            }
        });
    }

    preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    highlight(element) {
        element.classList.add('drag-over');
    }

    unhighlight(element) {
        element.classList.remove('drag-over');
    }

    handleFileSelect(event) {
        const files = Array.from(event.target.files);
        console.log('📁 Files selected:', files.length);
        if (files.length > 0) {
            this.uploadPhotos(files);
        }
    }

    handleDrop(event) {
        const dt = event.dataTransfer;
        const files = Array.from(dt.files);
        console.log('📁 Files dropped:', files.length);
        if (files.length > 0) {
            this.uploadPhotos(files);
        }
    }

    async loadPhotos() {
        try {
            console.log(`🔄 Loading photos for album ${this.albumId}...`);
            this.showLoading(true);

            const response = await fetch(`/api/albums/${this.albumId}/photos`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            console.log('📥 Response status:', response.status);

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.error || `HTTP error! status: ${response.status}`);
            }

            this.photos = await response.json();
            console.log('✅ Photos loaded:', this.photos.length);
            this.renderPhotos();
            this.updatePhotoCount();

        } catch (error) {
            console.error('❌ Error loading photos:', error);
            this.showError('Failed to load photos. ' + error.message);
        } finally {
            this.showLoading(false);
        }
    }

    async uploadPhotos(files) {
        if (this.isUploading) {
            this.showError('Upload already in progress');
            return;
        }

        if (!this.isOwnProfile) {
            this.showError('You can only upload photos to your own albums');
            return;
        }

        const validFiles = this.validateFiles(files);
        if (validFiles.length === 0) {
            return;
        }

        this.isUploading = true;
        this.showUploadProgress(true);

        try {
            console.log(`📤 Uploading ${validFiles.length} files to album ${this.albumId}...`);

            const formData = new FormData();
            validFiles.forEach(file => {
                formData.append('files', file);
            });

            const response = await fetch(`/api/albums/${this.albumId}/photos`, {
                method: 'POST',
                body: formData
            });

            console.log('📥 Upload response status:', response.status);
            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.error || 'Upload failed');
            }

            console.log('✅ Upload successful:', result);
            this.showSuccess(result.message || 'Photos uploaded successfully');

            await this.loadPhotos();

            const fileInput = document.getElementById('photoFileInput');
            if (fileInput) {
                fileInput.value = '';
            }

        } catch (error) {
            console.error('❌ Upload error:', error);
            this.showError(error.message || 'Failed to upload photos');
        } finally {
            this.isUploading = false;
            this.showUploadProgress(false);
        }
    }

    validateFiles(files) {
        const validExtensions = ['jpg', 'jpeg', 'png', 'gif', 'webp'];
        const maxFileSize = 10 * 1024 * 1024; // 10MB
        const validFiles = [];

        for (const file of files) {
            const extension = file.name.split('.').pop().toLowerCase();
            if (!validExtensions.includes(extension)) {
                this.showError(`Invalid file type: ${file.name}. Allowed: ${validExtensions.join(', ')}`);
                continue;
            }

            if (file.size > maxFileSize) {
                this.showError(`File too large: ${file.name}. Max size: 10MB`);
                continue;
            }

            validFiles.push(file);
        }

        console.log(`✅ Validated ${validFiles.length} of ${files.length} files`);
        return validFiles;
    }

    async deletePhoto(photoId) {
        if (!this.isOwnProfile) {
            this.showError('You can only delete photos from your own albums');
            return;
        }

        if (!confirm('Are you sure you want to delete this photo?')) {
            return;
        }

        try {
            console.log(`🗑️ Deleting photo ${photoId}...`);

            const response = await fetch(`/api/albums/photo/${photoId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.error || 'Delete failed');
            }

            console.log('✅ Photo deleted successfully');
            this.showSuccess('Photo deleted successfully');

            this.photos = this.photos.filter(p => p.id !== photoId);
            this.renderPhotos();
            this.updatePhotoCount();

        } catch (error) {
            console.error('❌ Delete error:', error);
            this.showError(error.message || 'Failed to delete photo');
        }
    }

    renderPhotos() {
        const container = document.getElementById('photoGallery');
        if (!container) {
            console.error('❌ Photo gallery container not found');
            return;
        }

        console.log(`🎨 Rendering ${this.photos.length} photos...`);

        if (this.photos.length === 0) {
            container.innerHTML = `
                <div class="empty-gallery">
                    <i class="far fa-image empty-gallery-icon"></i>
                    <h3>No photos yet</h3>
                    <p>${this.isOwnProfile ? 'Upload your first photo to bring this album to life!' : 'This album is empty.'}</p>
                </div>
            `;
            return;
        }

        container.innerHTML = this.photos.map((photo, index) => `
            <div class="photo-card" 
                 data-photo-id="${photo.id}" 
                 data-index="${index}"
                 draggable="${this.isOwnProfile}">
                <img src="${this.escapeHtml(photo.url)}" 
                     alt="Photo ${index + 1}"
                     loading="lazy"
                     onclick="albumManager.openLightbox(${index})">
                ${this.isOwnProfile ? `
                    <div class="photo-overlay">
                        <button class="btn-photo-delete" 
                                onclick="event.stopPropagation(); albumManager.deletePhoto(${photo.id})"
                                title="Delete photo">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                ` : ''}
            </div>
        `).join('');

        // Setup drag and drop for reordering (only for owner)
        if (this.isOwnProfile) {
            this.setupPhotoReordering();
        }

        console.log('✅ Photos rendered');
    }

    setupPhotoReordering() {
        const photoCards = document.querySelectorAll('.photo-card');

        photoCards.forEach(card => {
            card.addEventListener('dragstart', (e) => this.handleDragStart(e));
            card.addEventListener('dragover', (e) => this.handleDragOver(e));
            card.addEventListener('drop', (e) => this.handlePhotoReorder(e));
            card.addEventListener('dragend', (e) => this.handleDragEnd(e));
            card.addEventListener('dragenter', (e) => this.handleDragEnter(e));
            card.addEventListener('dragleave', (e) => this.handleDragLeave(e));
        });
    }

    handleDragStart(e) {
        this.draggedElement = e.currentTarget;
        this.draggedPhotoId = parseInt(e.currentTarget.dataset.photoId);
        e.currentTarget.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/html', e.currentTarget.innerHTML);
    }

    handleDragOver(e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        return false;
    }

    handleDragEnter(e) {
        if (e.currentTarget !== this.draggedElement) {
            e.currentTarget.classList.add('drag-over');
        }
    }

    handleDragLeave(e) {
        e.currentTarget.classList.remove('drag-over');
    }

    handleDragEnd(e) {
        e.currentTarget.classList.remove('dragging');

        // Remove all drag-over classes
        document.querySelectorAll('.photo-card').forEach(card => {
            card.classList.remove('drag-over');
        });
    }

    async handlePhotoReorder(e) {
        e.preventDefault();
        e.stopPropagation();

        const dropTarget = e.currentTarget;
        dropTarget.classList.remove('drag-over');

        if (this.draggedElement === dropTarget) {
            return;
        }

        const draggedIndex = parseInt(this.draggedElement.dataset.index);
        const targetIndex = parseInt(dropTarget.dataset.index);

        // Reorder in local array
        const [movedPhoto] = this.photos.splice(draggedIndex, 1);
        this.photos.splice(targetIndex, 0, movedPhoto);

        // Re-render immediately for visual feedback
        this.renderPhotos();

        // Send reorder request to backend
        await this.savePhotoOrder();
    }

    async savePhotoOrder() {
        try {
            const reorderData = this.photos.map((photo, index) => ({
                photoId: photo.id,
                newOrder: index
            }));

            const response = await fetch('/api/albums/photos/reorder', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(reorderData)
            });

            if (!response.ok) {
                throw new Error('Failed to save photo order');
            }

            console.log('✅ Photo order saved');
        } catch (error) {
            console.error('❌ Error saving photo order:', error);
            this.showError('Failed to save photo order');
            // Reload to get correct order from server
            await this.loadPhotos();
        }
    }

    updatePhotoCount() {
        const badge = document.getElementById('photoCountBadge');
        if (badge) {
            const count = this.photos.length;
            badge.textContent = `${count} photo${count !== 1 ? 's' : ''}`;
        }
    }

    openLightbox(index) {
        this.currentLightboxIndex = index;
        const modal = document.getElementById('lightboxModal');
        const img = document.getElementById('lightboxImage');
        const counter = document.getElementById('lightboxCounter');

        if (modal && img) {
            img.src = this.photos[index].url;
            counter.textContent = `${index + 1} / ${this.photos.length}`;
            modal.classList.add('active');
        }
    }

    closeLightbox() {
        const modal = document.getElementById('lightboxModal');
        if (modal) {
            modal.classList.remove('active');
        }
    }

    navigateLightbox(direction) {
        this.currentLightboxIndex += direction;

        if (this.currentLightboxIndex < 0) {
            this.currentLightboxIndex = this.photos.length - 1;
        } else if (this.currentLightboxIndex >= this.photos.length) {
            this.currentLightboxIndex = 0;
        }

        this.openLightbox(this.currentLightboxIndex);
    }

    showLoading(show) {
        const indicator = document.getElementById('loadingIndicator');
        if (indicator) {
            indicator.style.display = show ? 'flex' : 'none';
        }
    }

    showUploadProgress(show) {
        const progress = document.getElementById('uploadProgress');
        if (progress) {
            progress.style.display = show ? 'block' : 'none';
        }
    }

    showSuccess(message) {
        console.log('✅', message);
        this.showToast(message, 'success');
    }

    showError(message) {
        console.error('❌', message);
        this.showToast(message, 'danger');
    }

    showToast(message, type = 'info') {
        let toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toastContainer';
            toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        const toastId = `toast-${Date.now()}`;
        const toast = document.createElement('div');
        toast.id = toastId;
        toast.className = `toast align-items-center text-white bg-${type} border-0`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    ${this.escapeHtml(message)}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        toastContainer.appendChild(toast);

        const bsToast = new bootstrap.Toast(toast, { autohide: true, delay: 5000 });
        bsToast.show();

        toast.addEventListener('hidden.bs.toast', () => {
            toast.remove();
        });
    }

    escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return String(text).replace(/[&<>"']/g, m => map[m]);
    }
}

// Global instance
let albumManager;

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    console.log('🚀 DOM Content Loaded - Initializing album manager...');

    const albumIdElement = document.getElementById('albumId');
    const isOwnProfileElement = document.getElementById('isOwnProfile');

    const albumId = albumIdElement?.value;
    const isOwnProfile = isOwnProfileElement?.value === 'true';

    console.log('Parsed albumId:', albumId);
    console.log('Parsed isOwnProfile:', isOwnProfile);

    if (albumId) {
        albumManager = new AlbumPhotoManager(albumId, isOwnProfile);
        console.log('✅ Album manager initialized');
    } else {
        console.error('❌ Cannot initialize - albumId is missing!');
    }
});