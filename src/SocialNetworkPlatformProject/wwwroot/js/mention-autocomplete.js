// "@" suggestions for every text box marked data-mention (the post writer, the comment boxes, the edit dialog).
// Type "@" and a few letters: the people whose name or user name matches show up under the box; Enter, Tab or a click
// puts "@username " into the text. This is only typing help: which names are real people, who is linked and who gets
// a notification is decided by the server when the text is saved.

(function () {
  const MIN_LETTERS = 1;
  const SEARCH_DELAY = 220;

  let menu = null;      // the floating list
  let box = null;       // the text box it belongs to
  let tag = null;       // { query, start }: the "@letters" being typed, and where its "@" is
  let people = [];
  let active = 0;
  let timer = null;
  let searchNo = 0;

  const myId = () => getSession()?.userId;

  // The "@letters" the caret is right behind, if any ("@" not glued to a word, so e-mail addresses do not count).
  function tagAtCaret(el) {
    const before = el.value.slice(0, el.selectionStart);
    const match = /(^|[^\p{L}0-9_.@])@([a-zA-Z0-9._]{0,50})$/u.exec(before);
    return match ? { query: match[2], start: el.selectionStart - match[2].length - 1 } : null;
  }

  function closeMenu() {
    clearTimeout(timer);
    searchNo++;
    menu?.remove();
    menu = null;
    people = [];
    tag = null;
  }

  function drawMenu() {
    if (!people.length) { menu?.remove(); menu = null; return; }

    if (!menu) {
      menu = document.createElement("div");
      menu.className = "mention-menu";
      menu.addEventListener("mousedown", event => {
        event.preventDefault(); // keep the focus in the text box
        const row = event.target.closest("[data-index]");
        if (row) choose(Number(row.dataset.index));
      });
      document.body.appendChild(menu);
    }

    menu.innerHTML = people.map((p, i) => `
      <button type="button" data-index="${i}" class="${i === active ? "active" : ""}">
        <img src="${p.avatarUrl || DEFAULT_AVATAR}" class="avatar-xs" alt="">
        <span class="text-truncate"><b>${escapeHtml(p.fullName)}</b> <span class="text-muted">@${escapeHtml(p.userName)}</span></span>
      </button>`).join("");

    const rect = box.getBoundingClientRect();
    menu.style.left = `${Math.max(8, rect.left)}px`;
    menu.style.top = `${rect.bottom + 4}px`;
    menu.style.width = `${Math.min(Math.max(rect.width, 240), 340)}px`;
  }

  async function search(query, forTag) {
    const myNo = ++searchNo;
    try {
      const result = await apiFetch(`/api/users/search?term=${encodeURIComponent(query)}&page=1&pageSize=6`);
      if (myNo !== searchNo || tag !== forTag) return; // the person kept typing: this answer is old
      people = result.items.filter(p => p.id !== myId());
      active = 0;
      drawMenu();
    } catch (err) {
      closeMenu();
    }
  }

  function choose(index) {
    const person = people[index];
    if (!person || !box || !tag) return;

    const caret = box.selectionStart;
    const insert = `@${person.userName} `;
    box.value = box.value.slice(0, tag.start) + insert + box.value.slice(caret);
    box.selectionStart = box.selectionEnd = tag.start + insert.length;
    closeMenu();
    box.focus();
    box.dispatchEvent(new Event("input", { bubbles: true }));
  }

  document.addEventListener("input", event => {
    const el = event.target;
    if (!el.matches?.("[data-mention]")) return;

    const found = tagAtCaret(el);
    if (!found || found.query.length < MIN_LETTERS) { closeMenu(); return; }

    box = el;
    tag = found;
    clearTimeout(timer);
    timer = setTimeout(() => search(found.query, found), SEARCH_DELAY);
  });

  // While the list is open the arrow keys, Enter, Tab and Escape belong to it (Enter must not send the comment).
  document.addEventListener("keydown", event => {
    if (!menu || document.activeElement !== box) return;

    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      active = (active + (event.key === "ArrowDown" ? 1 : people.length - 1)) % people.length;
      drawMenu();
    } else if (event.key === "Enter" || event.key === "Tab") {
      event.preventDefault();
      choose(active);
    } else if (event.key === "Escape") {
      event.preventDefault();
      closeMenu();
    }
  }, true);

  document.addEventListener("focusout", event => {
    if (event.target === box) setTimeout(closeMenu, 120);
  });
  window.addEventListener("scroll", closeMenu, true);
})();
