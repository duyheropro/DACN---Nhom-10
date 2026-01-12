// TRONG FILE: wwwroot/js/main.js

console.log("Main JS loaded (Chat Only).");

document.addEventListener('DOMContentLoaded', () => {
    // 1. Khởi tạo giao diện Chat
    setupChatWidget();
    
    // 2. Tải tin nhắn cũ
    const savedMessages = localStorage.getItem('userChatMessages');
    if (savedMessages) {
        const messages = JSON.parse(savedMessages);
        messages.forEach(m => appendMessage(m.text, m.sender, false)); 
    }
});

// --- BIẾN TOÀN CỤC CHAT ---
var chatConnection = null;

// --- HÀM MỞ CHAT (GỌI TỪ MENU TRONG HOME.JS) ---
window.openChatBox = function() {
    const token = localStorage.getItem('userToken');
    if(!token) {
        alert("Vui lòng đăng nhập để chat với nhân viên.");
        window.location.href = '/html/login.html';
        return;
    }

    const chatBox = document.getElementById('chat-box');
    if(chatBox) {
        chatBox.style.display = 'flex';
        connectChatSignalR(token);
    }
    
    // Đóng menu user nếu đang mở (để gọn màn hình)
    const userDropdown = document.getElementById('userDropdown');
    if(userDropdown) userDropdown.classList.remove('show');
};

window.closeChatBox = function() {
    const chatBox = document.getElementById('chat-box');
    if(chatBox) chatBox.style.display = 'none';
};

window.handleChatSubmit = async function(e) {
    e.preventDefault();
    const chatInput = document.getElementById('chat-input');
    const msg = chatInput.value.trim();
    if(!msg) return;

    if(chatConnection && chatConnection.state === "Connected") {
        try {
            await chatConnection.invoke("SendMessageToAdmin", msg);
            appendMessage(msg, 'user');
            chatInput.value = '';
        } catch(err) {
            console.error(err);
            alert("Lỗi gửi tin nhắn.");
        }
    } else {
        const token = localStorage.getItem('userToken');
        if(token) connectChatSignalR(token);
    }
};

function setupChatWidget() {
    if (document.getElementById('chat-box')) return;
    const chatHtml = `
        <div id="chat-box" class="chat-box" style="display:none;">
            <div class="chat-header">
                <span>Hỗ trợ trực tuyến</span>
                <span onclick="closeChatBox()" style="cursor:pointer; font-size: 20px;">&times;</span>
            </div>
            <div id="chat-messages" class="chat-messages">
                <div class="message admin">Xin chào! Chúng tôi có thể giúp gì cho bạn?</div>
            </div>
            <form id="chat-form" class="chat-input-area" onsubmit="handleChatSubmit(event)">
                <input type="text" id="chat-input" placeholder="Nhập tin nhắn..." required autocomplete="off">
                <button type="submit"><i class="fa-solid fa-paper-plane"></i></button>
            </form>
        </div>
    `;
    const div = document.createElement('div');
    div.innerHTML = chatHtml;
    document.body.appendChild(div);
}

function connectChatSignalR(token) {
    if(chatConnection && chatConnection.state === "Connected") return;

    chatConnection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub", { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .build();

    chatConnection.on("ReceiveMessageFromAdmin", (message) => {
        appendMessage(message, 'admin');
        const chatBox = document.getElementById('chat-box');
        if(chatBox && chatBox.style.display === 'none') {
            chatBox.style.display = 'flex';
        }
    });

    chatConnection.start().catch(err => console.error("Chat Connection Error:", err));
}

function appendMessage(text, sender, shouldSave = true) {
    const messagesDiv = document.getElementById('chat-messages');
    if(!messagesDiv) return;
    
    const div = document.createElement('div');
    div.className = `message ${sender}`;
    div.textContent = text;
    messagesDiv.appendChild(div);
    messagesDiv.scrollTop = messagesDiv.scrollHeight;

    if (shouldSave) {
        let history = JSON.parse(localStorage.getItem('userChatMessages') || "[]");
        history.push({ text: text, sender: sender });
        localStorage.setItem('userChatMessages', JSON.stringify(history));
    }
}