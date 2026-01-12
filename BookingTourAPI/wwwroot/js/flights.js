document.addEventListener('DOMContentLoaded', () => {
    // --- KHAI BÁO BIẾN ---
    const searchForm = document.getElementById('flight-search-form');
    const errorDiv = document.getElementById('error');
    const resultsDiv = document.getElementById('results');
    const modal = document.getElementById('details-modal');
    // Sửa lại cách lấy modal body và close button cho an toàn hơn
    const modalBody = modal ? modal.querySelector('#modal-body') : null;
    const closeButton = modal ? modal.querySelector('.close-button') : null;
    let flightDataStore = []; // Lưu trữ kết quả gốc

    // --- HÀM TRỢ GIÚP LẤY HEADER (NẾU CẦN CHO API KHÁC) ---
    function getAuthHeaders() {
        const token = localStorage.getItem('userToken');
        const headers = { 'Content-Type': 'application/json' };
        if (token) { headers['Authorization'] = 'Bearer ' + token; }
        return headers;
    }

    // --- LOGIC AUTO SEARCH ---
    function autoSearchFromHome() {
        const params = localStorage.getItem('searchParams');
        if (params) {
            try {
                const searchData = JSON.parse(params);
                if (searchData.type === 'flight') {
                    document.getElementById('origin').value = searchData.origin || '';
                    document.getElementById('destination').value = searchData.destination || '';
                    document.getElementById('departureDate').value = searchData.departureDate || '';
                    document.getElementById('returnDate').value = searchData.returnDate || '';
                    document.getElementById('adults').value = searchData.adults || 1;
                    document.getElementById('children').value = searchData.children || 0;
                    document.getElementById('travelClass').value = searchData.travelClass || 'ECONOMY';
                    // Tự động submit nếu có đủ thông tin
                    if (searchForm && searchData.origin && searchData.destination && searchData.departureDate) {
                         searchForm.requestSubmit();
                    }
                }
            } catch (e) {
                console.error("Lỗi parse searchParams:", e);
            } finally {
                localStorage.removeItem('searchParams');
            }
        }
    }

    // --- HÀM XỬ LÝ TÌM KIẾM ---
     async function handleSearch(event) {
        if(event) event.preventDefault();

        const originInput = document.getElementById('origin').value.toLowerCase();
        const destinationInput = document.getElementById('destination').value.toLowerCase();

        // Sử dụng cityCodeMap hoặc giữ nguyên nếu là IATA code
        const origin = cityCodeMap[originInput] || originInput.toUpperCase();
        const destination = cityCodeMap[destinationInput] || destinationInput.toUpperCase();

        const departureDate = document.getElementById('departureDate').value;
        const returnDate = document.getElementById('returnDate').value;
        const adults = document.getElementById('adults').value;
        const children = document.getElementById('children').value || 0;
        const travelClass = document.getElementById('travelClass').value;

         // Kiểm tra ngày hợp lệ
        if (!origin || !destination || !departureDate || !adults) {
            errorDiv.textContent = 'Vui lòng điền đầy đủ Điểm đi, Điểm đến, Ngày đi và Số người lớn.';
            resultsDiv.innerHTML = '';
            flightDataStore = [];
            return;
        }
        if (returnDate && new Date(returnDate) < new Date(departureDate)) {
             errorDiv.textContent = 'Ngày về phải sau hoặc cùng ngày đi.';
             resultsDiv.innerHTML = '';
             flightDataStore = [];
             return;
        }

        errorDiv.textContent = '';
        resultsDiv.innerHTML = 'Đang tìm chuyến bay...';
        flightDataStore = [];

        let queryString = `originLocationCode=${origin}&destinationLocationCode=${destination}&departureDate=${departureDate}&adults=${adults}&children=${children}&travelClass=${travelClass}`;
        if (returnDate) {
            queryString += `&returnDate=${returnDate}`;
        }
        queryString += '&max=50'; // Giới hạn kết quả

        try {
            const response = await fetch(`/api/Amadeus/flights?${queryString}`);
            if (!response.ok) {
                const text = await response.text();
                throw new Error(`Lỗi ${response.status}: ${text || 'Không thể lấy dữ liệu chuyến bay'}`);
            }
            const data = await response.json();

            resultsDiv.innerHTML = ''; // Xóa "Searching..."
            if (!data.data || data.data.length === 0) {
                flightDataStore = [];
                displayResults([]); // Hiển thị rỗng
                return;
            }

            flightDataStore = data.data; // Lưu data gốc
            sortAndDisplayFlights(false); // Sắp xếp và hiển thị mặc định (giá thấp)

            // Đặt nút 'Giá thấp nhất' thành active mặc định
            if (sortAscBtn) sortAscBtn.classList.add('active');
            if (sortDescBtn) sortDescBtn.classList.remove('active');

        } catch (error) {
            console.error('Lỗi tìm kiếm chuyến bay:', error);
            resultsDiv.innerHTML = '';
            errorDiv.textContent = `Lỗi: ${error.message}`;
            flightDataStore = [];
        }
    }

    // --- HÀM SẮP XẾP VÀ GỌI HIỂN THỊ ---
    function sortAndDisplayFlights(descending = false) {
        console.log("sortAndDisplayFlights called with descending:", descending); // DEBUG
        if (!flightDataStore || flightDataStore.length === 0) {
             displayResults([]);
            return;
        }
        const sortedData = [...flightDataStore]; // Tạo bản sao

        sortedData.sort((a, b) => {
            const priceA = parseFloat(a.price.total);
            const priceB = parseFloat(b.price.total);
            const validPriceA = !isNaN(priceA);
            const validPriceB = !isNaN(priceB);

            if (validPriceA && !validPriceB) return -1;
            if (!validPriceA && validPriceB) return 1;
            if (!validPriceA && !validPriceB) return 0;

            return descending ? priceB - priceA : priceA - priceB;
        });
        displayResults(sortedData); // Gọi hàm display với data đã sắp xếp
    }

    // --- HÀM CHỈ HIỂN THỊ KẾT QUẢ ---
    function displayResults(dataToDisplay) {
        resultsDiv.innerHTML = '';

        if (!dataToDisplay || dataToDisplay.length === 0) {
            resultsDiv.innerHTML = '<p>Không tìm thấy chuyến bay nào phù hợp.</p>';
            return;
        }

        dataToDisplay.forEach(flight => {
            // === SỬA LỖI: TÌM INDEX ===
            const originalIndex = flightDataStore.findIndex(originalFlight => originalFlight.id === flight.id);
            
            // Nếu không tìm thấy (index = -1), BỎ QUA không hiển thị
            if (originalIndex === -1) {
                console.warn("Could not find original index for flight:", flight.id);
                return;
            }
            // === KẾT THÚC SỬA LỖI ===

            const flightElement = document.createElement('div');
            flightElement.className = 'flight-offer';

            const price = parseFloat(flight.price.total);
            const currency = flight.price.currency;
            const formattedPrice = !isNaN(price) ? price.toLocaleString('vi-VN') : 'N/A';

            let itinerariesHtml = '';
            flight.itineraries.forEach(itinerary => {
                 const duration = itinerary.duration.replace('PT', '').replace('H', 'H ').replace('M', 'M');
                 const segments = itinerary.segments;
                 const firstSegment = segments[0];
                 const lastSegment = segments[segments.length - 1];
                 const depTime = new Date(firstSegment.departure.at).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
                 const depDate = new Date(firstSegment.departure.at).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
                 const arrTime = new Date(lastSegment.arrival.at).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
                 const arrDate = new Date(lastSegment.arrival.at).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
                 const stops = segments.length - 1;
                 const stopText = stops === 0 ? 'Bay thẳng' : `${stops} điểm dừng`;
                 itinerariesHtml += `<div class="itinerary"><div class="airline-logo">${firstSegment.carrierCode}</div><div class="flight-times"><div class="time-iata">${depTime} <small>(${firstSegment.departure.iataCode})</small></div><div class="date-airport">${depDate}</div></div><div class="flight-duration"><div class="duration">${duration}</div><div class="stops">${stopText}</div></div><div class="flight-times"><div class="time-iata">${arrTime} <small>(${lastSegment.arrival.iataCode})</small></div><div class="date-airport">${arrDate}</div></div></div>`;
            });

            flightElement.innerHTML = `
                <div class="flight-details">${itinerariesHtml}</div>
                <div class="flight-pricing">
                    <div class="price">${formattedPrice} ${currency}</div>
                    <div class="price-note">Tổng giá vé</div>
                    <button class="select-button" data-index="${originalIndex}">Chọn</button>
                </div>
            `;
            resultsDiv.appendChild(flightElement);
        });
    }

    // --- HÀM XỬ LÝ ĐẶT VÉ DEMO ---
    async function handleDemoBooking(flightIndex, buttonElement) {
        // ... (Code hàm này giữ nguyên từ lần trước) ...
         if (flightIndex === null || !flightDataStore[flightIndex]) { console.error("Invalid flight index:", flightIndex); alert("Lỗi chọn chuyến bay."); return; }
         const selectedFlight = flightDataStore[flightIndex];
         const originalButtonText = buttonElement.textContent;
         buttonElement.textContent = 'Đang xử lý...'; buttonElement.disabled = true;
         let travelerName = "Demo User", travelerEmail = "demo@example.com", travelerPhone = "0123456789";
         const token = localStorage.getItem('userToken');
         if (token) {
             try {
                 const profileResponse = await fetch('/api/account/profile', { headers: getAuthHeaders() });
                 if (profileResponse.ok) {
                     const profileData = await profileResponse.json();
                     travelerName = profileData.fullName || profileData.userName || travelerName;
                     travelerEmail = profileData.email || travelerEmail;
                 }
             } catch (err) { console.warn("Could not fetch profile:", err); }
         }
         const bookingData = { flightOfferData: selectedFlight, traveler: { name: travelerName, email: travelerEmail, phone: travelerPhone } };
         try {
             const response = await fetch('/api/booking/save-flight-request', { method: 'POST', headers: getAuthHeaders(), body: JSON.stringify(bookingData) });
             if (response.ok) {
                 const result = await response.json();
                 buttonElement.parentElement.innerHTML = `<div style="text-align:center; color: green; font-weight: bold;">Đã đặt vé!<br><small>(Mã: ${result.bookingId || 'N/A'})</small></div>`;
             } else {
                 const errorData = await response.json();
                 throw new Error(errorData.message || `Lỗi ${response.status}`);
             }
         } catch (error) {
             console.error("Lỗi đặt vé demo:", error);
             alert(`Lỗi đặt vé: ${error.message || 'Không thể gửi yêu cầu.'}`);
             buttonElement.textContent = originalButtonText; buttonElement.disabled = false;
         }
    }

    // --- GẮN EVENT LISTENER ---
    if (searchForm) {
        searchForm.addEventListener('submit', handleSearch);
    }

    // Event listener cho nút "Chọn" -> Gọi đặt vé demo
    // Event listener cho nút "Chọn"
    if (resultsDiv) {
        resultsDiv.addEventListener('click', function(event) {
            // Chỉ xử lý khi click vào nút .select-button VÀ nút đó KHÔNG bị disabled
            if (event.target.classList.contains('select-button') && !event.target.disabled) {
                const index = event.target.getAttribute('data-index');
                
                if (index !== null && flightDataStore[index]) {
                    const selectedFlight = flightDataStore[index];

                    console.log("Flight selected:", selectedFlight); // Debug

                    // --- BẮT ĐẦU LOGIC MỚI: TRÍCH XUẤT CHI TIẾT ---
                    // Tạo danh sách chi tiết từng chiều bay (Đi/Về) đã được format đẹp
                    const itineraries = selectedFlight.itineraries.map((itinerary, idx) => {
                        const segments = itinerary.segments;
                        const firstSeg = segments[0];
                        const lastSeg = segments[segments.length - 1];

                        // Format ngày giờ: 10:30 20/10/2025
                        const depDate = new Date(firstSeg.departure.at).toLocaleString('vi-VN'); 
                        const arrDate = new Date(lastSeg.arrival.at).toLocaleString('vi-VN');

                        return {
                            direction: idx === 0 ? "Chiều đi" : "Chiều về",
                            airline: selectedFlight.validatingAirlineCodes[0], // Hãng bay
                            flightNumber: `${firstSeg.carrierCode} ${firstSeg.number}`,
                            origin: firstSeg.departure.iataCode,
                            destination: lastSeg.arrival.iataCode,
                            departureTime: depDate,
                            arrivalTime: arrDate,
                            duration: itinerary.duration.replace('PT', '').toLowerCase()
                        };
                    });

                    // Tạo object chuẩn để lưu vào localStorage
                    const paymentInfo = {
                        serviceType: 'flight', // Dùng thống nhất từ khóa này
                        id: selectedFlight.id,
                        price: parseFloat(selectedFlight.price.total),
                        currency: selectedFlight.price.currency,
                        
                        // QUAN TRỌNG: Mảng này chứa thông tin đã format để hiển thị bên Payment/History
                        flightDetails: itineraries, 
                        
                        // Dữ liệu gốc để gửi API Booking (Backend cần cái này để xử lý logic giá/vé)
                        itemData: selectedFlight 
                    };

                    localStorage.setItem('pendingPayment', JSON.stringify(paymentInfo));
                    // --- KẾT THÚC LOGIC MỚI ---

                    // Chuyển hướng sang trang thanh toán
                    window.location.href = '/html/payment.html';

                } else {
                    console.error("Invalid flight index:", index);
                    alert("Lỗi khi chọn chuyến bay.");
                }
            }
        });
    }

    // Event listener cho nút lọc giá
    const sortDescBtn = document.getElementById('sort-flights-desc');
    const sortAscBtn = document.getElementById('sort-flights-asc');

    if (sortDescBtn) {
        sortDescBtn.addEventListener('click', () => {
             console.log("Sort Desc (flights) clicked"); // DEBUG
            sortAndDisplayFlights(true); // Gọi hàm sắp xếp
            sortDescBtn.classList.add('active');
            if(sortAscBtn) sortAscBtn.classList.remove('active');
        });
    }
    if (sortAscBtn) {
        sortAscBtn.addEventListener('click', () => {
             console.log("Sort Asc (flights) clicked"); // DEBUG
            sortAndDisplayFlights(false); // Gọi hàm sắp xếp
            sortAscBtn.classList.add('active');
            if(sortDescBtn) sortDescBtn.classList.remove('active');
        });
    }


    // CHẠY HÀM AUTO SEARCH
    autoSearchFromHome();
});