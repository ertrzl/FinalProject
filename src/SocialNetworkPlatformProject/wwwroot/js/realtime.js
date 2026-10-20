// Live SignalR connections (notifications + messages) shared by every authenticated page.
// Pages react by listening to events on document:
//   "realtime:notification"  detail = notification DTO
//   "realtime:message"       detail = message DTO (cancelable: call preventDefault() if the page already shows it, to suppress the toast)
//   "realtime:message-deleted"  detail = the message DTO after the sender took it back (isDeleted = true)
//   "realtime:conversation-removed"  detail = { conversationId }: a group chat the user is no longer a member of
//   "realtime:game-updated"  detail = the game (X-O or rock-paper-scissors) as this user sees it (games.html and game.html redraw from it)
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
    CommentLiked: "yorumunu beğendi.",
    StoryLiked: "hikâyeni beğendi.",
    StoryMentioned: "seni bir hikâyede etiketledi.",
    StoryReshared: "etiketlendiğin hikâyeyi kendi hikâyesine ekledi.",
    Mentioned: "seni etiketledi.",
    PostShared: "gönderini paylaştı.",
    EventShared: "etkinliğini gönderide paylaştı.",
    GameInviteReceived: "sana oyun teklif etti.",
    GameInviteAccepted: "oyun davetini kabul etti.",
    MarketplacePriceDropped: "kaydettiğin bir ilanın fiyatını düşürdü.",
    GroupPostPinned: "grupta bir duyuru sabitledi."
  };

  function createConnection(url) {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, { accessTokenFactory: async () => (await getValidAccessToken()) || "" })
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
        // The server turned the token down (suspended account, password changed elsewhere, signed out): ask for a fresh one
        // once. If that is refused too, getValidAccessToken sends the person to the login page, so the retries stop.
        if (/401|unauthori[sz]ed/i.test(String(err?.message))) {
          try { await getValidAccessToken(true); } catch { return; }
        }
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
    if (text) toast(`<b>${escapeHtml(n.actorName)}</b> ${text}`, { html: true });
  });

  messages.connection.on("ReceiveMessage", async m => {
    const event = new CustomEvent("realtime:message", { detail: m, cancelable: true });
    document.dispatchEvent(event);
    window.refreshMessageBadge?.();

    if (m.isMine || event.defaultPrevented) return;
    const conversations = window.refreshChatConversations ? await window.refreshChatConversations() : [];
    const conversation = conversations.find(c => c.id === m.conversationId);
    // A group the person muted makes no noise: the message is in the chat and the list, without a toast.
    if (conversation && conversation.isMuted) return;
    const preview = window.chatMessagePreviewText ? window.chatMessagePreviewText(m) : m.text;
    // A group chat names the group and who wrote: "Kitap Kulübü · Ayşe: ...".
    const title = conversation ? conversation.otherUserName + (conversation.isGroup && m.senderName ? ` · ${m.senderName}` : "") : "Yeni mesaj";
    toast(`<b>${escapeHtml(title)}</b>: ${escapeHtml(preview)}`, { html: true });
  });

  // The person left or was removed from a group: its chat is no longer theirs (pages close it and drop it from the list).
  messages.connection.on("ConversationRemoved", data => {
    document.dispatchEvent(new CustomEvent("realtime:conversation-removed", { detail: data }));
    window.refreshMessageBadge?.();
  });

  // Feed changes pushed by the server, re-dispatched as "realtime:<name>" (see live-posts.js and feed.js).
  const FEED_EVENTS = {
    PostCreated: "post-created",
    PostUpdated: "post-updated",
    PostDeleted: "post-deleted",
    PostLikeCountChanged: "post-likes",
    PollVoted: "poll-voted",
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

  // The sender took a message back: detail = the message as this user sees it now (isDeleted, no text or photo).
  messages.connection.on("MessageDeleted", m => {
    document.dispatchEvent(new CustomEvent("realtime:message-deleted", { detail: m }));
    window.refreshMessageBadge?.();
  });

  // Somebody left or took back an emoji on a message: detail = the message as this user sees it now.
  messages.connection.on("MessageUpdated", m =>
    document.dispatchEvent(new CustomEvent("realtime:message-updated", { detail: m })));

  messages.connection.on("MessagesRead", data =>
    document.dispatchEvent(new CustomEvent("realtime:messages-read", { detail: data })));

  messages.connection.on("UserTyping", data =>
    document.dispatchEvent(new CustomEvent("realtime:typing", { detail: data })));

  // An X-O game changed (invited, answered, a move, the end): detail = the game as this user sees it (same shape as GET /api/games/{id}).
  messages.connection.on("GameUpdated", game =>
    document.dispatchEvent(new CustomEvent("realtime:game-updated", { detail: game })));

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
