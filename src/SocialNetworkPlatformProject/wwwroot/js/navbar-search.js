// Navbar live search suggestions — every authenticated page includes this. F7.
// Replaces the old hardcoded-name-list version that never called the backend.

function initSearchSuggestions() {
  document.querySelectorAll(".nav-search-wrap input[name='q']").forEach(input => {
    const wrap = input.closest(".nav-search-wrap");
    let box = wrap.querySelector(".search-suggestions");
    if (!box) {
      box = document.createElement("div");
      box.className = "search-suggestions d-none";
      wrap.appendChild(box);
    }

    let debounceTimer = null;

    input.addEventListener("input", () => {
      const term = input.value.trim();
      clearTimeout(debounceTimer);

      if (!term) {
        box.classList.add("d-none");
        box.innerHTML = "";
        return;
      }

      debounceTimer = setTimeout(async () => {
        try {
          const result = await apiFetch(`/api/users/search?term=${encodeURIComponent(term)}&page=1&pageSize=5`);
          if (!result.items.length) {
            box.classList.add("d-none");
            box.innerHTML = "";
            return;
          }
          box.innerHTML = result.items
            .map(u => `<button type="button" onclick="window.location.href='profile.html?id=${u.id}'">
              <img src="${u.avatarUrl || DEFAULT_AVATAR}" class="avatar-xs" alt="">
              <span class="small fw-semibold">${escapeHtml(u.fullName)}</span>
            </button>`)
            .join("");
          box.classList.remove("d-none");
        } catch (err) {
          box.classList.add("d-none");
        }
      }, 250);
    });

    document.addEventListener("click", e => {
      if (!wrap.contains(e.target)) box.classList.add("d-none");
    });
  });
}

document.addEventListener("DOMContentLoaded", initSearchSuggestions);
