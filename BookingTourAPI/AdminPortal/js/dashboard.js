let rolesModal, destModal, tourPackageModal, scheduleModal;
let currentWarningDepartureId = null;
let allRoles = [];

// --- GLOBAL VARIABLES FOR CHAT ---
let chatAdminConnection = null;
let currentChatUserId = null;
let chatHistory = {};

// --- 1. KHỞI TẠO KẾT NỐI SIGNALR (ADMIN) ---
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/adminHub", { accessTokenFactory: () => localStorage.getItem("adminToken") })
    .withAutomaticReconnect()
    .build();

document.addEventListener('DOMContentLoaded', () => {
    const rolesModalEl = document.getElementById('edit-roles-modal');
    const destModalEl = document.getElementById('edit-destination-modal');
    const tourPackageModalEl = document.getElementById('tour-package-modal');
    const scheduleModalEl = document.getElementById('tour-schedule-modal');
    const warningModalEl = document.getElementById('warningModal');

    if (rolesModalEl) rolesModal = new bootstrap.Modal(rolesModalEl);
    if (destModalEl) destModal = new bootstrap.Modal(destModalEl);
    if (tourPackageModalEl) tourPackageModal = new bootstrap.Modal(tourPackageModalEl);
    if (scheduleModalEl) scheduleModal = new bootstrap.Modal(scheduleModalEl);
    
    // Khởi tạo Modal Cảnh báo
    window.warningModalObj = warningModalEl ? new bootstrap.Modal(warningModalEl) : null;

    // --- CẤU HÌNH NÚT XUẤT EXCEL (.XLSX) ---
    const btnExport = document.getElementById('btn-export-pdf');
    if (btnExport) {
        // Đổi giao diện nút sang màu xanh Excel
        btnExport.innerHTML = '<i class="bi bi-file-earmark-excel-fill"></i> Xuất Excel (.xlsx)';
        btnExport.classList.remove('btn-secondary');
        btnExport.classList.add('btn-success'); 
        
        // Gọi hàm exportToExcel MỚI
        btnExport.addEventListener('click', exportToExcel);
    }
    // ---------------------------------------------

    const logoutBtn = document.getElementById('logout-button');
    if(logoutBtn) {
        logoutBtn.addEventListener('click', (e) => {
            e.preventDefault();
            localStorage.removeItem('adminToken');
            localStorage.removeItem('adminEmail');
            localStorage.removeItem('adminChatHistory');
            window.location.href = '/login.html'; 
        });
    }

    const sidebarLinks = document.querySelectorAll('#sidebarMenu .nav-link');
    sidebarLinks.forEach(link => {
        link.addEventListener('click', function (event) {
            sidebarLinks.forEach(l => l.classList.remove('active'));
            this.classList.add('active');
            document.querySelectorAll('.tab-pane').forEach(tab => tab.classList.remove('show', 'active'));
            const targetPane = document.querySelector(this.getAttribute('href'));
            if(targetPane) targetPane.classList.add('show', 'active');
        });
    });

    const firstTabLink = document.querySelector('#sidebarMenu .nav-link[href="#section-dashboard"]');
    if (firstTabLink) firstTabLink.click();

    const btnLoadReviews = document.getElementById('btn-load-reviews');
    if (btnLoadReviews) {
        btnLoadReviews.addEventListener('click', loadReviews);
    }

    const btnLockTour = document.getElementById('btn-lock-tour-now');
    if(btnLockTour) {
        btnLockTour.addEventListener('click', () => {
            lockTourNow(currentWarningDepartureId);
        });
    }

    checkAuthAndLoadData();
    setupTableActions();
    setupCmsModalEvents();
    setupRolesModalEvents();
    setupTourPackageModalEvents();
});


function getAuthHeaders() {
    const token = localStorage.getItem('adminToken');
    const headers = { 'Content-Type': 'application/json' };
    if (token) {
        headers['Authorization'] = 'Bearer ' + token;
    } else {
        console.warn("DEBUG: No adminToken found in localStorage!");
    }
    return headers;
}

// --- SIGNALR LISTENERS ---
connection.on("UserLockStatusChanged", (userId, isLocked) => {
    updateUserRowStatus(userId, isLocked);
});

connection.on("UserDeleted", (userId) => {
    const userRow = document.querySelector(`#table-users tbody tr[data-user-id="${userId}"]`);
    if (userRow) userRow.remove();
});

connection.on("OrderStatusChanged", (orderType, orderId, newStatus) => {
    updateOrderStatusInTable(orderType, orderId, newStatus);
    if (orderType === 'tour') loadTourWarnings();
});

connection.on("ReceiveTourWarning", (message, departureId) => {
    const toastEl = document.getElementById('adminToast');
    const toastBody = document.getElementById('toast-message');
    const btnView = document.getElementById('btn-view-warning-detail');
    
    if (toastEl && toastBody) {
        toastBody.textContent = message;
        currentWarningDepartureId = departureId;
        const toast = new bootstrap.Toast(toastEl);
        toast.show();

        if(btnView) {
            const newBtn = btnView.cloneNode(true);
            btnView.parentNode.replaceChild(newBtn, btnView);
            newBtn.addEventListener('click', () => loadWarningDetails(departureId));
        }
    }
    loadTourWarnings();
});

// --- HELPER FUNCTIONS ---
function updateUserRowStatus(userId, isLocked) {
    const userRow = document.querySelector(`#table-users tbody tr[data-user-id="${userId}"]`);
    if (!userRow) return;
    const statusCell = userRow.cells[3];
    const actionCell = userRow.cells[4];
    const lockUnlockButton = actionCell.querySelector('.lock-unlock-btn');
    const statusText = isLocked ? 'Bị khóa' : 'Hoạt động';
    const statusClass = isLocked ? 'status-locked' : 'status-active';
    const newAction = isLocked ? 'unlock' : 'lock';
    const newButtonText = isLocked ? 'Mở khóa' : 'Khóa';
    statusCell.innerHTML = `<span class="status ${statusClass}">${statusText}</span>`;
    if (lockUnlockButton) {
        lockUnlockButton.textContent = newButtonText;
        lockUnlockButton.setAttribute('data-action', newAction);
        if(isLocked) {
            lockUnlockButton.classList.remove('btn-danger');
            lockUnlockButton.classList.add('btn-success');
        } else {
            lockUnlockButton.classList.remove('btn-success');
            lockUnlockButton.classList.add('btn-danger');
        }
    }
}

