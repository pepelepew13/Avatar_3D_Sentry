import * as THREE from '../lib/three/three.module.min.js';
import { GLTFLoader } from '../lib/three/GLTFLoader.js';
import { OrbitControls } from '../lib/three/OrbitControls.js';

const globalScope = typeof window !== "undefined" ? window : globalThis;
globalScope.THREE = THREE;
globalScope.THREE.GLTFLoader = GLTFLoader;
globalScope.THREE.OrbitControls = OrbitControls;

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
        materials: new Map(),
        loadedTextures: new Map(),
        ground: null,
        ready: false,
        pendingAppearance: null,
        resizeHandler: null,
        animationFrame: null,
        currentModelUrl: null,
        loadToken: 0
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

        state.loadToken = 0;
        state.currentModelUrl = null;

        if (state.renderer) {
            updateAppearance(options);
            return;
        }

        state.canvas = canvas;
        state.renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
        state.renderer.outputEncoding = THREE.sRGBEncoding;
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

        state.controls = new THREE.OrbitControls(state.camera, canvas);
        state.controls.enableZoom = false;
        state.controls.enablePan = false;
        state.controls.minPolarAngle = Math.PI / 3;
        state.controls.maxPolarAngle = Math.PI / 2.1;
        state.controls.target.set(0, 1.5, 0);

        state.mixer = new THREE.AnimationMixer(null);

        resizeRenderer();
        state.resizeHandler = () => resizeRenderer();
        global.addEventListener("resize", state.resizeHandler);

        state.pendingAppearance = options || {};
        loadModel(state.pendingAppearance);
        renderLoop();
    }

    function loadModel(options) {
        const loader = new THREE.GLTFLoader();
        loader.setCrossOrigin("anonymous");

        const normalized = Object.assign({}, options || {});
        normalized.outfit = normalizeOutfitKey(normalized.outfit);
        normalized.modelUrl = normalized.modelUrl || resolveModelUrl(normalized.outfit);

        const requestId = ++state.loadToken;
        state.ready = false;
        state.pendingAppearance = normalized;

        disposeCurrentModel();

        loader.load(normalized.modelUrl,
            (gltf) => {
                if (requestId !== state.loadToken) {
                    disposeGltfScene(gltf);
                    return;
                }

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
            },
            undefined,
            (err) => {
                if (requestId === state.loadToken) {
                    console.error("AvatarViewer: error al cargar el modelo", err);
                    state.ready = false;
                    state.currentModelUrl = null;
                }
            }
        );
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
        const center = box.getCenter(new THREE.Vector3());
        const size = box.getSize(new THREE.Vector3());

        state.controls.target.copy(new THREE.Vector3(center.x, center.y + size.y * 0.1, center.z));

        const distance = Math.max(size.y, size.z) * 0.9;
        state.camera.position.set(center.x + distance * 0.4, center.y + distance * 0.6, center.z + distance);
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
                texture.encoding = THREE.sRGBEncoding;
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
        state.talkingAction.stop();
        state.talkingAction.enabled = false;
    }

    function applyVisemes(visemes) {
        if (!Array.isArray(visemes) || !state.skinnedMesh || !state.morphDictionary) {
            return;
        }

        const morphInfluences = state.skinnedMesh.morphTargetInfluences;
        if (!morphInfluences) {
            return;
        }

        const sequence = visemes
            .map(frame => ({
                time: typeof frame.tiempo === "number" ? frame.tiempo : 0,
                key: frame.shapeKey || frame.viseme
            }))
            .filter(frame => typeof frame.key === "string")
            .sort((a, b) => a.time - b.time);

        let startTime = null;

        function resetMorphs() {
            for (let i = 0; i < morphInfluences.length; i += 1) {
                morphInfluences[i] = 0;
            }
        }

        resetMorphs();

        function update() {
            if (!sequence.length) {
                resetMorphs();
                return;
            }

            if (startTime === null) {
                startTime = performance.now();
            }
            const elapsed = (performance.now() - startTime) / 1000;

            resetMorphs();
            let activeFound = false;
            for (const frame of sequence) {
                const index = state.morphDictionary[frame.key];
                if (typeof index !== "number") {
                    continue;
                }

                const distance = Math.abs(frame.time - elapsed);
                const strength = Math.max(1 - distance * 4, 0);
                if (strength > 0) {
                    morphInfluences[index] = Math.max(morphInfluences[index], strength);
                    activeFound = true;
                }
            }

            if (activeFound) {
                requestAnimationFrame(update);
            } else {
                resetMorphs();
            }
        }

        requestAnimationFrame(update);
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
    AvatarViewer.dispose = dispose;

    global.AvatarViewer = AvatarViewer;
})(globalScope);
