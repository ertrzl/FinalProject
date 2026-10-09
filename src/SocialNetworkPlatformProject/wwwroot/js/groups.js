// groups.html — real groups wired to the backend.

requireAuth();

function discoverActionBtn(group) {
  if (group.privacy === "Private") {
    return group.hasPendingJoinRequest
      ? `<button class="btn btn-outline-secondary w-100 rounded-pill" disabled>İstek Gönderildi</button>`
      : `<button class="btn btn-outline-primary w-100 rounded-pill" onclick="joinGroup('${group.id}', this)">İstek Gönder</button>`;
  }
  return `<button class="btn btn-outline-primary w-100 rounded-pill" onclick="joinGroup('${group.id}', this)">Katıl</button>`;
}

function groupCardHtml(group, discoverMode) {
  const privacyLabel = group.privacy === "Private" ? "Gizli" : "Genel";
  const actionBtn = discoverMode
    ? discoverActionBtn(group)
    : `<button class="btn btn-primary w-100 rounded-pill" onclick="leaveGroup('${group.id}', this)">Ayrıl</button>`;

  return `
    <div class="col-md-4" data-group-id="${group.id}">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100">
        <a href="group.html?id=${group.id}"><img src="${group.coverImageUrl || NO_PHOTO}" class="w-100" style="height:120px;object-fit:cover;" alt=""></a>
        <div class="p-3">
          <div class="fw-bold"><a href="group.html?id=${group.id}" class="text-body text-decoration-none">${escapeHtml(group.name)}</a></div>
          <div class="text-muted small mb-3"><i class="bi bi-people-fill me-1"></i>${group.memberCount} üye · ${privacyLabel}</div>
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
  btn.disabled = true;
  try {
    const result = await apiFetch(`/api/groups/${groupId}/join`, { method: "POST" });
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
  return `
    <div class="col-md-4" data-group-id="${invite.groupId}">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100">
        <img src="${invite.groupCoverImageUrl || NO_PHOTO}" class="w-100" style="height:120px;object-fit:cover;" alt="">
        <div class="p-3">
          <div class="fw-bold">${escapeHtml(invite.groupName)}</div>
          <div class="text-muted small mb-3"><b>${escapeHtml(invite.invitedByName)}</b> seni davet etti</div>
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
  btn.closest(".d-flex").querySelectorAll("button").forEach(b => b.disabled = true);
  try {
    await apiFetch(`/api/groups/${groupId}/invites/${accept ? "accept" : "decline"}`, { method: "POST" });
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

    await apiFetchForm("/api/groups", { method: "POST", body: formData });

    bootstrap.Modal.getInstance(document.getElementById("createGroupModal"))?.hide();
    document.getElementById("groupName").value = "";
    document.getElementById("groupDescription").value = "";
    toast("Grup oluşturuldu!");
    await loadMyGroups();
  } catch (err) {
    toast(err.message || "Grup oluşturulamadı.");
  }
}

loadMyGroups();
loadDiscoverGroups();
loadMyInvites();