function updateOrderStatusInTable(orderType, orderId, newStatus) {
    const row = document.querySelector(`tr[data-order-type="${orderType}"][data-order-id="${orderId}"]`);
    if (!row) return;
    let statusCellIndex = -1;
    if (orderType === 'flight') statusCellIndex = 5;
    else if (orderType === 'tour') statusCellIndex = 4;
    
    if (statusCellIndex !== -1 && row.cells[statusCellIndex]) {
        const statusInfo = formatStatus(newStatus);
        row.cells[statusCellIndex].innerHTML = `<span class="order-status ${statusInfo.class}">${statusInfo.text}</span>`;
    }
    const actionCell = row.cells[row.cells.length - 1];
    const confirmButton = actionCell.querySelector('.confirm-btn');
    if (confirmButton && newStatus?.toUpperCase().includes('CONFIRMED')) {
        confirmButton.remove();
    }
}

function formatStatus(status) {
    const upperStatus = status ? status.toUpperCase() : 'UNKNOWN';
    switch (upperStatus) {
        case 'PENDING_CONFIRMATION': return { text: 'Chờ xác nhận', class: 'status-pending_confirmation' };
        case 'CONFIRMED_FULL': return { text: 'Đã thanh toán (100%)', class: 'status-confirmed' }; 
        case 'CONFIRMED_DEPOSIT': return { text: 'Đã cọc (50%)', class: 'status-pending_confirmation' };
        case 'CONFIRMED': case 'DEMO_CONFIRMED': return { text: 'Đã xác nhận', class: 'status-confirmed' };
        case 'CANCELLED': return { text: 'Đã hủy', class: 'status-cancelled' };
        default: return { text: status || 'Không rõ', class: '' };
    }
}

async function checkAuthAndLoadData() {
    try {
        await loadAllRoles();
        const response = await fetch('/api/admin/dashboard-stats', { headers: getAuthHeaders() });
        if (response.ok) {
            const stats = await response.json();
            displayStats(stats);
            loadFlightOrders();
            loadActivityBookings();
            loadTourWarnings();
            loadMonthlyChart(stats);
            loadUsers();
            loadTopDestinations();
            loadTourPackages();
            loadReviews();
            startSignalRConnection();
            initChatSystem();
        } else if (response.status === 401 || response.status === 403) {
            window.location.href = '/login.html';
        }
    } catch (error) {
        console.error('Critical error:', error);
    }
}

async function loadAllRoles() {
    try {
        const response = await fetch('/api/admin/users/roles', { headers: getAuthHeaders() });
        if (response.ok) allRoles = await response.json();
    } catch (error) { allRoles = []; }
}

function displayStats(stats) {
    document.getElementById('stat-total-revenue').textContent = `${(stats.totalRevenue || 0).toLocaleString()} VND`;
    document.getElementById('stat-total-flights').textContent = stats.totalFlightOrders || 0;
    document.getElementById('stat-total-activities').textContent = stats.totalActivityBookings || 0;
}

// --- LOAD DATA TABLES ---
async function loadFlightOrders() {
    const tbody = document.querySelector('#table-flights tbody');
    tbody.innerHTML = '<tr><td colspan="7">Đang tải...</td></tr>';
    try {
        const response = await fetch('/api/admin/flight-orders', { headers: getAuthHeaders() });
        const orders = await response.json();
        tbody.innerHTML = '';
        if (!orders.length) { tbody.innerHTML = '<tr><td colspan="7">Không có đơn chuyến bay.</td></tr>'; return; }
        orders.forEach(o => {
            const statusInfo = formatStatus(o.status);
            const confirmBtn = o.status === 'PENDING_CONFIRMATION' 
                ? `<button class="action-btn btn btn-sm btn-success confirm-btn" data-order-type="flight" data-order-id="${o.orderId}">Xác nhận</button>` : '';
            const deleteBtn = `<button class="action-btn btn btn-sm btn-danger delete-order-btn" data-order-type="flight" data-order-id="${o.orderId}" title="Xóa đơn này">X</button>`;

            const row = `<tr data-order-type="flight" data-order-id="${o.orderId}">
                <td>${o.orderId}</td>
                <td>${new Date(o.createdAt).toLocaleString('vi-VN')}</td>
                <td>${o.travelerName}</td>
                <td>${o.travelerEmail}</td>
                <td>${(o.totalPrice || 0).toLocaleString('vi-VN')} ${o.currency || ''}</td>
                <td><span class="order-status ${statusInfo.class}">${statusInfo.text}</span></td>
                <td>${confirmBtn} ${deleteBtn}</td>
            </tr>`;             
            tbody.innerHTML += row;
        });
    } catch (error) { tbody.innerHTML = `<tr><td colspan="7" class="error-message">Lỗi tải đơn CB</td></tr>`; }
}

