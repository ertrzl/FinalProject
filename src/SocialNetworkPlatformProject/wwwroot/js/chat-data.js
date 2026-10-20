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
  if (m.isDeleted) return "🚫 Mesaj silindi";
  return m.type === "Image" ? "📷 Fotoğraf" : m.text;
};

// The story a message answers, shown above its text: who answered whose story, and the story itself (or a note that it
// is gone: stories last 24 hours).
window.chatStoryReplyHtml = function (m, compact) {
  const reply = m.storyReply;
  if (!reply) return "";

  const label = m.isMine ? "Bir hikâyeye yanıt verdin" : "Hikâyene yanıt verdi";
  let thumb;
  if (!reply.isAvailable) {
    thumb = `<div class="chat-story-gone small"><i class="bi bi-clock-history me-1"></i>Hikâye artık yok</div>`;
  } else if (reply.mediaType === "Video") {
    thumb = `<video src="${reply.mediaUrl}" class="chat-story-thumb d-block" muted preload="metadata"></video>`;
  } else {
    thumb = `<img src="${reply.mediaUrl}" class="chat-story-thumb d-block" alt="Hikâye" onclick="openChatImage('${reply.mediaUrl}')">`;
  }

  return `<div class="chat-story-reply${compact ? " compact" : ""} mb-2"><div class="chat-story-label"><i class="bi bi-reply-fill me-1"></i>${label}</div>${thumb}</div>`;
};

// The message a message answers, shown above it as a quote (a tap on it scrolls to the original).
window.chatReplyQuoteHtml = function (m) {
  const q = m.replyTo;
  if (!q) return "";

  let body;
  if (q.isDeleted) body = `<span class="fst-italic"><i class="bi bi-slash-circle me-1"></i>Bu mesaj silindi</span>`;
  else if (q.type === "Image") body = `${q.mediaUrl ? `<img src="${q.mediaUrl}" class="chat-quote-thumb" alt="">` : ""}<span data-no-i18n>📷 ${q.text ? escapeHtml(q.text) : i18n("chat.photo")}</span>`;
  else body = `<span data-no-i18n>${escapeHtml(q.text || "")}</span>`;

  const who = q.isMine ? i18n("chat.you") : escapeHtml(q.senderName);
  return `<div class="chat-quote" onclick="jumpToChatMessage('${q.messageId}')"><div class="chat-quote-name" data-no-i18n>${who}</div><div class="chat-quote-body">${body}</div></div>`;
};

// The emojis under a bubble, each with its count; the ones I left are highlighted, and a tap on one leaves it too (or takes mine back).
window.chatReactionsHtml = function (m) {
  if (!m.reactions || m.reactions.length === 0) return "";
  const pills = m.reactions.map(r => `<button type="button" class="chat-reaction${r.reactedByMe ? " mine" : ""}" onclick="reactToChatMessage('${m.id}', '${r.emoji}')"><span>${r.emoji}</span>${r.count > 1 ? `<span class="chat-reaction-count">${r.count}</span>` : ""}</button>`).join("");
  return `<div class="chat-reactions">${pills}</div>`;
};

// Inner HTML of a message bubble: the quote of the message it answers, a photo (+ caption), a large sticker emoji, or
// plain text, and the emojis left on it.
window.chatMessageBodyHtml = function (m, compact) {
  if (m.isDeleted)
    return `<div class="fst-italic ${compact ? "" : "small"}"><i class="bi bi-slash-circle me-1"></i>Bu mesaj silindi</div>`;

  let html = window.chatReplyQuoteHtml(m);
  if (m.type === "Sticker")
    return html + `<div class="chat-sticker" style="font-size:${compact ? 44 : 64}px;">${escapeHtml(m.text)}</div>` + window.chatReactionsHtml(m);

  html += window.chatStoryReplyHtml(m, compact);
  if (m.type === "Image") {
    html += `<img src="${m.mediaUrl}" class="chat-image d-block rounded-3" style="max-width:${compact ? 170 : 260}px;max-height:${compact ? 180 : 300}px;" onclick="openChatImage('${m.mediaUrl}')" alt="Fotoğraf">`;
  }
  if (m.text) html += `<div class="${compact ? "" : "small"}${m.type === "Image" ? " mt-1" : ""}" data-no-i18n>${escapeHtml(m.text)}</div>`;
  return html + window.chatReactionsHtml(m);
};

