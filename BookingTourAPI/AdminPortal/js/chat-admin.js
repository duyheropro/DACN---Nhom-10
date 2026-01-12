// TRONG FILE: AdminPortal/js/chat-admin.js

document.addEventListener('DOMContentLoaded', () => {
    const userList = document.getElementById('user-list');
    const messagesDiv = document.getElementById('admin-messages');
    const chatForm = document.getElementById('admin-chat-form');
    const chatInput = document.getElementById('admin-input');
    const headerUser = document.getElementById('current-chat-user');

    let currentChatUserId = null;
    
    // --- THAY ĐỔI 1: Tải lịch sử từ LocalStorage ngay khi mở trang ---
    let chatHistory = loadHistoryFromStorage(); 

    // 1. Kiểm tra Token Admin
    const token = localStorage.getItem('adminToken');
    if (!token) {
        console.warn("Không tìm thấy Admin Token.");
        return;
    }

    // 2. Render lại danh sách User từ lịch sử cũ (Để khi F5 danh sách vẫn còn)
    renderUserListFromHistory();

    // 3. Kết nối SignalR
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub", { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .build();

    // --- SỰ KIỆN NHẬN TIN NHẮN TỪ USER ---
    connection.on("ReceiveMessageFromUser", (userId, userName, message) => {
        // A. Lưu vào biến
        if (!chatHistory[userId]) {
            chatHistory[userId] = { name: userName, messages: [] };
        }
        chatHistory[userId].messages.push({ text: message, sender: 'received' });
        
        // --- THAY ĐỔI 2: Lưu ngay vào LocalStorage ---
        saveHistoryToStorage();

        // B. Cập nhật giao diện danh sách User
        updateUserListUI(userId, userName, message, true);

        // C. Nếu đang mở chat với User này -> Hiện tin nhắn ngay
        if (currentChatUserId === userId) {
            appendMessage(message, 'received');
            const item = document.querySelector(`.chat-user-item[data-id="${userId}"]`);
            if (item) item.classList.remove('unread');
        }
    });

    connection.start()
        .then(() => console.log("Admin Chat Connected"))
        .catch(err => console.error("SignalR Error:", err));


    // --- HÀM XỬ LÝ GIAO DIỆN DANH SÁCH USER ---
    function updateUserListUI(userId, userName, lastMsg, isIncoming) {
        let item = document.querySelector(`.chat-user-item[data-id="${userId}"]`);
        
        if (item) {
            item.querySelector('.chat-user-preview').textContent = (isIncoming ? "" : "Bạn: ") + lastMsg;
            item.remove();
        } else {
            item = document.createElement('div');
            item.className = 'chat-user-item';
            item.setAttribute('data-id', userId);
            item.onclick = () => selectUser(userId, userName);
            
            item.innerHTML = `
                <div class="chat-user-name">${userName}</div>
                <div class="chat-user-preview" style="font-size: 12px; color: #666; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
                    ${(isIncoming ? "" : "Bạn: ") + lastMsg}
                </div>
            `;
        }

        if (isIncoming && currentChatUserId !== userId) {
            item.classList.add('unread');
            // Style cho unread
            item.style.backgroundColor = '#fff3cd';
            item.style.fontWeight = 'bold';
        }
        userList.prepend(item);
    }

    // --- CHỌN USER ĐỂ CHAT ---
    function selectUser(userId, userName) {
        currentChatUserId = userId;
        headerUser.textContent = `Đang chat với: ${userName}`;
        chatForm.style.display = 'flex';
        
        // Reset style
        document.querySelectorAll('.chat-user-item').forEach(i => {
            i.classList.remove('active');
            i.style.backgroundColor = ''; // Reset màu nền
            i.style.fontWeight = 'normal';
        });

        const item = document.querySelector(`.chat-user-item[data-id="${userId}"]`);
        if (item) {
            item.classList.add('active');
            item.classList.remove('unread');
            item.style.backgroundColor = '#e9ecef'; // Highlight active
        }

        messagesDiv.innerHTML = '';
        if (chatHistory[userId]) {
            chatHistory[userId].messages.forEach(m => appendMessageUI(m.text, m.sender));
        }
        
        scrollToBottom();
        chatInput.focus();
    }

    // --- GỬI TIN NHẮN (ADMIN -> USER) ---
    chatForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const msg = chatInput.value.trim();
        if (!msg || !currentChatUserId) return;

        appendMessage(msg, 'sent');
        chatInput.value = '';
        chatInput.focus();

        const userName = chatHistory[currentChatUserId].name;
        updateUserListUI(currentChatUserId, userName, msg, false);

        chatHistory[currentChatUserId].messages.push({ text: msg, sender: 'sent' });
        
        // --- THAY ĐỔI 3: Lưu khi gửi tin ---
        saveHistoryToStorage();

        try {
            await connection.invoke("SendMessageToUser", currentChatUserId, msg);
        } catch (err) {
            console.error(err);
            alert("Lỗi gửi tin! User có thể đã offline.");
        }
    });

    function appendMessage(text, type) {
        appendMessageUI(text, type);
        scrollToBottom();
    }

    function appendMessageUI(text, type) {
        const div = document.createElement('div');
        div.className = `msg-bubble ${type}`;
        div.textContent = text;
        messagesDiv.appendChild(div);
    }

    function scrollToBottom() {
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
    }

    // ============================================================
    // --- CÁC HÀM HỖ TRỢ LOCAL STORAGE (MỚI) ---
    // ============================================================

    function saveHistoryToStorage() {
        localStorage.setItem('adminChatHistory', JSON.stringify(chatHistory));
    }

    function loadHistoryFromStorage() {
        const stored = localStorage.getItem('adminChatHistory');
        return stored ? JSON.parse(stored) : {};
    }

    function renderUserListFromHistory() {
        // Xóa nội dung cũ (ví dụ text "Chưa có tin nhắn")
        userList.innerHTML = '';

        // Lặp qua object chatHistory để vẽ lại danh sách bên trái khi F5
        const userIds = Object.keys(chatHistory);
        
        if (userIds.length === 0) {
            userList.innerHTML = '<div class="p-4 text-center text-muted small">Chưa có tin nhắn mới.<br>(Dữ liệu sẽ hiện khi User nhắn tin)</div>';
            return;
        }

        userIds.forEach(userId => {
            const data = chatHistory[userId];
            const lastMsgObj = data.messages[data.messages.length - 1];
            if (lastMsgObj) {
                const isIncoming = lastMsgObj.sender === 'received';
                // Vẽ lại item user
                updateUserListUI(userId, data.name, lastMsgObj.text, isIncoming);
            }
        });
    }
});