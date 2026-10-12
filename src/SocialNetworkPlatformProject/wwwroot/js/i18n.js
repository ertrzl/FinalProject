// Languages of the pages. Loaded first on every page (before api.js).
//
// The texts of the pages are written in Turkish (the default language), in the HTML and in the scripts. For another
// language the page is translated as it is shown:
//   locales/<code>.js   LOCALES.<code> = { "key": "text" }      texts a script asks for by key:   i18n("key", arg0, ...)
//                       PHRASES.<code> = { "Türkçe": "Text" }   Turkish text -> text in that language
// Everything that appears on the page (text, placeholder / title / alt / aria-label, the question boxes of
// confirmDialog(), the page title) is looked up in PHRASES. The pages never use the browser's own confirm() / alert()
// / prompt() boxes: those are outside the page and could not be translated or styled. A phrase may contain {0}, {1}... for the values the page fills in
// ("{0} dakika önce" -> "{0} minutes ago"). A text with no translation stays Turkish.
// Where the word order of a sentence differs between languages (the notification sentences), the script asks by key.
//
//   getLang() / setLang("en")   the chosen language (kept in this browser; chosen in Ayarlar) / switch and reload
//   localeTag()                 "tr-TR", "en-US"... for dates and numbers (toLocaleDateString)
//   data-no-i18n                on an element: its content is never translated (somebody's own text)
//
// The chosen language is also sent with every request to the server (api.js), whose messages follow it.
// Adding a language: see locales/languages.js.

const LANGUAGE_STORAGE_KEY = "socialnet-lang";
const DEFAULT_LANGUAGE = "tr";
const I18N_DEBUG_KEY = "socialnet-i18n-debug"; // localStorage "1": untranslated texts are collected in window.untranslatedTexts

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

const LOCALE_TAGS = { tr: "tr-TR", en: "en-US" };
function localeTag() {
  const lang = getLang();
  return LOCALE_TAGS[lang] || lang;
}

// The language files are loaded while the page is being read, so i18n() works from the very first script on.
// (A file that does not exist is simply skipped: its texts fall back to the default language.)
(function loadLanguageFiles() {
  const lang = getLang();
  document.write('<script src="/locales/languages.js"><\/script>');
  document.write('<script src="/locales/' + DEFAULT_LANGUAGE + '.js"><\/script>');
  if (lang !== DEFAULT_LANGUAGE) document.write('<script src="/locales/' + lang + '.js"><\/script>');
  document.documentElement.lang = lang;
})();

// ---- Texts asked for by key ----

function i18n(key, ...args) {
  const locales = window.LOCALES || {};
  let text = (locales[getLang()] || {})[key] ?? (locales[DEFAULT_LANGUAGE] || {})[key] ?? key;
  args.forEach((arg, i) => { text = text.split(`{${i}}`).join(arg); });
  return text;
}

// ---- Translating what is on the page ----

const translatedCache = new Map();
let phraseIndex = null;

function normalizePhrase(text) {
  return text.replace(/\s+/g, " ").trim();
}

