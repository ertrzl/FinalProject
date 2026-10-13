// The look of the pages ("design"). Loaded in the <head> of every page, before the stylesheets are painted, so there is no flash.
//
// The pages are built once; a design is a stylesheet that is switched on by the attribute  <html data-design="...">:
//   inbox     css/inbox.css  every rule starts with  html[data-design="inbox"]  (the default)
//   classic   no extra rules: css/style.css alone, the look the site had before the inbox design
// The choice is kept in this browser and is changed in Ayarlar > Görünüm. A new design is a new stylesheet scoped the same way,
// one line in DESIGNS and one in the Görünüm list of the settings page.
// Parts of a page that exist in one design only carry  class="inbox-only"  (hidden in the other designs) or  class="classic-only".
// The menu is drawn differently per design (js/navbar.js), so a switch reloads the page.
//
//   getDesign() / setDesign("classic")   the chosen design / switch (the page reloads)

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
  } catch (e) { /* storage may be blocked: the default design is used */ }
  return DEFAULT_DESIGN;
}

function setDesign(code) {
  try { localStorage.setItem(DESIGN_STORAGE_KEY, code); } catch (e) {}
  window.location.reload();
}

document.documentElement.setAttribute("data-design", getDesign());
