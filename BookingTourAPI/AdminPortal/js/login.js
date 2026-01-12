// TRONG FILE: AdminPortal/js/login.js
// THAY THẾ TOÀN BỘ NỘI DUNG BẰNG CODE NÀY

document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('login-form');
    const errorMessage = document.getElementById('error-message');

    if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault(); // Ngăn form gửi đi
            errorMessage.style.display = 'none'; // Ẩn lỗi cũ

            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;

            try {
                const response = await fetch('/api/account/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ email, password })
                });

                if (response.ok) {
                    // ĐĂNG NHẬP THÀNH CÔNG
                    const data = await response.json();

                    // --- LƯU TOKEN VÀ EMAIL VÀO LOCALSTORAGE ---
                    localStorage.setItem('adminToken', data.token); 
                    localStorage.setItem('adminEmail', data.email); 
                    // ------------------------------------------

                    // Chuyển hướng đến trang dashboard
                    window.location.href = '/dashboard.html';
                } else {
                    // Đăng nhập thất bại
                    const errorData = await response.json();
                    errorMessage.textContent = errorData.message || 'Sai email hoặc mật khẩu.';
                    errorMessage.style.display = 'block';
                }
            } catch (error) {
                console.error('Lỗi khi đăng nhập:', error);
                errorMessage.textContent = 'Không thể kết nối đến máy chủ.';
                errorMessage.style.display = 'block';
            }
        });
    }
});