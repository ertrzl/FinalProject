// Post cards with their comments and the like / edit / delete actions, shared by every page that shows posts
// (home, profile, group, the single-post page). The server decides what the current user may do (post.canEdit,
// post.canDelete, comment.canDelete) and what the card shows (post.isGroupPost); this file only draws the buttons
// and calls the API.

// The posts as they were last drawn, so "Düzenle" can fill its dialog without asking the server again.
const drawnPosts = new Map();

// What a post holds, for the filter tabs of the inbox design ("photo", "poll"; both, one or none).
function postKinds(post) {
  return [post.media?.length ? "photo" : "", post.poll ? "poll" : ""].filter(Boolean).join(" ");
}

function postCardHtml(post) {
  drawnPosts.set(post.id, post);

  const privacyIcon = post.privacy === "FriendsOnly" ? "bi-people-fill" : "bi-globe-americas";
  const privacyLabel = post.privacy === "FriendsOnly" ? "Sadece Arkadaşlar" : "Herkese Açık";
  const authorUrl = `profile.html?id=${post.authorId}`;
  const postUrl = `post.html?id=${post.id}`;

  const menuItems = [
    `<li><button class="dropdown-item" onclick="toggleSavePost('${post.id}', this)"><i class="bi ${post.isSavedByCurrentUser ? "bi-bookmark-fill" : "bi-bookmark"} me-2"></i><span class="save-label">${post.isSavedByCurrentUser ? "Kaydı Kaldır" : "Kaydet"}</span></button></li>`,
    post.canEdit ? `<li><button class="dropdown-item" onclick="editPost('${post.id}')"><i class="bi bi-pencil me-2"></i>Düzenle</button></li>` : "",
    post.canDelete ? `<li><button class="dropdown-item" onclick="removePost('${post.id}', this)"><i class="bi bi-trash me-2"></i>Gönderiyi Sil</button></li>` : "",
    post.authorId !== getSession()?.userId ? `<li><button class="dropdown-item" onclick="reportDialog('Post', '${post.id}')"><i class="bi bi-flag me-2"></i>${i18n("report.menuItem")}</button></li>` : ""
  ].join("");

  const menu = `<div class="dropdown">
         <button class="btn btn-sm btn-light rounded-circle" data-bs-toggle="dropdown" aria-label="Gönderi seçenekleri"><i class="bi bi-three-dots"></i></button>
         <ul class="dropdown-menu dropdown-menu-end">${menuItems}</ul>
       </div>`;

  return `
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-4" data-post-id="${post.id}" data-kind="${postKinds(post)}">
      <div class="d-flex align-items-center gap-3">
        <a href="${authorUrl}"><img src="${post.authorAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
        <div class="flex-grow-1">
          <div class="fw-bold"><a href="${authorUrl}" class="text-body text-decoration-none">${escapeHtml(post.authorName)}</a></div>
          <div class="text-muted small"><a href="${postUrl}" class="text-muted text-decoration-none">${timeAgo(post.createdAt)}</a>${post.isGroupPost ? "" : ` · <i class="bi ${privacyIcon}"></i> ${privacyLabel}`}</div>
        </div>
        ${menu}
      </div>
      ${post.text ? `<p class="mt-3 mb-2">${richTextHtml(post.text, post.mentions)}</p>` : ""}
      ${post.isShare ? sharedPostHtml(post) : ""}
      ${postMediaHtml(post)}
      ${post.poll ? pollHtml(post) : ""}
      <div class="d-flex justify-content-between text-muted small py-2 border-bottom">
        ${reactionSummaryHtml(post)}
        <span><span class="comment-count-label">${post.commentCount}</span> yorum${post.shareCount > 0 ? ` · ${post.shareCount} paylaşım` : ""}</span>
      </div>
      <div class="d-flex pt-1">
        <div class="reaction-wrap flex-fill position-relative">
          ${reactionButtonHtml(post)}
          <div class="reaction-bar">${Object.entries(REACTIONS).map(([type, r]) =>
            `<button type="button" title="${r.label}" onclick="reactToPost(this, '${type}')">${r.emoji}</button>`).join("")}</div>
        </div>
        <button class="btn btn-sm flex-fill text-muted fw-semibold" data-bs-toggle="collapse" data-bs-target="#comments-${post.id}" onclick="loadComments('${post.id}')">
          <i class="bi bi-chat"></i> Yorum Yap
        </button>
        <div class="dropdown flex-fill d-flex">
          <button class="btn btn-sm flex-fill text-muted fw-semibold" data-bs-toggle="dropdown"><i class="bi bi-share"></i> Paylaş</button>
          <ul class="dropdown-menu dropdown-menu-end">
            <li><button class="dropdown-item" onclick="openShare('${post.id}')"><i class="bi bi-arrow-repeat me-2"></i>Gönderiyi paylaş</button></li>
            <li><button class="dropdown-item" onclick="copyPostLink(this)"><i class="bi bi-link-45deg me-2"></i>Bağlantıyı kopyala</button></li>
          </ul>
        </div>
      </div>
      <div class="collapse comments-collapse mt-3 pt-3 border-top" id="comments-${post.id}">
        <div class="comments-list mb-2"></div>
        <form class="d-flex gap-2 align-items-center" onsubmit="return submitComment(event, '${post.id}')">
          <img src="${getSession()?.avatarUrl || DEFAULT_AVATAR}" class="avatar-xs" alt="">
          <input type="text" autocomplete="off" data-mention class="form-control form-control-sm rounded-pill" placeholder="Bir yorum yaz...">
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
  const reportLink = c.authorId !== getSession()?.userId
    ? `<a href="#" class="text-muted" onclick="event.preventDefault(); reportDialog('Comment', '${c.id}')">${i18n("report.menuItem")}</a>`
    : "";

  // The server sends the comments thread by thread (a comment, then its replies); a reply is just drawn indented.
  // Replying to a reply answers the original comment, so a thread never goes deeper than one level.
  const replyTarget = c.parentCommentId || c.id;

  return `
    <div class="d-flex gap-2 mb-3 ${c.parentCommentId ? "ms-5" : ""}" data-comment-id="${c.id}" data-reply-to="${replyTarget}">
      <img src="${c.authorAvatarUrl || DEFAULT_AVATAR}" class="avatar-xs flex-shrink-0" alt="">
      <div class="comment-bubble flex-grow-1">
        <div class="fw-bold small c-name"><a href="profile.html?id=${c.authorId}" class="text-body text-decoration-none">${escapeHtml(c.authorName)}</a></div>
        <div class="small">${richTextHtml(c.text, c.mentions)}</div>
        <div class="small text-muted mt-1 d-flex gap-3">
          <span>${timeAgo(c.createdAt)}</span>
          <a href="#" class="comment-like-link ${likedClass}" onclick="return toggleCommentLike(event, this)">${likeLabel}<span class="comment-like-count">${likeCountLabel}</span></a>
          <a href="#" class="text-muted" onclick="return startReply(event, this)">Yanıtla</a>
          ${deleteLink}${reportLink}
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
// The comments come a page at a time (the server pages the top-level comments and sends each one's replies with it).
const COMMENT_PAGE_SIZE = 20;

async function loadComments(postId, force = false) {
  const container = document.getElementById(`comments-${postId}`);
  if (!container || (container.dataset.loaded && !force)) return;
  container.dataset.loaded = "1";
  const list = container.querySelector(".comments-list");
  try {
    // A reload (after a deletion, a reply or a live push) brings back as many pages as were showing, not only the first.
    const wanted = force ? Math.max(Number(container.dataset.pages) || 1, 1) : 1;
    let comments = [];
    let page = null;
    let shown = 0;
    while (shown < wanted) {
      page = await apiFetch(`/api/comments/by-post/${postId}?page=${shown + 1}&pageSize=${COMMENT_PAGE_SIZE}`);
      comments = comments.concat(page.items);
      shown++;
      if (!page.hasMore) break;
    }
    container.dataset.pages = String(shown);

    if (comments.length) {
      list.innerHTML = comments.map(commentHtml).join("");
      delete list.dataset.empty;
    } else {
      list.innerHTML = `<div class="text-muted small mb-2">Henüz yorum yok.</div>`;
      list.dataset.empty = "1";
    }
    showMoreCommentsButton(container, postId, page.hasMore);
    setCommentCount(container, page.commentCount);
  } catch (err) {
    list.innerHTML = `<div class="text-danger small">${escapeHtml(err.message)}</div>`;
  }
}

// "Daha fazla yorum göster": draws the next page under the ones already showing.
async function loadMoreComments(postId) {
  const container = document.getElementById(`comments-${postId}`);
  const list = container.querySelector(".comments-list");
  const button = container.querySelector(".comments-more");
  const next = (Number(container.dataset.pages) || 1) + 1;
  button.disabled = true;
  try {
    const page = await apiFetch(`/api/comments/by-post/${postId}?page=${next}&pageSize=${COMMENT_PAGE_SIZE}`);
    // A comment that a live push or the viewer's own post already put on the screen is not drawn twice.
    list.insertAdjacentHTML("beforeend", page.items.filter(c => !list.querySelector(`[data-comment-id="${c.id}"]`)).map(commentHtml).join(""));
    container.dataset.pages = String(next);
    showMoreCommentsButton(container, postId, page.hasMore);
    setCommentCount(container, page.commentCount);
  } catch (err) {
    toast(err.message || "Yorumlar yüklenemedi.");
    button.disabled = false;
  }
}

function showMoreCommentsButton(container, postId, hasMore) {
  container.querySelector(".comments-more")?.remove();
  if (!hasMore) return;
  container.querySelector(".comments-list").insertAdjacentHTML("afterend",
    `<button type="button" class="comments-more btn btn-link btn-sm p-0 mb-3" onclick="loadMoreComments('${postId}')">Daha fazla yorum göster</button>`);
}

// The number next to "yorum" is the post's whole count, not what happens to be drawn.
function setCommentCount(container, count) {
  const label = container.closest(".card[data-post-id]")?.querySelector(".comment-count-label");
  if (label) label.textContent = count;
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

    // The live "comment-added" event may have shown this comment (and the new count) before this response arrived
    // (posting on your own post always echoes back to you). Only a comment that is not on the screen yet is added to the
    // count, so it is right no matter which one lands first. (Not every comment is drawn: they come a page at a time.)
    if (!list.querySelector(`[data-comment-id="${comment.id}"]`)) {
      if (list.dataset.empty) { list.innerHTML = ""; delete list.dataset.empty; }
      list.insertAdjacentHTML("beforeend", commentHtml(comment));
      const label = form.closest(".card[data-post-id]").querySelector(".comment-count-label");
      label.textContent = (Number(label.textContent) || 0) + 1;
    }
  } catch (err) {
    toast(err.message || "Yorum eklenemedi.");
  }
  return false;
}