function escapeRegExp(text) {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

// Exact phrases are looked up directly; phrases with {0}... become patterns, the one with the most fixed text first.
function buildPhraseIndex() {
  const table = (window.PHRASES || {})[getLang()] || {};
  const index = { exact: new Map(), patterns: [] };

  for (const [source, target] of Object.entries(table)) {
    if (!/\{\d+\}/.test(source)) {
      index.exact.set(normalizePhrase(source), target);
      continue;
    }

    const numbers = [];
    const pattern = normalizePhrase(source).split(/(\{\d+\})/).map(part => {
      if (!/^\{\d+\}$/.test(part)) return escapeRegExp(part);
      numbers.push(Number(part.slice(1, -1)));
      return "(.+?)";
    }).join("");

    index.patterns.push({ regex: new RegExp(`^${pattern}$`, "s"), numbers, target, weight: normalizePhrase(source).replace(/\{\d+\}/g, "").length });
  }

  index.patterns.sort((a, b) => b.weight - a.weight);
  return index;
}

// The translation of a piece of text, or null when there is none.
function translatePhrase(text) {
  if (getLang() === DEFAULT_LANGUAGE) return null;
  phraseIndex = phraseIndex || buildPhraseIndex();

  const phrase = normalizePhrase(text);
  if (!phrase) return null;
  if (translatedCache.has(phrase)) return translatedCache.get(phrase);

  let result = phraseIndex.exact.get(phrase) ?? null;

  if (result === null) {
    for (const entry of phraseIndex.patterns) {
      const match = entry.regex.exec(phrase);
      if (!match) continue;

      // The values filled in may themselves be phrases ("Herkese Açık", a time ago...).
      const values = {};
      entry.numbers.forEach((number, i) => { values[number] = translatePhrase(match[i + 1]) ?? match[i + 1]; });
      result = entry.target.replace(/\{(\d+)\}/g, (all, number) => values[number] ?? all);
      break;
    }
  }

  // "5 katılıyor · 3 ilgileniyor": the parts around a middle dot are translated one by one.
  if (result === null && phrase.includes(" · ")) {
    const parts = phrase.split(" · ");
    const translated = parts.map(part => translatePhrase(part));
    if (translated.some(part => part !== null)) result = translated.map((part, i) => part ?? parts[i]).join(" · ");
  }

  translatedCache.set(phrase, result);
  return result;
}

// Translates a text keeping its spaces around it; returns the text itself when there is nothing to translate.
function translateKeepingSpaces(text) {
  const translated = translatePhrase(text);
  if (translated === null) {
    if (localStorage.getItem(I18N_DEBUG_KEY) === "1" && /[A-Za-zÇĞİÖŞÜçğıöşü]{2,}/.test(text)) {
      (window.untranslatedTexts = window.untranslatedTexts || new Set()).add(normalizePhrase(text));
    }
    return text;
  }
  return text.match(/^\s*/)[0] + translated + text.match(/\s*$/)[0];
}

const TRANSLATED_ATTRIBUTES = ["placeholder", "title", "aria-label", "alt"];
const SKIPPED_TAGS = new Set(["SCRIPT", "STYLE", "TEXTAREA", "NOSCRIPT", "CODE"]);
const writtenTexts = new WeakMap(); // node or element -> what this file last wrote there (so its own change is not translated again)

function translateTextNode(node) {
  const parent = node.parentElement;
  if (!parent || SKIPPED_TAGS.has(parent.tagName) || parent.closest("[data-no-i18n]")) return;
  if (writtenTexts.get(node) === node.nodeValue) return;

  // An <option> without a value has its text as the value: keep the original, the form must send the same value in any language.
  if (parent.tagName === "OPTION" && !parent.hasAttribute("value")) parent.setAttribute("value", normalizePhrase(node.nodeValue));

  const translated = translateKeepingSpaces(node.nodeValue);
  if (translated !== node.nodeValue) node.nodeValue = translated;
  writtenTexts.set(node, node.nodeValue);
}

function translateAttributes(element) {
  if (element.closest("[data-no-i18n]")) return;

  for (const name of TRANSLATED_ATTRIBUTES) {
    const value = element.getAttribute(name);
    if (!value) continue;

    const key = writtenTexts.get(element) || {};
    if (key[name] === value) continue;

    const translated = translateKeepingSpaces(value);
    if (translated !== value) element.setAttribute(name, translated);
    key[name] = translated;
    writtenTexts.set(element, key);
  }
}

function translateTree(root) {
  if (root.nodeType === Node.TEXT_NODE) { translateTextNode(root); return; }
  if (root.nodeType !== Node.ELEMENT_NODE && root.nodeType !== Node.DOCUMENT_NODE) return;

  if (root.nodeType === Node.ELEMENT_NODE) translateAttributes(root);

  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT | NodeFilter.SHOW_ELEMENT);
  let node;
  while ((node = walker.nextNode())) {
    if (node.nodeType === Node.TEXT_NODE) translateTextNode(node);
    else translateAttributes(node);
  }
}

function startTranslating() {
  if (getLang() === DEFAULT_LANGUAGE) return;

  translateTree(document.documentElement);

  // Whatever the scripts draw later (lists, toasts, modals, titles...) is translated as it appears.
  new MutationObserver(mutations => {
    for (const mutation of mutations) {
      if (mutation.type === "characterData") translateTextNode(mutation.target);
      else if (mutation.type === "attributes") translateAttributes(mutation.target);
      else mutation.addedNodes.forEach(translateTree);
    }
  }).observe(document.documentElement, {
    childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: TRANSLATED_ATTRIBUTES
  });
}

// Fills the elements of the HTML that ask for a text by key: data-i18n="key" (also data-i18n-placeholder / -title).
function translatePage(root = document) {
  root.querySelectorAll("[data-i18n]").forEach(el => { el.textContent = i18n(el.dataset.i18n); });
  root.querySelectorAll("[data-i18n-placeholder]").forEach(el => { el.placeholder = i18n(el.dataset.i18nPlaceholder); });
  root.querySelectorAll("[data-i18n-title]").forEach(el => { el.title = i18n(el.dataset.i18nTitle); });
}

// The language picker: a small select listing locales/languages.js (settings.html uses its own, see settings.js).
function languagePickerHtml(extraClass = "") {
  const current = getLang();
  const options = (window.LANGUAGES || []).map(l => `<option value="${l.code}" ${l.code === current ? "selected" : ""} data-no-i18n>${l.name}</option>`).join("");
  return `<select class="form-select form-select-sm lang-select ${extraClass}" aria-label="${i18n("language.label")}" title="${i18n("language.label")}" onchange="setLang(this.value)">${options}</select>`;
}

document.addEventListener("DOMContentLoaded", () => {
  translatePage();
  startTranslating();

  // The sign-in / register pages have no menu: the picker floats in the corner.
  if (document.querySelector(".auth-gradient")) {
    document.body.insertAdjacentHTML("beforeend", `<div class="lang-float" data-no-i18n>${languagePickerHtml()}</div>`);
  }
});
