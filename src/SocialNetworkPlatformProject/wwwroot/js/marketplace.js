// Marketplace page — real listings wired to the backend.

requireAuth();

const categories = ["Tümü", "Elektronik", "Ev Eşyası", "Giyim", "Kitap", "Spor", "Araç"];
let activeCategory = "Tümü";
let currentListings = [];

function formatPrice(n) {
  return n.toLocaleString("tr-TR") + " ₺";
}

function renderCategories() {
  const wrap = document.getElementById("categoryList");
  wrap.innerHTML = categories
    .map(cat => `<button type="button" class="btn btn-sm text-start rounded-pill ${cat === activeCategory ? "btn-primary" : "btn-light"}" onclick="setCategory('${cat}')">${cat}</button>`)
    .join("");
}

function setCategory(cat) {
  activeCategory = cat;
  renderCategories();
  renderListings();
}

async function renderListings() {
  const grid = document.getElementById("listingsGrid");
  const query = document.getElementById("marketSearch").value.trim();
  const category = activeCategory === "Tümü" ? "" : activeCategory;

  try {
    const params = new URLSearchParams({ page: "1", pageSize: "30" });
    if (category) params.set("category", category);
    if (query) params.set("search", query);

    const result = await apiFetch(`/api/marketplace?${params.toString()}`);
    currentListings = result.items;
    document.getElementById("resultCount").textContent = result.totalCount + " ilan";

    grid.innerHTML = result.items.length
      ? result.items.map(item => `
          <div class="col-6 col-md-4">
            <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100" role="button" onclick="openListing('${item.id}')">
              <img src="${item.imageUrl || "https://picsum.photos/seed/" + item.id + "/500/400"}" class="w-100" style="height:140px;object-fit:cover;" alt="">
              <div class="p-2">
                <div class="fw-bold text-primary">${formatPrice(item.price)}</div>
                <div class="small text-truncate">${escapeHtml(item.title)}</div>
                <div class="text-muted small text-truncate"><i class="bi bi-geo-alt"></i> ${escapeHtml(item.location || "Belirtilmedi")}</div>
              </div>
            </div>
          </div>`)
          .join("")
      : `<div class="col-12 text-center text-muted py-5">Sonuç bulunamadı.</div>`;
  } catch (err) {
    grid.innerHTML = `<div class="col-12"><div class="alert alert-danger small">${escapeHtml(err.message)}</div></div>`;
  }
}

function openListing(id) {
  const item = currentListings.find(l => l.id === id);
  if (!item) return;

  document.getElementById("listingModalTitle").textContent = item.title;
  document.getElementById("listingModalImage").src = item.imageUrl || "https://picsum.photos/seed/" + item.id + "/500/400";
  document.getElementById("listingModalPrice").textContent = formatPrice(item.price);
  document.getElementById("listingModalMeta").innerHTML = `<i class="bi bi-geo-alt"></i> ${escapeHtml(item.location || "Belirtilmedi")} · ${escapeHtml(item.category)}`;
  document.getElementById("listingModalDescription").textContent = item.description || "";
  document.getElementById("listingModalSellerAvatar").src = item.sellerAvatarUrl || DEFAULT_AVATAR;
  document.getElementById("listingModalSellerName").textContent = item.sellerName;

  const session = getSession();
  const isMine = session && item.sellerId === session.userId;
  document.getElementById("listingModalMessageLink").classList.toggle("d-none", isMine);
  document.getElementById("listingModalMessageLink").href = `messages.html?userId=${item.sellerId}`;
  const deleteBtn = document.getElementById("listingModalDeleteBtn");
  deleteBtn.classList.toggle("d-none", !isMine);
  deleteBtn.dataset.listingId = item.id;

  bootstrap.Modal.getOrCreateInstance(document.getElementById("listingModal")).show();
}

async function deleteListing() {
  const id = document.getElementById("listingModalDeleteBtn").dataset.listingId;
  try {
    await apiFetch(`/api/marketplace/${id}`, { method: "DELETE" });
    bootstrap.Modal.getInstance(document.getElementById("listingModal"))?.hide();
    toast("İlan kaldırıldı.");
    await renderListings();
  } catch (err) {
    toast(err.message || "İlan kaldırılamadı.");
  }
}

// ---- Sell form ----
function previewSellImage(input) {
  const wrap = document.getElementById("sellPreviewWrap");
  const img = document.getElementById("sellPreviewImg");
  if (input.files && input.files[0]) {
    img.src = URL.createObjectURL(input.files[0]);
    wrap.classList.remove("d-none");
  }
}

async function publishListing() {
  const title = document.getElementById("sellTitle").value.trim();
  if (!title) {
    document.getElementById("sellTitle").classList.add("is-invalid");
    setTimeout(() => document.getElementById("sellTitle").classList.remove("is-invalid"), 1200);
    return;
  }

  const errorBox = document.getElementById("sellError");
  errorBox.classList.add("d-none");

  try {
    const formData = new FormData();
    formData.append("title", title);
    formData.append("price", document.getElementById("sellPrice").value || "0");
    formData.append("category", document.getElementById("sellCategory").value);
    formData.append("location", document.getElementById("sellLocation").value.trim());
    formData.append("description", document.getElementById("sellDescription").value.trim());

    const image = document.getElementById("sellImageInput").files[0];
    if (image) formData.append("image", image);

    await apiFetchForm("/api/marketplace", { method: "POST", body: formData });

    bootstrap.Modal.getInstance(document.getElementById("sellModal"))?.hide();
    document.getElementById("sellTitle").value = "";
    document.getElementById("sellPrice").value = "";
    document.getElementById("sellLocation").value = "";
    document.getElementById("sellDescription").value = "";
    document.getElementById("sellPreviewWrap").classList.add("d-none");

    activeCategory = "Tümü";
    renderCategories();
    toast("İlanın yayınlandı!");
    await renderListings();
  } catch (err) {
    errorBox.textContent = err.message || "İlan yayınlanamadı.";
    errorBox.classList.remove("d-none");
  }
}

renderCategories();
renderListings();
