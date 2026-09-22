// friends.html — real friend requests/friends/suggestions wired to the backend.

requireAuth();

function requestCardHtml(req, kind) {
  // kind: "incoming" (they sent it to me) or "sent" (I sent it to them)
  const otherName = kind === "incoming" ? req.senderName : req.receiverName;
  const otherAvatar = kind === "incoming" ? req.senderAvatarUrl : req.receiverAvatarUrl;
  const mutualLabel = req.mutualFriendsCount > 0 ? `${req.mutualFriendsCount} ortak arkadaş` : "Ortak arkadaş yok";

  const actions = kind === "incoming"
    ? `<button class="btn btn-primary btn-sm rounded-pill" onclick="respondRequest('${req.id}', true)">Kabul Et</button>
       <button class="btn btn-outline-danger btn-sm rounded-pill" onclick="respondRequest('${req.id}', false)">Reddet</button>`
    : `<button class="btn btn-outline-secondary btn-sm rounded-pill" onclick="cancelRequest('${req.id}')">İsteği İptal Et</button>`;

  return `
    <div class="request-card d-flex align-items-center gap-3 rounded-3 p-3 mb-2 bg-body-tertiary" data-request-id="${req.id}">
      <img src="${otherAvatar || DEFAULT_AVATAR}" class="avatar-md" alt="">
      <div class="flex-grow-1">
        <div class="fw-bold">${escapeHtml(otherName)}</div>
        <div class="text-muted small">${kind === "incoming" ? mutualLabel : "Bekleniyor..."}</div>
      </div>
      ${actions}
    </div>`;
}

function friendCardHtml(friend) {
  return `
    <div class="col-md-6 d-flex align-items-center gap-3 rounded-3 p-3 bg-body-tertiary">
      <img src="${friend.avatarUrl || DEFAULT_AVATAR}" class="avatar-md" alt="">
      <div class="flex-grow-1">
        <div class="fw-bold"><a href="profile.html?id=${friend.userId}" class="text-dark text-decoration-none">${escapeHtml(friend.name)}</a></div>
        <div class="text-muted small">${escapeHtml(friend.location || "")}</div>
      </div>
      <a href="messages.html?userId=${friend.userId}" class="btn btn-outline-primary btn-sm rounded-pill">Mesaj</a>
    </div>`;
}

function suggestionCardHtml(user) {
  const mutualLabel = user.mutualFriendsCount > 0 ? `${user.mutualFriendsCount} ortak arkadaş` : "Yeni üye";
  return `
    <div class="col-md-6 d-flex align-items-center gap-3 rounded-3 p-3 bg-body-tertiary" data-user-id="${user.id}">
      <img src="${user.avatarUrl || DEFAULT_AVATAR}" class="avatar-md" alt="">
      <div class="flex-grow-1">
        <div class="fw-bold"><a href="profile.html?id=${user.id}" class="text-dark text-decoration-none">${escapeHtml(user.fullName)}</a></div>
        <div class="text-muted small">${mutualLabel}</div>
      </div>
      <button class="btn btn-outline-primary btn-sm rounded-pill" onclick="sendRequestTo('${user.id}', this)">Ekle</button>
    </div>`;
}

async function loadIncoming() {
  const list = document.getElementById("incomingRequestsList");
  try {
    const requests = await apiFetch("/api/friends/requests/incoming");
    document.getElementById("incomingCount").textContent = requests.length;
    list.innerHTML = requests.length
      ? requests.map(r => requestCardHtml(r, "incoming")).join("")
      : `<div class="text-muted small">Bekleyen arkadaşlık isteğin yok.</div>`;
  } catch (err) {
    list.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
}

async function loadSent() {
  const list = document.getElementById("sentRequestsList");
  try {
    const requests = await apiFetch("/api/friends/requests/sent");
    list.innerHTML = requests.length
      ? requests.map(r => requestCardHtml(r, "sent")).join("")
      : `<div class="text-muted small">Gönderilen istek yok.</div>`;
  } catch (err) {
    list.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
}

async function loadFriends() {
  const list = document.getElementById("friendsList");
  try {
    const friends = await apiFetch("/api/friends");
    document.getElementById("friendsCount").textContent = friends.length;
    list.innerHTML = friends.length
      ? friends.map(friendCardHtml).join("")
      : `<div class="text-muted small">Henüz arkadaşın yok.</div>`;
  } catch (err) {
    list.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
}

async function loadSuggestions() {
  const list = document.getElementById("suggestionsList");
  try {
    const suggestions = await apiFetch("/api/friends/suggestions?take=10");
    list.innerHTML = suggestions.length
      ? suggestions.map(suggestionCardHtml).join("")
      : `<div class="text-muted small">Şu an önerecek kimse yok.</div>`;
  } catch (err) {
    list.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
}

async function respondRequest(requestId, accepted) {
  try {
    await apiFetch(`/api/friends/requests/${requestId}/${accepted ? "accept" : "decline"}`, { method: "POST" });
    toast(accepted ? "Arkadaşlık isteği kabul edildi." : "İstek reddedildi.");
    await Promise.all([loadIncoming(), loadFriends()]);
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

async function cancelRequest(requestId) {
  try {
    await apiFetch(`/api/friends/requests/${requestId}/cancel`, { method: "POST" });
    toast("İstek iptal edildi.");
    await loadSent();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
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

loadIncoming();
loadSent();
loadFriends();
loadSuggestions();
