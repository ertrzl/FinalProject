// group.html — a group's own wall: header, join/leave/delete, chat, mute, a members-only composer, pinned announcements,
// search, its posts, events and rules. What anyone may do is decided by the server (the flags on the group); this file
// shows the matching buttons.

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
  return `<button class="btn btn-primary rounded-pill" onclick="joinGroupPage(this)">Gruba Katıl</button>`;
}

function muteButtonHtml(group) {
  return group.isMutedByCurrentUser
    ? `<i class="bi bi-bell-slash-fill me-1"></i>Sesi Aç`
    : `<i class="bi bi-bell me-1"></i>Sessize Al`;
}

// Whoever may see the wall may search it: the members, and anyone for a public group.
function canSeeWall(group) {
  return group.isCurrentUserMember || group.privacy === "Public";
}

function renderGroup(group) {
  currentGroup = group;
  document.title = group.name + " | Stoop";

  const cover = document.getElementById("groupCover");
  cover.style.backgroundImage = group.coverImageUrl ? `url(${group.coverImageUrl})` : "";
  cover.style.backgroundSize = "cover";
  cover.style.backgroundPosition = "center";

  document.getElementById("groupHeaderAvatar").innerHTML = groupAvatarHtml(group, "avatar-lg");
  document.getElementById("groupName").textContent = group.name;
  document.getElementById("groupDescription").textContent = group.description || "";
  document.getElementById("groupMemberCount").textContent = group.memberCount;
  document.getElementById("groupPrivacyIcon").className = "bi " + (group.privacy === "Private" ? "bi-lock-fill" : "bi-globe-americas");
  document.getElementById("groupPrivacyLabel").textContent = group.privacy === "Private" ? "Gizli Grup" : "Genel Grup";
  document.getElementById("groupActionArea").innerHTML = actionAreaHtml(group);

  // The chat and muting are a member's own things.
  document.getElementById("groupChatBtn").classList.toggle("d-none", !group.isCurrentUserMember);
  const muteBtn = document.getElementById("groupMuteBtn");
  muteBtn.classList.toggle("d-none", !group.isCurrentUserMember);
  muteBtn.innerHTML = muteButtonHtml(group);
  muteBtn.title = group.isMutedByCurrentUser ? "Bu grubun bildirimleri kapalı" : "Bu grubun bildirimlerini kapat";

  document.getElementById("composerAvatar").src = session.avatarUrl || DEFAULT_AVATAR;
  document.getElementById("groupComposerWrap").classList.toggle("d-none", !group.isCurrentUserMember);
  document.getElementById("groupNonMemberNotice").classList.toggle("d-none", group.isCurrentUserMember);
  document.getElementById("groupSearchForm").classList.toggle("d-none", !canSeeWall(group));

  // Events are for members only: non-members never see the tab. Admins and moderators may create group events.
  document.getElementById("groupEventsTabItem").classList.toggle("d-none", !group.isCurrentUserMember);
  document.getElementById("groupEventCreateBtn").classList.toggle("d-none", !(group.isCurrentUserAdmin || group.isCurrentUserModerator));
  groupEventsLoaded = false;

  const ruleBadge = document.getElementById("groupRuleCountBadge");
  ruleBadge.textContent = group.ruleCount;
  ruleBadge.classList.toggle("d-none", !group.ruleCount);
  document.getElementById("groupRulesEditBtn").classList.toggle("d-none", !group.isCurrentUserAdmin);
  groupRulesLoaded = false;

  if (group.isCurrentUserAdmin) {
    document.getElementById("editGroupName").value = group.name;
    document.getElementById("editGroupDescription").value = group.description || "";
    document.getElementById("editGroupPrivacy").value = group.privacy;
    document.getElementById("editGroupAvatarPreview").innerHTML = groupAvatarHtml(group, "avatar-md");
    document.getElementById("editGroupRemoveAvatarWrap").classList.toggle("d-none", !group.avatarUrl);
    document.getElementById("editGroupRemoveCoverWrap").classList.toggle("d-none", !group.coverImageUrl);
    document.getElementById("editGroupRemoveAvatar").checked = false;
    document.getElementById("editGroupRemoveCover").checked = false;
    loadJoinRequests();
    document.getElementById("groupInviteSection").classList.remove("d-none");
    document.getElementById("groupInviteSearchInput").value = "";
    document.getElementById("groupInviteResultsList").innerHTML = "";
  } else {
    document.getElementById("groupJoinRequestsSection").classList.add("d-none");
    document.getElementById("groupInviteSection").classList.add("d-none");
  }

  if (document.getElementById("groupMembersModal").classList.contains("show")) loadMembers();
}

