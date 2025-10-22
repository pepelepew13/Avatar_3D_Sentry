// wwwroot/js/avatarViewer.js
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

const state = {
  renderer: null,
  scene: null,
  camera: null,
  controls: null,
  root: null,
  clock: new THREE.Clock(),
  raf: 0,
  canvas: null,
  mixer: null,
  mouthTarget: null,   // para simular visemas (escala/rotación sencilla)
  turntableTween: null,
  currentAudio: null,
  disposeFns: [],
  pendingVisemes: [],
  visemeTimer: 0,
  visemeIndex: 0,
};

const BG = {
  oficina:  { clear: 0xf6f7fb, hemi: 0xffffff, ground: 0xeef2f5 },
  moderno:  { clear: 0xf2f3ff, hemi: 0xffffff, ground: 0xe6e6ff },
  naturaleza:{ clear: 0xf6fff7, hemi: 0xffffff, ground: 0xdff5e9 },
  default:  { clear: 0xf7f7f7, hemi: 0xffffff, ground: 0xf0f0f0 }
};

function pickBg(key) { return BG[key?.toLowerCase?.()] ?? BG.default; }

function createRenderer(canvas) {
    const r = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: false, powerPreference: 'high-performance' });
    r.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    r.setSize(canvas.clientWidth, canvas.clientHeight, false);
    r.toneMapping = THREE.ACESFilmicToneMapping;
    r.outputColorSpace = THREE.SRGBColorSpace;
    r.shadowMap.enabled = true;
    r.shadowMap.type = THREE.PCFSoftShadowMap;
    return r;
}

function resize() {
  if (!state.renderer || !state.camera || !state.canvas) return;
  const w = state.canvas.clientWidth || 1;
  const h = state.canvas.clientHeight || 1;
  state.renderer.setSize(w, h, false);
  state.camera.aspect = w / h;
  state.camera.updateProjectionMatrix();
}

function loop() {
  state.raf = requestAnimationFrame(loop);

  // animación de “habla” simple con visemas (oscilar boca/mandíbula)
  const dt = state.clock.getDelta();

  if (state.pendingVisemes.length > 0 && state.mouthTarget) {
    state.visemeTimer -= dt * 1000.0;
    if (state.visemeTimer <= 0) {
      const v = state.pendingVisemes[state.visemeIndex];
      // v.shapeKey, v.tiempo (ms)
      // simulamos abriendo/cerrando “boca”
      const strength = visemeStrength(v?.shapeKey);
      state.mouthTarget.scale.y = THREE.MathUtils.lerp(1.0, 0.7, strength);
      state.mouthTarget.position.y = THREE.MathUtils.lerp(0, -0.03, strength);
      state.visemeTimer = Math.max(1, v?.tiempo ?? 60);
      state.visemeIndex++;
      if (state.visemeIndex >= state.pendingVisemes.length) {
        state.pendingVisemes.length = 0;
        state.visemeIndex = 0;
        // volver a neutro
        state.mouthTarget.scale.y = 1;
        state.mouthTarget.position.y = 0;
      }
    }
  }

  state.controls?.update();
  state.renderer?.render(state.scene, state.camera);
}

function visemeStrength(shapeKey = '') {
  // asigna “fuerza” a algunos visemas conocidos; el resto ~ 0.4
  const s = shapeKey.toLowerCase();
  if (s.includes('aa') || s.includes('ah') || s.includes('ao')) return 0.9;
  if (s.includes('ih') || s.includes('iy') || s.includes('ee')) return 0.5;
  if (s.includes('uh') || s.includes('uw') || s.includes('oo')) return 0.7;
  if (s.includes('sil') || s.includes('rest')) return 0.0;
  return 0.4;
}

async function loadModel(url) {
  // limpia root anterior
  if (state.root) {
    state.scene.remove(state.root);
    disposeDeep(state.root);
    state.root = null;
  }

  const loader = new GLTFLoader();
  const group = new THREE.Group(); // fallback
  state.root = group;

  if (url) {
    try {
      const gltf = await loader.loadAsync(url);
      const root = gltf.scene || gltf.scenes?.[0];
      root.traverse(o => {
        if (o.isMesh) {
          o.castShadow = true;
          o.receiveShadow = true;
        }
      });
      group.add(root);

      // busca un objeto para “boca” si no hay morphs
      state.mouthTarget = findMouth(root) ?? createSimpleMouth(root);
    } catch (e) {
      console.warn('No se pudo cargar GLB:', e);
      // avatar minimalista
      createFallback(group);
      state.mouthTarget = createSimpleMouth(group);
    }
  } else {
    createFallback(group);
    state.mouthTarget = createSimpleMouth(group);
  }

  group.position.set(0, 0, 0);
  state.scene.add(group);
}

