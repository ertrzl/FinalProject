// Messenger-style message bar shared by messages.html and the chat dock popups:
// photo, sticker, an emoji picker inside the input, and a thumbs-up that turns into "send" once there is something to send.
//
// createChatComposer(form, { getTarget, onSent, onTyping, compact })
//   getTarget() -> { conversationId } or { receiverId } (or null when nothing is selected)
//   onSent(message) is called after a message went out; onTyping() on keystrokes.

(function () {
  const STICKERS = ["😀", "😂", "🥰", "😍", "😎", "🤩", "😭", "😡", "🥳", "🤯", "😴", "🤔",
    "🙄", "😱", "🤗", "😇", "🤝", "👏", "🙌", "🙏", "💪", "👍", "👎", "✌️",
    "🔥", "✨", "🎉", "❤️", "💔", "💯", "🎂", "🍕", "☕", "🍺", "🐶", "🐱",
    "🦄", "🌈", "⭐", "🚀"];

  window.createChatComposer = function (form, options) {
    const { getTarget, onSent, onTyping, compact = false } = options;
    let attachedFile = null;

    form.classList.add("chat-composer");
    if (compact) form.classList.add("compact-bar");
    form.innerHTML = `
      <div class="chat-attach-preview d-none">
        <img alt="">
        <button type="button" class="btn-close btn-close-white" title="Kaldır"></button>
      </div>
      <div class="chat-composer-row">
        <button type="button" class="chat-tool" data-tool="photo" title="Fotoğraf gönder"><i class="bi bi-image-fill"></i></button>
        <button type="button" class="chat-tool" data-tool="sticker" title="Çıkartma"><i class="bi bi-sticky-fill"></i></button>
        <div class="chat-input-wrap">
          <input type="text" class="form-control rounded-pill chat-input" placeholder="Bir mesaj yaz..." autocomplete="off">
          <button type="button" class="chat-tool chat-emoji-btn" data-tool="emoji" title="Emoji"><i class="bi bi-emoji-smile-fill"></i></button>
        </div>
        <button type="button" class="chat-tool chat-send-btn" title="Beğen"><i class="bi bi-hand-thumbs-up-fill"></i></button>
      </div>
      <div class="chat-popover d-none" data-popover="emoji"></div>
      <div class="chat-popover d-none" data-popover="sticker">
        <div class="chat-sticker-grid">${STICKERS.map(s => `<button type="button" data-sticker="${s}">${s}</button>`).join("")}</div>
      </div>
      <input type="file" class="d-none" accept="image/*">`;

    const input = form.querySelector(".chat-input");
    const sendBtn = form.querySelector(".chat-send-btn");
    const fileInput = form.querySelector("input[type=file]");
    const preview = form.querySelector(".chat-attach-preview");
    const popovers = {
      emoji: form.querySelector('[data-popover="emoji"]'),
      sticker: form.querySelector('[data-popover="sticker"]')
    };

    // The thumbs-up becomes a send arrow as soon as there is text or a photo.
    function refreshSendButton() {
      const hasContent = input.value.trim() !== "" || attachedFile !== null;
      sendBtn.title = hasContent ? "Gönder" : "Beğen";
      sendBtn.innerHTML = `<i class="bi ${hasContent ? "bi-send-fill" : "bi-hand-thumbs-up-fill"}"></i>`;
    }

    function closePopovers() {
      Object.values(popovers).forEach(p => p.classList.add("d-none"));
    }

    function togglePopover(name) {
      const wasOpen = !popovers[name].classList.contains("d-none");
      closePopovers();
      if (wasOpen) return;

      if (name === "emoji" && !popovers.emoji.firstChild) {
        const picker = document.createElement("emoji-picker");
        picker.className = document.documentElement.getAttribute("data-bs-theme") === "dark" ? "dark" : "light";
        picker.classList.add(compact ? "compact-picker" : "full-picker");
        picker.addEventListener("emoji-click", e => insertAtCaret(e.detail.unicode));
        popovers.emoji.appendChild(picker);
      }
      popovers[name].classList.remove("d-none");
    }

    function insertAtCaret(text) {
      const start = input.selectionStart ?? input.value.length;
      const end = input.selectionEnd ?? input.value.length;
      input.setRangeText(text, start, end, "end");
      input.dispatchEvent(new Event("input", { bubbles: true }));
      input.focus();
    }

    function setAttachment(file) {
      attachedFile = file;
      if (preview.dataset.url) URL.revokeObjectURL(preview.dataset.url);
      if (file) {
        preview.dataset.url = URL.createObjectURL(file);
        preview.querySelector("img").src = preview.dataset.url;
      } else {
        delete preview.dataset.url;
        fileInput.value = "";
      }
      preview.classList.toggle("d-none", !file);
      refreshSendButton();
    }

    async function send(sender) {
      const target = getTarget();
      if (!target) return;
      try {
        const sent = await sender(target);
        onSent(sent);
      } catch (err) {
        toast(err.message || "Mesaj gönderilemedi.");
      }
    }

    function sendSticker(emoji) {
      closePopovers();
      return send(target => window.realtime.sendMessage({ ...target, type: "Sticker", text: emoji }));
    }

    function sendImage(file, caption) {
      return send(target => {
        const data = new FormData();
        if (target.conversationId) data.append("conversationId", target.conversationId);
        if (target.receiverId) data.append("receiverId", target.receiverId);
        data.append("image", file);
        if (caption) data.append("text", caption);
        return apiFetchForm("/api/messages/image", { method: "POST", body: data });
      });
    }

    // Sends the typed text and/or the attached photo; does nothing when both are empty.
    function submitContent() {
      closePopovers();
      const text = input.value.trim();

      if (attachedFile) {
        const file = attachedFile;
        setAttachment(null);
        input.value = "";
        refreshSendButton();
        sendImage(file, text);
      } else if (text) {
        input.value = "";
        refreshSendButton();
        send(target => window.realtime.sendMessage({ ...target, text }));
      }
    }

    // The button acts as "like" only when clicked on an empty bar; pressing Enter on an empty bar sends nothing.
    sendBtn.addEventListener("click", () => {
      if (input.value.trim() === "" && attachedFile === null) sendSticker("👍");
      else submitContent();
    });

    input.addEventListener("keydown", e => {
      if (e.key !== "Enter") return;
      e.preventDefault();
      submitContent();
    });

    form.addEventListener("submit", e => e.preventDefault());

    input.addEventListener("input", () => {
      refreshSendButton();
      onTyping?.();
    });

    form.querySelector('[data-tool="photo"]').addEventListener("click", () => fileInput.click());
    fileInput.addEventListener("change", () => {
      const file = fileInput.files[0];
      if (file) setAttachment(file);
    });
    preview.querySelector("button").addEventListener("click", () => setAttachment(null));

    form.querySelector('[data-tool="emoji"]').addEventListener("click", () => togglePopover("emoji"));
    form.querySelector('[data-tool="sticker"]').addEventListener("click", () => togglePopover("sticker"));
    form.querySelectorAll("[data-sticker]").forEach(btn =>
      btn.addEventListener("click", () => sendSticker(btn.dataset.sticker)));

    // Click outside / Escape closes whichever picker is open.
    document.addEventListener("click", e => {
      if (!form.contains(e.target)) closePopovers();
    });
    form.addEventListener("keydown", e => {
      if (e.key === "Escape") closePopovers();
    });

    return { input, focus: () => input.focus() };
  };
})();
