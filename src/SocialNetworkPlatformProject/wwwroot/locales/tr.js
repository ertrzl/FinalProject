// Türkçe — the texts of the pages (js/i18n.js: t("key") in scripts, data-i18n="key" in the HTML).
// Turkish is the default language: a key missing in another language is shown in Turkish.
window.LOCALES = window.LOCALES || {};
window.LOCALES.tr = {
  "net.offline": "Sunucuya ulaşılamıyor. İnternet bağlantını kontrol edip tekrar dene.",
  "net.timeout": "İstek çok uzun sürdü. Lütfen tekrar dene.",
  "session.expired": "Oturum sona erdi.",
  "error.generic": "Bir hata oluştu.",

  "confirm.deletePost": "Bu gönderi silinsin mi? Bu işlem geri alınamaz.",
  "confirm.deleteComment": "Bu yorum silinsin mi? Altındaki yanıtlar da silinir.",
  "confirm.removeFriend": "{0} arkadaşlıktan çıkarılsın mı?",
  "confirm.blockUser": "{0} engellensin mi? Birbirinizin gönderilerini göremezsiniz, mesaj ve arkadaşlık isteği gönderemezsiniz; arkadaşsanız arkadaşlığınız da biter.",

  "block.menuItem": "Engelle",
  "block.more": "Diğer",
  "block.unblock": "Engeli Kaldır",
  "block.notice": "Bu kişiyi engelledin. Birbirinizin gönderilerini göremez, mesaj yazamazsınız.",
  "block.done": "Kişi engellendi.",
  "block.failed": "Engellenemedi.",
  "block.undone": "Engel kaldırıldı.",
  "block.undoFailed": "Engel kaldırılamadı.",

  "settings.blocked.title": "Engellenenler",
  "settings.blocked.intro": "Engellediğin kişiler seni ve gönderilerini göremez, sen de onları göremezsin. Birbirinize mesaj ve arkadaşlık isteği gönderemezsiniz.",
  "settings.blocked.empty": "Kimseyi engellemedin.",
  "common.loading": "Yükleniyor...",

  "language.label": "Dil"
};
