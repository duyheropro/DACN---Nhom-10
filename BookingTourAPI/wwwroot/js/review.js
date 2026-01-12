// wwwroot/js/review.js
// Hiển thị danh sách review + gửi review

function formatDateTime(iso) {
    const d = new Date(iso);
    return d.toLocaleString('vi-VN');
}

function renderStars(num) {
    const full = '★'.repeat(num);
    const empty = '☆'.repeat(5 - num);
    return `<span class="review-stars">${full}${empty}</span>`;
}

async function loadReviews(tourId) {
    const container = document.getElementById('reviews-container');
    const summary = document.getElementById('reviews-summary');
    if (!container) return;

    container.innerHTML = '<p>Đang tải đánh giá...</p>';

    try {
        const res = await fetch(`/api/review/by-tour/${tourId}`);
        if (!res.ok) {
            container.innerHTML = '<p>Không tải được đánh giá.</p>';
            return;
        }

        const data = await res.json();

        if (summary) {
            if (data.totalReviews > 0) {
                summary.innerHTML = `
                    <div class="review-summary-line">
                        <span class="review-score">${data.averageRating.toFixed(1)}</span>
                        <span class="review-score-max">/ 5</span>
                        <span class="review-count">(${data.totalReviews} đánh giá)</span>
                    </div>
                `;
            } else {
                summary.innerHTML = `<span class="review-count">Chưa có đánh giá nào.</span>`;
            }
        }

        if (data.items.length === 0) {
            container.innerHTML = '<p>Chưa có đánh giá nào. Hãy là người đầu tiên đánh giá!</p>';
            return;
        }

        container.innerHTML = data.items.map(r => `
            <div class="review-card">
                <div class="review-card-header">
                    <div class="review-user">${r.userEmail || 'Ẩn danh'}</div>
                    <div class="review-rating">${renderStars(r.rating)} <span class="review-rating-text">${r.rating} / 5</span></div>
                </div>
                <div class="review-comment">
                    ${r.comment ? r.comment.replace(/\n/g, '<br/>') : ''}
                </div>
                <div class="review-time">
                    ${formatDateTime(r.createdAt)}
                </div>
            </div>
        `).join('');
    } catch (err) {
        console.error(err);
        container.innerHTML = '<p>Có lỗi khi tải đánh giá.</p>';
    }
}

async function submitReview(tourId) {
    const rating = parseInt(document.getElementById('review-rating').value, 10);
    const comment = document.getElementById('review-comment').value.trim();

    const token = localStorage.getItem('userToken');
    if (!token) {
        alert('Bạn cần đăng nhập để gửi đánh giá.');
        return;
    }

    if (!rating || rating < 1 || rating > 5) {
        alert('Điểm đánh giá không hợp lệ.');
        return;
    }

    try {
        const res = await fetch('/api/review', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ tourPackageId: parseInt(tourId, 10), rating, comment })
        });

        if (res.ok) {
            alert('Gửi đánh giá thành công!');
            document.getElementById('review-comment').value = '';
            loadReviews(tourId);
        } else {
            const err = await res.json().catch(() => ({}));
            alert(err.message || 'Lỗi khi gửi đánh giá.');
        }
    } catch (err) {
        console.error(err);
        alert('Không gửi được đánh giá. Vui lòng thử lại.');
    }
}
