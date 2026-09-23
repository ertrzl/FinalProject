// groups.html — real groups wired to the backend.

requireAuth();

function groupCardHtml(group, discoverMode) {
  const privacyLabel = group.privacy === "Private" ? "Gizli" : "Genel";
  const actionBtn = discoverMode
    ? `<button class="btn btn-outline-primary w-100 rounded-pill" onclick="joinGroup('${group.id}', this)">Katıl</button>`
    : `<button class="btn btn-primary w-100 rounded-pill" onclick="leaveGroup('${group.id}', this)">Ayrıl</button>`;

  return `
    <div class="col-md-4" data-group-id="${group.id}">
      <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100">
        <img src="${group.coverImageUrl || "https://picsum.photos/seed/" + group.id + "/400/160"}" class="w-100" style="height:120px;object-fit:cover;" alt="">
        <div class="p-3">
          <div class="fw-bold"><a href="groups.html#" class="text-dark text-decoration-none">${escapeHtml(group.name)}</a></div>
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
    await apiFetch(`/api/groups/${groupId}/join`, { method: "POST" });
    toast("Gruba katıldın.");
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
