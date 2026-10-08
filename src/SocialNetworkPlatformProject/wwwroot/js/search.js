// search.html — real user search (F7) plus a bonus post/hashtag search.

const session = requireAuth();

const FRIENDSHIP_LABEL = {
  None: "Arkadaş Ekle",
  RequestSent: "İstek Gönderildi",
  RequestReceived: "İstek Bekliyor",
  Friends: "Arkadaşsınız"
};

function searchResultHtml(user) {
  const isSelf = user.id === session.userId;
  const mutualLabel = user.mutualFriendsCount > 0 ? `${user.mutualFriendsCount} ortak arkadaş` : "Ortak arkadaş yok";
  const label = FRIENDSHIP_LABEL[user.friendshipStatus] || "Arkadaş Ekle";
  const disabled = user.friendshipStatus !== "None" ? "disabled" : "";
  const btnClass = user.friendshipStatus === "None" ? "btn-outline-primary" : "btn-secondary";

  const actionHtml = isSelf
    ? `<a href="profile.html" class="btn btn-light btn-sm rounded-pill">Profilim</a>`
    : `<button class="btn ${btnClass} btn-sm rounded-pill" ${disabled} onclick="sendRequestTo('${user.id}', this)">${label}</button>`;

  return `
    <div class="d-flex align-items-center gap-3 p-3 border-bottom">
      <img src="${user.avatarUrl || DEFAULT_AVATAR}" class="avatar-md" alt="">
      <div class="flex-grow-1">
        <div class="fw-bold"><a href="${isSelf ? "profile.html" : `profile.html?id=${user.id}`}" class="text-dark text-decoration-none">${escapeHtml(user.fullName)}</a></div>
        <div class="text-muted small">@${escapeHtml(user.userName)}${isSelf ? " · sen" : ` · ${mutualLabel}`}</div>
      </div>
      ${actionHtml}
    </div>`;
}

function postResultHtml(post) {
  const heartIcon = post.isLikedByCurrentUser ? "bi-heart-fill" : "bi-heart";
  const likedClass = post.isLikedByCurrentUser ? "liked" : "";

  return `
    <div class="d-flex gap-3 p-3 border-bottom" data-post-id="${post.id}">
      <a href="profile.html?id=${post.authorId}"><img src="${post.authorAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm flex-shrink-0" alt=""></a>
      <div class="flex-grow-1 overflow-hidden">
        <div class="fw-bold small"><a href="profile.html?id=${post.authorId}" class="text-dark text-decoration-none">${escapeHtml(post.authorName)}</a></div>
        <div class="text-muted small mb-1"><a href="post.html?id=${post.id}" class="text-muted text-decoration-none">${timeAgo(post.createdAt)}</a></div>
        ${post.text ? `<p class="mb-2">${linkifyHashtags(escapeHtml(post.text))}</p>` : ""}
        ${post.mediaUrl ? (post.mediaType === "Video"
          ? `<video src="${post.mediaUrl}" class="rounded-3 mb-2" style="max-width:100%;max-height:280px;object-fit:cover;" controls></video>`
          : `<img src="${post.mediaUrl}" class="rounded-3 mb-2" style="max-width:100%;max-height:280px;object-fit:cover;" alt="">`) : ""}
        <button class="btn btn-sm p-0 text-muted fw-semibold like-btn ${likedClass}" onclick="togglePostResultLike(this)">
          <i class="like-icon bi ${heartIcon}"></i> <span class="like-count">${post.likeCount}</span>
        </button>
      </div>
    </div>`;
}

async function togglePostResultLike(btn) {
  const postId = btn.closest("[data-post-id]").dataset.postId;
  btn.disabled = true;
  try {
    const result = await apiFetch(`/api/posts/${postId}/like`, { method: "POST" });
    btn.querySelector(".like-count").textContent = result.likeCount;
    btn.querySelector(".like-icon").className = "like-icon bi " + (result.isLiked ? "bi-heart-fill" : "bi-heart");
    btn.classList.toggle("liked", result.isLiked);
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  } finally {
    btn.disabled = false;
  }
}

function switchSearchTab(tab) {
  document.getElementById("searchTabUsersBtn").classList.toggle("active", tab === "users");
  document.getElementById("searchTabPostsBtn").classList.toggle("active", tab === "posts");
  document.getElementById("searchResultsUsers").classList.toggle("d-none", tab !== "users");
  document.getElementById("searchResultsPosts").classList.toggle("d-none", tab !== "posts");
}

async function runSearch(term) {
  const users = document.getElementById("searchResultsUsers");
  const posts = document.getElementById("searchResultsPosts");
  document.getElementById("searchHeading").textContent = term ? `"${term}" için arama sonuçları` : "Arama sonuçları";

  if (!term) {
    users.innerHTML = `<div class="text-muted small">Bir kullanıcı adı yazarak arama yap.</div>`;
    posts.innerHTML = `<div class="text-muted small">Bir kelime ya da #hashtag yazarak arama yap.</div>`;
    return;
  }

  // A term starting with "#" is hashtag-only, so skip the user search — it can't match a username.
  // Both lists show 20 results at a time with a "Daha fazla yükle" button (pager.js).
  if (!term.startsWith("#")) {
    createPager({
      list: "searchResultsUsers",
      url: page => `/api/users/search?term=${encodeURIComponent(term)}&page=${page}&pageSize=20`,
      render: searchResultHtml,
      emptyHtml: `<div class="text-muted small py-3">"${escapeHtml(term)}" için sonuç bulunamadı.</div>`
    }).reload();
  } else {
    users.innerHTML = `<div class="text-muted small py-3">Hashtag'ler sadece gönderilerde aranır.</div>`;
  }

  createPager({
    list: "searchResultsPosts",
    url: page => `/api/posts/search?term=${encodeURIComponent(term)}&page=${page}&pageSize=20`,
    render: postResultHtml,
    emptyHtml: `<div class="text-muted small py-3">"${escapeHtml(term)}" için gönderi bulunamadı.</div>`
  }).reload();

  if (term.startsWith("#")) switchSearchTab("posts");
}

async function sendRequestTo(userId, btn) {
  btn.disabled = true;
  try {
    await apiFetch("/api/friends/requests", { method: "POST", body: { receiverId: userId } });
    btn.textContent = "İstek Gönderildi";
    btn.classList.remove("btn-outline-primary");
    btn.classList.add("btn-secondary");
    toast("Arkadaşlık isteği gönderildi.");
  } catch (err) {
    btn.disabled = false;
    toast(err.message || "İstek gönderilemedi.");
  }
}

const initialQuery = new URLSearchParams(window.location.search).get("q");
if (initialQuery) document.getElementById("searchInput").value = initialQuery;
runSearch(initialQuery || "");
