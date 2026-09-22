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
          <div class="text-muted small">${timeAgo(post.createdAt)} · <i class="bi ${privacyIcon}"></i> ${privacyLabel}</div>
        </div>
        <button class="btn btn-sm btn-outline-danger rounded-pill" onclick="unsavePost('${post.id}', this)"><i class="bi bi-bookmark-x me-1"></i>Kaydı Kaldır</button>
      </div>
      ${post.text ? `<p class="mt-3 mb-2">${escapeHtml(post.text)}</p>` : ""}
      ${post.imageUrl ? `<div class="rounded-3 overflow-hidden"><img src="${post.imageUrl}" class="w-100" style="max-height:420px;object-fit:cover;" alt=""></div>` : ""}
    </div>`;
}

async function loadSaved() {
  const list = document.getElementById("savedList");
  const emptyState = document.getElementById("emptyState");
  try {
    const result = await apiFetch("/api/posts/saved?page=1&pageSize=30");
    if (!result.items.length) {
      list.innerHTML = "";
      emptyState.classList.remove("d-none");
      return;
    }
    emptyState.classList.add("d-none");
    list.innerHTML = result.items.map(savedPostHtml).join("");
  } catch (err) {
    list.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
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
