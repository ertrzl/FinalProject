const DESIGN_STORAGE_KEY = "socialnet-design";
const DEFAULT_DESIGN = "inbox";
const DESIGNS = [
  { code: "inbox", name: "Gelen kutusu" },
  { code: "classic", name: "Klasik" }
];

function getDesign() {
  try {
    const saved = localStorage.getItem(DESIGN_STORAGE_KEY);
    if (DESIGNS.some(design => design.code === saved)) return saved;
  } catch (e) { }
  return DEFAULT_DESIGN;
}

function setDesign(code) {
  try { localStorage.setItem(DESIGN_STORAGE_KEY, code); } catch (e) {}
  window.location.reload();
}

document.documentElement.setAttribute("data-design", getDesign());
