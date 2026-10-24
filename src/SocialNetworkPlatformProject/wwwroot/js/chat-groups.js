let chatGroupFriends = null;
const chatGroupPicked = new Set();
let chatGroupInfo = null;
let chatGroupAddOpen = false;
const chatGroupToAdd = new Set();

async function loadChatGroupFriends() {
  if (chatGroupFriends) return chatGroupFriends;
  try {
    chatGroupFriends = await apiFetch("/api/friends");
  } catch (err) {
    chatGroupFriends = [];
    toast(err.message || i18n("chatGroup.friendsLoadFailed"));
  }
  return chatGroupFriends;
}

function chatGroupAvatarHtml(url, name, sizeClass) {
  if (url) return `<img src="${escapeHtml(url)}" class="${sizeClass} group-avatar" alt="">`;
  const initial = (name || "?").trim().charAt(0).toLocaleUpperCase(localeTag()) || "?";
  return `<span class="${sizeClass} group-avatar group-avatar-empty" aria-hidden="true" data-no-i18n>${escapeHtml(initial)}</span>`;
}

function chatGroupFriendRowsHtml(friends, picked, taken, search) {
  const term = (search || "").trim().toLocaleLowerCase(localeTag());
  const rows = friends
    .filter(f => !taken.has(f.userId) && (!term || f.name.toLocaleLowerCase(localeTag()).includes(term)))
    .map(f => `
      <label class="chat-group-friend">
        <input type="checkbox" class="form-check-input flex-shrink-0 m-0" value="${f.userId}" ${picked.has(f.userId) ? "checked" : ""}>
        <img src="${f.avatarUrl || DEFAULT_AVATAR}" class="avatar-xs" alt="">
        <span class="flex-grow-1 text-truncate" data-no-i18n>${escapeHtml(f.name)}</span>
      </label>`);
  return rows.length ? rows.join("") : `<div class="text-muted small p-2">${i18n("chatGroup.noFriends")}</div>`;
}


async function openNewChatGroup() {
  chatGroupPicked.clear();
  document.getElementById("newChatGroupTitle").value = "";
  document.getElementById("newChatGroupSearch").value = "";
  document.getElementById("newChatGroupAvatar").value = "";
  document.getElementById("newChatGroupPreview").classList.add("d-none");
  document.getElementById("newChatGroupCamera").classList.remove("d-none");

  bootstrap.Modal.getOrCreateInstance(document.getElementById("newChatGroupModal")).show();
  await loadChatGroupFriends();
  renderNewChatGroupFriends();
}

function renderNewChatGroupFriends() {
  const box = document.getElementById("newChatGroupFriends");
  box.innerHTML = chatGroupFriendRowsHtml(chatGroupFriends || [], chatGroupPicked, new Set(), document.getElementById("newChatGroupSearch").value);
  updateNewChatGroupState();
}

function updateNewChatGroupState() {
  const count = chatGroupPicked.size;
  document.getElementById("newChatGroupCount").textContent = count ? `(${i18n("chatGroup.picked", count)})` : "";
  const hasTitle = document.getElementById("newChatGroupTitle").value.trim().length > 0;
  document.getElementById("newChatGroupCreate").disabled = !(hasTitle && count >= 2);
}

document.getElementById("newChatGroupFriends")?.addEventListener("change", event => {
  const box = event.target.closest("input[type=checkbox]");
  if (!box) return;
  if (box.checked) chatGroupPicked.add(box.value); else chatGroupPicked.delete(box.value);
  updateNewChatGroupState();
});
document.getElementById("newChatGroupSearch")?.addEventListener("input", renderNewChatGroupFriends);
document.getElementById("newChatGroupTitle")?.addEventListener("input", updateNewChatGroupState);
document.getElementById("newChatGroupAvatar")?.addEventListener("change", event => {
  const file = event.target.files && event.target.files[0];
  const preview = document.getElementById("newChatGroupPreview");
  if (!file) {
    preview.classList.add("d-none");
    document.getElementById("newChatGroupCamera").classList.remove("d-none");
    return;
  }
  preview.src = URL.createObjectURL(file);
  preview.classList.remove("d-none");
  document.getElementById("newChatGroupCamera").classList.add("d-none");
});

async function createChatGroup() {
  const button = document.getElementById("newChatGroupCreate");
  button.disabled = true;

  const form = new FormData();
  form.append("title", document.getElementById("newChatGroupTitle").value.trim());
  chatGroupPicked.forEach(id => form.append("memberIds", id));
  const avatar = document.getElementById("newChatGroupAvatar").files[0];
  if (avatar) form.append("avatar", avatar);

  try {
    const chat = await apiFetchForm("/api/messages/group-chats", { method: "POST", body: form });
    bootstrap.Modal.getInstance(document.getElementById("newChatGroupModal"))?.hide();
    toast(i18n("chatGroup.created"));
    await selectConversation(chat.id);
  } catch (err) {
    toast(err.message || i18n("chatGroup.createFailed"));
    updateNewChatGroupState();
  }
}