// Stickers have no bubble background.
window.chatBubbleColorClasses = function (m) {
  if (m.isDeleted) return "border text-muted";
  if (m.type === "Sticker") return "";
  return m.isMine ? "bg-primary text-white" : "bg-body-tertiary";
};

// The messages on screen, by id: what "Yanıtla" and the emoji bar need to know about a message.
window.chatMessageCache = new Map();

// The small buttons beside a bubble (shown on hover; always on touch screens, see .msg-action): leave an emoji, answer
// it, and (my own messages only) take it back. A message that was taken back has none.
window.chatMessageActionsHtml = function (m) {
  if (m.isDeleted) return "";
  window.chatMessageCache.set(m.id, m);
  const remove = m.isMine ? `<button type="button" class="msg-action" title="Mesajı sil" onclick="deleteChatMessage('${m.id}')"><i class="bi bi-trash"></i></button>` : "";
  return `<span class="msg-actions d-flex align-items-center mx-1">
    <button type="button" class="msg-action" title="Tepki ver" onclick="openChatReactionPicker(event, '${m.id}')"><i class="bi bi-emoji-smile"></i></button>
    <button type="button" class="msg-action" title="Yanıtla" onclick="replyToChatMessage('${m.id}')"><i class="bi bi-reply"></i></button>${remove}</span>`;
};

// The emojis offered under "Tepki ver". Which ones are accepted is the server's rule (MessageReactions.Allowed).
window.CHAT_REACTIONS = ["❤️", "😂", "😮", "😢", "😡", "👍"];

window.openChatReactionPicker = function (event, id) {
  event.stopPropagation();
  document.querySelector(".reaction-picker")?.remove();

  const mine = (window.chatMessageCache.get(id)?.reactions || []).find(r => r.reactedByMe)?.emoji;
  const picker = document.createElement("div");
  picker.className = "reaction-picker";
  picker.innerHTML = window.CHAT_REACTIONS.map(emoji => `<button type="button" class="${emoji === mine ? "mine" : ""}" data-emoji="${emoji}">${emoji}</button>`).join("");
  document.body.appendChild(picker);

  // Above the button, kept inside the screen.
  const button = event.currentTarget.getBoundingClientRect();
  const width = picker.offsetWidth;
  picker.style.left = Math.max(8, Math.min(window.innerWidth - width - 8, button.left + button.width / 2 - width / 2)) + "px";
  picker.style.top = Math.max(8, button.top - picker.offsetHeight - 6) + "px";

  const close = () => { picker.remove(); document.removeEventListener("click", close); document.removeEventListener("keydown", onKey); };
  const onKey = e => { if (e.key === "Escape") close(); };
  picker.addEventListener("click", e => {
    const choice = e.target.closest("[data-emoji]");
    if (choice) window.reactToChatMessage(id, choice.dataset.emoji);
    close();
  });
  setTimeout(() => { document.addEventListener("click", close); document.addEventListener("keydown", onKey); }, 0);
};

// Leaves the emoji on a message, or takes it back when it is the one I left. The server tells every open tab of both
// people ("MessageUpdated"); the event is also raised here from the answer, so this tab updates even when the live
// connection is down. Pages redraw on the event.
window.reactToChatMessage = async function (id, emoji) {
  try {
    const updated = await apiFetch(`/api/messages/${id}/reaction`, { method: "PUT", body: { emoji } });
    document.dispatchEvent(new CustomEvent("realtime:message-updated", { detail: updated }));
  } catch (err) {
    toast(err.message || "Tepki bırakılamadı.");
  }
};

// "Yanıtla": the message bar of that conversation (chat-composer.js) shows what is being answered.
window.replyToChatMessage = function (id) {
  const message = window.chatMessageCache.get(id);
  if (message) document.dispatchEvent(new CustomEvent("chat:reply-to", { detail: message }));
};

// A tap on a quote scrolls to the message it quotes, which lights up for a moment.
window.jumpToChatMessage = function (id) {
  const row = Array.from(document.querySelectorAll(`[data-message-id="${id}"]`)).find(el => el.offsetParent !== null);
  if (!row) return;
  row.scrollIntoView({ block: "center", behavior: "smooth" });
  row.classList.add("chat-msg-flash");
  setTimeout(() => row.classList.remove("chat-msg-flash"), 1600);
};

