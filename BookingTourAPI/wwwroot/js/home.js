// TRONG FILE: wwwroot/js/home.js

// Hàm Setup Dropdown (Giữ nguyên)
function setupCustomDropdown(containerId, displayId, valueId, menuId) {
    const container = document.getElementById(containerId);
    if (!container) return;
    const display = document.getElementById(displayId);
    const valueInput = document.getElementById(valueId);
    const menu = document.getElementById(menuId);
    if (!display || !valueInput || !menu) return;

    display.addEventListener('click', (e) => {
        e.stopPropagation();
        document.querySelectorAll('.custom-dropdown-menu.show').forEach(m => {
            if (m.id !== menuId) m.classList.remove('show');
        });
        document.querySelectorAll('.custom-dropdown-display.active').forEach(d => {
            if (d.id !== displayId) d.classList.remove('active');
        });
        menu.classList.toggle('show');
        display.classList.toggle('active');
    });

    menu.addEventListener('click', (e) => {
        if (e.target.classList.contains('custom-dropdown-item')) {
            const selectedValue = e.target.getAttribute('data-value');
            const selectedText = e.target.textContent;
            valueInput.value = selectedValue; 
            if (selectedValue === "") {
                const defaultText = display.id.includes('budget') ? "Chọn mức giá" : "Chọn vùng miền";
                display.textContent = defaultText;
                display.classList.remove('selected');
            } else {
                display.textContent = selectedText;
                display.classList.add('selected');
            }
            menu.classList.remove('show');
            display.classList.remove('active');
        }
    });
}

