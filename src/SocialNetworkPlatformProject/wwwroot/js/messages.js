requireAuth();

let activeId = null;
let draftUser = null;

function conversationListItemHtml(conv) {
  const activeClass = conv.id === activeId ? "bg-primary-subtle" : "";
  const presenceDot = conv.isGroup ? ""
    : `<span class="position-absolute bottom-0 end-0 bg-success border border-2 border-white rounded-circle ${conv.isOtherUserOnline ? "" : "d-none"}" style="width:11px;height:11px;" data-presence-user="${conv.otherUserId}"></span>`;
  return `
    <button class="btn w-100 text-start rounded-0 px-3 py-3 border-0 border-bottom d-flex align-items-center gap-2 ${activeClass}" onclick="selectConversation('${conv.id}')">
      <div class="position-relative flex-shrink-0">
        ${window.chatConversationAvatarHtml(conv, "avatar-sm")}
        ${presenceDot}
      </div>
      <div class="flex-grow-1 overflow-hidden">
        <div class="fw-bold small text-truncate"><span data-no-i18n>${escapeHtml(conv.otherUserName)}</span>${conv.isGroup ? ` <i class="bi bi-people-fill text-muted ms-1" title="Grup sohbeti"></i>` : ""}${conv.isMuted ? ` <i class="bi bi-bell-slash text-muted ms-1" title="Sessizde"></i>` : ""}</div>
        <div class="text-muted small text-truncate">${window.chatLastMessageHtml(conv)}</div>
      </div>
      ${conv.unreadCount > 0 ? `<span class="badge bg-danger rounded-pill flex-shrink-0">${conv.unreadCount}</span>` : ""}
    </button>`;
}

async function renderList() {
  const list = document.getElementById("conversationList");
  const conversations = await window.refreshChatConversations();
  list.innerHTML = conversations.length
    ? conversations.map(conversationListItemHtml).join("")
    : `<div class="p-3 text-muted small">Henüz mesajın yok.</div>`;
  filterConversations();
}

function filterConversations() {
  const box = document.getElementById("conversationSearch");
  const term = (box ? box.value : "").trim().toLocaleLowerCase(localeTag());
  document.querySelectorAll("#conversationList > button").forEach(row => {
    row.classList.toggle("d-none", !!term && !row.textContent.toLocaleLowerCase(localeTag()).includes(term));
  });
}

function readStatusHtml(isRead) {
  return ` <i class="bi ${isRead ? "bi-check2-all text-info" : "bi-check2"} read-status ms-1"></i>`;
}

function messageBubbleHtml(m) {
  return `
      <div class="chat-msg-row d-flex align-items-center mb-3 ${m.isMine ? "justify-content-end" : "justify-content-start"}" data-message-id="${m.id}" data-sent-at="${m.sentAt}">
        ${m.isMine ? window.chatMessageActionsHtml(m) : window.chatSenderAvatarHtml(m)}
        <div class="px-3 py-2 rounded-4 ${window.chatBubbleColorClasses(m)}" style="max-width:70%;">
          ${window.chatSenderNameHtml(m)}${window.chatMessageBodyHtml(m)}
          <div class="mt-1 ${m.isMine && m.type !== "Sticker" && !m.isDeleted ? "text-white-50" : "text-muted"}" style="font-size:10.5px;">${timeAgo(m.sentAt)}${m.isMine && !m.isDeleted ? readStatusHtml(m.isRead) : ""}</div>
        </div>
        ${m.isMine ? "" : window.chatMessageActionsHtml(m)}
      </div>`;
}

function appendMessage(m) {
  const thread = document.getElementById("conversationThread");
  if (thread.querySelector(`[data-message-id="${m.id}"]`)) return;
  if (!thread.querySelector("[data-message-id]")) thread.innerHTML = "";
  thread.insertAdjacentHTML("beforeend", messageBubbleHtml(m));
  thread.scrollTop = thread.scrollHeight;
}

function showThread() {
  document.getElementById("messagesLayout").classList.add("thread-open");
}

function showConversationList() {
  document.getElementById("messagesLayout").classList.remove("thread-open");
}

