// Live SignalR connections (notifications + messages) shared by every authenticated page.
// Pages react by listening to events on document:
//   "realtime:notification"  detail = notification DTO
//   "realtime:message"       detail = message DTO (cancelable: call preventDefault() if the page already shows it, to suppress the toast)
//   "realtime:reconnected"   the connection was lost and came back — reload anything that may have been missed

(function () {
  if (!getSession()) return;

  // SignalR client failed to load (e.g. CDN blocked): pages still work, just without live updates.
  if (typeof signalR === "undefined") {
    window.realtime = {
      sendTyping() {},
      sendMessage: dto => apiFetch("/api/messages", { method: "POST", body: dto })
    };
    return;
  }

  const NOTIFICATION_TOAST = {
    FriendRequestReceived: "sana arkadaşlık isteği gönderdi.",
    FriendRequestAccepted: "arkadaşlık isteğini kabul etti.",
    PostLiked: "gönderini beğendi.",
    CommentAdded: "gönderine yorum yaptı.",
    CommentLiked: "yorumunu beğendi."
  };

  function createConnection(url) {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, { accessTokenFactory: () => getToken() || "" })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .build();

    let needsResync = false;

    async function start() {
      if (!getSession()) return;
      try {
        await connection.start();
        if (needsResync) {
          needsResync = false;
          document.dispatchEvent(new CustomEvent("realtime:reconnected"));
        }
      } catch (err) {
        needsResync = true;
        setTimeout(start, 5000);
      }
    }

    // Built-in automatic reconnect gives up after its schedule; keep retrying so the page never goes silently dead.
    connection.onclose(() => {
      needsResync = true;
      setTimeout(start, 5000);
    });
    connection.onreconnected(() => document.dispatchEvent(new CustomEvent("realtime:reconnected")));

    return { connection, start };
  }

  const notifications = createConnection("/hubs/notifications");
  const messages = createConnection("/hubs/messages");

  notifications.connection.on("ReceiveNotification", n => {
    document.dispatchEvent(new CustomEvent("realtime:notification", { detail: n }));
    window.refreshNotificationBadge?.();
    const text = NOTIFICATION_TOAST[n.type];
    if (text) toast(`<b>${escapeHtml(n.actorName)}</b> ${text}`);
  });

  messages.connection.on("ReceiveMessage", async m => {
    const event = new CustomEvent("realtime:message", { detail: m, cancelable: true });
    document.dispatchEvent(event);
    window.refreshMessageBadge?.();

    if (m.isMine || event.defaultPrevented) return;
    const conversations = window.refreshChatConversations ? await window.refreshChatConversations() : [];
    const conversation = conversations.find(c => c.id === m.conversationId);
    toast(`<b>${escapeHtml(conversation ? conversation.otherUserName : "Yeni mesaj")}</b>: ${escapeHtml(m.text)}`);
  });

  // Feed changes pushed by the server, re-dispatched as "realtime:<name>" (see live-posts.js and feed.js).
  const FEED_EVENTS = {
    PostCreated: "post-created",
    PostUpdated: "post-updated",
    PostDeleted: "post-deleted",
    PostLikeCountChanged: "post-likes",
    CommentAdded: "comment-added",
    CommentDeleted: "comment-deleted",
    CommentLikeCountChanged: "comment-likes"
  };
  Object.entries(FEED_EVENTS).forEach(([serverName, eventName]) => {
    notifications.connection.on(serverName, data =>
      document.dispatchEvent(new CustomEvent("realtime:" + eventName, { detail: data })));
  });

  // Elements showing someone's online state carry data-presence-user (+ data-presence-mode="text" for a "Çevrimiçi" label, otherwise a dot).
  notifications.connection.on("PresenceChanged", ({ userId, isOnline }) => {
    (window.chatConversationsCache || []).forEach(c => {
      if (c.otherUserId === userId) c.isOtherUserOnline = isOnline;
    });
    document.querySelectorAll(`[data-presence-user="${userId}"]`).forEach(el => {
      if (el.dataset.presenceMode === "text") el.textContent = isOnline ? "Çevrimiçi" : "Çevrimdışı";
      else el.classList.toggle("d-none", !isOnline);
    });
    document.dispatchEvent(new CustomEvent("realtime:presence", { detail: { userId, isOnline } }));
  });

  messages.connection.on("MessagesRead", data =>
    document.dispatchEvent(new CustomEvent("realtime:messages-read", { detail: data })));

  messages.connection.on("UserTyping", data =>
    document.dispatchEvent(new CustomEvent("realtime:typing", { detail: data })));

  // Same call the REST endpoint does; falls back to it when the live connection is down so a send never just fails.
  window.realtime = {
    sendTyping(conversationId) {
      if (messages.connection.state === signalR.HubConnectionState.Connected)
        messages.connection.invoke("Typing", conversationId).catch(() => {});
    },

    async sendMessage(dto) {
      if (messages.connection.state === signalR.HubConnectionState.Connected) {
        try {
          return await messages.connection.invoke("SendMessage", dto);
        } catch (err) {
          if (messages.connection.state === signalR.HubConnectionState.Connected) throw err;
        }
      }
      return apiFetch("/api/messages", { method: "POST", body: dto });
    }
  };

  notifications.start();
  messages.start();
})();
