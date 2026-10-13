// Keyboard shortcuts of the inbox design. Every signed-in page loads this after navbar.js and app.js; with the classic
// design nothing is bound. A key is ignored while a box is being typed in, or when Ctrl / Alt / Cmd is held.
//
//   /           the search box
//   g, then a   go to a page: a Ana Sayfa, k Keşfet, r Arkadaşlar, m Mesajlar, b Bildirimler, g Gruplar,
//               e Etkinlikler, p Pazar, s Kaydedilenler, t Ayarlar   (the letters are the ones of the menu, js/navbar.js)
//   n  (c)      write: the post box of the page, or the "create" button of a page (a group, an event, a listing)
//   j / k       next / previous post of the list      Enter  open it
//   l           like the post      r  answer (comments)      e  save it
//   ?           this list

if (getDesign() === "inbox") {
  const SHORTCUT_LIST = [
    ["/", "Ara"],
    ["g  a", "Ana Sayfa"], ["g  k", "Keşfet"], ["g  r", "Arkadaşlar"], ["g  m", "Mesajlar"], ["g  b", "Bildirimler"],
    ["g  g", "Gruplar"], ["g  e", "Etkinlikler"], ["g  p", "Pazar"], ["g  s", "Kaydedilenler"], ["g  t", "Ayarlar"],
    ["c", "Yaz / oluştur"], ["j  k", "Gönderiler arasında gez"], ["↵", "Gönderiyi aç"], ["l", "Beğen"], ["r", "Yanıtla"], ["e", "Kaydet"], ["?", "Bu liste"]
  ];

  let waitingForPage = false;   // "g" was pressed, the next letter picks the page
  let waitTimer = null;
  let selected = null;          // the post that j / k stopped on

  const isTyping = el => el && (el.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(el.tagName));
  const visible = el => el && el.offsetParent !== null;

  function posts() {
    return [...document.querySelectorAll(".card[data-post-id]")].filter(visible);
  }

  function selectPost(post) {
    if (selected) selected.classList.remove("kb-selected");
    selected = post;
    if (!post) return;
    post.classList.add("kb-selected");
    post.scrollIntoView({ block: "center", behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth" });
  }

  function movePost(step) {
    const list = posts();
    if (!list.length) return;
    const at = selected ? list.indexOf(selected) : -1;
    selectPost(list[Math.min(Math.max(at + step, 0), list.length - 1)]);
  }

  function focusSearch() {
    const box = [...document.querySelectorAll("[data-search-input]")].find(visible);
    if (box) { box.focus(); box.select(); }
  }

  // "Yaz": the post box if the page has one, else the main button of the page's heading.
  function startWriting() {
    const composer = document.getElementById("composerText");
    if (visible(composer)) { composer.focus(); return; }
    const button = document.querySelector(".page-head .btn.btn-primary, .page-head .btn-primary");
    if (visible(button)) button.click();
  }

  function showShortcuts() {
    const rows = SHORTCUT_LIST.map(([keys, what]) => `
      <div class="d-flex justify-content-between align-items-center py-1">
        <span>${what}</span><span>${keys.split("  ").map(k => `<span class="kbd">${k}</span>`).join(" ")}</span>
      </div>`).join("");
    openSiteDialog(`
      <div class="fw-bold fs-5 mb-3">Klavye kısayolları</div>
      <div class="mb-3">${rows}</div>
      <div class="d-flex justify-content-end"><button type="button" class="btn btn-primary rounded-pill px-4" data-answer="close">Kapat</button></div>`,
      (box, close) => {
        box.querySelector('[data-answer="close"]').addEventListener("click", close);
        box.addEventListener("dismiss", close);
        box.querySelector('[data-answer="close"]').focus();
      });
  }

  document.addEventListener("keydown", event => {
    if (event.ctrlKey || event.altKey || event.metaKey) return;
    if (isTyping(event.target) || document.querySelector(".confirm-overlay, .modal.show, .offcanvas.show")) return;

    const key = event.key;

    if (waitingForPage) {
      waitingForPage = false;
      clearTimeout(waitTimer);
      const link = document.querySelector(`[data-shortcut="${key.toLowerCase()}"]`);
      if (link) { event.preventDefault(); window.location.href = link.getAttribute("href"); }
      return;
    }

    if (key === "/") { event.preventDefault(); focusSearch(); }
    else if (key === "g") { waitingForPage = true; waitTimer = setTimeout(() => { waitingForPage = false; }, 1200); }
    else if (key === "c" || key === "n") { event.preventDefault(); startWriting(); }
    else if (key === "j") movePost(1);
    else if (key === "k") movePost(-1);
    else if (key === "?") showShortcuts();
    else if (selected && key === "Enter") window.location.href = `post.html?id=${selected.dataset.postId}`;
    else if (selected && key === "l") selected.querySelector(".like-btn")?.click();
    else if (selected && key === "r") {
      selected.querySelector('[data-bs-target^="#comments-"]')?.click();
      setTimeout(() => selected?.querySelector(".comments-collapse input[type=text]")?.focus(), 350);
      event.preventDefault();
    }
    else if (selected && key === "e") selected.querySelector('[onclick^="toggleSavePost"]')?.click();
  });
}
