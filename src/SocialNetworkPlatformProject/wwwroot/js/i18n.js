// Languages of the pages. Loaded first on every page (before api.js).
//
//   t("key", arg0, arg1)         the text for the key in the chosen language ("{0}", "{1}" are filled with the arguments)
//   data-i18n="key"              in the HTML: the element's text is replaced (also data-i18n-placeholder / -title)
//   getLang() / setLang("en")    the chosen language (kept in this browser) / switch and reload
//
// The texts live in locales/<code>.js, the list of languages in locales/languages.js (see there to add a language).
// The chosen language is also sent with every request to the server (api.js), whose messages follow it.

const LANGUAGE_STORAGE_KEY = "socialnet-lang";
const DEFAULT_LANGUAGE = "tr";

function getLang() {
  try {
    const saved = localStorage.getItem(LANGUAGE_STORAGE_KEY);
    if (saved && /^[a-z]{2,3}$/.test(saved)) return saved;
  } catch (e) { /* storage may be blocked: the default language is used */ }
  return DEFAULT_LANGUAGE;
}

function setLang(code) {
  try { localStorage.setItem(LANGUAGE_STORAGE_KEY, code); } catch (e) {}
  window.location.reload();
}

// The language files are loaded while the page is being read, so t() works from the very first script on.
// (A file that does not exist is simply skipped: its texts fall back to the default language.)
(function loadLanguageFiles() {
  const lang = getLang();
  document.write('<script src="/locales/languages.js"><\/script>');
  document.write('<script src="/locales/' + DEFAULT_LANGUAGE + '.js"><\/script>');
  if (lang !== DEFAULT_LANGUAGE) document.write('<script src="/locales/' + lang + '.js"><\/script>');
  document.documentElement.lang = lang;
})();

function t(key, ...args) {
  const locales = window.LOCALES || {};
  let text = (locales[getLang()] || {})[key] ?? (locales[DEFAULT_LANGUAGE] || {})[key] ?? key;
  args.forEach((arg, i) => { text = text.split(`{${i}}`).join(arg); });
  return text;
}

// Fills the elements of the HTML that carry data-i18n* attributes.
function translatePage(root = document) {
  root.querySelectorAll("[data-i18n]").forEach(el => { el.textContent = t(el.dataset.i18n); });
  root.querySelectorAll("[data-i18n-placeholder]").forEach(el => { el.placeholder = t(el.dataset.i18nPlaceholder); });
  root.querySelectorAll("[data-i18n-title]").forEach(el => { el.title = t(el.dataset.i18nTitle); });
}

// The language picker: a small select listing locales/languages.js.
function languagePickerHtml(extraClass = "") {
  const current = getLang();
  const options = (window.LANGUAGES || []).map(l => `<option value="${l.code}" ${l.code === current ? "selected" : ""}>${l.name}</option>`).join("");
  return `<select class="form-select form-select-sm lang-select ${extraClass}" aria-label="${t("language.label")}" title="${t("language.label")}" onchange="setLang(this.value)">${options}</select>`;
}

document.addEventListener("DOMContentLoaded", () => {
  translatePage();

  // The sign-in / register pages have no menu: the picker floats in the corner.
  if (document.querySelector(".auth-gradient")) {
    document.body.insertAdjacentHTML("beforeend", `<div class="lang-float">${languagePickerHtml()}</div>`);
  }
});
