// === IMPORTS ===
import * as THREE from 'three';
import { GLTFLoader }   from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

// ======= Paletas =======
const outfitPalettes = {
  corporativo: { color: '#1c8f6d', emissive: '#174d3c' },
  ejecutivo:   { color: '#1b4332', emissive: '#0f241c' },
  casual:      { color: '#0d6efd', emissive: '#11284a' }
};

const backgroundPalettes = {
  oficina:    { light: '#f7f8fb', ground: '#f2f6f9' },
  moderno:    { light: '#f2f3ff', ground: '#e1e0ff' },
  naturaleza: { light: '#f6fff7', ground: '#d6f5e3' },
  default:    { light: '#ffffff', ground: '#f0f0f0' }
};

const defaultOutfit = outfitPalettes.corporativo;
const VISEME_KEYS = ['viseme_aa','viseme_E','viseme_I','viseme_O','viseme_U','viseme_SS','viseme_RR','viseme_kk'];

// ======= FACTORÍA =======
export async function createViewer(canvas, options) {
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true, preserveDrawingBuffer: true });
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.setPixelRatio(window.devicePixelRatio ?? 1);
  renderer.shadowMap.enabled = true;

  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(35, 1, 0.1, 100);

  const CAM_BASE = { x: 0, y: 1.6, z: 3.2 };
  setCam(camera, CAM_BASE);

  const hemiLight = new THREE.HemisphereLight(0xffffff, 0x3f3f3f, 1.0);
  scene.add(hemiLight);

  const keyLight = new THREE.DirectionalLight(0xffffff, 1.15);
  keyLight.position.set(3, 6, 6);
  keyLight.castShadow = true;
  keyLight.shadow.mapSize.set(2048, 2048);
  scene.add(keyLight);

  const fillLight = new THREE.DirectionalLight(0xffffff, 0.35);
  fillLight.position.set(-2.5, 4, -4);
  scene.add(fillLight);

  const groundMaterial = new THREE.MeshStandardMaterial({
    color: backgroundPalettes.default.ground, roughness: 0.9, metalness: 0.05
  });
  const ground = new THREE.Mesh(new THREE.CircleGeometry(3, 48), groundMaterial);
  ground.rotation.x = -Math.PI / 2; ground.position.y = 0; ground.receiveShadow = true;
  scene.add(ground);

  // Orbit controls
  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true; controls.dampingFactor = 0.06;
  controls.minDistance = 1.2; controls.maxDistance = 6.5;
  controls.target.set(0, 1.5, 0);
  controls.autoRotate = false; controls.autoRotateSpeed = 0.8;

  const loader = new GLTFLoader(); loader.setCrossOrigin('anonymous');

  const state = {
    renderer, scene, camera, canvas, keyLight, groundMaterial, ground,
    controls,
    outfitMaterials: new Set(),
    logoMaterial: null, logoTexture: null,
    backgroundTexture: null,
    resizeObserver: null, animationHandle: 0,
    root: null, clock: new THREE.Clock(),
    mouthMeshes: [], audio: null, audioEl: options?.audioEl ?? null, mouthTimer: 0, currentVisemeTimeout: 0,
    dragHandlers: [], _keyHandler: null,
    mixer: null, talkingAction: null, isTalking: false
  };

  // === Cargar modelo ===
  let root;
  if (options?.modelUrl) {
    try {
      const gltf = await loader.loadAsync(options.modelUrl);
      root = gltf.scene;
      root.traverse(child => {
        if (!child.isMesh) return;
        child.castShadow = true; child.receiveShadow = true;

        const lower = (child.name ?? '').toLowerCase();
        if (/(shirt|cloth|outfit|jacket|torso|body)/.test(lower)) collectMaterial(state.outfitMaterials, child.material);

        if (child.morphTargetDictionary && typeof child.morphTargetDictionary === 'object') {
          const dict = child.morphTargetDictionary;
          const hits = VISEME_KEYS.filter(k => dict[k] !== undefined);
          if (hits.length > 0) state.mouthMeshes.push(child);
        }
      });

      if (gltf.animations?.length){
        state.mixer = new THREE.AnimationMixer(root);
        const talkingClip = gltf.animations.find(a => /talkinganimation/i.test(a.name) || /talk/i.test(a.name));
        if (talkingClip) {
          state.talkingAction = state.mixer.clipAction(talkingClip);
          state.talkingAction.loop = THREE.LoopRepeat;
        }
      }
    } catch { /* fallback */ }
  }
  if (!root) root = createFallbackAvatar(state.outfitMaterials);

  state.logoMaterial = ensureLogoPlaneOrFind(root);
  state.logoMaterial.transparent = true; state.logoMaterial.opacity = 0;

  root.position.y = 0; scene.add(root); state.root = root;

  renderer.setClearColor(0x000000, 0);
  resizeRenderer(state);
  const resizeObserver = new ResizeObserver(() => resizeRenderer(state));
  resizeObserver.observe(canvas);
  state.resizeObserver = resizeObserver;

  // Drag&drop de imagen para logo
  setupDragAndDrop(state);

  // Keybindings
  const onKey = (e)=>{
    if (e.target && /input|textarea|select/i.test(e.target.tagName)) return;
    const k = e.key.toLowerCase();
    if (k==='1') setPreset(state, 'full');
    if (k==='2') setPreset(state, 'waist');
    if (k==='3') setPreset(state, 'chest');
    if (k==='4') setPreset(state, 'head');
    if (k==='r') { controls.reset(); setCam(camera, CAM_BASE); controls.target.set(0,1.5,0); controls.update(); }
    if (k==='a') controls.autoRotate = !controls.autoRotate;
    if (k==='l') keyLight.intensity = keyLight.intensity > 0.05 ? 0.0 : 1.15;
    if (k==='g') ground.visible = !ground.visible;
    if (k==='s') downloadDataUrl(`avatar_${Date.now()}.png`, capture(renderer, 1));
  };
  window.addEventListener('keydown', onKey);
  state._keyHandler = onKey;

  const renderLoop = () => {
    state.animationHandle = window.requestAnimationFrame(renderLoop);
    const delta = state.clock.getDelta();

    if (state.mixer) state.mixer.update(delta);

    if (state.mouthTimer > 0 && !hasMorphTargets(state)) {
      state.root.rotation.x = Math.sin(performance.now() * 0.02) * 0.04;
      state.mouthTimer -= delta;
      if (state.mouthTimer <= 0) state.root.rotation.x = 0;
    }

    state.controls.update();
    renderer.render(scene, camera);
  };
  renderLoop();

  // Aplicar estado inicial
  if (options?.logoUrl) await applyLogo(state, options.logoUrl);
  applyOutfit(state, options?.outfit ?? null);
  await applyBackground(state, options?.background ?? null);
  if (options?.hairColor) applyHairColor(state, options.hairColor);

  // ===== API expuesta a .NET =====
  return {
    setLogo: (value) => applyLogo(state, value),
    setOutfit: (value) => applyOutfit(state, value),
    setBackground: (value) => applyBackground(state, value),
    setHairColor: (value) => applyHairColor(state, value),
    setAutoRotate: (on) => { state.controls.autoRotate = !!on; },
    resetCamera: () => { controls.reset(); setCam(camera, CAM_BASE); controls.target.set(0,1.5,0); controls.update(); },
    frame: () => { setCam(camera, CAM_BASE); controls.target.set(0,1.5,0); controls.update(); },
    turntable: (ms=3000) => { state.controls.autoRotate = true; setTimeout(()=> state.controls.autoRotate = false, ms); },
    setCameraPreset: (p) => setPreset(state, p),
    toggleGround: () => { state.ground.visible = !state.ground.visible; },
    toggleLight:  () => { state.keyLight.intensity = state.keyLight.intensity > 0.05 ? 0.0 : 1.15; },
    speak: (audioUrl, visemas) => playSpeech(state, audioUrl, visemas),
    screenshot: (scale=1) => capture(renderer, scale),
    dispose: () => disposeViewer(state)
  };
}