document.addEventListener('DOMContentLoaded', () => {
    
    // === LOGIC MỞ/ĐÓNG MENU (QUAN TRỌNG: ĐÃ THÊM LẠI) ===
    const userIcon = document.getElementById('userIcon');
    const userDropdown = document.getElementById('userDropdown');

    if (userIcon && userDropdown) {
        // Bấm vào icon -> Bật/Tắt menu
        userIcon.addEventListener('click', (e) => {
            e.stopPropagation(); // Ngăn chặn sự kiện lan ra ngoài
            userDropdown.classList.toggle('show');
        });

        // Bấm ra ngoài -> Đóng menu
        window.addEventListener('click', (e) => {
            if (!userIcon.contains(e.target) && !userDropdown.contains(e.target)) {
                userDropdown.classList.remove('show');
            }
        });
    }

    // === KIỂM TRA ĐĂNG NHẬP ===
    checkUserLogin_JWT(); 

    function checkUserLogin_JWT() {
        const token = localStorage.getItem('userToken');
        const email = localStorage.getItem('userEmail');

        if (token && email) {
            updateDropdownForLoggedInUser(email);
            setupSignalRForUser(token);
        } else {
            updateDropdownForGuest();
        }
    }

    function updateDropdownForLoggedInUser(email) {
        if (!userDropdown) return;
        userDropdown.innerHTML = `
            <a href="#" style="color: #333; font-weight: bold; pointer-events: none;">Xin chào, ${email}</a>
            <a href="/html/profile.html">Thông tin cá nhân</a>
            <a href="/html/history.html">Lịch sử</a>
            <a href="#" onclick="openChatBox(); return false;" style="color: #007bff; cursor: pointer;">
                <i class="fa-solid fa-comments"></i> Hỗ trợ trực tuyến
            </a>
            <a href="#" id="logout-button">Đăng xuất</a>
        `;
        
        setTimeout(() => {
            const logoutButton = document.getElementById('logout-button');
            if (logoutButton) {
                logoutButton.addEventListener('click', (e) => {
                    e.preventDefault();
                    localStorage.removeItem('userToken');
                    localStorage.removeItem('userEmail');
                    localStorage.removeItem('userChatMessages');
                    window.location.reload();
                });
            }
        }, 0);
    }

    function updateDropdownForGuest() {
        if (!userDropdown) return;
        userDropdown.innerHTML = `
            <a href="/html/login.html">Đăng nhập</a>
            <a href="/html/register.html">Đăng ký</a>
        `;
    }

    // ============================================================
    // 2. LOGIC RIÊNG CHO TRANG CHỦ (CHỈ CHẠY KHI CÓ ELEMENT)
    // ============================================================

    // --- Tab chuyển đổi Form ---
    const tabButtons = document.querySelectorAll('.tab-btn');
    const searchForms = document.querySelectorAll('.search-form');
    if (tabButtons.length > 0) {
        tabButtons.forEach(button => {
            button.addEventListener('click', () => {
                tabButtons.forEach(btn => btn.classList.remove('active'));
                searchForms.forEach(form => form.classList.remove('active'));
                button.classList.add('active');
                const formId = button.getAttribute('data-form');
                const form = document.getElementById(formId);
                if(form) form.classList.add('active');
            });
        });
    }

    // --- Tour Nổi Bật & Điểm Đến (Chỉ tải nếu đang ở trang chủ) ---
    if (document.getElementById('top-destinations-grid')) {
        loadTopDestinations_Public();
    }
    if (document.getElementById('featured-tours-grid')) {
        loadFeaturedTours();
    }

    // --- Dropdown Ngân sách/Vùng miền ---
    setupCustomDropdown('tour-form-home', 'tour-budget-display', 'tour-budget-value', 'tour-budget-menu');
    setupCustomDropdown('tour-form-home', 'tour-area-display', 'tour-area-value', 'tour-area-menu');

    // --- Xử lý Submit Form Trang Chủ ---
    const tourFormHome = document.getElementById('tour-form-home');
    if (tourFormHome) {
        tourFormHome.addEventListener('submit', (e) => {
            e.preventDefault();
            const params = {
                type: 'tour',
                location: document.getElementById('tour-location').value,
                date: document.getElementById('tour-date').value,
                budget: document.getElementById('tour-budget-value').value, 
                area: document.getElementById('tour-area-value').value
            };
            localStorage.setItem('searchParams', JSON.stringify(params));
            window.location.href = '/html/tours.html';
        });
    }

    const flightForm = document.getElementById('flight-form-home');
    if (flightForm) {
        flightForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const params = {
                type: 'flight',
                origin: document.getElementById('flight-origin').value,
                destination: document.getElementById('flight-dest').value,
                departureDate: document.getElementById('flight-depart').value,
                returnDate: document.getElementById('flight-return').value,
                adults: document.getElementById('flight-adults').value,
                children: document.getElementById('flight-children').value
            };
            localStorage.setItem('searchParams', JSON.stringify(params));
            window.location.href = '/html/flights.html';
        });
    }

    // --- Logic đóng Modal chung ---
    const modal = document.getElementById('details-modal');
    window.addEventListener('click', function(event) {
        if (modal && event.target == modal) {
            modal.style.display = 'none';
        }
    });

    // ============================================================
    // 3. CÁC HÀM HỖ TRỢ
    // ============================================================

    async function loadTopDestinations_Public() {
        const grid = document.getElementById('top-destinations-grid');
        if (!grid) return;
        try {
            const response = await fetch('/api/public/cms/top-destinations');
            if (!response.ok) throw new Error('Cannot load destinations');
            const destinations = await response.json();
            grid.innerHTML = ''; 
            if (destinations.length === 0) {
                grid.innerHTML = '<p>Chưa có điểm đến nào.</p>';
                return;
            }
            destinations.forEach(dest => {
                const card = document.createElement('a');
                card.className = 'dest-card';
                card.href = "#"; 
                card.setAttribute('data-city', dest.searchTerm);
                card.addEventListener('click', (e) => {
                    e.preventDefault();
                    const params = { type: dest.type, city: dest.searchTerm, location: dest.searchTerm };
                    let targetUrl = dest.type === 'tour' ? '/html/tours.html' : '/html/hotels.html';
                    localStorage.setItem('searchParams', JSON.stringify(params));
                    window.location.href = targetUrl;
                });
                const firstImage = (dest.imageUrl || '').split('\n')[0].trim();
                card.innerHTML = `<img src="${firstImage || 'https://placehold.co/300x380'}" alt="${dest.cityName}"><div class="dest-overlay"><div class="dest-name">${dest.cityName}</div></div>`;
                grid.appendChild(card);
            });
        } catch (error) { console.error(error); }
    }

    async function loadFeaturedTours() {
        const grid = document.getElementById('featured-tours-grid');
        if (!grid) return;
        try {
            const response = await fetch('/api/public/cms/tours');
            if (!response.ok) throw new Error('Lỗi tải tour');
            const allTours = await response.json();
            const featuredTours = allTours.filter(t => t.price > 0).sort((a, b) => a.price - b.price).slice(0, 8);
            grid.innerHTML = '';
            featuredTours.forEach(tour => {
                const card = document.createElement('div');
                card.className = 'activity-card';
                const firstImage = (tour.imageUrl || '').split('\n')[0].trim();
                const imageUrl = firstImage || 'https://placehold.co/300x180?text=Tour';
                const priceText = `${tour.price.toLocaleString('vi-VN')} VND`;
                card.innerHTML = `
                    <img src="${imageUrl}" alt="${tour.title}" style="width: 100%; height: 180px; object-fit: cover; border-bottom: 1px solid #eee;">
                    <div class="activity-content" style="padding: 15px; display: flex; flex-direction: column; flex-grow: 1;">
                        <h3 style="font-size: 18px; margin: 0 0 10px 0; height: 45px; overflow: hidden;">${tour.title}</h3>
                        <p style="font-size: 13px; color: #666; margin-bottom: 10px;"><i class="bi bi-clock"></i> ${tour.duration || 'N/A'}</p>
                    </div>
                    <div class="activity-footer" style="padding: 15px; background-color: #f8f9fa; border-top: 1px solid #eee; display: flex; justify-content: space-between; align-items: center;">
                        <div style="font-weight: bold; color: #d9534f; font-size: 18px;">${priceText}</div>
                        <a href="/html/tour-details.html?id=${tour.id}" class="select-button" style="background-color: #28a745; color: white; padding: 8px 12px; border-radius: 4px; text-decoration: none; font-size: 14px;">Xem ngay</a>
                    </div>
                `;
                grid.appendChild(card);
            });
        } catch (error) { console.error(error); }
    }

    function setupSignalRForUser(token) {
        if (typeof signalR === 'undefined') return; 
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/userHub", { accessTokenFactory: () => token })
            .withAutomaticReconnect()
            .build();
        connection.on("OrderConfirmed", (message, orderId) => {
            alert(message); 
            if (window.location.pathname.includes('/history.html') && typeof updateOrderStatusOnPage === 'function') {
                updateOrderStatusOnPage(orderId, 'CONFIRMED');
            }
        });
        connection.on("AccountLocked", (message) => {
            alert(message);
            localStorage.removeItem('userToken');
            window.location.href = '/html/login.html';
        });
        connection.start().catch(err => console.error(err));
    }
});