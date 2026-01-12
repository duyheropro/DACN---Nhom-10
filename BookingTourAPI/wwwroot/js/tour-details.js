// TRONG FILE: wwwroot/js/tour-details.js

let currentSelectedDep = null; // Biến toàn cục để lưu ngày đang chọn

document.addEventListener('DOMContentLoaded', () => {
    
    // Lấy các element trên trang
    const loadingMessage = document.getElementById('loading-message');
    const errorMessage = document.getElementById('error-message');
    const contentDiv = document.getElementById('tour-details-content');

    let currentTourData = null; // Biến lưu trữ data tour gốc
    let currentTourDepartures = []; // Biến lưu danh sách ngày khởi hành
    let selectedDepartureId = null; // ID của ngày đang chọn

    // --- HÀM TẢI CHI TIẾT TOUR ---
    async function loadTourDetails() {
        const params = new URLSearchParams(window.location.search);
        const tourId = params.get('id');

        if (!tourId) {
            showError('Không tìm thấy ID tour. Vui lòng thử lại.');
            return;
        }

        try {
            const response = await fetch(`/api/public/cms/tours/${tourId}`);
            if (!response.ok) {
                throw new Error(`Lỗi ${response.status}: Không thể tải chi tiết tour.`);
            }
            
            const tour = await response.json();
            currentTourData = tour;
            
            displayTourDetails(tour);

        } catch (error) {
            showError(error.message);
        }
    }

    // --- HÀM HIỂN THỊ DỮ LIỆU ---
    function displayTourDetails(tour) {
        loadingMessage.style.display = 'none';
        contentDiv.style.display = 'block';

        // Thông tin cơ bản
        document.getElementById('tour-title').textContent = tour.title;
        
        // Giá hiển thị ban đầu (nếu có)
        const priceEl = document.getElementById('tour-price');
        if(priceEl) {
             priceEl.textContent = `${(tour.price || 0).toLocaleString('vi-VN')} VND`;
        }

        // Điểm nổi bật & Chính sách
        document.getElementById('highlights-container').innerHTML = formatText(tour.highlights);
        document.getElementById('policy-includes-container').innerHTML = formatText(tour.policyIncludes);
        document.getElementById('policy-excludes-container').innerHTML = formatText(tour.policyExcludes);

        // Lịch trình
        const itineraryContainer = document.getElementById('itinerary-container');
        itineraryContainer.innerHTML = '';
        if (tour.itineraries && tour.itineraries.length > 0) {
            tour.itineraries
                .sort((a, b) => a.dayNumber - b.dayNumber)
                .forEach(day => {
                    const dayElement = document.createElement('div');
                    dayElement.className = 'itinerary-day';
                    dayElement.innerHTML = `
                        <h4>${day.title} (Ngày ${day.dayNumber})</h4>
                        <div>${formatText(day.description)}</div>
                    `;
                    itineraryContainer.appendChild(dayElement);
                });
        } else {
            itineraryContainer.innerHTML = '<p>Chưa có lịch trình chi tiết cho tour này.</p>';
        }

        // --- HIỂN THỊ ẢNH (GALERY) ---
        const galleryContainer = document.getElementById('tour-image-gallery');
        galleryContainer.innerHTML = '';

        const imageUrls = (tour.imageUrl || '')
            .split('\n')
            .filter(url => url.trim() !== '');

        if (imageUrls.length > 0) {
            const mainImage = document.createElement('img');
            mainImage.className = 'tour-gallery-main-image';
            mainImage.src = imageUrls[0];
            galleryContainer.appendChild(mainImage);

            if (imageUrls.length > 1) {
                const thumbnailsContainer = document.createElement('div');
                thumbnailsContainer.className = 'tour-gallery-thumbnails';
                
                imageUrls.forEach(url => {
                    const thumb = document.createElement('img');
                    thumb.src = url;
                    thumb.addEventListener('click', () => {
                        mainImage.src = url;
                    });
                    thumbnailsContainer.appendChild(thumb);
                });
                galleryContainer.appendChild(thumbnailsContainer);
            }
        } else {
            galleryContainer.innerHTML = '<img src="https://placehold.co/800x400" alt="Chưa có ảnh" class="tour-gallery-main-image">';
        }

        // --- HIỂN THỊ LỊCH KHỞI HÀNH ---
        currentTourDepartures = tour.departures || [];
        
        if (currentTourDepartures.length > 0) {
            renderMonthTabs();
            // Mặc định chọn tháng đầu tiên
            const firstDate = new Date(currentTourDepartures[0].startDate);
            filterDeparturesByMonth(firstDate.getMonth(), firstDate.getFullYear());
        } else {
            const depList = document.getElementById('departure-list');
            if(depList) depList.innerHTML = '<p>Chưa có lịch khởi hành cho tour này.</p>';
        }
    }

    // --- CÁC HÀM XỬ LÝ LỊCH KHỞI HÀNH ---

    function renderMonthTabs() {
        const tabsContainer = document.getElementById('month-tabs');
        if(!tabsContainer) return;
        tabsContainer.innerHTML = '';
        
        // Lấy danh sách các tháng duy nhất (Format: "MM/YYYY")
        const months = [...new Set(currentTourDepartures.map(d => {
            const date = new Date(d.startDate);
            return `${date.getMonth() + 1}/${date.getFullYear()}`;
        }))];

        months.forEach((m, index) => {
            const btn = document.createElement('button');
            btn.className = `month-btn ${index === 0 ? 'active' : ''}`;
            btn.textContent = m;
            btn.onclick = () => {
                document.querySelectorAll('.month-btn').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                
                const [month, year] = m.split('/');
                filterDeparturesByMonth(parseInt(month) - 1, parseInt(year));
            };
            tabsContainer.appendChild(btn);
        });
    }

    // Hàm filterDeparturesByMonth đã cập nhật logic khóa
    function filterDeparturesByMonth(monthIndex, year) {
        const listContainer = document.getElementById('departure-list');
        if(!listContainer) return;
        listContainer.innerHTML = '';
        
        const filtered = currentTourDepartures.filter(d => {
            const date = new Date(d.startDate);
            return date.getMonth() === monthIndex && date.getFullYear() === year;
        });
        
        filtered.forEach(dep => {
            const date = new Date(dep.startDate);
            const div = document.createElement('div');
            
            // Kiểm tra logic khóa: từ API (isLocked) hoặc hết chỗ
            const isLocked = dep.isLocked || dep.availableSeats <= 0;
            
            div.className = `departure-item ${isLocked ? 'locked' : ''}`;
            
            let statusText = isLocked ? '<br><span style="font-size:10px; color:red;">(Đã khóa/Đầy)</span>' : '';

            div.innerHTML = `
                <span class="dep-date">${date.getDate()}/${date.getMonth() + 1}</span>
                <span class="dep-price">${dep.priceAdult.toLocaleString()}đ</span>
                ${statusText}
            `;
            
            if (!isLocked) {
                div.onclick = () => selectDeparture(dep, div);
            }
            listContainer.appendChild(div);
        });
    }

    // Hàm selectDeparture đã cập nhật reset input và tính giá
    function selectDeparture(departureData, element) {
        currentSelectedDep = departureData; // Lưu biến toàn cục
        selectedDepartureId = departureData.id;
        
        // Highlight ô được chọn
        document.querySelectorAll('.departure-item').forEach(el => el.classList.remove('selected'));
        element.classList.add('selected');
        
        const infoBox = document.getElementById('selected-departure-info');
        if(infoBox) infoBox.style.display = 'block';
        
        const startDate = new Date(departureData.startDate);
        const endDate = departureData.endDate ? new Date(departureData.endDate) : startDate;
        
        document.getElementById('selected-date-display').textContent = startDate.toLocaleDateString('vi-VN');
        
        // Thông tin bay
        document.getElementById('flight-start-date').textContent = startDate.toLocaleDateString('vi-VN');
        document.getElementById('flight-end-date').textContent = endDate.toLocaleDateString('vi-VN');
        document.getElementById('flight-code-out').textContent = departureData.flightNumberOut || 'VNA';
        document.getElementById('flight-code-in').textContent = departureData.flightNumberIn || 'VNA';
        document.getElementById('flight-time-out').textContent = departureData.flightTimeOut || 'Giờ đi';
        document.getElementById('flight-time-in').textContent = departureData.flightTimeIn || 'Giờ về';

        // Bảng giá chi tiết (Đơn giá)
        document.getElementById('price-adult').textContent = `${departureData.priceAdult.toLocaleString()} đ`;
        document.getElementById('price-child').textContent = `${departureData.priceChild.toLocaleString()} đ`;
        document.getElementById('price-infant').textContent = `${departureData.priceInfant.toLocaleString()} đ`;
        document.getElementById('seats-available').textContent = departureData.availableSeats + " khách";

        // Reset số lượng về mặc định khi chọn ngày mới
        document.getElementById('qty-adult').value = 1;
        document.getElementById('qty-child').value = 0;
        document.getElementById('qty-infant').value = 0;

        // Tính tổng tiền ngay lập tức
        calculateTotal();

        // Cập nhật nút Book
        const bookBtn = document.getElementById('book-now-button');
        if(bookBtn) {
            const newBtn = bookBtn.cloneNode(true);
            bookBtn.parentNode.replaceChild(newBtn, bookBtn);
            newBtn.addEventListener('click', () => {
                handleBooking(departureData);
            });
        }
    }

    // Hàm handleBooking đã cập nhật gửi số lượng sang Payment
    function handleBooking(departureData) {
        const token = localStorage.getItem('userToken');
        if (!token) {
            alert('Vui lòng đăng nhập để đặt tour. Đang chuyển hướng đến trang đăng nhập...');
            localStorage.setItem('redirectAfterLogin', window.location.href);
            window.location.href = '/html/login.html';
            return; 
        }

        if (!currentTourData) {
            alert('Lỗi: Dữ liệu tour chưa sẵn sàng.');
            return;
        }

        // Lấy số lượng thực tế từ input
        const numAdults = parseInt(document.getElementById('qty-adult').value) || 0;
        const numChildren = parseInt(document.getElementById('qty-child').value) || 0;
        const numInfants = parseInt(document.getElementById('qty-infant').value) || 0;

        // Tính lại tổng giá cuối cùng để gửi sang trang thanh toán
        const finalPrice = (numAdults * departureData.priceAdult) + 
                           (numChildren * departureData.priceChild) + 
                           (numInfants * departureData.priceInfant);

        const paymentInfo = {
            type: 'tour',
            item: {
                ...currentTourData,
                price: finalPrice, // Ghi đè giá tổng
                departureId: departureData.id,
                startDate: departureData.startDate,
                endDate: departureData.endDate,
                
                // QUAN TRỌNG: Lưu số lượng để tạo đơn
                quantities: {
                    adults: numAdults,
                    children: numChildren,
                    infants: numInfants
                },

                prices: {
                    adult: departureData.priceAdult,
                    child: departureData.priceChild,
                    infant: departureData.priceInfant
                },
                flightInfo: {
                    airline: departureData.airline,
                    out: { number: departureData.flightNumberOut, time: departureData.flightTimeOut },
                    in: { number: departureData.flightNumberIn, time: departureData.flightTimeIn }
                }
            }
        };

        localStorage.setItem('pendingPayment', JSON.stringify(paymentInfo));
        window.location.href = '/html/payment.html';
    }

    function showError(message) {
        loadingMessage.style.display = 'none';
        errorMessage.textContent = message;
        errorMessage.style.display = 'block';
    }

    function formatText(text) {
        if (!text) return '<p><i>Chưa cập nhật thông tin.</i></p>';
        return '<p>' + text.replace(/\n/g, '<br>') + '</p>';
    }

    // --- KHỞI CHẠY ---
    loadTourDetails();
});

