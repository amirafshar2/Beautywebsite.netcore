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

  /* ---------- Morph: images dissolve into each other through noise (WebGL) ----------
     One full-screen shader pass over two frames. An fbm noise field gives every pixel a
     threshold and the progress sweeps past it, so the old picture tears away in drifting
     tatters; the threshold is biased by the brightness of the new picture, so its light
     areas burn through first. Each frame is the image drawn exactly where the <img> sits,
     on a transparent background, so opening dissolves the photo in over the dark backdrop.
     No WebGL / reduced motion: plain swap, exactly as before. */
  function createMorph(host) {
    var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)");
    var canvas = document.createElement("canvas");
    canvas.className = "lb-morph"; canvas.setAttribute("aria-hidden", "true");
    var gl = null;
    try { gl = canvas.getContext("webgl", { alpha: true, premultipliedAlpha: true, antialias: false }); } catch (e) { gl = null; }
    if (!gl) return null;

    var VERT = "attribute vec2 a_position;varying vec2 v_uv;void main(){v_uv=a_position*0.5+0.5;gl_Position=vec4(a_position,0.0,1.0);}";
    var FRAG = [
      "precision highp float;",
      "uniform sampler2D u_from;uniform sampler2D u_to;uniform float u_progress;uniform float u_scale;",
      "uniform float u_direction;uniform float u_edge;uniform float u_drift;varying vec2 v_uv;",
      "vec3 permute(vec3 x){return mod(((x*34.0)+1.0)*x,289.0);}",
      "float snoise(vec2 v){const vec4 C=vec4(0.211324865405187,0.366025403784439,-0.577350269189626,0.024390243902439);",
      "vec2 i=floor(v+dot(v,C.yy));vec2 x0=v-i+dot(i,C.xx);vec2 i1=(x0.x>x0.y)?vec2(1.0,0.0):vec2(0.0,1.0);",
      "vec4 x12=x0.xyxy+C.xxzz;x12.xy-=i1;i=mod(i,289.0);",
      "vec3 p=permute(permute(i.y+vec3(0.0,i1.y,1.0))+i.x+vec3(0.0,i1.x,1.0));",
      "vec3 m=max(0.5-vec3(dot(x0,x0),dot(x12.xy,x12.xy),dot(x12.zw,x12.zw)),0.0);m=m*m;m=m*m;",
      "vec3 x=2.0*fract(p*C.www)-1.0;vec3 h=abs(x)-0.5;vec3 ox=floor(x+0.5);vec3 a0=x-ox;",
      "m*=1.79284291400159-0.85373472095314*(a0*a0+h*h);vec3 g;g.x=a0.x*x0.x+h.x*x0.y;g.yz=a0.yz*x12.xz+h.yz*x12.yw;",
      "return 130.0*dot(m,g);}",
      "float fbm(vec2 v){float value=0.0;float amp=0.5;for(int i=0;i<5;i++){value+=amp*snoise(v);v*=2.0;amp*=0.5;}return value;}",
      "void main(){",
      "float adjusted=u_progress*(1.0+2.0*u_edge)-u_edge;",
      // fbm spread over 0..1, so the front really sweeps across the picture
      "float n=clamp((fbm(v_uv*u_scale+vec2(0.0,u_progress*u_direction))*0.5+0.5-0.18)/0.64,0.0,1.0);",
      // light areas of the new picture cross first (burn through); kept small so bright photos still tear
      "float lum=dot(texture2D(u_to,v_uv).rgb,vec3(0.299,0.587,0.114));",
      "float noise=clamp(n*0.78+(1.0-lum)*0.22,0.0,1.0);",
      "float mixFactor=1.0-smoothstep(adjusted-u_edge,adjusted+u_edge,noise);",
      // tatters drift against each other; centred on 0 so the picture as a whole stays put
      "float d=(noise-0.5)*u_drift*u_direction;",
      "vec2 fromUV=v_uv+vec2(0.0,d*u_progress);",
      "vec2 toUV=v_uv-vec2(0.0,d*(1.0-u_progress)*0.5);",
      "gl_FragColor=mix(texture2D(u_from,fromUV),texture2D(u_to,toUV),mixFactor);}"
    ].join("\n");

    function compile(type, src) {
      var sh = gl.createShader(type); gl.shaderSource(sh, src); gl.compileShader(sh);
      if (!gl.getShaderParameter(sh, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(sh));
      return sh;
    }
    var program, u = {};
    try {
      program = gl.createProgram();
      gl.attachShader(program, compile(gl.VERTEX_SHADER, VERT));
      gl.attachShader(program, compile(gl.FRAGMENT_SHADER, FRAG));
      gl.linkProgram(program);
      if (!gl.getProgramParameter(program, gl.LINK_STATUS)) throw new Error("link");
    } catch (e) { return null; }
    gl.useProgram(program);
    var buf = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buf);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
    var loc = gl.getAttribLocation(program, "a_position");
    gl.enableVertexAttribArray(loc); gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);
    ["from", "to", "progress", "scale", "direction", "edge", "drift"].forEach(function (n) { u[n] = gl.getUniformLocation(program, "u_" + n); });
    gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
    gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, true);
    var texFrom = gl.createTexture(), texTo = gl.createTexture();
    [texFrom, texTo].forEach(function (t) {
      gl.bindTexture(gl.TEXTURE_2D, t);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    });
    host.appendChild(canvas);

    var lost = false;
    canvas.addEventListener("webglcontextlost", function (e) { e.preventDefault(); lost = true; });

    var raf = 0, lastFrame = null; // snapshot of the picture now on screen (the <img> itself changes src)
    // The image drawn where it really sits on screen, on a transparent frame the size of the box.
    function frame(entry, w, h, dpr, hostRect) {
      var c = document.createElement("canvas"); c.width = w; c.height = h;
      if (entry) {
        var g = c.getContext("2d"), r = entry.rect;
        g.drawImage(entry.img, (r.left - hostRect.left) * dpr, (r.top - hostRect.top) * dpr, r.width * dpr, r.height * dpr);
      }
      return c;
    }
    function upload(tex, c) {
      gl.bindTexture(gl.TEXTURE_2D, tex);
      gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, c);
    }
    function ease(t) { t = Math.min(Math.max(t, 0), 1); return t < 0.5 ? 16 * Math.pow(t, 5) : 1 - Math.pow(-2 * t + 2, 5) / 2; }

    return {
      // Plays old -> new over the real <img>; calls done() when the <img> may show again.
      play: function (imgEl, direction, done) {
        var next = { img: imgEl, rect: imgEl.getBoundingClientRect() };
        cancelAnimationFrame(raf);
        var hostRect = host.getBoundingClientRect();
        var dpr = Math.min(window.devicePixelRatio || 1, 2);
        var w = Math.max(1, Math.round(hostRect.width * dpr)), h = Math.max(1, Math.round(hostRect.height * dpr));
        var from = lastFrame && lastFrame.width === w && lastFrame.height === h ? lastFrame : null;
        var to;
        try { to = frame(next, w, h, dpr, hostRect); } catch (e) { to = null; }
        lastFrame = to;
        if (!to || lost || (reduce && reduce.matches) || !next.rect.width) { canvas.style.opacity = "0"; done(); return; }
        canvas.width = w; canvas.height = h; gl.viewport(0, 0, w, h);
        try {
          upload(texFrom, from || frame(null, w, h, dpr, hostRect));
          upload(texTo, to);
        } catch (e) { canvas.style.opacity = "0"; done(); return; }
        gl.activeTexture(gl.TEXTURE0); gl.bindTexture(gl.TEXTURE_2D, texFrom); gl.uniform1i(u.from, 0);
        gl.activeTexture(gl.TEXTURE1); gl.bindTexture(gl.TEXTURE_2D, texTo); gl.uniform1i(u.to, 1);
        gl.uniform1f(u.scale, 3.5); gl.uniform1f(u.edge, 0.15); gl.uniform1f(u.drift, 0.06);
        gl.uniform1f(u.direction, direction < 0 ? -1 : 1);
        var duration = from ? 1400 : 1100, start = performance.now();
        canvas.style.opacity = "1";
        (function tick() {
          var p = ease((performance.now() - start) / duration);
          gl.clearColor(0, 0, 0, 0); gl.clear(gl.COLOR_BUFFER_BIT);
          gl.uniform1f(u.progress, p);
          gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
          if (p < 1) raf = requestAnimationFrame(tick);
          else { done(); requestAnimationFrame(function () { canvas.style.opacity = "0"; }); }
        })();
      },
      reset: function () { cancelAnimationFrame(raf); lastFrame = null; canvas.style.opacity = "0"; }
    };
  }

  var img = box.querySelector("[data-lb-img]");
  var morph = createMorph(box);
  var cap = box.querySelector("[data-lb-caption]");
  var links = [], index = 0, opener = null;

  function visibleLinks() {
    return Array.prototype.filter.call(document.querySelectorAll("[data-lightbox]"), function (a) {
      return !a.closest("[hidden]");
    });
  }
  var showToken = 0;
  function show(i, direction) {
    if (!links.length) return;
    index = (i + links.length) % links.length;
    var a = links[index];
    var src = a.getAttribute("data-full") || a.href;
    var c = a.getAttribute("data-caption") || "";
    cap.textContent = c; cap.hidden = !c;
    if (!morph) { img.src = src; img.alt = c; return; }
    // Load the next picture off-screen first, then morph from what is shown now.
    var token = ++showToken;
    var pre = new Image();
    pre.decoding = "async";
    pre.onload = pre.onerror = function () {
      if (token !== showToken || box.hidden) return;
      img.classList.add("is-morphing");
      img.src = src; img.alt = c;
      var go = function () {
        if (token !== showToken) return;
        morph.play(img, direction || 1, function () { if (token === showToken) img.classList.remove("is-morphing"); });
      };
      if (img.complete && img.naturalWidth) go(); else { img.onload = go; img.onerror = go; }
    };
    pre.src = src;
  }
  function open(a) {
    links = visibleLinks(); opener = a;
    box.hidden = false;
    show(links.indexOf(a), 1);
    document.body.classList.add("menu-open");
    box.querySelector("[data-lb-close]").focus();
  }
  function close() {
    showToken++;
    if (morph) morph.reset();
    img.classList.remove("is-morphing");
    box.hidden = true; img.removeAttribute("src");
    document.body.classList.remove("menu-open");
    if (opener) opener.focus();
  }
  function next() { show(index + 1, 1); }
  function prev() { show(index - 1, -1); }

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
