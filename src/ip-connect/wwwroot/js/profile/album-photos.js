// Album Photo Manager - Modern ES6+ Implementation
class AlbumPhotoManager {
    constructor(albumId, isOwnProfile) {
        console.log('=== AlbumPhotoManager Constructor ===');
        console.log('albumId:', albumId, 'type:', typeof albumId);
        console.log('isOwnProfile:', isOwnProfile, 'type:', typeof isOwnProfile);

        this.albumId = albumId;
        this.isOwnProfile = isOwnProfile;
        this.photos = [];
        this.isUploading = false;

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
        } else {
            console.warn('⚠️ File input not found');
        }

        // Browse files button
        const browseBtn = document.getElementById('browseFilesBtn');
        if (browseBtn) {
            browseBtn.addEventListener('click', () => fileInput?.click());
            console.log('✅ Browse button listener attached');
        } else {
            console.warn('⚠️ Browse button not found');
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
        } else {
            console.warn('⚠️ Drop area not found (user may not be owner)');
        }
    }

    preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    highlight(element) {
        element.classList.add('border-primary', 'bg-primary-subtle');
    }

    unhighlight(element) {
        element.classList.remove('border-primary', 'bg-primary-subtle');
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

        // Validate files
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

            // Reload photos
            await this.loadPhotos();

            // Reset file input
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
            // Check file type
            const extension = file.name.split('.').pop().toLowerCase();
            if (!validExtensions.includes(extension)) {
                this.showError(`Invalid file type: ${file.name}. Allowed: ${validExtensions.join(', ')}`);
                continue;
            }

            // Check file size
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

            // Remove from local array
            this.photos = this.photos.filter(p => p.id !== photoId);
            this.renderPhotos();

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
                <div class="col-12">
                    <div class="text-center py-5">
                        <i class="fas fa-image text-muted" style="font-size: 4rem;"></i>
                        <p class="text-muted mt-3">No Photos Yet</p>
                        ${this.isOwnProfile ? '<p class="text-muted">Upload your first photo to this album!</p>' : ''}
                    </div>
                </div>
            `;
            return;
        }

        container.innerHTML = this.photos.map(photo => `
            <div class="col-6 col-md-4 col-lg-3 mb-3" data-photo-id="${photo.id}">
                <div class="card photo-card">
                    <img src="${this.escapeHtml(photo.url)}" 
                         class="card-img-top" 
                         alt="Photo"
                         loading="lazy"
                         style="aspect-ratio: 1; object-fit: cover; cursor: pointer;"
                         onclick="albumManager.openPhotoModal('${this.escapeHtml(photo.url)}')">
                    ${this.isOwnProfile ? `
                        <div class="card-body p-2">
                            <button class="btn btn-sm btn-danger w-100" 
                                    onclick="albumManager.deletePhoto(${photo.id})">
                                <i class="fas fa-trash"></i> Delete
                            </button>
                        </div>
                    ` : ''}
                </div>
            </div>
        `).join('');

        console.log('✅ Photos rendered');
    }

    openPhotoModal(photoUrl) {
        // Create modal dynamically if it doesn't exist
        let modal = document.getElementById('photoModal');
        if (!modal) {
            modal = document.createElement('div');
            modal.id = 'photoModal';
            modal.className = 'modal fade';
            modal.innerHTML = `
                <div class="modal-dialog modal-dialog-centered modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body p-0">
                            <img id="photoModalImage" src="" class="w-100" alt="Photo">
                        </div>
                    </div>
                </div>
            `;
            document.body.appendChild(modal);
        }

        const img = document.getElementById('photoModalImage');
        img.src = photoUrl;

        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();
    }

    showLoading(show) {
        const indicator = document.getElementById('loadingIndicator');
        if (indicator) {
            indicator.style.display = show ? 'block' : 'none';
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
        // Create toast container if it doesn't exist
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

        // Remove toast element after it's hidden
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

    console.log('albumId element:', albumIdElement);
    console.log('isOwnProfile element:', isOwnProfileElement);

    const albumId = albumIdElement?.value;
    const isOwnProfile = isOwnProfileElement?.value === 'True' || isOwnProfileElement?.value === 'true';

    console.log('Parsed albumId:', albumId);
    console.log('Parsed isOwnProfile:', isOwnProfile);

    if (albumId) {
        albumManager = new AlbumPhotoManager(albumId, isOwnProfile);
        console.log('✅ Album manager initialized');
    } else {
        console.error('❌ Cannot initialize - albumId is missing!');
        console.log('Available elements:');
        console.log('- albumId element:', albumIdElement);
        console.log('- albumId value:', albumIdElement?.value);
    }
});