function findMouth(root) {
  let mouth = null;
  root.traverse(o => {
    if (mouth) return;
    const n = (o.name || '').toLowerCase();
    if (n.includes('mouth') || n.includes('jaw') || n.includes('lip')) mouth = o;
  });
  return mouth;
}

function createSimpleMouth(root) {
  const geo = new THREE.BoxGeometry(0.18, 0.06, 0.02);
  const mat = new THREE.MeshStandardMaterial({ color: 0x333333, roughness: 0.7, metalness: 0.05 });
  const m = new THREE.Mesh(geo, mat);
  m.position.set(0, 1.6, 0.33);
  root.add(m);
  return m;
}

function createFallback(group) {
  const torsoMat = new THREE.MeshStandardMaterial({ color: 0x223aee, roughness: 0.45, metalness: 0.1 });
  const torso = new THREE.Mesh(new THREE.CapsuleGeometry(0.45, 1.5, 16, 24), torsoMat);
  torso.position.y = 1.35;
  group.add(torso);

  const headMat = new THREE.MeshStandardMaterial({ color: 0xffe0bd, roughness: 0.6, metalness: 0.05 });
  const head = new THREE.Mesh(new THREE.SphereGeometry(0.3, 24, 24), headMat);
  head.position.set(0, 2.2, 0);
  group.add(head);
}

function buildLights(bg) {
  const hemi = new THREE.HemisphereLight(bg.hemi, 0x444444, 0.9);
  const dir1 = new THREE.DirectionalLight(0xffffff, 1.0);
  dir1.position.set(3, 6, 6);
  dir1.castShadow = true;

  const dir2 = new THREE.DirectionalLight(0xffffff, 0.35);
  dir2.position.set(-2.5, 4, -4);

  return [hemi, dir1, dir2];
}

function disposeDeep(obj) {
  obj.traverse((o) => {
    if (o.geometry) o.geometry.dispose?.();
    if (o.material) {
      if (Array.isArray(o.material)) o.material.forEach(m => m.dispose?.());
      else o.material.dispose?.();
    }
  });
}

async function init(canvas, options) {
    await dispose();
  
    state.canvas = canvas;
    state.renderer = createRenderer(canvas);
    const scene = state.scene = new THREE.Scene();
  
    // fondo
    const bg = pickBg(options?.background);
    scene.background = new THREE.Color(bg.clear);
  
    // cámara estilo “character creator”
    const cam = state.camera = new THREE.PerspectiveCamera(35, 1, 0.1, 100);
    cam.position.set(0.9, 1.75, 3.3);
  
    // suelo y grid sutil
    const groundMat = new THREE.MeshStandardMaterial({ color: bg.ground, roughness: 0.95, metalness: 0.02 });
    const ground = new THREE.Mesh(new THREE.CircleGeometry(4.2, 64), groundMat);
    ground.rotation.x = -Math.PI / 2;
    ground.receiveShadow = true;
    scene.add(ground);
  
    const grid = new THREE.GridHelper(8, 32, 0x334, 0x224);
    grid.material.opacity = 0.12; grid.material.transparent = true;
    grid.position.y = 0.001;
    scene.add(grid);
  
    // luces
    buildLights(bg).forEach(l => scene.add(l));
  
    await loadModel(options?.modelUrl);
  
    // Orbit
    const controls = state.controls = new OrbitControls(cam, canvas);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;
    controls.minDistance = 1.15;
    controls.maxDistance = 6.0;
    controls.maxPolarAngle = Math.PI * 0.53;   // evita ir “debajo”
    controls.target.set(0, 1.55, 0);
    controls.update();
  
    const ro = new ResizeObserver(() => resize());
    ro.observe(canvas);
    state.disposeFns.push(() => ro.disconnect());
    resize();
  
    state.clock.start();
    loop();
  
    await updateAppearance(options ?? {});
}

async function updateAppearance(options) {
  // modelo
  if (options?.modelUrl) {
    await loadModel(options.modelUrl);
    state.controls?.update?.();
  }

  // “vestimenta” → tono del torso si es fallback
  if (state.root && options?.outfit) {
    const map = {
      predeterminado: 0xe01e37,
      traje: 0x1f6f8b,
      vestido: 0x8b1f6f,
      corporativo: 0xe01e37,
      ejecutivo: 0x1f6f8b,
      casual: 0x0d6efd
    };
    const color = map[options.outfit?.toLowerCase?.()] ?? 0x223aee;
    state.root.traverse(o => {
      if (o.isMesh && o.material && o.material.color) o.material.color.lerp(new THREE.Color(color), 0.6);
    });
  }

  // “hairColor” → aplicamos a objetos que parezcan cabello
  if (state.root && options?.hairColor != null) {
    const hc = new THREE.Color(options.hairColor);
    state.root.traverse(o => {
      const n = (o.name || '').toLowerCase();
      if (o.isMesh && (n.includes('hair') || n.includes('pelo') || n.includes('cabello'))) {
        if (o.material?.color) o.material.color.set(hc);
      }
    });
  }

  // fondo
  if (options?.background) {
    const bg = pickBg(options.background);
    state.scene.background = new THREE.Color(bg.clear);
  }
}

