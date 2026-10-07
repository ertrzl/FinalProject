// Post cards with their comments and the like / delete actions, shared by every page that shows a wall of posts
// (home, profile, group). The server decides what the current user may do (post.canDelete, comment.canDelete);
// this file only draws the buttons and calls the API.

// options: showPrivacy — the "Herkese Açık / Sadece Arkadaşlar" label (a group wall has no per-post privacy);
//          showShare   — the "Paylaş" (copy link) button.
function postCardHtml(post, options = {}) {
  const { showPrivacy = true, showShare = true } = options;
  const privacyIcon = post.privacy === "FriendsOnly" ? "bi-people-fill" : "bi-globe-americas";
  const privacyLabel = post.privacy === "FriendsOnly" ? "Sadece Arkadaşlar" : "Herkese Açık";
  const likedClass = post.isLikedByCurrentUser ? "liked" : "";
  const heartIcon = post.isLikedByCurrentUser ? "bi-heart-fill" : "bi-heart";
  const authorUrl = `profile.html?id=${post.authorId}`;

  const deleteMenu = post.canDelete
    ? `<div class="dropdown">
         <button class="btn btn-sm btn-light rounded-circle" data-bs-toggle="dropdown"><i class="bi bi-three-dots"></i></button>
         <ul class="dropdown-menu dropdown-menu-end">
           <li><button class="dropdown-item" onclick="removePost('${post.id}', this)"><i class="bi bi-trash me-2"></i>Gönderiyi Sil</button></li>
         </ul>
       </div>`
    : "";

  const shareButton = showShare
    ? `<button class="btn btn-sm flex-fill text-muted fw-semibold" onclick="copyPostLink(this)"><i class="bi bi-share"></i> Paylaş</button>`
    : "";

  return `
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-4" data-post-id="${post.id}">
      <div class="d-flex align-items-center gap-3">
        <a href="${authorUrl}"><img src="${post.authorAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
        <div class="flex-grow-1">
          <div class="fw-bold"><a href="${authorUrl}" class="text-dark text-decoration-none">${escapeHtml(post.authorName)}</a></div>
          <div class="text-muted small">${timeAgo(post.createdAt)}${showPrivacy ? ` · <i class="bi ${privacyIcon}"></i> ${privacyLabel}` : ""}</div>
        </div>
        ${deleteMenu}
      </div>
      ${post.text ? `<p class="mt-3 mb-2">${linkifyHashtags(escapeHtml(post.text))}</p>` : ""}
      ${postMediaHtml(post)}
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
        ${shareButton}
      </div>
      <div class="collapse comments-collapse mt-3 pt-3 border-top" id="comments-${post.id}">
        <div class="comments-list mb-2"></div>
        <form class="d-flex gap-2 align-items-center" onsubmit="return submitComment(event, '${post.id}')">
          <img src="${getSession()?.avatarUrl || DEFAULT_AVATAR}" class="avatar-xs" alt="">
          <input type="text" autocomplete="off" class="form-control form-control-sm rounded-pill" placeholder="Bir yorum yaz...">
        </form>
      </div>
    </div>`;
}

function commentHtml(c) {
  const likedClass = c.isLikedByCurrentUser ? "fw-bold text-primary" : "text-muted";
  const likeLabel = c.isLikedByCurrentUser ? "Beğenildi" : "Beğen";
  const likeCountLabel = c.likeCount > 0 ? ` (${c.likeCount})` : "";
  const deleteLink = c.canDelete
    ? `<a href="#" class="text-danger" onclick="return removeComment(event, this)">Sil</a>`
    : "";

  return `
    <div class="d-flex gap-2 mb-3" data-comment-id="${c.id}">
      <img src="${c.authorAvatarUrl || DEFAULT_AVATAR}" class="avatar-xs flex-shrink-0" alt="">
      <div class="comment-bubble flex-grow-1">
        <div class="fw-bold small c-name">${escapeHtml(c.authorName)}</div>
        <div class="small">${escapeHtml(c.text)}</div>
        <div class="small text-muted mt-1 d-flex gap-3">
          <span>${timeAgo(c.createdAt)}</span>
          <a href="#" class="comment-like-link ${likedClass}" onclick="return toggleCommentLike(event, this)">${likeLabel}<span class="comment-like-count">${likeCountLabel}</span></a>
          ${deleteLink}
        </div>
      </div>
    </div>`;
}