// "Delete for everyone". The server tells every open tab of both people ("MessageDeleted"); the event is also raised
// here from the answer, so this tab updates even when the live connection is down. Pages redraw on the event.
window.deleteChatMessage = async function (id) {
  if (!(await confirmDialog("Bu mesaj herkes için silinsin mi?"))) return;
  try {
    const deleted = await apiFetch(`/api/messages/${id}`, { method: "DELETE" });
    document.dispatchEvent(new CustomEvent("realtime:message-deleted", { detail: deleted }));
  } catch (err) {
    toast(err.message || "Mesaj silinemedi.");
  }
};

// ---- Group chats (conv.isGroup): the list and the header show the group; each bubble names who wrote it ----

// Where the name in a list row / header leads: the group's page, or the person's profile.
window.chatConversationLink = function (conv) {
  return conv.isGroup ? `group.html?id=${conv.groupId}` : `profile.html?id=${conv.otherUserId}`;
};

// The round picture of a conversation: the person's, or the group's (its initial when it has none).
window.chatConversationAvatarHtml = function (conv, sizeClass) {
  if (!conv.isGroup) return `<img src="${conv.otherUserAvatarUrl || DEFAULT_AVATAR}" class="${sizeClass}" alt="">`;
  if (conv.otherUserAvatarUrl) return `<img src="${conv.otherUserAvatarUrl}" class="${sizeClass} group-avatar" alt="">`;
  const initial = (conv.otherUserName || "?").trim().charAt(0).toLocaleUpperCase(localeTag()) || "?";
  return `<span class="${sizeClass} group-avatar group-avatar-empty" aria-hidden="true" data-no-i18n>${escapeHtml(initial)}</span>`;
};

// The last message under a list row: "Sen: ..." for mine, "Ayşe: ..." in a group, the text alone otherwise. Safe HTML.
window.chatLastMessageHtml = function (conv) {
  if (!conv.lastMessageText) return "Henüz mesaj yok";
  const who = conv.lastMessageIsMine ? "Sen: " : conv.isGroup && conv.lastMessageSenderName ? `${escapeHtml(conv.lastMessageSenderName)}: ` : "";
  return `${who}<span data-no-i18n>${escapeHtml(conv.lastMessageText)}</span>`;
};

// The line under the name in a header: the group's member count (direct chats show the online state instead).
window.chatGroupSubtitle = function (conv) {
  return i18n("chat.groupMembers", conv.memberCount);
};

// A group chat's bubble from somebody else: their small picture beside it (the space stays when the previous bubble is
// theirs too) and their name above it.
window.chatSenderAvatarHtml = function (m) {
  if (m.isMine || !m.senderName) return "";
  return `<a href="profile.html?id=${m.senderId}" class="chat-sender-avatar flex-shrink-0 align-self-end me-1" title="${escapeHtml(m.senderName)}"><img src="${m.senderAvatarUrl || DEFAULT_AVATAR}" class="avatar-xs" alt=""></a>`;
};

window.chatSenderNameHtml = function (m) {
  if (m.isMine || !m.senderName || m.isDeleted) return "";
  return `<div class="chat-sender-name" data-no-i18n>${escapeHtml(m.senderName)}</div>`;
};

// "yazıyor..." — in a group chat with the name of who is typing.
window.chatTypingText = function (detail) {
  return detail.userName ? i18n("chat.typingName", detail.userName) : "yazıyor...";
};

// The others have read my messages: double ticks. In a group chat only up to when *all* the others have read (readUpTo,
// from the server); the rows carry their time (data-sent-at) for that.
window.chatApplyReadReceipt = function (container, detail, readClass) {
  if (!container) return;
  const upTo = detail.readUpTo ? parseServerDate(detail.readUpTo).getTime() : null;
  container.querySelectorAll(".read-status").forEach(icon => {
    const row = icon.closest("[data-sent-at]");
    if (upTo !== null && row && parseServerDate(row.dataset.sentAt).getTime() > upTo) return;
    icon.className = readClass;
  });
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