// ======= util =======
function setCam(cam, {x,y,z}){ cam.position.set(x,y,z); cam.lookAt(0,1.5,0); cam.updateProjectionMatrix(); }
function hasMorphTargets(state){ return state.mouthMeshes.length > 0; }

function collectMaterial(set, material){
  if (!material) return;
  if (Array.isArray(material)){ material.forEach(m => collectMaterial(set, m)); return; }
  set.add(material);
}

function ensureLogoPlaneOrFind(root){
  let material = null;
  root.traverse(child=>{
    if (material) return;
    if (!child.isMesh) return;
    const lower = (child.name ?? '').toLowerCase();
    if (/(logo|badge|emblem|logolabel)/.test(lower)){
      material = ensureSingleMaterial(child);
    }
  });
  if (!material) material = createLogoPlane(root);
  return material;
}

function ensureSingleMaterial(mesh){
  if (!mesh.material){
    mesh.material = new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0 });
    return mesh.material;
  }
  if (Array.isArray(mesh.material)){
    const m = mesh.material[0]; mesh.material = m; return m;
  }
  return mesh.material;
}

function applyHairColor(state, value){
  if (!value || !state?.root) return;
  const color = new THREE.Color(value);
  state.root.traverse(child => {
    const n = (child.name || '').toLowerCase();
    if (child.isMesh && /hair|pelo|cabello/.test(n)){
      if (child.material?.color){
        child.material.color.set(color);
        child.material.needsUpdate = true;
      }
    }
  });
}

