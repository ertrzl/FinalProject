// groups.html — real groups wired to the backend.

requireAuth();

// The groups and invitations as last drawn, so "Katıl" / "Kabul Et" know the name and whether there are rules to accept.
const shownGroups = new Map();
const shownInvites = new Map();

function discoverActionBtn(group) {
  if (group.privacy === "Private") {
    return group.hasPendingJoinRequest
      ? `<button class="btn btn-outline-secondary w-100 rounded-pill" disabled>İstek Gönderildi</button>`
      : `<button class="btn btn-outline-primary w-100 rounded-pill" onclick="joinGroup('${group.id}', this)">İstek Gönder</button>`;
  }
  return `<button class="btn btn-outline-primary w-100 rounded-pill" onclick="joinGroup('${group.id}', this)">Katıl</button>`;
}

function groupCardHtml(group, discoverMode) {
  shownGroups.set(group.id, group);
  const privacyLabel = group.privacy === "Private" ? "Gizli" : "Genel";
  const actionBtn = discoverMode
    ? discoverActionBtn(group)
    : `<button class="btn btn-primary w-100 rounded-pill" onclick="leaveGroup('${group.id}', this)">Ayrıl</button>`;

  return `
    <div class="col-md-4" data-group-id="${group.id}">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100">
        <a href="group.html?id=${group.id}"><img src="${group.coverImageUrl || NO_PHOTO}" class="w-100" style="height:120px;object-fit:cover;" alt=""></a>
        <div class="p-3 group-card-body">
          <a href="group.html?id=${group.id}" class="group-card-avatar">${groupAvatarHtml(group, "avatar-md")}</a>
          <div class="fw-bold"><a href="group.html?id=${group.id}" class="text-body text-decoration-none" data-no-i18n>${escapeHtml(group.name)}</a></div>
          <div class="text-muted small mb-3"><i class="bi bi-people-fill me-1"></i>${group.memberCount} üye · ${privacyLabel}${group.ruleCount > 0 ? ` · <i class="bi bi-list-check"></i> ${group.ruleCount} kural` : ""}</div>
          ${actionBtn}
        </div>
      </div>
    </div>`;
}

async function loadMyGroups() {
  const grid = document.getElementById("myGroupsGrid");
  try {
    const groups = await apiFetch("/api/groups/mine");
    document.getElementById("myGroupsCount").textContent = groups.length;
    grid.innerHTML = groups.length
      ? groups.map(g => groupCardHtml(g, false)).join("")
      : `<div class="col-12 text-muted small py-4 text-center">Henüz bir gruba katılmadın.</div>`;
  } catch (err) {
    grid.innerHTML = `<div class="col-12"><div class="alert alert-danger small">${escapeHtml(err.message)}</div></div>`;
  }
}

async function loadDiscoverGroups() {
  const grid = document.getElementById("discoverGrid");
  const search = document.getElementById("discoverSearch").value.trim();
  try {
    const groups = await apiFetch(`/api/groups/discover${search ? "?search=" + encodeURIComponent(search) : ""}`);
    grid.innerHTML = groups.length
      ? groups.map(g => groupCardHtml(g, true)).join("")
      : `<div class="col-12 text-muted small py-4 text-center">Keşfedilecek grup bulunamadı.</div>`;
  } catch (err) {
    grid.innerHTML = `<div class="col-12"><div class="alert alert-danger small">${escapeHtml(err.message)}</div></div>`;
  }
}

async function joinGroup(groupId, btn) {
  // A group with rules: they are shown first, and joining (or asking to) only goes ahead once they are accepted.
  const group = shownGroups.get(groupId);
  if (group && group.ruleCount > 0) {
    const label = group.privacy === "Private" ? "İstek Gönder" : "Gruba Katıl";
    if (!(await acceptGroupRulesDialog(groupId, group.name, label))) return;
  }

  btn.disabled = true;
  try {
    const result = await apiFetch(`/api/groups/${groupId}/join${group && group.ruleCount > 0 ? "?acceptRules=true" : ""}`, { method: "POST" });
    toast(result.hasPendingJoinRequest ? "Katılma isteği gönderildi." : "Gruba katıldın.");
    await Promise.all([loadMyGroups(), loadDiscoverGroups()]);
  } catch (err) {
    btn.disabled = false;
    toast(err.message || "Katılamadın.");
  }
}