// ---- Group chat: opens in the chat dock where there is one, otherwise on the messages page ----

async function openGroupChat(btn) {
  btn.disabled = true;
  try {
    const conversation = await apiFetch(`/api/messages/groups/${groupId}`, { method: "POST" });
    const dock = document.getElementById("chatDock");
    if (dock && getComputedStyle(dock).display !== "none" && typeof openChatPopup === "function") {
      await window.refreshChatConversations();
      await openChatPopup(conversation.id);
    } else {
      window.location.href = `messages.html?conversation=${conversation.id}`;
    }
  } catch (err) {
    toast(err.message || "Grup sohbeti açılamadı.");
  } finally {
    btn.disabled = false;
  }
}

// ---- Mute: the member's own choice, kept by the server ----

async function toggleGroupMute(btn) {
  btn.disabled = true;
  const mute = !currentGroup.isMutedByCurrentUser;
  try {
    const group = await apiFetch(`/api/groups/${groupId}/${mute ? "mute" : "unmute"}`, { method: "POST" });
    renderGroup(group);
    toast(mute ? "Grup sessize alındı. Bu gruptan bildirim almayacaksın." : "Grubun sesi açıldı.");
    window.refreshMessageBadge?.();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  } finally {
    btn.disabled = false;
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

// ---- Rules tab, and the admins' editor ----

let groupRulesLoaded = false;
let groupRules = [];

const MAX_RULES = 10; // the server's limit (GroupLimits.MaxRules); here only to hide "Kural Ekle" at the limit

function ruleItemHtml(rule, index) {
  return `
    <div class="group-rule-card border rounded-3 p-3 mb-2">
      <div class="fw-semibold" data-no-i18n>${index + 1}. ${escapeHtml(rule.title)}</div>
      ${rule.text ? `<div class="small text-muted mt-1 group-rule-text" data-no-i18n>${escapeHtml(rule.text)}</div>` : ""}
    </div>`;
}

async function loadGroupRules(force) {
  if (groupRulesLoaded && !force) return;
  const box = document.getElementById("groupRulesList");
  try {
    groupRules = await apiFetch(`/api/groups/${groupId}/rules`);
    box.innerHTML = groupRules.length
      ? groupRules.map(ruleItemHtml).join("")
      : `<div class="text-muted small text-center py-4">Bu grubun henüz kuralı yok.</div>`;
    groupRulesLoaded = true;
  } catch (err) {
    box.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
}

function ruleRowHtml(rule = { title: "", text: "" }) {
  return `
    <div class="rule-editor-row border rounded-3 p-2 mb-2">
      <div class="d-flex gap-2 align-items-start">
        <div class="flex-grow-1">
          <input type="text" class="form-control form-control-sm mb-2 rule-title" maxlength="100" placeholder="Kural başlığı" aria-label="Kural başlığı" value="${escapeHtml(rule.title)}">
          <textarea class="form-control form-control-sm rule-text" rows="2" maxlength="500" placeholder="Açıklama (isteğe bağlı)" aria-label="Kural açıklaması">${escapeHtml(rule.text || "")}</textarea>
        </div>
        <button type="button" class="btn btn-light border rule-remove" title="Kuralı kaldır" aria-label="Kuralı kaldır" onclick="removeRuleRow(this)"><i class="bi bi-trash"></i></button>
      </div>
    </div>`;
}

function refreshRuleAddButton() {
  const count = document.querySelectorAll("#rulesEditorList .rule-editor-row").length;
  document.getElementById("rulesAddBtn").classList.toggle("d-none", count >= MAX_RULES);
}

async function openRulesEditor() {
  await loadGroupRules(true);
  const list = document.getElementById("rulesEditorList");
  list.innerHTML = groupRules.map(ruleRowHtml).join("");
  if (!groupRules.length) list.innerHTML = ruleRowHtml();
  refreshRuleAddButton();
  bootstrap.Modal.getOrCreateInstance(document.getElementById("editRulesModal")).show();
}

function addRuleRow() {
  document.getElementById("rulesEditorList").insertAdjacentHTML("beforeend", ruleRowHtml());
  refreshRuleAddButton();
  document.querySelector("#rulesEditorList .rule-editor-row:last-child .rule-title")?.focus();
}

function removeRuleRow(btn) {
  btn.closest(".rule-editor-row").remove();
  refreshRuleAddButton();
}

async function saveRules(btn) {
  // Rows left completely empty are simply not rules; the server checks the rest.
  const rules = [...document.querySelectorAll("#rulesEditorList .rule-editor-row")]
    .map(row => ({ title: row.querySelector(".rule-title").value.trim(), text: row.querySelector(".rule-text").value.trim() }))
    .filter(r => r.title || r.text);

  btn.disabled = true;
  try {
    groupRules = await apiFetch(`/api/groups/${groupId}/rules`, { method: "PUT", body: { rules } });
    bootstrap.Modal.getInstance(document.getElementById("editRulesModal"))?.hide();
    currentGroup.ruleCount = groupRules.length;
    renderGroup(currentGroup);
    await loadGroupRules(true);
    toast("Kurallar kaydedildi.");
  } catch (err) {
    toast(err.message || "Kurallar kaydedilemedi.");
  } finally {
    btn.disabled = false;
  }
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
    else if (document.getElementById("editGroupRemoveCover").checked) formData.append("removeCoverImage", "true");
    const avatarFile = document.getElementById("editGroupAvatarInput").files[0];
    if (avatarFile) formData.append("avatar", avatarFile);
    else if (document.getElementById("editGroupRemoveAvatar").checked) formData.append("removeAvatar", "true");

    const group = await apiFetchForm(`/api/groups/${groupId}`, { method: "PUT", body: formData });
    bootstrap.Modal.getInstance(document.getElementById("editGroupModal"))?.hide();
    document.getElementById("editGroupCoverInput").value = "";
    document.getElementById("editGroupAvatarInput").value = "";
    // The update answers without the member list; the page keeps the one it has.
    group.members = currentGroup.members;
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
      <div class="flex-grow-1" style="min-width:0;">
        <a href="profile.html?id=${r.userId}" class="text-body text-decoration-none fw-bold">${escapeHtml(r.fullName)}</a>
        <div class="text-muted small">${timeAgo(r.requestedAt)}</div>
      </div>
      <div class="flex-shrink-0 d-flex gap-1">
        <button class="btn btn-sm btn-success member-action" onclick="approveJoinRequest('${r.userId}')" title="Onayla"><i class="bi bi-check-lg"></i></button>
        <button class="btn btn-sm btn-outline-danger member-action" onclick="rejectJoinRequest('${r.userId}')" title="Reddet"><i class="bi bi-x-lg"></i></button>
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

// ---- Members list (modal): searched, filtered and paged by the server; roles, kicking, and ownership transfer ----

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
        <button class="btn btn-sm btn-light border member-action" data-bs-toggle="dropdown" title="Rolü Değiştir"><i class="bi bi-gear"></i></button>
        <ul class="dropdown-menu dropdown-menu-end">${options}</ul>
      </div>`;
  }
  if (canTransfer) {
    actions += `<button class="btn btn-sm btn-outline-warning ms-1 member-action" onclick="transferOwnershipPage('${m.userId}')" title="Sahipliği Devret"><i class="bi bi-award"></i></button>`;
  }
  if (canKick) {
    actions += `<button class="btn btn-sm btn-outline-danger ms-1 member-action" onclick="kickMember('${m.userId}')" title="Gruptan Çıkar"><i class="bi bi-person-x"></i></button>`;
  }

  return `
    <div class="d-flex align-items-center gap-2 py-2 border-bottom" data-member-id="${m.userId}">
      <a href="profile.html?id=${m.userId}"><img src="${m.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
      <div class="flex-grow-1" style="min-width:0;">
        <a href="profile.html?id=${m.userId}" class="text-body text-decoration-none fw-bold">${escapeHtml(m.fullName)}</a>
        ${m.isOwner ? ' <i class="bi bi-award-fill text-warning" title="Grup Sahibi"></i>' : ""}
        <div>${roleBadge(m.role)}</div>
      </div>
      <div class="flex-shrink-0">${actions}</div>
    </div>`;
}

let membersPager = null;

function loadMembers() {
  if (!currentGroup) return;
  const search = document.getElementById("groupMemberSearch").value.trim();
  const role = document.getElementById("groupMemberRole").value;
  // A new pager for each search: it starts from the first page again (and replaces the previous "Daha fazla yükle").
  membersPager = createPager({
    list: "groupMembersList",
    url: page => `/api/groups/${groupId}/members?page=${page}&pageSize=20${search ? "&search=" + encodeURIComponent(search) : ""}${role ? "&role=" + role : ""}`,
    render: m => memberRowHtml(m, currentGroup),
    shown: m => !!document.querySelector(`#groupMembersList [data-member-id="${m.userId}"]`),
    emptyHtml: `<div class="text-muted small text-center py-3">Üye bulunamadı.</div>`
  });
  return membersPager.reload();
}

let memberSearchTimer = null;
function scheduleMemberSearch() {
  clearTimeout(memberSearchTimer);
  memberSearchTimer = setTimeout(loadMembers, 300);
}

document.getElementById("groupMembersModal").addEventListener("show.bs.modal", () => loadMembers());

async function kickMember(userId) {
  if (!(await confirmDialog("Bu kişiyi gruptan çıkarmak istediğine emin misin?", { confirmText: "Gruptan Çıkar" }))) return;
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
  if (!(await confirmDialog("Grup sahipliğini bu kişiye devretmek istediğine emin misin? Bu işlem geri alınamaz.", { confirmText: "Devret" }))) return;
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
    await reloadWall();
  } catch (err) {
    document.getElementById("groupLoading").innerHTML = `<div class="alert alert-danger">${escapeHtml(err.message || "Grup bulunamadı.")}</div>`;
  }
}

