import * as THREE from 'three';
import { GLTFLoader }   from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

const globalScope = typeof window !== "undefined" ? window : globalThis;
globalScope.THREE = THREE;

(function (global) {
    if (!global) {
        return;
    }

    const AvatarViewer = {};
    const state = {
        canvas: null,
        renderer: null,
        scene: null,
        camera: null,
        controls: null,
        mixer: null,
        clock: new THREE.Clock(),
        eyeAction: null,
        talkingAction: null,
        talkingTimeout: null,
        root: null,
        skinnedMesh: null,
        morphDictionary: null,
        morphLookup: null,
        materials: new Map(),
        loadedTextures: new Map(),
        ground: null,
        ready: false,
        pendingAppearance: null,
        resizeHandler: null,
        animationFrame: null,
        currentModelUrl: null,
        loadToken: 0,
        initializing: false,
        visemeSequence: [],
        visemeIndices: new Set(),
        visemeAnimationId: null,
        visemeAudio: null,
        pendingVisemes: null,
        loadingUrl: null,

    };

    const backgroundPresets = {
        oficina: { background: 0xe9f6f1, ground: 0xffffff, rim: 0xd6ede4 },
        moderno: { background: 0xe4f1ed, ground: 0xd8e7e2, rim: 0xc7ddd6 },
        naturaleza: { background: 0xe9f7ef, ground: 0xd5ecda, rim: 0xc2dfca }
    };

    const outfitPresets = {
        predeterminado: {
            model: "models/Avatar.glb",
            palette: {
                shirt: 0xf6f9ff,
                pants: 0xc8b59b,
                shoes: 0x2f2f35,
                accessories: 0x274c77,
                hair: 0x452c25
            },
            applyColors: true
        },
        traje: {
            model: "models/traje.glb",
            palette: {
                hair: 0x33221d
            },
            applyColors: false
        },
        vestido: {
            model: "models/vestido.glb",
            palette: {
                hair: 0x503129
            },
            applyColors: false
        }
    };

    const outfitAliases = {
        corporativo: "predeterminado",
        ejecutivo: "traje",
        casual: "vestido"
    };

    const logoMaterialNames = ["avaturn_look_0.002", "logo", "avatar_logo"];
    const shirtMaterialHints = ["shirt", "blouse", "upper", "body_cloth"];
    const pantsMaterialHints = ["pant", "trouser", "lower"];
    const shoeMaterialHints = ["shoe", "boot"];
    const accessoryHints = ["tie", "belt", "accessory"];
    const hairMaterialNames = ["avaturn_hair_1", "hair"];

    function normalizeOutfitKey(key) {
        if (!key) {
            return "predeterminado";
        }
        const lowered = String(key).toLowerCase();
        if (Object.prototype.hasOwnProperty.call(outfitPresets, lowered)) {
            return lowered;
        }
        if (Object.prototype.hasOwnProperty.call(outfitAliases, lowered)) {
            return outfitAliases[lowered];
        }
        return "predeterminado";
    }

    function resolveModelUrl(outfitKey) {
        const normalized = normalizeOutfitKey(outfitKey);
        const preset = outfitPresets[normalized] || outfitPresets.predeterminado;
        return preset.model;
    }

    function init(canvas, options) {
        if (!canvas || typeof THREE === "undefined") {
            console.warn("AvatarViewer: no canvas or THREE not loaded");
            return;
        }

        if (state.renderer) {
            state.initializing = false;
            updateAppearance(options);
            return;
        }

        state.loadToken = 0;
        state.currentModelUrl = null;
        state.loadingUrl = null;

        state.canvas = canvas;
        state.renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
        state.renderer.outputColorSpace = THREE.SRGBColorSpace;
        state.renderer.setPixelRatio(global.devicePixelRatio || 1);

        state.scene = new THREE.Scene();
        state.scene.background = new THREE.Color(backgroundPresets.oficina.background);

        state.camera = new THREE.PerspectiveCamera(35, 1, 0.1, 100);
        state.camera.position.set(0, 1.7, 3.2);

        const hemiLight = new THREE.HemisphereLight(0xffffff, 0xd6d6d6, 0.85);
        const dirLight = new THREE.DirectionalLight(0xffffff, 0.8);
        dirLight.position.set(2.5, 5, 4);
        dirLight.castShadow = false;
        state.scene.add(hemiLight, dirLight);

        const groundMaterial = new THREE.MeshStandardMaterial({ color: backgroundPresets.oficina.ground, roughness: 0.85, metalness: 0.05 });
        const groundGeometry = new THREE.CircleGeometry(2.4, 48);
        state.ground = new THREE.Mesh(groundGeometry, groundMaterial);
        state.ground.rotation.x = -Math.PI / 2;
        state.ground.position.y = 0;
        state.scene.add(state.ground);

        state.controls = new OrbitControls(state.camera, canvas);
        state.controls.enableZoom = true;
        state.controls.zoomSpeed = 1;
        state.controls.enableDamping = true;
        state.controls.dampingFactor = 0.08;
        state.controls.enablePan = true;
        state.controls.panSpeed = 0.8;
        state.controls.minPolarAngle = Math.PI / 4;
        state.controls.maxPolarAngle = Math.PI / 1.9;
        state.controls.target.set(0, 1.5, 0);

        state.mixer = new THREE.AnimationMixer(null);

        resizeRenderer();
        state.resizeHandler = () => resizeRenderer();
        global.addEventListener("resize", state.resizeHandler);

        state.pendingAppearance = options || {};
        loadModel(state.pendingAppearance);
        renderLoop();
        state.initializing = false;  
    }

    function loadModel(options) {
        const loader = new GLTFLoader();
        loader.setCrossOrigin("anonymous");

        const normalized = Object.assign({}, options || {});
        normalized.outfit = normalizeOutfitKey(normalized.outfit);
        normalized.modelUrl = normalized.modelUrl || resolveModelUrl(normalized.outfit);

        const requestId = ++state.loadToken;
        state.ready = false;
        state.pendingAppearance = normalized;

        disposeCurrentModel();

        state.loadingUrl = normalized.modelUrl;

        loader.load(normalized.modelUrl,
            (gltf) => {
                if (requestId !== state.loadToken) {
                    disposeGltfScene(gltf);
                    return;
                }

                state.loadingUrl = null;
                state.root = gltf.scene;
                state.currentModelUrl = normalized.modelUrl;
                state.scene.add(state.root);
                state.mixer = new THREE.AnimationMixer(state.root);

                state.materials.clear();
                gltf.scene.traverse((child) => {
                    if (child.isMesh || child.isSkinnedMesh) {
                        if (child.morphTargetDictionary && !state.skinnedMesh) {
                            state.skinnedMesh = child;
                            state.morphDictionary = child.morphTargetDictionary;
                            state.morphLookup = null;
                        }

                        const materials = Array.isArray(child.material) ? child.material : [child.material];
                        materials.forEach((mat) => {
                            if (mat && mat.name) {
                                state.materials.set(mat.name, mat);
                            }
                        });
                    }
                });

                const eyeClip = THREE.AnimationClip.findByName(gltf.animations, "EyesAnimation");
                if (eyeClip) {
                    state.eyeAction = state.mixer.clipAction(eyeClip);
                    state.eyeAction.play();
                }

                const talkClip = THREE.AnimationClip.findByName(gltf.animations, "TalkingAnimation");
                if (talkClip) {
                    state.talkingAction = state.mixer.clipAction(talkClip);
                    state.talkingAction.clampWhenFinished = false;
                    state.talkingAction.loop = THREE.LoopRepeat;
                }

                centerModel();
                state.ready = true;
                applyAppearance(state.pendingAppearance);
                if (state.pendingVisemes) {
                    const queued = state.pendingVisemes;
                    state.pendingVisemes = null;
                    applyVisemes(queued);
                }
            },
            undefined,
            (err) => {
                if (requestId === state.loadToken) {
                    state.loadingUrl = null;
                    console.error("AvatarViewer: error al cargar el modelo", err);
                    state.ready = false;
                    state.currentModelUrl = null;
                }
            }
        );
    }

    // Gira el modelo suavemente en 'ms' milisegundos (por defecto 3000)
    function turntable(ms = 3000) {
        if (!state.root) return;
        const start = performance.now();
        const startY = state.root.rotation.y;
        const duration = Math.max(500, ms);
        const fullTurn = startY + Math.PI * 2;

        function step(t) {
            const p = Math.min((t - start) / duration, 1);
            // easing suave
            const eased = p < 0.5 ? 2*p*p : -1 + (4 - 2*p) * p;
            state.root.rotation.y = startY + (fullTurn - startY) * eased;
            if (p < 1) requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    }

    // Encadra el avatar (fit to view)
    function frame() {
        centerModel();
        if (state.controls) {
            state.controls.update();
        }
    }

    // Descarga una captura PNG del canvas
    function screenshot() {
        if (!state.canvas) return;
        // asegura un render antes de capturar
        if (state.renderer && state.scene && state.camera) {
            state.renderer.render(state.scene, state.camera);
        }
        const url = state.canvas.toDataURL('image/png');
        const a = document.createElement('a');
        a.href = url;
        a.download = 'avatar.png';
        a.click();
    }


    function disposeCurrentModel() {
        if (state.root && state.scene) {
            state.scene.remove(state.root);
        }

        if (state.root) {
            state.root.traverse((child) => {
                if (child.isMesh || child.isSkinnedMesh) {
                    if (child.geometry && typeof child.geometry.dispose === "function") {
                        child.geometry.dispose();
                    }

                    const materials = Array.isArray(child.material) ? child.material : [child.material];
                    materials.forEach(disposeMaterial);
                }
            });
        }

        state.loadedTextures.forEach((tex) => {
            if (tex && typeof tex.dispose === "function") {
                tex.dispose();
            }
        });
        state.loadedTextures.clear();

        state.materials.clear();
        state.mixer = null;
        state.skinnedMesh = null;
        state.morphDictionary = null;
        state.morphLookup = null;
        stopVisemePlayback();
        state.visemeSequence = [];
        state.visemeIndices = new Set();
        state.pendingVisemes = null;
        state.eyeAction = null;
        state.talkingAction = null;
        state.root = null;
        state.currentModelUrl = null;
    }

    function disposeMaterial(material) {
        if (!material) {
            return;
        }

        if (Array.isArray(material)) {
            material.forEach(disposeMaterial);
            return;
        }

        if (material.map && typeof material.map.dispose === "function") {
            material.map.dispose();
            material.map = null;
        }

        if (typeof material.dispose === "function") {
            material.dispose();
        }
    }

    function disposeGltfScene(gltf) {
        if (!gltf || !gltf.scene) {
            return;
        }

        gltf.scene.traverse((child) => {
            if (child.isMesh || child.isSkinnedMesh) {
                if (child.geometry && typeof child.geometry.dispose === "function") {
                    child.geometry.dispose();
                }

                const materials = Array.isArray(child.material) ? child.material : [child.material];
                materials.forEach(disposeMaterial);
            }
        });
    }

    function centerModel() {
        if (!state.root) {
            return;
        }

        const box = new THREE.Box3().setFromObject(state.root);
        if (box.isEmpty()) {
            return;
        }

        const sphere = box.getBoundingSphere(new THREE.Sphere());
        const center = sphere.center.clone();
        const radius = sphere.radius || 1;
        const marginFactor = 1.4;
        const distance = radius * marginFactor;

        const target = center.clone();
        target.y += radius * 0.15;
        state.controls.target.copy(target);

        const offsetDirection = new THREE.Vector3(0.4, 0.6, 1).normalize();
        const cameraPosition = center.clone().add(offsetDirection.multiplyScalar(distance));
        state.camera.position.copy(cameraPosition);

        if (state.controls) {
            state.controls.minDistance = radius * 2.5;
            state.controls.maxDistance = radius * 10;
        }

        state.camera.updateProjectionMatrix();
    }

    function updateAppearance(options) {
        if (!options) {
            return;
        }

        const normalized = Object.assign({}, options);
        normalized.outfit = normalizeOutfitKey(normalized.outfit);
        normalized.modelUrl = normalized.modelUrl || resolveModelUrl(normalized.outfit);

        state.pendingAppearance = normalized;

        if (state.loadingUrl && state.loadingUrl === normalized.modelUrl) {
            return;
        }

        if (!state.currentModelUrl || normalized.modelUrl !== state.currentModelUrl) {
            loadModel(normalized);
            return;
        }

        if (state.ready) {
            applyAppearance(normalized);
        }
    }

    function applyAppearance(options) {
        if (!options) {
            return;
        }

        const normalized = Object.assign({}, options);
        normalized.outfit = normalizeOutfitKey(normalized.outfit);

        state.pendingAppearance = normalized;

        applyBackground(normalized.background);
        const fallbackHair = applyOutfit(normalized.outfit);
        applyLogo(normalized.logoUrl);

        const hasExplicitHair = Object.prototype.hasOwnProperty.call(normalized, "hairColor")
            && normalized.hairColor !== null
            && typeof normalized.hairColor !== "undefined";

        if (hasExplicitHair) {
            applyHairColor(normalized.hairColor);
        } else if (typeof fallbackHair === "number") {
            applyHairColor(fallbackHair);
        }
    }

    function applyBackground(key) {
        const preset = backgroundPresets[key] || backgroundPresets.oficina;
        if (state.scene) {
            state.scene.background = new THREE.Color(preset.background);
        }
        if (state.ground) {
            state.ground.material.color.setHex(preset.ground);
        }
    }

    function applyOutfit(key) {
        const normalized = normalizeOutfitKey(key);
        const preset = outfitPresets[normalized] || outfitPresets.predeterminado;
        const palette = preset.palette || {};

        if (preset.applyColors !== false) {
            const shirtMat = findMaterial(shirtMaterialHints);
            const pantsMat = findMaterial(pantsMaterialHints);
            const shoeMat = findMaterial(shoeMaterialHints);
            const accessoryMat = findMaterial(accessoryHints);

            if (shirtMat && palette.shirt) {
                setMaterialColor(shirtMat, palette.shirt);
            }
            if (pantsMat && palette.pants) {
                setMaterialColor(pantsMat, palette.pants);
            }
            if (shoeMat && palette.shoes) {
                setMaterialColor(shoeMat, palette.shoes);
            }
            if (accessoryMat && palette.accessories) {
                setMaterialColor(accessoryMat, palette.accessories);
            }
        }

        return typeof palette.hair === "number" ? palette.hair : null;
    }

    function applyHairColor(color) {
        const material = findMaterial(hairMaterialNames);
        if (material) {
            setMaterialColor(material, color);
        }
    }

    function applyLogo(url) {
        const material = findMaterial(logoMaterialNames);
        if (!material) {
            return;
        }

        if (state.loadedTextures.has("logo")) {
            const previous = state.loadedTextures.get("logo");
            if (previous) {
                previous.dispose();
            }
            state.loadedTextures.delete("logo");
        }

        if (!url) {
            material.map = null;
            material.needsUpdate = true;
            return;
        }

        const loader = new THREE.TextureLoader();
        loader.setCrossOrigin("anonymous");
        loader.load(url,
            (texture) => {
                texture.colorSpace = THREE.SRGBColorSpace;
                texture.flipY = false;
                material.map = texture;
                material.needsUpdate = true;
                state.loadedTextures.set("logo", texture);
            },
            undefined,
            (err) => console.warn("AvatarViewer: no se pudo cargar el logo", err)
        );
    }

    function setMaterialColor(material, hexColor) {
        if (!material || typeof material.color === "undefined") {
            return;
        }
        material.color.setHex(hexColor);
        material.needsUpdate = true;
    }

    function findMaterial(hints) {
        if (!hints) {
            return null;
        }

        const allMaterials = Array.from(state.materials.values());
        for (const hint of hints) {
            if (!hint) continue;
            const exact = state.materials.get(hint);
            if (exact) {
                return exact;
            }
        }

        const lowered = hints.map(h => h.toLowerCase());
        for (const mat of allMaterials) {
            if (!mat || !mat.name) continue;
            const name = mat.name.toLowerCase();
            if (lowered.some(h => name.includes(h))) {
                return mat;
            }
        }
        return null;
    }

    function playTalking(durationMs) {
        if (!state.talkingAction) {
            return;
        }

        const duration = typeof durationMs === "number" && durationMs > 0 ? durationMs : 2500;
        const targetSeconds = duration / 1000;
        const clip = typeof state.talkingAction.getClip === "function"
            ? state.talkingAction.getClip()
            : null;

        if (clip && clip.duration > 0 && Number.isFinite(targetSeconds) && targetSeconds > 0) {
            const loops = Math.max(Math.round(targetSeconds / clip.duration), 1);
            const speed = (clip.duration * loops) / targetSeconds;
            state.talkingAction.setLoop(THREE.LoopRepeat, loops);
            state.talkingAction.timeScale = Number.isFinite(speed) && speed > 0 ? speed : 1;
        } else {
            state.talkingAction.setLoop(THREE.LoopRepeat, Infinity);
            state.talkingAction.timeScale = 1;
        }

        state.talkingAction.reset();
        state.talkingAction.enabled = true;
        state.talkingAction.play();

        if (state.talkingTimeout) {
            clearTimeout(state.talkingTimeout);
        }

        state.talkingTimeout = global.setTimeout(() => {
            stopTalking();
        }, duration);
    }

    function stopTalking() {
        if (!state.talkingAction) {
            return;
        }

        if (state.talkingTimeout) {
            clearTimeout(state.talkingTimeout);
            state.talkingTimeout = null;
        }

        state.talkingAction.stop();
        state.talkingAction.enabled = false;
        state.talkingAction.timeScale = 1;
        state.talkingAction.setLoop(THREE.LoopRepeat, Infinity);
    }

    const visemeCanonicalMap = {
        sil: "sil",
        silence: "sil",
        rest: "sil",
        idle: "sil",
        pp: "pp",
        p: "pp",
        b: "pp",
        m: "pp",
        ff: "ff",
        f: "ff",
        v: "ff",
        th: "th",
        dh: "th",
        dd: "dd",
        t: "dd",
        d: "dd",
        kk: "kk",
        k: "kk",
        g: "kk",
        ch: "ch",
        jh: "ch",
        j: "ch",
        ss: "ss",
        s: "ss",
        z: "ss",
        sh: "ss",
        zh: "ss",
        nn: "nn",
        n: "nn",
        l: "nn",
        rr: "rr",
        r: "rr",
        er: "rr",
        aa: "aa",
        a: "aa",
        ah: "aa",
        aw: "aa",
        ae: "aa",
        e: "e",
        eh: "e",
        ey: "e",
        i: "i",
        iy: "i",
        ee: "i",
        o: "o",
        oh: "o",
        ow: "o",
        u: "u",
        oo: "u",
        uw: "u",
        w: "u"
    };

    function normalizeVisemeKey(value) {
        if (typeof value !== "string") {
            return null;
        }

        const trimmed = value.trim();
        if (!trimmed) {
            return null;
        }

        const lowered = trimmed.toLowerCase();
        return visemeCanonicalMap[lowered] || lowered;
    }

    function ensureMorphLookup() {
        if (!state.morphDictionary) {
            return null;
        }

        if (state.morphLookup) {
            return state.morphLookup;
        }

        const lookup = {};
        Object.entries(state.morphDictionary).forEach(([name, index]) => {
            if (typeof index !== "number" || typeof name !== "string") {
                return;
            }

            const trimmed = name.trim();
            if (!trimmed) {
                return;
            }

            const lowered = trimmed.toLowerCase();
            const sanitized = lowered.replace(/[^a-z0-9]+/g, "");

            lookup[trimmed] = index;
            lookup[lowered] = index;
            if (sanitized) {
                lookup[sanitized] = index;
            }

            if (lowered.startsWith("viseme_")) {
                const bare = lowered.slice(7);
                if (bare) {
                    lookup[bare] = index;
                    const bareSanitized = bare.replace(/[^a-z0-9]+/g, "");
                    if (bareSanitized) {
                        lookup[bareSanitized] = index;
                    }
                }
            }
        });

        state.morphLookup = lookup;
        return lookup;
    }

    function lookupMorphIndex(key) {
        const lookup = ensureMorphLookup();
        if (!lookup || typeof key !== "string") {
            return null;
        }

        const trimmed = key.trim();
        if (!trimmed) {
            return null;
        }

        const lowered = trimmed.toLowerCase();
        const sanitized = lowered.replace(/[^a-z0-9]+/g, "");

        const candidates = [trimmed, lowered, sanitized];
        if (lowered && !lowered.startsWith("viseme_")) {
            candidates.push(`viseme_${lowered}`);
            candidates.push(`viseme-${lowered}`);
            candidates.push(`viseme${lowered}`);
        }

        if (trimmed.startsWith("viseme") && sanitized && !sanitized.startsWith("viseme")) {
            candidates.push(sanitized);
        }

        for (const candidate of candidates) {
            if (!candidate) {
                continue;
            }
            if (Object.prototype.hasOwnProperty.call(lookup, candidate)) {
                return lookup[candidate];
            }
        }

        return null;
    }

    function resolveVisemeIndex(frame) {
        if (!frame) {
            return null;
        }

        if (typeof frame.shapeKey === "string") {
            const indexFromShapeKey = lookupMorphIndex(frame.shapeKey);
            if (typeof indexFromShapeKey === "number") {
                return indexFromShapeKey;
            }
        }

        const normalized = normalizeVisemeKey(frame.viseme);
        if (!normalized) {
            return null;
        }

        const candidates = [frame.viseme, normalized];
        if (!normalized.startsWith("viseme_")) {
            candidates.push(`viseme_${normalized}`);
            candidates.push(`viseme-${normalized}`);
            candidates.push(`viseme${normalized}`);
        }

        if (normalized.length === 1) {
            candidates.push(`viseme_${normalized}${normalized}`);
        }

        for (const candidate of candidates) {
            const index = lookupMorphIndex(candidate);
            if (typeof index === "number") {
                return index;
            }
        }

        return null;
    }

    function normalizeVisemeSequence(visemes) {
        const frames = [];
        const rawTimes = [];

        for (let i = 0; i < visemes.length; i += 1) {
            const frame = visemes[i];
            const index = resolveVisemeIndex(frame);
            if (typeof index !== "number") {
                continue;
            }

            let timeValue = 0;
            if (frame && typeof frame.tiempo === "number" && Number.isFinite(frame.tiempo)) {
                timeValue = frame.tiempo;
            } else if (frame && typeof frame.time === "number" && Number.isFinite(frame.time)) {
                timeValue = frame.time;
            } else if (frame && typeof frame.timestamp === "number" && Number.isFinite(frame.timestamp)) {
                timeValue = frame.timestamp;
            }

            rawTimes.push(timeValue);
            frames.push({ index, rawTime: timeValue });
        }

        if (!frames.length) {
            return [];
        }

        let useMilliseconds = false;
        for (let i = 0; i < rawTimes.length; i += 1) {
            if (rawTimes[i] > 20) {
                useMilliseconds = true;
                break;
            }
        }

        const normalized = [];
        for (let i = 0; i < frames.length; i += 1) {
            const frame = frames[i];
            const time = useMilliseconds ? frame.rawTime / 1000 : frame.rawTime;
            normalized.push({ index: frame.index, time });
        }

        normalized.sort(function (a, b) {
            return a.time - b.time;
        });

        return normalized;
    }

    function resetVisemeMorphs() {
        if (!state.skinnedMesh || !state.skinnedMesh.morphTargetInfluences) {
            return;
        }

        if (!(state.visemeIndices instanceof Set) || state.visemeIndices.size === 0) {
            return;
        }

        const influences = state.skinnedMesh.morphTargetInfluences;
        state.visemeIndices.forEach((index) => {
            if (typeof index === "number" && index >= 0 && index < influences.length) {
                influences[index] = 0;
            }
        });
    }

    function stopVisemePlayback() {
        if (state.visemeAnimationId) {
            cancelAnimationFrame(state.visemeAnimationId);
            state.visemeAnimationId = null;
        }
        state.visemeAudio = null;
        resetVisemeMorphs();
    }

    function startVisemePlayback(audioElement) {
        if (!audioElement || !Array.isArray(state.visemeSequence) || state.visemeSequence.length === 0) {
            resetVisemeMorphs();
            return;
        }

        if (!state.skinnedMesh || !state.skinnedMesh.morphTargetInfluences) {
            return;
        }
      
        stopVisemePlayback();
        state.visemeAudio = audioElement;

        const influences = state.skinnedMesh.morphTargetInfluences;
        const windowSize = 0.14;

        function step() {
            if (!state.visemeAudio) {
                state.visemeAnimationId = null;
                resetVisemeMorphs();
                return;
            }

            if (audioElement.ended) {
                state.visemeAnimationId = null;
                state.visemeAudio = null;
                resetVisemeMorphs();
                return;
            }

            const current = audioElement.currentTime;
            resetVisemeMorphs();

            for (let i = 0; i < state.visemeSequence.length; i += 1) {
                const frame = state.visemeSequence[i];
                if (!frame || typeof frame.index !== "number") {
                    continue;
                }

                const frameTimeSeconds = typeof frame.time === "number" ? frame.time / 1000 : 0;
                const distance = Math.abs(current - frameTimeSeconds);
                if (distance > windowSize) {
                    continue;
                }

                const strength = Math.max(1 - (distance / windowSize), 0);
                const index = frame.index;
                if (index >= 0 && index < influences.length) {
                    influences[index] = Math.max(influences[index], strength);
                }
            }

            state.visemeAnimationId = requestAnimationFrame(step);
        }

        state.visemeAnimationId = requestAnimationFrame(step);
    }

    function applyVisemes(visemes) {
        const payload = Array.isArray(visemes) ? visemes : [];

        if (!state.skinnedMesh || !state.morphDictionary) {
            if (payload.length === 0) {
                stopVisemePlayback();
                state.visemeSequence = [];
                state.visemeIndices = new Set();
                state.pendingVisemes = null;
            } else {
                state.pendingVisemes = payload.slice();
            }
            return;
        }

        if (payload.length === 0) {
            stopVisemePlayback();
            state.visemeSequence = [];
            state.visemeIndices = new Set();
            state.pendingVisemes = null;
            resetVisemeMorphs();
            return;
        }

        stopVisemePlayback();
        const sequence = normalizeVisemeSequence(payload);

        if (!sequence.length) {
            state.visemeSequence = [];
            state.visemeIndices = new Set();
            state.pendingVisemes = null;
            resetVisemeMorphs();
            return;
        }

        state.visemeSequence = sequence;

        const indices = new Set();
        for (let i = 0; i < sequence.length; i += 1) {
            const frame = sequence[i];
            if (frame && typeof frame.index === "number") {
                indices.add(frame.index);
            }
        }

        state.visemeIndices = indices;
        state.pendingVisemes = null;
    }

    function prepareAudioClip(audioElement, source) {
        if (!audioElement || !source) {
            return Promise.resolve(0);
        }

        stopAudioClip(audioElement);

        return new Promise((resolve, reject) => {
            const onLoaded = () => {
                cleanup();
                const duration = Number.isFinite(audioElement.duration) && audioElement.duration > 0
                    ? audioElement.duration * 1000
                    : 0;
                resolve(duration);
            };

            const onError = (err) => {
                cleanup();
                reject(err instanceof Error ? err : new Error("AvatarViewer: no se pudo cargar el audio de vista previa."));
            };

            const cleanup = () => {
                audioElement.removeEventListener("loadedmetadata", onLoaded);
                audioElement.removeEventListener("error", onError);
            };

            audioElement.addEventListener("loadedmetadata", onLoaded, { once: true });
            audioElement.addEventListener("error", onError, { once: true });

            try {
                audioElement.src = source;
                audioElement.load();
                if (audioElement.readyState >= 1) {
                    onLoaded();
                }
            } catch (err) {
                onError(err);
            }
        });
    }

    function playPreparedAudioClip(audioElement) {
        if (!audioElement) {
            return Promise.resolve();
        }

        stopAudioClip(audioElement);

        return new Promise((resolve, reject) => {
            let started = false;
            const playbackState = {
                done: false,
                cleanup() {
                    audioElement.removeEventListener("ended", onEnded);
                    audioElement.removeEventListener("error", onError);
                    audioElement.removeEventListener("play", onStarted);
                    audioElement.removeEventListener("playing", onStarted);
                },
                finish() {
                    if (playbackState.done) {
                        return;
                    }
                    playbackState.done = true;
                    stopVisemePlayback();
                    playbackState.cleanup();
                    delete audioElement.__avatarPreviewPlayback;
                    resolve();
                },
                fail(err) {
                    if (playbackState.done) {
                        return;
                    }
                    playbackState.done = true;
                    stopVisemePlayback();
                    playbackState.cleanup();
                    delete audioElement.__avatarPreviewPlayback;
                    reject(err instanceof Error ? err : new Error("AvatarViewer: error al reproducir el audio de vista previa."));
                }
            };

            const onEnded = () => playbackState.finish();
            const onError = (err) => playbackState.fail(err);
            const onStarted = () => {
                if (started || playbackState.done) {
                    return;
                }
                started = true;
                startVisemePlayback(audioElement);
            };

            audioElement.__avatarPreviewPlayback = playbackState;

            audioElement.addEventListener("ended", onEnded, { once: true });
            audioElement.addEventListener("error", onError, { once: true });
            audioElement.addEventListener("play", onStarted);
            audioElement.addEventListener("playing", onStarted);

            try {
                const playPromise = audioElement.play();
                if (playPromise && typeof playPromise.then === "function") {
                    playPromise.then(onStarted).catch((err) => playbackState.fail(err));
                } else {
                    onStarted();
                }
            } catch (err) {
                playbackState.fail(err);
            }
        });
    }

    function stopAudioClip(audioElement) {
        if (!audioElement) {
            return;
        }

        stopVisemePlayback();

        const playbackState = audioElement.__avatarPreviewPlayback;
        if (playbackState) {
            delete audioElement.__avatarPreviewPlayback;
            if (typeof playbackState.finish === "function") {
                playbackState.finish();
            } else if (typeof playbackState.cleanup === "function") {
                playbackState.cleanup();
            }
        }

        audioElement.pause();
        try {
            audioElement.currentTime = 0;
        } catch (err) {
            // Ignorar errores al reiniciar la posición del audio.
        }
    }

    function prepareAudioClip(audioElement, source) {
        if (!audioElement || !source) {
            return Promise.resolve(0);
        }

        stopAudioClip(audioElement);

        return new Promise((resolve, reject) => {
            const onLoaded = () => {
                cleanup();
                const duration = Number.isFinite(audioElement.duration) && audioElement.duration > 0
                    ? audioElement.duration * 1000
                    : 0;
                resolve(duration);
            };

            const onError = (err) => {
                cleanup();
                reject(err instanceof Error ? err : new Error("AvatarViewer: no se pudo cargar el audio de vista previa."));
            };

            const cleanup = () => {
                audioElement.removeEventListener("loadedmetadata", onLoaded);
                audioElement.removeEventListener("error", onError);
            };

            audioElement.addEventListener("loadedmetadata", onLoaded, { once: true });
            audioElement.addEventListener("error", onError, { once: true });

            try {
                audioElement.src = source;
                audioElement.load();
                if (audioElement.readyState >= 1) {
                    onLoaded();
                }
            } catch (err) {
                onError(err);
            }
        });
    }

    function playPreparedAudioClip(audioElement) {
        if (!audioElement) {
            return Promise.resolve();
        }

        stopAudioClip(audioElement);

        return new Promise((resolve, reject) => {
            const playbackState = {
                cleanup() {
                    audioElement.removeEventListener("ended", onEnded);
                    audioElement.removeEventListener("error", onError);
                },
                finish() {
                    playbackState.cleanup();
                    delete audioElement.__avatarPreviewPlayback;
                    resolve();
                },
                fail(err) {
                    playbackState.cleanup();
                    delete audioElement.__avatarPreviewPlayback;
                    reject(err instanceof Error ? err : new Error("AvatarViewer: error al reproducir el audio de vista previa."));
                }
            };

            const onEnded = () => playbackState.finish();
            const onError = (err) => playbackState.fail(err);

            audioElement.__avatarPreviewPlayback = playbackState;

            audioElement.addEventListener("ended", onEnded, { once: true });
            audioElement.addEventListener("error", onError, { once: true });

            const playPromise = audioElement.play();
            if (playPromise && typeof playPromise.then === "function") {
                playPromise.catch((err) => playbackState.fail(err));
            }
        });
    }

    function stopAudioClip(audioElement) {
        if (!audioElement) {
            return;
        }

        const playbackState = audioElement.__avatarPreviewPlayback;
        if (playbackState) {
            delete audioElement.__avatarPreviewPlayback;
            if (typeof playbackState.finish === "function") {
                playbackState.finish();
            } else if (typeof playbackState.cleanup === "function") {
                playbackState.cleanup();
            }
        }

        audioElement.pause();
        try {
            audioElement.currentTime = 0;
        } catch (err) {
            // Ignorar errores al reiniciar la posición del audio.
        }
    }

    function resizeRenderer() {
        if (!state.renderer || !state.canvas || !state.camera) {
            return;
        }
        const width = state.canvas.clientWidth || (state.canvas.parentElement ? state.canvas.parentElement.clientWidth : 600);
        const height = state.canvas.clientHeight || (state.canvas.parentElement ? state.canvas.parentElement.clientHeight : 400);
        state.renderer.setSize(width, height, false);
        state.camera.aspect = width / Math.max(height, 1);
        state.camera.updateProjectionMatrix();
    }

    function renderLoop() {
        state.animationFrame = requestAnimationFrame(renderLoop);
        if (state.mixer) {
            const delta = state.clock.getDelta();
            state.mixer.update(delta);
        }
        if (state.controls) {
            state.controls.update();
        }
        if (state.renderer && state.scene && state.camera) {
            state.renderer.render(state.scene, state.camera);
        }
    }

    function dispose() {
        if (state.animationFrame) {
            cancelAnimationFrame(state.animationFrame);
        }

        if (state.resizeHandler) {
            global.removeEventListener("resize", state.resizeHandler);
        }

        disposeCurrentModel();

        if (state.controls) {
            state.controls.dispose();
        }

        if (state.renderer) {
            state.renderer.dispose();
        }

        state.canvas = null;
        state.renderer = null;
        state.scene = null;
        state.camera = null;
        state.controls = null;
        state.mixer = null;
        state.ready = false;
        state.pendingAppearance = null;
    }

    
    AvatarViewer.init = init;
    AvatarViewer.updateAppearance = updateAppearance;
    AvatarViewer.playTalking = playTalking;
    AvatarViewer.stopTalking = stopTalking;
    AvatarViewer.applyVisemes = applyVisemes;
    AvatarViewer.prepareAudioClip = prepareAudioClip;
    AvatarViewer.playPreparedAudioClip = playPreparedAudioClip;
    AvatarViewer.stopAudioClip = stopAudioClip;
    AvatarViewer.turntable = turntable;
    AvatarViewer.frame = frame;
    AvatarViewer.screenshot = screenshot;
    AvatarViewer.dispose = dispose;

    global.AvatarViewer = AvatarViewer;
})(globalScope);
