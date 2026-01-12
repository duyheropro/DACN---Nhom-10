// File: wwwroot/js/user-register.js
document.addEventListener('DOMContentLoaded', () => {
    const registerForm = document.getElementById('register-form');
    const errorDiv = document.getElementById('error-message');

    if (!registerForm) return;

    registerForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const email = document.getElementById('email').value.trim();
        const password = document.getElementById('password').value;
        const confirmPassword = document.getElementById('confirmPassword').value;
        const fullNameInput = document.getElementById('fullName'); // nếu sau này bạn thêm
        const fullName = fullNameInput ? fullNameInput.value.trim() : null;
        const btn = document.querySelector('.auth-button');

        if (errorDiv) {
            errorDiv.style.display = 'none';
            errorDiv.textContent = '';
        }

        if (!email || !password) {
            showError('Vui lòng nhập đầy đủ email và mật khẩu.');
            return;
        }

        if (password !== confirmPassword) {
            showError('Mật khẩu nhập lại không khớp.');
            return;
        }

        if (btn) {
            btn.disabled = true;
            btn.textContent = 'Đang tạo tài khoản...';
        }

        try {
            const response = await fetch('/api/account/register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    email,
                    password,
                    fullName
                })
            });

            const data = await response.json().catch(() => ({}));

            if (response.ok) {
                alert(data.message || 'Đăng ký thành công. Vui lòng kiểm tra email để kích hoạt tài khoản.');
                // Sau khi đăng ký → quay về trang login
                window.location.href = '/html/login.html';
            } else {
                showError(data.message || 'Không thể đăng ký tài khoản.');
            }
        } catch (err) {
            console.error(err);
            showError('Không thể kết nối đến máy chủ.');
        } finally {
            if (btn) {
                btn.disabled = false;
                btn.textContent = 'Tạo tài khoản';
            }
        }
    });

    function showError(msg) {
        if (errorDiv) {
            errorDiv.textContent = msg;
            errorDiv.style.display = 'block';
        } else {
            alert(msg);
        }
    }
});
