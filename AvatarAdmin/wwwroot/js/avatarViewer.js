window.avatarViewer = {
    show: async function (elementId) {
        const [{ GLTFLoader }, THREE] = await Promise.all([
            import('https://cdn.jsdelivr.net/npm/three@0.160.0/examples/jsm/loaders/GLTFLoader.js'),
            import('https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js')
        ]);
        const scene = new THREE.Scene();
        const camera = new THREE.PerspectiveCamera(75, 1, 0.1, 1000);
        const renderer = new THREE.WebGLRenderer({ antialias: true });
        renderer.setSize(400, 400);
        const container = document.getElementById(elementId);
        if (!container) return;
        container.innerHTML = '';
        container.appendChild(renderer.domElement);
        const light = new THREE.HemisphereLight(0xffffff, 0x444444);
        scene.add(light);
        const loader = new GLTFLoader();
        loader.load('https://modelviewer.dev/shared-assets/models/Astronaut.glb', gltf => {
            scene.add(gltf.scene);
            camera.position.z = 2;
            renderer.render(scene, camera);
        });
    }
};