async function loadActivityBookings() {
    const tbody = document.querySelector('#table-tours tbody');
    tbody.innerHTML = '<tr><td colspan="7">Đang tải...</td></tr>'; 
    try {
        const response = await fetch('/api/admin/activity-bookings', { headers: getAuthHeaders() });
        const bookings = await response.json();
        tbody.innerHTML = '';
        if (!bookings.length) { tbody.innerHTML = '<tr><td colspan="7">Không có đơn tour.</td></tr>'; return; }
        
        bookings.forEach(b => {
            const statusInfo = formatStatus(b.status);
            
            const confirmBtn = b.status === 'PENDING_CONFIRMATION' 
                ? `<button class="action-btn btn btn-sm btn-success confirm-btn" data-order-type="tour" data-order-id="${b.amadeusOrderId}">Xác nhận</button>` 
                : '';
            const deleteBtn = `<button class="action-btn btn btn-sm btn-danger delete-order-btn" data-order-type="tour" data-order-id="${b.amadeusOrderId}" title="Xóa đơn này">X</button>`;

            // Nút Xem DS Khách
            const viewTripBtn = b.tourDepartureId 
                ? `<button class="action-btn btn btn-sm btn-info view-trip-btn" data-departure-id="${b.tourDepartureId}" title="Xem danh sách khách"><i class="bi bi-people-fill"></i> DS Khách</button>`
                : '';

            const row = `<tr data-order-type="tour" data-order-id="${b.amadeusOrderId}">
                <td>${b.amadeusOrderId}</td>
                <td>
                    <div class="fw-bold">${b.activityName}</div>
                    <div class="small text-muted">Khởi hành: ${new Date(b.startDate).toLocaleDateString('vi-VN')}</div>
                </td>
                <td>${new Date(b.bookingDate).toLocaleString('vi-VN')}</td>
                <td>${(b.totalPrice || 0).toLocaleString('vi-VN')}</td>
                <td><span class="order-status ${statusInfo.class}">${statusInfo.text}</span></td>
                <td>
                    ${confirmBtn}
                    ${viewTripBtn}
                    ${deleteBtn}
                </td>
            </tr>`;             
            tbody.innerHTML += row;
        });
    } catch (error) { console.error(error); tbody.innerHTML = `<tr><td colspan="7" class="error-message">Lỗi tải đơn tour</td></tr>`; }
}

async function loadUsers() {
    const tbody = document.querySelector('#table-users tbody');
    try {
        const response = await fetch('/api/admin/users', { headers: getAuthHeaders() });
        const users = await response.json();
        tbody.innerHTML = '';
        users.forEach(user => {
            const isLocked = user.lockoutEnd && new Date(user.lockoutEnd) > new Date();
            const statusText = isLocked ? 'Bị khóa' : 'Hoạt động';
            const lockAction = isLocked ? 'unlock' : 'lock';
            const lockText = isLocked ? 'Mở khóa' : 'Khóa';
            const lockClass = isLocked ? 'btn-success' : 'btn-danger';
            const row = `<tr data-user-id="${user.id}" data-user-roles='${JSON.stringify(user.roles)}'><td>${user.email}</td><td>${user.userName}</td><td>${user.roles.join(', ')}</td><td><span class="status ${isLocked ? 'status-locked' : 'status-active'}">${statusText}</span></td><td><button class="action-btn btn btn-sm btn-info edit-roles-btn">Sửa Roles</button><button class="action-btn btn btn-sm btn-warning reset-pw-btn">Reset PW</button><button class="action-btn btn btn-sm ${lockClass} lock-unlock-btn" data-action="${lockAction}">${lockText}</button><button class="action-btn btn btn-sm btn-secondary delete-btn">Xóa</button></td></tr>`;
            tbody.innerHTML += row;
        });
    } catch (error) {}
}   

async function startSignalRConnection() {
    if (connection.state === signalR.HubConnectionState.Disconnected) {
        try { await connection.start(); } catch (err) { setTimeout(startSignalRConnection, 5000); }
    }
}
connection.onclose(async () => { await startSignalRConnection(); });

// --- USER ACTIONS ---
function openEditRolesModal(userId, userEmail, currentUserRoles) {
    const emailSpan = document.getElementById('modal-user-email');
    const checkboxesDiv = document.getElementById('modal-roles-checkboxes');
    const errorDiv = document.getElementById('modal-error-message');
    
    errorDiv.style.display = 'none';
    emailSpan.textContent = userEmail;
    checkboxesDiv.innerHTML = '';

    allRoles.forEach(role => {
        const isChecked = currentUserRoles.includes(role);
        checkboxesDiv.innerHTML += `
            <div class="form-check">
                <input class="form-check-input" type="checkbox" id="role-${role}" value="${role}" ${isChecked ? 'checked' : ''}>
                <label class="form-check-label" for="role-${role}">${role}</label>
            </div>`;
    });

    document.getElementById('modal-save-roles-btn').setAttribute('data-user-id', userId);
    if(rolesModal) rolesModal.show();
}

function setupRolesModalEvents() {
    const saveBtn = document.getElementById('modal-save-roles-btn');
    if(saveBtn) {
        saveBtn.addEventListener('click', () => {
            const userId = saveBtn.getAttribute('data-user-id');
            saveUserRoles(userId);
        });
    }
}

async function saveUserRoles(userId) {
    const checkboxes = document.querySelectorAll('#modal-roles-checkboxes input:checked');
    const selectedRoles = Array.from(checkboxes).map(cb => cb.value);
    const errorDiv = document.getElementById('modal-error-message');

    try {
        const response = await fetch(`/api/admin/users/${userId}/roles`, {
            method: 'PUT',
            headers: getAuthHeaders(),
            body: JSON.stringify(selectedRoles)
        });

        if (response.ok) {
            if(rolesModal) rolesModal.hide();
            loadUsers(); 
        } else {
            const error = await response.json();
            errorDiv.textContent = `Lỗi: ${error.message || JSON.stringify(error.errors)}`;
            errorDiv.style.display = 'block';
        }
    } catch (err) {
        errorDiv.textContent = 'Lỗi kết nối đến máy chủ.';
        errorDiv.style.display = 'block';
    }
}

async function resetUserPassword(userId, userEmail) {
    if (!confirm(`Bạn có chắc chắn muốn reset mật khẩu cho ${userEmail}?`)) return;
    try {
        const response = await fetch(`/api/admin/users/${userId}/reset-password`, {
            method: 'POST',
            headers: getAuthHeaders()
        });
        const data = await response.json();
        if (response.ok) alert(data.message);
        else alert(`Lỗi: ${data.message || 'Không thể reset mật khẩu.'}`);
    } catch (error) { alert('Lỗi kết nối khi reset mật khẩu.'); }
}