function createLogoPlane(root){
  const material = new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0 });
  const plane = new THREE.Mesh(new THREE.PlaneGeometry(0.55, 0.55), material);
  plane.position.set(0, 1.55, 0.52);
  root.add(plane);
  return material;
}

function createFallbackAvatar(outfitMaterials){
  const group = new THREE.Group();
  const bodyMaterial = new THREE.MeshStandardMaterial({ color: defaultOutfit.color, roughness: 0.55, metalness: 0.1 });
  const body = new THREE.Mesh(new THREE.CapsuleGeometry(0.45, 1.6, 24, 32), bodyMaterial);
  body.position.y = 1.4; body.castShadow = true; body.receiveShadow = true;
  group.add(body); outfitMaterials.add(bodyMaterial);
  const headMaterial = new THREE.MeshStandardMaterial({ color: 0xffe0bd, roughness: 0.6, metalness: 0.1 });
  const head = new THREE.Mesh(new THREE.SphereGeometry(0.32, 32, 32), headMaterial);
  head.position.set(0, 2.35, 0); head.castShadow = true; head.receiveShadow = true;
  group.add(head);
  return group;
}

async function applyLogo(state, url){
  if (!state.logoMaterial) return;
  if (state.logoTexture){ state.logoTexture.dispose(); state.logoTexture = null; }

  if (!url){ state.logoMaterial.map = null; state.logoMaterial.opacity = 0; state.logoMaterial.needsUpdate = true; return; }

  const loader = new THREE.TextureLoader(); loader.setCrossOrigin('anonymous');
  try{
    const texture = await loader.loadAsync(url);
    texture.colorSpace = THREE.SRGBColorSpace;
    texture.anisotropy = Math.min(8, state.renderer.capabilities.getMaxAnisotropy?.() ?? 4);
    state.logoMaterial.map = texture; state.logoMaterial.opacity = 1; state.logoMaterial.needsUpdate = true;
    state.logoTexture = texture;
  }catch{
    state.logoMaterial.map = null; state.logoMaterial.opacity = 0; state.logoMaterial.needsUpdate = true;
  }
}

function applyOutfit(state, value){
  const key = (value ?? '').toString().toLowerCase();
  const palette = outfitPalettes[key] ?? defaultOutfit;
  state.outfitMaterials.forEach(m=>{
    if (!m) return;
    if (m.color) m.color.set(palette.color);
    if (m.emissive && palette.emissive) m.emissive.set(palette.emissive);
    m.needsUpdate = true;
  });
}

async function applyBackground(state, value){
  const raw = (value ?? '').toString();
  const key = raw.toLowerCase();

  if (isImageValue(raw) && !backgroundPalettes[key]) {
    await setBackgroundImage(state, raw);
    return;
  }

  clearBackgroundImage(state);
  const palette = backgroundPalettes[key] ?? backgroundPalettes.default;
  state.keyLight.color.set(palette.light);
  if (state.groundMaterial?.color){ state.groundMaterial.color.set(palette.ground); state.groundMaterial.needsUpdate = true; }
}

function resizeRenderer(state){
  const canvas = state.canvas; if (!canvas) return;
  const w = canvas.clientWidth, h = canvas.clientHeight; if (!w || !h) return;
  state.renderer.setSize(w, h, false); state.camera.aspect = w/h; state.camera.updateProjectionMatrix();
}

