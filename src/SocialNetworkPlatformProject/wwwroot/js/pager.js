// "Daha fazla yükle": a list that shows one page of what the server has, with a button under it for the next page.
// The server says whether there is more (result.hasMore), so the button shows up only while there is.
//
//   const pager = createPager({
//     list: "feedList",                                    // id of the element the items are drawn into
//     url: page => `/api/posts/feed?page=${page}&pageSize=10`,
//     render: post => postCardHtml(post),                  // the html of one item
//     shown: post => !!document.querySelector(...),        // optional: true when an item is already on the page
//     emptyHtml: "<div>Henüz gönderi yok.</div>",          // shown when there is nothing at all
//     onLoaded: (items, firstPage) => { ... }              // optional: called after every page
//   });
//   pager.reload();                                        // start again from the first page
//
// `shown` matters because pages are counted from the newest item: when something new arrives live while the list
// is open, the item that was last on one page is first on the next, and must not be drawn twice.
function createPager({ list, url, render, shown = () => false, emptyHtml = "", onLoaded = () => {} }) {
  const buttonId = `${list}-more`;
  document.getElementById(buttonId)?.remove(); // a new pager for the same list (a new search) replaces the old button

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

      if (firstPage) target.innerHTML = ""; // replaces "Yükleniyor..." or whatever was there before a reload

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
