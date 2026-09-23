// search.html — real user search wired to the backend. F7.

requireAuth();

const FRIENDSHIP_LABEL = {
  None: "Arkadaş Ekle",
  RequestSent: "İstek Gönderildi",
  RequestReceived: "İstek Bekliyor",
  Friends: "Arkadaşsınız"
};

function searchResultHtml(user) {
  const mutualLabel = user.mutualFriendsCount > 0 ? `${user.mutualFriendsCount} ortak arkadaş` : "Ortak arkadaş yok";
  const label = FRIENDSHIP_LABEL[user.friendshipStatus] || "Arkadaş Ekle";
  const disabled = user.friendshipStatus !== "None" ? "disabled" : "";
  const btnClass = user.friendshipStatus === "None" ? "btn-outline-primary" : "btn-secondary";

  return `
    <div class="d-flex align-items-center gap-3 p-3 border-bottom">
      <img src="${user.avatarUrl || DEFAULT_AVATAR}" class="avatar-md" alt="">
      <div class="flex-grow-1">
        <div class="fw-bold"><a href="profile.html?id=${user.id}" class="text-dark text-decoration-none">${escapeHtml(user.fullName)}</a></div>
        <div class="text-muted small">@${escapeHtml(user.userName)} · ${mutualLabel}</div>
      </div>
      <button class="btn ${btnClass} btn-sm rounded-pill" ${disabled} onclick="sendRequestTo('${user.id}', this)">${label}</button>
    </div>`;
}

async function runSearch(term) {
  const results = document.getElementById("searchResults");
  document.getElementById("searchHeading").textContent = `"${term}" için arama sonuçları`;

  if (!term) {
    results.innerHTML = `<div class="text-muted small">Bir kullanıcı adı yazarak arama yap.</div>`;
    return;
  }

  try {
    const result = await apiFetch(`/api/users/search?term=${encodeURIComponent(term)}&page=1&pageSize=20`);
    results.innerHTML = result.items.length
      ? result.items.map(searchResultHtml).join("")
      : `<div class="text-muted small py-3">"${escapeHtml(term)}" için sonuç bulunamadı.</div>`;
  } catch (err) {
    results.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
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