async function openChatGroupInfo(conversationId) {
  try {
    chatGroupInfo = await apiFetch(`/api/messages/group-chats/${conversationId}`);
  } catch (err) {
    toast(err.message || i18n("chatGroup.loadFailed"));
    return;
  }

  chatGroupAddOpen = false;
  chatGroupToAdd.clear();
  renderChatGroupInfo();
  bootstrap.Modal.getOrCreateInstance(document.getElementById("chatGroupInfoModal")).show();
  loadChatGroupFriends();
}

function renderChatGroupInfo() {
  const g = chatGroupInfo;
  const body = document.getElementById("chatGroupInfoBody");

  const memberRow = m => {
    const actions = g.canManage && !m.isMe ? `
      <div class="d-flex flex-wrap gap-1 justify-content-end flex-shrink-0">
        <button type="button" class="btn btn-light btn-sm rounded-pill" data-cg-admin="${m.userId}" data-make="${!m.isAdmin}">${i18n(m.isAdmin ? "chatGroup.removeAdmin" : "chatGroup.makeAdmin")}</button>
        ${m.isAdmin ? "" : `<button type="button" class="btn btn-outline-danger btn-sm rounded-pill" data-cg-remove="${m.userId}" data-name="${escapeHtml(m.fullName)}">${i18n("chatGroup.remove")}</button>`}
      </div>` : "";
    return `
      <div class="chat-group-member">
        <a href="profile.html?id=${m.userId}"><img src="${m.avatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt=""></a>
        <div class="flex-grow-1 text-truncate">
          <a href="profile.html?id=${m.userId}" class="fw-semibold text-body text-decoration-none" data-no-i18n>${escapeHtml(m.fullName)}</a>
          ${m.isMe ? `<span class="small text-muted">${i18n("chatGroup.you")}</span>` : ""}
          ${m.isAdmin ? `<span class="badge text-bg-primary ms-1">${i18n("chatGroup.admin")}</span>` : ""}
        </div>
        ${actions}
      </div>`;
  };

  const memberIds = new Set(g.members.map(m => m.userId));
  const canAddMore = g.canManage && g.memberCount < g.maxMembers;

  body.innerHTML = `
    <div class="d-flex align-items-center gap-3 mb-3">
      ${chatGroupAvatarHtml(g.avatarUrl, g.title, "avatar-md")}
      <div class="flex-grow-1" style="min-width:0;">
        ${g.canManage ? `
          <label class="form-label small fw-semibold mb-1" for="chatGroupInfoTitle">${i18n("chatGroup.name")}</label>
          <div class="input-group input-group-sm mb-2">
            <input type="text" id="chatGroupInfoTitle" class="form-control" maxlength="60" value="${escapeHtml(g.title)}" autocomplete="off" data-no-i18n>
            <button type="button" class="btn btn-primary" data-cg="rename">${i18n("chatGroup.save")}</button>
          </div>
          <div class="d-flex flex-wrap gap-2">
            <label class="btn btn-light btn-sm rounded-pill m-0">${i18n("chatGroup.changePicture")}<input type="file" id="chatGroupInfoAvatar" class="d-none" accept="image/*"></label>
            ${g.avatarUrl ? `<button type="button" class="btn btn-light btn-sm rounded-pill" data-cg="removeAvatar">${i18n("chatGroup.removePicture")}</button>` : ""}
          </div>` : `<div class="fw-bold fs-5 text-truncate" data-no-i18n>${escapeHtml(g.title)}</div>`}
      </div>
    </div>

    <div class="form-check form-switch mb-3">
      <input class="form-check-input" type="checkbox" role="switch" id="chatGroupMute" ${g.isMuted ? "checked" : ""}>
      <label class="form-check-label small" for="chatGroupMute">${i18n("chatGroup.mute")}</label>
    </div>

    <div class="d-flex align-items-center justify-content-between mb-2">
      <div class="fw-bold">${i18n("chatGroup.members", g.memberCount)}</div>
      ${canAddMore ? `<button type="button" class="btn btn-primary btn-sm rounded-pill" data-cg="toggleAdd"><i class="bi bi-person-plus-fill me-1"></i>${i18n("chatGroup.add")}</button>` : ""}
    </div>

    ${chatGroupAddOpen ? `
      <div class="border rounded-3 p-2 mb-3">
        <input type="search" id="chatGroupAddSearch" class="form-control form-control-sm mb-2" autocomplete="off" placeholder="${i18n("chatGroup.searchFriends")}">
        <div id="chatGroupAddList" class="chat-group-friends">${chatGroupFriendRowsHtml(chatGroupFriends || [], chatGroupToAdd, memberIds, "")}</div>
        <div class="d-flex justify-content-end mt-2">
          <button type="button" class="btn btn-primary btn-sm rounded-pill" data-cg="addSelected" ${chatGroupToAdd.size ? "" : "disabled"}>${i18n("chatGroup.addSelected")}</button>
        </div>
      </div>` : ""}

    <div class="chat-group-members">${g.members.map(memberRow).join("")}</div>

    <div class="d-flex justify-content-end mt-4">
      <button type="button" class="btn btn-outline-danger rounded-pill" data-cg="leave"><i class="bi bi-box-arrow-right me-1"></i>${i18n("chatGroup.leave")}</button>
    </div>`;
}

