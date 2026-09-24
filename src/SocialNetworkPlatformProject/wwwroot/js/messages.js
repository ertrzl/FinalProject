// messages.html — real conversations wired to the backend; new messages arrive live through realtime.js (SignalR).

requireAuth();

let activeId = null;      // real conversation id once one exists
let draftUser = null;     // { id, fullName, avatarUrl } when starting a brand-new conversation

function conversationListItemHtml(conv) {
  const activeClass = conv.id === activeId ? "bg-primary-subtle" : "";
  return `
    <button class="btn w-100 text-start rounded-0 px-3 py-3 border-0 border-bottom d-flex align-items-center gap-2 ${activeClass}" onclick="selectConversation('${conv.id}')">
      <div class="position-relative flex-shrink-0">
        <img src="${conv.otherUserAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">
        <span class="position-absolute bottom-0 end-0 bg-success border border-2 border-white rounded-circle ${conv.isOtherUserOnline ? "" : "d-none"}" style="width:11px;height:11px;" data-presence-user="${conv.otherUserId}"></span>
      </div>
      <div class="flex-grow-1 overflow-hidden">
        <div class="fw-bold small text-truncate">${escapeHtml(conv.otherUserName)}</div>
        <div class="text-muted small text-truncate">${conv.lastMessageText ? (conv.lastMessageIsMine ? "Sen: " : "") + escapeHtml(conv.lastMessageText) : "Henüz mesaj yok"}</div>
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
}

// One tick = delivered, two ticks = the other person has seen it.
function readStatusHtml(isRead) {
  return ` <i class="bi ${isRead ? "bi-check2-all text-info" : "bi-check2"} read-status ms-1"></i>`;
}

function messageBubbleHtml(m) {
  return `
      <div class="d-flex mb-3 ${m.isMine ? "justify-content-end" : "justify-content-start"}" data-message-id="${m.id}">
        <div class="px-3 py-2 rounded-4 ${m.isMine ? "bg-primary text-white" : "bg-body-tertiary"}" style="max-width:70%;">
          <div class="small">${escapeHtml(m.text)}</div>
          <div class="mt-1 ${m.isMine ? "text-white-50" : "text-muted"}" style="font-size:10.5px;">${timeAgo(m.sentAt)}${m.isMine ? readStatusHtml(m.isRead) : ""}</div>
        </div>
      </div>`;
}

// Live messages can arrive twice (our own echo + the send() result), so skip anything already on screen.
function appendMessage(m) {
  const thread = document.getElementById("conversationThread");
  if (thread.querySelector(`[data-message-id="${m.id}"]`)) return;
  if (!thread.querySelector("[data-message-id]")) thread.innerHTML = "";
  thread.insertAdjacentHTML("beforeend", messageBubbleHtml(m));
  thread.scrollTop = thread.scrollHeight;
}

function renderHeader(userId, name, avatar, online) {
  document.getElementById("conversationHeader").innerHTML = `
    <img src="${avatar || DEFAULT_AVATAR}" class="avatar-sm" alt="">
    <div>
      <div class="fw-bold">${escapeHtml(name)}</div>
      <div class="text-muted small" id="presenceText" data-presence-user="${userId}" data-presence-mode="text">${online ? "Çevrimiçi" : "Çevrimdışı"}</div>
    </div>`;
}

async function selectConversation(id) {
  activeId = id;
  draftUser = null;
  document.getElementById("messageForm").classList.remove("d-none");
  await renderList();

  const conv = window.findChatConversation(id);
  if (conv) renderHeader(conv.otherUserId, conv.otherUserName, conv.otherUserAvatarUrl, conv.isOtherUserOnline);

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
  document.getElementById("messageForm").classList.remove("d-none");
  renderHeader(draftUser.id, draftUser.fullName, draftUser.avatarUrl, draftUser.isOnline);
  document.getElementById("conversationThread").innerHTML = `<div class="text-muted small text-center py-4">${escapeHtml(draftUser.fullName)} ile henüz bir sohbetin yok. İlk mesajı gönder!</div>`;
}

document.getElementById("messageForm").addEventListener("submit", async function (e) {
  e.preventDefault();
  const input = document.getElementById("messageInput");
  const text = input.value.trim();
  if (!text) return;
  input.value = "";

  try {
    const body = activeId ? { conversationId: activeId, text } : { receiverId: draftUser.id, text };
    const sent = await window.realtime.sendMessage(body);

    if (!activeId) {
      activeId = sent.conversationId;
      draftUser = null;
      await selectConversation(activeId);
    } else {
      appendMessage(sent);
      await renderList();
    }
  } catch (err) {
    toast(err.message || "Mesaj gönderilemedi.");
  }
});

document.addEventListener("realtime:message", async e => {
  const m = e.detail;

  // First reply from someone we were just about to message: the conversation exists now, so open it.
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
    // A message only counts as seen while the tab is actually in front (see the visibilitychange handler below).
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
    // Silent: it will be marked read next time the conversation is opened.
  }
}

document.addEventListener("visibilitychange", () => {
  if (!document.hidden) markActiveConversationRead();
});

// The other person has opened our messages: turn our single ticks into double ticks.
document.addEventListener("realtime:messages-read", e => {
  if (e.detail.conversationId !== activeId) return;
  document.querySelectorAll("#conversationThread .read-status").forEach(icon => {
    icon.className = "bi bi-check2-all text-info read-status ms-1";
  });
});

// "yazıyor..." replaces the online label for a few seconds after each typing signal.
let typingTimer = null;

function hideTyping() {
  clearTimeout(typingTimer);
  const label = document.getElementById("presenceText");
  if (!label || label.dataset.typing !== "1") return;
  delete label.dataset.typing;
  label.classList.remove("text-primary");
  const conv = window.findChatConversation(activeId);
  label.textContent = conv && conv.isOtherUserOnline ? "Çevrimiçi" : "Çevrimdışı";
}

document.addEventListener("realtime:typing", e => {
  if (e.detail.conversationId !== activeId) return;
  const label = document.getElementById("presenceText");
  if (!label) return;
  label.dataset.typing = "1";
  label.classList.add("text-primary");
  label.textContent = "yazıyor...";
  clearTimeout(typingTimer);
  typingTimer = setTimeout(hideTyping, 3500);
});

let lastTypingSentAt = 0;
document.getElementById("messageInput").addEventListener("input", () => {
  if (!activeId || Date.now() - lastTypingSentAt < 2500) return;
  lastTypingSentAt = Date.now();
  window.realtime.sendTyping(activeId);
});

document.addEventListener("realtime:reconnected", () => {
  if (activeId) selectConversation(activeId);
  else renderList();
});

async function init() {
  await renderList();

  const params = new URLSearchParams(window.location.search);
  const userId = params.get("userId");

  if (userId) {
    const existing = window.chatConversationsCache.find(c => c.otherUserId === userId);
    if (existing) {
      await selectConversation(existing.id);
    } else {
      await startDraftConversation(userId);
    }
  } else if (window.chatConversationsCache.length) {
    await selectConversation(window.chatConversationsCache[0].id);
  }
}

init();
