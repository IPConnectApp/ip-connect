document.addEventListener('DOMContentLoaded', function () {
    // Only run if albumsConfig exists (we're on the Photos page)
    if (!window.albumsConfig) return;

    loadAlbums();
});

// Load albums from API
async function loadAlbums() {
    try {
        const response = await fetch(`/api/album/user/${window.albumsConfig.profileUserId}`);

        if (!response.ok) {
            console.error('Failed to load albums:', response.status);
            return;
        }

        const albums = await response.json();
        renderAlbums(albums);
    }
    catch (error) {
        console.error('Error loading albums:', error);
    }
}

// Render albums or show empty state
function renderAlbums(albums) {
    const albumsGrid = document.getElementById('albumsGrid');
    const emptyAlbums = document.getElementById('emptyAlbums');

    if (albums.length === 0)
    {
        // No albums - show empty state
        albumsGrid.style.display = 'none';
        emptyAlbums.style.display = 'block';
        return;
    }

    // Has albums - show grid, hide empty state
    albumsGrid.style.display = 'grid';
    emptyAlbums.style.display = 'none';

    // Clear existing cards
    albumsGrid.innerHTML = '';

    // Render each album card
    albums.forEach(album => {
        albumsGrid.appendChild(createAlbumCard(album));
    });
}

//Create album card element
function createAlbumCard(album) {
    const card = document.createElement('div');
    card.className = 'album-card';

    const createdDate = new Date(album.createdAt).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long'
    });

    card.innerHTML = `
        <div class="album-card-cover">
            <i class="fas fa-images"></i>
        </div>
        <div class="album-card-info">
            <h4 class="album-card-name">${escapeHtml(album.name)}</h4>
            ${album.description ? `<p class="album-card-description">${escapeHtml(album.description)}</p>` : ''}
            <div class="album-card-meta">
                <span class="album-card-photos">${album.photoCount} photos</span>
                <span class="album-card-date">${createdDate}</span>
            </div>
        </div>
        ${window.albumsConfig.isOwnProfile ? createAlbumActions(album.id, album.name) : ''}
    `;

    return card;
}

//Create action buttons (Edit, Delete) - only for owner
function createAlbumActions(albumId, albumName) {
    return `
        <div class="album-card-actions">
            <button class="btn-album-edit" onclick="openEditAlbumModal(${albumId})">
                <i class="fas fa-edit"></i>
            </button>
            <button class="btn-album-delete-icon" onclick="openDeleteAlbumModal(${albumId}, '${escapeHtml(albumName)}')">
                <i class="fas fa-trash-alt"></i>
            </button>
        </div>
    `;
}


//Create Album
function openCreateAlbumModal() {
    document.getElementById('albumName').value = '';
    document.getElementById('albumDescription').value = '';
    hideError('albumNameError');
    document.getElementById('createAlbumModal').style.display = 'flex';
}

function closeCreateAlbumModal() {
    document.getElementById('createAlbumModal').style.display = 'none';
}

async function createAlbum() {
    const name = document.getElementById('albumName').value.trim();
    const description = document.getElementById('albumDescription').value.trim();

    // Validate
    if (!name) {
        showError('albumNameError', 'Album name is required');
        return;
    }

    try {
        const response = await fetch('/api/album', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiforgeryToken()
            },
            body: JSON.stringify({ name, description })
        });

        if (!response.ok) {
            const error = await response.json();
            showError('albumNameError', error.error || 'Failed to create album');
            return;
        }

        // Success - close modal and reload albums
        closeCreateAlbumModal();
        loadAlbums();
    }
    catch (error) {
        console.error('Error creating album:', error);
        showError('albumNameError', 'Something went wrong');
    }
}

//Edit Album
function openEditAlbumModal(albumId) {
    //Find the album card to get current values
    const cards = document.querySelectorAll('.album-card');
    
    cards.forEach(card => {
        const nameEl = card.querySelector('.album-card-name');
        const descEl = card.querySelector('.album-card-description');
        const editBtn = card.querySelector('.btn-album-edit');

        if (editBtn && editBtn.getAttribute('onclick').includes(albumId)) {
            document.getElementById('editAlbumId').value = albumId;
            document.getElementById('editAlbumName').value = nameEl?.textContent || '';
            document.getElementById('editAlbumDescription').value = descEl?.textContent || '';
        }
    });

    hideError('editAlbumNameError');
    document.getElementById('editAlbumModal').style.display = 'flex';
}

function closeEditAlbumModal() {
    document.getElementById('editAlbumModal').style.display = 'none';
}

async function updateAlbum() {
    const albumId = document.getElementById('editAlbumId').value;
    const name = document.getElementById('editAlbumName').value.trim();
    const description = document.getElementById('editAlbumDescription').value.trim();

    //Validate
    if (!name) {
        showError('editAlbumNameError', 'Album name is required');
        return;
    }

    try {
        const response = await fetch(`/api/album/${albumId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiforgeryToken()
            },
            body: JSON.stringify({ name, description })
        });

        if (!response.ok) {
            const error = await response.json();
            showError('editAlbumNameError', error.error || 'Failed to update album');
            return;
        }

        // Success - close modal and reload albums
        closeEditAlbumModal();
        loadAlbums();
    }
    catch (error) {
        console.error('Error updating album:', error);
        showError('editAlbumNameError', 'Something went wrong');
    }
}

//Delete album
let albumToDelete = null;

function openDeleteAlbumModal(albumId, albumName) {
    albumToDelete = albumId;
    document.getElementById('deleteAlbumName').textContent = albumName;
    document.getElementById('deleteAlbumModal').style.display = 'flex';
}

function closeDeleteAlbumModal() {
    albumToDelete = null;
    document.getElementById('deleteAlbumModal').style.display = 'none';
}

async function confirmDeleteAlbum() {
    if (!albumToDelete) return;

    try {
        const response = await fetch(`/api/album/${albumToDelete}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': getAntiforgeryToken()
            }
        });

        if (!response.ok) {
            console.error('Failed to delete album:', response.status);
            return;
        }

        //Success - close modal and reload albums
        closeDeleteAlbumModal();
        loadAlbums();
    }
    catch (error) {
        console.error('Error deleting album:', error);
    }
}

//Utility functions
function showError(elementId, message) {
    const errorEl = document.getElementById(elementId);
    if (errorEl) {
        errorEl.textContent = message;
        errorEl.style.display = 'block';
    }
}

function hideError(elementId) {
    const errorEl = document.getElementById(elementId);
    if (errorEl) {
        errorEl.style.display = 'none';
    }
}

function getAntiforgeryToken() {
    const token = document.querySelector('meta[name="_csrf"]')?.getAttribute('content')
        || document.querySelector('input[name="__RequestVerificationToken"]')?.value
        || '';
    return token;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}