async function removeComment(event, link) {
  event.preventDefault();
  if (!(await confirmDialog(i18n("confirm.deleteComment")))) return;

  const card = link.closest(".card[data-post-id]");
  const commentId = link.closest("[data-comment-id]").dataset.commentId;
  try {
    await apiFetch(`/api/comments/${commentId}`, { method: "DELETE" });
    // Replies go with their comment on the server, so reload the list instead of guessing what disappeared.
    await loadComments(card.dataset.postId, true);
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
      <input type="text" autocomplete="off" data-mention class="form-control form-control-sm rounded-pill">
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

  try {
    await apiFetch("/api/comments", { method: "POST", body: { postId, text, parentCommentId } });
    // The server arranges the thread (a reply goes right under its comment), so the list is simply loaded again.
    await loadComments(postId, true);
  } catch (err) {
    toast(err.message || "Yanıt eklenemedi.");
  }
  return false;
}

// ---- Tepkiler: the "Beğen" button, the emoji bar above it and the "who reacted" list ----

// The button under a post: the viewer's own reaction (emoji + name) when there is one, a plain "Beğen" otherwise.
function reactionButtonHtml(post) {
  const mine = post.myReaction ? REACTIONS[post.myReaction] : null;
  return `<button type="button" class="btn btn-sm w-100 fw-semibold like-btn ${mine ? "liked" : "text-muted"}" data-my-reaction="${post.myReaction || ""}" onclick="toggleLike(this)">${
    mine ? `<span class="reaction-emoji">${mine.emoji}</span> ${mine.label}` : `<i class="like-icon bi bi-heart"></i> Beğen`}</button>`;
}

// "👍❤️ 12 tepki": the three most used reactions and the total; a click lists who reacted.
function reactionSummaryHtml(post) {
  const icons = (post.reactions || []).slice(0, 3).map(r => REACTIONS[r.type]?.emoji || "").join("");
  const clickable = post.likeCount > 0 ? `role="button" onclick="showReactors('${post.id}')"` : "";
  return `<span class="reaction-summary" ${clickable}><span class="reaction-icons">${icons || `<i class="bi bi-heart-fill text-danger"></i>`}</span> <span class="like-count-label">${post.likeCount}</span> tepki</span>`;
}

// Applies what the server answered (or pushed) to a card. `mine` is false for a push about somebody else's reaction.
function applyReactionResult(card, result, mine = true) {
  const summary = card.querySelector(".reaction-summary");
  if (summary) summary.outerHTML = reactionSummaryHtml({ id: card.dataset.postId, likeCount: result.likeCount, reactions: result.reactions });

  if (mine) {
    const button = card.querySelector(".like-btn");
    if (button) button.outerHTML = reactionButtonHtml({ myReaction: result.myReaction });
  }

  const drawn = drawnPosts.get(card.dataset.postId);
  if (drawn) {
    drawn.likeCount = result.likeCount;
    drawn.reactions = result.reactions;
    if (mine) drawn.myReaction = result.myReaction;
  }
}

// The button itself: takes any reaction back, or likes when there is none.
async function toggleLike(btn) {
  if (btn.dataset.longPress) { delete btn.dataset.longPress; return; } // the press only opened the emoji bar
  const card = btn.closest(".card[data-post-id]");
  btn.disabled = true;
  try {
    applyReactionResult(card, await apiFetch(`/api/posts/${card.dataset.postId}/like`, { method: "POST" }));
  } catch (err) {
    toast(err.message || "Beğeni işlenemedi.");
    btn.disabled = false;
  }
}

// One emoji of the bar: sets that reaction, or takes it back when it is already the viewer's.
async function reactToPost(emojiButton, type) {
  const card = emojiButton.closest(".card[data-post-id]");
  const current = card.querySelector(".like-btn")?.dataset.myReaction;
  card.querySelector(".reaction-wrap").classList.remove("open");
  try {
    const result = await apiFetch(`/api/posts/${card.dataset.postId}/reaction`, {
      method: "PUT",
      body: { type: type === current ? null : type }
    });
    applyReactionResult(card, result);
  } catch (err) {
    toast(err.message || "Tepki işlenemedi.");
  }
}

// On a touch screen there is no hover: pressing the button for a moment opens the emoji bar, a tap elsewhere closes it.
(function setUpReactionBarOnTouch() {
  let timer = null;

  document.addEventListener("touchstart", event => {
    const button = event.target.closest(".reaction-wrap .like-btn");
    if (!button) { document.querySelectorAll(".reaction-wrap.open").forEach(w => w.classList.remove("open")); return; }
    timer = setTimeout(() => {
      button.closest(".reaction-wrap").classList.add("open");
      button.dataset.longPress = "1";
    }, 450);
  }, { passive: true });

  ["touchend", "touchmove", "touchcancel"].forEach(name =>
    document.addEventListener(name, () => clearTimeout(timer), { passive: true }));
})();

function ensureReactorsModal() {
  if (document.getElementById("reactorsModal")) return;

  document.body.insertAdjacentHTML("beforeend", `
    <div class="modal fade" id="reactorsModal" tabindex="-1">
      <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
        <div class="modal-content rounded-4">
          <div class="modal-header">
            <h5 class="modal-title fw-bold">Tepkiler</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <div class="modal-body">
            <div id="reactorsTabs" class="d-flex flex-wrap gap-2 mb-3"></div>
            <div id="reactorsList"></div>
          </div>
        </div>
      </div>
    </div>`);
}

// The people who reacted, newest first (the server sends the latest 100), with a filter per reaction.
async function showReactors(postId) {
  ensureReactorsModal();
  const list = document.getElementById("reactorsList");
  const tabs = document.getElementById("reactorsTabs");
  list.innerHTML = `<div class="text-muted small">Yükleniyor...</div>`;
  tabs.innerHTML = "";
  bootstrap.Modal.getOrCreateInstance(document.getElementById("reactorsModal")).show();

  try {
    const people = await apiFetch(`/api/posts/${postId}/reactions`);
    const draw = type => {
      const shown = type ? people.filter(p => p.type === type) : people;
      list.innerHTML = shown.map(p => `
        <a href="profile.html?id=${p.userId}" class="d-flex align-items-center gap-3 text-decoration-none text-body mb-3">
          <img src="${p.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">
          <span class="flex-grow-1 fw-semibold">${escapeHtml(p.fullName)}</span>
          <span style="font-size:22px;">${REACTIONS[p.type]?.emoji || ""}</span>
        </a>`).join("") || `<div class="text-muted small">Henüz kimse tepki vermedi.</div>`;
      tabs.querySelectorAll("button").forEach(b => b.classList.toggle("active", (b.dataset.type || "") === (type || "")));
    };

    const counts = {};
    people.forEach(p => { counts[p.type] = (counts[p.type] || 0) + 1; });
    tabs.innerHTML = [`<button type="button" class="btn btn-sm btn-outline-primary rounded-pill" data-type="">Tümü ${people.length}</button>`]
      .concat(Object.entries(counts).map(([type, n]) => `<button type="button" class="btn btn-sm btn-outline-primary rounded-pill" data-type="${type}">${REACTIONS[type]?.emoji || ""} ${n}</button>`))
      .join("");
    tabs.querySelectorAll("button").forEach(b => b.addEventListener("click", () => draw(b.dataset.type)));
    draw("");
  } catch (err) {
    list.innerHTML = `<div class="text-danger small">${escapeHtml(err.message)}</div>`;
  }
}

// ---- Paylaş: a post you share shows the original in a frame under your own words ----

// The original inside a share. When the viewer may not see it (deleted, or hidden from them) there is only a note.
function sharedPostHtml(post) {
  const s = post.sharedPost;
  if (!s) {
    return `<div class="shared-post text-muted small mb-2"><i class="bi bi-slash-circle me-1"></i>Bu gönderi silinmiş ya da artık görüntülenemiyor.</div>`;
  }

  return `
    <div class="shared-post mb-2">
      <div class="d-flex align-items-center gap-2 mb-2">
        <a href="profile.html?id=${s.authorId}"><img src="${s.authorAvatarUrl || DEFAULT_AVATAR}" class="avatar-xs" alt=""></a>
        <div>
          <a href="profile.html?id=${s.authorId}" class="fw-bold small text-body text-decoration-none">${escapeHtml(s.authorName)}</a>
          <div class="text-muted" style="font-size:12px;"><a href="post.html?id=${s.id}" class="text-muted text-decoration-none">${timeAgo(s.createdAt)}</a></div>
        </div>
      </div>
      ${s.text ? `<p class="mb-2">${richTextHtml(s.text, s.mentions)}</p>` : ""}
      ${postMediaHtml(s)}
    </div>`;
}

function ensureShareModal() {
  if (document.getElementById("postShareModal")) return;

  document.body.insertAdjacentHTML("beforeend", `
    <div class="modal fade" id="postShareModal" tabindex="-1">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content rounded-4">
          <div class="modal-header">
            <h5 class="modal-title fw-bold">Gönderiyi Paylaş</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
          </div>
          <div class="modal-body">
            <textarea id="postShareText" data-mention class="form-control" rows="3" maxlength="5000" placeholder="Bir şey eklemek ister misin? (isteğe bağlı)"></textarea>
            <div class="mt-3">
              <label class="form-label small fw-semibold" for="postSharePrivacy">Kimler görebilir?</label>
              <select id="postSharePrivacy" class="form-select form-select-sm rounded-pill">
                <option value="Public">Herkese Açık</option>
                <option value="FriendsOnly">Sadece Arkadaşlar</option>
              </select>
            </div>
            <div class="text-danger small mt-2 d-none" id="postShareError"></div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-light rounded-pill" data-bs-dismiss="modal">Vazgeç</button>
            <button type="button" class="btn btn-primary rounded-pill" id="postShareSend" onclick="submitShare()">Paylaş</button>
          </div>
        </div>
      </div>
    </div>`);
}

function openShare(postId) {
  ensureShareModal();
  const modal = document.getElementById("postShareModal");
  modal.dataset.postId = postId;
  document.getElementById("postShareText").value = "";
  document.getElementById("postSharePrivacy").value = "Public";
  document.getElementById("postShareError").classList.add("d-none");
  bootstrap.Modal.getOrCreateInstance(modal).show();
}

async function submitShare() {
  const modal = document.getElementById("postShareModal");
  const error = document.getElementById("postShareError");
  const button = document.getElementById("postShareSend");
  button.disabled = true;

  try {
    await apiFetch(`/api/posts/${modal.dataset.postId}/share`, {
      method: "POST",
      body: {
        text: document.getElementById("postShareText").value,
        privacy: document.getElementById("postSharePrivacy").value
      }
    });
    bootstrap.Modal.getInstance(modal).hide();
    toast("Gönderi paylaşıldı.");
  } catch (err) {
    error.textContent = err.message || "Paylaşılamadı.";
    error.classList.remove("d-none");
  } finally {
    button.disabled = false;
  }
}

// The "Kaydet" item of a post's menu. On the saved-posts page a removed post leaves the list (afterPostUnsaved).
async function toggleSavePost(postId, item) {
  try {
    const saved = await apiFetch(`/api/posts/${postId}/save`, { method: "POST" });
    const card = item.closest(".card[data-post-id]");
    item.querySelector(".save-label").textContent = saved ? "Kaydı Kaldır" : "Kaydet";
    item.querySelector("i").className = `bi ${saved ? "bi-bookmark-fill" : "bi-bookmark"} me-2`;
    const drawn = drawnPosts.get(postId);
    if (drawn) drawn.isSavedByCurrentUser = saved;
    toast(saved ? "Gönderi kaydedildi." : "Kayıt kaldırıldı.");
    if (!saved) window.afterPostUnsaved?.(card);
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

// ---- Anket: the answers of a poll as buttons (before voting) or as bars with the results ----

function pollTimeLeft(endsAt) {
  const end = new Date(/Z$|[+-]\d\d:\d\d$/.test(endsAt) ? endsAt : `${endsAt}Z`).getTime();
  const minutes = Math.max(1, Math.round((end - Date.now()) / 60000));
  if (minutes < 60) return `${minutes} dakika kaldı`;
  const hours = Math.round(minutes / 60);
  return hours < 48 ? `${hours} saat kaldı` : `${Math.round(hours / 24)} gün kaldı`;
}

// What the server allows (post.poll.showResults / canVote) decides everything: this only draws it.
function pollHtml(post) {
  const poll = post.poll;

  const rows = poll.options.map(option => {
    const vote = poll.canVote ? `onclick="votePoll('${post.id}', '${option.id}')"` : "";

    if (!poll.showResults) {
      return `<button type="button" class="btn btn-outline-primary w-100 text-start rounded-pill mb-2" ${vote}>${escapeHtml(option.text)}</button>`;
    }

    const mine = poll.myOptionId === option.id;
    return `
      <div class="poll-row ${poll.canVote ? "poll-clickable" : ""} ${mine ? "poll-mine" : ""}" ${poll.canVote ? `role="button"` : ""} ${vote}>
        <div class="poll-fill" style="width:${option.percent}%"></div>
        <span class="poll-text">${mine ? `<i class="bi bi-check-circle-fill me-1"></i>` : ""}${escapeHtml(option.text)}</span>
        <span class="poll-percent">${option.percent}% <span class="text-muted">(${option.voteCount})</span></span>
      </div>`;
  }).join("");

  return `
    <div class="post-poll mb-2">
      ${rows}
      <div class="text-muted small">${poll.totalVotes} oy · ${poll.isEnded ? "Anket sona erdi" : pollTimeLeft(poll.endsAt)}</div>
    </div>`;
}

// Draws only the poll again (after a vote here, or a push that somebody voted).
function replacePoll(post) {
  const drawn = drawnPosts.get(post.id);
  if (drawn) drawn.poll = post.poll;
  const block = document.querySelector(`.card[data-post-id="${post.id}"] .post-poll`);
  if (block && post.poll) block.outerHTML = pollHtml(post);
}

async function votePoll(postId, optionId) {
  try {
    replacePoll(await apiFetch(`/api/posts/${postId}/poll/vote`, { method: "POST", body: { optionId } }));
  } catch (err) {
    toast(err.message || "Oy verilemedi.");
  }
}

async function removePost(postId, btn) {
  if (!(await confirmDialog(i18n("confirm.deletePost")))) return;

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
            <textarea id="postEditText" data-mention class="form-control" rows="5" maxlength="5000"></textarea>
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
