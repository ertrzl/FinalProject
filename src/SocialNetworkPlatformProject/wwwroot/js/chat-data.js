// Shared conversation data used by both the full Messages page and the
// bottom-right chat dock. Static seed data, no backend — state resets on reload.

window.chatConversations = [
  {
    id: "lana",
    name: "Lana Rose",
    avatar: "https://i.pravatar.cc/100?img=47",
    online: true,
    messages: [
      { fromMe: false, text: "Merhaba! Bugün buluşma hâlâ geçerli mi?", time: "09:12" },
      { fromMe: true, text: "Evet, saat 6'da kafede buluşalım.", time: "09:15" },
      { fromMe: false, text: "Harika, orada görüşürüz! 🎉", time: "09:16" },
    ],
  },
  {
    id: "winnie",
    name: "Winnie Haley",
    avatar: "https://i.pravatar.cc/100?img=32",
    online: false,
    messages: [
      { fromMe: false, text: "Renk paleti için mavi tonlarına karar verdim, teşekkürler!", time: "Dün" },
      { fromMe: true, text: "Ne demek, harika görünecek 👍", time: "Dün" },
    ],
  },
  {
    id: "daniel",
    name: "Daniel Bale",
    avatar: "https://i.pravatar.cc/100?img=15",
    online: true,
    messages: [
      { fromMe: false, text: "Hafta sonu yürüyüşe geliyor musun?", time: "Salı" },
      { fromMe: true, text: "Kesinlikle, saat kaçta?", time: "Salı" },
      { fromMe: false, text: "Sabah 9'da buluşalım.", time: "Salı" },
    ],
  },
  {
    id: "diana",
    name: "Diana Prince",
    avatar: "https://i.pravatar.cc/100?img=60",
    online: false,
    messages: [
      { fromMe: false, text: "Projeyle ilgili dosyaları gönderdim, kontrol edebilir misin?", time: "Pazartesi" },
    ],
  },
  {
    id: "jane",
    name: "Jane Doe",
    avatar: "https://i.pravatar.cc/100?img=25",
    online: true,
    messages: [
      { fromMe: false, text: "Arkadaşlık isteğimi kabul ettiğin için teşekkürler!", time: "1 hafta önce" },
      { fromMe: true, text: "Rica ederim, tanıştığımıza sevindim 🙂", time: "1 hafta önce" },
    ],
  },
];

window.findChatConversation = function (id) {
  return window.chatConversations.find(c => c.id === id);
};

window.lastChatMessage = function (conv) {
  return conv.messages[conv.messages.length - 1];
};

window.addChatConversation = function (name, avatar) {
  const existing = window.chatConversations.find(c => c.name.toLowerCase() === name.toLowerCase());
  if (existing) return existing;
  const conv = {
    id: "new-" + Date.now(),
    name,
    avatar: avatar || "https://i.pravatar.cc/100?img=1",
    online: false,
    messages: [],
  };
  window.chatConversations.unshift(conv);
  return conv;
};
