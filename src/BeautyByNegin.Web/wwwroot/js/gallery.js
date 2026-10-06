/* Gallery: category filter, lightbox (keyboard + swipe), before/after slider. No external libraries. */
(function () {
  "use strict";
  var rtl = document.documentElement.dir === "rtl";

  /* ---------- Category filter ---------- */
  document.querySelectorAll("[data-gallery]").forEach(function (root) {
    var tabs = root.querySelectorAll("[data-filter]");
    var items = root.querySelectorAll(".gallery-item");
    tabs.forEach(function (tab) {
      tab.addEventListener("click", function () {
        var f = tab.getAttribute("data-filter");
        tabs.forEach(function (t) { t.setAttribute("aria-selected", String(t === tab)); });
        items.forEach(function (it) { it.hidden = !(f === "all" || it.getAttribute("data-category") === f); });
      });
    });
  });

  /* ---------- Before / after ---------- */
  document.querySelectorAll("[data-ba]").forEach(function (slider) {
    var range = slider.querySelector(".ba-range");
    var set = function (v) { slider.style.setProperty("--pos", v + "%"); };
    range.addEventListener("input", function () { set(range.value); });
    set(range.value);
  });

  /* ---------- Lightbox ---------- */
  var box = document.querySelector("[data-lightbox-box]");
  if (!box) {
    // Pages without a lightbox element (service detail) get one created on the fly.
    var group = document.querySelector("[data-lightbox-group]");
    if (!group) return;
    box = document.createElement("div");
    box.className = "lightbox"; box.hidden = true;
    box.setAttribute("data-lightbox-box", ""); box.setAttribute("role", "dialog"); box.setAttribute("aria-modal", "true");
    box.innerHTML = '<button class="lb-close" type="button" data-lb-close aria-label="×"><svg class="icon" aria-hidden="true"><use href="#i-close"/></svg></button>' +
      '<button class="lb-prev" type="button" data-lb-prev aria-label="‹"><svg class="icon" aria-hidden="true"><use href="#i-chevron"/></svg></button>' +
      '<figure><img alt="" data-lb-img><figcaption data-lb-caption></figcaption></figure>' +
      '<button class="lb-next" type="button" data-lb-next aria-label="›"><svg class="icon" aria-hidden="true"><use href="#i-chevron"/></svg></button>';
    document.body.appendChild(box);
  }
  var img = box.querySelector("[data-lb-img]");
  var cap = box.querySelector("[data-lb-caption]");
  var links = [], index = 0, opener = null;

  function visibleLinks() {
    return Array.prototype.filter.call(document.querySelectorAll("[data-lightbox]"), function (a) {
      return !a.closest("[hidden]");
    });
  }
  function show(i) {
    if (!links.length) return;
    index = (i + links.length) % links.length;
    var a = links[index];
    img.src = a.getAttribute("data-full") || a.href;
    var c = a.getAttribute("data-caption") || "";
    img.alt = c; cap.textContent = c; cap.hidden = !c;
  }
  function open(a) {
    links = visibleLinks(); opener = a;
    show(links.indexOf(a));
    box.hidden = false;
    document.body.classList.add("menu-open");
    box.querySelector("[data-lb-close]").focus();
  }
  function close() {
    box.hidden = true; img.removeAttribute("src");
    document.body.classList.remove("menu-open");
    if (opener) opener.focus();
  }
  function next() { show(index + 1); }
  function prev() { show(index - 1); }

  document.addEventListener("click", function (e) {
    var a = e.target.closest("[data-lightbox]");
    if (a) { e.preventDefault(); open(a); }
  });
  box.querySelector("[data-lb-close]").addEventListener("click", close);
  box.querySelector("[data-lb-next]").addEventListener("click", next);
  box.querySelector("[data-lb-prev]").addEventListener("click", prev);
  box.addEventListener("click", function (e) { if (e.target === box) close(); });
  document.addEventListener("keydown", function (e) {
    if (box.hidden) return;
    if (e.key === "Escape") close();
    else if (e.key === "ArrowRight") (rtl ? prev : next)();
    else if (e.key === "ArrowLeft") (rtl ? next : prev)();
  });
  var startX = null;
  box.addEventListener("touchstart", function (e) { startX = e.touches[0].clientX; }, { passive: true });
  box.addEventListener("touchend", function (e) {
    if (startX === null) return;
    var dx = e.changedTouches[0].clientX - startX; startX = null;
    if (Math.abs(dx) < 40) return;
    var forward = dx < 0; if (rtl) forward = !forward;
    (forward ? next : prev)();
  });
})();