async function lockUnlockUser(userId, action, userEmail) {
    const confirmationText = action === 'lock' ? `KHÓA tài khoản ${userEmail}?` : `MỞ KHÓA tài khoản ${userEmail}?`;
    if (!confirm(confirmationText)) return;
    try {
        const response = await fetch(`/api/admin/users/${userId}/${action}`, {
            method: 'PUT',
            headers: getAuthHeaders()
        });
        if (!response.ok) {
            const data = await response.json();
            alert(`Lỗi: ${data.message}`);
        }
    } catch (error) { alert('Lỗi kết nối.'); }
}

async function deleteUser(userId, userEmail) {
    if (!confirm(`!!! CẢNH BÁO !!!\nXÓA vĩnh viễn người dùng ${userEmail}?`)) return;
    try {
        const response = await fetch(`/api/admin/users/${userId}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });
        if (response.ok) {
            alert(`Đã xóa người dùng ${userEmail}.`);
            const row = document.querySelector(`#table-users tbody tr[data-user-id="${userId}"]`);
            if (row) row.remove();
        } else {
            const data = await response.json();
            alert(`Lỗi: ${data.message}`);
        }
    } catch (error) { alert('Lỗi kết nối.'); }
}

async function confirmOrder(orderType, orderId, buttonElement) {
    if (!confirm(`Bạn có chắc muốn xác nhận đơn ${orderType} mã ${orderId}?`)) return;
    
    buttonElement.textContent = 'Đang xử lý...';
    buttonElement.disabled = true;

    try {
        const response = await fetch(`/api/admin/bookings/confirm/${orderType}/${orderId}`, {
            method: 'PUT',
            headers: getAuthHeaders()
        });
        if (!response.ok) {
            const data = await response.json();
            alert('Lỗi: ' + (data.message || `Lỗi ${response.status}`));
            buttonElement.textContent = 'Xác nhận';
            buttonElement.disabled = false;
        }
    } catch (error) {
        alert('Lỗi khi thực hiện yêu cầu xác nhận.');
        buttonElement.textContent = 'Xác nhận';
        buttonElement.disabled = false;
    }
}

// --- TABLE EVENTS ---
function setupTableActions() {
    document.querySelector('main').addEventListener('click', (event) => {
        const target = event.target;
        const actionBtn = target.closest('.action-btn');
        if (!actionBtn) return;
        const row = actionBtn.closest('tr');
        
        if (row.closest('#table-users')) {
            const userId = row.getAttribute('data-user-id');
            const userEmail = row.cells[0]?.textContent;
            if (actionBtn.classList.contains('edit-roles-btn')) openEditRolesModal(userId, userEmail, JSON.parse(row.getAttribute('data-user-roles')));
            else if (actionBtn.classList.contains('reset-pw-btn')) resetUserPassword(userId, userEmail);
            else if (actionBtn.classList.contains('lock-unlock-btn')) lockUnlockUser(userId, actionBtn.getAttribute('data-action'), userEmail);
            else if (actionBtn.classList.contains('delete-btn')) deleteUser(userId, userEmail);
        }
        else if (actionBtn.classList.contains('confirm-btn')) {
            confirmOrder(actionBtn.getAttribute('data-order-type'), actionBtn.getAttribute('data-order-id'), actionBtn);
        }
        else if (actionBtn.classList.contains('edit-dest-btn')) openDestinationModal(JSON.parse(row.getAttribute('data-dest-object')));
        else if (actionBtn.classList.contains('delete-dest-btn')) deleteTopDestination(row.getAttribute('data-dest-id'));
        else if (actionBtn.classList.contains('edit-tour-btn')) openTourPackageModal(row.getAttribute('data-tour-id'));
        else if (actionBtn.classList.contains('delete-tour-btn')) deleteTourPackage(row.getAttribute('data-tour-id'));
        else if (actionBtn.classList.contains('schedule-tour-btn')) openScheduleModal(row.getAttribute('data-tour-id'), row.cells[0].textContent);
        else if (actionBtn.classList.contains('delete-order-btn')) {
            const orderType = actionBtn.getAttribute('data-order-type');
            const orderId = actionBtn.getAttribute('data-order-id');
            deleteOrderAdmin(orderType, orderId);
            deleteReview(actionBtn.getAttribute('data-review-id'));
        }
        // SỰ KIỆN NÚT DS KHÁCH
        else if (actionBtn.classList.contains('view-trip-btn')) {
            const departureId = actionBtn.getAttribute('data-departure-id');
            const modalTitle = document.getElementById('warningModalLabel');
            if(modalTitle) modalTitle.textContent = "Danh sách khách đặt chuyến này";
            loadWarningDetails(departureId);
        }
    });
}

