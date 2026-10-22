import * as THREE from "three";
import { PLAY } from "./constants";
import type { Obstacle, Quality } from "./types";

export type DrawCar = {
  x: number;
  z: number;
  yaw: number;
  vx: number;
  vz: number;
  speed: number;
  turret: number;
  wheel: number;
  color: number;
  scale: [number, number, number];
  dead: boolean;
  nitro: boolean;
  player: boolean;
};

export type DrawBullet = { x: number; z: number; yaw: number; kind: string; alive: boolean };
export type DrawLoot = { x: number; z: number; kind: string; active: boolean };

export type FrameState = {
  mode: "showcase" | "battle";
  time: number;
  cars: DrawCar[];
  bullets: DrawBullet[];
  loot: DrawLoot[];
  obstacles: Obstacle[];
  zone: { x: number; z: number; r: number; shrinking: boolean };
  shield: number;
  cam: { x: number; y: number; z: number; lx: number; ly: number; lz: number; fov: number };
  quality: Quality;
};

type Puff = { x: number; y: number; z: number; vx: number; vy: number; vz: number; life: number; max: number; size: number; color: THREE.Color };

const CROWD_MAX = 360;

export class ArenaView {
  readonly renderer: THREE.WebGLRenderer;
  readonly scene = new THREE.Scene();
  readonly camera = new THREE.PerspectiveCamera(62, 1, 0.1, 220);
  private obstacles: Obstacle[] = [];
  private crowdN = 140;
  private dustAcc = 0;
  private crowd: { a: number; r: number; phase: number; y: number; tint: THREE.Color }[] = [];
  private bodyIM: THREE.InstancedMesh;
  private headIM: THREE.InstancedMesh;
  private eyeIM: THREE.InstancedMesh;
  private blockIM: THREE.InstancedMesh;
  private bannerIM: THREE.InstancedMesh;
  private wallIM: THREE.InstancedMesh;
  private shaftIM: THREE.InstancedMesh;
  private obsMesh = new Map<string, THREE.InstancedMesh>();
  private body: THREE.InstancedMesh;
  private cage: THREE.InstancedMesh;
  private turret: THREE.InstancedMesh;
  private gun: THREE.InstancedMesh;
  private wheels: THREE.InstancedMesh;
  private rims: THREE.InstancedMesh;
  private lamps: THREE.InstancedMesh;
  private bulletIM: THREE.InstancedMesh;
  private lootIM: THREE.InstancedMesh;
  private points: THREE.Points;
  private puffs: Puff[] = [];
  private puffPos: Float32Array;
  private puffCol: Float32Array;
  private zoneMat: THREE.ShaderMaterial;
  private shieldMat: THREE.ShaderMaterial;
  private ring: THREE.Mesh;
  private dummy = new THREE.Object3D();
  private dummy2 = new THREE.Object3D();
  private mOut = new THREE.Matrix4();
  private color = new THREE.Color();
  private quality: Quality = "HIGH";
  private sand: THREE.Mesh;