function frame() {
  state.controls?.target?.set(0, 1.5, 0);
  state.camera?.position.set(0, 1.6, 3.1);
  state.controls?.update?.();
}

function screenshot() {
  if (!state.renderer) return;
  state.renderer.render(state.scene, state.camera);
  const dataURL = state.renderer.domElement.toDataURL('image/png');
  const a = document.createElement('a');
  a.href = dataURL;
  a.download = 'avatar_screenshot.png';
  document.body.appendChild(a);
  a.click();
  a.remove();
}

function turntable(ms = 3000) {
  if (!state.controls || !state.camera) return;
  // rotación suave alrededor del target
  const start = performance.now();
  const startPos = state.camera.position.clone();
  const radius = startPos.distanceTo(state.controls.target);
  const y = startPos.y;

  function step(now) {
    const t = Math.min(1, (now - start) / ms);
    const angle = t * Math.PI * 2;
    state.camera.position.x = state.controls.target.x + Math.sin(angle) * radius;
    state.camera.position.z = state.controls.target.z + Math.cos(angle) * radius;
    state.camera.position.y = y;
    state.camera.lookAt(state.controls.target);
    state.controls.update();

    if (t < 1) state.turntableTween = requestAnimationFrame(step);
  }
  if (state.turntableTween) cancelAnimationFrame(state.turntableTween);
  state.turntableTween = requestAnimationFrame(step);
}

function applyVisemes(list) {
  state.pendingVisemes = Array.isArray(list) ? list.slice(0) : [];
  state.visemeIndex = 0;
  state.visemeTimer = 0;
}

function playTalking(durationSec = 2.0) {
  // Si tu modelo tiene mezclas/animaciones reales, aquí las activarías.
  // Como fallback hacemos un leve “idle” para la cabeza:
  if (!state.root) return;
  const head = findHead(state.root);
  if (!head) return;
  const start = performance.now();
  const maxMs = durationSec * 1000;

  function wobble(now) {
    const t = (now - start);
    const s = Math.sin(t * 0.01) * 0.05;
    head.rotation.y = s;
    head.rotation.x = s * 0.4;

    if (t < maxMs) {
      state.raf = requestAnimationFrame(wobble);
    }
  }
  state.raf = requestAnimationFrame(wobble);
}

function stopTalking() {
  // retorna cabeza a neutro si la tocamos
  if (!state.root) return;
  const head = findHead(state.root);
  if (head) { head.rotation.x = 0; head.rotation.y = 0; }
  state.pendingVisemes.length = 0;
  state.visemeIndex = 0;
  state.visemeTimer = 0;
  if (state.mouthTarget) {
    state.mouthTarget.scale.y = 1;
    state.mouthTarget.position.y = 0;
  }
}

function findHead(root) {
  let head = null;
  root.traverse(o => {
    if (head) return;
    const n = (o.name || '').toLowerCase();
    if (n.includes('head') || n.includes('cabeza')) head = o;
  });
  return head;
}

async function prepareAudioClip(audioElem, url) {
  if (!audioElem) return 0;
  audioElem.src = url;
  await audioElem.load?.();
  await audioElem.play?.().catch(() => {}); // intenta precargar; si bloquea, ignoramos
  audioElem.pause?.();
  return audioElem.duration || 0;
}

function playPreparedAudioClip(audioElem) {
  if (!audioElem) return;
  audioElem.currentTime = 0;
  audioElem.play();
  state.currentAudio = audioElem;
}

function stopAudioClip(audioElem) {
  try {
    audioElem?.pause?.();
    audioElem.currentTime = 0;
  } catch { /* ignore */ }
}

async function dispose() {
  cancelAnimationFrame(state.raf);
  if (state.turntableTween) cancelAnimationFrame(state.turntableTween);
  state.turntableTween = null;

  state.disposeFns.forEach(fn => { try { fn(); } catch {} });
  state.disposeFns.length = 0;

  if (state.root) {
    state.scene?.remove(state.root);
    disposeDeep(state.root);
  }
  state.root = null;

  if (state.renderer) {
    state.renderer.dispose();
  }

  Object.assign(state, {
    renderer: null, scene: null, camera: null, controls: null,
    clock: new THREE.Clock(), raf: 0, canvas: null, mixer: null,
    mouthTarget: null, currentAudio: null, pendingVisemes: [],
    visemeTimer: 0, visemeIndex: 0
  });
}

window.AvatarViewer = {
  init,
  updateAppearance,
  frame,
  screenshot,
  turntable,
  applyVisemes,
  playTalking,
  stopTalking,
  prepareAudioClip,
  playPreparedAudioClip,
  stopAudioClip,
  dispose
};