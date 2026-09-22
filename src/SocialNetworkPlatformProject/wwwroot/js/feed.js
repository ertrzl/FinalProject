// home.html — real feed, comments and stories wired to the backend (replaces the mocks from app.js/stories.js).
// Friend suggestions on this page are still mock.

const session = requireAuth();

function postCardHtml(post) {
  const privacyIcon = post.privacy === "FriendsOnly" ? "bi-people-fill" : "bi-globe-americas";
  const privacyLabel = post.privacy === "FriendsOnly" ? "Sadece Arkadaşlar" : "Herkese Açık";
  const avatar = post.authorAvatarUrl || "https://i.pravatar.cc/80?img=45";
  const likedClass = post.isLikedByCurrentUser ? "liked" : "";
  const heartIcon = post.isLikedByCurrentUser ? "bi-heart-fill" : "bi-heart";

  return `
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-4" data-post-id="${post.id}">
      <div class="d-flex align-items-center gap-3">
        <img src="${avatar}" class="avatar-sm" alt="">
        <div class="flex-grow-1">
          <div class="fw-bold">${escapeHtml(post.authorName)}</div>
          <div class="text-muted small">${timeAgo(post.createdAt)} · <i class="bi ${privacyIcon}"></i> ${privacyLabel}</div>
        </div>
      </div>
      ${post.text ? `<p class="mt-3 mb-2">${escapeHtml(post.text)}</p>` : ""}
      ${post.imageUrl ? `<div class="rounded-3 overflow-hidden mb-2"><img src="${post.imageUrl}" class="w-100" style="max-height:420px;object-fit:cover;" alt=""></div>` : ""}
      <div class="d-flex justify-content-between text-muted small py-2 border-bottom">
        <span><i class="bi bi-heart-fill text-danger"></i> <span class="like-count-label">${post.likeCount}</span> beğeni</span>
        <span><span class="comment-count-label">${post.commentCount}</span> yorum</span>
      </div>
      <div class="d-flex pt-1">
        <button class="btn btn-sm flex-fill text-muted fw-semibold like-btn ${likedClass}" onclick="toggleLike(this)">
          <i class="like-icon bi ${heartIcon}"></i> Beğen <span class="like-count" data-count="${post.likeCount}">${post.likeCount}</span>
        </button>
        <button class="btn btn-sm flex-fill text-muted fw-semibold" data-bs-toggle="collapse" data-bs-target="#comments-${post.id}" onclick="loadComments('${post.id}')">
          <i class="bi bi-chat"></i> Yorum Yap
        </button>
        <button class="btn btn-sm flex-fill text-muted fw-semibold" onclick="copyPostLink(this)"><i class="bi bi-share"></i> Paylaş</button>
      </div>
      <div class="collapse comments-collapse mt-3 pt-3 border-top" id="comments-${post.id}">
        <div class="comments-list mb-2"></div>
        <form class="d-flex gap-2 align-items-center" onsubmit="return submitComment(event, '${post.id}')">
          <img src="${session.avatarUrl || DEFAULT_AVATAR}" class="avatar-xs" alt="">
          <input type="text" class="form-control form-control-sm rounded-pill" placeholder="Bir yorum yaz...">
        </form>
      </div>
    </div>`;
}

function commentHtml(c) {
  return `
    <div class="d-flex gap-2 mb-3">
      <img src="${c.authorAvatarUrl || DEFAULT_AVATAR}" class="avatar-xs flex-shrink-0" alt="">
      <div class="comment-bubble flex-grow-1">
        <div class="fw-bold small c-name">${escapeHtml(c.authorName)}</div>
        <div class="small">${escapeHtml(c.text)}</div>
        <div class="small text-muted mt-1 d-flex gap-3"><span>${timeAgo(c.createdAt)}</span></div>
      </div>
    </div>`;
}

