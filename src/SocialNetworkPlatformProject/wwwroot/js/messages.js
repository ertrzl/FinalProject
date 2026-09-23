// messages.html — real conversations wired to the backend. REST-based (no live push yet;
// SignalR's MessagesHub is wired server-side but this page doesn't connect to it — reload to see new messages).

requireAuth();

let activeId = null;      // real conversation id once one exists
let draftUser = null;     // { id, fullName, avatarUrl } when starting a brand-new conversation

function conversationListItemHtml(conv) {
  const activeClass = conv.id === activeId ? "bg-primary-subtle" : "";
  return `
    <button class="btn w-100 text-start rounded-0 px-3 py-3 border-0 border-bottom d-flex align-items-center gap-2 ${activeClass}" onclick="selectConversation('${conv.id}')">
      <div class="position-relative flex-shrink-0">
        <img src="${conv.otherUserAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">
        ${conv.isOtherUserOnline ? '<span class="position-absolute bottom-0 end-0 bg-success border border-2 border-white rounded-circle" style="width:11px;height:11px;"></span>' : ""}
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

function messageBubbleHtml(m) {
  return `
      <div class="d-flex mb-3 ${m.isMine ? "justify-content-end" : "justify-content-start"}">
        <div class="px-3 py-2 rounded-4 ${m.isMine ? "bg-primary text-white" : "bg-body-tertiary"}" style="max-width:70%;">
          <div class="small">${escapeHtml(m.text)}</div>
          <div class="mt-1 ${m.isMine ? "text-white-50" : "text-muted"}" style="font-size:10.5px;">${timeAgo(m.sentAt)}</div>
        </div>
      </div>`;
}

function renderHeader(name, avatar, online) {
  document.getElementById("conversationHeader").innerHTML = `
    <img src="${avatar || DEFAULT_AVATAR}" class="avatar-sm" alt="">
    <div>
      <div class="fw-bold">${escapeHtml(name)}</div>
      <div class="text-muted small">${online ? "Çevrimiçi" : "Çevrimdışı"}</div>
    </div>`;
}

async function selectConversation(id) {
  activeId = id;
  draftUser = null;
  document.getElementById("messageForm").classList.remove("d-none");
  await renderList();

  const conv = window.findChatConversation(id);
  if (conv) renderHeader(conv.otherUserName, conv.otherUserAvatarUrl, conv.isOtherUserOnline);

  const thread = document.getElementById("conversationThread");
  try {
    const result = await apiFetch(`/api/messages/conversations/${id}?page=1&pageSize=50`);
    thread.innerHTML = result.items.map(messageBubbleHtml).join("");
    thread.scrollTop = thread.scrollHeight;
    await apiFetch(`/api/messages/conversations/${id}/read`, { method: "POST" });
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
  renderHeader(draftUser.fullName, draftUser.avatarUrl, draftUser.isOnline);
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
    const sent = await apiFetch("/api/messages", { method: "POST", body });

    if (!activeId) {
      activeId = sent.conversationId;
      draftUser = null;
    }
    await selectConversation(activeId);
  } catch (err) {
    toast(err.message || "Mesaj gönderilemedi.");
  }
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