  constructor(canvas: HTMLCanvasElement) {
    this.renderer = new THREE.WebGLRenderer({
      canvas,
      antialias: true,
      alpha: false,
      powerPreference: "high-performance",
    });
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1.22;
    this.renderer.setClearColor(0x8a2e22, 1);
    this.scene.background = new THREE.Color(0xc45a32);
    this.scene.fog = new THREE.FogExp2(0xc56a3a, 0.012);
    this.scene.add(this.sky());

    const hemi = new THREE.HemisphereLight(0xffb089, 0x6a4030, 0.85);
    this.scene.add(hemi);
    const sun = new THREE.DirectionalLight(0xffc2a0, 1.65);
    sun.position.set(-18, 36, 22);
    sun.castShadow = true;
    sun.shadow.mapSize.set(1024, 1024);
    sun.shadow.camera.near = 10;
    sun.shadow.camera.far = 120;
    sun.shadow.camera.left = -40;
    sun.shadow.camera.right = 40;
    sun.shadow.camera.top = 40;
    sun.shadow.camera.bottom = -40;
    this.scene.add(sun);
    const rim = new THREE.DirectionalLight(0xff6a3a, 0.55);
    rim.position.set(-20, 12, -10);
    this.scene.add(rim);

    const apron = new THREE.Mesh(
      new THREE.CircleGeometry(78, 48),
      new THREE.MeshStandardMaterial({ color: 0x8a5a32, roughness: 1, metalness: 0 }),
    );
    apron.rotation.x = -Math.PI / 2;
    apron.position.y = -0.05;
    this.scene.add(apron);

    this.sand = new THREE.Mesh(
      new THREE.CircleGeometry(PLAY + 1.2, 64),
      new THREE.MeshStandardMaterial({ map: this.sandTex(), color: 0xffffff, roughness: 0.92, metalness: 0.02 }),
    );
    this.sand.rotation.x = -Math.PI / 2;
    this.sand.receiveShadow = true;
    this.scene.add(this.sand);

    this.zoneMat = new THREE.ShaderMaterial({
      transparent: true,
      depthWrite: false,
      uniforms: {
        uCenter: { value: new THREE.Vector2(0, 0) },
        uInner: { value: PLAY },
        uColor: { value: new THREE.Color(0xd32f2f) },
      },
      vertexShader: `
        varying vec3 wpos;
        void main() {
          vec4 wp = modelMatrix * vec4(position, 1.0);
          wpos = wp.xyz;
          gl_Position = projectionMatrix * viewMatrix * wp;
        }
      `,
      fragmentShader: `
        uniform vec2 uCenter;
        uniform float uInner;
        uniform vec3 uColor;
        varying vec3 wpos;
        void main() {
          float d = distance(wpos.xz, uCenter);
          float a = smoothstep(uInner - 0.15, uInner + 4.0, d);
          a = min(a, 0.5);
          if (a < 0.02) discard;
          gl_FragColor = vec4(uColor, a * 0.85);
        }
      `,
    });
    const zoneDisc = new THREE.Mesh(new THREE.CircleGeometry(PLAY + 6, 64), this.zoneMat);
    zoneDisc.rotation.x = -Math.PI / 2;
    zoneDisc.position.y = 0.04;
    this.scene.add(zoneDisc);

    this.ring = new THREE.Mesh(
      new THREE.TorusGeometry(1, 0.08, 8, 80),
      new THREE.MeshBasicMaterial({ color: 0xff8f00 }),
    );
    this.ring.rotation.x = Math.PI / 2;
    this.ring.position.y = 0.35;
    this.scene.add(this.ring);

    this.shieldMat = new THREE.ShaderMaterial({
      transparent: true,
      depthWrite: false,
      side: THREE.DoubleSide,
      uniforms: {
        uPulse: { value: 0 },
        uTime: { value: 0 },
      },
      vertexShader: `
        varying vec2 vUv;
        varying vec3 vN;
        void main() {
          vUv = uv;
          vN = normalize(normalMatrix * normal);
          gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
        }
      `,
      fragmentShader: `
        uniform float uPulse;
        uniform float uTime;
        varying vec2 vUv;
        varying vec3 vN;
        void main() {
          float hx = abs(fract(vUv.x * 36.0) - 0.5);
          float hy = abs(fract(vUv.y * 7.0 + vUv.x * 0.5) - 0.5);
          float grid = smoothstep(0.46, 0.5, hx) + smoothstep(0.42, 0.5, hy);
          float fres = pow(1.0 - abs(dot(normalize(vN), vec3(0.0, 0.0, 1.0))), 1.6);
          float scan = 0.35 + 0.65 * sin(vUv.y * 18.0 - uTime * 3.0);
          float a = (0.04 + grid * 0.25 * scan + fres * 0.08) * (0.25 + uPulse);
          gl_FragColor = vec4(1.0, 0.55, 0.2, a);
        }
      `,
    });
    const shield = new THREE.Mesh(new THREE.CylinderGeometry(38.4, 38.4, 7.2, 64, 1, true), this.shieldMat);
    shield.position.y = 3.4;
    shield.visible = false;
    this.scene.add(shield);

    this.blockIM = this.colosseum();
    this.bannerIM = this.banners();
    this.wallIM = this.innerWall();
    this.shaftIM = this.shafts();
    const crowd = this.makeCrowd();
    this.bodyIM = crowd.body;
    this.headIM = crowd.head;
    this.eyeIM = crowd.eyes;
    this.floods();

    const stone = new THREE.MeshStandardMaterial({ color: 0xd9c4a2, roughness: 0.86 });
    const concrete = new THREE.MeshStandardMaterial({ color: 0xb7a48c, roughness: 0.8 });
    const rubble = new THREE.MeshStandardMaterial({ color: 0x6b5344, roughness: 0.95 });
    const tire = new THREE.MeshStandardMaterial({ color: 0x1a1614, roughness: 0.7 });
    const wreck = new THREE.MeshStandardMaterial({ color: 0x2a2428, roughness: 0.55, metalness: 0.35 });
    const barrel = new THREE.MeshStandardMaterial({ color: 0xb5522a, roughness: 0.5, metalness: 0.2 });
    this.obsMesh.set("column", this.pool(new THREE.CylinderGeometry(0.7, 0.85, 3.2, 8), stone, 12, true));
    this.obsMesh.set("concrete", this.pool(new THREE.BoxGeometry(2.2, 1.1, 0.7), concrete, 12, true));
    this.obsMesh.set("rubble", this.pool(new THREE.DodecahedronGeometry(0.85, 0), rubble, 12, true));
    this.obsMesh.set("tires", this.pool(new THREE.CylinderGeometry(0.55, 0.55, 0.7, 10), tire, 12));
    this.obsMesh.set("wreck", this.pool(new THREE.BoxGeometry(1.5, 0.7, 2.6), wreck, 12, true));
    this.obsMesh.set("barrel", this.pool(new THREE.CylinderGeometry(0.32, 0.32, 0.7, 8), barrel, 8));

    const bodyMat = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.42, metalness: 0.48 });
    const gunMat = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.35, metalness: 0.7 });
    const lampMat = new THREE.MeshStandardMaterial({
      color: 0xfff6d0,
      emissive: 0xfff1c4,
      emissiveIntensity: 2.2,
      roughness: 0.2,
    });
    this.body = this.pool(this.buggyShell(), bodyMat, 16, true);
    this.cage = this.pool(this.buggyCage(), new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.15, metalness: 0.7 }), 16, true);
    this.turret = this.pool(new THREE.BoxGeometry(0.28, 0.16, 0.36), gunMat, 16);
    this.gun = this.pool(new THREE.BoxGeometry(0.08, 0.08, 0.7), gunMat, 16);
    const wheelGeo = new THREE.CylinderGeometry(0.42, 0.42, 0.32, 12);
    wheelGeo.rotateZ(Math.PI / 2);
    this.wheels = this.pool(wheelGeo, new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.8 }), 64, true);
    const rimGeo = new THREE.CylinderGeometry(0.2, 0.2, 0.34, 8);
    rimGeo.rotateZ(Math.PI / 2);
    this.rims = this.pool(rimGeo, new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.3, metalness: 0.8 }), 64);
    this.lamps = this.pool(new THREE.BoxGeometry(0.22, 0.14, 0.08), lampMat, 32);
    this.bulletIM = this.pool(
      new THREE.BoxGeometry(0.08, 0.08, 0.7),
      new THREE.MeshBasicMaterial({ color: 0xffe08a }),
      96,
    );
    this.lootIM = this.pool(
      new THREE.OctahedronGeometry(0.38, 0),
      new THREE.MeshStandardMaterial({ color: 0xffffff, emissive: 0x442200, emissiveIntensity: 0.35, roughness: 0.35, metalness: 0.4 }),
      16,
    );

    this.puffPos = new Float32Array(240 * 3);
    this.puffCol = new Float32Array(240 * 3);
    const puffGeo = new THREE.BufferGeometry();
    puffGeo.setAttribute("position", new THREE.BufferAttribute(this.puffPos, 3));
    puffGeo.setAttribute("color", new THREE.BufferAttribute(this.puffCol, 3));
    this.points = new THREE.Points(
      puffGeo,
      new THREE.PointsMaterial({ size: 0.35, vertexColors: true, transparent: true, depthWrite: false, opacity: 0.85 }),
    );
    this.scene.add(this.points);
    this.resize();
  }

  setObstacles(list: Obstacle[]) {
    this.obstacles = list;
    for (const mesh of this.obsMesh.values()) {
      mesh.count = 0;
    }
    const counts: Record<string, number> = {};
    for (const o of list) {
      const mesh = this.obsMesh.get(o.kind);
      if (!mesh) continue;
      const i = counts[o.kind] ?? 0;
      counts[o.kind] = i + 1;
      const h =
        o.kind === "column" ? 4.8 : o.kind === "concrete" ? 1.3 : o.kind === "tires" ? 0.9 : o.kind === "barrel" ? 1.15 : o.kind === "wreck" ? 0.85 : 1.15;
      this.dummy.position.set(o.x, h * 0.5, o.z);
      this.dummy.rotation.set(0, o.rot, 0);
      const sx = o.kind === "column" ? o.r : o.kind === "wreck" ? o.r * 0.85 : o.r * 1.15;
      const sz = o.kind === "column" ? o.r : o.r * (o.kind === "concrete" ? 1.8 : 1.15);
      this.dummy.scale.set(sx, h, sz);
      this.dummy.updateMatrix();
      mesh.setMatrixAt(i, this.dummy.matrix);
      mesh.count = i + 1;
    }
    for (const mesh of this.obsMesh.values()) mesh.instanceMatrix.needsUpdate = true;
  }

  setQuality(q: Quality) {
    this.quality = q;
    this.crowdN = q === "LOW" ? 70 : q === "MEDIUM" ? 140 : q === "ULTRA" ? 320 : 220;
    this.renderer.shadowMap.enabled = q === "HIGH" || q === "ULTRA";
    const cap = q === "LOW" ? 1 : q === "ULTRA" ? 1.6 : 1.3;
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, cap));
    this.resize();
  }

  resize() {
    const canvas = this.renderer.domElement;
    const w = canvas.clientWidth || 1;
    const h = canvas.clientHeight || 1;
    this.renderer.setSize(w, h, false);
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
  }

  burst(x: number, z: number, n: number, color: number, speed: number, up = 2) {
    const cap = this.quality === "LOW" ? 80 : 200;
    for (let i = 0; i < n; i++) {
      if (this.puffs.length > cap) this.puffs.shift();
      const a = Math.random() * Math.PI * 2;
      const s = speed * (0.4 + Math.random());
      this.puffs.push({
        x,
        y: 0.6 + Math.random() * 0.4,
        z,
        vx: Math.cos(a) * s,
        vy: up * (0.3 + Math.random()),
        vz: Math.sin(a) * s,
        life: 0.35 + Math.random() * 0.4,
        max: 0.7,
        size: 1,
        color: new THREE.Color(color),
      });
    }
  }

  draw(frame: FrameState) {
    const dt = 1 / 60;
    (this.shieldMat.uniforms.uTime.value as number) = frame.time;
    (this.shieldMat.uniforms.uPulse.value as number) = frame.shield;
    (this.zoneMat.uniforms.uCenter.value as THREE.Vector2).set(frame.zone.x, frame.zone.z);
    (this.zoneMat.uniforms.uInner.value as number) = frame.zone.r;
    this.ring.scale.set(Math.max(0.4, frame.zone.r), Math.max(0.4, frame.zone.r), 1);
    (this.ring.material as THREE.MeshBasicMaterial).color.set(frame.zone.shrinking ? 0xd32f2f : 0xff8f00);

    this.hideAll(this.obsMesh);
    const tallies: Record<string, number> = {};
    frame.obstacles.forEach((o, idx) => {
      const mesh = this.obsMesh.get(o.kind);
      if (!mesh) return;
      const i = tallies[o.kind] ?? 0;
      tallies[o.kind] = i + 1;
      const src = this.obstacles[idx] ?? o;
      const h =
        o.kind === "column" ? 3.2 : o.kind === "concrete" ? 1.1 : o.kind === "tires" ? 0.7 : o.kind === "barrel" ? 0.7 : o.kind === "wreck" ? 0.7 : 0.9;
      const alive = !o.dead;
      this.dummy.position.set(src.x, alive ? h * 0.5 : -6, src.z);
      this.dummy.rotation.set(0, src.rot, 0);
      this.dummy.scale.set(alive ? 1 : 0.001, alive ? 1 : 0.001, alive ? 1 : 0.001);
      this.dummy.updateMatrix();
      mesh.setMatrixAt(i, this.dummy.matrix);
      mesh.count = Math.max(mesh.count, i + 1);
    });
    for (const mesh of this.obsMesh.values()) mesh.instanceMatrix.needsUpdate = true;

    this.animateCrowd(frame.time);
    this.dustAcc -= dt;
    this.drawCars(frame.cars, this.dustAcc <= 0);
    if (this.dustAcc <= 0) this.dustAcc = 0.07;
    this.drawLoot(frame);
    this.drawBullets(frame.bullets);
    this.stepPuffs(dt);

    this.camera.position.set(frame.cam.x, frame.cam.y, frame.cam.z);
    this.camera.lookAt(frame.cam.lx, frame.cam.ly, frame.cam.lz);
    this.camera.fov = frame.cam.fov;
    this.camera.updateProjectionMatrix();
    this.renderer.render(this.scene, this.camera);
  }

  dispose() {
    this.renderer.dispose();
    this.scene.traverse((obj) => {
      const mesh = obj as THREE.Mesh;
      if (mesh.geometry) mesh.geometry.dispose();
      const mat = mesh.material as THREE.Material | THREE.Material[] | undefined;
      if (Array.isArray(mat)) mat.forEach((m) => m.dispose());
      else mat?.dispose();
    });
  }

  private hideAll(map: Map<string, THREE.InstancedMesh>) {
    for (const mesh of map.values()) mesh.count = 0;
  }

  private pool(geo: THREE.BufferGeometry, mat: THREE.Material, n: number, shadow = false) {
    const mesh = new THREE.InstancedMesh(geo, mat, n);
    mesh.count = 0;
    mesh.frustumCulled = false;
    mesh.castShadow = shadow;
    mesh.receiveShadow = true;
    this.scene.add(mesh);
    return mesh;
  }

  private sandTex() {
    const c = document.createElement("canvas");
    c.width = 512;
    c.height = 512;
    const g = c.getContext("2d");
    if (!g) return null;
    g.fillStyle = "#c4a06a";
    g.fillRect(0, 0, 512, 512);
    for (let i = 0; i < 5000; i++) {
      g.fillStyle = Math.random() > 0.5 ? "rgba(90,55,28,0.2)" : "rgba(240,210,160,0.16)";
      const s = 2 + Math.random() * 4;
      g.fillRect(Math.random() * 512, Math.random() * 512, s, s);
    }
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.wrapS = THREE.RepeatWrapping;
    tex.wrapT = THREE.RepeatWrapping;
    return tex;
  }

  private colosseum() {
    const geo = new THREE.BoxGeometry(2.15, 2.35, 1.7);
    const mat = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.88, metalness: 0.03 });
    const mesh = new THREE.InstancedMesh(geo, mat, 48 * 3);
    mesh.receiveShadow = true;
    let n = 0;
    const segs = 40;
    for (let tier = 0; tier < 3; tier++) {
      for (let i = 0; i < segs; i++) {
        const arch = i % 5 === 0 && tier === 0;
        if (arch) continue;
        const a = (i / segs) * Math.PI * 2;
        const rad = 43.5 + tier * 0.35;
        this.dummy.position.set(Math.sin(a) * rad, 1.15 + tier * 2.3, Math.cos(a) * rad);
        this.dummy.lookAt(0, this.dummy.position.y, 0);
        this.dummy.rotateY(Math.PI);
        const shade = 0.78 + (i % 3) * 0.08;
        this.dummy.scale.set(1, tier === 2 ? 0.85 : 1, 1);
        this.dummy.updateMatrix();
        mesh.setMatrixAt(n, this.dummy.matrix);
        mesh.setColorAt(n, this.color.setRGB(0.95 * shade, 0.86 * shade, 0.64 * shade));
        n++;
      }
    }
    mesh.count = n;
    mesh.instanceMatrix.needsUpdate = true;
    if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
    this.scene.add(mesh);
    return mesh;
  }

  private banners() {
    const geo = new THREE.PlaneGeometry(1.3, 2.4);
    const mat = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.7, side: THREE.DoubleSide });
    const mesh = new THREE.InstancedMesh(geo, mat, 16);
    const colors = [0x8e1d2a, 0xa32030, 0x7a1824, 0x9a2433];
    for (let i = 0; i < 12; i++) {
      const a = (i / 12) * Math.PI * 2 + 0.2;
      this.dummy.position.set(Math.sin(a) * 41.2, 4.3, Math.cos(a) * 41.2);
      this.dummy.lookAt(0, 4.3, 0);
      this.dummy.updateMatrix();
      mesh.setMatrixAt(i, this.dummy.matrix);
      mesh.setColorAt(i, this.color.set(colors[i % colors.length]));
    }
    mesh.count = 12;
    mesh.instanceMatrix.needsUpdate = true;
    if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
    this.scene.add(mesh);
    return mesh;
  }

  private innerWall() {
    const geo = new THREE.BoxGeometry(2.4, 1.15, 0.7);
    const mat = new THREE.MeshStandardMaterial({ color: 0xe4d2b4, roughness: 0.82 });
    const mesh = new THREE.InstancedMesh(geo, mat, 48);
    const segs = 36;
    for (let i = 0; i < segs; i++) {
      const a = (i / segs) * Math.PI * 2;
      this.dummy.position.set(Math.sin(a) * 37.4, 0.55, Math.cos(a) * 37.4);
      this.dummy.lookAt(0, 0.55, 0);
      this.dummy.updateMatrix();
      mesh.setMatrixAt(i, this.dummy.matrix);
    }
    mesh.count = segs;
    mesh.instanceMatrix.needsUpdate = true;
    this.scene.add(mesh);
    return mesh;
  }

  private shafts() {
    const geo = new THREE.PlaneGeometry(2.2, 8);
    const mat = new THREE.MeshBasicMaterial({
      color: 0xffe0b8,
      transparent: true,
      opacity: 0.08,
      depthWrite: false,
      side: THREE.DoubleSide,
    });
    const mesh = new THREE.InstancedMesh(geo, mat, 8);
    for (let i = 0; i < 8; i++) {
      const a = (i / 8) * Math.PI * 2;
      this.dummy.position.set(Math.sin(a) * 30, 6, Math.cos(a) * 30);
      this.dummy.lookAt(0, 2, 0);
      this.dummy.updateMatrix();
      mesh.setMatrixAt(i, this.dummy.matrix);
    }
    mesh.count = 8;
    mesh.instanceMatrix.needsUpdate = true;
    this.scene.add(mesh);
    return mesh;
  }

  private makeCrowd() {
    const body = new THREE.InstancedMesh(
      new THREE.CapsuleGeometry(0.28, 0.55, 3, 6),
      new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.55, metalness: 0.35 }),
      CROWD_MAX,
    );
    const head = new THREE.InstancedMesh(
      new THREE.BoxGeometry(0.36, 0.32, 0.32),
      new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.45, metalness: 0.4 }),
      CROWD_MAX,
    );
    const eyes = new THREE.InstancedMesh(
      new THREE.BoxGeometry(0.16, 0.06, 0.06),
      new THREE.MeshStandardMaterial({ color: 0xff8f00, emissive: 0xff8f00, emissiveIntensity: 0.8, roughness: 0.3 }),
      CROWD_MAX,
    );
    for (const m of [body, head, eyes]) {
      m.frustumCulled = false;
      this.scene.add(m);
    }
    for (let i = 0; i < CROWD_MAX; i++) {
      const a = (i / CROWD_MAX) * Math.PI * 2;
      const ring = i % 2;
      this.crowd.push({
        a,
        r: 41.4 + ring * 0.55,
        phase: a * 3 + i,
        y: 2.55 + ring * 2.05,
        tint: new THREE.Color(i % 3 === 0 ? 0x8a4034 : 0x3d5274),
      });
    }
    return { body, head, eyes };
  }

  private animateCrowd(time: number) {
    const n = Math.min(this.crowdN, CROWD_MAX);
    this.bodyIM.count = n;
    this.headIM.count = n;
    this.eyeIM.count = n;
    for (let i = 0; i < n; i++) {
      const c = this.crowd[i];
      const bob = Math.sin(time * 2.1 + c.phase) * 0.07;
      const jump = Math.sin(time * 0.8 + c.phase) > 0.96 ? 0.18 : 0;
      const y = c.y + bob + jump;
      const x = Math.sin(c.a) * c.r;
      const z = Math.cos(c.a) * c.r;
      this.place(this.bodyIM, i, x, y, z, c.a);
      this.place(this.headIM, i, x, y + 0.62, z, c.a);
      this.place(this.eyeIM, i, x * 0.985, y + 0.66, z * 0.985, c.a);
      this.bodyIM.setColorAt(i, c.tint);
      this.headIM.setColorAt(i, c.tint);
    }
    this.bodyIM.instanceMatrix.needsUpdate = true;
    this.headIM.instanceMatrix.needsUpdate = true;
    this.eyeIM.instanceMatrix.needsUpdate = true;
    if (this.bodyIM.instanceColor) this.bodyIM.instanceColor.needsUpdate = true;
    if (this.headIM.instanceColor) this.headIM.instanceColor.needsUpdate = true;
  }

  private place(mesh: THREE.InstancedMesh, i: number, x: number, y: number, z: number, a: number) {
    this.dummy.position.set(x, y, z);
    this.dummy.lookAt(0, y, 0);
    this.dummy.rotateY(Math.PI);
    this.dummy.scale.set(1, 1, 1);
    this.dummy.updateMatrix();
    mesh.setMatrixAt(i, this.dummy.matrix);
  }

  private drawCars(cars: DrawCar[], puff: boolean) {
    const meshes = [this.body, this.cage, this.turret, this.gun, this.wheels, this.rims, this.lamps];
    for (const m of meshes) m.count = 0;
    cars.forEach((car, i) => {
      if (i > 15) return;
      const sunk = car.dead ? -0.45 : 0;
      const rgtX = Math.cos(car.yaw);
      const rgtZ = -Math.sin(car.yaw);
      const lat = car.vx * rgtX + car.vz * rgtZ;
      const roll = Math.max(-0.12, Math.min(0.12, -lat * 0.02));
      this.compose(this.body, i, car, 0, sunk, 0, 0, 0, roll, car.scale[0], car.scale[1], car.scale[2], car.color);
      this.compose(this.cage, i, car, 0, sunk, 0, 0, 0, roll, car.scale[0], car.scale[1], car.scale[2], 0x141820);
      const aim = car.turret - car.yaw;
      this.compose(this.turret, i, car, 0, 1.28 + sunk, -0.15, aim, 0, 0, 1, 1, 1, 0x1a1c20);
      this.compose(this.gun, i, car, 0, 1.38 + sunk, 0.28, aim, 0.2, 0, 1, 1, 1, 0x101216);
      const spin = car.wheel;
      const spots = [
        [0.85, 1.15],
        [-0.85, 1.15],
        [0.85, -1.15],
        [-0.85, -1.15],
      ] as const;
      spots.forEach((w, k) => {
        this.compose(this.wheels, i * 4 + k, car, w[0], 0.42 + sunk, w[1], 0, spin, 0, 1, 1, 1, 0x161311);
        this.compose(this.rims, i * 4 + k, car, w[0], 0.42 + sunk, w[1], 0, spin, 0, 1, 1, 1, 0xd8d2c8);
      });
      this.compose(this.lamps, i * 2, car, 0.42, 0.58 + sunk, 1.72, 0, 0, 0, 1, 1, 1, 0xfff6d8);
      this.compose(this.lamps, i * 2 + 1, car, -0.42, 0.58 + sunk, 1.72, 0, 0, 0, 1, 1, 1, 0xfff6d8);
      if (!car.dead && puff && Math.abs(car.speed) > 6) this.burst(car.x, car.z, 2, 0xc4a06a, 0.8, 0.35);
      if (car.nitro && !car.dead) this.burst(car.x, car.z, 2, 0xff6a00, 1.2, 0.45);
    });
    for (const m of meshes) m.instanceMatrix.needsUpdate = true;
  }

  private buggyShell() {
    const box = new THREE.BoxGeometry(1, 1, 1);
    return this.fuse([
      this.xform(box, 0, 0.55, 0.05, 0, 0, 0, 1.55, 0.42, 2.7),
      this.xform(box, 0, 0.62, 1.05, -0.35, 0, 0, 1.35, 0.28, 1.15),
      this.xform(box, 0, 0.95, -0.35, 0, 0, 0, 1.25, 0.38, 1.15),
      this.xform(box, 0, 0.72, -1.35, 0.2, 0, 0, 1.4, 0.22, 0.7),
      this.xform(box, 0, 1.05, -1.15, 0, 0, 0, 1.15, 0.06, 0.08),
      this.xform(box, 0, 1.12, -1.15, 0, 0, 0, 1.15, 0.05, 0.08),
      this.xform(box, 0, 1.19, -1.15, 0, 0, 0, 1.15, 0.05, 0.08),
      this.xform(box, 0, 0.48, 1.72, 0, 0, 0, 1.7, 0.28, 0.28),
      this.xform(box, 0.72, 0.48, 0.15, 0, 0, 0, 0.16, 0.22, 2.3),
      this.xform(box, -0.72, 0.48, 0.15, 0, 0, 0, 0.16, 0.22, 2.3),
    ]);
  }

  private buggyCage() {
    const box = new THREE.BoxGeometry(1, 1, 1);
    return this.fuse([
      this.xform(box, 0, 1.05, -0.15, -0.45, 0, 0, 1.05, 0.42, 0.08),
      this.xform(box, 0.58, 0.95, -0.35, 0, 0, 0, 0.06, 0.32, 0.7),
      this.xform(box, -0.58, 0.95, -0.35, 0, 0, 0, 0.06, 0.32, 0.7),
    ]);
  }

  private sky() {
    const mat = new THREE.ShaderMaterial({
      side: THREE.BackSide,
      depthWrite: false,
      uniforms: {},
      vertexShader: `
        varying vec3 vPos;
        void main() {
          vPos = position;
          gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
        }
      `,
      fragmentShader: `
        varying vec3 vPos;
        void main() {
          float h = normalize(vPos).y;
          vec3 hor = vec3(1.0, 0.45, 0.16);
          vec3 mid = vec3(0.86, 0.22, 0.16);
          vec3 top = vec3(0.42, 0.08, 0.14);
          vec3 col = mix(hor, mid, smoothstep(0.0, 0.35, h));
          col = mix(col, top, smoothstep(0.25, 0.9, h));
          gl_FragColor = vec4(col, 1.0);
        }
      `,
    });
    const mesh = new THREE.Mesh(new THREE.SphereGeometry(160, 24, 16), mat);
    mesh.frustumCulled = false;
    return mesh;
  }

  private floods() {
    const mat = new THREE.MeshStandardMaterial({
      color: 0xfff6d8,
      emissive: 0xfff2c4,
      emissiveIntensity: 2.4,
      roughness: 0.3,
    });
    const geo = new THREE.BoxGeometry(1.6, 0.35, 0.45);
    for (let i = 0; i < 6; i++) {
      const a = (i / 6) * Math.PI * 2;
      const m = new THREE.Mesh(geo, mat);
      m.position.set(Math.sin(a) * 42.5, 8.2, Math.cos(a) * 42.5);
      m.lookAt(0, 3, 0);
      this.scene.add(m);
      const spot = new THREE.SpotLight(0xffe6c2, 8, 70, 0.55, 0.45, 1);
      spot.position.set(Math.sin(a) * 42.5, 8.4, Math.cos(a) * 42.5);
      spot.target.position.set(Math.sin(a) * 8, 0, Math.cos(a) * 8);
      this.scene.add(spot);
      this.scene.add(spot.target);
    }
  }

  private treeGeo() {
    const trunk = new THREE.CylinderGeometry(0.16, 0.24, 1.5, 6);
    trunk.translate(0, 0.75, 0);
    this.paint(trunk, 0x6b4226);
    const top = new THREE.ConeGeometry(0.9, 2.15, 7);
    top.translate(0, 2.35, 0);
    this.paint(top, 0x2f8f3a);
    const top2 = new THREE.ConeGeometry(0.7, 1.5, 7);
    top2.translate(0, 3.05, 0);
    this.paint(top2, 0x3aaa48);
    return this.fuse([trunk, top, top2]);
  }

  private houseGeo() {
    const wall = new THREE.BoxGeometry(2.4, 1.5, 1.8);
    wall.translate(0, 0.75, 0);
    this.paint(wall, 0xe6d3b0);
    const roof = new THREE.BoxGeometry(2.7, 0.45, 2.1);
    roof.translate(0, 1.7, 0);
    roof.rotateZ(0.08);
    this.paint(roof, 0xc2410c);
    const door = new THREE.BoxGeometry(0.46, 0.8, 0.08);
    door.translate(0, 0.42, 0.9);
    this.paint(door, 0x5c3a24);
    return this.fuse([wall, roof, door]);
  }

  private paint(g: THREE.BufferGeometry, hex: number) {
    const c = new THREE.Color(hex);
    const n = g.getAttribute("position").count;
    const arr = new Float32Array(n * 3);
    for (let i = 0; i < n; i++) {
      arr[i * 3] = c.r;
      arr[i * 3 + 1] = c.g;
      arr[i * 3 + 2] = c.b;
    }
    g.setAttribute("color", new THREE.BufferAttribute(arr, 3));
    return g;
  }

  private xform(src: THREE.BufferGeometry, x: number, y: number, z: number, rx: number, ry: number, rz: number, sx: number, sy: number, sz: number) {
    const g = src.clone();
    g.applyMatrix4(
      new THREE.Matrix4().compose(
        new THREE.Vector3(x, y, z),
        new THREE.Quaternion().setFromEuler(new THREE.Euler(rx, ry, rz)),
        new THREE.Vector3(sx, sy, sz),
      ),
    );
    return g;
  }

  private fuse(parts: THREE.BufferGeometry[]) {
    const bits = parts.map((g) => (g.index ? g.toNonIndexed() : g));
    let count = 0;
    for (const g of bits) count += g.getAttribute("position").count;
    const pos = new Float32Array(count * 3);
    const nor = new Float32Array(count * 3);
    const uv = new Float32Array(count * 2);
    const col = new Float32Array(count * 3);
    let v = 0;
    for (const g of bits) {
      const p = g.getAttribute("position");
      const n = g.getAttribute("normal");
      const u = g.getAttribute("uv");
      const c = g.getAttribute("color");
      pos.set(p.array as ArrayLike<number>, v * 3);
      nor.set(n.array as ArrayLike<number>, v * 3);
      if (u) uv.set(u.array as ArrayLike<number>, v * 2);
      if (c) col.set(c.array as ArrayLike<number>, v * 3);
      else for (let i = 0; i < p.count; i++) col.set([1, 1, 1], (v + i) * 3);
      v += p.count;
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    geo.setAttribute("normal", new THREE.BufferAttribute(nor, 3));
    geo.setAttribute("uv", new THREE.BufferAttribute(uv, 2));
    geo.setAttribute("color", new THREE.BufferAttribute(col, 3));
    return geo;
  }

  private compose(
    mesh: THREE.InstancedMesh,
    index: number,
    car: DrawCar,
    lx: number,
    ly: number,
    lz: number,
    lyaw: number,
    lpitch: number,
    lroll: number,
    sx: number,
    sy: number,
    sz: number,
    color: number,
  ) {
    this.dummy.position.set(car.x, 0, car.z);
    this.dummy.rotation.set(0, car.yaw + Math.PI, 0);
    this.dummy.scale.set(1, 1, 1);
    this.dummy.updateMatrix();
    this.dummy2.position.set(lx, ly, lz);
    this.dummy2.rotation.set(lpitch, lyaw, lroll);
    this.dummy2.scale.set(sx, sy, sz);
    this.dummy2.updateMatrix();
    this.mOut.multiplyMatrices(this.dummy.matrix, this.dummy2.matrix);
    mesh.setMatrixAt(index, this.mOut);
    mesh.setColorAt(index, this.color.set(color));
    mesh.count = Math.max(mesh.count, index + 1);
    if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
  }

  private drawLoot(frame: FrameState) {
    this.lootIM.count = 0;
    const colors: Record<string, number> = {
      HEALTH: 0x8bc34a,
      AMMO: 0xff8f00,
      NITRO: 0xd84315,
      ARMOR: 0xc4a574,
      WEAPON: 0xf5f5f5,
      REPAIR: 0xffb300,
    };
    frame.loot.forEach((l, i) => {
      if (!l.active) return;
      const bob = Math.sin(frame.time * 2.4 + i) * 0.12;
      this.dummy.position.set(l.x, 0.85 + bob, l.z);
      this.dummy.rotation.set(frame.time, frame.time * 0.8, 0);
      this.dummy.scale.set(1, 1, 1);
      this.dummy.updateMatrix();
      const slot = this.lootIM.count;
      this.lootIM.setMatrixAt(slot, this.dummy.matrix);
      this.lootIM.setColorAt(slot, this.color.set(colors[l.kind] ?? 0xffffff));
      this.lootIM.count = slot + 1;
    });
    this.lootIM.instanceMatrix.needsUpdate = true;
    if (this.lootIM.instanceColor) this.lootIM.instanceColor.needsUpdate = true;
  }

  private drawBullets(bullets: DrawBullet[]) {
    this.bulletIM.count = 0;
    for (const b of bullets) {
      if (!b.alive) continue;
      const slot = this.bulletIM.count;
      if (slot >= 96) break;
      this.dummy.position.set(b.x, b.kind === "rocket" ? 0.9 : 1.15, b.z);
      this.dummy.rotation.set(0, b.yaw + Math.PI, 0);
      const len = b.kind === "rocket" ? 1.4 : b.kind === "shell" ? 1.1 : 0.7;
      this.dummy.scale.set(b.kind === "pellet" ? 0.6 : 1, 1, len);
      this.dummy.updateMatrix();
      this.bulletIM.setMatrixAt(slot, this.dummy.matrix);
      const col = b.kind === "emp" ? 0xff8f00 : b.kind === "rocket" ? 0xd84315 : 0xffe08a;
      this.bulletIM.setColorAt(slot, this.color.set(col));
      this.bulletIM.count = slot + 1;
    }
    this.bulletIM.instanceMatrix.needsUpdate = true;
    if (this.bulletIM.instanceColor) this.bulletIM.instanceColor.needsUpdate = true;
  }

  private stepPuffs(dt: number) {
    const geo = this.points.geometry;
    let n = 0;
    this.puffs = this.puffs.filter((p) => p.life > 0);
    for (const p of this.puffs) {
      if (n >= 240) break;
      p.life -= dt;
      p.x += p.vx * dt;
      p.y += p.vy * dt;
      p.z += p.vz * dt;
      p.vy -= 3 * dt;
      this.puffPos[n * 3] = p.x;
      this.puffPos[n * 3 + 1] = p.y;
      this.puffPos[n * 3 + 2] = p.z;
      this.puffCol[n * 3] = p.color.r;
      this.puffCol[n * 3 + 1] = p.color.g;
      this.puffCol[n * 3 + 2] = p.color.b;
      n++;
    }
    for (let i = n; i < 240; i++) {
      this.puffPos[i * 3 + 1] = -10;
    }
    (geo.getAttribute("position") as THREE.BufferAttribute).needsUpdate = true;
    (geo.getAttribute("color") as THREE.BufferAttribute).needsUpdate = true;
    geo.setDrawRange(0, n);
  }
}
