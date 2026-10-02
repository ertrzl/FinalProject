// Turns a plain text input into a city/country autocomplete backed by OpenStreetMap's free Nominatim search
// (no API key). Usage: enableLocationAutocomplete(document.getElementById("editLocation")).

function enableLocationAutocomplete(input) {
  const wrap = document.createElement("div");
  wrap.className = "position-relative";
  input.parentNode.insertBefore(wrap, input);
  wrap.appendChild(input);

  const list = document.createElement("div");
  list.className = "location-suggestions d-none";
  wrap.appendChild(list);

  let debounceTimer = null;
  let controller = null;
  let items = [];
  let activeIndex = -1;

  function hide() {
    list.classList.add("d-none");
    list.innerHTML = "";
    items = [];
    activeIndex = -1;
  }

  function renderItems() {
    list.innerHTML = items.map((item, i) =>
      `<button type="button" class="location-suggestion${i === activeIndex ? " active" : ""}" data-index="${i}">
        <i class="bi bi-geo-alt me-2 text-muted"></i>${escapeHtml(item.label)}
      </button>`
    ).join("");
    list.classList.toggle("d-none", items.length === 0);
  }

  function choose(item) {
    input.value = item.label;
    hide();
    // Setting .value programmatically fires nothing, so pages that react to the field (e.g. a live filter)
    // listen for "change". Not "input": that would restart the search and reopen the list.
    input.dispatchEvent(new Event("change", { bubbles: true }));
  }

  async function search(term) {
    controller?.abort();
    controller = new AbortController();
    try {
      const url = `https://nominatim.openstreetmap.org/search?format=jsonv2&addressdetails=1&limit=6&accept-language=tr&q=${encodeURIComponent(term)}`;
      const response = await fetch(url, { signal: controller.signal, headers: { Accept: "application/json" } });
      if (!response.ok) throw new Error("lookup failed");
      const results = await response.json();

      items = results
        .filter(r => r.address && (r.address.city || r.address.town || r.address.village || r.address.state || r.address.country))
        .map(r => {
          const a = r.address;
          const place = a.city || a.town || a.village || a.state;
          const label = place && a.country && place !== a.country ? `${place}, ${a.country}` : (a.country || r.display_name);
          return { label };
        })
        .filter((r, i, arr) => arr.findIndex(x => x.label === r.label) === i);

      activeIndex = -1;
      renderItems();
    } catch (err) {
      if (err.name !== "AbortError") hide();
    }
  }

  input.addEventListener("input", () => {
    const term = input.value.trim();
    clearTimeout(debounceTimer);
    if (term.length < 2) { hide(); return; }
    debounceTimer = setTimeout(() => search(term), 400);
  });

  input.addEventListener("keydown", e => {
    if (list.classList.contains("d-none")) return;

    if (e.key === "ArrowDown") {
      e.preventDefault();
      activeIndex = Math.min(activeIndex + 1, items.length - 1);
      renderItems();
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      activeIndex = Math.max(activeIndex - 1, 0);
      renderItems();
    } else if (e.key === "Enter" && activeIndex >= 0) {
      e.preventDefault();
      choose(items[activeIndex]);
    } else if (e.key === "Escape") {
      hide();
    }
  });

  list.addEventListener("mousedown", e => {
    const btn = e.target.closest("[data-index]");
    if (btn) choose(items[Number(btn.dataset.index)]);
  });

  document.addEventListener("click", e => {
    if (!wrap.contains(e.target)) hide();
  });
}