async function leaveGroup(groupId, btn) {
  btn.disabled = true;
  try {
    await apiFetch(`/api/groups/${groupId}/leave`, { method: "POST" });
    toast("Gruptan ayrıldın.");
    await Promise.all([loadMyGroups(), loadDiscoverGroups()]);
  } catch (err) {
    btn.disabled = false;
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

function inviteCardHtml(invite) {
  shownInvites.set(invite.groupId, invite);
  return `
    <div class="col-md-4" data-group-id="${invite.groupId}">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100">
        <img src="${invite.groupCoverImageUrl || NO_PHOTO}" class="w-100" style="height:120px;object-fit:cover;" alt="">
        <div class="p-3 group-card-body">
          <span class="group-card-avatar">${groupAvatarHtml(invite, "avatar-md")}</span>
          <div class="fw-bold" data-no-i18n>${escapeHtml(invite.groupName)}</div>
          <div class="text-muted small mb-3"><a href="profile.html?id=${invite.invitedByUserId}" class="fw-bold text-body text-decoration-none">${escapeHtml(invite.invitedByName)}</a> <span>seni davet etti</span></div>
          <div class="d-flex gap-2">
            <button class="btn btn-primary flex-fill rounded-pill" onclick="respondToInvite('${invite.groupId}', true, this)">Kabul Et</button>
            <button class="btn btn-light border flex-fill rounded-pill" onclick="respondToInvite('${invite.groupId}', false, this)">Reddet</button>
          </div>
        </div>
      </div>
    </div>`;
}

async function loadMyInvites() {
  const grid = document.getElementById("myInvitesGrid");
  try {
    const invites = await apiFetch("/api/groups/invites/mine");
    document.getElementById("myInvitesCount").textContent = invites.length;
    grid.innerHTML = invites.length
      ? invites.map(inviteCardHtml).join("")
      : `<div class="col-12 text-muted small py-4 text-center">Bekleyen davetin yok.</div>`;
  } catch (err) {
    grid.innerHTML = `<div class="col-12"><div class="alert alert-danger small">${escapeHtml(err.message)}</div></div>`;
  }
}

async function respondToInvite(groupId, accept, btn) {
  const invite = shownInvites.get(groupId);
  const needsRules = accept && invite && invite.groupRuleCount > 0;
  if (needsRules && !(await acceptGroupRulesDialog(groupId, invite.groupName, "Daveti Kabul Et"))) return;

  btn.closest(".d-flex").querySelectorAll("button").forEach(b => b.disabled = true);
  try {
    await apiFetch(`/api/groups/${groupId}/invites/${accept ? "accept" : "decline"}${needsRules ? "?acceptRules=true" : ""}`, { method: "POST" });
    toast(accept ? "Davet kabul edildi." : "Davet reddedildi.");
    await Promise.all([loadMyInvites(), loadMyGroups(), loadDiscoverGroups()]);
  } catch (err) {
    btn.closest(".d-flex").querySelectorAll("button").forEach(b => b.disabled = false);
    toast(err.message || "İşlem gerçekleştirilemedi.");
  }
}

async function publishGroup() {
  const name = document.getElementById("groupName").value.trim();
  if (!name) {
    document.getElementById("groupName").classList.add("is-invalid");
    setTimeout(() => document.getElementById("groupName").classList.remove("is-invalid"), 1200);
    return;
  }

  try {
    const formData = new FormData();
    formData.append("name", name);
    formData.append("description", document.getElementById("groupDescription").value.trim());
    formData.append("privacy", document.getElementById("groupPrivacy").value);
    const avatar = document.getElementById("groupAvatarInput").files[0];
    if (avatar) formData.append("avatar", avatar);

    await apiFetchForm("/api/groups", { method: "POST", body: formData });

    bootstrap.Modal.getInstance(document.getElementById("createGroupModal"))?.hide();
    document.getElementById("groupName").value = "";
    document.getElementById("groupDescription").value = "";
    document.getElementById("groupAvatarInput").value = "";
    toast("Grup oluşturuldu!");
    await loadMyGroups();
  } catch (err) {
    toast(err.message || "Grup oluşturulamadı.");
  }
}

// groups.html?tab=invites (e.g. from a notification whose invitation needs the rules accepted) opens that tab.
if (new URLSearchParams(window.location.search).get("tab") === "invites") {
  document.addEventListener("DOMContentLoaded", () =>
    bootstrap.Tab.getOrCreateInstance(document.querySelector('[data-bs-target="#panel-invites"]')).show());
}

loadMyGroups();
loadDiscoverGroups();
loadMyInvites();