// ======= Cámara: presets =======
function setPreset(state, preset){
  const cam = state.camera, controls = state.controls;
  switch ((preset||'').toString()){
    case 'head':  setCam(cam, {x:0.0, y:1.7, z:1.1}); controls.target.set(0,1.7,0); break;
    case 'chest': setCam(cam, {x:0.0, y:1.6, z:1.6}); controls.target.set(0,1.5,0); break;
    case 'waist': setCam(cam, {x:0.0, y:1.5, z:2.2}); controls.target.set(0,1.45,0); break;
    default:      setCam(cam, {x:0.0, y:1.6, z:3.2}); controls.target.set(0,1.5,0); break;
  }
  controls.update();
}

// ======= Hablar =======
async function playSpeech(state, audioUrl, visemas){
  stopMouth(state);

  const audio = state.audioEl ?? new Audio();
  audio.crossOrigin = 'anonymous';
  audio.src = audioUrl;
  audio.load();
  state.audio = audio;

  const list = Array.isArray(visemas) ? visemas.slice().sort((a,b)=> (a?.tiempo ?? 0) - (b?.tiempo ?? 0)) : [];

  audio.addEventListener('play', () => {
    setTalking(state, true);
    if (hasMorphTargets(state)){
      scheduleMorphs(state, list, audio);
    }else{
      state.mouthTimer = Math.max(0.6, audio.duration || 2);
    }
  }, { once:true });
  audio.addEventListener('ended', ()=> stopMouth(state));
  await audio.play().catch(()=>{ /* autoplay bloqueado */ });
}