function renderHeader(userId, name, avatar, online) {
  document.getElementById("conversationHeader").innerHTML = `
    <button type="button" class="btn btn-link text-body p-0 d-md-none" onclick="showConversationList()" aria-label="Sohbetlere dön"><i class="bi bi-arrow-left fs-4"></i></button>
    <img src="${avatar || DEFAULT_AVATAR}" class="avatar-sm" alt="">
    <div>
      <div class="fw-bold"><a href="profile.html?id=${userId}" class="text-body text-decoration-none">${escapeHtml(name)}</a></div>
      <div class="text-muted small" id="presenceText" data-presence-user="${userId}" data-presence-mode="text">${online ? "Çevrimiçi" : "Çevrimdışı"}</div>
    </div>`;
}

function renderGroupHeader(conv) {
  document.getElementById("conversationHeader").innerHTML = `
    <button type="button" class="btn btn-link text-body p-0 d-md-none" onclick="showConversationList()" aria-label="Sohbetlere dön"><i class="bi bi-arrow-left fs-4"></i></button>
    ${window.chatConversationAvatarHtml(conv, "avatar-sm")}
    <div style="min-width:0;">
      <div class="fw-bold text-truncate">${conv.isCustomGroup
        ? `<button type="button" class="btn btn-link text-body text-decoration-none fw-bold p-0 text-truncate" onclick="openChatGroupInfo('${conv.id}')" title="Grup bilgisi" data-no-i18n>${escapeHtml(conv.otherUserName)}</button>`
        : `<a href="${window.chatConversationLink(conv)}" class="text-body text-decoration-none" data-no-i18n>${escapeHtml(conv.otherUserName)}</a>`}${conv.isMuted ? ` <i class="bi bi-bell-slash text-muted ms-1" title="Sessizde"></i>` : ""}</div>
      <div class="text-muted small" id="presenceText" data-no-i18n>${escapeHtml(window.chatGroupSubtitle(conv))}</div>
    </div>
    ${conv.isCustomGroup ? `<button type="button" class="btn btn-light btn-sm rounded-circle ms-auto flex-shrink-0" style="width:44px;height:44px;" onclick="openChatGroupInfo('${conv.id}')" aria-label="Grup bilgisi" title="Grup bilgisi"><i class="bi bi-info-circle"></i></button>` : ""}`;
}

async function selectConversation(id, { keepView = false } = {}) {
  activeId = id;
  draftUser = null;
  if (!keepView) showThread();
  document.getElementById("messageForm").classList.remove("d-none");
  await renderList();

  const conv = window.findChatConversation(id);
  if (conv && conv.isGroup) renderGroupHeader(conv);
  else if (conv) renderHeader(conv.otherUserId, conv.otherUserName, conv.otherUserAvatarUrl, conv.isOtherUserOnline);

  const thread = document.getElementById("conversationThread");
  try {
    const result = await apiFetch(`/api/messages/conversations/${id}?page=1&pageSize=50`);
    thread.innerHTML = result.items.map(messageBubbleHtml).join("");
    thread.scrollTop = thread.scrollHeight;
    await apiFetch(`/api/messages/conversations/${id}/read`, { method: "POST" });
    window.refreshMessageBadge?.();
    await renderList();
  } catch (err) {
    thread.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
  }
}

async function startDraftConversation(userId) {
  try {
    draftUser = await apiFetch(`/api/users/${userId}`);
  } catch (err) {
    document.getElementById("conversationThread").innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
    return;
  }
  activeId = null;
  showThread();
  document.getElementById("messageForm").classList.remove("d-none");
  renderHeader(draftUser.id, draftUser.fullName, draftUser.avatarUrl, draftUser.isOnline);
  document.getElementById("conversationThread").innerHTML = `<div class="text-muted small text-center py-4">${escapeHtml(draftUser.fullName)} ile henüz bir sohbetin yok. İlk mesajı gönder!</div>`;
}

let lastTypingSentAt = 0;

