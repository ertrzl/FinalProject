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
