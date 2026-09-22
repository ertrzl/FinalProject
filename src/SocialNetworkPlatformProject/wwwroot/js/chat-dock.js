// Bottom-right chat dock (Messenger-style), shown on every logged-in page.
// Shares data with messages.html via js/chat-data.js. REST-based (no live push yet).

const openPopups = [];
const MAX_POPUPS = 2;

async function renderDockList() {
  const list = document.getElementById("chatDockList");
  if (!list) return;
  const conversations = await window.refreshChatConversations();
  list.innerHTML = conversations
    .map(conv => `
        <button class="btn w-100 text-start rounded-0 px-3 py-2 border-0 border-bottom d-flex align-items-center gap-2" onclick="openChatPopup('${conv.id}')">
          <div class="position-relative flex-shrink-0">
            <img src="${conv.otherUserAvatarUrl || DEFAULT_AVATAR}" class="avatar-sm" alt="">
            ${conv.isOtherUserOnline ? '<span class="position-absolute bottom-0 end-0 bg-success border border-2 border-white rounded-circle" style="width:10px;height:10px;"></span>' : ""}
          </div>
          <div class="flex-grow-1 overflow-hidden">
            <div class="fw-bold small text-truncate">${escapeHtml(conv.otherUserName)}</div>
            <div class="text-muted small text-truncate">${conv.lastMessageText ? escapeHtml(conv.lastMessageText) : "Henüz mesaj yok"}</div>
          </div>
          ${conv.unreadCount > 0 ? `<span class="badge bg-danger rounded-pill flex-shrink-0">${conv.unreadCount}</span>` : ""}
        </button>`)
    .join("") || `<div class="p-3 text-muted small">Henüz mesajın yok.</div>`;
}

function toggleChatDockList() {
  const list = document.getElementById("chatDockList");
  if (list) list.classList.toggle("d-none");
}

function closeChatDockList(event) {
  const dock = document.getElementById("chatDock");
  const list = document.getElementById("chatDockList");
  if (dock && list && !dock.contains(event.target)) {
    list.classList.add("d-none");
  }
}

async function openChatPopup(id) {
  document.getElementById("chatDockList")?.classList.add("d-none");

  if (openPopups.includes(id)) {
    renderPopup(id);
    return;
  }
  openPopups.push(id);
  if (openPopups.length > MAX_POPUPS) {
    closeChatPopup(openPopups.shift());
  }
  buildPopupShell(id);
  await renderPopup(id);
}

function buildPopupShell(id) {
  const conv = window.findChatConversation(id);
  if (!conv) return;
  const container = document.getElementById("chatPopups");
  const popup = document.createElement("div");
  popup.className = "chat-popup";
  popup.id = "chatPopup-" + id;
  popup.innerHTML = `
    <div class="chat-popup-header">
      <div class="position-relative flex-shrink-0">
        <img src="${conv.otherUserAvatarUrl || DEFAULT_AVATAR}" class="avatar-xs" alt="">
        ${conv.isOtherUserOnline ? '<span class="position-absolute bottom-0 end-0 bg-success border border-1 border-white rounded-circle" style="width:9px;height:9px;"></span>' : ""}
      </div>
      <div class="flex-grow-1 overflow-hidden">
        <div class="fw-bold small text-truncate">${escapeHtml(conv.otherUserName)}</div>
      </div>
      <button type="button" class="btn-close btn-close-white btn-sm" onclick="closeChatPopup('${id}')"></button>
    </div>
    <div class="chat-popup-thread" id="chatPopupThread-${id}"></div>
    <form class="chat-popup-form" onsubmit="sendChatPopupMessage(event, '${id}')">
      <input type="text" class="form-control form-control-sm rounded-pill" placeholder="Bir mesaj yaz...">
      <button type="submit" class="btn btn-primary btn-sm rounded-circle chat-popup-send"><i class="bi bi-send-fill"></i></button>
    </form>`;
  container.appendChild(popup);
}

async function renderPopup(id) {
  const thread = document.getElementById("chatPopupThread-" + id);
  if (!thread) return;
  try {
    const result = await apiFetch(`/api/messages/conversations/${id}?page=1&pageSize=30`);
    thread.innerHTML = result.items.map(bubbleHtml).join("");
    thread.scrollTop = thread.scrollHeight;
    apiFetch(`/api/messages/conversations/${id}/read`, { method: "POST" }).catch(() => {});
  } catch (err) {
    thread.innerHTML = `<div class="text-danger small p-2">${escapeHtml(err.message)}</div>`;
  }
}

function bubbleHtml(m) {
  return `
      <div class="d-flex mb-2 ${m.isMine ? "justify-content-end" : "justify-content-start"}">
        <div class="px-2 py-1 rounded-3 ${m.isMine ? "bg-primary text-white" : "bg-body-tertiary"}" style="max-width:80%; font-size:13px;">
          ${escapeHtml(m.text)}
        </div>
      </div>`;
}

async function sendChatPopupMessage(event, id) {
  event.preventDefault();
  const form = event.target;
  const input = form.querySelector("input");
  const text = input.value.trim();
  if (!text) return;
  input.value = "";

  try {
    await apiFetch("/api/messages", { method: "POST", body: { conversationId: id, text } });
    await renderPopup(id);
  } catch (err) {
    toast(err.message || "Mesaj gönderilemedi.");
  }
}

function closeChatPopup(id) {
  const el = document.getElementById("chatPopup-" + id);
  if (el) el.remove();
  const idx = openPopups.indexOf(id);
  if (idx !== -1) openPopups.splice(idx, 1);
}

window.refreshChatDock = function () {
  renderDockList();
  openPopups.forEach(renderPopup);
};

document.addEventListener("click", closeChatDockList);
renderDockList();
