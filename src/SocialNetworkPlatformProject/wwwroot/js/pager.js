function createPager({ list, url, render, shown = () => false, emptyHtml = "", onLoaded = () => {} }) {
  const buttonId = `${list}-more`;
  document.getElementById(buttonId)?.remove();

  let page = 0;
  let busy = false;

  function moreButton() {
    let wrap = document.getElementById(buttonId);
    if (!wrap) {
      wrap = document.createElement("div");
      wrap.id = buttonId;
      wrap.className = "text-center my-3 d-none";
      wrap.innerHTML = `<button type="button" class="btn btn-light border rounded-pill px-4">Daha fazla yükle</button>`;
      wrap.querySelector("button").addEventListener("click", loadNext);
      document.getElementById(list).insertAdjacentElement("afterend", wrap);
    }
    return wrap;
  }

  async function loadNext() {
    if (busy) return;
    busy = true;

    const target = document.getElementById(list);
    const wrap = moreButton();
    const button = wrap.querySelector("button");
    button.disabled = true;

    try {
      const result = await apiFetch(url(page + 1));
      const firstPage = page === 0;
      page += 1;

      if (firstPage) target.innerHTML = "";

      if (firstPage && result.items.length === 0) {
        target.innerHTML = emptyHtml;
      } else {
        target.insertAdjacentHTML("beforeend", result.items.filter(item => !shown(item)).map(render).join(""));
      }

      wrap.classList.toggle("d-none", !result.hasMore);
      onLoaded(result.items, firstPage);
    } catch (err) {
      if (page === 0) target.innerHTML = `<div class="alert alert-danger small">${escapeHtml(err.message)}</div>`;
      else toast(err.message || "Yüklenemedi.");
    } finally {
      button.disabled = false;
      busy = false;
    }
  }

  return {
    loadNext,
    reload() {
      page = 0;
      moreButton().classList.add("d-none");
      return loadNext();
    }
  };
}
