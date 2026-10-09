// group.html — a group's own wall: header, join/leave/delete, a members-only composer, and its posts.

const session = requireAuth();
const groupId = new URLSearchParams(window.location.search).get("id");

let currentGroup = null;

if (!groupId) {
  window.location.href = "groups.html";
}

function actionAreaHtml(group) {
  if (group.isCurrentUserAdmin) {
    return `
      <button class="btn btn-light border rounded-pill" data-bs-toggle="modal" data-bs-target="#editGroupModal"><i class="bi bi-pencil me-1"></i>Düzenle</button>
      <button class="btn btn-outline-danger rounded-pill" onclick="deleteGroupPage()"><i class="bi bi-trash me-1"></i>Grubu Sil</button>`;
  }
  if (group.isCurrentUserMember) {
    return `<button class="btn btn-light border rounded-pill" onclick="leaveGroupPage()">Gruptan Ayrıl</button>`;
  }
  if (group.privacy === "Private") {
    return `<span class="badge text-bg-light border text-muted fw-normal">Bu grup gizli</span>`;
  }
  return `<button class="btn btn-primary rounded-pill" onclick="joinGroupPage()">Gruba Katıl</button>`;
}

function renderGroup(group) {
  currentGroup = group;
  document.title = group.name + " | SocialNet";

  const cover = document.getElementById("groupCover");
  cover.style.backgroundImage = group.coverImageUrl ? `url(${group.coverImageUrl})` : "";
  cover.style.backgroundSize = "cover";
  cover.style.backgroundPosition = "center";

  document.getElementById("groupName").textContent = group.name;
  document.getElementById("groupDescription").textContent = group.description || "";
  document.getElementById("groupMemberCount").textContent = group.memberCount;
  document.getElementById("groupPrivacyIcon").className = "bi " + (group.privacy === "Private" ? "bi-lock-fill" : "bi-globe-americas");
  document.getElementById("groupPrivacyLabel").textContent = group.privacy === "Private" ? "Gizli Grup" : "Genel Grup";
  document.getElementById("groupActionArea").innerHTML = actionAreaHtml(group);

  document.getElementById("composerAvatar").src = session.avatarUrl || DEFAULT_AVATAR;
  document.getElementById("groupComposerWrap").classList.toggle("d-none", !group.isCurrentUserMember);
  document.getElementById("groupNonMemberNotice").classList.toggle("d-none", group.isCurrentUserMember);

  renderMembers(group);

  // Events are for members only: non-members never see the tab. Admins and moderators may create group events.
  document.getElementById("groupTabs").classList.toggle("d-none", !group.isCurrentUserMember);
  document.getElementById("groupEventCreateBtn").classList.toggle("d-none", !(group.isCurrentUserAdmin || group.isCurrentUserModerator));
  groupEventsLoaded = false;

  if (group.isCurrentUserAdmin) {
    document.getElementById("editGroupName").value = group.name;
    document.getElementById("editGroupDescription").value = group.description || "";
    document.getElementById("editGroupPrivacy").value = group.privacy;
    loadJoinRequests();
    document.getElementById("groupInviteSection").classList.remove("d-none");
    document.getElementById("groupInviteSearchInput").value = "";
    document.getElementById("groupInviteResultsList").innerHTML = "";
  } else {
    document.getElementById("groupJoinRequestsSection").classList.add("d-none");
    document.getElementById("groupInviteSection").classList.add("d-none");
  }
}

// ---- Events tab: upcoming events of this group (members only) ----

let groupEventsLoaded = false;