async function toggleCommentLike(event, link) {
  event.preventDefault();
  const commentId = link.closest("[data-comment-id]").dataset.commentId;
  try {
    const result = await apiFetch(`/api/comments/${commentId}/like`, { method: "POST" });
    link.classList.toggle("fw-bold", result.isLiked);
    link.classList.toggle("text-primary", result.isLiked);
    link.classList.toggle("text-muted", !result.isLiked);
    const countLabel = result.likeCount > 0 ? ` (${result.likeCount})` : "";
    link.innerHTML = `${result.isLiked ? "Beğenildi" : "Beğen"}<span class="comment-like-count">${countLabel}</span>`;
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
  return false;
}

// Fills a post's comment box from the server. The first call (opening the box) loads once; force = true loads again,
// which is how a live "new comment" or a deletion shows up with the right buttons for this viewer.
async function loadComments(postId, force = false) {
  const container = document.getElementById(`comments-${postId}`);
  if (!container || (container.dataset.loaded && !force)) return;
  container.dataset.loaded = "1";
  const list = container.querySelector(".comments-list");
  try {
    const comments = await apiFetch(`/api/comments/by-post/${postId}`);
    if (comments.length) {
      list.innerHTML = comments.map(commentHtml).join("");
      delete list.dataset.empty;
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
    input.value = "";

    // The live "comment-added" event may have shown this comment before this response arrived (posting on your
    // own post always echoes back to you). Counting the actual rendered comments — instead of adding 1 to
    // whatever the label currently says — makes this safe no matter which one lands first.
    if (!list.querySelector(`[data-comment-id="${comment.id}"]`)) {
      if (list.dataset.empty) { list.innerHTML = ""; delete list.dataset.empty; }
      list.insertAdjacentHTML("beforeend", commentHtml(comment));
    }
    form.closest(".card[data-post-id]").querySelector(".comment-count-label").textContent =
      list.querySelectorAll("[data-comment-id]").length;
  } catch (err) {
    toast(err.message || "Yorum eklenemedi.");
  }
  return false;
}

async function removeComment(event, link) {
  event.preventDefault();
  const card = link.closest(".card[data-post-id]");
  const commentId = link.closest("[data-comment-id]").dataset.commentId;
  try {
    await apiFetch(`/api/comments/${commentId}`, { method: "DELETE" });
    // Replies go with their comment on the server, so reload the list instead of guessing what disappeared.
    await loadComments(card.dataset.postId, true);
    card.querySelector(".comment-count-label").textContent = card.querySelectorAll("[data-comment-id]").length;
    toast("Yorum silindi.");
  } catch (err) {
    toast(err.message || "Yorum silinemedi.");
  }
  return false;
}

async function toggleLike(btn) {
  const card = btn.closest(".card[data-post-id]");
  const postId = card.dataset.postId;
  btn.disabled = true;
  try {
    const result = await apiFetch(`/api/posts/${postId}/like`, { method: "POST" });
    const countEl = btn.querySelector(".like-count");
    countEl.dataset.count = result.likeCount;
    countEl.textContent = result.likeCount;
    card.querySelector(".like-count-label").textContent = result.likeCount;
    btn.classList.toggle("liked", result.isLiked);
    btn.querySelector(".like-icon").className = "like-icon bi " + (result.isLiked ? "bi-heart-fill" : "bi-heart");
  } catch (err) {
    toast(err.message || "Beğeni işlenemedi.");
  } finally {
    btn.disabled = false;
  }
}

async function removePost(postId, btn) {
  try {
    await apiFetch(`/api/posts/${postId}`, { method: "DELETE" });
    btn.closest(".card").remove();
    window.afterLivePostRemoved?.(); // the home page shows its "no posts yet" text when the last one is gone
    toast("Gönderi silindi.");
  } catch (err) {
    toast(err.message || "Gönderi silinemedi.");
  }
}

function copyPostLink(btn) {
  const url = window.location.href.split("#")[0] + "#post";
  if (navigator.clipboard) {
    navigator.clipboard.writeText(url).catch(() => {});
  }
  toast("Bağlantı kopyalandı.");
}
