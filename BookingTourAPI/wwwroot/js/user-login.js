// TRONG FILE: wwwroot/js/user-login.js
document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('login-form');
    const errorMessage = document.getElementById('error-message');

    if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            errorMessage.style.display = 'none';

            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;

            try {
                const response = await fetch('/api/account/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email, password })
                });

                if (response.ok) {
                    // ĐĂNG NHẬP THÀNH CÔNG
                    const data = await response.json();
                    
                    localStorage.setItem('userToken', data.token);
                    localStorage.setItem('userEmail', data.email);
                    
                    // === BẮT ĐẦU SỬA LỖI ===
                    // Kiểm tra xem có cần chuyển hướng đặc biệt không
                    const redirectUrl = localStorage.getItem('redirectAfterLogin');
                    if (redirectUrl) {
                        localStorage.removeItem('redirectAfterLogin'); // Xóa link đã lưu
                        window.location.href = redirectUrl; // Quay lại trang tour
                    } else {
                        // Mặc định về trang chủ
                        window.location.href = '/html/home.html'; 
                    }
                    // === KẾT THÚC SỬA LỖI === 
                } else {
                    const errorData = await response.json();
                    errorMessage.textContent = errorData.message || 'Sai email hoặc mật khẩu.';
                    errorMessage.style.display = 'block';
                }
            } catch (error) {
                errorMessage.textContent = 'Không thể kết nối đến máy chủ.';
                errorMessage.style.display = 'block';
            }
        });
    }
});