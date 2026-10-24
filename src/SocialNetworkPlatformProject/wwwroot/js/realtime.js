(function () {
  if (!getSession()) return;

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
    GroupPostPinned: "grupta bir duyuru sabitledi.",
    GroupChatAdded: "seni bir grup sohbetine ekledi."
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
        if (/401|unauthori[sz]ed/i.test(String(err?.message))) {
          try { await getValidAccessToken(true); } catch { return; }
        }
        setTimeout(start, 5000);
      }
    }

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
    if (conversation && conversation.isMuted) return;
    const preview = window.chatMessagePreviewText ? window.chatMessagePreviewText(m) : m.text;
    const title = conversation ? conversation.otherUserName + (conversation.isGroup && m.senderName ? ` · ${m.senderName}` : "") : "Yeni mesaj";
    toast(`<b>${escapeHtml(title)}</b>: ${escapeHtml(preview)}`, { html: true });
  });

  messages.connection.on("ConversationRemoved", data => {
    document.dispatchEvent(new CustomEvent("realtime:conversation-removed", { detail: data }));
    window.refreshMessageBadge?.();
  });

  for (const eventName of ["ConversationAdded", "ConversationUpdated"]) {
    messages.connection.on(eventName, conversation => {
      document.dispatchEvent(new CustomEvent("realtime:conversation-changed", { detail: conversation }));
      window.refreshMessageBadge?.();
    });
  }

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

  messages.connection.on("MessageDeleted", m => {
    document.dispatchEvent(new CustomEvent("realtime:message-deleted", { detail: m }));
    window.refreshMessageBadge?.();
  });

  messages.connection.on("MessageUpdated", m =>
    document.dispatchEvent(new CustomEvent("realtime:message-updated", { detail: m })));

  messages.connection.on("MessagesRead", data =>
    document.dispatchEvent(new CustomEvent("realtime:messages-read", { detail: data })));

  messages.connection.on("UserTyping", data =>
    document.dispatchEvent(new CustomEvent("realtime:typing", { detail: data })));

  messages.connection.on("GameUpdated", game =>
    document.dispatchEvent(new CustomEvent("realtime:game-updated", { detail: game })));

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