window.createChatComposer(document.getElementById("messageForm"), {
  getTarget: () => (activeId ? { conversationId: activeId } : draftUser ? { receiverId: draftUser.id } : null),

  onSent: async sent => {
    if (!activeId) {
      activeId = sent.conversationId;
      draftUser = null;
      await selectConversation(activeId);
    } else {
      appendMessage(sent);
      await renderList();
    }
  },

  onTyping: () => {
    if (!activeId || Date.now() - lastTypingSentAt < 2500) return;
    lastTypingSentAt = Date.now();
    window.realtime.sendTyping(activeId);
  }
});

document.addEventListener("realtime:message", async e => {
  const m = e.detail;

  if (!activeId && draftUser && m.senderId === draftUser.id) {
    e.preventDefault();
    activeId = m.conversationId;
    draftUser = null;
    await selectConversation(activeId);
    return;
  }

  if (m.conversationId === activeId) {
    e.preventDefault();
    appendMessage(m);
    hideTyping();
    if (!m.isMine && !document.hidden) await markActiveConversationRead();
  }
  renderList();
});

async function markActiveConversationRead() {
  if (!activeId) return;
  try {
    await apiFetch(`/api/messages/conversations/${activeId}/read`, { method: "POST" });
    window.refreshMessageBadge?.();
    renderList();
  } catch (err) {
  }
}

document.addEventListener("visibilitychange", () => {
  if (!document.hidden) markActiveConversationRead();
});

document.addEventListener("realtime:messages-read", e => {
  if (e.detail.conversationId !== activeId) return;
  window.chatApplyReadReceipt(document.getElementById("conversationThread"), e.detail, "bi bi-check2-all text-info read-status ms-1");
});

document.addEventListener("realtime:conversation-removed", async e => {
  if (e.detail.conversationId === activeId) {
    activeId = null;
    document.getElementById("conversationHeader").innerHTML = "";
    document.getElementById("conversationThread").innerHTML = `<div class="text-muted small text-center py-4">Artık bu grubun üyesi değilsin.</div>`;
    document.getElementById("messageForm").classList.add("d-none");
    showConversationList();
  }
  await renderList();
});

document.addEventListener("realtime:conversation-changed", async e => {
  await renderList();
  if (e.detail && e.detail.id === activeId) {
    const conv = window.findChatConversation(activeId);
    if (conv) renderGroupHeader(conv);
  }
});

for (const eventName of ["realtime:message-deleted", "realtime:message-updated"]) {
  document.addEventListener(eventName, e => {
    const row = document.querySelector(`#conversationThread [data-message-id="${e.detail.id}"]`);
    if (row) row.outerHTML = messageBubbleHtml(e.detail);
    renderList();
  });
}

let typingTimer = null;

function hideTyping() {
  clearTimeout(typingTimer);
  const label = document.getElementById("presenceText");
  if (!label || label.dataset.typing !== "1") return;
  delete label.dataset.typing;
  label.classList.remove("text-primary");
  const conv = window.findChatConversation(activeId);
  if (conv && conv.isGroup) label.textContent = window.chatGroupSubtitle(conv);
  else label.textContent = conv && conv.isOtherUserOnline ? "Çevrimiçi" : "Çevrimdışı";
}

document.addEventListener("realtime:typing", e => {
  if (e.detail.conversationId !== activeId) return;
  const label = document.getElementById("presenceText");
  if (!label) return;
  label.dataset.typing = "1";
  label.classList.add("text-primary");
  label.textContent = window.chatTypingText(e.detail);
  clearTimeout(typingTimer);
  typingTimer = setTimeout(hideTyping, 3500);
});

document.addEventListener("realtime:reconnected", () => {
  if (activeId) selectConversation(activeId, { keepView: true });
  else renderList();
});

async function init() {
  await renderList();

  const params = new URLSearchParams(window.location.search);
  const userId = params.get("userId");
  const conversationId = params.get("conversation");

  if (conversationId && window.findChatConversation(conversationId)) {
    await selectConversation(conversationId);
  } else if (userId) {
    const existing = window.chatConversationsCache.find(c => c.otherUserId === userId);
    if (existing) {
      await selectConversation(existing.id);
    } else {
      await startDraftConversation(userId);
    }
  } else if (window.chatConversationsCache.length && window.matchMedia("(min-width: 768px)").matches) {
    await selectConversation(window.chatConversationsCache[0].id);
  }
}

init();
