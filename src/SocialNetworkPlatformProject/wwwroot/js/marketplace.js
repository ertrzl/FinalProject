// Marketplace page — static seed data, no backend. Visual only, resets on reload.

const categories = ["Tümü", "Elektronik", "Ev Eşyası", "Giyim", "Kitap", "Spor", "Araç"];
let activeCategory = "Tümü";

const listings = [
  {
    id: 1, title: "Dağ Bisikleti", price: 4500, category: "Spor", location: "İstanbul, Türkiye",
    image: "https://picsum.photos/seed/market1/500/400",
    description: "Az kullanılmış, 21 vitesli dağ bisikleti. Bakımlı ve sorunsuz.",
    seller: { name: "Daniel Bale", avatar: "https://i.pravatar.cc/100?img=15" },
  },
  {
    id: 2, title: "Kablosuz Kulaklık", price: 890, category: "Elektronik", location: "Ankara, Türkiye",
    image: "https://picsum.photos/seed/market2/500/400",
    description: "Kutusunda, garantisi devam ediyor. Gürültü engelleme özellikli.",
    seller: { name: "Winnie Haley", avatar: "https://i.pravatar.cc/100?img=32" },
  },
  {
    id: 3, title: "3'lü Koltuk Takımı", price: 6200, category: "Ev Eşyası", location: "İzmir, Türkiye",
    image: "https://picsum.photos/seed/market3/500/400",
    description: "Ev taşınması nedeniyle satılık, çok temiz kullanıldı.",
    seller: { name: "Lana Rose", avatar: "https://i.pravatar.cc/100?img=47" },
  },
  {
    id: 4, title: "Deri Ceket (M Beden)", price: 750, category: "Giyim", location: "İstanbul, Türkiye",
    image: "https://picsum.photos/seed/market4/500/400",
    description: "Bir kez giyildi, mükemmel durumda hakiki deri ceket.",
    seller: { name: "Diana Prince", avatar: "https://i.pravatar.cc/100?img=60" },
  },
  {
    id: 5, title: "Roman Seti (12 Kitap)", price: 320, category: "Kitap", location: "Bursa, Türkiye",
    image: "https://picsum.photos/seed/market5/500/400",
    description: "Klasik roman seti, hepsi orijinal ve temiz sayfalı.",
    seller: { name: "Jane Doe", avatar: "https://i.pravatar.cc/100?img=25" },
  },
  {
    id: 6, title: "Elektrikli Scooter", price: 8900, category: "Araç", location: "Antalya, Türkiye",
    image: "https://picsum.photos/seed/market6/500/400",
    description: "1 yıllık, bataryası hâlâ %95 kapasitede. Şarj aleti dahil.",
    seller: { name: "Marcus Lee", avatar: "https://i.pravatar.cc/100?img=51" },
  },
];

let nextId = 100;

function formatPrice(n) {
  return n.toLocaleString("tr-TR") + " ₺";
}

function renderCategories() {
  const wrap = document.getElementById("categoryList");
  wrap.innerHTML = categories
    .map(cat => `
      <button type="button" class="btn btn-sm text-start rounded-pill ${cat === activeCategory ? "btn-primary" : "btn-light"}" onclick="setCategory('${cat}')">${cat}</button>`)
    .join("");
}

function setCategory(cat) {
  activeCategory = cat;
  renderCategories();
  renderListings();
}

function renderListings() {
  const query = document.getElementById("marketSearch").value.trim().toLowerCase();
  const filtered = listings.filter(item => {
    const matchesCategory = activeCategory === "Tümü" || item.category === activeCategory;
    const matchesQuery = !query || item.title.toLowerCase().includes(query);
    return matchesCategory && matchesQuery;
  });

  document.getElementById("resultCount").textContent = filtered.length + " ilan";

  const grid = document.getElementById("listingsGrid");
  grid.innerHTML = filtered
    .map(item => `
      <div class="col-6 col-md-4">
        <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100" role="button" onclick="openListing(${item.id})">
          <img src="${item.image}" class="w-100" style="height:140px;object-fit:cover;" alt="">
          <div class="p-2">
            <div class="fw-bold text-primary">${formatPrice(item.price)}</div>
            <div class="small text-truncate">${item.title}</div>
            <div class="text-muted small text-truncate"><i class="bi bi-geo-alt"></i> ${item.location}</div>
          </div>
        </div>
      </div>`)
    .join("") || `<div class="col-12 text-center text-muted py-5">Sonuç bulunamadı.</div>`;
}

function openListing(id) {
  const item = listings.find(l => l.id === id);
  if (!item) return;
  document.getElementById("listingModalTitle").textContent = item.title;
  document.getElementById("listingModalImage").src = item.image;
  document.getElementById("listingModalPrice").textContent = formatPrice(item.price);
  document.getElementById("listingModalMeta").innerHTML = `<i class="bi bi-geo-alt"></i> ${item.location} · ${item.category}`;
  document.getElementById("listingModalDescription").textContent = item.description;
  document.getElementById("listingModalSellerAvatar").src = item.seller.avatar;
  document.getElementById("listingModalSellerName").textContent = item.seller.name;
  document.getElementById("listingModalMessageLink").href = "messages.html?with=" + encodeURIComponent(item.seller.name) + "&avatar=" + encodeURIComponent(item.seller.avatar);

  const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById("listingModal"));
  modal.show();
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

function publishListing() {
  const title = document.getElementById("sellTitle").value.trim();
  const price = parseFloat(document.getElementById("sellPrice").value) || 0;
  const category = document.getElementById("sellCategory").value;
  const location = document.getElementById("sellLocation").value.trim() || "Belirtilmedi";
  const description = document.getElementById("sellDescription").value.trim();
  const previewImg = document.getElementById("sellPreviewImg");
  const hasImage = previewImg.src && !document.getElementById("sellPreviewWrap").classList.contains("d-none");

  if (!title) {
    document.getElementById("sellTitle").classList.add("is-invalid");
    setTimeout(() => document.getElementById("sellTitle").classList.remove("is-invalid"), 1200);
    return;
  }

  listings.unshift({
    id: nextId++,
    title, price, category, location, description,
    image: hasImage ? previewImg.src : "https://picsum.photos/seed/market" + nextId + "/500/400",
    seller: { name: "Ada Lovelace", avatar: "https://i.pravatar.cc/100?img=45" },
  });

  bootstrap.Modal.getInstance(document.getElementById("sellModal")).hide();
  document.getElementById("sellTitle").value = "";
  document.getElementById("sellPrice").value = "";
  document.getElementById("sellLocation").value = "";
  document.getElementById("sellDescription").value = "";
  document.getElementById("sellPreviewWrap").classList.add("d-none");

  activeCategory = "Tümü";
  renderCategories();
  renderListings();
  toast("İlanın yayınlandı!");
}

renderCategories();
renderListings();
