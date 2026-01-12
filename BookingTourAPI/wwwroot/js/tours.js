document.addEventListener('DOMContentLoaded', () => {
    // --- KHAI BÁO BIẾN ---
    const searchForm = document.getElementById('tour-search-form');
    const errorDiv = document.getElementById('error');
    const resultsDiv = document.getElementById('results');
    
    // Các trường lọc (từ form sidebar mới)
    const locationInput = document.getElementById('tour-location-sidebar');
    const dateInput = document.getElementById('tour-date-sidebar');
    const budgetInput = document.getElementById('tour-budget-value-sidebar'); // Lấy input ẩn
    const areaInput = document.getElementById('tour-area-value-sidebar');
    // Nút sắp xếp
    const sortDescBtn = document.getElementById('sort-tours-desc');
    const sortAscBtn = document.getElementById('sort-tours-asc');

    let allCmsTours = []; // Lưu trữ tất cả tour gốc từ CMS
    let currentFilteredTours = []; // Lưu trữ tour đã lọc (để sắp xếp)
    let currentSortIsDesc = false; // Trạng thái sắp xếp hiện tại

    // --- 1. HÀM TẢI TẤT CẢ TOUR (CHỈ CHẠY 1 LẦN) ---
    async function fetchAllCmsTours() {
        errorDiv.textContent = '';
        resultsDiv.innerHTML = 'Đang tải các tour hấp dẫn nhất...';
        
        try {
            // Thay đổi: Thêm cache-busting query
            const response = await fetch(`/api/public/cms/tours?_=${new Date().getTime()}`);
            if (!response.ok) {
                throw new Error(`Lỗi ${response.status}: Không thể tải danh sách tour`);
            }
            const tourPackages = await response.json();

            if (!tourPackages || tourPackages.length === 0) {
                allCmsTours = [];
                currentFilteredTours = [];
                resultsDiv.innerHTML = '<p>Chưa có gói tour nào được mở bán.</p>';
                return;
            }
            
            allCmsTours = tourPackages; // Lưu data gốc
            currentFilteredTours = [...allCmsTours]; // Ban đầu, tất cả tour đều được hiển thị
            
            // Tự động tìm kiếm nếu có tham số từ trang chủ
            autoSearchFromHome(); 

        } catch (error) {
            console.error('Lỗi tải CMS Tours:', error);
            resultsDiv.innerHTML = '';
            errorDiv.textContent = `Lỗi: ${error.message}`;
            allCmsTours = [];
        }
    }

    // --- 2. LOGIC AUTO SEARCH ---
    function autoSearchFromHome() {
        const params = localStorage.getItem('searchParams');
        let searchLocation = '';
        let searchBudget = '';
        let searchDate = '';
        let searchArea = ''; 

        if (params) {
            try {
                const searchData = JSON.parse(params);
                if (searchData.type === 'tour') {
                    searchLocation = searchData.location || '';
                    searchBudget = searchData.budget || '';
                    searchDate = searchData.date || '';
                    searchArea = searchData.area || ''; 

                    if (locationInput) locationInput.value = searchLocation;
                    if (dateInput) dateInput.value = searchDate;
                    
                    // Xử lý điền dropdown ngân sách
                    const budgetValueInput = document.getElementById('tour-budget-value-sidebar');
                    const budgetDisplay = document.getElementById('tour-budget-display-sidebar');
                    if (budgetValueInput && budgetDisplay) {
                        budgetValueInput.value = searchBudget;
                        const selectedItem = document.querySelector(`#tour-budget-menu-sidebar .custom-dropdown-item[data-value="${searchBudget}"]`);
                        if (selectedItem) {
                            budgetDisplay.textContent = selectedItem.textContent;
                            budgetDisplay.classList.add('selected');
                        } else {
                            budgetDisplay.textContent = "Chọn mức giá";
                            budgetDisplay.classList.remove('selected');
                        }
                    }

                    // Xử lý điền dropdown vùng miền
                    const areaValueInput = document.getElementById('tour-area-value-sidebar');
                    const areaDisplay = document.getElementById('tour-area-display-sidebar');
                    if (areaValueInput && areaDisplay) {
                        areaValueInput.value = searchArea;
                        const selectedItem = document.querySelector(`#tour-area-menu-sidebar .custom-dropdown-item[data-value="${searchArea}"]`);
                        if (selectedItem) {
                            areaDisplay.textContent = selectedItem.textContent;
                            areaDisplay.classList.add('selected');
                        } else {
                            areaDisplay.textContent = "Chọn vùng miền";
                            areaDisplay.classList.remove('selected');
                        }
                    }
                }
            } catch (e) {
                console.error("Lỗi parse searchParams:", e);
            } finally {
                localStorage.removeItem('searchParams');
            }
        }
        
        // Chạy lọc VÀ sắp xếp
        filterAndSortTours(searchLocation, searchBudget, searchArea, currentSortIsDesc);
    }
    
    // TRONG FILE: wwwroot/js/tours.js

    // --- 3. HÀM LỌC VÀ SẮP XẾP (ĐÃ SỬA) ---
    function filterAndSortTours(location, budget, area, isDescending) {
        let filteredTours = [...allCmsTours]; // Bắt đầu với tất cả tour
        
        const normLocation = location ? location.toLowerCase().trim() : '';
        const normArea = area ? area.toLowerCase().trim() : ''; 

        // 1. Lọc theo Từ khóa
        if (normLocation) {
            filteredTours = filteredTours.filter(tour => 
                (tour.title && tour.title.toLowerCase().includes(normLocation)) ||
                (tour.region && tour.region.toLowerCase().includes(normLocation)) ||
                (tour.country && tour.country.toLowerCase().includes(normLocation))
            );
        }

        // 2. Lọc theo Ngân sách (LOGIC MỚI: Hỗ trợ khoảng giá min-max)
        if (budget && budget.includes('-')) {
            // Nếu budget có dạng "5000000-10000000"
            const parts = budget.split('-');
            const minPrice = parseFloat(parts[0]);
            const maxPrice = parseFloat(parts[1]);

            filteredTours = filteredTours.filter(tour => 
                tour.price >= minPrice && tour.price <= maxPrice
            );
        } else if (budget) {
            // Fallback cho trường hợp cũ (nếu có)
            const maxBudget = parseFloat(budget);
            if (!isNaN(maxBudget) && maxBudget > 0) {
                 filteredTours = filteredTours.filter(tour => tour.price <= maxBudget);
            }
        }
        
        // 3. Lọc theo Vùng miền
        if (normArea) {
            filteredTours = filteredTours.filter(tour => 
                (tour.area && tour.area.toLowerCase() === normArea)
            );
        }
        
        currentFilteredTours = filteredTours; // Lưu kết quả lọc
        
        // Sắp xếp và hiển thị
        sortAndDisplayTours(isDescending);
    }
    
    // --- 4. HÀM SẮP XẾP VÀ HIỂN THỊ ---
    function sortAndDisplayTours(isDescending) {
        currentSortIsDesc = isDescending; // Cập nhật trạng thái sắp xếp
        
        // Sắp xếp trên danh sách ĐÃ LỌC
        const sortedTours = [...currentFilteredTours].sort((a, b) => {
            const priceA = (a.price > 0) ? a.price : (isDescending ? -Infinity : Infinity);
            const priceB = (b.price > 0) ? b.price : (isDescending ? -Infinity : Infinity);
            
            return isDescending ? priceB - priceA : priceA - priceB;
        });

        // Cập nhật trạng thái active cho nút
        if (isDescending) {
            if(sortDescBtn) sortDescBtn.classList.add('active');
            if(sortAscBtn) sortAscBtn.classList.remove('active');
        } else {
            if(sortDescBtn) sortDescBtn.classList.remove('active');
            if(sortAscBtn) sortAscBtn.classList.add('active');
        }
        
        // Hiển thị kết quả đã lọc VÀ sắp xếp
        displayCmsTours(sortedTours);
    }

    // --- 5. HÀM HIỂN THỊ ---
    function displayCmsTours(tours) {
        resultsDiv.innerHTML = ''; // Xóa nội dung cũ

        if (tours.length === 0) {
            resultsDiv.innerHTML = '<p>Không tìm thấy gói tour nào phù hợp với yêu cầu của bạn.</p>';
            return;
        }

        tours.forEach(tour => {
            const tourElement = document.createElement('div');
            tourElement.className = 'activity-card';

            const priceText = (tour.price > 0) ? `${tour.price.toLocaleString('vi-VN')} VND` : 'Giá liên hệ';
            // Lấy ảnh đầu tiên trong danh sách (nếu có)
            const firstImage = (tour.imageUrl || '').split('\n')[0].trim();
            const imageUrl = firstImage || 'https://placehold.co/300x180/e9ecef/6c757d?text=Tour';

            tourElement.innerHTML = `
                <img src="${imageUrl}" alt="${tour.title}">
                <div class="activity-content">
                    <h3>${tour.title}</h3>
                    <p class="description" style="height: auto; font-size: 13px; line-height: 1.5;">
                        <strong>Thời lượng:</strong> ${tour.duration || 'N/A'}<br>
                        <strong>Điểm nổi bật:</strong> ${tour.highlights ? tour.highlights.substring(0, 80) + '...' : 'N/A'}
                    </p>
                </div>
                <div class="activity-footer">
                    <div>
                        <span class="price-note" style="font-size: 12px;">Giá trọn gói</span>
                        <div class="price">${priceText}</div>
                    </div>
                    <button class="select-button tour-details-btn" data-id="${tour.id}">
                        Xem Chi Tiết
                    </button>
                </div>
            `;
            resultsDiv.appendChild(tourElement);
        });
    }

    // --- 6. GẮN SỰ KIỆN CHO FORM LỌC ---
    if (searchForm) {
        searchForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const location = locationInput.value;
            const budget = budgetInput.value; // Đọc từ input ẩn
            const area = areaInput.value; // Đọc giá trị area
            
            // Gọi hàm lọc và sắp xếp
            filterAndSortTours(location, budget, area, currentSortIsDesc); 
        });
    }
    
    // --- 7. GẮN SỰ KIỆN CHO NÚT SẮP XẾP ---
    if (sortDescBtn) {
        sortDescBtn.addEventListener('click', () => sortAndDisplayTours(true));
    }
    if (sortAscBtn) {
        sortAscBtn.addEventListener('click', () => sortAndDisplayTours(false));
    }


    // --- 8. GẮN EVENT LISTENER CHO NÚT "Xem Chi Tiết" ---
    if (resultsDiv) {
        resultsDiv.addEventListener('click', function(event) {
            // Check cả nút hoặc thẻ <i> bên trong nút
            const button = event.target.closest('.tour-details-btn');
            if (button) {
                const tourId = button.getAttribute('data-id');
                if (tourId) {
                    window.location.href = `/html/tour-details.html?id=${tourId}`;
                } else {
                    console.error("Không tìm thấy data-id trên nút");
                }
            }
        });
    }

    // --- 9. CHẠY HÀM TẢI TOUR KHI MỞ TRANG ---
    fetchAllCmsTours(); // Tải tất cả tour trước

    // --- 10. LOGIC DROPDOWN CHO SIDEBAR ---
    // Kiểm tra xem hàm `setupCustomDropdown` (từ home.js) đã tồn tại chưa
    if (typeof setupCustomDropdown === 'function') {
         // Khởi tạo dropdown cho form sidebar
         setupCustomDropdown(
            'tour-search-form', 
            'tour-budget-display-sidebar', 
            'tour-budget-value-sidebar', 
            'tour-budget-menu-sidebar'
        );
        setupCustomDropdown(
           'tour-search-form',
           'tour-area-display-sidebar',
           'tour-area-value-sidebar',
           'tour-area-menu-sidebar'
        );
    } else {
        // Đây là lỗi bạn đã thấy, nhưng nó sẽ không xảy ra nữa
        // vì Bước 1 đã di chuyển hàm ra toàn cục.
        console.error("Lỗi: hàm setupCustomDropdown từ home.js chưa được tải.");
    }
});