async function joinGroupPage(btn) {
  // A group with rules lets nobody in who did not accept them: they are shown first.
  const needsRules = currentGroup.ruleCount > 0;
  if (needsRules && !(await acceptGroupRulesDialog(groupId, currentGroup.name, "Gruba Katıl"))) return;

  if (btn) btn.disabled = true;
  try {
    const group = await apiFetch(`/api/groups/${groupId}/join${needsRules ? "?acceptRules=true" : ""}`, { method: "POST" });
    toast(group.hasPendingJoinRequest ? "Katılma isteği gönderildi." : "Gruba katıldın.");
    await loadGroup();
  } catch (err) {
    if (btn) btn.disabled = false;
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
  if (!(await confirmDialog("Bu grup, gönderileri, etkinlikleri ve sohbetiyle birlikte silinsin mi? Bu işlem geri alınamaz.", { confirmText: "Grubu Sil" }))) return;
  try {
    await apiFetch(`/api/groups/${groupId}`, { method: "DELETE" });
    toast("Grup silindi.");
    window.location.href = "groups.html";
  } catch (err) {
    toast(err.message || "Grup silinemedi.");
  }
}

// ---- Posts (the card, comments and like / edit / delete actions are in post-card.js, the button is pager.js) ----

// The ids of the pinned posts: their cards carry "Sabitlendi", and the admins' menu offers to pin or unpin.
let pinnedIds = new Set();

// post-card.js draws the cards; this adds what belongs to a group wall. Done on every card that appears (also one that
// post-card.js draws again after an edit), so it is watched for rather than called by hand.
function decorateGroupCard(card) {
  if (card.dataset.groupDecorated === "1") return;
  card.dataset.groupDecorated = "1";

  const postId = card.dataset.postId;
  const isPinned = pinnedIds.has(postId);

  if (isPinned) {
    card.classList.add("group-pinned-card");
    card.querySelector(".text-muted.small")?.insertAdjacentHTML("beforeend",
      ` · <span class="badge rounded-pill group-pinned-badge"><i class="bi bi-pin-angle-fill me-1"></i>Sabitlendi</span>`);
  }

  if (currentGroup?.isCurrentUserAdmin) {
    card.querySelector(".dropdown-menu")?.insertAdjacentHTML("afterbegin", isPinned
      ? `<li><button class="dropdown-item" onclick="unpinGroupPost('${postId}')"><i class="bi bi-pin-angle me-2"></i>Sabitlemeyi Kaldır</button></li>`
      : `<li><button class="dropdown-item" onclick="pinGroupPost('${postId}')"><i class="bi bi-pin-angle-fill me-2"></i>Duyuru olarak sabitle</button></li>`);
  }
}

new MutationObserver(() => {
  document.querySelectorAll("#groupContent .card[data-post-id]:not([data-group-decorated])").forEach(decorateGroupCard);
}).observe(document.getElementById("groupContent"), { childList: true, subtree: true });

function renderPinned(posts) {
  pinnedIds = new Set(posts.map(p => p.id));
  document.getElementById("groupPinnedList").innerHTML = posts.map(postCardHtml).join("");
  document.getElementById("groupPinnedSection").classList.toggle("d-none", posts.length === 0);
}

async function loadPinned() {
  if (!canSeeWall(currentGroup)) {
    renderPinned([]);
    return;
  }
  try {
    renderPinned(await apiFetch(`/api/groups/${groupId}/pinned-posts`));
  } catch (err) {
    renderPinned([]);
  }
}

// The pinned posts first, then the wall: the wall's pager leaves out what is already on the page (the pinned ones).
async function reloadWall() {
  document.getElementById("groupPostsList").innerHTML = "";
  await loadPinned();
  await loadGroupPosts();
}

async function pinGroupPost(postId) {
  try {
    renderPinned(await apiFetch(`/api/groups/${groupId}/pinned-posts/${postId}`, { method: "POST" }));
    toast("Gönderi sabitlendi. Üyelere bildirildi.");
    await loadGroupPosts();
  } catch (err) {
    toast(err.message || "Gönderi sabitlenemedi.");
  }
}

async function unpinGroupPost(postId) {
  try {
    renderPinned(await apiFetch(`/api/groups/${groupId}/pinned-posts/${postId}`, { method: "DELETE" }));
    toast("Sabitleme kaldırıldı.");
    await loadGroupPosts();
  } catch (err) {
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

const groupWallPager = createPager({
  list: "groupPostsList",
  url: page => `/api/posts/by-group/${groupId}?page=${page}&pageSize=20`,
  render: post => postCardHtml(post),
  shown: post => !!livePostCard(post.id),
  emptyHtml: `<div class="text-center text-muted small py-4">Bu grupta henüz gönderi yok.</div>`
});

function loadGroupPosts() {
  document.getElementById("groupPostsList").innerHTML = "";
  return groupWallPager.reload();
}

async function publishGroupPost(btn) {
  const textarea = document.getElementById("composerText");
  const text = textarea.value.trim();
  const files = getComposerFiles();

  if (!text && files.length === 0) {
    textarea.classList.add("is-invalid");
    setTimeout(() => textarea.classList.remove("is-invalid"), 1200);
    return;
  }

  btn.disabled = true;
  try {
    const formData = new FormData();
    if (text) formData.append("text", text);
    appendComposerAttachments(formData);
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

// ---- Search inside the group: the server searches the wall's text and hashtags; the wall steps aside meanwhile ----

function searchGroupPosts(event) {
  event.preventDefault();
  const term = document.getElementById("groupSearchInput").value.trim();
  if (!term) {
    clearGroupSearch();
    return false;
  }

  // The wall's cards are taken off the page while the results show, so each post is on the page once (live updates
  // and "Düzenle" find the card by its id).
  document.getElementById("groupPinnedList").innerHTML = "";
  document.getElementById("groupPostsList").innerHTML = "";
  document.getElementById("groupPostsList-more")?.classList.add("d-none");
  document.getElementById("groupWall").classList.add("d-none");
  document.getElementById("groupSearchResults").classList.remove("d-none");
  document.getElementById("groupSearchClear").classList.remove("d-none");
  document.getElementById("groupSearchSummary").innerHTML = i18n("group.searchFor", `<b data-no-i18n>${escapeHtml(term)}</b>`);

  createPager({
    list: "groupSearchList",
    url: page => `/api/groups/${groupId}/posts/search?q=${encodeURIComponent(term)}&page=${page}&pageSize=10`,
    render: post => postCardHtml(post),
    shown: post => !!document.querySelector(`#groupSearchList .card[data-post-id="${post.id}"]`),
    emptyHtml: `<div class="text-center text-muted small py-4">Bu aramayla eşleşen gönderi yok.</div>`
  }).reload();
  return false;
}

function clearGroupSearch() {
  document.getElementById("groupSearchInput").value = "";
  document.getElementById("groupSearchClear").classList.add("d-none");
  document.getElementById("groupSearchList").innerHTML = "";
  document.getElementById("groupSearchList-more")?.remove();
  document.getElementById("groupSearchResults").classList.add("d-none");
  document.getElementById("groupWall").classList.remove("d-none");
  reloadWall();
}

// Live like/comment updates on already-rendered post cards are handled by js/live-posts.js. When a post goes (deleted
// here or elsewhere) and it was the last pinned one, the "Sabitlenen" heading goes with it.
window.afterLivePostRemoved = () => {
  if (!document.querySelector("#groupPinnedList .card")) document.getElementById("groupPinnedSection").classList.add("d-none");
};

loadGroup();
