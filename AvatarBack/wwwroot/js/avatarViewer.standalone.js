// wwwroot/js/avatarViewer.standalone.js
import * as THREE from "https://unpkg.com/three@0.160.0/build/three.module.js";
import { GLTFLoader }   from "https://unpkg.com/three@0.160.0/examples/jsm/loaders/GLTFLoader.js";
import { OrbitControls } from "https://unpkg.com/three@0.160.0/examples/jsm/controls/OrbitControls.js";

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
        backdrop: null,        // 👈 nuevo: plano para fotos no equirect
        envTexture: null,      // 👈 nuevo: textura de fondo actual (si la hay)
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
        bgOptions: {
            blurriness: 0,   // 0..1
            intensity: 1,    // backgroundIntensity (solo texturas)
            envIntensity: 1, // environmentIntensity (si usamos scene.environment)
            rotationDeg: 0   // rotación Y en grados para background/environment
          },
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

    const logoMaterialNames = ["LogoLabel", "LogoMesh", "avaturn_look_0.002", "logo", "avatar_logo"];
    const shirtMaterialHints = ["shirt", "blouse", "upper", "body_cloth"];
    const pantsMaterialHints = ["pant", "trouser", "lower"];
    const shoeMaterialHints = ["shoe", "boot"];
    const accessoryHints = ["tie", "belt", "accessory"];
    const hairMaterialNames = ["avaturn_hair_0", "avaturn_hair_1", "avaturn_hair_0_material", "avaturn_hair_1_material", "hair", "hair_01", "hair_mat", "scalp", "cap", "head_hair"];

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
        normalized.outfit   = normalizeOutfitKey(normalized.outfit);
        normalized.modelUrl = normalized.modelUrl || resolveModelUrl(normalized.outfit);
    
        const requestId = ++state.loadToken;
        state.ready = false;
        state.pendingAppearance = normalized;
    
        stopTalking();
        stopVisemePlayback();
        disposeCurrentModel();
    
        state.loadingUrl = normalized.modelUrl;
    
        loader.load(
            normalized.modelUrl,
            (gltf) => {
                // si llegó otro load más nuevo, descarta éste
                if (requestId !== state.loadToken) {
                    disposeGltfScene(gltf);
                    return;
                }
    
                state.loadingUrl = null;
    
                // --- preparar escena / mixer ---
                state.root = gltf.scene;
                state.scene.add(state.root);
                state.currentModelUrl = normalized.modelUrl;
                state.mixer = new THREE.AnimationMixer(state.root);
    
                // borra cualquier blendshape activado por defecto
                resetAllMorphs(state.root);
    
                // --- elegir el skinnedMesh “bueno” y recolectar materiales ---
                state.skinnedMesh     = null;
                state.morphDictionary = null;
                state.morphLookup     = null;
                state.materials.clear();
    
                gltf.scene.traverse((child) => {
                    if (child.isMesh || child.isSkinnedMesh) {
                        // materiales
                        const mats = Array.isArray(child.material) ? child.material : [child.material];
                        mats.forEach((m) => { if (m && m.name) state.materials.set(m.name, m); });
    
                        // elegir mejor mesh para labios/visemas
                        if (child.morphTargetDictionary) {
                            // puntúa por presencia de nombres “viseme / mouth / lip”
                            const keys  = Object.keys(child.morphTargetDictionary).map(n => n.toLowerCase());
                            const score = keys.filter(n =>
                                n.includes("viseme") || n.includes("mouth") || n.includes("lip")
                            ).length;
    
                            if (!state.skinnedMesh || score > 0) {
                                state.skinnedMesh     = child;
                                state.morphDictionary = child.morphTargetDictionary;
                            }
                        }
                    }
                });

                cacheHairOriginals();
    
                // si no encontró uno “especial”, usa el primero que tenga morphs
                if (!state.skinnedMesh) {
                    gltf.scene.traverse((child) => {
                        if ((child.isMesh || child.isSkinnedMesh) && child.morphTargetDictionary && !state.skinnedMesh) {
                            state.skinnedMesh     = child;
                            state.morphDictionary = child.morphTargetDictionary;
                        }
                    });
                }
    
                // --- animaciones: idle/ojos/habla ---
                // Idle para evitar T-pose: nombres típicos en Avaturn/glTF
                const idleNames = ["avaturn_animation", "idle", "stand", "pose", "rest", "tpose fix"];
                let idleClip = findClipByNames(gltf.animations, idleNames) || (gltf.animations?.[0] ?? null);
                if (idleClip) {
                    const idle = state.mixer.clipAction(idleClip);
                    idle.setLoop(THREE.LoopRepeat, Infinity);
                    idle.enabled = true;
                    idle.play();
                }
    
                // Parpadeo/ojos
                const eyeClip =
                    THREE.AnimationClip.findByName(gltf.animations, "EyesAnimation") ||
                    findClipByNames(gltf.animations, ["eye", "blink"]);
                if (eyeClip) {
                    state.eyeAction = state.mixer.clipAction(eyeClip);
                    state.eyeAction.setLoop(THREE.LoopRepeat, Infinity);
                    state.eyeAction.enabled = true;
                    state.eyeAction.play();
                } else {
                    state.eyeAction = null;
                }
    
                // Habla (gesticulación corporal/facial base del rig)
                const talkClip = THREE.AnimationClip.findByName(gltf.animations, "TalkingAnimation");
                if (talkClip) {
                    state.talkingAction = state.mixer.clipAction(talkClip);
                    state.talkingAction.loop = THREE.LoopRepeat;
                    state.talkingAction.clampWhenFinished = false;
                    // gesto natural (ni exagerado ni acelerado)
                    if (typeof state.talkingAction.setEffectiveWeight === "function") {
                        state.talkingAction.setEffectiveWeight(0.35);
                    } else {
                        state.talkingAction.weight = 0.35;
                    }
                    state.talkingAction.timeScale = 1.0;
                } else {
                    state.talkingAction = null;
                }
    
                // --- encuadre y aplicación de estilo ---
                centerModel();
                state.ready = true;
                applyAppearance(state.pendingAppearance);
    
                // si ya venían visemas en cola, aplicarlos ahora
                if (state.pendingVisemes) {
                    const queued = state.pendingVisemes.slice();
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

        clearBackgroundAssets()

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

    function disposeTexture(tex) {
        try { tex?.dispose?.(); } catch {}
      }
      
    function clearBackgroundAssets() {
        if (state.envTexture) {
            disposeTexture(state.envTexture);
            state.envTexture = null;
        }
        if (state.scene) {
            // asegura que no queden reflejos del entorno anterior
            state.scene.environment = null;
        }
        if (state.backdrop) {
            disposeTexture(state.backdrop.material?.map);
            state.backdrop.geometry?.dispose?.();
            state.backdrop.material?.dispose?.();
            state.scene.remove(state.backdrop);
            state.backdrop = null;
        }
    }      
      
    function ensureBackdrop(texture) {
        if (!state.scene || !state.camera) return;
      
        const mat = new THREE.MeshBasicMaterial({ map: texture, depthWrite: false });
        mat.depthTest = false;                // 👈 asegura que no “luchen” los z-buffers
        const geo = new THREE.PlaneGeometry(2, 2);
        const mesh = new THREE.Mesh(geo, mat);
        mesh.renderOrder = -1000;
        state.scene.add(mesh);
        state.backdrop = mesh;
    }      
      
    // actualiza escala/posición para que llene la vista (llamar en renderLoop)
    function updateBackdrop() {
        if (!state.backdrop || !state.camera) return;
        
        // distancia del plano detrás del target (bastante lejos, pero dentro del far)
        const dist = state.camera.far * 0.8;
        
        // calculamos alto/alto visible en esa distancia
        const vFov = THREE.MathUtils.degToRad(state.camera.fov);
        const height = 2 * Math.tan(vFov / 2) * dist;
        const width  = height * state.camera.aspect;
        
        state.backdrop.scale.set(width, height, 1);
        
        // ponlo justo detrás del target, frente a la cámara
        const dir = new THREE.Vector3();
        state.camera.getWorldDirection(dir);
        const pos = state.controls?.target?.clone?.() ?? new THREE.Vector3(0, 1.5, 0);
        pos.add(dir.multiplyScalar(dist));
        state.backdrop.position.copy(pos);
        state.backdrop.quaternion.copy(state.camera.quaternion);
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
        if (!state.root || !state.camera || !state.controls) return;

        // 🔧 asegura matrices correctas antes del bounding box
        state.root.updateWorldMatrix(true, true);
        // bounding box → sphere
        const box = new THREE.Box3().setFromObject(state.root);
        if (box.isEmpty()) return;
        const sphere = box.getBoundingSphere(new THREE.Sphere());
      
        const center = sphere.center;
        const radius = Math.max(sphere.radius, 0.5);
      
        // target un poco arriba del centro (para ver cabeza/pecho)
        const target = center.clone();
        target.y += radius * 0.15;
        state.controls.target.copy(target);
      
        // coloca la cámara en una diagonal agradable
        const offset = new THREE.Vector3(0.6, 0.55, 1).normalize().multiplyScalar(radius * 2.2);
        state.camera.position.copy(center.clone().add(offset));
      
        // límites de órbita/zoom cómodos
        state.controls.minDistance = radius * 0.6;
        state.controls.maxDistance = radius * 3.0;
        state.controls.minPolarAngle = Math.PI / 4;
        state.controls.maxPolarAngle = Math.PI / 1.9;
      
        state.camera.near = Math.max(radius / 100, 0.01);
        state.camera.far = radius * 30;
        state.camera.updateProjectionMatrix();
        state.controls.update();
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

        const hasHairProp = Object.prototype.hasOwnProperty.call(normalized, "hairColor");
        const hasExplicitHair =
        hasHairProp && normalized.hairColor !== null && typeof normalized.hairColor !== "undefined";

        if (hasHairProp && normalized.hairColor === null) {
        // El usuario pidió "predeterminado": quitamos tinte y volvemos al material original
        resetHairToDefault();
        } else if (hasExplicitHair) {
        applyHairColor(normalized.hairColor);
        } else if (typeof fallbackHair === "number") {
        // Sin selección explícita: usa el color del preset de la vestimenta
        applyHairColor(fallbackHair);
        }
    }

    function isUrlLike(s) {
        return typeof s === 'string' && (s.startsWith('http://') || s.startsWith('https://') || s.startsWith('/'));
    }
      
      // Heurística: ¿2:1 ~ equirect?
    function looksEquirectangular(image) {
        const w = image?.width || 0, h = image?.height || 0;
        if (!w || !h) return false;
        const ratio = w / h;
        return Math.abs(ratio - 2.0) < 0.12; // tolerancia ~6%
    }
      
    function applyBackground(keyOrUrl) {
        if (!state.scene) return;
      
        clearBackgroundAssets(); // limpia env/backdrop previos
      
        // presets por clave
        const preset = backgroundPresets[keyOrUrl] || backgroundPresets.oficina;
      
        // plataforma/“ground”
        if (state.ground) {
          state.ground.visible = true;
          state.ground.material.color.setHex(preset.ground);
        }
      
        // ❑ Caso COLOR PLANO (hex, #rrggbb, nombre)
        if (isColorInput(keyOrUrl)) {
          const col = toThreeColor(keyOrUrl) || new THREE.Color(preset.background);
          state.scene.background  = col;
          state.scene.environment = null;              // sin reflections
          applyBgNumericOptions();                     // aplica blur/intensity aunque no haga efecto con color
          return;
        }
      
        // ❑ Caso URL/Path (imagen)
        if (isUrlLike(keyOrUrl)) {
          const loader = new THREE.TextureLoader();
          loader.setCrossOrigin("anonymous");
          loader.load(
            keyOrUrl,
            (tex) => {
              tex.colorSpace = THREE.SRGBColorSpace;
      
              if (looksEquirectangular(tex.image)) {
                // ambiente real: fondo + environment (reflexiones PBR)
                tex.mapping = THREE.EquirectangularReflectionMapping;
                state.scene.background  = tex;
                state.scene.environment = tex;
                state.envTexture = tex; // para limpiar luego
                applyBgNumericOptions();
              } else {
                // foto normal: fondo como plano detrás (backdrop) + color neutro al fondo de escena
                state.scene.background = new THREE.Color(preset.background);
                ensureBackdrop(tex);
                applyBgNumericOptions(); // rotación no aplica al backdrop, pero intensity/blur no afectan aquí (es MeshBasic)
              }
            },
            undefined,
            () => {
              // Fallback a color si falla la carga
              state.scene.background  = new THREE.Color(preset.background);
              state.scene.environment = null;
              applyBgNumericOptions();
            }
          );
          return;
        }
      
        // ❑ Clave preset conocida
        state.scene.background  = new THREE.Color(preset.background);
        state.scene.environment = null;
        applyBgNumericOptions();
    }  

    function applyBgNumericOptions() {
        const o = state.bgOptions;
        // blurriness/intensity: solo afectan cuando hay textura en background
        state.scene.backgroundBlurriness = THREE.MathUtils.clamp(o.blurriness ?? 0, 0, 1);
        state.scene.backgroundIntensity  = (o.intensity ?? 1);
      
        // environmentIntensity afecta materiales físicos cuando usamos scene.environment
        if ('environmentIntensity' in state.scene) {
          state.scene.environmentIntensity = (o.envIntensity ?? 1);
        }
      
        // Rotación del fondo/entorno (si lo soporta la versión de three)
        const hasBgRot = 'backgroundRotation' in state.scene;
        const hasEnvRot = 'environmentRotation' in state.scene;
        if (hasBgRot || hasEnvRot) {
          const yaw = THREE.MathUtils.degToRad(o.rotationDeg ?? 0);
          const euler = new THREE.Euler(0, yaw, 0, 'YXZ');
          if (hasBgRot)  state.scene.backgroundRotation = euler;
          if (hasEnvRot) state.scene.environmentRotation = euler;
        }
    }
      
    function setBackgroundOptions(opts = {}) {
        state.bgOptions = Object.assign({}, state.bgOptions, opts);
        applyBgNumericOptions();
    }
      
    
    function isColorInput(v) {
        if (typeof v === 'number') return true;
        if (typeof v !== 'string') return false;
        const s = v.trim().toLowerCase();
        if (s.startsWith('#')) return true;
        if (s.startsWith('0x')) return true;
        // nombres CSS simples (opcional): white, black, etc.
        const cssNames = new Set(['white','black','gray','grey','red','green','blue','cyan','magenta','yellow','orange','purple']);
        return cssNames.has(s);
    }
      
    function toThreeColor(v) {
    try {
        if (typeof v === 'number') return new THREE.Color(v);
        const s = String(v).trim();
        if (s.startsWith('0x')) return new THREE.Color(parseInt(s, 16));
        return new THREE.Color(s);
    } catch { return null; }
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

    function applyHairColor(hexOrNumber, strength = 0.45) {
        if (hexOrNumber == null) return;
      
        const namesWanted = new Set([
          'avaturn_hair_0_material','avaturn_hair_1_material','avaturn_hair_0','avaturn_hair_1'
        ]);
      
        const mats = Array.from(state.materials.values());
        const hits = mats.filter(m => {
          const n = (m?.name || '').toLowerCase();
          const looksLikeHair = /(avaturn_hair|hair(_\d+)?|head_hair)/.test(n) && !/scalp|cap/.test(n);
          return looksLikeHair || namesWanted.has(n);
        });
      
        if (!hits.length) return;
      
        // tinte más visible pero sin oscurecer: strength 0.45 + lift 0.22
        hits.forEach(mat => tintStandardLikeMaterial(mat, hexOrNumber, strength, 0.22));
    }
      
      
    function tintStandardLikeMaterial(mat, hexColor, strength = 0.1, lift = 0.30) {
        if (!mat) return;
      
        mat.userData.hairTint ||= {
          color: new THREE.Color(hexColor || 0xffffff),
          strength,
          lift
        };
        if (hexColor != null) mat.userData.hairTint.color.set(hexColor);
        if (typeof strength === 'number') mat.userData.hairTint.strength = THREE.MathUtils.clamp(strength, 0, 1);
        if (typeof lift === 'number')     mat.userData.hairTint.lift     = THREE.MathUtils.clamp(lift, 0, 1);
      
        if (!mat.userData.hairTintHooked) {
          // guarda el onBeforeCompile original UNA sola vez
          if (mat.userData._origOnBeforeCompile === undefined) {
            mat.userData._origOnBeforeCompile = mat.onBeforeCompile;
          }
      
          mat.onBeforeCompile = (shader) => {
            shader.uniforms.uHairTint      = { value: mat.userData.hairTint.color };
            shader.uniforms.uHairStrength  = { value: mat.userData.hairTint.strength };
            shader.uniforms.uHairLift      = { value: mat.userData.hairTint.lift };
      
            shader.fragmentShader = `
              uniform vec3  uHairTint;
              uniform float uHairStrength;
              uniform float uHairLift;
            ` + shader.fragmentShader;
      
            shader.fragmentShader = shader.fragmentShader.replace(
              'vec4 diffuseColor = vec4( diffuse, opacity );',
              `
              vec3 tintMix   = mix(vec3(1.0), uHairTint, uHairStrength);
              vec3 baseTint  = diffuse * tintMix;
      
              float luma     = dot(baseTint, vec3(0.299, 0.587, 0.114));
              vec3 lightened = mix(baseTint, vec3(1.0), uHairLift * (1.0 - luma));
      
              vec4 diffuseColor = vec4(lightened, opacity);
              `
            );
      
            mat.userData.hairTintShader = shader;
          };
      
          mat.userData.hairTintHooked = true;   // ← marcado
        }
      
        const sh = mat.userData.hairTintShader;
        if (sh?.uniforms) {
          sh.uniforms.uHairTint.value.copy(mat.userData.hairTint.color);
          sh.uniforms.uHairStrength.value = mat.userData.hairTint.strength;
          sh.uniforms.uHairLift.value     = mat.userData.hairTint.lift;
        }
      
        mat.needsUpdate = true;
    }      
            
    function resetHairToDefault() {
        const mats = Array.from(state.materials.values());
        for (const mat of mats) {
          if (!mat || !mat.name) continue;
          const n = mat.name.toLowerCase();
      
          // Solo materiales que TENÍAN tinte activo y NO scalp/cap
          const isHairNamed = /(avaturn_hair|hair(_\d+)?|head_hair)/.test(n);
          const isScalpOrCap = /scalp|cap/.test(n);
          const hadTint = !!mat.userData?.hairTintHooked;
      
          if (!(isHairNamed && hadTint) || isScalpOrCap) continue;
      
          // restaurar onBeforeCompile original
          if (mat.userData && '._origOnBeforeCompile' in mat.userData === false) {
            // nada
          }
          mat.onBeforeCompile = (mat.userData?._origOnBeforeCompile) || undefined;
      
          // restaurar mapa y color SOLO si los teníamos guardados
          if (mat.userData?.origMap !== undefined)  mat.map = mat.userData.origMap;
          if (mat.userData?.origColor && mat.color) mat.color.copy(mat.userData.origColor);
      
          // limpiar marcas
          if (mat.userData) {
            delete mat.userData.hairTint;
            delete mat.userData.hairTintShader;
            delete mat.userData.hairTintHooked;
            // conservamos _origOnBeforeCompile por si se vuelve a teñir y resetear
          }
      
          mat.needsUpdate = true;
        }
    }  

    function cacheHairOriginals() {
        for (const mat of state.materials.values()) {
          if (!mat || !mat.name) continue;
          const n = mat.name.toLowerCase();
          const isHair = /(avaturn_hair|hair(_\d+)?|head_hair)/.test(n) && !/scalp|cap/.test(n);
          if (!isHair) continue;
      
          mat.userData ||= {};
          if (!('origMap' in mat.userData))   mat.userData.origMap = mat.map || null;
          if (!('origColor' in mat.userData) && mat.color) {
            mat.userData.origColor = mat.color.clone();
          }
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

    function playTalking(durationMs, opts = {}) {
        if (!state.talkingAction) return;
      
        const clip = state.talkingAction.getClip?.();
        const desiredMs = (typeof durationMs === "number" && durationMs > 0) ? durationMs : 80000;
      
        // Opciones
        const {
          startPhase = 0.9,     // 0 = inicio, 0.5 = mitad, 0.75 = último cuarto
          minSpeed  = 0.6,      // límites para que no se vea extraño
          maxSpeed  = 3.0,
          weight    = 0.45      // intensidad de los gestos
        } = opts;
      
        // Peso (intensidad) natural
        if (typeof state.talkingAction.setEffectiveWeight === 'function') {
          state.talkingAction.setEffectiveWeight(weight);
        } else {
          state.talkingAction.weight = weight;
        }
      
        let loops = 1;
        let speed = 1;
      
        if (clip && clip.duration > 0) {
          const clipSec = clip.duration;
          const desiredSec = desiredMs / 1000;
      
          // nº de repeticiones enteras que mejor “rellenan” la duración deseada
          loops = Math.max(Math.round(desiredSec / clipSec), 1);
      
          // timeScale para que (loops * clipSec) ≈ desiredSec
          speed = (loops * clipSec) / desiredSec;
          speed = Math.min(Math.max(speed, minSpeed), maxSpeed);
      
          state.talkingAction.setLoop(THREE.LoopRepeat, loops);
      
          if (typeof state.talkingAction.setEffectiveTimeScale === 'function') {
            state.talkingAction.setEffectiveTimeScale(speed);
          } else {
            state.talkingAction.timeScale = speed;
          }
      
          // Arranca en un punto avanzado del ciclo
          const offset = THREE.MathUtils.clamp(startPhase, 0, 1) * clipSec;
          state.talkingAction.time = offset;
        } else {
          // Si no hubiera clip válido, cae a un bucle suave
          state.talkingAction.setLoop(THREE.LoopRepeat, Infinity);
          state.talkingAction.timeScale = 1.0;
        }
      
        state.talkingAction.enabled = true;
        state.talkingAction.play();
      
        clearTimeout(state.talkingTimeout);
        state.talkingTimeout = setTimeout(() => stopTalking(), desiredMs);
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

        function toSeconds(frame) {
            if (!frame) {
                return 0;
            }

            if (typeof frame.time === "number" && Number.isFinite(frame.time)) {
                return frame.time > 20 ? frame.time / 1000 : frame.time;
            }

            if (typeof frame.tiempo === "number" && Number.isFinite(frame.tiempo)) {
                return frame.tiempo / 1000;
            }

            if (typeof frame.timestamp === "number" && Number.isFinite(frame.timestamp)) {
                return frame.timestamp > 20 ? frame.timestamp / 1000 : frame.timestamp;
            }

            return 0;
        }


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

                const frameTimeSeconds = toSeconds(frame);
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
        if (state.backdrop) updateBackdrop();
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

    function resetAllMorphs(root) {
        root.traverse((child) => {
            if ((child.isMesh || child.isSkinnedMesh) && child.morphTargetInfluences) {
                for (let i = 0; i < child.morphTargetInfluences.length; i++) {
                    child.morphTargetInfluences[i] = 0;
                }
            }
        });
    }
    
    function findClipByNames(clips, names) {
        if (!clips || !clips.length) return null;
        const lower = names.map(n => n.toLowerCase());
        for (const c of clips) {
          const n = (c?.name || "").toLowerCase();
          if (n && lower.some(k => n.includes(k))) return c;
        }
        return null;
    }

    async function playAudioWithVisemes(audioElement, audioUrl, visemes, talkingOpts = {}) {
        // 1) prepara audio y visemas
        const ms = await prepareAudioClip(audioElement, audioUrl);
        applyVisemes(visemes);
    
        // 2) anima gestos en paralelo
        const opts = Object.assign(
        { startPhase: 0.7, minSpeed: 0.8, maxSpeed: 2.2, weight: 0.45 },
        talkingOpts || {}
        );
        playTalking(ms, opts);
    
        // 3) dispara el audio (esto activa el loop de visemas)
        await playPreparedAudioClip(audioElement);
    }
    
    /**
     * Llama al endpoint de anuncio y reproduce el resultado.
     * @param {HTMLAudioElement} audioElement  <audio> donde sonará la voz
     * @param {Object} announcePayload         { empresa, sede, modulo, turno, nombre }
     * @param {Object} options                 { baseUrl, idioma, voz, talkingOpts }
     */
    async function announceAndPlay(audioElement, announcePayload, options = {}) {
        const baseUrl = options.baseUrl || ""; // mismo host por defecto
        const idioma  = options.idioma  || "es";
        const voz     = options.voz     || ""; // vacío = que backend elija
    
        const url = `${baseUrl}/api/avatar/announce?idioma=${encodeURIComponent(idioma)}${voz ? `&voz=${encodeURIComponent(voz)}` : ""}`;
    
        const res = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(announcePayload || {})
        });
    
        if (!res.ok) {
        const txt = await res.text().catch(() => "");
        throw new Error(`Announce fallo (${res.status}): ${txt}`);
        }
    
        const data = await res.json();
    
        // data.audioUrl (string) + data.visemas (array)
        await playAudioWithVisemes(audioElement, data.audioUrl, data.visemas, options.talkingOpts);
        return data; // por si quieres leer data.texto, etc.
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
    AvatarViewer.setBackgroundOptions = setBackgroundOptions;
    AvatarViewer.dispose = dispose;

    AvatarViewer.playAudioWithVisemes   = playAudioWithVisemes;
    AvatarViewer.announceAndPlay        = announceAndPlay;

    global.AvatarViewer = AvatarViewer;
})(globalScope);
