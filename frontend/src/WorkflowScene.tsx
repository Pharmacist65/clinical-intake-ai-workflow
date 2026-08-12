import { useEffect, useRef, useState } from "react";
import {
  BoxGeometry,
  BufferGeometry,
  Color,
  DirectionalLight,
  EdgesGeometry,
  Fog,
  GridHelper,
  Group,
  HemisphereLight,
  Line,
  LineBasicMaterial,
  LineSegments,
  Material,
  Mesh,
  MeshStandardMaterial,
  PCFShadowMap,
  PerspectiveCamera,
  PlaneGeometry,
  Scene,
  SRGBColorSpace,
  Timer,
  Vector2,
  Vector3,
  WebGLRenderer
} from "three";

const workflowNodes = [
  { label: "Intake", detail: "fictional source", color: "#0f766e" },
  { label: "Context", detail: "source capture", color: "#2563eb" },
  { label: "Provenance", detail: "evidence link", color: "#7c3aed" },
  { label: "Summary", detail: "deterministic mock", color: "#c2410c" },
  { label: "Review", detail: "human decision", color: "#b45309" },
  { label: "Audit", detail: "traceable state", color: "#15803d" }
];

export function WorkflowScene() {
  const hostRef = useRef<HTMLDivElement>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const host = hostRef.current;
    if (!host) return;

    const scene = new Scene();
    scene.background = new Color("#f4f7f7");
    scene.fog = new Fog("#f4f7f7", 16, 32);

    const camera = new PerspectiveCamera(34, 1, 0.1, 100);
    camera.position.set(0, 4.5, 15);
    camera.lookAt(0, 0, 0);

    const renderer = new WebGLRenderer({ antialias: true, alpha: false });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.75));
    renderer.outputColorSpace = SRGBColorSpace;
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = PCFShadowMap;
    renderer.domElement.setAttribute("aria-hidden", "true");
    renderer.domElement.setAttribute("data-workflow-canvas", "ready");
    host.prepend(renderer.domElement);

    scene.add(new HemisphereLight("#ffffff", "#9aa8a5", 2.1));
    const keyLight = new DirectionalLight("#ffffff", 3.4);
    keyLight.position.set(-4, 7, 8);
    keyLight.castShadow = true;
    scene.add(keyLight);

    const floor = new Mesh(
      new PlaneGeometry(24, 8),
      new MeshStandardMaterial({ color: "#e8eeee", roughness: 0.95, metalness: 0 })
    );
    floor.rotation.x = -Math.PI / 2;
    floor.position.y = -1.2;
    floor.receiveShadow = true;
    scene.add(floor);

    const group = new Group();
    scene.add(group);
    const positions = workflowNodes.map((_, index) =>
      new Vector3(-6.6 + index * 2.64, index % 2 === 0 ? 0.28 : -0.08, index % 3 === 1 ? -0.4 : 0.2)
    );

    const materials: Material[] = [];
    const geometries: BufferGeometry[] = [];
    const nodeMeshes: Mesh[] = [];

    workflowNodes.forEach((node, index) => {
      const geometry = new BoxGeometry(1.42, 1.04, 1.42, 2, 2, 2);
      const material = new MeshStandardMaterial({
        color: node.color,
        roughness: 0.52,
        metalness: 0.08
      });
      geometries.push(geometry);
      materials.push(material);
      const mesh = new Mesh(geometry, material);
      mesh.position.copy(positions[index]);
      mesh.rotation.set(0.08, -0.28, index % 2 === 0 ? -0.03 : 0.03);
      mesh.castShadow = true;
      mesh.receiveShadow = true;
      nodeMeshes.push(mesh);
      group.add(mesh);

      const edgeGeometry = new EdgesGeometry(geometry);
      const edgeMaterial = new LineBasicMaterial({ color: "#ffffff", transparent: true, opacity: 0.44 });
      geometries.push(edgeGeometry);
      materials.push(edgeMaterial);
      mesh.add(new LineSegments(edgeGeometry, edgeMaterial));
    });

    const connectorMaterial = new LineBasicMaterial({ color: "#7b8f8b", transparent: true, opacity: 0.7 });
    materials.push(connectorMaterial);
    for (let index = 0; index < positions.length - 1; index += 1) {
      const geometry = new BufferGeometry().setFromPoints([positions[index], positions[index + 1]]);
      geometries.push(geometry);
      group.add(new Line(geometry, connectorMaterial));
    }

    const packetGeometry = new BoxGeometry(0.16, 0.16, 0.16);
    const packetMaterial = new MeshStandardMaterial({ color: "#172c2a", emissive: "#172c2a", emissiveIntensity: 0.2 });
    geometries.push(packetGeometry);
    materials.push(packetMaterial);
    const packets = positions.slice(0, -1).map((position, index) => {
      const packet = new Mesh(packetGeometry, packetMaterial);
      packet.position.copy(position);
      packet.castShadow = true;
      group.add(packet);
      return { mesh: packet, from: positions[index], to: positions[index + 1], offset: index / (positions.length - 1) };
    });

    const grid = new GridHelper(22, 22, "#b8c7c4", "#d8e1df");
    grid.position.y = -1.18;
    const gridMaterials = Array.isArray(grid.material) ? grid.material : [grid.material];
    gridMaterials.forEach((material) => {
      material.transparent = true;
      material.opacity = 0.48;
      materials.push(material);
    });
    scene.add(grid);

    const pointer = new Vector2();
    const targetRotation = new Vector2();
    const onPointerMove = (event: PointerEvent) => {
      const bounds = host.getBoundingClientRect();
      pointer.x = ((event.clientX - bounds.left) / bounds.width) * 2 - 1;
      pointer.y = -((event.clientY - bounds.top) / bounds.height) * 2 + 1;
      targetRotation.set(pointer.y * 0.05, pointer.x * 0.1);
      renderer.domElement.dataset.pointerInput = `${pointer.x.toFixed(2)},${pointer.y.toFixed(2)}`;
    };
    host.addEventListener("pointermove", onPointerMove);

    const resize = () => {
      const width = Math.max(host.clientWidth, 1);
      const height = Math.max(host.clientHeight, 1);
      renderer.setSize(width, height, false);
      camera.aspect = width / height;
      camera.position.z = width < 680 ? 22.5 : 15;
      camera.position.y = width < 680 ? 4.2 : 4.5;
      camera.updateProjectionMatrix();
    };
    const resizeObserver = new ResizeObserver(resize);
    resizeObserver.observe(host);
    resize();

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const timer = new Timer();
    timer.connect(document);
    let frameId = 0;
    let hasMarkedReady = false;
    let frameCount = 0;

    const render = (timestamp?: number) => {
      timer.update(timestamp);
      const elapsed = timer.getElapsed();
      group.rotation.x += (targetRotation.x - group.rotation.x) * 0.035;
      group.rotation.y += (targetRotation.y - group.rotation.y) * 0.035;

      nodeMeshes.forEach((mesh, index) => {
        mesh.position.y = positions[index].y + Math.sin(elapsed * 0.7 + index * 0.8) * 0.08;
      });
      packets.forEach((packet, index) => {
        const progress = (elapsed * 0.14 + packet.offset + index * 0.03) % 1;
        packet.mesh.position.lerpVectors(packet.from, packet.to, progress);
        packet.mesh.position.y += 0.12;
      });

      renderer.render(scene, camera);
      frameCount += 1;
      if (frameCount % 15 === 0 || frameCount === 1) {
        renderer.domElement.dataset.frame = String(frameCount);
      }
      if (!hasMarkedReady) {
        hasMarkedReady = true;
        setReady(true);
      }
      if (!reducedMotion) frameId = window.requestAnimationFrame(render);
    };
    render();

    return () => {
      window.cancelAnimationFrame(frameId);
      resizeObserver.disconnect();
      host.removeEventListener("pointermove", onPointerMove);
      timer.dispose();
      geometries.forEach((geometry) => geometry.dispose());
      materials.forEach((material) => material.dispose());
      renderer.dispose();
      renderer.domElement.remove();
    };
  }, []);

  return (
    <section className="workflow-visual" aria-label="Evidence-linked intake workflow">
      <div className="workflow-visual-heading">
        <div>
          <p className="eyebrow dark">Evidence spine</p>
          <h3>Every output stays connected to source and review state.</h3>
        </div>
        <span className="visual-status"><span aria-hidden="true" /> deterministic flow</span>
      </div>
      <div className="workflow-stage" ref={hostRef} data-rendered={ready ? "true" : "false"}>
        <div className="workflow-labels" aria-hidden="true">
          {workflowNodes.map((node) => (
            <span key={node.label}>
              <strong>{node.label}</strong>
              <small>{node.detail}</small>
            </span>
          ))}
        </div>
      </div>
    </section>
  );
}
