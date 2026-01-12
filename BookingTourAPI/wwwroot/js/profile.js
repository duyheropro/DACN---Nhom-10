// TRONG FILE: wwwroot/js/profile.js
// THAY THẾ TOÀN BỘ FILE

// Hàm lấy header (giữ nguyên)
function getAuthHeaders() {
    const token = localStorage.getItem('userToken');
    const headers = { 'Content-Type': 'application/json' };
    if (token) { headers['Authorization'] = 'Bearer ' + token; }
    return headers;
}

document.addEventListener('DOMContentLoaded', () => {
    const profileForm = document.getElementById('profile-form');
    const emailInput = document.getElementById('profile-email');
    const usernameInput = document.getElementById('profile-username');
    // --- Lấy thêm input mới ---
    const fullnameInput = document.getElementById('profile-fullname');
    const dobInput = document.getElementById('profile-dob');
    // -------------------------
    const loadingMessage = document.getElementById('loading-message');
    const errorMessage = document.getElementById('error-message');
    const successMessage = document.getElementById('success-message');

    function hideMessages() { /* ... (code cũ giữ nguyên) ... */
        loadingMessage.style.display = 'none';
        errorMessage.style.display = 'none';
        successMessage.style.display = 'none';
        errorMessage.textContent = '';
        successMessage.textContent = '';
    }

    // --- 1. SỬA HÀM TẢI PROFILE ---
    async function loadUserProfile() {
        hideMessages();
        loadingMessage.style.display = 'block';
        try {
            const response = await fetch('/api/account/profile', { headers: getAuthHeaders() });
            if (response.ok) {
                const data = await response.json();
                emailInput.value = data.email || '';
                usernameInput.value = data.userName || '';
                // --- Hiển thị dữ liệu mới ---
                fullnameInput.value = data.fullName || '';
                dobInput.value = data.dateOfBirth || ''; // API trả về YYYY-MM-DD
                // -------------------------
                hideMessages();
            } else if (response.status === 401 || response.status === 403) {
                window.location.href = '/html/login.html';
            } else {
                throw new Error(`Lỗi ${response.status}: Không thể tải thông tin.`);
            }
        } catch (error) { /* ... (code xử lý lỗi cũ giữ nguyên) ... */
             console.error('Lỗi tải profile:', error);
            hideMessages();
            errorMessage.textContent = error.message || 'Lỗi kết nối máy chủ.';
            errorMessage.style.display = 'block';
            profileForm.querySelectorAll('input, button').forEach(el => el.disabled = true);
        }
    }

    // --- 2. SỬA HÀM LƯU THAY ĐỔI ---
    if (profileForm) {
        profileForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            hideMessages();
            const saveButton = profileForm.querySelector('.save-button');
            saveButton.textContent = 'Đang lưu...'; // Thông báo đang xử lý
            saveButton.disabled = true;

            // Lấy dữ liệu mới từ form
            const updatedUsername = usernameInput.value;
            const updatedFullname = fullnameInput.value;
            const updatedDob = dobInput.value; // Lấy giá trị YYYY-MM-DD

            try {
                // Gọi API PUT mới
                const response = await fetch('/api/account/profile', {
                    method: 'PUT',
                    headers: getAuthHeaders(),
                    body: JSON.stringify({
                        userName: updatedUsername,
                        fullName: updatedFullname,
                        dateOfBirth: updatedDob // Gửi chuỗi YYYY-MM-DD
                    })
                });

                if (response.ok) {
                    const data = await response.json();
                    successMessage.textContent = data.message || 'Cập nhật thành công!';
                    successMessage.style.display = 'block';
                    // Cập nhật lại email hiển thị ở header nếu username thay đổi email
                    // (Thường username không đổi email, nên không cần)
                    // localStorage.setItem('userEmail', updatedUsername);
                } else {
                     // Xử lý lỗi từ server (vd: username trùng, ngày sai định dạng)
                     const errorData = await response.json();
                     let errorText = 'Lỗi cập nhật thông tin.';
                     if (errorData && typeof errorData === 'object') {
                         if (errorData.message) {
                            errorText = errorData.message;
                         } else if (errorData.errors) {
                            errorText = Object.values(errorData.errors).flat().join('\n');
                         } else if (errorData.title) { // Lỗi validation chuẩn của .NET
                             errorText = errorData.title;
                         }
                     } else if (typeof errorData === 'string') {
                         errorText = errorData;
                     }
                     throw new Error(errorText);
                }
            } catch (error) {
                console.error('Lỗi cập nhật profile:', error);
                errorMessage.textContent = error.message || 'Lỗi kết nối máy chủ.';
                errorMessage.style.display = 'block';
            } finally {
                // Khôi phục nút sau khi xử lý xong
                saveButton.textContent = 'Lưu thay đổi';
                saveButton.disabled = false;
            }
        });
    }

    // --- CHẠY HÀM TẢI PROFILE ---
    loadUserProfile();
});