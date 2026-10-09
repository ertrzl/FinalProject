// Post cards with their comments and the like / edit / delete actions, shared by every page that shows posts
// (home, profile, group, the single-post page). The server decides what the current user may do (post.canEdit,
// post.canDelete, comment.canDelete) and what the card shows (post.isGroupPost); this file only draws the buttons
// and calls the API.

// The posts as they were last drawn, so "Düzenle" can fill its dialog without asking the server again.
const drawnPosts = new Map();

function postCardHtml(post) {
  drawnPosts.set(post.id, post);

  const privacyIcon = post.privacy === "FriendsOnly" ? "bi-people-fill" : "bi-globe-americas";
  const privacyLabel = post.privacy === "FriendsOnly" ? "Sadece Arkadaşlar" : "Herkese Açık";
  const likedClass = post.isLikedByCurrentUser ? "liked" : "";
  const heartIcon = post.isLikedByCurrentUser ? "bi-heart-fill" : "bi-heart";
  const authorUrl = `profile.html?id=${post.authorId}`;
  const postUrl = `post.html?id=${post.id}`;

  const menuItems = [
    post.canEdit ? `<li><button class="dropdown-item" onclick="editPost('${post.id}')"><i class="bi bi-pencil me-2"></i>Düzenle</button></li>` : "",
    post.canDelete ? `<li><button class="dropdown-item" onclick="removePost('${post.id}', this)"><i class="bi bi-trash me-2"></i>Gönderiyi Sil</button></li>` : ""
  ].join("");

  const menu = menuItems
    ? `<div class="dropdown">
         <button class="btn btn-sm btn-light rounded-circle" data-bs-toggle="dropdown"><i class="bi bi-three-dots"></i></button>
         <ul class="dropdown-menu dropdown-menu-end">${menuItems}</ul>
       </div>`
    : "";

  return `
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-4" data-post-id="${post.id}">
      <div class="d-flex align-items-center gap-3">
        <a href="${authorUrl}"><img src="${post.authorAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
        <div class="flex-grow-1">
          <div class="fw-bold"><a href="${authorUrl}" class="text-body text-decoration-none">${escapeHtml(post.authorName)}</a></div>
          <div class="text-muted small"><a href="${postUrl}" class="text-muted text-decoration-none">${timeAgo(post.createdAt)}</a>${post.isGroupPost ? "" : ` · <i class="bi ${privacyIcon}"></i> ${privacyLabel}`}</div>
        </div>
        ${menu}
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
        <button class="btn btn-sm flex-fill text-muted fw-semibold" onclick="copyPostLink(this)"><i class="bi bi-share"></i> Paylaş</button>
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

  // The server sends the comments thread by thread (a comment, then its replies); a reply is just drawn indented.
  // Replying to a reply answers the original comment, so a thread never goes deeper than one level.
  const replyTarget = c.parentCommentId || c.id;

  return `
    <div class="d-flex gap-2 mb-3 ${c.parentCommentId ? "ms-5" : ""}" data-comment-id="${c.id}" data-reply-to="${replyTarget}">
      <img src="${c.authorAvatarUrl || DEFAULT_AVATAR}" class="avatar-xs flex-shrink-0" alt="">
      <div class="comment-bubble flex-grow-1">
        <div class="fw-bold small c-name">${escapeHtml(c.authorName)}</div>
        <div class="small">${escapeHtml(c.text)}</div>
        <div class="small text-muted mt-1 d-flex gap-3">
          <span>${timeAgo(c.createdAt)}</span>
          <a href="#" class="comment-like-link ${likedClass}" onclick="return toggleCommentLike(event, this)">${likeLabel}<span class="comment-like-count">${likeCountLabel}</span></a>
          <a href="#" class="text-muted" onclick="return startReply(event, this)">Yanıtla</a>
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

// "Yanıtla": opens a small answer box right under the comment (a second click closes it again).
function startReply(event, link) {
  event.preventDefault();
  const row = link.closest("[data-comment-id]");
  const open = row.querySelector(".reply-form");
  if (open) {
    open.remove();
    return false;
  }

  const postId = link.closest(".card[data-post-id]").dataset.postId;
  row.querySelector(".comment-bubble").insertAdjacentHTML("beforeend", `
    <form class="reply-form d-flex gap-2 mt-2" onsubmit="return submitReply(event, '${postId}', '${row.dataset.replyTo}')">
      <input type="text" autocomplete="off" class="form-control form-control-sm rounded-pill">
      <button type="submit" class="btn btn-primary btn-sm rounded-pill">Yanıtla</button>
    </form>`);

  const input = row.querySelector(".reply-form input");
  input.placeholder = `${row.querySelector(".c-name").textContent} kişisine yanıt yaz...`;
  input.focus();
  return false;
}

async function submitReply(event, postId, parentCommentId) {
  event.preventDefault();
  const input = event.target.querySelector("input");
  const text = input.value.trim();
  if (!text) return false;

  const card = event.target.closest(".card[data-post-id]");
  try {
    await apiFetch("/api/comments", { method: "POST", body: { postId, text, parentCommentId } });
    // The server arranges the thread (a reply goes right under its comment), so the list is simply loaded again.
    await loadComments(postId, true);
    card.querySelector(".comment-count-label").textContent = card.querySelectorAll("[data-comment-id]").length;
  } catch (err) {
    toast(err.message || "Yanıt eklenemedi.");
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

// ---- Düzenle: a small dialog (made on first use, so no page needs its own copy) for the text and the privacy ----

function ensurePostEditModal() {
  if (document.getElementById("postEditModal")) return;

  document.body.insertAdjacentHTML("beforeend", `
    <div class="modal fade" id="postEditModal" tabindex="-1">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content rounded-4">
          <div class="modal-header">
            <h5 class="modal-title fw-bold">Gönderiyi Düzenle</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <div class="modal-body">
            <textarea id="postEditText" class="form-control" rows="5" maxlength="5000"></textarea>
            <div id="postEditPrivacyRow" class="mt-3">
              <label class="form-label small fw-semibold" for="postEditPrivacy">Kimler görebilir?</label>
              <select id="postEditPrivacy" class="form-select form-select-sm rounded-pill">
                <option value="Public">Herkese Açık</option>
                <option value="FriendsOnly">Sadece Arkadaşlar</option>
              </select>
            </div>
            <div class="text-danger small mt-2 d-none" id="postEditError"></div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-light rounded-pill" data-bs-dismiss="modal">Vazgeç</button>
            <button type="button" class="btn btn-primary rounded-pill" id="postEditSave" onclick="savePostEdit()">Kaydet</button>
          </div>
        </div>
      </div>
    </div>`);
}

function editPost(postId) {
  const post = drawnPosts.get(postId);
  if (!post) return;

  ensurePostEditModal();
  const modal = document.getElementById("postEditModal");
  modal.dataset.postId = postId;
  document.getElementById("postEditText").value = post.text || "";
  document.getElementById("postEditPrivacy").value = post.privacy;
  document.getElementById("postEditPrivacyRow").classList.toggle("d-none", post.isGroupPost); // a group post follows its group
  document.getElementById("postEditError").classList.add("d-none");
  bootstrap.Modal.getOrCreateInstance(modal).show();
}

async function savePostEdit() {
  const modal = document.getElementById("postEditModal");
  const error = document.getElementById("postEditError");
  const button = document.getElementById("postEditSave");
  button.disabled = true;

  try {
    const updated = await apiFetch(`/api/posts/${modal.dataset.postId}`, {
      method: "PUT",
      body: {
        text: document.getElementById("postEditText").value,
        privacy: document.getElementById("postEditPrivacy").value
      }
    });
    bootstrap.Modal.getInstance(modal).hide();
    replacePostCard(updated);
    toast("Gönderi güncellendi.");
  } catch (err) {
    error.textContent = err.message || "Kaydedilemedi.";
    error.classList.remove("d-none");
  } finally {
    button.disabled = false;
  }
}

// Draws a post again where it is on the page (after it was edited here, or on another device).
function replacePostCard(post) {
  const card = document.querySelector(`.card[data-post-id="${post.id}"]`);
  if (card) card.outerHTML = postCardHtml(post);
}

function copyPostLink(btn) {
  const postId = btn.closest(".card[data-post-id]").dataset.postId;
  const url = new URL(`post.html?id=${postId}`, window.location.href).href;
  if (navigator.clipboard) {
    navigator.clipboard.writeText(url).catch(() => {});
  }
  toast("Bağlantı kopyalandı.");
}
