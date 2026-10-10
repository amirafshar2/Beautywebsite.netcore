/* 3D gallery (test page /{lang}/gallery/3d): the photos float towards the viewer through depth.
   Scrolling bends them like cloth, the one under the pointer waves like a flag, they blur and fade in
   at the far end and out near the camera, then wrap around with the next photos. Autoplay resumes
   3 s after the last interaction. Plain WebGL, no libraries. Click/tap a photo -> normal lightbox.
   No WebGL: the photo links are shown as a simple grid instead. */
(function () {
  "use strict";
  var root = document.querySelector("[data-g3d]");
  if (!root) return;
  var canvas = root.querySelector("[data-g3d-canvas]");
  var links = Array.prototype.slice.call(root.querySelectorAll("[data-g3d-src]"));
  if (!links.length) { root.classList.add("is-fallback"); return; }

  var gl = null;
  try { gl = canvas.getContext("webgl", { antialias: true, alpha: true, premultipliedAlpha: false }); } catch (e) { gl = null; }
  if (!gl) { root.classList.add("is-fallback"); return; }

  var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)");

  // ---- settings (same as the original component) ----
  var VISIBLE = 12, DEPTH = 50, MAX_X = 8, MAX_Y = 8, SPEED = 1.2;
  var FADE_IN = [0.05, 0.25], FADE_OUT = [0.40, 0.43];
  var BLUR_IN = [0.0, 0.1], BLUR_OUT = [0.40, 0.43], MAX_BLUR = 8.0;

  var VERT = [
    "precision highp float;",
    "attribute vec3 position;",
    "uniform mat4 u_proj; uniform mat4 u_model;",
    "uniform float u_scroll; uniform float u_time; uniform float u_hover;",
    "varying vec2 v_uv;",
    "void main(){",
    "  v_uv = position.xy + 0.5;",
    "  vec3 pos = position;",
    "  float k = u_scroll * 0.3;",
    "  float d = length(pos.xy);",
    "  float curve = d * d * k;",                                         // bend with scroll force
    "  float cloth = (sin(pos.x * 2.0 + u_scroll * 3.0) * 0.02 + sin(pos.y * 2.5 + u_scroll * 2.0) * 0.015) * abs(k) * 2.0;",
    "  float flag = 0.0;",
    "  if (u_hover > 0.5) {",                                             // flag wave on hover, free edge on the right
    "    float damp = smoothstep(-0.5, 0.5, pos.x);",
    "    flag = sin(pos.x * 3.0 + u_time * 8.0) * 0.1 * damp + sin(pos.x * 5.0 + u_time * 12.0) * 0.03 * damp;",
    "  }",
    "  pos.z -= curve + cloth + flag;",
    "  gl_Position = u_proj * u_model * vec4(pos, 1.0);",
    "}"
  ].join("\n");

  var FRAG = [
    "precision highp float;",
    "uniform sampler2D u_map; uniform vec2 u_texel;",
    "uniform float u_opacity; uniform float u_blur; uniform float u_scroll;",
    "varying vec2 v_uv;",
    "void main(){",
    "  vec4 color = texture2D(u_map, v_uv);",
    "  if (u_blur > 0.0) {",
    "    vec4 sum = vec4(0.0); float total = 0.0;",
    "    for (float x = -2.0; x <= 2.0; x += 1.0) {",
    "      for (float y = -2.0; y <= 2.0; y += 1.0) {",
    "        float w = 1.0 / (1.0 + length(vec2(x, y)));",
    "        sum += texture2D(u_map, v_uv + vec2(x, y) * u_texel * u_blur) * w; total += w;",
    "      }",
    "    }",
    "    color = sum / total;",
    "  }",
    "  color.rgb += vec3(abs(u_scroll) * 0.005);",
    "  gl_FragColor = vec4(color.rgb, color.a * u_opacity);",
    "}"
  ].join("\n");

  function compile(type, src) {
    var s = gl.createShader(type); gl.shaderSource(s, src); gl.compileShader(s);
    if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(s));
    return s;
  }
  var prog;
  try {
    prog = gl.createProgram();
    gl.attachShader(prog, compile(gl.VERTEX_SHADER, VERT));
    gl.attachShader(prog, compile(gl.FRAGMENT_SHADER, FRAG));
    gl.linkProgram(prog);
    if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(prog));
  } catch (e) { if (window.console) console.warn("gallery3d:", e.message); root.classList.add("is-fallback"); return; }
  gl.useProgram(prog);
  var U = {};
  ["proj", "model", "scroll", "time", "hover", "map", "texel", "opacity", "blur"].forEach(function (n) { U[n] = gl.getUniformLocation(prog, "u_" + n); });

  // ---- plane geometry: 1x1, 32x32 segments (needed for the bending) ----
  var SEG = 32, verts = [], idx = [];
  for (var j = 0; j <= SEG; j++) for (var i = 0; i <= SEG; i++) verts.push(i / SEG - 0.5, j / SEG - 0.5, 0);
  for (j = 0; j < SEG; j++) for (i = 0; i < SEG; i++) {
    var a = j * (SEG + 1) + i, b = a + 1, c = a + SEG + 1, d = c + 1;
    idx.push(a, b, c, b, d, c);
  }
  var vbo = gl.createBuffer();
  gl.bindBuffer(gl.ARRAY_BUFFER, vbo); gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(verts), gl.STATIC_DRAW);
  var ibo = gl.createBuffer();
  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, ibo); gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(idx), gl.STATIC_DRAW);
  var aPos = gl.getAttribLocation(prog, "position");
  gl.enableVertexAttribArray(aPos); gl.vertexAttribPointer(aPos, 3, gl.FLOAT, false, 0, 0);

  gl.enable(gl.BLEND); gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
  gl.disable(gl.DEPTH_TEST);
  gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);

  // ---- textures: uploaded as each photo arrives ----
  var photos = links.map(function (a) { return { src: a.getAttribute("data-g3d-src"), tex: null, w: 1, h: 1, link: a }; });
  var loaded = 0;
  photos.forEach(function (p) {
    var img = new Image();
    img.decoding = "async";
    img.onload = function () {
      p.tex = gl.createTexture();
      gl.bindTexture(gl.TEXTURE_2D, p.tex);
      gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
      gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
      p.w = img.naturalWidth; p.h = img.naturalHeight;
      if (++loaded === 1) root.classList.add("is-ready");
    };
    img.src = p.src;
  });

  // ---- planes: golden-angle spread around the centre ----
  var total = photos.length, planes = [];
  for (i = 0; i < VISIBLE; i++) {
    var ha = (i * 2.618) % (Math.PI * 2), va = (i * 1.618 + Math.PI / 3) % (Math.PI * 2);
    planes.push({
      z: (DEPTH / VISIBLE) * i % DEPTH,
      img: i % total,
      x: Math.sin(ha) * ((i % 3) * 1.2) * MAX_X / 3,
      y: Math.cos(va) * (((i + 1) % 4) * 0.8) * MAX_Y / 4,
      opacity: 0, rect: null
    });
  }
  var advance = (VISIBLE % total) || total;

  // ---- camera ----
  var proj = new Float32Array(16), aspect = 1;
  function perspective(fovDeg, asp, near, far) {
    var f = 1 / Math.tan(fovDeg * Math.PI / 360), nf = 1 / (near - far);
    proj.fill(0);
    proj[0] = f / asp; proj[5] = f; proj[10] = (far + near) * nf; proj[11] = -1; proj[14] = 2 * far * near * nf;
  }
  function resize() {
    var dpr = Math.min(window.devicePixelRatio || 1, 2);
    var w = Math.max(1, Math.round(canvas.clientWidth * dpr)), h = Math.max(1, Math.round(canvas.clientHeight * dpr));
    if (canvas.width !== w || canvas.height !== h) { canvas.width = w; canvas.height = h; }
    gl.viewport(0, 0, w, h);
    aspect = w / h;
    perspective(55, aspect, 0.1, 100);
  }
  if (window.ResizeObserver) new ResizeObserver(resize).observe(canvas); else window.addEventListener("resize", resize);
  resize();

  // Plane corners -> screen rectangle (CSS px), for hover and click.
  function screenRect(x, y, z, sx, sy) {
    var f = proj[5];
    if (-z <= 0.1) return null;
    var cx = (x * f / aspect) / -z, cy = (y * f) / -z, hw = (sx / 2 * f / aspect) / -z, hh = (sy / 2 * f) / -z;
    var W = canvas.clientWidth, H = canvas.clientHeight;
    return { l: (cx - hw + 1) / 2 * W, r: (cx + hw + 1) / 2 * W, t: (1 - (cy + hh)) / 2 * H, b: (1 - (cy - hh)) / 2 * H };
  }

  // ---- input ----
  var velocity = 0, autoplay = true, lastInput = 0, pointer = null, hovered = -1;
  function nudge(v) { velocity += v; autoplay = false; lastInput = performance.now(); }
  canvas.addEventListener("wheel", function (e) {
    e.preventDefault();
    var dy = e.deltaMode === 1 ? e.deltaY * 16 : e.deltaY;
    nudge(dy * 0.01 * SPEED);
  }, { passive: false });
  canvas.addEventListener("keydown", function (e) {
    if (e.key === "ArrowUp" || e.key === "ArrowLeft") { e.preventDefault(); nudge(-2 * SPEED); }
    else if (e.key === "ArrowDown" || e.key === "ArrowRight") { e.preventDefault(); nudge(2 * SPEED); }
    else if ((e.key === "Enter" || e.key === " ") && frontmost() >= 0) { e.preventDefault(); openPlane(frontmost()); }
  });
  var drag = null;
  canvas.addEventListener("pointerdown", function (e) { drag = { y: e.clientY, x: e.clientX, last: e.clientY, moved: 0 }; });
  canvas.addEventListener("pointermove", function (e) {
    var r = canvas.getBoundingClientRect();
    pointer = { x: e.clientX - r.left, y: e.clientY - r.top };
    if (drag && e.pointerType !== "mouse") {
      var dy = drag.last - e.clientY; drag.last = e.clientY; drag.moved += Math.abs(dy);
      nudge(dy * 0.04 * SPEED);
    } else if (drag) drag.moved += Math.abs(e.clientY - drag.last), drag.last = e.clientY;
  });
  canvas.addEventListener("pointerleave", function () { pointer = null; });
  canvas.addEventListener("pointerup", function (e) {
    var wasTap = drag && drag.moved < 8; drag = null;
    if (!wasTap) return;
    var r = canvas.getBoundingClientRect();
    var hit = hitTest(e.clientX - r.left, e.clientY - r.top);
    if (hit >= 0) openPlane(hit);
  });
  canvas.style.touchAction = "none";

  function hitTest(px, py) {
    var best = -1, bestZ = -Infinity;
    planes.forEach(function (p, k) {
      if (!p.rect || p.opacity < 0.35) return;
      if (px >= p.rect.l && px <= p.rect.r && py >= p.rect.t && py <= p.rect.b && p.wz > bestZ) { best = k; bestZ = p.wz; }
    });
    return best;
  }
  function frontmost() {
    var best = -1, bestZ = -Infinity;
    planes.forEach(function (p, k) { if (p.opacity > 0.6 && p.wz > bestZ) { best = k; bestZ = p.wz; } });
    return best;
  }
  function openPlane(k) {
    var link = photos[planes[k].img].link;
    link.click(); // gallery.js opens its lightbox for [data-lightbox] links
  }

  // ---- loop ----
  function range(v, a, b) { return (v - a) / (b - a); }
  var model = new Float32Array(16), start = performance.now(), prev = start, raf = 0, running = true;
  function frame(now) {
    var dt = Math.min((now - prev) / 1000, 0.1); prev = now;
    var t = (now - start) / 1000;
    if (!autoplay && now - lastInput > 3000) autoplay = true;
    if (autoplay && !(reduce && reduce.matches)) velocity += 0.3 * dt;
    velocity *= Math.pow(0.95, dt * 60);

    resize();
    gl.clearColor(0, 0, 0, 0); gl.clear(gl.COLOR_BUFFER_BIT);
    gl.uniformMatrix4fv(U.proj, false, proj);
    gl.uniform1f(U.time, t);
    gl.uniform1f(U.scroll, velocity);
    hovered = pointer && !drag ? hitTest(pointer.x, pointer.y) : -1;
    canvas.style.cursor = hovered >= 0 ? "pointer" : "";

    // move + wrap
    planes.forEach(function (p) {
      var nz = p.z + velocity * dt * 10;
      if (nz >= DEPTH) { var f = Math.floor(nz / DEPTH); nz -= DEPTH * f; p.img = (p.img + f * advance) % total; }
      else if (nz < 0) { var bw = Math.ceil(-nz / DEPTH); nz += DEPTH * bw; p.img = (((p.img - bw * advance) % total) + total) % total; }
      p.z = nz; p.wz = p.z - DEPTH / 2;
      var n = p.z / DEPTH;
      var op = n < FADE_IN[0] ? 0 : n <= FADE_IN[1] ? range(n, FADE_IN[0], FADE_IN[1]) : n < FADE_OUT[0] ? 1 : n <= FADE_OUT[1] ? 1 - range(n, FADE_OUT[0], FADE_OUT[1]) : 0;
      var bl = n < BLUR_IN[0] ? MAX_BLUR : n <= BLUR_IN[1] ? MAX_BLUR * (1 - range(n, BLUR_IN[0], BLUR_IN[1])) : n < BLUR_OUT[0] ? 0 : n <= BLUR_OUT[1] ? MAX_BLUR * range(n, BLUR_OUT[0], BLUR_OUT[1]) : MAX_BLUR;
      p.opacity = Math.max(0, Math.min(1, op)); p.blur = Math.max(0, Math.min(MAX_BLUR, bl));
    });

    // far to near, so transparency blends correctly
    var order = planes.map(function (p, k) { return k; }).sort(function (a, b) { return planes[a].wz - planes[b].wz; });
    order.forEach(function (k) {
      var p = planes[k], ph = photos[p.img];
      p.rect = null;
      if (!ph.tex || p.opacity <= 0.001) return;
      var asp = ph.w / ph.h, sx = asp > 1 ? 2 * asp : 2, sy = asp > 1 ? 2 : 2 / asp;
      p.rect = screenRect(p.x, p.y, p.wz, sx, sy);
      model.fill(0); model[0] = sx; model[5] = sy; model[10] = 1; model[12] = p.x; model[13] = p.y; model[14] = p.wz; model[15] = 1;
      gl.uniformMatrix4fv(U.model, false, model);
      gl.uniform1f(U.opacity, p.opacity);
      gl.uniform1f(U.blur, p.blur);
      gl.uniform1f(U.hover, k === hovered ? 1 : 0);
      gl.uniform2f(U.texel, 1 / ph.w, 1 / ph.h);
      gl.activeTexture(gl.TEXTURE0); gl.bindTexture(gl.TEXTURE_2D, ph.tex); gl.uniform1i(U.map, 0);
      gl.drawElements(gl.TRIANGLES, idx.length, gl.UNSIGNED_SHORT, 0);
    });
    if (running) raf = requestAnimationFrame(frame);
  }
  raf = requestAnimationFrame(frame);

  // Stop drawing while the tab is hidden or the gallery is off screen.
  function setRunning(on) {
    if (on === running) return;
    running = on;
    if (on) { prev = performance.now(); raf = requestAnimationFrame(frame); } else cancelAnimationFrame(raf);
  }
  var visible = true;
  document.addEventListener("visibilitychange", function () { setRunning(!document.hidden && visible); });
  if (window.IntersectionObserver) new IntersectionObserver(function (entries) {
    visible = entries[0].isIntersecting; setRunning(visible && !document.hidden);
  }).observe(root);
  canvas.addEventListener("webglcontextlost", function (e) { e.preventDefault(); setRunning(false); root.classList.add("is-fallback"); });
})();