function scheduleMorphs(state, visemas, audio){
  if (!visemas?.length) return;
  state.mouthMeshes.forEach(mesh=>{
    const dict = mesh.morphTargetDictionary;
    const inf  = mesh.morphTargetInfluences ?? [];
    VISEME_KEYS.forEach(k => { if (dict[k] !== undefined) inf[dict[k]] = 0; });
  });

  let i = 0;
  const tick = ()=>{
    if (i >= visemas.length) return;
    const tNow = audio.currentTime * 1000;
    const tNext = visemas[i].tiempo ?? 0;
    if (tNow >= tNext - 30){
      applyMorph(state, visemas[i].shapeKey);
      i++;
    }
    if (!audio.paused) requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
}

function applyMorph(state, key){
  if (!key) return;
  state.mouthMeshes.forEach(mesh=>{
    const dict = mesh.morphTargetDictionary;
    const inf  = mesh.morphTargetInfluences ?? [];
    VISEME_KEYS.forEach(k => { if (dict[k] !== undefined) inf[dict[k]] = 0; });
    if (dict[key] !== undefined){
      const idx = dict[key];
      inf[idx] = 0.85;
      setTimeout(()=>{ if (inf[idx] > 0) inf[idx] *= 0.35; }, 80);
      setTimeout(()=>{ if (inf[idx] > 0) inf[idx] *= 0.15; }, 140);
      setTimeout(()=>{ if (inf[idx] > 0) inf[idx] = 0; }, 220);
    }
  });
}

function stopMouth(state){
  state.mouthTimer = 0;
  setTalking(state, false);
  if (state.mouthMeshes.length){
    state.mouthMeshes.forEach(mesh=>{
      const dict = mesh.morphTargetDictionary;
      const inf  = mesh.morphTargetInfluences ?? [];
      VISEME_KEYS.forEach(k => { if (dict[k] !== undefined) inf[dict[k]] = 0; });
    });
  }
  if (state.audio){
    try{
      state.audio.pause();
      state.audio.currentTime = 0;
      if (!state.audioEl) state.audio.src = '';
    }catch{}
    state.audio = null;
  }
}

// ======= Captura =======
function capture(renderer, scale=1){
  return renderer.domElement.toDataURL('image/png');
}

// ======= Drag & Drop (logo) =======
function setupDragAndDrop(state){
  const el = state.canvas;
  const host = el.parentElement;
  const prevent = e => { e.preventDefault(); e.stopPropagation(); };

  const over  = e => { prevent(e); if (host) host.classList.add('is-dragover'); };
  const leave = e => { prevent(e); if (host) host.classList.remove('is-dragover'); };
  const drop  = e => {
    prevent(e); if (host) host.classList.remove('is-dragover');
    const f = e.dataTransfer?.files?.[0]; if (!f) return;
    if (!/^image\//i.test(f.type)) return;
    const url = URL.createObjectURL(f);
    applyLogo(state, url);
  };

  el.addEventListener('dragenter', over);
  el.addEventListener('dragover',  over);
  el.addEventListener('dragleave', leave);
  el.addEventListener('drop',      drop);

  state.dragHandlers.push(['dragenter', over], ['dragover', over], ['dragleave', leave], ['drop', drop]);
}

async function disposeViewer(state){
  if (state.animationHandle) window.cancelAnimationFrame(state.animationHandle);
  if (state.resizeObserver) state.resizeObserver.disconnect();
  if (state.logoTexture) state.logoTexture.dispose();
  if (state.backgroundTexture) state.backgroundTexture.dispose();
  stopMouth(state);

  if (state.dragHandlers?.length){
    for (const [evt, fn] of state.dragHandlers) state.canvas.removeEventListener(evt, fn);
    state.dragHandlers.length = 0;
  }
  if (state._keyHandler){ window.removeEventListener('keydown', state._keyHandler); state._keyHandler = null; }

  state.controls?.dispose?.();
  state.mixer?.stopAllAction?.();
  state.renderer.dispose();
}

function setTalking(state, on){
  if (!state.talkingAction) return;
  if (on && !state.isTalking){
    state.talkingAction.reset();
    state.talkingAction.fadeIn(0.12);
    state.talkingAction.play();
    state.isTalking = true;
  }else if (!on && state.isTalking){
    state.talkingAction.fadeOut(0.2);
    state.isTalking = false;
  }
}

function isImageValue(value){
  if (!value) return false;
  return /^(https?:)?\/\//i.test(value)
    || /\.(png|jpe?g|webp|gif)$/i.test(value)
    || value.includes('/');
}

function clearBackgroundImage(state){
  if (state.backgroundTexture){
    state.backgroundTexture.dispose();
    state.backgroundTexture = null;
  }
  state.scene.background = null;
}

async function setBackgroundImage(state, url){
  clearBackgroundImage(state);
  const loader = new THREE.TextureLoader(); loader.setCrossOrigin('anonymous');
  try{
    const texture = await loader.loadAsync(url);
    texture.colorSpace = THREE.SRGBColorSpace;
    state.scene.background = texture;
    state.backgroundTexture = texture;
  }catch{
    state.scene.background = null;
  }
}

// ======= Helper para descargar desde .NET =======
export function downloadDataUrl(filename, dataUrl){
  try{
    const a = document.createElement('a');
    a.href = dataUrl; a.download = filename || 'captura.png';
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
  }catch{}
}

let _viewerInstance = null;
let _canvas = null;
let _lastOptions = {};

export async function init(canvas, options){
  _canvas = canvas;
  _lastOptions = { ...options };
  if (_viewerInstance) await _viewerInstance.dispose?.();
  _viewerInstance = await createViewer(canvas, _lastOptions);
}

export async function updateAppearance(options){
  if (!_canvas) return;
  const prevModel = _lastOptions?.modelUrl;
  _lastOptions = { ..._lastOptions, ...options };

  if (options?.modelUrl && options.modelUrl !== prevModel){
    await init(_canvas, _lastOptions);
    return;
  }

  if (!_viewerInstance) return;
  if (options?.logoUrl !== undefined) await _viewerInstance.setLogo(options.logoUrl);
  if (options?.outfit    !== undefined) _viewerInstance.setOutfit(options.outfit);
  if (options?.background!== undefined) await _viewerInstance.setBackground(options.background);
  if (options?.hairColor !== undefined) _viewerInstance.setHairColor(options.hairColor);
}

export function frame(){ _viewerInstance?.frame?.(); }
export function turntable(ms){ _viewerInstance?.turntable?.(ms ?? 3000); }
export function screenshot(){ return _viewerInstance?.screenshot?.(1); }
export function speak(audioUrl, visemas){ _viewerInstance?.speak?.(audioUrl, visemas); }

export async function dispose(){
  if (_viewerInstance){ await _viewerInstance.dispose?.(); _viewerInstance = null; }
  _canvas = null; _lastOptions = {};
}
