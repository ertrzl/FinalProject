// "Arkadaşlık Önerileri": people you may know, with an "Ekle" button. Shared by home.html and explore.html, which
// both have a #suggestionsList element for it.

function suggestionCardHtml(user) {
  const mutualLabel = user.mutualFriendsCount > 0 ? `${user.mutualFriendsCount} ortak arkadaş` : "Yeni üye";
  return `
    <div class="d-flex align-items-center gap-2 mb-3" data-user-id="${user.id}">
      <img src="${user.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">
      <div class="flex-grow-1">
        <div class="fw-bold small"><a href="profile.html?id=${user.id}" class="text-body text-decoration-none">${escapeHtml(user.fullName)}</a></div>
        <div class="text-muted small">${mutualLabel}</div>
      </div>
      <button class="btn btn-outline-primary btn-sm rounded-pill" onclick="sendSuggestionRequest('${user.id}', this)">Ekle</button>
    </div>`;
}

async function loadSuggestions(take = 3) {
  const list = document.getElementById("suggestionsList");
  try {
    const suggestions = await apiFetch(`/api/friends/suggestions?take=${take}`);
    list.innerHTML = suggestions.length
      ? suggestions.map(suggestionCardHtml).join("")
      : `<div class="text-muted small">Şu an önerecek kimse yok.</div>`;
  } catch (err) {
    list.innerHTML = "";
  }
}

async function sendSuggestionRequest(userId, btn) {
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
