// HÀM KHỞI TẠO CUSTOM DROPDOWN
function setupCustomDropdown(containerId, displayId, valueId, menuId, defaultText = "Chọn một tùy chọn") {
    // Dùng querySelector để tìm bên trong container, tránh ID trùng lặp
    const container = document.getElementById(containerId);
    if (!container) return;
    
    const display = document.getElementById(displayId);
    const valueInput = document.getElementById(valueId);
    const menu = document.getElementById(menuId);

    if (!display || !valueInput || !menu) return;

    // 1. Toggle menu
    display.addEventListener('click', (e) => {
        e.stopPropagation();
        // Đóng các dropdown khác trước khi mở cái này
        document.querySelectorAll('.custom-dropdown-menu.show').forEach(otherMenu => {
            if (otherMenu !== menu) {
                otherMenu.classList.remove('show');
                const otherDisplayId = otherMenu.id.replace('-menu', '-display');
                document.getElementById(otherDisplayId)?.classList.remove('active');
            }
        });
        menu.classList.toggle('show');
        display.classList.toggle('active');
    });

    // 2. Handle selection
    menu.addEventListener('click', (e) => {
        if (e.target.classList.contains('custom-dropdown-item')) {
            const selectedValue = e.target.getAttribute('data-value');
            const selectedText = e.target.textContent;
            
            valueInput.value = selectedValue; 
            
            if (selectedValue === "") {
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

// 3. ĐÓNG DROPDOWN KHI CLICK RA NGOÀI (CHUNG)
window.addEventListener('click', (e) => {
    // Đóng tất cả menu dropdown
    document.querySelectorAll('.custom-dropdown-menu.show').forEach(menu => {
        menu.classList.remove('show');
    });
    document.querySelectorAll('.custom-dropdown-display.active').forEach(display => {
        display.classList.remove('active');
    });
});
