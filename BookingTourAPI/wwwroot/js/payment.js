// TRONG FILE: wwwroot/js/payment.js (ĐÃ SỬA LỖI TOUR)

function getAuthHeaders() {
    const token = localStorage.getItem('userToken');
    const headers = { 'Content-Type': 'application/json' };
    if (token) {
        headers['Authorization'] = 'Bearer ' + token;
    }
    return headers;
}

// --- BIẾN TOÀN CỤC CHO POLLING ---
let pollingInterval = null;
let currentPollingOrderId = null;
let currentPollingOrderType = null;

document.addEventListener('DOMContentLoaded', () => {
    // Lấy các element cần thiết
    const loadingMessage = document.getElementById('loading-message');
    const errorMessage = document.getElementById('error-message');
    const paymentForm = document.getElementById('payment-form');
    const itemDetailsDiv = document.getElementById('item-details');
    const nameInput = document.getElementById('traveler-name');
    const emailInput = document.getElementById('traveler-email');
    const phoneInput = document.getElementById('traveler-phone');
    const paymentErrorDiv = document.getElementById('payment-error');
    const successMessageDiv = document.getElementById('success-message');
    
    // Nút
    const payLaterButton = document.getElementById('confirm-payment-btn');
    const momoButton = document.getElementById('momo-payment-btn');
    
    // Tóm tắt
    const summaryTotalPriceEl = document.getElementById('summary-total-price');
    const paymentContainer = document.querySelector('.payment-container');

    // Modal
    const momoQrModal = document.getElementById('momo-qr-modal');
    const closeQrModalBtn = document.getElementById('close-qr-modal');
    const qrCodeImage = document.getElementById('qr-code-image');
    const qrSpinner = document.getElementById('qr-loading-spinner');

    let pendingItemData = null; 

    function hideMessages() {
        loadingMessage.style.display = 'none';
        errorMessage.style.display = 'none';
        paymentErrorDiv.style.display = 'none';
        successMessageDiv.style.display = 'none';
        errorMessage.textContent = '';
        paymentErrorDiv.textContent = '';
        successMessageDiv.textContent = '';
    }

    // --- 1. TẢI THÔNG TIN ---
    function loadPendingItem() {
        hideMessages();
        loadingMessage.style.display = 'block';
        if (paymentForm) paymentForm.style.display = 'none'; 

        const pendingPaymentJson = localStorage.getItem('pendingPayment');
        if (!pendingPaymentJson) {
            loadingMessage.style.display = 'none';
            errorMessage.innerHTML = 'Không tìm thấy thông tin đặt vé/phòng. Vui lòng <a href="/html/home.html" style="color:red; font-weight:bold;">quay lại trang chủ</a> và chọn lại dịch vụ.';
            errorMessage.style.display = 'block';
            if(payLaterButton) payLaterButton.disabled = true;
            if(momoButton) momoButton.disabled = true;
            return;
        }

        try {
            pendingItemData = JSON.parse(pendingPaymentJson);
            if (!pendingItemData || !pendingItemData.type || typeof pendingItemData.item !== 'object' || pendingItemData.item === null) {
                throw new Error("Dữ liệu đơn hàng không hợp lệ.");
            }
            displayItemSummary(pendingItemData.type, pendingItemData.item);
            if (paymentForm) paymentForm.style.display = 'block';
            loadingMessage.style.display = 'none';
            autofillUserInfo();
        } catch (error) {
            console.error("Lỗi xử lý pendingPayment (dữ liệu cũ hoặc hỏng):", error.message);
            loadingMessage.style.display = 'none';
            localStorage.removeItem('pendingPayment'); 
            errorMessage.innerHTML = `
                Lỗi: Dữ liệu đơn hàng đã cũ hoặc không hợp lệ.<br>
                Vui lòng <a href="/html/home.html" style="color:red; font-weight:bold;">quay lại trang chủ</a> và chọn lại dịch vụ.
            `;
            errorMessage.style.display = 'block';
            if(payLaterButton) payLaterButton.disabled = true;
            if(momoButton) momoButton.disabled = true;
            pendingItemData = null; 
            if (paymentForm) paymentForm.style.display = 'none';
        }
    }

    // --- 2. HIỂN THỊ TÓM TẮT ---
    function displayItemSummary(type, item) {
        itemDetailsDiv.innerHTML = ''; 
        let summaryHtml = '';
        let displayAmount = 0;
        let displayCurrency = 'VND';

        const usdToVndRate = 25000;
        const eurToVndRate = 27000; 

        try {
            // Cấu trúc FLIGHT (item.price.total)
            if (type === 'flight' && item.price && item.price.total) { 
                displayAmount = parseFloat(item.price.total);
                displayCurrency = item.price.currency;
                const itinerary = item.itineraries[0]; 
                const firstSeg = itinerary.segments[0];
                const lastSeg = itinerary.segments[itinerary.segments.length - 1];
                summaryHtml = `
                    <p><strong>Loại dịch vụ:</strong> <span>Chuyến bay</span></p>
                    <p><strong>Hành trình:</strong> <span>${firstSeg.departure.iataCode} ✈️ ${lastSeg.arrival.iataCode}</span></p>
                    <p><strong>Hãng bay:</strong> <span>${firstSeg.carrierCode}</span></p>
                `;
            // Cấu trúc HOTEL (item.offer.price.total)
            } else if (type === 'hotel' && item.offer && item.offer.price && item.offer.price.total) { 
                const totalPrice = parseFloat(item.offer.price.total);
                const currency = item.offer.price.currency;
                if(currency === 'USD'){ displayAmount = totalPrice * usdToVndRate; displayCurrency = 'VND'; } 
                else { displayAmount = totalPrice; displayCurrency = currency; }
                summaryHtml = `
                    <p><strong>Loại dịch vụ:</strong> <span>Khách sạn</span></p>
                    <p><strong>Khách sạn:</strong> <span>${item.name || 'N/A'}</span></p>
                    <p><strong>Nhận phòng:</strong> <span>${item.offer.checkInDate}</span></p>
                    <p><strong>Trả phòng:</strong> <span>${item.offer.checkOutDate}</span></p>
                `;
            // Cấu trúc TOUR (item.price)
            } else if (type === 'tour' && item.price) {
                const totalPrice = parseFloat(item.price);
                const currency = item.currency || 'VND'; 
                
                if(currency === 'EUR'){ displayAmount = totalPrice * eurToVndRate; displayCurrency = 'VND'; } 
                else { displayAmount = totalPrice; displayCurrency = currency; }
                
                // --- PHẦN THÊM MỚI ---
                let departureDateText = 'Chưa chọn ngày';
                if (item.startDate) {
                    const dateObj = new Date(item.startDate);
                    departureDateText = dateObj.toLocaleDateString('vi-VN'); // VD: 27/11/2025
                }
                
                // Lấy mã tour (ID)
                const tourId = item.id || 'N/A';
                // ---------------------

                summaryHtml = `
                    <p><strong>Loại dịch vụ:</strong> <span>Tour du lịch</span></p>
                    <p><strong>Mã tour:</strong> <span style="color: #007bff; font-weight:bold;">#${tourId}</span></p>
                    <p><strong>Tên tour:</strong> <span>${item.title || 'N/A'}</span></p>
                    <p><strong>Ngày khởi hành:</strong> <span style="color: #d9534f; font-weight:bold;">${departureDateText}</span></p>
                `;
            } else {
                // Nếu không khớp BẤT KỲ cấu trúc nào, báo lỗi
                throw new Error("Dữ liệu 'item' thiếu thông tin 'price' hoặc cấu trúc không đúng.");
            }
        } catch (e) {
             console.error("Lỗi hiển thị tóm tắt:", e);
             throw new Error("Dữ liệu đơn hàng không hợp lệ (sai cấu trúc).");
        }

        itemDetailsDiv.innerHTML = summaryHtml;
        if (summaryTotalPriceEl) {
            // Kiểm tra NaN trước khi hiển thị
            if (isNaN(displayAmount)) {
                summaryTotalPriceEl.textContent = 'Lỗi giá';
                throw new Error("Tính toán giá thất bại (NaN).");
            }
            summaryTotalPriceEl.textContent = `${displayAmount.toLocaleString('vi-VN')} ${displayCurrency}`;
        }
    }

    // --- 3. TỰ ĐỘNG ĐIỀN ---
    async function autofillUserInfo() {
        const token = localStorage.getItem('userToken');
        if (token) {
            try {
                const response = await fetch('/api/account/profile', { headers: getAuthHeaders() });
                if (response.ok) {
                    const profile = await response.json();
                    if (nameInput) nameInput.value = profile.fullName || profile.userName || '';
                    if (emailInput) emailInput.value = profile.email || '';
                } else if (response.status === 401) {
                    console.warn("Token hết hạn, không thể tự động điền thông tin. Chuyển hướng đăng nhập.");
                    alert('Phiên đăng nhập của bạn đã hết hạn. Vui lòng đăng nhập lại để tiếp tục.');
                    localStorage.setItem('redirectAfterLogin', window.location.href);
                    window.location.href = '/html/login.html';
                }
            } catch (error) { console.warn("Không thể tự động điền thông tin user:", error); }
        }
         if (nameInput) nameInput.placeholder = 'Nguyễn Văn A';
         if (emailInput) emailInput.placeholder = 'email@example.com';
         if (phoneInput) phoneInput.placeholder = '09xxxxxxxx';
    }


    // --- 4. HÀM TẠO ĐƠN HÀNG (DÙNG CHUNG) ---
    async function createPendingOrder() {
        if (!pendingItemData) {
            paymentErrorDiv.textContent = 'Không có thông tin đơn hàng để xử lý.';
            paymentErrorDiv.style.display = 'block';
            return null;
        }

        const travelerName = nameInput.value;
        const travelerEmail = emailInput.value;
        const travelerPhone = phoneInput.value;
        
        if (!travelerName || !travelerEmail || !travelerPhone) {
             paymentErrorDiv.textContent = 'Vui lòng điền đầy đủ Họ tên, Email và SĐT.';
             paymentErrorDiv.style.display = 'block';
             return null;
        }

        const orderData = {
            serviceType: pendingItemData.type,
            itemData: pendingItemData.item,
            traveler: {
                name: travelerName,
                email: travelerEmail,
                phone: travelerPhone
            }
        };

        try {
            const response = await fetch('/api/booking/create-pending-order', {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify(orderData)
            });

            if (response.ok) {
                const result = await response.json();
                return result; 
            } else {
                if(response.status === 401) {
                    alert('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
                    localStorage.setItem('redirectAfterLogin', window.location.href);
                    window.location.href = '/html/login.html';
                    return null;
                }
                const errorData = await response.json();
                throw new Error(errorData.message || `Lỗi ${response.status} từ server.`);
            }
        } catch (error) {
            console.error("Lỗi khi tạo đơn hàng:", error);
            paymentErrorDiv.textContent = error.message || 'Không thể gửi yêu cầu.';
            paymentErrorDiv.style.display = 'block';
            return null;
        }
    }

    // --- 5. HÀM HIỂN THỊ THÀNH CÔNG (DÙNG CHUNG) ---
    function showSuccessPage(bookingId, isMomoPayment) {
        localStorage.removeItem('pendingPayment');
        if (paymentContainer) paymentContainer.style.display = 'none';

        const paymentMethodText = isMomoPayment 
            ? "Thanh toán MoMo của bạn đã thành công!" 
            : "Yêu cầu của bạn đã được ghi nhận (Thanh toán sau).";
        
        successMessageDiv.innerHTML = `
            <h2>${isMomoPayment ? 'Thanh toán thành công!' : 'Yêu cầu đã được gửi!'}</h2>
            <p>${paymentMethodText}</p>
            <p>Mã đơn hàng: <strong>${bookingId || 'N/A'}</strong></p>
            <p>Trạng thái: <strong>${isMomoPayment ? 'Đã xác nhận' : 'Chờ xác nhận'}</strong>.</p>
            <p>Bạn có thể theo dõi đơn hàng trong <a href="/html/history.html">Lịch sử đặt vé</a>.</p>
            <p style="margin-top: 20px;"><a href="/html/home.html" class="auth-button" style="background-color:#007bff; text-decoration: none;">Quay về trang chủ</a></p>
        `;
        successMessageDiv.style.display = 'block';
    }


    // --- 6. XỬ LÝ NÚT (THANH TOÁN SAU) ---
    if (paymentForm) {
        paymentForm.addEventListener('submit', async (e) => {
            e.preventDefault(); 
            hideMessages();
            payLaterButton.textContent = 'Đang xử lý...';
            payLaterButton.disabled = true;
            momoButton.disabled = true;

            const result = await createPendingOrder(); 

            if (result && result.bookingId) {
                showSuccessPage(result.bookingId, false);
            } else {
                payLaterButton.textContent = 'Xác nhận (Thanh toán sau)';
                payLaterButton.disabled = false;
                momoButton.disabled = false;
            }
        });
    }

    // --- 7. XỬ LÝ NÚT (THANH TOÁN MOMO) ---
    if (momoButton) {
        momoButton.addEventListener('click', async () => {
            hideMessages();
            if (!paymentForm.checkValidity()) {
                paymentErrorDiv.textContent = 'Vui lòng điền đầy đủ Họ tên, Email và SĐT.';
                paymentErrorDiv.style.display = 'block';
                return;
            }
            
            payLaterButton.disabled = true;
            momoButton.textContent = 'Đang tạo đơn...';
            momoButton.disabled = true;

            const orderResult = await createPendingOrder();

            if (orderResult && orderResult.bookingId) {
                try {
                    qrCodeImage.style.display = 'none';
                    qrSpinner.style.display = 'block'; 
                    qrSpinner.classList.add('visible');
                    momoQrModal.style.display = 'block';
                    
                    const qrResponse = await fetch('/api/payment/create-momo-qr', {
                        method: 'POST',
                        headers: getAuthHeaders(),
                        body: JSON.stringify({ 
                            orderId: orderResult.bookingId,
                            orderType: pendingItemData.type 
                        })
                    });

                    if (!qrResponse.ok) throw new Error('Không thể tạo mã QR.');

                    const qrData = await qrResponse.json();

                    qrCodeImage.src = qrData.qrCodeUrl; 
                    qrCodeImage.style.display = 'block';
                    qrSpinner.style.display = 'none'; 
                    qrSpinner.classList.remove('visible');

                    startPolling(pendingItemData.type, orderResult.bookingId);

                } catch (qrError) {
                    paymentErrorDiv.textContent = qrError.message;
                    paymentErrorDiv.style.display = 'block';
                    payLaterButton.disabled = false;
                    momoButton.textContent = 'Thanh toán bằng MoMo';
                    momoButton.disabled = false;
                    momoQrModal.style.display = 'none'; 
                }
            } else {
                payLaterButton.disabled = false;
                momoButton.textContent = 'Thanh toán bằng MoMo';
                momoButton.disabled = false;
            }
        });
    }

    // --- 8. HÀM POLLING VÀ MODAL ---
    function startPolling(orderType, orderId) {
        currentPollingOrderId = orderId;
        currentPollingOrderType = orderType;

        if (pollingInterval) clearInterval(pollingInterval);

        pollingInterval = setInterval(async () => {
            if (!currentPollingOrderId) {
                clearInterval(pollingInterval);
                return;
            }
            
            console.log(`Polling status for ${orderType} - ${orderId}...`); // Debug
            
            try {
                const statusResponse = await fetch(`/api/payment/check-momo-status/${orderType}/${orderId}`, {
                    headers: getAuthHeaders()
                });

                if (!statusResponse.ok) {
                    if(statusResponse.status === 401) {
                         clearInterval(pollingInterval);
                         console.warn("Dừng polling do token hết hạn.");
                    }
                    return; 
                }
                
                const statusData = await statusResponse.json();
                
                if (statusData.status === 'PAID') {
                    console.log("Polling result: PAID"); 
                    clearInterval(pollingInterval);
                    momoQrModal.style.display = 'none';
                    showSuccessPage(currentPollingOrderId, true); 
                    currentPollingOrderId = null;
                }
                else if (statusData.status === 'EXPIRED') {
                    console.log("Polling result: EXPIRED"); 
                    clearInterval(pollingInterval);
                    momoQrModal.style.display = 'none';
                    paymentErrorDiv.textContent = 'Phiên thanh toán đã hết hạn. Vui lòng thử lại.';
                    paymentErrorDiv.style.display = 'block';
                    payLaterButton.disabled = false;
                    momoButton.textContent = 'Thanh toán bằng MoMo';
                    momoButton.disabled = false;
                }
                // Nếu là 'PENDING', tiếp tục polling...

            } catch (err) {
                console.warn("Lỗi polling:", err);
            }
        }, 3000); // Kiểm tra mỗi 3 giây
    }

    // Đóng Modal
    if (closeQrModalBtn) {
        closeQrModalBtn.onclick = () => {
            momoQrModal.style.display = 'none';
            clearInterval(pollingInterval); 
            currentPollingOrderId = null;
            payLaterButton.disabled = false;
            momoButton.textContent = 'Thanh toán bằng MoMo';
            momoButton.disabled = false;
        }
    }

    // --- CHẠY HÀM TẢI DỮ LIỆU KHI VÀO TRANG ---
    loadPendingItem();
});