// --- HÀM TÍNH TỔNG TIỀN (GẮN VÀO WINDOW) ---
// Hàm này cần nằm ngoài DOMContentLoaded để HTML onchange gọi được
window.calculateTotal = function() {
    if (!currentSelectedDep) return;

    const a = parseInt(document.getElementById('qty-adult').value) || 0;
    const c = parseInt(document.getElementById('qty-child').value) || 0;
    const i = parseInt(document.getElementById('qty-infant').value) || 0;

    // Validate số chỗ (Người lớn + Trẻ em chiếm ghế)
    const totalSeatsRequired = a + c; 
    
    if (totalSeatsRequired > currentSelectedDep.availableSeats) {
        alert(`Chỉ còn ${currentSelectedDep.availableSeats} chỗ nhận! Vui lòng giảm số lượng.`);
        // Reset về mặc định nếu vượt quá
        document.getElementById('qty-adult').value = 1;
        document.getElementById('qty-child').value = 0;
        // Gọi lại để cập nhật giá về mặc định
        window.calculateTotal(); 
        return;
    }

    const total = (a * currentSelectedDep.priceAdult) + 
                  (c * currentSelectedDep.priceChild) + 
                  (i * currentSelectedDep.priceInfant);

    const totalEl = document.getElementById('total-booking-price');
    if(totalEl) {
        totalEl.textContent = total.toLocaleString('vi-VN') + " VND";
    }
};