async function deleteOrderAdmin(orderType, orderId) {
    if (!confirm(`CẢNH BÁO: XÓA VĨNH VIỄN đơn hàng ${orderId}?`)) return;
    try {
        const response = await fetch(`/api/admin/orders/${orderType}/${orderId}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });
        if (response.ok) {
            alert('Đã xóa thành công.');
            const row = document.querySelector(`tr[data-order-id="${orderId}"]`);
            if (row) row.remove();
        } else {
            alert('Lỗi khi xóa đơn hàng.');
        }
    } catch (error) { alert('Lỗi kết nối.'); }
}

// --- CMS FUNCTIONS ---
async function loadTopDestinations() {
    const tbody = document.querySelector('#table-destinations tbody');
    if (!tbody) return;
    tbody.innerHTML = '<tr><td colspan="6">Đang tải...</td></tr>';
    try {
        const response = await fetch('/api/admin/cms/top-destinations', { headers: getAuthHeaders() });
        const destinations = await response.json();
        tbody.innerHTML = '';
        destinations.forEach(dest => {
            const row = document.createElement('tr');
            row.setAttribute('data-dest-id', dest.id);
            row.setAttribute('data-dest-object', JSON.stringify(dest));
            row.innerHTML = `<td>${dest.cityName}</td><td class="td-wrap" title="${dest.imageUrl}">${dest.imageUrl}</td><td>${dest.searchTerm}</td><td>${dest.type}</td><td>${dest.displayOrder}</td><td><button class="action-btn btn btn-sm btn-info edit-dest-btn">Sửa</button> <button class="action-btn btn btn-sm btn-danger delete-dest-btn">Xóa</button></td>`;
            tbody.appendChild(row);
        });
    } catch (error) {}
}

function openDestinationModal(destData = null) {
    const form = document.getElementById('destination-form');
    form.reset();
    if (destData) {
        document.getElementById('modal-dest-title').textContent = 'Sửa Điểm đến';
        document.getElementById('dest-id').value = destData.id;
        document.getElementById('dest-city-name').value = destData.cityName;
        document.getElementById('dest-image-url').value = destData.imageUrl;
        document.getElementById('dest-search-term').value = destData.searchTerm;
        document.getElementById('dest-type').value = destData.type;
        document.getElementById('dest-display-order').value = destData.displayOrder;
    } else {
        document.getElementById('modal-dest-title').textContent = 'Thêm Điểm đến';
        document.getElementById('dest-id').value = '';
    }
    if(destModal) destModal.show();
}

function setupCmsModalEvents() {
    const form = document.getElementById('destination-form');
    const addBtn = document.getElementById('add-destination-btn');
    const saveBtn = document.getElementById('modal-save-dest-btn');
    if (addBtn) addBtn.onclick = () => openDestinationModal(null);
    if (saveBtn) saveBtn.addEventListener('click', () => form.requestSubmit());
    if (form) {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const destId = document.getElementById('dest-id').value;
            const data = {
                id: destId ? parseInt(destId) : 0,
                cityName: document.getElementById('dest-city-name').value,
                imageUrl: document.getElementById('dest-image-url').value,
                searchTerm: document.getElementById('dest-search-term').value,
                type: document.getElementById('dest-type').value,
                displayOrder: parseInt(document.getElementById('dest-display-order').value)
            };
            try {
                const url = destId ? `/api/admin/cms/top-destinations/${destId}` : '/api/admin/cms/top-destinations';
                const method = destId ? 'PUT' : 'POST';
                const response = await fetch(url, { method: method, headers: getAuthHeaders(), body: JSON.stringify(data) });
                if (response.ok) { if(destModal) destModal.hide(); loadTopDestinations(); }
            } catch (error) {}
        });
    }
}

async function deleteTopDestination(id) {
    try {
        const response = await fetch(`/api/admin/cms/top-destinations/${id}`, { method: 'DELETE', headers: getAuthHeaders() });
        if (response.ok) loadTopDestinations();
    } catch (error) {}
}

// --- TOUR PACKAGE FUNCTIONS ---
async function loadTourPackages() {
    const tbody = document.querySelector('#table-tour-packages tbody');
    if (!tbody) return;
    try {
        const response = await fetch('/api/admin/cms/tours', { headers: getAuthHeaders() });
        const tours = await response.json();
        tbody.innerHTML = '';
        tours.forEach(tour => {
            const row = document.createElement('tr');
            row.setAttribute('data-tour-id', tour.id);
            row.innerHTML = `<td>${tour.title}</td><td>${tour.duration || 'N/A'}</td><td>${(tour.price || 0).toLocaleString('vi-VN')} VND</td><td><button class="action-btn btn btn-sm btn-warning schedule-tour-btn">Lịch</button> <button class="action-btn btn btn-sm btn-info edit-tour-btn">Sửa</button> <button class="action-btn btn btn-sm btn-danger delete-tour-btn">Xóa</button></td>`;
            tbody.appendChild(row);
        });
    } catch (error) {}
}

function setupTourPackageModalEvents() {
    document.getElementById('add-tour-package-btn').onclick = () => openTourPackageModal(null);
    document.getElementById('modal-save-tour-btn').onclick = saveTourPackage;
    document.getElementById('add-itinerary-item-btn').onclick = addItineraryItemForm;
    const modalEl = document.getElementById('tour-package-modal');
    if (modalElement) {
        modalElement.addEventListener('click', (event) => {
            const saveBtn = event.target.closest('.save-itinerary-btn');
            const deleteBtn = event.target.closest('.delete-itinerary-btn');
            if (saveBtn) saveItineraryItem(saveBtn);
            else if (deleteBtn) deleteItineraryItem(deleteBtn);
        });
    }
}

async function openTourPackageModal(tourId) {
    const form = document.getElementById('tour-package-form');
    const itineraryListDiv = document.getElementById('itinerary-list');
    form.reset();
    itineraryListDiv.innerHTML = '';
    currentEditingTourId = tourId;
    if (tourId) {
        document.getElementById('modal-tour-title').textContent = 'Sửa Gói Tour';
        document.getElementById('add-itinerary-item-btn').disabled = false;
        try {
            const response = await fetch(`/api/admin/cms/tours/${tourId}`, { headers: getAuthHeaders() });
            const tour = await response.json();
            document.getElementById('tour-id').value = tour.id;
            document.getElementById('tour-title').value = tour.title;
            document.getElementById('tour-image-urls').value = tour.imageUrl;
            document.getElementById('tour-duration').value = tour.duration;
            document.getElementById('tour-price').value = tour.price;
            document.getElementById('tour-country').value = tour.country;
            document.getElementById('tour-region').value = tour.region;
            document.getElementById('tour-area').value = tour.area || "";
            document.getElementById('tour-highlights').value = tour.highlights;
            document.getElementById('tour-policy-includes').value = tour.policyIncludes;
            document.getElementById('tour-policy-excludes').value = tour.policyExcludes;
            renderItineraries(tour.itineraries);
        } catch (error) {}
    } else {
        document.getElementById('modal-tour-title').textContent = 'Thêm Gói Tour mới';
        document.getElementById('add-itinerary-item-btn').disabled = true;
        document.getElementById('tour-id').value = '';
    }
    if(tourPackageModal) tourPackageModal.show();
}

async function saveTourPackage() {
    const tourId = document.getElementById('tour-id').value;
    const data = {
        id: tourId ? parseInt(tourId) : 0,
        title: document.getElementById('tour-title').value,
        imageUrl: document.getElementById('tour-image-urls').value,
        duration: document.getElementById('tour-duration').value,
        price: parseFloat(document.getElementById('tour-price').value) || 0,
        country: document.getElementById('tour-country').value,
        region: document.getElementById('tour-region').value,
        area: document.getElementById('tour-area').value,
        currency: "VND",
        highlights: document.getElementById('tour-highlights').value,
        policyIncludes: document.getElementById('tour-policy-includes').value,
        policyExcludes: document.getElementById('tour-policy-excludes').value,
    };
    const url = tourId ? `/api/admin/cms/tours/${tourId}` : '/api/admin/cms/tours';
    const method = tourId ? 'PUT' : 'POST';
    try {
        const response = await fetch(url, { method: method, headers: getAuthHeaders(), body: JSON.stringify(data) });
        if (response.ok) {
            const savedTour = await response.json();
            if (!tourId) {
                document.getElementById('tour-id').value = savedTour.id;
                currentEditingTourId = savedTour.id;
                document.getElementById('add-itinerary-item-btn').disabled = false;
                alert('Đã lưu. Bạn có thể thêm lịch trình.');
            } else {
                if(tourPackageModal) tourPackageModal.hide();
            }
            loadTourPackages();
        } else alert('Lỗi lưu');
    } catch (error) {}
}

async function deleteTourPackage(id) {
    if(confirm('Xóa tour này?')) {
        await fetch(`/api/admin/cms/tours/${id}`, { method: 'DELETE', headers: getAuthHeaders() });
        loadTourPackages();
    }
}

function renderItineraries(itineraries) {
    const listDiv = document.getElementById('itinerary-list');
    listDiv.innerHTML = '';
    if (itineraries && itineraries.length) {
        itineraries.forEach(item => listDiv.appendChild(createItineraryItemForm(item)));
    }
}

function createItineraryItemForm(itemData) {
    const isNew = !itemData;
    const itemId = isNew ? 0 : itemData.id;
    const div = document.createElement('div');
    div.className = 'itinerary-item-form card mb-3';
    div.setAttribute('data-itinerary-id', itemId);
    div.innerHTML = `<div class="card-body"><div class="row g-2"><div class="col-md-2"><input type="number" class="form-control form-control-sm itinerary-day-input" value="${isNew?'':itemData.dayNumber}" placeholder="Ngày"></div><div class="col-md-10"><input type="text" class="form-control form-control-sm itinerary-title-input" value="${isNew?'':itemData.title}" placeholder="Tiêu đề"></div><div class="col-12"><textarea class="form-control form-control-sm itinerary-desc-textarea" rows="2">${isNew?'':itemData.description}</textarea></div><div class="col-12 text-end"><button class="btn btn-sm btn-success save-itinerary-btn" data-is-new="${isNew}">Lưu</button> <button class="btn btn-sm btn-danger delete-itinerary-btn">Xóa</button></div></div></div>`;
    return div;
}

function addItineraryItemForm() {
    document.getElementById('itinerary-list').appendChild(createItineraryItemForm(null));
}

async function saveItineraryItem(buttonElement) {
    const formDiv = buttonElement.closest('.itinerary-item-form');
    const itemId = formDiv.getAttribute('data-itinerary-id');
    const isNew = buttonElement.getAttribute('data-is-new') === 'true';
    const data = {
        id: parseInt(itemId),
        dayNumber: parseInt(formDiv.querySelector('.itinerary-day-input').value) || 0,
        title: formDiv.querySelector('.itinerary-title-input').value,
        description: formDiv.querySelector('.itinerary-desc-textarea').value,
        tourPackageId: currentEditingTourId
    };
    const url = isNew ? '/api/admin/cms/itineraries' : `/api/admin/cms/itineraries/${itemId}`;
    const method = isNew ? 'POST' : 'PUT';
    try {
        const response = await fetch(url, { method: method, headers: getAuthHeaders(), body: JSON.stringify(data) });
        if (response.ok) {
            const savedItem = await response.json();
            formDiv.setAttribute('data-itinerary-id', savedItem.id);
            buttonElement.setAttribute('data-is-new', 'false');
            alert('Đã lưu lịch trình.');
        }
    } catch (error) {}
}

async function deleteItineraryItem(buttonElement) {
    const formDiv = buttonElement.closest('.itinerary-item-form');
    const itemId = formDiv.getAttribute('data-itinerary-id');
    if (!itemId || itemId === '0') { formDiv.remove(); return; }
    if (!confirm('Xóa lịch trình này?')) return;
    try {
        const response = await fetch(`/api/admin/cms/itineraries/${itemId}`, { method: 'DELETE', headers: getAuthHeaders() });
        if (response.ok) formDiv.remove();
    } catch (error) {}
}

// --- SCHEDULE FUNCTIONS ---
async function openScheduleModal(tourId, tourName) {
    document.getElementById('schedule-tour-title').textContent = tourName;
    document.getElementById('schedule-tour-id').value = tourId;
    document.getElementById('add-schedule-form').reset();
    await loadSchedules(tourId);
    if(scheduleModal) scheduleModal.show();
}

async function loadSchedules(tourId) {
    const tbody = document.querySelector('#table-schedules tbody');
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">Đang tải...</td></tr>';
    try {
        const res = await fetch(`/api/admin/cms/tours/${tourId}/departures`, { headers: getAuthHeaders() });
        const data = await res.json();
        tbody.innerHTML = '';
        data.forEach(item => {
            const tr = document.createElement('tr');
            tr.innerHTML = `<td>${new Date(item.startDate).toLocaleDateString('vi-VN')}</td><td>${item.endDate ? new Date(item.endDate).toLocaleDateString('vi-VN') : '---'}</td><td>${item.priceAdult.toLocaleString()}</td><td>${item.availableSeats}</td><td class="text-center"><button class="btn btn-sm btn-outline-danger delete-sched-btn" data-id="${item.id}">&times;</button></td>`;
            tr.querySelector('.delete-sched-btn').onclick = () => deleteSchedule(item.id, tourId);
            tbody.appendChild(tr);
        });
    } catch (err) {}
}

const addSchedForm = document.getElementById('add-schedule-form');
if(addSchedForm){
    addSchedForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const tourId = document.getElementById('schedule-tour-id').value;
        const price = parseFloat(document.getElementById('sched-price').value);
        const data = {
            tourPackageId: parseInt(tourId),
            startDate: document.getElementById('sched-start').value,
            priceAdult: price,
            priceChild: price * 0.75,
            priceInfant: price * 0.3,
            availableSeats: parseInt(document.getElementById('sched-seats').value)
        };
        try {
            const res = await fetch('/api/admin/cms/departures', { method: 'POST', headers: getAuthHeaders(), body: JSON.stringify(data) });
            if(res.ok) loadSchedules(tourId);
        } catch(err) {}
    });
}

async function deleteSchedule(id, tourId) {
    if(!confirm("Xóa ngày khởi hành này?")) return;
    try {
        const res = await fetch(`/api/admin/cms/departures/${id}`, { method: 'DELETE', headers: getAuthHeaders() });
        if(res.ok) loadSchedules(tourId);
    } catch(err) {}
}

// --- CHARTS & REVIEWS ---
let revenueChartInstance = null; 
async function loadMonthlyChart(summaryStats) {
    const ctxPie = document.getElementById('orderTypeChart');
    if (ctxPie) {
        new Chart(ctxPie, {
            type: 'doughnut',
            data: {
                labels: ['Chuyến bay', 'Tour du lịch'],
                datasets: [{ data: [summaryStats.totalFlightOrders, summaryStats.totalActivityBookings], backgroundColor: ['#4e73df', '#1cc88a'] }],
            }
        });
    }
    try {
        const res = await fetch('/api/admin/monthly-stats', { headers: getAuthHeaders() });
        const monthlyData = await res.json();
        const ctxBar = document.getElementById('revenueChart');
        if (ctxBar) {
            if (revenueChartInstance) revenueChartInstance.destroy();
            revenueChartInstance = new Chart(ctxBar, {
                type: 'bar',
                data: {
                    labels: monthlyData.map(item => `Tháng ${item.month}`),
                    datasets: [{ label: 'Doanh thu (VND)', data: monthlyData.map(item => item.revenue), backgroundColor: "#4e73df" }]
                },
                options: {
                    onClick: (evt, activeElements) => {
                        if (activeElements.length > 0) showMonthlyDetails(activeElements[0].index + 1, new Date().getFullYear());
                    }
                }
            });
        }
    } catch (err) {}
}

async function showMonthlyDetails(month, year) {
    const modal = new bootstrap.Modal(document.getElementById('monthDetailsModal'));
    document.getElementById('modal-month-title').textContent = `${month}/${year}`;
    const tbody = document.getElementById('modal-month-tbody');
    tbody.innerHTML = '<tr><td colspan="6">Đang tải...</td></tr>';
    modal.show();
    try {
        const res = await fetch(`/api/admin/monthly-details?month=${month}&year=${year}`, { headers: getAuthHeaders() });
        const orders = await res.json();
        tbody.innerHTML = '';
        orders.forEach(o => {
            tbody.innerHTML += `<tr><td><small>${o.orderId}</small></td><td>${o.type}</td><td class="text-truncate" style="max-width:200px">${o.name}</td><td>${o.customer}</td><td>${new Date(o.date).toLocaleDateString()}</td><td class="text-end text-danger">${o.total.toLocaleString()} ₫</td></tr>`;
        });
    } catch (err) {}
}

async function loadReviews() {
    const tbody = document.querySelector("#table-reviews tbody");
    if (!tbody) return;
    try {
        const res = await fetch("/api/Review/admin", { headers: getAuthHeaders() });
        const reviews = await res.json();
        tbody.innerHTML = "";
        let total = 0;
        reviews.forEach(r => {
            total += r.rating;
            tbody.innerHTML += `<tr><td>${r.id}</td><td>${r.tourName}</td><td>${r.userEmail}</td><td>${r.rating}</td><td>${r.comment}</td><td>${new Date(r.createdAt).toLocaleDateString()}</td><td><button class="action-btn btn btn-sm btn-danger delete-review-btn" data-review-id="${r.id}">X</button></td></tr>`;
        });
        document.getElementById("badge-total-reviews").textContent = reviews.length;
        if(reviews.length) document.getElementById("badge-average-rating").textContent = (total / reviews.length).toFixed(1);
    } catch (err) {}
}

async function deleteReview(id) {
    if(confirm('Xóa đánh giá?')) {
        await fetch(`/api/Review/admin/${id}`, { method: 'DELETE', headers: getAuthHeaders() });
        loadReviews();
    }
}

// --- WARNINGS & LOCKS ---
async function loadWarningDetails(departureId) {
    const modal = new bootstrap.Modal(document.getElementById('warningModal'));
    const tbody = document.getElementById('warning-bookings-table');
    tbody.innerHTML = '<tr><td colspan="4">Đang tải...</td></tr>';
    modal.show();
    try {
        const res = await fetch(`/api/admin/cms/tour-bookings/${departureId}`, { headers: getAuthHeaders() });
        const bookings = await res.json();
        tbody.innerHTML = '';
        bookings.forEach(b => tbody.innerHTML += `<tr><td>${b.orderId}</td><td>${b.contactName}</td><td>${b.contactPhone}</td><td>${b.people}</td></tr>`);
    } catch (err) {}
}

async function lockTourNow(departureId) {
    if(!confirm("Khóa tour ngay lập tức?")) return;
    try {
        const res = await fetch(`/api/admin/cms/departures/${departureId}/lock`, { method: 'PUT', headers: getAuthHeaders() });
        if(res.ok) {
            alert("Đã khóa tour!");
            loadTourWarnings();
            if(window.warningModalObj) window.warningModalObj.hide();
        }
    } catch(err) {}
}

async function loadTourWarnings() {
    const card = document.getElementById('tour-warnings-card');
    const tbody = document.getElementById('table-tour-warnings');
    if (!tbody) return;
    try {
        const res = await fetch('/api/admin/cms/warning-departures', { headers: getAuthHeaders() });
        const warnings = await res.json();
        if (!warnings.length) { card.style.display = 'none'; return; }
        card.style.display = 'block';
        tbody.innerHTML = '';
        warnings.forEach(w => {
            const row = document.createElement('tr');
            row.innerHTML = `<td>${w.tourName}</td><td>${new Date(w.startDate).toLocaleDateString('vi-VN')}</td><td><span class="badge bg-danger">${w.bookedSeats}/${w.totalSeats}</span></td><td><button class="btn btn-sm btn-info view-warning-btn" data-id="${w.departureId}">Xem</button> <button class="btn btn-sm btn-danger lock-warning-btn" data-id="${w.departureId}">Khóa</button></td>`;
            row.querySelector('.view-warning-btn').onclick = () => loadWarningDetails(w.departureId);
            row.querySelector('.lock-warning-btn').onclick = () => lockTourNow(w.departureId);
            tbody.appendChild(row);
        });
    } catch (err) {}
}

// --- CHAT SYSTEM ---
function initChatSystem() {
    const userList = document.getElementById('user-list');
    const msgDiv = document.getElementById('admin-messages');
    const chatForm = document.getElementById('admin-chat-form');
    const input = document.getElementById('admin-input');
    const header = document.getElementById('current-chat-user');

    if (!userList || !msgDiv) return;
    const token = localStorage.getItem('adminToken');
    if (!token || (chatAdminConnection && chatAdminConnection.state === signalR.HubConnectionState.Connected)) return;

    chatAdminConnection = new signalR.HubConnectionBuilder().withUrl("/chatHub", { accessTokenFactory: () => token }).withAutomaticReconnect().build();
    
    chatAdminConnection.on("ReceiveMessageFromUser", (uid, name, msg) => {
        if (!chatHistory[uid]) {
            chatHistory[uid] = { name: name, msgs: [] };
            const div = document.createElement('div');
            div.className = 'chat-user-item p-3 border-bottom bg-white';
            div.style.cursor = 'pointer';
            div.setAttribute('data-id', uid);
            div.innerHTML = `<div class="fw-bold">${name}</div><div class="small text-muted prev">${msg}</div>`;
            div.onclick = () => selectUser(uid, name);
            userList.prepend(div);
        } else {
            const item = document.querySelector(`.chat-user-item[data-id="${uid}"]`);
            if(item) { item.querySelector('.prev').textContent = msg; userList.prepend(item); }
        }
        chatHistory[uid].msgs.push({ txt: msg, type: 'in' });
        if (currentChatUserId === uid) renderMsg(msg, 'in');
    });

    chatAdminConnection.start();

    function selectUser(uid, name) {
        currentChatUserId = uid;
        header.textContent = `Chat với: ${name}`;
        chatForm.style.display = 'flex';
        msgDiv.innerHTML = '';
        if(chatHistory[uid]) chatHistory[uid].msgs.forEach(m => renderMsg(m.txt, m.type));
    }

    function renderMsg(txt, type) {
        const div = document.createElement('div');
        div.style.padding = '8px 12px'; div.style.borderRadius = '15px'; div.style.margin = '5px'; div.style.maxWidth = '70%';
        if(type === 'in') { div.style.background = '#e9ecef'; div.style.alignSelf = 'flex-start'; }
        else { div.style.background = '#0d6efd'; div.style.color = '#fff'; div.style.alignSelf = 'flex-end'; }
        div.textContent = txt;
        msgDiv.appendChild(div);
        msgDiv.scrollTop = msgDiv.scrollHeight;
    }

    chatForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const txt = input.value.trim();
        if(!txt || !currentChatUserId) return;
        renderMsg(txt, 'out');
        input.value = '';
        chatHistory[currentChatUserId].msgs.push({ txt: txt, type: 'out' });
        await chatAdminConnection.invoke("SendMessageToUser", currentChatUserId, txt);
    });
}

// --- HÀM XUẤT EXCEL (.XLSX) MỚI (DÙNG SHEETJS) ---
async function exportToExcel() {
    try {
        const res = await fetch('/api/admin/monthly-stats', { headers: getAuthHeaders() });
        if (!res.ok) throw new Error("Không thể tải dữ liệu báo cáo.");
        const data = await res.json();

        // Chuẩn bị dữ liệu mảng 2 chiều
        const title = [["HỆ THỐNG BOOKING TOUR - BÁO CÁO DOANH THU"]];
        const info = [
            ["Người xuất:", "Admin System"],
            ["Ngày xuất:", new Date().toLocaleString('vi-VN')],
            [] // Dòng trống
        ];
        const headers = [["Tháng", "Tổng Số Đơn Hàng", "Doanh Thu (VND)"]];

        let totalRevenue = 0;
        let totalOrders = 0;
        const rows = [];

        data.forEach(item => {
            totalRevenue += item.revenue;
            totalOrders += item.orderCount;
            rows.push([
                `Tháng ${item.month}`, 
                item.orderCount, 
                item.revenue 
            ]);
        });

        const footer = [
            [],
            ["TỔNG CỘNG NĂM " + new Date().getFullYear(), totalOrders, totalRevenue]
        ];

        const finalData = [...title, ...info, ...headers, ...rows, ...footer];

        // Tạo Workbook
        const wb = XLSX.utils.book_new();
        const ws = XLSX.utils.aoa_to_sheet(finalData);

        // Căn chỉnh độ rộng cột
        ws['!cols'] = [
            { wch: 20 }, // Cột A
            { wch: 25 }, // Cột B
            { wch: 30 }  // Cột C
        ];

        // Gộp ô tiêu đề
        ws['!merges'] = [
            { s: { r: 0, c: 0 }, e: { r: 0, c: 2 } } 
        ];

        // Xuất file
        XLSX.utils.book_append_sheet(wb, ws, "Báo Cáo");
        XLSX.writeFile(wb, `Bao_Cao_Doanh_Thu_${new Date().getFullYear()}.xlsx`);

    } catch (err) {
        console.error(err);
        alert("Lỗi khi xuất file Excel: " + err.message);
    }
}