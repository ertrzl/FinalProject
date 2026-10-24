const LANGUAGE_STORAGE_KEY = "socialnet-lang";
const DEFAULT_LANGUAGE = "tr";
const I18N_DEBUG_KEY = "socialnet-i18n-debug";

function getLang() {
  try {
    const saved = localStorage.getItem(LANGUAGE_STORAGE_KEY);
    if (saved && /^[a-z]{2,3}$/.test(saved)) return saved;
  } catch (e) { }
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

(function loadLanguageFiles() {
  const lang = getLang();
  document.write('<script src="/locales/languages.js"><\/script>');
  document.write('<script src="/locales/' + DEFAULT_LANGUAGE + '.js"><\/script>');
  if (lang !== DEFAULT_LANGUAGE) document.write('<script src="/locales/' + lang + '.js"><\/script>');
  document.documentElement.lang = lang;
})();


function i18n(key, ...args) {
  const locales = window.LOCALES || {};
  let text = (locales[getLang()] || {})[key] ?? (locales[DEFAULT_LANGUAGE] || {})[key] ?? key;
  args.forEach((arg, i) => { text = text.split(`{${i}}`).join(arg); });
  return text;
}


const translatedCache = new Map();
let phraseIndex = null;

function normalizePhrase(text) {
  return text.replace(/\s+/g, " ").trim();
}

function escapeRegExp(text) {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

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

      const values = {};
      entry.numbers.forEach((number, i) => { values[number] = translatePhrase(match[i + 1]) ?? match[i + 1]; });
      result = entry.target.replace(/\{(\d+)\}/g, (all, number) => values[number] ?? all);
      break;
    }
  }

  if (result === null && phrase.includes(" · ")) {
    const parts = phrase.split(" · ");
    const translated = parts.map(part => translatePhrase(part));
    if (translated.some(part => part !== null)) result = translated.map((part, i) => part ?? parts[i]).join(" · ");
  }

  translatedCache.set(phrase, result);
  return result;
}

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
const writtenTexts = new WeakMap();

function translateTextNode(node) {
  const parent = node.parentElement;
  if (!parent || SKIPPED_TAGS.has(parent.tagName) || parent.closest("[data-no-i18n]")) return;
  if (writtenTexts.get(node) === node.nodeValue) return;

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

function translatePage(root = document) {
  root.querySelectorAll("[data-i18n]").forEach(el => { el.textContent = i18n(el.dataset.i18n); });
  root.querySelectorAll("[data-i18n-placeholder]").forEach(el => { el.placeholder = i18n(el.dataset.i18nPlaceholder); });
  root.querySelectorAll("[data-i18n-title]").forEach(el => { el.title = i18n(el.dataset.i18nTitle); });
}

function languagePickerHtml(extraClass = "") {
  const current = getLang();
  const options = (window.LANGUAGES || []).map(l => `<option value="${l.code}" ${l.code === current ? "selected" : ""} data-no-i18n>${l.name}</option>`).join("");
  return `<select class="form-select form-select-sm lang-select ${extraClass}" aria-label="${i18n("language.label")}" title="${i18n("language.label")}" onchange="setLang(this.value)">${options}</select>`;
}

document.addEventListener("DOMContentLoaded", () => {
  translatePage();
  startTranslating();

  if (document.querySelector(".auth-gradient")) {
    document.body.insertAdjacentHTML("beforeend", `<div class="lang-float" data-no-i18n>${languagePickerHtml()}</div>`);
  }
});