function groupEventRowHtml(ev) {
  return `
    <a href="event.html?id=${ev.id}" class="d-flex gap-3 text-decoration-none text-body p-2 rounded-3 border mb-2 align-items-center">
      <img src="${ev.coverImageUrl || NO_PHOTO}" class="rounded-3 flex-shrink-0" style="width:72px;height:72px;object-fit:cover;" alt="">
      <div style="min-width:0;">
        <div class="text-danger fw-bold small"><i class="bi bi-calendar3 me-1"></i>${formatEventRange(ev.startsAt, ev.endsAt)} ${eventStateBadgeHtml(ev)}</div>
        <div class="fw-bold text-truncate">${escapeHtml(ev.title)}</div>
        <div class="text-muted small text-truncate">${eventPlaceHtml(ev)} · ${eventAttendanceText(ev)}${ev.isFull ? " · Dolu" : ""}${ev.currentUserStatus === "Going" ? " · katılıyorsun" : ev.currentUserStatus === "Interested" ? " · ilgileniyorsun" : ""}</div>
      </div>
    </a>`;
}

async function loadGroupEvents(force) {
  if (groupEventsLoaded && !force) return;
  const box = document.getElementById("groupEventsList");
  try {
    const events = await apiFetch(`/api/events/group/${groupId}`);
    box.innerHTML = events.length
      ? events.map(groupEventRowHtml).join("")
      : `<div class="text-muted small text-center py-4">Yaklaşan grup etkinliği yok.</div>`;
    groupEventsLoaded = true;
  } catch (err) {
    box.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
}

function createGroupEvent() {
  openEventForm(null, () => loadGroupEvents(true), { group: { id: groupId, name: currentGroup.name } });
}

// ---- Admin-only: edit group details ----

async function saveGroupEdit() {
  const name = document.getElementById("editGroupName").value.trim();
  if (!name) {
    document.getElementById("editGroupName").classList.add("is-invalid");
    setTimeout(() => document.getElementById("editGroupName").classList.remove("is-invalid"), 1200);
    return;
  }

  try {
    const formData = new FormData();
    formData.append("name", name);
    formData.append("description", document.getElementById("editGroupDescription").value.trim());
    formData.append("privacy", document.getElementById("editGroupPrivacy").value);
    const coverFile = document.getElementById("editGroupCoverInput").files[0];
    if (coverFile) formData.append("coverImage", coverFile);

    const group = await apiFetchForm(`/api/groups/${groupId}`, { method: "PUT", body: formData });
    bootstrap.Modal.getInstance(document.getElementById("editGroupModal"))?.hide();
    document.getElementById("editGroupCoverInput").value = "";
    renderGroup(group);
    toast("Grup güncellendi.");
  } catch (err) {
    toast(err.message || "Grup güncellenemedi.");
  }
}

// ---- Admin-only: pending join requests for private groups ----

function joinRequestRowHtml(r) {
  return `
    <div class="d-flex align-items-center gap-2 py-2 border-bottom" data-request-id="${r.userId}">
      <a href="profile.html?id=${r.userId}"><img src="${r.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
      <div class="flex-grow-1">
        <a href="profile.html?id=${r.userId}" class="text-body text-decoration-none fw-bold">${escapeHtml(r.fullName)}</a>
        <div class="text-muted small">${timeAgo(r.requestedAt)}</div>
      </div>
      <div class="flex-shrink-0 d-flex gap-1">
        <button class="btn btn-sm btn-success" onclick="approveJoinRequest('${r.userId}')" title="Onayla"><i class="bi bi-check-lg"></i></button>
        <button class="btn btn-sm btn-outline-danger" onclick="rejectJoinRequest('${r.userId}')" title="Reddet"><i class="bi bi-x-lg"></i></button>
      </div>
    </div>`;
}

async function loadJoinRequests() {
  const section = document.getElementById("groupJoinRequestsSection");
  const list = document.getElementById("groupJoinRequestsList");
  try {
    const requests = await apiFetch(`/api/groups/${groupId}/join-requests`);
    section.classList.toggle("d-none", requests.length === 0);
    list.innerHTML = requests.map(joinRequestRowHtml).join("");
  } catch (err) {
    section.classList.add("d-none");
  }
}

// ---- Admin-only: search any real user and invite them (not limited to friends) ----

// The people already in the group get a badge instead of the "Davet Et" button (see user-search.js).
const searchInviteCandidates = createUserSearch({
  resultsId: "groupInviteResultsList",
  actionFor: u => (currentGroup.members || []).some(m => m.userId === u.id)
    ? `<span class="badge text-bg-light border text-muted fw-normal">Zaten üye</span>`
    : `<button class="btn btn-sm btn-outline-primary" onclick="inviteUser('${u.id}', this)">Davet Et</button>`
});

async function inviteUser(userId, btn) {
  btn.disabled = true;
  try {
    await apiFetch(`/api/groups/${groupId}/invites/${userId}`, { method: "POST" });
    toast("Davet gönderildi.");
    // Not reloading the whole group here: an invite doesn't change membership (except the rare case
    // where the user already had a pending join request, which just gets folded in silently).
    btn.outerHTML = `<span class="badge text-bg-light border text-muted fw-normal">Davet gönderildi</span>`;
  } catch (err) {
    btn.disabled = false;
    toast(err.message || "Davet gönderilemedi.");
  }
}

async function approveJoinRequest(userId) {
  try {
    await apiFetch(`/api/groups/${groupId}/join-requests/${userId}/approve`, { method: "POST" });
    toast("İstek onaylandı.");
    await loadGroup();
  } catch (err) {
    toast(err.message || "Onaylanamadı.");
  }
}

async function rejectJoinRequest(userId) {
  try {
    await apiFetch(`/api/groups/${groupId}/join-requests/${userId}/reject`, { method: "POST" });
    toast("İstek reddedildi.");
    document.querySelector(`[data-request-id="${userId}"]`)?.remove();
  } catch (err) {
    toast(err.message || "Reddedilemedi.");
  }
}

// ---- Members list (modal): roles, kicking, and ownership transfer ----

function roleBadge(role) {
  if (role === "Admin") return `<span class="badge text-bg-primary">Yönetici</span>`;
  if (role === "Moderator") return `<span class="badge text-bg-info text-dark">Moderatör</span>`;
  return `<span class="badge text-bg-light border text-muted fw-normal">Üye</span>`;
}

function memberRowHtml(m, group) {
  const isSelf = m.userId === session.userId;
  const canKick = !isSelf && m.role !== "Admin"
    && (group.isCurrentUserAdmin || (group.isCurrentUserModerator && m.role === "Member"));
  const canChangeRole = !isSelf && group.isCurrentUserAdmin && !m.isOwner;
  const canTransfer = !isSelf && group.isCurrentUserOwner;

  let actions = "";
  if (canChangeRole) {
    const options = ["Admin", "Moderator", "Member"]
      .filter(r => r !== m.role)
      .map(r => `<li><button class="dropdown-item" onclick="setMemberRole('${m.userId}','${r}')">${r === "Admin" ? "Yönetici Yap" : r === "Moderator" ? "Moderatör Yap" : "Üye Yap"}</button></li>`)
      .join("");
    actions += `
      <div class="dropdown d-inline-block">
        <button class="btn btn-sm btn-light border" data-bs-toggle="dropdown" title="Rolü Değiştir"><i class="bi bi-gear"></i></button>
        <ul class="dropdown-menu dropdown-menu-end">${options}</ul>
      </div>`;
  }
  if (canTransfer) {
    actions += `<button class="btn btn-sm btn-outline-warning ms-1" onclick="transferOwnershipPage('${m.userId}')" title="Sahipliği Devret"><i class="bi bi-award"></i></button>`;
  }
  if (canKick) {
    actions += `<button class="btn btn-sm btn-outline-danger ms-1" onclick="kickMember('${m.userId}')" title="Gruptan Çıkar"><i class="bi bi-person-x"></i></button>`;
  }

  return `
    <div class="d-flex align-items-center gap-2 py-2 border-bottom">
      <a href="profile.html?id=${m.userId}"><img src="${m.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
      <div class="flex-grow-1">
        <a href="profile.html?id=${m.userId}" class="text-body text-decoration-none fw-bold">${escapeHtml(m.fullName)}</a>
        ${m.isOwner ? ' <i class="bi bi-award-fill text-warning" title="Grup Sahibi"></i>' : ""}
        <div>${roleBadge(m.role)}</div>
      </div>
      <div class="flex-shrink-0">${actions}</div>
    </div>`;
}

function renderMembers(group) {
  const list = document.getElementById("groupMembersList");
  list.innerHTML = (group.members && group.members.length)
    ? group.members.map(m => memberRowHtml(m, group)).join("")
    : `<div class="text-muted small text-center py-3">Üye bulunamadı.</div>`;
}

async function kickMember(userId) {
  if (!confirm("Bu kişiyi gruptan çıkarmak istediğine emin misin?")) return;
  try {
    await apiFetch(`/api/groups/${groupId}/members/${userId}`, { method: "DELETE" });
    toast("Üye gruptan çıkarıldı.");
    await loadGroup();
  } catch (err) {
    toast(err.message || "Üye çıkarılamadı.");
  }
}

async function setMemberRole(userId, role) {
  try {
    const group = await apiFetch(`/api/groups/${groupId}/members/${userId}/role`, { method: "PUT", body: { role } });
    renderGroup(group);
    toast("Rol güncellendi.");
  } catch (err) {
    toast(err.message || "Rol güncellenemedi.");
  }
}

async function transferOwnershipPage(userId) {
  if (!confirm("Grup sahipliğini bu kişiye devretmek istediğine emin misin? Bu işlem geri alınamaz.")) return;
  try {
    const group = await apiFetch(`/api/groups/${groupId}/transfer-ownership/${userId}`, { method: "POST" });
    renderGroup(group);
    toast("Grup sahipliği devredildi.");
  } catch (err) {
    toast(err.message || "Devredilemedi.");
  }
}

async function loadGroup() {
  try {
    const group = await apiFetch(`/api/groups/${groupId}`);
    renderGroup(group);
    document.getElementById("groupLoading").classList.add("d-none");
    document.getElementById("groupContent").classList.remove("d-none");
    await loadGroupPosts();
  } catch (err) {
    document.getElementById("groupLoading").innerHTML = `<div class="alert alert-danger">${escapeHtml(err.message || "Grup bulunamadı.")}</div>`;
  }
}

async function joinGroupPage() {
  try {
    const group = await apiFetch(`/api/groups/${groupId}/join`, { method: "POST" });
    renderGroup(group);
    await loadGroupPosts();
    toast("Gruba katıldın.");
  } catch (err) {
    toast(err.message || "Katılamadın.");
  }
}

async function leaveGroupPage() {
  try {
    await apiFetch(`/api/groups/${groupId}/leave`, { method: "POST" });
    toast("Gruptan ayrıldın.");
    await loadGroup();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

async function deleteGroupPage() {
  try {
    await apiFetch(`/api/groups/${groupId}`, { method: "DELETE" });
    toast("Grup silindi.");
    window.location.href = "groups.html";
  } catch (err) {
    toast(err.message || "Grup silinemedi.");
  }
}

// ---- Posts (the card, comments and like / edit / delete actions are in post-card.js, the button is pager.js) ----

const groupWallPager = createPager({
  list: "groupPostsList",
  url: page => `/api/posts/by-group/${groupId}?page=${page}&pageSize=20`,
  render: post => postCardHtml(post),
  shown: post => !!livePostCard(post.id),
  emptyHtml: `<div class="text-center text-muted small py-4">Bu grupta henüz gönderi yok.</div>`
});

function loadGroupPosts() {
  return groupWallPager.reload();
}

async function publishGroupPost(btn) {
  const textarea = document.getElementById("composerText");
  const fileInput = document.getElementById("composerFile");
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
    if (file) formData.append("media", file);
    formData.append("groupId", groupId);

    await apiFetchForm("/api/posts", { method: "POST", body: formData });

    textarea.value = "";
    clearComposerImage();
    toast("Gönderi paylaşıldı!");
    await loadGroupPosts();
  } catch (err) {
    toast(err.message || "Gönderi paylaşılamadı.");
  } finally {
    btn.disabled = false;
  }
}

// Live like/comment updates on already-rendered post cards are handled by js/live-posts.js.

loadGroup();
