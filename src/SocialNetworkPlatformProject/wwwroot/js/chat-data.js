// Shared conversation cache backed by the real backend. Used by chat-dock.js and messages.js.
// requireAuth() must have already run on the page (every page that includes this also includes api.js).

window.chatConversationsCache = [];

window.refreshChatConversations = async function () {
  try {
    window.chatConversationsCache = await apiFetch("/api/messages/conversations");
  } catch (err) {
    window.chatConversationsCache = [];
  }
  return window.chatConversationsCache;
};

window.findChatConversation = function (id) {
  return window.chatConversationsCache.find(c => c.id === id);
};

// What a message looks like as plain text (toasts, previews).
window.chatMessagePreviewText = function (m) {
  return m.type === "Image" ? "📷 Fotoğraf" : m.text;
};

// Inner HTML of a message bubble: a photo (+ caption), a large sticker emoji, or plain text.
window.chatMessageBodyHtml = function (m, compact) {
  if (m.type === "Sticker")
    return `<div class="chat-sticker" style="font-size:${compact ? 44 : 64}px;">${escapeHtml(m.text)}</div>`;

  let html = "";
  if (m.type === "Image") {
    html += `<img src="${m.mediaUrl}" class="chat-image d-block rounded-3" style="max-width:${compact ? 170 : 260}px;max-height:${compact ? 180 : 300}px;" onclick="openChatImage('${m.mediaUrl}')" alt="Fotoğraf">`;
  }
  if (m.text) html += `<div class="${compact ? "" : "small"}${m.type === "Image" ? " mt-1" : ""}">${escapeHtml(m.text)}</div>`;
  return html;
};

// Stickers have no bubble background.
window.chatBubbleColorClasses = function (m) {
  if (m.type === "Sticker") return "";
  return m.isMine ? "bg-primary text-white" : "bg-body-tertiary";
};

window.openChatImage = function (url) {
  const overlay = document.createElement("div");
  overlay.className = "chat-image-viewer";
  overlay.innerHTML = `<img src="${url}" alt="">`;
  overlay.addEventListener("click", () => overlay.remove());
  document.addEventListener("keydown", function close(e) {
    if (e.key === "Escape") { overlay.remove(); document.removeEventListener("keydown", close); }
  });
  document.body.appendChild(overlay);
};
