import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';
import { GLTFLoader } from 'https://cdn.jsdelivr.net/npm/three@0.160.0/examples/jsm/loaders/GLTFLoader.js';

const outfitPalettes = {
    corporativo: { color: '#1c8f6d', emissive: '#174d3c' },
    ejecutivo: { color: '#1b4332', emissive: '#0f241c' },
    casual: { color: '#0d6efd', emissive: '#11284a' }
};

const backgroundPalettes = {
    oficina: { light: '#f7f8fb', ground: '#f2f6f9' },
    moderno: { light: '#f2f3ff', ground: '#e1e0ff' },
    naturaleza: { light: '#f6fff7', ground: '#d6f5e3' },
    default: { light: '#ffffff', ground: '#f0f0f0' }
};

const defaultOutfit = outfitPalettes.corporativo;

export async function createViewer(canvas, options) {
    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: true });
    renderer.outputEncoding = THREE.sRGBEncoding;
    renderer.setPixelRatio(window.devicePixelRatio ?? 1);
    renderer.shadowMap.enabled = true;

    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(35, 1, 0.1, 100);
    camera.position.set(0, 1.55, 3.2);

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

    const groundMaterial = new THREE.MeshStandardMaterial({ color: backgroundPalettes.default.ground, roughness: 0.9, metalness: 0.05 });
    const ground = new THREE.Mesh(new THREE.CircleGeometry(3, 48), groundMaterial);
    ground.rotation.x = -Math.PI / 2;
    ground.position.y = 0;
    ground.receiveShadow = true;
    scene.add(ground);

    const loader = new GLTFLoader();
    loader.setCrossOrigin('anonymous');

    const state = {
        renderer,
        scene,
        camera,
        canvas,
        keyLight,
        groundMaterial,
        outfitMaterials: new Set(),
        logoMaterial: null,
        logoTexture: null,
        resizeObserver: null,
        animationHandle: 0,
        root: null,
        clock: new THREE.Clock()
    };

    let root;
    if (options?.modelUrl) {
        try {
            const gltf = await loader.loadAsync(options.modelUrl);
            root = gltf.scene;
            root.traverse((child) => {
                if (!child.isMesh) {
                    return;
                }

                child.castShadow = true;
                child.receiveShadow = true;
                const lower = child.name?.toLowerCase?.() ?? '';

                if (/(shirt|cloth|outfit|jacket|torso|body)/.test(lower)) {
                    collectMaterial(state.outfitMaterials, child.material);
                }

                if (!state.logoMaterial && /(logo|badge|emblem)/.test(lower)) {
                    state.logoMaterial = ensureSingleMaterial(child);
                }
            });
        } catch (error) {
            console.warn('No se pudo cargar el modelo GLB. Se usará una silueta base.', error);
        }
    }

    if (!root) {
        root = createFallbackAvatar(state.outfitMaterials);
        state.logoMaterial = createLogoPlane(root);
    } else {
        if (!state.logoMaterial) {
            state.logoMaterial = createLogoPlane(root);
        }
    }

    if (!state.logoMaterial) {
        state.logoMaterial = createLogoPlane(root);
    }

    state.logoMaterial.transparent = true;
    state.logoMaterial.opacity = 0;

    root.position.y = 0;
    scene.add(root);
    state.root = root;

    renderer.setClearColor(0x000000, 0);
    resizeRenderer(state);

    const resizeObserver = new ResizeObserver(() => resizeRenderer(state));
    resizeObserver.observe(canvas);
    state.resizeObserver = resizeObserver;

    const renderLoop = () => {
        state.animationHandle = window.requestAnimationFrame(renderLoop);
        const delta = state.clock.getDelta();
        if (state.root) {
            state.root.rotation.y += delta * 0.35;
        }

        renderer.render(scene, camera);
    };

    renderLoop();

    if (options?.logoUrl) {
        await applyLogo(state, options.logoUrl);
    }

    if (options?.outfit) {
        applyOutfit(state, options.outfit);
    } else {
        applyOutfit(state, null);
    }

    if (options?.background) {
        applyBackground(state, options.background);
    } else {
        applyBackground(state, null);
    }

    return {
        setLogo: (value) => applyLogo(state, value),
        setOutfit: (value) => applyOutfit(state, value),
        setBackground: (value) => applyBackground(state, value),
        dispose: () => disposeViewer(state)
    };
}

