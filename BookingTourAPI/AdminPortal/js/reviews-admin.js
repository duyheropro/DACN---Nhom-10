// AdminPortal/js/reviews-admin.js

document.addEventListener("DOMContentLoaded", () => {
    const tourIdInput = document.getElementById("review-tour-id");
    const loadBtn = document.getElementById("btn-load-reviews");
    const tbody = document.getElementById("reviews-tbody");
    const alertBox = document.getElementById("reviews-alert");
    const badgeTotal = document.getElementById("badge-total-reviews");
    const badgeAvg = document.getElementById("badge-average-rating");

    // Lấy header có token admin
    function getAdminAuthHeaders() {
        const token = localStorage.getItem("adminToken");
        const headers = { "Accept": "application/json" };
        if (token) {
            headers["Authorization"] = "Bearer " + token;
        } else {
            console.warn("Không tìm thấy adminToken trong localStorage");
        }
        return headers;
    }

    function setAlert(type, message) {
        if (!alertBox) return;
        if (!message) {
            alertBox.style.display = "none";
            alertBox.textContent = "";
            return;
        }
        alertBox.className = "alert mt-3 mb-0 alert-" + type;
        alertBox.textContent = message;
        alertBox.style.display = "block";
    }

    function formatDateTime(isoString) {
        if (!isoString) return "";
        const d = new Date(isoString);
        if (isNaN(d.getTime())) return isoString;
        const pad = n => n.toString().padStart(2, "0");
        const time = `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
        const date = `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`;
        return `${time}\n${date}`;
    }

    function renderRows(reviews) {
        tbody.innerHTML = "";

        if (!reviews || reviews.length === 0) {
            const tr = document.createElement("tr");
            const td = document.createElement("td");
            td.colSpan = 7;
            td.className = "text-center text-muted py-3";
            td.textContent = "Chưa có đánh giá nào.";
            tr.appendChild(td);
            tbody.appendChild(tr);

            if (badgeTotal) badgeTotal.textContent = "0";
            if (badgeAvg) badgeAvg.textContent = "0.0";
            return;
        }

        let total = 0;

        reviews.forEach(r => {
            total += r.rating || 0;

            const tr = document.createElement("tr");

            const tdId = document.createElement("td");
            tdId.textContent = r.id;
            tdId.className = "fw-semibold";
            tr.appendChild(tdId);

            // --- SỬA ĐỔI TẠI ĐÂY: Đổi r.tourTitle thành r.tourName ---
            const tdTour = document.createElement("td");
            tdTour.innerHTML = `
                <div class="fw-semibold">${r.tourName || "(Không có tiêu đề)"}</div>
                <div class="text-muted small">ID: ${r.tourId}</div>
            `;
            tr.appendChild(tdTour);
            // ---------------------------------------------------------

            const tdEmail = document.createElement("td");
            tdEmail.textContent = r.userEmail || "";
            tr.appendChild(tdEmail);

            const tdRating = document.createElement("td");
            tdRating.innerHTML = `<span class="badge bg-warning text-dark">
                <i class="bi bi-star-fill me-1"></i>${r.rating || 0}
            </span>`;
            tr.appendChild(tdRating);

            const tdComment = document.createElement("td");
            tdComment.textContent = r.comment || "";
            tr.appendChild(tdComment);

            const tdTime = document.createElement("td");
            tdTime.className = "small text-muted";
            tdTime.style.whiteSpace = "pre-line";
            tdTime.textContent = formatDateTime(
                r.createdAt || r.createdAtUtc || r.createdDate
            );
            tr.appendChild(tdTime);

            const tdAction = document.createElement("td");
            tdAction.className = "text-center";

            const btnDelete = document.createElement("button");
            btnDelete.className = "btn btn-sm btn-danger";
            btnDelete.innerHTML = `<i class="bi bi-trash"></i>`;
            btnDelete.addEventListener("click", () => {
                if (confirm(`Xoá đánh giá ID ${r.id}?`)) {
                    deleteReview(r.id);
                }
            });

            tdAction.appendChild(btnDelete);
            tr.appendChild(tdAction);

            tbody.appendChild(tr);
        });

        if (badgeTotal) badgeTotal.textContent = reviews.length.toString();
        const avg = total / reviews.length;
        if (badgeAvg) badgeAvg.textContent = avg.toFixed(1);
    }

    async function loadReviews() {
        setAlert("info", "Đang tải dữ liệu...");
        tbody.innerHTML = "";

        let tourId = tourIdInput ? tourIdInput.value.trim() : "";

        let url = "/api/Review/admin";
        if (tourId) {
            url += `?tourId=${encodeURIComponent(tourId)}`;
        }

        try {
            const res = await fetch(url, {
                headers: getAdminAuthHeaders()
            });

            if (res.status === 401 || res.status === 403) {
                window.location.href = "/login.html";
                return;
            }

            if (!res.ok) throw new Error(`Lỗi ${res.status}`);

            const data = await res.json();
            renderRows(data);
            setAlert("success", "Đã tải xong danh sách đánh giá.");
        } catch (err) {
            console.error(err);
            setAlert("danger", "Không tải được danh sách đánh giá. Vui lòng thử lại.");
            tbody.innerHTML = "";
        }
    }

    async function deleteReview(id) {
        try {
            const res = await fetch(`/api/Review/admin/${id}`, {
                method: "DELETE",
                headers: getAdminAuthHeaders()
            });

            if (res.status === 401 || res.status === 403) {
                window.location.href = "/login.html";
                return;
            }

            if (!res.ok) throw new Error(`Lỗi ${res.status}`);

            await loadReviews();
        } catch (err) {
            console.error(err);
            alert("Không xoá được đánh giá. Vui lòng thử lại.");
        }
    }

    if (loadBtn) loadBtn.addEventListener("click", loadReviews);

    if (tourIdInput) {
        tourIdInput.addEventListener("keyup", e => {
            if (e.key === "Enter") loadReviews();
        });
    }

    loadReviews();
});