// --- SIGNALR REALTIME SEATS UPDATE ---
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/publicHub")
    .withAutomaticReconnect()
    .build();

connection.on("UpdateSeats", (departureId, newSeats) => {
    // 1. Cập nhật dữ liệu trong biến toàn cục
    const dep = currentTourDepartures.find(d => d.id === departureId);
    if (dep) {
        dep.availableSeats = newSeats;
        
        // 2. Nếu đang chọn đúng ngày này -> Cập nhật hiển thị ngay lập tức
        if (selectedDepartureId === departureId) {
            const seatsEl = document.getElementById('seats-available');
            if (seatsEl) {
                seatsEl.textContent = newSeats + " khách";
                // Hiệu ứng nhấp nháy để người dùng thấy thay đổi
                seatsEl.style.color = "red";
                setTimeout(() => seatsEl.style.color = "green", 500);
            }
            // Gọi lại calculateTotal để check xem số lượng đang chọn có lố không
            if (typeof window.calculateTotal === 'function') {
                window.calculateTotal(); 
            }
        }
    }
});

connection.start().then(() => {
    // Lấy tourId từ URL và Join Group
    const params = new URLSearchParams(window.location.search);
    const tourId = params.get('id');
    if (tourId) {
        connection.invoke("JoinTourGroup", tourId.toString());
    }
}).catch(err => console.error(err));