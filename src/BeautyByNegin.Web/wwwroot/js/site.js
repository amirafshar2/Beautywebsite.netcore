/* Beauty by Negin — small vanilla JS (no external libraries). */
(function () {
  "use strict";
  var doc = document.documentElement;
  doc.classList.remove("no-js");

  var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  /* ---------- Header shadow on scroll ---------- */
  var header = document.querySelector("[data-header]");
  if (header) {
    var onScroll = function () { header.classList.toggle("is-scrolled", window.scrollY > 8); };
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();
  }

  /* ---------- Focus trap helper for dialogs ---------- */
  function focusables(root) {
    return Array.prototype.filter.call(
      root.querySelectorAll('a[href], button:not([disabled]), input:not([type="hidden"]):not([tabindex="-1"]), select, textarea, [tabindex]:not([tabindex="-1"])'),
      function (el) { return el.offsetParent !== null || el === document.activeElement; });
  }
  function trap(root, e) {
    if (e.key !== "Tab") return;
    var items = focusables(root);
    if (!items.length) return;
    var first = items[0], last = items[items.length - 1];
    if (e.shiftKey && document.activeElement === first) { last.focus(); e.preventDefault(); }
    else if (!e.shiftKey && document.activeElement === last) { first.focus(); e.preventDefault(); }
  }

  /* ---------- Mobile menu ---------- */
  var menu = document.querySelector("[data-menu]");
  var menuOpener = document.querySelector("[data-menu-open]");
  function setMenu(open) {
    if (!menu) return;
    menu.classList.toggle("is-open", open);
    document.body.classList.toggle("menu-open", open);
    if (menuOpener) menuOpener.setAttribute("aria-expanded", String(open));
    if (open) { var f = focusables(menu)[0]; if (f) f.focus(); }
    else if (menuOpener) menuOpener.focus();
  }
  if (menu) {
    document.querySelectorAll("[data-menu-open]").forEach(function (b) { b.addEventListener("click", function () { setMenu(true); }); });
    document.querySelectorAll("[data-menu-close]").forEach(function (b) { b.addEventListener("click", function () { setMenu(false); }); });
    menu.addEventListener("keydown", function (e) { if (e.key === "Escape") setMenu(false); trap(menu, e); });
  }

  /* ---------- Generic modals: <button data-modal-open="id"> + <div class="modal" id="id"> ---------- */
  var lastOpener = null;
  function openModal(modal, opener) {
    lastOpener = opener || null;
    modal.classList.add("is-open");
    modal.setAttribute("aria-hidden", "false");
    document.body.classList.add("menu-open");
    var f = modal.querySelector(".modal-dialog");
    setTimeout(function () { (focusables(modal)[0] || f).focus(); }, 50);
    modal.dispatchEvent(new CustomEvent("modal:open", { detail: { opener: opener } }));
  }
  function closeModal(modal) {
    modal.classList.remove("is-open");
    modal.setAttribute("aria-hidden", "true");
    document.body.classList.remove("menu-open");
    if (lastOpener) lastOpener.focus();
  }
  document.addEventListener("click", function (e) {
    var opener = e.target.closest("[data-modal-open]");
    if (opener) {
      var m = document.getElementById(opener.getAttribute("data-modal-open"));
      if (m) { e.preventDefault(); openModal(m, opener); }
      return;
    }
    var closer = e.target.closest("[data-modal-close]");
    if (closer) { var mm = closer.closest(".modal"); if (mm) closeModal(mm); }
  });
  document.addEventListener("keydown", function (e) {
    var open = document.querySelector(".modal.is-open");
    if (!open) return;
    if (e.key === "Escape") closeModal(open);
    else trap(open, e);
  });
  window.BBN = window.BBN || {};
  window.BBN.openModal = openModal;
  window.BBN.closeModal = closeModal;

  /* ---------- "Ask about this treatment": fill the shared window with the clicked service ---------- */
  var ask = document.querySelector("[data-ask-modal]");
  if (ask) {
    var defaults = {};
    ask.querySelectorAll("[data-ask-link]").forEach(function (a) { defaults[a.getAttribute("data-ask-link")] = a.getAttribute("href"); });
    var titleEl = ask.querySelector("[data-ask-title]");
    var defaultTitle = titleEl ? titleEl.textContent : "";
    ask.addEventListener("modal:open", function (e) {
      var b = e.detail && e.detail.opener;
      var name = b && b.getAttribute("data-ask-name");
      if (titleEl) titleEl.textContent = name || defaultTitle;
      ask.querySelectorAll("[data-ask-link]").forEach(function (a) {
        var key = a.getAttribute("data-ask-link");
        var v = b && b.getAttribute("data-ask-" + key);
        a.setAttribute("href", v || defaults[key]);
      });
    });
  }

  /* ---------- Chat panel open/close (conversation logic: chat.js) ---------- */
  var chat = document.querySelector("[data-chat]");
  var chatOpener = document.querySelector("[data-chat-open]");
  if (chat && chatOpener) {
    var setChat = function (open) {
      chat.classList.toggle("is-open", open);
      chatOpener.setAttribute("aria-expanded", String(open));
      if (open) {
        chat.dispatchEvent(new CustomEvent("chat:open"));
        setTimeout(function () { var f = focusables(chat).filter(function (el) { return !el.closest("[data-chat-close]"); })[0]; if (f) f.focus(); }, 80);
      }
    };
    chatOpener.addEventListener("click", function () { setChat(!chat.classList.contains("is-open")); });
    chat.querySelector("[data-chat-close]").addEventListener("click", function () { setChat(false); chatOpener.focus(); });
    chat.addEventListener("keydown", function (e) { if (e.key === "Escape") { setChat(false); chatOpener.focus(); } });
  }

  /* ---------- Subtle fade-up on scroll ---------- */
  var reveals = document.querySelectorAll(".reveal");
  if (reveals.length) {
    if (reduceMotion || !("IntersectionObserver" in window)) {
      reveals.forEach(function (el) { el.classList.add("is-visible"); });
    } else {
      var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) { entry.target.classList.add("is-visible"); io.unobserve(entry.target); }
        });
      }, { rootMargin: "0px 0px -8% 0px", threshold: 0.08 });
      reveals.forEach(function (el) { io.observe(el); });
    }
  }
})();
