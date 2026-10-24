(function () {
  function addToggle(input) {
    if (input.dataset.passwordToggle) return;
    input.dataset.passwordToggle = "1";

    const holder = document.createElement("div");
    holder.className = "password-field";
    input.parentNode.insertBefore(holder, input);
    holder.appendChild(input);

    const button = document.createElement("button");
    button.type = "button";
    button.className = "password-toggle";
    button.innerHTML = '<i class="bi bi-eye" aria-hidden="true"></i>';
    holder.appendChild(button);

    function paint() {
      const visible = input.type !== "password";
      const label = i18n(visible ? "password.hide" : "password.show");
      button.firstElementChild.className = visible ? "bi bi-eye-slash" : "bi bi-eye";
      button.setAttribute("aria-label", label);
      button.setAttribute("aria-pressed", String(visible));
      button.title = label;
    }

    button.addEventListener("click", () => {
      input.type = input.type === "password" ? "text" : "password";
      paint();
      input.focus();
    });

    paint();
  }

  function init() {
    document.querySelectorAll("input[type=password]").forEach(addToggle);
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init);
  else init();
})();