let chatGroupBusy = false;
async function chatGroupRequest(request, doneText) {
  if (chatGroupBusy) return;
  chatGroupBusy = true;
  try {
    chatGroupInfo = await request();
    if (doneText) toast(doneText);
    renderChatGroupInfo();
  } catch (err) {
    toast(err.message || i18n("chatGroup.failed"));
  } finally {
    chatGroupBusy = false;
  }
}

function chatGroupJson(path, method, body) {
  return () => apiFetch(`/api/messages/group-chats/${chatGroupInfo.id}${path}`, body === undefined ? { method } : { method, body });
}

function chatGroupForm(fields) {
  const form = new FormData();
  Object.entries(fields).forEach(([key, value]) => form.append(key, value));
  return () => apiFetchForm(`/api/messages/group-chats/${chatGroupInfo.id}`, { method: "PUT", body: form });
}

document.getElementById("chatGroupInfoBody")?.addEventListener("click", async event => {
  if (!chatGroupInfo) return;

  const admin = event.target.closest("[data-cg-admin]");
  if (admin) {
    const make = admin.dataset.make === "true";
    await chatGroupRequest(chatGroupJson(`/members/${admin.dataset.cgAdmin}/admin`, "PUT", { isAdmin: make }), i18n(make ? "chatGroup.madeAdmin" : "chatGroup.removedAdmin"));
    return;
  }

  const remove = event.target.closest("[data-cg-remove]");
  if (remove) {
    if (!(await confirmDialog(i18n("chatGroup.removeConfirm", remove.dataset.name)))) return;
    await chatGroupRequest(chatGroupJson(`/members/${remove.dataset.cgRemove}`, "DELETE"), i18n("chatGroup.removed"));
    return;
  }

  const action = event.target.closest("[data-cg]")?.dataset.cg;
  if (action === "rename") {
    const title = document.getElementById("chatGroupInfoTitle").value.trim();
    if (title) await chatGroupRequest(chatGroupForm({ title }), i18n("chatGroup.renamed"));
  } else if (action === "removeAvatar") {
    await chatGroupRequest(chatGroupForm({ removeAvatar: "true" }), i18n("chatGroup.pictureRemoved"));
  } else if (action === "toggleAdd") {
    chatGroupAddOpen = !chatGroupAddOpen;
    chatGroupToAdd.clear();
    renderChatGroupInfo();
  } else if (action === "addSelected") {
    const ids = [...chatGroupToAdd];
    chatGroupAddOpen = false;
    chatGroupToAdd.clear();
    await chatGroupRequest(chatGroupJson("/members", "POST", { userIds: ids }), i18n("chatGroup.added"));
  } else if (action === "leave") {
    if (!(await confirmDialog(i18n("chatGroup.leaveConfirm")))) return;
    try {
      await apiFetch(`/api/messages/group-chats/${chatGroupInfo.id}/leave`, { method: "POST" });
      bootstrap.Modal.getInstance(document.getElementById("chatGroupInfoModal"))?.hide();
      toast(i18n("chatGroup.left"));
    } catch (err) {
      toast(err.message || i18n("chatGroup.failed"));
    }
  }
});

document.getElementById("chatGroupInfoBody")?.addEventListener("change", async event => {
  if (!chatGroupInfo) return;

  if (event.target.id === "chatGroupInfoAvatar" && event.target.files[0]) {
    await chatGroupRequest(chatGroupForm({ avatar: event.target.files[0] }), i18n("chatGroup.pictureChanged"));
  } else if (event.target.id === "chatGroupMute") {
    await chatGroupRequest(chatGroupJson(event.target.checked ? "/mute" : "/unmute", "POST"), null);
  } else if (event.target.closest("#chatGroupAddList")) {
    const box = event.target.closest("input[type=checkbox]");
    if (!box) return;
    if (box.checked) chatGroupToAdd.add(box.value); else chatGroupToAdd.delete(box.value);
    const button = document.querySelector('#chatGroupInfoBody [data-cg="addSelected"]');
    if (button) button.disabled = chatGroupToAdd.size === 0;
  }
});

document.getElementById("chatGroupInfoBody")?.addEventListener("input", event => {
  if (event.target.id !== "chatGroupAddSearch") return;
  const memberIds = new Set(chatGroupInfo.members.map(m => m.userId));
  document.getElementById("chatGroupAddList").innerHTML = chatGroupFriendRowsHtml(chatGroupFriends || [], chatGroupToAdd, memberIds, event.target.value);
});
