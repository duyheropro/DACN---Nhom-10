// TRONG FILE: wwwroot/js/history.js

document.addEventListener('DOMContentLoaded', () => {
    loadOrderHistory();
});

// Hàm định dạng trạng thái thông minh
function formatStatus(status, startDateStr, endDateStr) {
    const upperStatus = status ? status.toUpperCase() : 'UNKNOWN';
    const now = new Date(); // Ngày hiện tại

    // 1. Đã hủy
    if (upperStatus === 'CANCELLED') {
        return { text: 'Đã hủy', class: 'status-cancelled' };
    }

    // 2. Chờ xác nhận
    if (upperStatus === 'PENDING_CONFIRMATION') {
        return { text: 'Chờ xác nhận', class: 'status-pending_confirmation' };
    }

    // 3. Đã xác nhận/Đã thanh toán
    if (upperStatus.includes('CONFIRMED')) {
        // Kiểm tra thời gian để hiển thị lộ trình
        if (startDateStr && endDateStr) {
            const start = new Date(startDateStr);
            const end = new Date(endDateStr);
            // Tính hết ngày về (23:59:59)
            end.setHours(23, 59, 59); 

            if (now > end) {
                // Đã đi xong
                return { text: 'Đã hoàn thành tour', class: 'status-completed' }; 
            } else if (now >= start && now <= end) {
                // Đang đi
                return { text: 'Đang đi tour', class: 'status-ongoing' };
            } else {
                // Chưa đi
                return { text: 'Chuẩn bị đi tour', class: 'status-confirmed' };
            }
        }
        // Nếu không có ngày tháng (ví dụ vé máy bay) thì hiện Đã xác nhận
        return { text: 'Đã xác nhận', class: 'status-confirmed' };
    }

    return { text: (status || 'Không rõ'), class: '' };
}

async function loadOrderHistory() {
    const loadingMessage = document.getElementById('loading-message');
    const errorMessage = document.getElementById('error-message');
    const orderListDiv = document.getElementById('order-list');
    const noOrdersDiv = document.getElementById('no-orders');

    loadingMessage.style.display = 'block';
    errorMessage.style.display = 'none';
    noOrdersDiv.style.display = 'none';
    orderListDiv.innerHTML = '';

    const token = localStorage.getItem('userToken');
    if (!token) {
        window.location.href = '/html/login.html';
        return;
    }

    try {
        const response = await fetch('/api/booking/my-orders', {
            headers: { 'Authorization': 'Bearer ' + token }
        });

        if (response.ok) {
            const orders = await response.json();
            loadingMessage.style.display = 'none';

            if (orders && orders.length > 0) {
                displayOrders(orders);
            } else {
                noOrdersDiv.style.display = 'block';
            }
        } else {
            throw new Error(`Lỗi ${response.status}: Không thể tải lịch sử.`);
        }
    } catch (error) {
        loadingMessage.style.display = 'none';
        errorMessage.textContent = error.message;
        errorMessage.style.display = 'block';
    }
}

function displayOrders(orders) {
    const orderListDiv = document.getElementById('order-list');
    orderListDiv.innerHTML = '';

    orders.forEach(order => {
        // Format ngày tháng
        const bookingDate = order.date ? new Date(order.date).toLocaleString('vi-VN') : 'N/A';
        const startStr = order.startDate ? new Date(order.startDate).toLocaleDateString('vi-VN') : '';
        const endStr = order.endDate ? new Date(order.endDate).toLocaleDateString('vi-VN') : '';
        
        // Lấy trạng thái hiển thị
        const statusInfo = formatStatus(order.status, order.startDate, order.endDate);
        const orderType = order.type === 'Tour' ? 'tour' : 'flight';

        // Logic nút Hủy: Chỉ hủy được nếu chưa đi (start date còn ở tương lai)
        let actionButton = '';
        // Kiểm tra xem ngày đi có hợp lệ và còn ở tương lai không
        let isFutureTrip = true;
        if (order.startDate) {
            isFutureTrip = new Date() < new Date(order.startDate);
        }

        if (order.status === 'PENDING_CONFIRMATION') {
            actionButton = `<button class="btn-cancel-order pending" onclick="cancelUserOrder('${orderType}', '${order.orderId}')"><i class="fa-solid fa-xmark"></i> Xóa yêu cầu</button>`;
        } 
        else if (order.status.includes('CONFIRMED') && isFutureTrip) {
            actionButton = `<button class="btn-cancel-order confirmed" onclick="cancelUserOrder('${orderType}', '${order.orderId}')">Hủy đặt chỗ</button>`;
        }

        // HTML chi tiết
        let infoHtml = '';
        if (order.type === 'Tour') {
            // Sử dụng biến startStr và endStr đã khai báo ở trên
            infoHtml = `
                <div class="tour-detail-grid">
                    <p><i class="fa-regular fa-calendar-days"></i> <strong>Lịch trình:</strong> ${startStr} - ${endStr}</p>
                    <p><i class="fa-solid fa-user-group"></i> <strong>Số người:</strong> ${order.people || '1 người'}</p>
                    <p><i class="fa-solid fa-plane-departure"></i> <strong>Phương tiện:</strong> ${order.transport || 'Xe du lịch'}</p>
                </div>
            `;
        }

        orderListDiv.innerHTML += `
            <div class="order-card" id="order-${order.orderId}">
                <div class="order-header">
                    <div class="header-left">
                        <span class="order-type">${order.type || 'N/A'}</span>
                        <span class="order-id">#${order.orderId}</span>
                    </div>
                    <span class="booking-date">Ngày đặt: ${bookingDate}</span>
                </div>
                <div class="order-body">
                    <h3 class="order-title">${order.details || 'N/A'}</h3>
                    
                    ${infoHtml}

                    <p class="order-price"><strong>Tổng tiền:</strong> <span class="price-text">${(order.totalPrice || 0).toLocaleString('vi-VN')} ${order.currency || ''}</span></p>
                </div>
                <div class="order-footer">
                     <span class="order-status ${statusInfo.class}">${statusInfo.text}</span>
                     ${actionButton}
                </div>
            </div>
        `;
    });
}

// Logic hủy đơn (Giữ nguyên)
async function cancelUserOrder(type, id) {
    if (!confirm('Bạn có chắc chắn muốn thực hiện hành động này không?')) return;
    const token = localStorage.getItem('userToken');
    try {
        const res = await fetch(`/api/booking/cancel/${type}/${id}`, {
            method: 'PUT',
            headers: { 'Authorization': 'Bearer ' + token, 'Content-Type': 'application/json' }
        });
        const data = await res.json();
        if (res.ok) {
            alert(data.message);
            // Reload lại để cập nhật trạng thái
            loadOrderHistory();
        } else {
            alert(data.message || 'Có lỗi xảy ra.');
        }
    } catch (err) {
        console.error(err);
        alert('Lỗi kết nối.');
    }
}