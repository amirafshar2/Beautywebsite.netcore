/* Floating chat (bottom-left): register with e-mail -> verify 6-digit code -> conversation.
   Talks to /{lang}/api/chat/*. Polls for replies (fast while open, slow while closed for the badge). */
(function () {
  "use strict";
  var panel = document.querySelector("[data-chat]");
  if (!panel) return;

  var api = panel.getAttribute("data-api");
  var csrf = (document.querySelector('meta[name="csrf-token"]') || {}).content || "";
  var body = panel.querySelector("[data-chat-body]");
  var steps = {
    register: panel.querySelector('[data-step="register"]'),
    verify: panel.querySelector('[data-step="verify"]'),
    conversation: panel.querySelector('[data-step="conversation"]')
  };
  var list = panel.querySelector("[data-chat-messages]");
  var compose = panel.querySelector("[data-chat-compose]");
  var badge = document.querySelector("[data-chat-badge]");
  var lastId = 0, step = null, timer = null, busy = false;
  // "loggedout" | "verify" | "loggedin" (rendered by the server). Anonymous visitors cause no requests until they open the chat.
  var session = panel.getAttribute("data-session") || "loggedout";

  function isOpen() { return panel.classList.contains("is-open"); }

  function post(action, data) {
    var fd = new FormData();
    Object.keys(data || {}).forEach(function (k) { fd.append(k, data[k]); });
    return fetch(api + "/" + action, {
      method: "POST", body: fd, credentials: "same-origin",
      headers: { "X-CSRF-TOKEN": csrf, "X-Requested-With": "fetch", "Accept": "application/json" }
    }).then(function (r) { return r.json(); });
  }

  function showError(form, message) {
    var el = form && form.querySelector("[data-error]");
    if (!el) return;
    el.textContent = message || "";
    el.hidden = !message;
  }

  function setStep(next) {
    if (step === next) return;
    step = next;
    Object.keys(steps).forEach(function (k) { steps[k].hidden = k !== next; });
    compose.hidden = next !== "conversation";
    if (isOpen()) focusStep();
  }

  function focusStep() {
    var target = step === "conversation" ? compose.querySelector("textarea") : steps[step].querySelector("input:not(.hp-field)");
    if (target) target.focus();
  }

  function addMessage(m) {
    if (m.id <= lastId) return;
    lastId = m.id;
    var div = document.createElement("div");
    div.className = "chat-msg " + (m.fromAdmin ? "from-admin" : "from-visitor");
    div.textContent = m.text;            // textContent: never interpret HTML from messages
    var t = document.createElement("time");
    t.textContent = m.time;
    div.appendChild(t);
    list.appendChild(div);
  }

  function scrollDown() { body.scrollTop = body.scrollHeight; }

  function refresh() {
    return fetch(api + "/state?after=" + lastId, { credentials: "same-origin", headers: { "Accept": "application/json" } })
      .then(function (r) { return r.json(); })
      .then(function (d) {
        if (!d.ok) return;
        setStep(d.step);
        if (d.messages && d.messages.length) { d.messages.forEach(addMessage); scrollDown(); }
        if (isOpen() && d.unread > 0) { post("read"); d.unread = 0; }
        if (badge) { badge.hidden = !(d.unread > 0); badge.textContent = d.unread > 9 ? "9+" : String(d.unread || ""); }
      })
      .catch(function () { /* offline: try again later */ });
  }

  function schedule() {
    clearTimeout(timer);
    if (session === "loggedout" && !isOpen() && step !== "conversation") return;
    var delay = isOpen() && step === "conversation" ? 6000 : 60000;
    timer = setTimeout(function () { refresh().then(schedule); }, delay);
  }

  function lock(form, on) {
    busy = on;
    form.querySelectorAll("button, input, textarea").forEach(function (el) { el.disabled = on; });
  }

  // Step 1: name + e-mail
  steps.register.addEventListener("submit", function (e) {
    e.preventDefault();
    if (busy) return;
    var f = steps.register;
    if (!f.checkValidity()) { f.reportValidity(); return; }
    lock(f, true); showError(f, "");
    post("register", { name: f.name.value, email: f.email.value, website: f.website.value })
      .then(function (d) {
        if (d.ok) { setStep("verify"); }
        else showError(f, d.message);
      })
      .catch(function () { showError(f, "…"); })
      .finally(function () { lock(f, false); });
  });

  // Step 2: 6-digit code
  steps.verify.addEventListener("submit", function (e) {
    e.preventDefault();
    if (busy) return;
    var f = steps.verify;
    lock(f, true); showError(f, "");
    post("verify", { code: f.code.value })
      .then(function (d) {
        if (d.ok) { f.reset(); session = "loggedin"; refresh().then(function () { setStep("conversation"); focusStep(); schedule(); }); }
        else showError(f, d.message);
      })
      .finally(function () { lock(f, false); });
  });
  panel.querySelector("[data-chat-resend]").addEventListener("click", function () {
    var f = steps.verify;
    post("resend").then(function (d) { showError(f, d.ok ? "" : d.message); });
  });
  panel.querySelector("[data-chat-change]").addEventListener("click", function () {
    post("reset").then(function () { lastId = 0; list.innerHTML = ""; setStep("register"); });
  });

  // Step 3: conversation
  var textarea = compose.querySelector("textarea");
  compose.addEventListener("submit", function (e) {
    e.preventDefault();
    var text = textarea.value.trim();
    if (!text || busy) return;
    lock(compose, true);
    post("send", { text: text, page: location.pathname })
      .then(function (d) {
        if (d.ok) { textarea.value = ""; textarea.style.height = ""; return refresh(); }
        var err = document.createElement("div");
        err.className = "chat-msg from-admin chat-error";
        err.textContent = d.message || "";
        list.appendChild(err); scrollDown();
      })
      .finally(function () { lock(compose, false); textarea.focus(); schedule(); });
  });
  textarea.addEventListener("keydown", function (e) {
    if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); compose.requestSubmit(); }
  });
  textarea.addEventListener("input", function () {
    textarea.style.height = "auto";
    textarea.style.height = Math.min(textarea.scrollHeight, 120) + "px";
  });

  panel.addEventListener("chat:open", function () {
    refresh().then(function () { focusStep(); scrollDown(); schedule(); });
  });

  setStep(session === "verify" ? "verify" : "register");
  if (session !== "loggedout") refresh().then(schedule);
})();
