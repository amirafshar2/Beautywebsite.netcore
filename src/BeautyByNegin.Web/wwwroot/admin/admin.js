/* Beauty by Negin — admin panel behaviour (vanilla JS, local libraries loaded on demand). */
(function () {
  "use strict";
  var body = document.body;
  var base = body.getAttribute("data-base") || "/admin";
  var csrf = (document.querySelector('meta[name="csrf-token"]') || {}).content || "";

  /* ---------------- helpers ---------------- */
  function toast(message, ok) {
    var host = document.querySelector("[data-toast-host]");
    if (!host || !message) return;
    var t = document.createElement("div");
    t.className = "toast " + (ok === false ? "toast-err" : "toast-ok");
    t.textContent = message;
    host.appendChild(t);
    setTimeout(function () { t.remove(); }, Math.max(3200, message.length * 70));
  }
  function post(url, data) {
    var fd = data instanceof FormData ? data : new FormData();
    if (!(data instanceof FormData)) Object.keys(data || {}).forEach(function (k) { fd.append(k, data[k]); });
    return fetch(url, { method: "POST", body: fd, credentials: "same-origin", headers: { "X-CSRF-TOKEN": csrf, "X-Requested-With": "fetch", "Accept": "application/json" } })
      .then(function (r) { return r.json().catch(function () { return { ok: r.ok }; }); });
  }
  var loaded = {};
  function loadAsset(src) {
    if (loaded[src]) return loaded[src];
    loaded[src] = new Promise(function (resolve, reject) {
      var el;
      if (src.endsWith(".css")) { el = document.createElement("link"); el.rel = "stylesheet"; el.href = src; }
      else { el = document.createElement("script"); el.src = src; }
      el.onload = resolve; el.onerror = reject;
      document.head.appendChild(el);
    });
    return loaded[src];
  }
  window.Panel = { toast: toast, post: post };

  /* ---------------- toasts already on the page fade out ---------------- */
  document.querySelectorAll("[data-toast]").forEach(function (t) { setTimeout(function () { t.style.transition = "opacity .5s"; t.style.opacity = "0"; }, 4000); });

  /* ---------------- sidebar (mobile) ---------------- */
  var sidebar = document.querySelector("[data-sidebar]");
  var backdrop = document.querySelector("[data-sidebar-close]");
  document.querySelectorAll("[data-sidebar-open]").forEach(function (b) {
    b.addEventListener("click", function () { sidebar.classList.add("is-open"); backdrop.classList.add("is-open"); });
  });
  if (backdrop) backdrop.addEventListener("click", function () { sidebar.classList.remove("is-open"); backdrop.classList.remove("is-open"); });

  document.querySelectorAll("[data-autosubmit]").forEach(function (s) { s.addEventListener("change", function () { s.form.submit(); }); });

  /* ---------------- language tabs: all tab groups on the page switch together ---------------- */
  function showLang(code) {
    document.querySelectorAll("[data-tab]").forEach(function (t) {
      var on = t.getAttribute("data-tab") === code;
      t.classList.toggle("is-active", on); t.setAttribute("aria-selected", String(on));
    });
    document.querySelectorAll(".tab-pane").forEach(function (p) { p.classList.toggle("is-active", p.getAttribute("data-lang") === code); });
  }
  document.addEventListener("click", function (e) {
    var tab = e.target.closest("[data-tab]");
    if (tab) showLang(tab.getAttribute("data-tab"));
  });
  // Open the first tab that contains a validation error.
  var errPane = document.querySelector(".tab-pane .has-error");
  if (errPane) showLang(errPane.closest(".tab-pane").getAttribute("data-lang"));

  /* ---------------- unsaved changes warning ---------------- */
  var dirty = false;
  document.querySelectorAll("form[data-dirty]").forEach(function (f) {
    f.addEventListener("input", function () { dirty = true; });
    f.addEventListener("change", function () { dirty = true; });
    f.addEventListener("submit", function () { dirty = false; });
  });
  window.addEventListener("beforeunload", function (e) {
    if (!dirty) return;
    e.preventDefault(); e.returnValue = body.getAttribute("data-confirm-leave") || "";
  });

  /* ---------------- confirm dialog (delete etc.) ---------------- */
  var dialog = document.getElementById("confirm-dialog");
  function confirmBox(text) {
    return new Promise(function (resolve) {
      if (!dialog || !dialog.showModal) { resolve(window.confirm(text)); return; }
      dialog.querySelector("[data-confirm-text]").textContent = text;
      dialog.returnValue = "";
      dialog.showModal();
      dialog.addEventListener("close", function handler() { dialog.removeEventListener("close", handler); resolve(dialog.returnValue === "ok"); });
    });
  }
  document.addEventListener("submit", function (e) {
    var f = e.target;
    var text = f.getAttribute("data-confirm");
    if (!text || f._confirmed) return;
    e.preventDefault();
    confirmBox(text).then(function (ok) { if (ok) { f._confirmed = true; dirty = false; f.submit(); } });
  }, true);

  /* ---------------- submit buttons that need confirmation: <button formaction=".." data-confirm-btn="text"> ---------------- */
  document.addEventListener("click", function (e) {
    var b = e.target.closest("[data-confirm-btn]");
    if (!b || b._confirmed) return;
    e.preventDefault();
    confirmBox(b.getAttribute("data-confirm-btn")).then(function (ok) {
      if (!ok) return;
      b._confirmed = true; dirty = false; b.click();
    });
  });

  /* ---------------- AJAX buttons: <button data-post="url" data-confirm="?" data-remove="selector"> ---------------- */
  document.addEventListener("click", function (e) {
    var b = e.target.closest("[data-post]");
    if (!b) return;
    e.preventDefault();
    var go = function () {
      b.disabled = true;
      post(b.getAttribute("data-post")).then(function (d) {
        toast(d.message, d.ok);
        if (d.ok) {
          var rm = b.getAttribute("data-remove");
          if (rm) { var el = b.closest(rm); if (el) el.remove(); }
          if (b.hasAttribute("data-reload")) location.reload();
        }
      }).finally(function () { b.disabled = false; });
    };
    var c = b.getAttribute("data-confirm");
    if (c) confirmBox(c).then(function (ok) { if (ok) go(); }); else go();
  });

  /* ---------------- switches: <input type=checkbox data-toggle-url="..."> ---------------- */
  document.addEventListener("change", function (e) {
    var cb = e.target.closest("[data-toggle-url]");
    if (!cb) return;
    post(cb.getAttribute("data-toggle-url")).then(function (d) {
      if (!d.ok) { cb.checked = !cb.checked; toast(d.message, false); return; }
      toast(d.message, true);
      var item = cb.closest("[data-hide-when-off]");
      if (item) item.classList.toggle("is-hidden", !cb.checked);
    }).catch(function () { cb.checked = !cb.checked; toast(body.getAttribute("data-error"), false); });
  });

  /* ---------------- selects that save immediately: <select name="x" data-post-change="url"> ---------------- */
  document.addEventListener("change", function (e) {
    var s = e.target.closest("select[data-post-change]");
    if (!s) return;
    var data = {}; data[s.name] = s.value;
    post(s.getAttribute("data-post-change"), data).then(function (d) { toast(d.message, d.ok); });
  });

  /* ---------------- sorting: drag & drop (desktop) + up/down buttons (phone) ---------------- */
  function saveOrder(list) {
    var ids = Array.prototype.map.call(list.querySelectorAll(":scope > [data-id]"), function (el) { return el.getAttribute("data-id"); });
    return post(list.getAttribute("data-sortable"), { ids: ids.join(",") }).then(function (d) { toast(d.message, d.ok); });
  }
  var sortables = document.querySelectorAll("[data-sortable]");
  if (sortables.length) {
    loadAsset("/lib/sortablejs/Sortable.min.js").then(function () {
      sortables.forEach(function (list) {
        window.Sortable.create(list, { handle: ".drag-handle", animation: 150, ghostClass: "sortable-ghost", onEnd: function () { saveOrder(list); } });
      });
    });
    document.addEventListener("click", function (e) {
      var b = e.target.closest("[data-move]");
      if (!b) return;
      var item = b.closest("[data-id]"), list = item.parentElement;
      if (b.getAttribute("data-move") === "up" && item.previousElementSibling) list.insertBefore(item, item.previousElementSibling);
      else if (b.getAttribute("data-move") === "down" && item.nextElementSibling) list.insertBefore(item.nextElementSibling, item);
      else return;
      saveOrder(list);
    });
  }

  /* ---------------- sorting inside a form (saved with the form): [data-sortable-local] ---------------- */
  var localLists = document.querySelectorAll("[data-sortable-local]");
  if (localLists.length) {
    loadAsset("/lib/sortablejs/Sortable.min.js").then(function () {
      localLists.forEach(function (list) { window.Sortable.create(list, { handle: ".drag-handle", animation: 150, onEnd: function () { dirty = true; } }); });
    });
    document.addEventListener("click", function (e) {
      var b = e.target.closest("[data-move-local]");
      if (!b) return;
      var item = b.closest("[data-id]"), list = item.parentElement;
      if (b.getAttribute("data-move-local") === "up" && item.previousElementSibling) list.insertBefore(item, item.previousElementSibling);
      else if (b.getAttribute("data-move-local") === "down" && item.nextElementSibling) list.insertBefore(item.nextElementSibling, item);
      dirty = true;
    });
  }

  /* ---------------- image upload with cropping ---------------- */
  function uploadFile(file, crop, onProgress) {
    return new Promise(function (resolve, reject) {
      var fd = new FormData();
      fd.append("file", file);
      if (crop) { fd.append("x", Math.round(crop.x)); fd.append("y", Math.round(crop.y)); fd.append("w", Math.round(crop.width)); fd.append("h", Math.round(crop.height)); }
      var xhr = new XMLHttpRequest();
      xhr.open("POST", base + "/media/upload");
      xhr.setRequestHeader("X-CSRF-TOKEN", csrf);
      xhr.setRequestHeader("X-Requested-With", "fetch");
      xhr.upload.onprogress = function (e) { if (e.lengthComputable && onProgress) onProgress(e.loaded / e.total); };
      xhr.onload = function () { try { resolve(JSON.parse(xhr.responseText)); } catch (err) { reject(err); } };
      xhr.onerror = reject;
      xhr.send(fd);
    });
  }

  var cropDialog = null;
  function cropImage(file, ratio) {
    return Promise.all([loadAsset("/lib/cropperjs/cropper.min.css"), loadAsset("/lib/cropperjs/cropper.min.js")]).then(function () {
      return new Promise(function (resolve) {
        if (!cropDialog) {
          cropDialog = document.createElement("dialog");
          cropDialog.className = "cropper-dialog";
          cropDialog.innerHTML = '<div class="crop-area"><img alt=""></div><div class="row-actions crop-actions">' +
            '<button type="button" class="btn" data-c="cancel"></button><button type="button" class="btn btn-primary" data-c="ok"></button></div>';
          document.body.appendChild(cropDialog);
        }
        cropDialog.querySelector('[data-c="cancel"]').textContent = document.documentElement.lang === "fa" ? "انصراف" : "Cancel";
        cropDialog.querySelector('[data-c="ok"]').textContent = document.documentElement.lang === "fa" ? "تأیید و آپلود" : "OK";
        var img = cropDialog.querySelector("img");
        var url = URL.createObjectURL(file);
        img.src = url;
        cropDialog.showModal();
        var cropper = new window.Cropper(img, { aspectRatio: ratio > 0 ? ratio : NaN, viewMode: 1, autoCropArea: 1, background: false });
        function done(result) {
          cropper.destroy(); URL.revokeObjectURL(url); cropDialog.close();
          cropDialog.querySelectorAll("[data-c]").forEach(function (b) { b.onclick = null; });
          resolve(result);
        }
        cropDialog.querySelector('[data-c="ok"]').onclick = function () { done(cropper.getData(true)); };
        cropDialog.querySelector('[data-c="cancel"]').onclick = function () { done(null); };
      });
    });
  }

  function setupImageField(field) {
    var input = field.querySelector("[data-file]");
    var value = field.querySelector("[data-value]");
    var preview = field.querySelector("[data-preview]");
    var empty = field.querySelector(".empty-text");
    var remove = field.querySelector("[data-remove]");
    var progress = field.querySelector("[data-progress]");
    var ratio = parseFloat(field.getAttribute("data-ratio") || "0");

    function handle(file) {
      if (!file) return;
      if (file.size > 20 * 1024 * 1024) { toast(field.closest("form") && body.getAttribute("data-error"), false); return; }
      cropImage(file, ratio).then(function (crop) {
        if (crop === null) return; // cancelled
        progress.hidden = false;
        var bar = progress.querySelector("span");
        return uploadFile(file, crop, function (p) { bar.style.width = Math.round(p * 100) + "%"; }).then(function (d) {
          progress.hidden = true; bar.style.width = "0";
          if (!d.ok) { toast(d.message, false); return; }
          value.value = d.id;
          value.dispatchEvent(new Event("change", { bubbles: true }));
          preview.src = d.url; preview.hidden = false; empty.hidden = true; remove.hidden = false;
        });
      }).catch(function () { progress.hidden = true; toast(body.getAttribute("data-error"), false); });
    }
    input.addEventListener("change", function () { handle(input.files[0]); input.value = ""; });
    remove.addEventListener("click", function () {
      value.value = ""; value.dispatchEvent(new Event("change", { bubbles: true }));
      preview.hidden = true; preview.removeAttribute("src"); empty.hidden = false; remove.hidden = true;
    });
    var box = field.querySelector(".image-box");
    ["dragenter", "dragover"].forEach(function (ev) { box.addEventListener(ev, function (e) { e.preventDefault(); field.classList.add("is-dragover"); }); });
    ["dragleave", "drop"].forEach(function (ev) { box.addEventListener(ev, function (e) { e.preventDefault(); field.classList.remove("is-dragover"); }); });
    box.addEventListener("drop", function (e) { handle(e.dataTransfer.files[0]); });
  }
  document.querySelectorAll("[data-image-field]").forEach(setupImageField);

  /* ---------------- several photos inside a form (service gallery): [data-images-field] ---------------- */
  document.querySelectorAll("[data-images-field]").forEach(function (field) {
    var value = field.querySelector("[data-value]");
    var thumbs = field.querySelector("[data-thumbs]");
    var status = field.querySelector("[data-status]");
    function sync() {
      value.value = Array.prototype.map.call(thumbs.querySelectorAll("[data-id]"), function (t) { return t.getAttribute("data-id"); }).join(",");
      value.dispatchEvent(new Event("change", { bubbles: true }));
    }
    thumbs.addEventListener("click", function (e) {
      var b = e.target.closest("[data-remove-thumb]");
      if (b) { b.closest("[data-id]").remove(); sync(); }
    });
    field.querySelector("[data-files]").addEventListener("change", function (e) {
      var files = Array.prototype.slice.call(e.target.files); e.target.value = "";
      var total = files.length, n = 0;
      var next = function () {
        if (!files.length) { status.textContent = ""; sync(); return; }
        status.textContent = (++n) + " / " + total;
        var f = files.shift();
        uploadFile(f, null).then(function (d) {
          if (!d.ok) { toast(f.name + ": " + d.message, false); return next(); }
          var card = document.createElement("div");
          card.className = "thumb-card"; card.setAttribute("data-id", d.id);
          card.innerHTML = '<img alt=""><div class="actions"><span></span><button type="button" class="icon-btn" data-remove-thumb><svg class="i"><use href="#a-close"/></svg></button></div>';
          card.querySelector("img").src = d.url;
          thumbs.appendChild(card);
          next();
        });
      };
      next();
    });
  });

  /* ---------------- multiple photos: <input type=file multiple data-multi-upload="attach-url"> ---------------- */
  document.querySelectorAll("[data-multi-upload]").forEach(function (input) {
    input.addEventListener("change", function () {
      var files = Array.prototype.slice.call(input.files);
      if (!files.length) return;
      var status = document.querySelector(input.getAttribute("data-status") || "#upload-status");
      var ids = [], done = 0;
      var next = function () {
        if (!files.length) {
          if (!ids.length) return;
          var extra = new FormData();
          extra.append("ids", ids.join(","));
          var sel = input.getAttribute("data-extra");
          if (sel) document.querySelectorAll(sel).forEach(function (el) { extra.append(el.name, el.value); });
          return post(input.getAttribute("data-multi-upload"), extra).then(function () { location.reload(); });
        }
        var f = files.shift();
        if (status) status.textContent = (++done) + " / " + (done + files.length);
        return uploadFile(f, null).then(function (d) {
          if (d.ok) ids.push(d.id); else toast(f.name + ": " + d.message, false);
          return next();
        });
      };
      next();
    });
  });

  /* ---------------- rich text editor (Quill, local) ---------------- */
  var richAreas = document.querySelectorAll("textarea[data-rich]");
  if (richAreas.length) {
    Promise.all([loadAsset("/lib/quill/quill.snow.css"), loadAsset("/lib/quill/quill.js")]).then(function () {
      richAreas.forEach(function (ta) {
        var holder = document.createElement("div");
        holder.className = "rich-editor";
        var pane = ta.closest("[data-lang]");
        var rtl = pane && ["fa", "ar"].indexOf(pane.getAttribute("data-lang")) >= 0;
        holder.setAttribute("dir", rtl ? "rtl" : "ltr");
        ta.parentNode.insertBefore(holder, ta);
        ta.hidden = true;
        var q = new window.Quill(holder, {
          theme: "snow",
          modules: { toolbar: [[{ header: [2, 3, false] }], ["bold", "italic", "underline"], [{ list: "ordered" }, { list: "bullet" }], ["blockquote", "link"], ["clean"]] }
        });
        if (rtl) { q.format("direction", "rtl"); q.format("align", "right"); }
        q.clipboard.dangerouslyPasteHTML(ta.value || "");
        ta._quill = q; // used by the translate button
        q.on("text-change", function () {
          ta.value = q.root.innerHTML === "<p><br></p>" ? "" : q.root.innerHTML;
          dirty = true;
        });
      });
    });
  }

  /* ---------------- small forms saved on change (Instagram post links): <form data-ajax-save> ---------------- */
  document.querySelectorAll("form[data-ajax-save]").forEach(function (f) {
    f.addEventListener("change", function () { post(f.action, new FormData(f)).then(function (d) { toast(d.message, d.ok); }); });
    f.addEventListener("submit", function (e) { e.preventDefault(); f.dispatchEvent(new Event("change")); });
  });

  /* ---------------- opening hours: "closed" greys out the times ---------------- */
  document.querySelectorAll("[data-hour-row]").forEach(function (row) {
    var cb = row.querySelector("[data-closed]");
    var sync = function () { row.classList.toggle("is-closed", cb.checked); };
    cb.addEventListener("change", sync); sync();
  });

  /* ---------------- settings: country -> suggested time zone; Telegram "find my Chat ID" ---------------- */
  var country = document.querySelector("[data-country]");
  if (country) country.addEventListener("change", function () {
    var tz = country.selectedOptions[0].getAttribute("data-tz"), sel = document.getElementById("timeZone");
    if (tz && sel) sel.value = tz;
  });
  document.querySelectorAll("[data-find-chat]").forEach(function (b) {
    b.addEventListener("click", function () {
      b.disabled = true;
      post(b.getAttribute("data-find-chat")).then(function (d) {
        toast(d.message, d.ok);
        if (d.ok && d.data && d.data.length) {
          var input = document.getElementById("telegramChatId");
          input.value = d.data[0].id; input.dispatchEvent(new Event("change", { bubbles: true }));
          var list = document.querySelector("[data-chat-list]");
          if (list) list.textContent = d.data.map(function (c) { return c.name + ": " + c.id; }).join(" · ");
        }
      }).finally(function () { b.disabled = false; });
    });
  });

  /* ---------------- "Test" buttons for phone / WhatsApp / links ---------------- */
  document.addEventListener("click", function (e) {
    var b = e.target.closest("[data-test-link]");
    if (!b) return;
    var input = document.getElementById(b.getAttribute("data-test-link"));
    if (!input || !input.value.trim()) return;
    var v = input.value.trim(), kind = b.getAttribute("data-kind"), digits = v.replace(/[^\d+]/g, "");
    var url = kind === "tel" ? "tel:" + digits
      : kind === "wa" ? "https://wa.me/" + digits.replace(/^\+/, "").replace(/^00/, "")
      : kind === "mail" ? "mailto:" + v
      : kind === "ig" ? "https://instagram.com/" + v.replace(/^@/, "")
      : kind === "tg" ? "https://t.me/" + v.replace(/^@/, "")
      : (/^https?:/.test(v) ? v : "https://" + v);
    window.open(url, "_blank", "noopener");
  });

  /* ---------------- automatic translation (Gemini): <button data-translate> next to language tabs,
     plus a small button under every row of side-by-side language fields (.lang-inline).
     Fields are matched by name: "Tr[fa].Name" ↔ "Tr[de].Name", "Texts[key][fa]" ↔ "Texts[key][de]".
     Results are only filled in – the admin checks them and presses Save. ---------------- */
  var LANGS = ["fa", "tr", "de", "en", "ar"];
  var langRe = /\[(fa|tr|de|en|ar)\]/;
  function translatable(el) {
    if (!el.name || el.disabled || el.readOnly || el.hasAttribute("data-no-translate")) return false;
    if (el.tagName === "INPUT" && ["hidden", "checkbox", "radio", "password", "file", "number", "date", "email", "url", "tel"].indexOf(el.type) >= 0) return false;
    if (el.getAttribute("dir") === "ltr" && el.closest(".advanced")) return false; // page addresses (slugs)
    return langRe.test(el.name);
  }
  function setValue(el, value) {
    if (el._quill) { el._quill.setContents([]); el._quill.clipboard.dangerouslyPasteHTML(value || ""); el.value = value || ""; }
    else { el.value = value || ""; }
    el.dispatchEvent(new Event("input", { bubbles: true }));
  }
  function translateScope(btn) {
    var scope = btn.closest("form") || btn.closest(".card") || document;
    var activeTab = scope.querySelector(".lang-tab.is-active") || document.querySelector(".lang-tab.is-active");
    var groups = {};
    scope.querySelectorAll("input[name], textarea[name]").forEach(function (el) {
      if (!translatable(el)) return;
      var code = el.name.match(langRe)[1];
      var key = el.name.replace("[" + code + "]", "[*]");
      (groups[key] = groups[key] || {})[code] = el;
    });
    var keys = Object.keys(groups);
    // Source: the open tab; otherwise Persian (or the main language), otherwise the first language that has text.
    var source = activeTab ? activeTab.getAttribute("data-tab") : body.getAttribute("data-ai-source") || "fa";
    var hasText = function (code) { return keys.some(function (k) { var el = groups[k][code]; return el && el.value.trim(); }); };
    if (!hasText(source)) { var other = LANGS.filter(hasText)[0]; if (other) source = other; }
    var items = [], copies = [], targets = {}, overwrite = false;
    keys.forEach(function (k, i) {
      var g = groups[k], src = g[source];
      if (!src || !src.value.trim()) return;
      Object.keys(g).forEach(function (c) {
        if (c === source) return;
        targets[c] = true;
        var v = g[c].value.trim();
        if (v && v !== src.value.trim()) overwrite = true;
      });
      if (src.hasAttribute("data-translate-copy")) { copies.push(k); return; }
      items.push({ key: String(i), text: src.value, html: src.hasAttribute("data-rich"), field: k });
    });
    return { groups: groups, source: source, items: items, copies: copies, targets: Object.keys(targets), overwrite: overwrite };
  }
  function runTranslate(btn) {
    if (body.getAttribute("data-ai-ready") !== "true") {
      confirmBox(body.getAttribute("data-ai-not-ready")).then(function (ok) { if (ok) location.href = body.getAttribute("data-ai-settings"); });
      return;
    }
    var job = translateScope(btn);
    if ((!job.items.length && !job.copies.length) || !job.targets.length) { toast(body.getAttribute("data-ai-empty"), false); return; }
    var go = function () {
      var label = btn.innerHTML;
      btn.disabled = true; btn.classList.add("is-busy"); btn.textContent = body.getAttribute("data-ai-busy");
      var finish = function () { btn.disabled = false; btn.classList.remove("is-busy"); btn.innerHTML = label; };
      job.copies.forEach(function (k) { var g = job.groups[k]; job.targets.forEach(function (c) { if (g[c]) setValue(g[c], g[job.source].value); }); });
      if (!job.items.length) { finish(); toast(body.getAttribute("data-saved"), true); return; }
      var payload = { source: job.source, targets: job.targets, items: job.items.map(function (i) { return { key: i.key, text: i.text, html: i.html }; }) };
      post(body.getAttribute("data-ai-url"), { payload: JSON.stringify(payload) }).then(function (d) {
        finish();
        if (!d.ok) { toast(d.message || body.getAttribute("data-error"), false); return; }
        job.items.forEach(function (i) {
          var g = job.groups[i.field];
          job.targets.forEach(function (c) {
            var value = d.translations && d.translations[c] && d.translations[c][i.key];
            if (g[c] && value) setValue(g[c], value);
          });
        });
        dirty = true;
        toast(d.message, true);
      }).catch(function () { finish(); toast(body.getAttribute("data-error"), false); });
    };
    if (job.overwrite) confirmBox(body.getAttribute("data-ai-overwrite")).then(function (ok) { if (ok) go(); });
    else go();
  }
  document.addEventListener("click", function (e) {
    var b = e.target.closest("[data-translate]");
    if (b) { e.preventDefault(); runTranslate(b); }
  });
  // Rows with all languages side by side (lists, categories, booking times): add a small button to each.
  document.querySelectorAll(".lang-inline").forEach(function (row) {
    if (row.querySelectorAll("input[name]").length < 2) return;
    var b = document.createElement("button");
    b.type = "button"; b.className = "btn btn-sm translate-btn translate-inline"; b.setAttribute("data-translate", "");
    b.textContent = body.getAttribute("data-ai-label-short");
    row.parentNode.insertBefore(b, row.nextSibling);
  });
})();