async function loadComments(postId) {
  const container = document.getElementById(`comments-${postId}`);
  if (!container || container.dataset.loaded) return;
  container.dataset.loaded = "1";
  const list = container.querySelector(".comments-list");
  try {
    const comments = await apiFetch(`/api/comments/by-post/${postId}`);
    if (comments.length) {
      list.innerHTML = comments.map(commentHtml).join("");
    } else {
      list.innerHTML = `<div class="text-muted small mb-2">Henüz yorum yok.</div>`;
      list.dataset.empty = "1";
    }
  } catch (err) {
    list.innerHTML = `<div class="text-danger small">${escapeHtml(err.message)}</div>`;
  }
}

async function submitComment(event, postId) {
  event.preventDefault();
  const form = event.target;
  const input = form.querySelector("input[type=text]");
  const text = input.value.trim();
  if (!text) return false;

  try {
    const comment = await apiFetch("/api/comments", { method: "POST", body: { postId, text } });
    const list = form.closest(".comments-collapse").querySelector(".comments-list");
    if (list.dataset.empty) { list.innerHTML = ""; delete list.dataset.empty; }
    list.insertAdjacentHTML("beforeend", commentHtml(comment));
    input.value = "";

    const card = form.closest(".card[data-post-id]");
    const label = card.querySelector(".comment-count-label");
    label.textContent = (parseInt(label.textContent, 10) || 0) + 1;
  } catch (err) {
    toast(err.message || "Yorum eklenemedi.");
  }
  return false;
}

async function loadFeed() {
  const list = document.getElementById("feedList");
  try {
    const result = await apiFetch("/api/posts/feed?page=1&pageSize=10");
    if (!result.items.length) {
      list.innerHTML = `<div class="text-center text-muted small py-4">Henüz gönderi yok. İlk paylaşımı sen yap!</div>`;
      return;
    }
    list.innerHTML = result.items.map(postCardHtml).join("");
  } catch (err) {
    list.innerHTML = `<div class="alert alert-danger small">Akış yüklenemedi: ${escapeHtml(err.message)}</div>`;
  }
}

// Overrides the mock version from app.js: actually posts to the backend.
async function publishPost(btn) {
  const composer = btn.closest(".composer");
  const textarea = document.getElementById("composerText");
  const fileInput = document.getElementById("composerFile");
  const privacy = document.getElementById("composerPrivacy").value;
  const text = textarea.value.trim();
  const file = fileInput.files[0];

  if (!text && !file) {
    textarea.classList.add("is-invalid");
    setTimeout(() => textarea.classList.remove("is-invalid"), 1200);
    return;
  }

  btn.disabled = true;
  try {
    const formData = new FormData();
    if (text) formData.append("text", text);
    if (file) formData.append("image", file);
    formData.append("privacy", privacy);

    await apiFetchForm("/api/posts", { method: "POST", body: formData });

    textarea.value = "";
    clearComposerImage();
    toast("Gönderi paylaşıldı!");
    await loadFeed();
  } catch (err) {
    toast(err.message || "Gönderi paylaşılamadı.");
  } finally {
    btn.disabled = false;
  }
}

// Overrides the mock version from app.js: actually toggles the like on the backend.
async function toggleLike(btn) {
  const card = btn.closest(".card[data-post-id]");
  const postId = card.dataset.postId;
  btn.disabled = true;
  try {
    const result = await apiFetch(`/api/posts/${postId}/like`, { method: "POST" });

    const countEl = btn.querySelector(".like-count");
    const icon = btn.querySelector(".like-icon");
    countEl.dataset.count = result.likeCount;
    countEl.textContent = result.likeCount;
    card.querySelector(".like-count-label").textContent = result.likeCount;
    btn.classList.toggle("liked", result.isLiked);
    icon.className = "like-icon bi " + (result.isLiked ? "bi-heart-fill" : "bi-heart");
  } catch (err) {
    toast(err.message || "Beğeni işlenemedi.");
  } finally {
    btn.disabled = false;
  }
}

loadFeed();