function collectMaterial(collection, material) {
    if (!material) {
        return;
    }

    if (Array.isArray(material)) {
        material.forEach((m) => collectMaterial(collection, m));
        return;
    }

    collection.add(material);
}

function ensureSingleMaterial(mesh) {
    if (!mesh.material) {
        mesh.material = new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0 });
        return mesh.material;
    }

    if (Array.isArray(mesh.material)) {
        const material = mesh.material[0];
        mesh.material = material;
        return material;
    }

    return mesh.material;
}

function createLogoPlane(root) {
    const material = new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0 });
    const plane = new THREE.Mesh(new THREE.PlaneGeometry(0.55, 0.55), material);
    plane.position.set(0, 1.55, 0.52);
    root.add(plane);
    return material;
}

function createFallbackAvatar(outfitMaterials) {
    const group = new THREE.Group();

    const bodyMaterial = new THREE.MeshStandardMaterial({ color: defaultOutfit.color, roughness: 0.55, metalness: 0.1 });
    const body = new THREE.Mesh(new THREE.CapsuleGeometry(0.45, 1.6, 24, 32), bodyMaterial);
    body.position.y = 1.4;
    body.castShadow = true;
    body.receiveShadow = true;
    group.add(body);
    outfitMaterials.add(bodyMaterial);

    const headMaterial = new THREE.MeshStandardMaterial({ color: 0xffe0bd, roughness: 0.6, metalness: 0.1 });
    const head = new THREE.Mesh(new THREE.SphereGeometry(0.32, 32, 32), headMaterial);
    head.position.set(0, 2.35, 0);
    head.castShadow = true;
    head.receiveShadow = true;
    group.add(head);

    return group;
}

async function applyLogo(state, url) {
    if (!state.logoMaterial) {
        return;
    }

    if (state.logoTexture) {
        state.logoTexture.dispose();
        state.logoTexture = null;
    }

    if (!url) {
        state.logoMaterial.map = null;
        state.logoMaterial.opacity = 0;
        state.logoMaterial.needsUpdate = true;
        return;
    }

    const loader = new THREE.TextureLoader();
    loader.setCrossOrigin('anonymous');

    try {
        const texture = await loader.loadAsync(url);
        texture.colorSpace = THREE.SRGBColorSpace;
        texture.anisotropy = Math.min(8, state.renderer.capabilities.getMaxAnisotropy?.() ?? 4);
        state.logoMaterial.map = texture;
        state.logoMaterial.opacity = 1;
        state.logoMaterial.needsUpdate = true;
        state.logoTexture = texture;
    } catch (error) {
        console.warn('No fue posible cargar la textura del logo.', error);
        state.logoMaterial.map = null;
        state.logoMaterial.opacity = 0;
        state.logoMaterial.needsUpdate = true;
    }
}

function applyOutfit(state, value) {
    const key = (value ?? '').toString().toLowerCase();
    const palette = outfitPalettes[key] ?? defaultOutfit;

    state.outfitMaterials.forEach((material) => {
        if (!material) {
            return;
        }

        if (material.color) {
            material.color.set(palette.color);
        }

        if (material.emissive && palette.emissive) {
            material.emissive.set(palette.emissive);
        }

        material.needsUpdate = true;
    });
}

function applyBackground(state, value) {
    const key = (value ?? '').toString().toLowerCase();
    const palette = backgroundPalettes[key] ?? backgroundPalettes.default;

    state.keyLight.color.set(palette.light);
    if (state.groundMaterial && state.groundMaterial.color) {
        state.groundMaterial.color.set(palette.ground);
        state.groundMaterial.needsUpdate = true;
    }
}

function resizeRenderer(state) {
    const canvas = state.canvas;
    if (!canvas) {
        return;
    }

    const width = canvas.clientWidth;
    const height = canvas.clientHeight;
    if (width === 0 || height === 0) {
        return;
    }

    state.renderer.setSize(width, height, false);
    state.camera.aspect = width / height;
    state.camera.updateProjectionMatrix();
}

async function disposeViewer(state) {
    if (state.animationHandle) {
        window.cancelAnimationFrame(state.animationHandle);
        state.animationHandle = 0;
    }

    if (state.resizeObserver) {
        state.resizeObserver.disconnect();
        state.resizeObserver = null;
    }

    if (state.logoTexture) {
        state.logoTexture.dispose();
        state.logoTexture = null;
    }

    state.renderer.dispose();
}
