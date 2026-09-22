// Messages page — uses the shared conversation data from js/chat-data.js.

let activeId = window.chatConversations[0].id;

function renderList() {
  const list = document.getElementById("conversationList");
  list.innerHTML = window.chatConversations
    .map(conv => {
      const last = window.lastChatMessage(conv);
      const activeClass = conv.id === activeId ? "bg-primary-subtle" : "";
      return `
        <button class="btn w-100 text-start rounded-0 px-3 py-3 border-0 border-bottom d-flex align-items-center gap-2 ${activeClass}" onclick="selectConversation('${conv.id}')">
          <div class="position-relative flex-shrink-0">
            <img src="${conv.avatar}" class="avatar-sm" alt="">
            ${conv.online ? '<span class="position-absolute bottom-0 end-0 bg-success border border-2 border-white rounded-circle" style="width:11px;height:11px;"></span>' : ""}
          </div>
          <div class="flex-grow-1 overflow-hidden">
            <div class="fw-bold small text-truncate">${conv.name}</div>
            <div class="text-muted small text-truncate">${last ? (last.fromMe ? "Sen: " : "") + last.text : "Henüz mesaj yok"}</div>
          </div>
          <div class="text-muted" style="font-size:11px;">${last ? last.time : ""}</div>
        </button>`;
    })
    .join("");
}

function renderConversation() {
  const conv = window.findChatConversation(activeId);
  const header = document.getElementById("conversationHeader");
  header.innerHTML = `
    <img src="${conv.avatar}" class="avatar-sm" alt="">
    <div>
      <div class="fw-bold">${conv.name}</div>
      <div class="text-muted small">${conv.online ? "Çevrimiçi" : "Çevrimdışı"}</div>
    </div>`;

  const thread = document.getElementById("conversationThread");
  thread.innerHTML = conv.messages
    .map(m => `
      <div class="d-flex mb-3 ${m.fromMe ? "justify-content-end" : "justify-content-start"}">
        <div class="px-3 py-2 rounded-4 ${m.fromMe ? "bg-primary text-white" : "bg-body-tertiary"}" style="max-width:70%;">
          <div class="small">${m.text}</div>
          <div class="mt-1 ${m.fromMe ? "text-white-50" : "text-muted"}" style="font-size:10.5px;">${m.time}</div>
        </div>
      </div>`)
    .join("");
  thread.scrollTop = thread.scrollHeight;
}

function selectConversation(id) {
  activeId = id;
  renderList();
  renderConversation();
}

document.getElementById("messageForm").addEventListener("submit", function (e) {
  e.preventDefault();
  const input = document.getElementById("messageInput");
  const text = input.value.trim();
  if (!text) return;
  const conv = window.findChatConversation(activeId);
  conv.messages.push({ fromMe: true, text, time: "Şimdi" });
  input.value = "";
  renderList();
  renderConversation();
  if (window.refreshChatDock) window.refreshChatDock();
});

// Deep-link support: friends.html links here as messages.html?with=Name
const params = new URLSearchParams(window.location.search);
const withName = params.get("with");
if (withName) {
  const conv = window.addChatConversation(withName, params.get("avatar"));
  activeId = conv.id;
}

renderList();
renderConversation();
