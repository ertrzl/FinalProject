// saved.html — real saved posts wired to the backend.

requireAuth();

function savedPostHtml(post) {
  const privacyIcon = post.privacy === "FriendsOnly" ? "bi-people-fill" : "bi-globe-americas";
  const privacyLabel = post.privacy === "FriendsOnly" ? "Sadece Arkadaşlar" : "Herkese Açık";

  return `
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-3" data-post-id="${post.id}">
      <div class="d-flex align-items-center gap-3">
        <img src="${post.authorAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">
        <div class="flex-grow-1">
          <div class="fw-bold">${escapeHtml(post.authorName)}</div>
          <div class="text-muted small"><a href="post.html?id=${post.id}" class="text-muted text-decoration-none">${timeAgo(post.createdAt)}</a>${post.isGroupPost ? "" : ` · <i class="bi ${privacyIcon}"></i> ${privacyLabel}`}</div>
        </div>
        <button class="btn btn-sm btn-outline-danger rounded-pill" onclick="unsavePost('${post.id}', this)"><i class="bi bi-bookmark-x me-1"></i>Kaydı Kaldır</button>
      </div>
      ${post.text ? `<p class="mt-3 mb-2">${escapeHtml(post.text)}</p>` : ""}
      ${postMediaHtml(post)}
    </div>`;
}

// 30 saved posts at a time (pager.js); with nothing saved the page shows its own "empty" picture instead.
const savedPager = createPager({
  list: "savedList",
  url: page => `/api/posts/saved?page=${page}&pageSize=30`,
  render: savedPostHtml,
  onLoaded: (items, firstPage) => {
    if (firstPage) document.getElementById("emptyState").classList.toggle("d-none", items.length > 0);
  }
});

function loadSaved() {
  return savedPager.reload();
}

async function unsavePost(postId, btn) {
  btn.disabled = true;
  try {
    await apiFetch(`/api/posts/${postId}/save`, { method: "POST" });
    btn.closest(".card").remove();
    if (!document.getElementById("savedList").children.length) {
      document.getElementById("emptyState").classList.remove("d-none");
    }
    toast("Kayıt kaldırıldı.");
  } catch (err) {
    btn.disabled = false;
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

loadSaved();
