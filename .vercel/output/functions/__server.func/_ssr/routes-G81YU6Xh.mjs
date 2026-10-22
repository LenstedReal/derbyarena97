import { i as __toESM } from "../_runtime.mjs";
import { K as require_react, b as require_jsx_runtime } from "../_libs/@tanstack/react-router+[...].mjs";
import { a as Swords, c as Pause, d as Crosshair, l as Gauge, n as VolumeX, o as Shield, r as Volume2, s as Settings, t as Wrench, u as Flame } from "../_libs/lucide-react.mjs";
import { A as ShaderMaterial, C as PlaneGeometry, D as RepeatWrapping, E as Raycaster, M as Vector2, N as Vector3, O as SRGBColorSpace, S as Plane, T as PointsMaterial, _ as MeshBasicMaterial, a as CanvasTexture, b as OctahedronGeometry, c as Color, d as DodecahedronGeometry, f as Fog, g as Mesh, h as Matrix4, i as BufferGeometry, j as TorusGeometry, k as Scene, l as CylinderGeometry, m as InstancedMesh, n as BoxGeometry, o as CapsuleGeometry, p as HemisphereLight, r as BufferAttribute, s as CircleGeometry, t as WebGLRenderer, u as DirectionalLight, v as MeshStandardMaterial, w as Points, x as PerspectiveCamera, y as Object3D } from "../_libs/three.mjs";
//#region node_modules/.nitro/vite/services/ssr/assets/routes-G81YU6Xh.js
var import_react = /* @__PURE__ */ __toESM(require_react());
var import_jsx_runtime = require_jsx_runtime();
var CAR_R = 1.42;
var WEAPONS = {
	MG: {
		id: "MG",
		label: "MAKİNELİ",
		damage: 9,
		rate: .1,
		range: 26,
		spread: .04,
		mag: 80,
		reload: 1.85,
		heat: .045,
		speed: 48,
		pellets: 1,
		splash: 0,
		stun: 0,
		kind: "tracer"
	},
	SHOTGUN: {
		id: "SHOTGUN",
		label: "SAÇMA",
		damage: 11,
		rate: .7,
		range: 12.5,
		spread: .2,
		mag: 8,
		reload: 2.5,
		heat: .12,
		speed: 40,
		pellets: 6,
		splash: 0,
		stun: 0,
		kind: "pellet"
	},
	ROCKET: {
		id: "ROCKET",
		label: "ROKET",
		damage: 58,
		rate: 1.25,
		range: 33,
		spread: .02,
		mag: 4,
		reload: 3,
		heat: .2,
		speed: 22,
		pellets: 1,
		splash: 5.4,
		stun: 0,
		kind: "rocket"
	},
	EMP: {
		id: "EMP",
		label: "EMP",
		damage: 14,
		rate: 1.5,
		range: 21,
		spread: .03,
		mag: 3,
		reload: 3.2,
		heat: .22,
		speed: 28,
		pellets: 1,
		splash: 4.6,
		stun: .85,
		kind: "emp"
	},
	CANNON: {
		id: "CANNON",
		label: "TOP",
		damage: 46,
		rate: .82,
		range: 36,
		spread: .012,
		mag: 6,
		reload: 2.8,
		heat: .18,
		speed: 58,
		pellets: 1,
		splash: 1.8,
		stun: 0,
		kind: "shell"
	}
};
var DIFFS = [
	{
		id: "scrap",
		title: "HURDA ÇUKURU",
		blurb: "9 sürücü. Bölge yavaş kapanır. Isınmak için.",
		count: 9,
		hp: 1,
		zone: 1,
		boss: false
	},
	{
		id: "iron",
		title: "DEMİR HALKA",
		blurb: "12 sürücü. Kuşatma daha erken başlar.",
		count: 12,
		hp: 1.12,
		zone: .82,
		boss: false
	},
	{
		id: "war",
		title: "SAVAŞ RİGİ",
		blurb: "War Rig ve 13 silahlı araç. Son daire acımasız.",
		count: 14,
		hp: 1.22,
		zone: .74,
		boss: true
	}
];
var PHASES = [
	{
		wait: 28,
		shrink: 22,
		radius: 26.7,
		dps: 4
	},
	{
		wait: 20,
		shrink: 20,
		radius: 18.5,
		dps: 6
	},
	{
		wait: 16,
		shrink: 16,
		radius: 11.8,
		dps: 9
	},
	{
		wait: 12,
		shrink: 14,
		radius: 6.7,
		dps: 14
	},
	{
		wait: 10,
		shrink: 12,
		radius: 3.6,
		dps: 22
	}
];
var UPGRADE_COST = [
	50,
	90,
	150,
	240,
	360
];
var RAW = [
	{
		x: 0,
		z: -1.5,
		r: 2.15,
		kind: "wreck",
		hp: 520
	},
	{
		x: -10,
		z: -16.5,
		r: 1.65,
		kind: "column",
		hp: 900
	},
	{
		x: -3.4,
		z: -18.2,
		r: 1.65,
		kind: "column",
		hp: 900
	},
	{
		x: 3.6,
		z: -18,
		r: 1.65,
		kind: "column",
		hp: 900
	},
	{
		x: 10.2,
		z: -16.2,
		r: 1.65,
		kind: "column",
		hp: 900
	},
	{
		x: 16.4,
		z: -4.2,
		r: 1.35,
		kind: "concrete",
		hp: 460
	},
	{
		x: 18.6,
		z: 1.6,
		r: 1.35,
		kind: "concrete",
		hp: 460
	},
	{
		x: 16.2,
		z: 7.4,
		r: 1.35,
		kind: "concrete",
		hp: 460
	},
	{
		x: 12.4,
		z: -10.5,
		r: 1.35,
		kind: "concrete",
		hp: 420
	},
	{
		x: -16.2,
		z: -.4,
		r: .95,
		kind: "tires",
		hp: 160
	},
	{
		x: -18.4,
		z: 4.6,
		r: .95,
		kind: "tires",
		hp: 160
	},
	{
		x: -14.2,
		z: 5.8,
		r: .95,
		kind: "tires",
		hp: 160
	},
	{
		x: -17.5,
		z: -6.2,
		r: .95,
		kind: "tires",
		hp: 160
	},
	{
		x: -8.2,
		z: 15.6,
		r: 1.55,
		kind: "rubble",
		hp: 280
	},
	{
		x: 8.4,
		z: 15.4,
		r: 1.55,
		kind: "rubble",
		hp: 280
	},
	{
		x: -14.5,
		z: 11.2,
		r: 1.5,
		kind: "rubble",
		hp: 260
	},
	{
		x: 14.2,
		z: 11.6,
		r: 1.5,
		kind: "rubble",
		hp: 260
	},
	{
		x: 11.5,
		z: 16.8,
		r: 2,
		kind: "wreck",
		hp: 380
	},
	{
		x: -12.4,
		z: -8.2,
		r: 2.05,
		kind: "wreck",
		hp: 400
	},
	{
		x: 9.2,
		z: -8.6,
		r: 1.9,
		kind: "wreck",
		hp: 340
	},
	{
		x: -22,
		z: -12,
		r: 1.9,
		kind: "wreck",
		hp: 360
	},
	{
		x: 22.5,
		z: 12.5,
		r: 1.85,
		kind: "wreck",
		hp: 340
	},
	{
		x: 12.2,
		z: -14.6,
		r: .62,
		kind: "barrel",
		hp: 28,
		explosive: true
	},
	{
		x: -11.4,
		z: 8.4,
		r: .62,
		kind: "barrel",
		hp: 28,
		explosive: true
	},
	{
		x: 20.4,
		z: -8.8,
		r: .62,
		kind: "barrel",
		hp: 28,
		explosive: true
	},
	{
		x: -5.5,
		z: 19.2,
		r: .62,
		kind: "barrel",
		hp: 28,
		explosive: true
	},
	{
		x: .4,
		z: -22.4,
		r: .62,
		kind: "barrel",
		hp: 28,
		explosive: true
	},
	{
		x: 24,
		z: -16,
		r: 1.45,
		kind: "rubble",
		hp: 240
	},
	{
		x: -24.5,
		z: 14.5,
		r: 1.4,
		kind: "concrete",
		hp: 400
	}
];
function hypot(x, z) {
	return Math.hypot(x, z);
}
/** Push overlaps apart and keep every prop inside the driveable sand. */
function makeObstacles() {
	const items = RAW.map((o, id) => ({
		id,
		x: o.x,
		z: o.z,
		r: o.r,
		kind: o.kind,
		hp: o.hp,
		maxHp: o.hp,
		explosive: Boolean(o.explosive),
		dead: false,
		rot: id * 1.7 % Math.PI
	}));
	for (let iter = 0; iter < 14; iter++) for (let i = 0; i < items.length; i++) {
		for (let j = i + 1; j < items.length; j++) {
			const a = items[i];
			const b = items[j];
			let dx = b.x - a.x;
			let dz = b.z - a.z;
			let d = hypot(dx, dz);
			const min = a.r + b.r + 1.15;
			if (d < 1e-4) {
				dx = 1;
				dz = .2;
				d = Math.hypot(dx, dz);
			}
			if (d < min) {
				const push = (min - d) / 2;
				const nx = dx / d;
				const nz = dz / d;
				a.x -= nx * push;
				a.z -= nz * push;
				b.x += nx * push;
				b.z += nz * push;
			}
		}
		const a = items[i];
		const d = hypot(a.x, a.z);
		const max = 36 - a.r - 1.35;
		if (d > max && d > .001) {
			a.x *= max / d;
			a.z *= max / d;
		}
	}
	return items;
}
var LOOT_RAW = [
	{
		x: 6.2,
		z: 5.4,
		kind: "HEALTH"
	},
	{
		x: -6.4,
		z: 4.8,
		kind: "AMMO"
	},
	{
		x: 5.6,
		z: -6.2,
		kind: "NITRO"
	},
	{
		x: -5.2,
		z: -6.6,
		kind: "ARMOR"
	},
	{
		x: .2,
		z: -12.4,
		kind: "WEAPON",
		weapon: "SHOTGUN"
	},
	{
		x: 15.5,
		z: 3.2,
		kind: "REPAIR"
	},
	{
		x: -15.2,
		z: -3.4,
		kind: "AMMO"
	},
	{
		x: 9.5,
		z: 18.2,
		kind: "WEAPON",
		weapon: "ROCKET"
	},
	{
		x: -9.2,
		z: 18.6,
		kind: "HEALTH"
	},
	{
		x: 21,
		z: -4.5,
		kind: "NITRO"
	},
	{
		x: -21.2,
		z: 6.5,
		kind: "WEAPON",
		weapon: "EMP"
	},
	{
		x: 2.2,
		z: 9.4,
		kind: "WEAPON",
		weapon: "CANNON"
	}
];
function makeLoot(obstacles) {
	return LOOT_RAW.map((spot, id) => {
		let x = spot.x;
		let z = spot.z;
		for (let n = 0; n < 8; n++) {
			const hit = obstacles.find((o) => hypot(o.x - x, o.z - z) < o.r + 1.8);
			if (!hit) break;
			const dx = x - hit.x || 1;
			const dz = z - hit.z || .3;
			const d = hypot(dx, dz) || 1;
			x += dx / d * .8;
			z += dz / d * .8;
		}
		const d = hypot(x, z);
		const max = 33;
		if (d > max) {
			x *= max / d;
			z *= max / d;
		}
		return {
			id,
			x,
			z,
			kind: spot.kind,
			weapon: spot.weapon ?? null,
			active: true,
			respawn: 0
		};
	});
}
function forward(yaw) {
	return {
		x: -Math.sin(yaw),
		z: -Math.cos(yaw)
	};
}
function right(yaw) {
	return {
		x: Math.cos(yaw),
		z: -Math.sin(yaw)
	};
}
function yawToward(x, z, tx, tz) {
	const dx = tx - x;
	const dz = tz - z;
	const d = Math.hypot(dx, dz) || 1;
	return Math.atan2(-dx / d, -dz / d);
}
function wrapAngle(a) {
	let w = a;
	while (w > Math.PI) w -= Math.PI * 2;
	while (w < -Math.PI) w += Math.PI * 2;
	return w;
}
function clamp(v, a, b) {
	return Math.max(a, Math.min(b, v));
}
var Sfx = class {
	ctx = null;
	enabled = true;
	lastZone = 0;
	engineOsc = null;
	engineOsc2 = null;
	engineGain = null;
	unlock() {
		if (!this.enabled) return;
		const ctx = this.ensure();
		if (ctx.state === "suspended") ctx.resume();
		this.ensureEngine();
	}
	stop() {
		try {
			this.engineOsc?.stop();
			this.engineOsc2?.stop();
		} catch {}
		this.engineOsc = null;
		this.engineOsc2 = null;
		this.engineGain = null;
	}
	ensure() {
		if (!this.ctx) {
			const AC = window.AudioContext || window.webkitAudioContext;
			this.ctx = new AC();
		}
		return this.ctx;
	}
	ensureEngine() {
		if (!this.enabled || this.engineOsc) return;
		const ctx = this.ensure();
		const osc = ctx.createOscillator();
		const osc2 = ctx.createOscillator();
		const g = ctx.createGain();
		const filter = ctx.createBiquadFilter();
		osc.type = "sawtooth";
		osc2.type = "square";
		osc.frequency.value = 48;
		osc2.frequency.value = 24;
		filter.type = "lowpass";
		filter.frequency.value = 280;
		g.gain.value = 1e-4;
		osc.connect(filter);
		osc2.connect(filter);
		filter.connect(g);
		g.connect(ctx.destination);
		osc.start();
		osc2.start();
		this.engineOsc = osc;
		this.engineOsc2 = osc2;
		this.engineGain = g;
	}
	/** Continuous idle/RPM. speed is sim units, not the HUD km/h. */
	engine(speed, nitro) {
		if (!this.enabled) {
			if (this.engineGain && this.ctx) this.engineGain.gain.setTargetAtTime(1e-4, this.ctx.currentTime, .05);
			return;
		}
		const ctx = this.ensure();
		if (ctx.state === "suspended") return;
		this.ensureEngine();
		if (!this.engineOsc || !this.engineOsc2 || !this.engineGain) return;
		const rpm = 42 + Math.min(110, Math.abs(speed) * 8.5) + (nitro ? 36 : 0);
		const t = ctx.currentTime;
		this.engineOsc.frequency.setTargetAtTime(rpm, t, .08);
		this.engineOsc2.frequency.setTargetAtTime(rpm * .5, t, .08);
		const gain = .012 + Math.min(.045, Math.abs(speed) * .0032) + (nitro ? .02 : 0);
		this.engineGain.gain.setTargetAtTime(gain, t, .06);
	}
	tone(freq, dur, type, gain, slide = 0) {
		if (!this.enabled) return;
		const ctx = this.ensure();
		if (ctx.state === "suspended") return;
		const t = ctx.currentTime;
		const osc = ctx.createOscillator();
		const g = ctx.createGain();
		osc.type = type;
		osc.frequency.setValueAtTime(freq, t);
		if (slide) osc.frequency.exponentialRampToValueAtTime(Math.max(40, freq + slide), t + dur);
		g.gain.setValueAtTime(gain, t);
		g.gain.exponentialRampToValueAtTime(1e-4, t + dur);
		osc.connect(g);
		g.connect(ctx.destination);
		osc.start(t);
		osc.stop(t + dur + .02);
	}
	shot(kind) {
		if (kind === "rocket") this.tone(140, .28, "sawtooth", .05, -80);
		else if (kind === "shell") this.tone(90, .22, "square", .05, -40);
		else if (kind === "pellet") this.tone(220, .12, "square", .04, -100);
		else if (kind === "emp") this.tone(520, .18, "sine", .04, -300);
		else this.tone(640, .05, "square", .03, -200);
	}
	boom() {
		this.tone(90, .4, "sawtooth", .07, -60);
	}
	hit() {
		this.tone(180, .08, "triangle", .04, -80);
	}
	crunch() {
		this.tone(70, .16, "square", .06, -40);
	}
	pick() {
		this.tone(520, .09, "square", .03, 180);
	}
	zone(now) {
		if (now - this.lastZone < .75) return;
		this.lastZone = now;
		this.tone(160, .14, "sawtooth", .04, -40);
	}
	win() {
		this.tone(440, .12, "square", .04, 0);
		setTimeout(() => this.tone(660, .18, "square", .04, 0), 120);
	}
	lose() {
		this.tone(180, .3, "sawtooth", .05, -100);
	}
	buzz(ms) {
		if (!this.enabled) return;
		navigator.vibrate?.(ms);
	}
};
var CROWD_MAX = 360;
var ArenaView = class {
	renderer;
	scene = new Scene();
	camera = new PerspectiveCamera(62, 1, .1, 220);
	obstacles = [];
	crowdN = 140;
	crowd = [];
	bodyIM;
	headIM;
	eyeIM;
	blockIM;
	bannerIM;
	wallIM;
	shaftIM;
	obsMesh = /* @__PURE__ */ new Map();
	chassis;
	cabin;
	hood;
	turret;
	gun;
	wheels;
	lamps;
	bulletIM;
	lootIM;
	points;
	puffs = [];
	puffPos;
	puffCol;
	zoneMat;
	shieldMat;
	ring;
	dummy = new Object3D();
	dummy2 = new Object3D();
	mOut = new Matrix4();
	color = new Color();
	quality = "HIGH";
	sand;
	constructor(canvas) {
		this.renderer = new WebGLRenderer({
			canvas,
			antialias: true,
			alpha: false,
			powerPreference: "high-performance"
		});
		this.renderer.outputColorSpace = SRGBColorSpace;
		this.renderer.toneMapping = 4;
		this.renderer.toneMappingExposure = 1.08;
		this.renderer.setClearColor(1314314, 1);
		this.scene.background = new Color(1708556);
		this.scene.fog = new Fog(1708556, 42, 110);
		const hemi = new HemisphereLight(16769727, 3811868, .72);
		this.scene.add(hemi);
		const sun = new DirectionalLight(16763274, 1.55);
		sun.position.set(28, 48, 14);
		sun.castShadow = true;
		sun.shadow.mapSize.set(1024, 1024);
		sun.shadow.camera.near = 10;
		sun.shadow.camera.far = 120;
		sun.shadow.camera.left = -40;
		sun.shadow.camera.right = 40;
		sun.shadow.camera.top = 40;
		sun.shadow.camera.bottom = -40;
		this.scene.add(sun);
		const rim = new DirectionalLight(14172949, .35);
		rim.position.set(-20, 12, -10);
		this.scene.add(rim);
		const apron = new Mesh(new CircleGeometry(78, 48), new MeshStandardMaterial({
			color: 2760984,
			roughness: 1,
			metalness: 0
		}));
		apron.rotation.x = -Math.PI / 2;
		apron.position.y = -.05;
		this.scene.add(apron);
		this.sand = new Mesh(new CircleGeometry(37.2, 64), new MeshStandardMaterial({
			map: this.sandTex(),
			color: 16777215,
			roughness: .92,
			metalness: .02
		}));
		this.sand.rotation.x = -Math.PI / 2;
		this.sand.receiveShadow = true;
		this.scene.add(this.sand);
		this.zoneMat = new ShaderMaterial({
			transparent: true,
			depthWrite: false,
			uniforms: {
				uCenter: { value: new Vector2(0, 0) },
				uInner: { value: 36 },
				uColor: { value: new Color(13840175) }
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
      `
		});
		const zoneDisc = new Mesh(new CircleGeometry(42, 64), this.zoneMat);
		zoneDisc.rotation.x = -Math.PI / 2;
		zoneDisc.position.y = .04;
		this.scene.add(zoneDisc);
		this.ring = new Mesh(new TorusGeometry(1, .08, 8, 80), new MeshBasicMaterial({ color: 16748288 }));
		this.ring.rotation.x = Math.PI / 2;
		this.ring.position.y = .35;
		this.scene.add(this.ring);
		this.shieldMat = new ShaderMaterial({
			transparent: true,
			depthWrite: false,
			side: 2,
			uniforms: {
				uPulse: { value: 0 },
				uTime: { value: 0 }
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
      `
		});
		const shield = new Mesh(new CylinderGeometry(38.4, 38.4, 7.2, 64, 1, true), this.shieldMat);
		shield.position.y = 3.4;
		this.scene.add(shield);
		this.blockIM = this.colosseum();
		this.bannerIM = this.banners();
		this.wallIM = this.innerWall();
		this.shaftIM = this.shafts();
		const crowd = this.makeCrowd();
		this.bodyIM = crowd.body;
		this.headIM = crowd.head;
		this.eyeIM = crowd.eyes;
		const stone = new MeshStandardMaterial({
			color: 11569512,
			roughness: .86,
			metalness: .04
		});
		const concrete = new MeshStandardMaterial({
			color: 9077109,
			roughness: .78,
			metalness: .08
		});
		const rubble = new MeshStandardMaterial({
			color: 7033668,
			roughness: .95,
			metalness: .02
		});
		const tire = new MeshStandardMaterial({
			color: 1709588,
			roughness: .7,
			metalness: .1
		});
		const wreck = new MeshStandardMaterial({
			color: 3812392,
			roughness: .62,
			metalness: .35
		});
		const barrel = new MeshStandardMaterial({
			color: 11883050,
			roughness: .55,
			metalness: .28
		});
		this.obsMesh.set("column", this.pool(new CylinderGeometry(1, 1.15, 1, 8), stone, 12));
		this.obsMesh.set("concrete", this.pool(new BoxGeometry(1, 1, 1), concrete, 12));
		this.obsMesh.set("rubble", this.pool(new DodecahedronGeometry(1, 0), rubble, 12));
		this.obsMesh.set("tires", this.pool(new CylinderGeometry(1, 1, .55, 10), tire, 12));
		this.obsMesh.set("wreck", this.pool(new BoxGeometry(1.6, .7, 2.4), wreck, 12));
		this.obsMesh.set("barrel", this.pool(new CylinderGeometry(.7, .7, 1.3, 8), barrel, 8));
		const bodyMat = new MeshStandardMaterial({
			color: 16777215,
			roughness: .42,
			metalness: .46
		});
		const darkMat = new MeshStandardMaterial({
			color: 16777215,
			roughness: .55,
			metalness: .4
		});
		const gunMat = new MeshStandardMaterial({
			color: 16777215,
			roughness: .4,
			metalness: .6
		});
		const lampMat = new MeshStandardMaterial({
			color: 16777215,
			emissive: 16757082,
			emissiveIntensity: .9,
			roughness: .35
		});
		this.chassis = this.pool(new BoxGeometry(1.5, .42, 3.15), bodyMat, 16, true);
		this.cabin = this.pool(new BoxGeometry(1.2, .4, 1.25), darkMat, 16);
		this.hood = this.pool(new BoxGeometry(1.28, .2, 1.05), bodyMat, 16);
		this.turret = this.pool(new BoxGeometry(.72, .26, .72), gunMat, 16);
		this.gun = this.pool(new BoxGeometry(.16, .16, 1.15), gunMat, 16);
		const wheelGeo = new CylinderGeometry(.38, .38, .28, 10);
		wheelGeo.rotateZ(Math.PI / 2);
		this.wheels = this.pool(wheelGeo, new MeshStandardMaterial({
			color: 16777215,
			roughness: .7,
			metalness: .2
		}), 64);
		this.lamps = this.pool(new BoxGeometry(.18, .12, .08), lampMat, 32);
		this.bulletIM = this.pool(new BoxGeometry(.08, .08, .7), new MeshBasicMaterial({ color: 16769162 }), 96);
		this.lootIM = this.pool(new OctahedronGeometry(.38, 0), new MeshStandardMaterial({
			color: 16777215,
			emissive: 4465152,
			emissiveIntensity: .35,
			roughness: .35,
			metalness: .4
		}), 16);
		this.puffPos = /* @__PURE__ */ new Float32Array(720);
		this.puffCol = /* @__PURE__ */ new Float32Array(720);
		const puffGeo = new BufferGeometry();
		puffGeo.setAttribute("position", new BufferAttribute(this.puffPos, 3));
		puffGeo.setAttribute("color", new BufferAttribute(this.puffCol, 3));
		this.points = new Points(puffGeo, new PointsMaterial({
			size: .35,
			vertexColors: true,
			transparent: true,
			depthWrite: false,
			opacity: .85
		}));
		this.scene.add(this.points);
		this.resize();
	}
	setObstacles(list) {
		this.obstacles = list;
		for (const mesh of this.obsMesh.values()) mesh.count = 0;
		const counts = {};
		for (const o of list) {
			const mesh = this.obsMesh.get(o.kind);
			if (!mesh) continue;
			const i = counts[o.kind] ?? 0;
			counts[o.kind] = i + 1;
			const h = o.kind === "column" ? 4.8 : o.kind === "concrete" ? 1.3 : o.kind === "tires" ? .9 : o.kind === "barrel" ? 1.15 : o.kind === "wreck" ? .85 : 1.15;
			this.dummy.position.set(o.x, h * .5, o.z);
			this.dummy.rotation.set(0, o.rot, 0);
			const sx = o.kind === "column" ? o.r : o.kind === "wreck" ? o.r * .85 : o.r * 1.15;
			const sz = o.kind === "column" ? o.r : o.r * (o.kind === "concrete" ? 1.8 : 1.15);
			this.dummy.scale.set(sx, h, sz);
			this.dummy.updateMatrix();
			mesh.setMatrixAt(i, this.dummy.matrix);
			mesh.count = i + 1;
		}
		for (const mesh of this.obsMesh.values()) mesh.instanceMatrix.needsUpdate = true;
	}
	setQuality(q) {
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
	burst(x, z, n, color, speed, up = 2) {
		const cap = this.quality === "LOW" ? 80 : 200;
		for (let i = 0; i < n; i++) {
			if (this.puffs.length > cap) this.puffs.shift();
			const a = Math.random() * Math.PI * 2;
			const s = speed * (.4 + Math.random());
			this.puffs.push({
				x,
				y: .6 + Math.random() * .4,
				z,
				vx: Math.cos(a) * s,
				vy: up * (.3 + Math.random()),
				vz: Math.sin(a) * s,
				life: .35 + Math.random() * .4,
				max: .7,
				size: 1,
				color: new Color(color)
			});
		}
	}
	draw(frame) {
		const dt = 1 / 60;
		this.shieldMat.uniforms.uTime.value = frame.time;
		this.shieldMat.uniforms.uPulse.value = frame.shield;
		this.zoneMat.uniforms.uCenter.value.set(frame.zone.x, frame.zone.z);
		this.zoneMat.uniforms.uInner.value = frame.zone.r;
		this.ring.scale.set(Math.max(.4, frame.zone.r), Math.max(.4, frame.zone.r), 1);
		this.ring.material.color.set(frame.zone.shrinking ? 13840175 : 16748288);
		this.hideAll(this.obsMesh);
		const tallies = {};
		frame.obstacles.forEach((o, idx) => {
			const mesh = this.obsMesh.get(o.kind);
			if (!mesh) return;
			const i = tallies[o.kind] ?? 0;
			tallies[o.kind] = i + 1;
			const src = this.obstacles[idx] ?? o;
			const h = o.kind === "column" ? 4.8 : o.kind === "concrete" ? 1.3 : o.kind === "tires" ? .9 : o.kind === "barrel" ? 1.15 : o.kind === "wreck" ? .85 : 1.15;
			const alive = !o.dead;
			this.dummy.position.set(src.x, alive ? h * .5 : -4, src.z);
			this.dummy.rotation.set(0, src.rot, 0);
			const sx = o.kind === "column" ? o.r : o.kind === "wreck" ? o.r * .85 : o.r * 1.15;
			this.dummy.scale.set(alive ? sx : .001, alive ? h : .001, alive ? o.r : .001);
			if (o.kind === "concrete") this.dummy.scale.set(alive ? o.r * 1.3 : .001, alive ? h : .001, alive ? o.r * 2.1 : .001);
			this.dummy.updateMatrix();
			mesh.setMatrixAt(i, this.dummy.matrix);
			mesh.count = Math.max(mesh.count, i + 1);
		});
		for (const mesh of this.obsMesh.values()) mesh.instanceMatrix.needsUpdate = true;
		this.animateCrowd(frame.time);
		this.drawCars(frame.cars);
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
			const mesh = obj;
			if (mesh.geometry) mesh.geometry.dispose();
			const mat = mesh.material;
			if (Array.isArray(mat)) mat.forEach((m) => m.dispose());
			else mat?.dispose();
		});
	}
	hideAll(map) {
		for (const mesh of map.values()) mesh.count = 0;
	}
	pool(geo, mat, n, shadow = false) {
		const mesh = new InstancedMesh(geo, mat, n);
		mesh.count = 0;
		mesh.frustumCulled = false;
		mesh.castShadow = shadow;
		mesh.receiveShadow = true;
		this.scene.add(mesh);
		return mesh;
	}
	sandTex() {
		const c = document.createElement("canvas");
		c.width = 512;
		c.height = 512;
		const g = c.getContext("2d");
		if (!g) return null;
		g.fillStyle = "#c4a574";
		g.fillRect(0, 0, 512, 512);
		for (let i = 0; i < 5e3; i++) {
			g.fillStyle = Math.random() > .5 ? "rgba(90,58,28,0.18)" : "rgba(255,226,190,0.14)";
			const s = 1 + Math.random() * 2;
			g.fillRect(Math.random() * 512, Math.random() * 512, s, s);
		}
		g.strokeStyle = "rgba(90,60,30,0.28)";
		g.lineWidth = 10;
		g.beginPath();
		g.arc(256, 256, 150, 0, Math.PI * 2);
		g.stroke();
		g.beginPath();
		g.arc(256, 256, 96, 0, Math.PI * 2);
		g.stroke();
		const tex = new CanvasTexture(c);
		tex.colorSpace = SRGBColorSpace;
		tex.wrapS = RepeatWrapping;
		tex.wrapT = RepeatWrapping;
		return tex;
	}
	colosseum() {
		const geo = new BoxGeometry(2.15, 2.35, 1.7);
		const mat = new MeshStandardMaterial({
			color: 16777215,
			roughness: .88,
			metalness: .03
		});
		const mesh = new InstancedMesh(geo, mat, 144);
		mesh.receiveShadow = true;
		let n = 0;
		const segs = 40;
		for (let tier = 0; tier < 3; tier++) for (let i = 0; i < segs; i++) {
			if (i % 5 === 0 && tier === 0) continue;
			const a = i / segs * Math.PI * 2;
			const rad = 43.5 + tier * .35;
			this.dummy.position.set(Math.sin(a) * rad, 1.15 + tier * 2.3, Math.cos(a) * rad);
			this.dummy.lookAt(0, this.dummy.position.y, 0);
			this.dummy.rotateY(Math.PI);
			const shade = .78 + i % 3 * .08;
			this.dummy.scale.set(1, tier === 2 ? .85 : 1, 1);
			this.dummy.updateMatrix();
			mesh.setMatrixAt(n, this.dummy.matrix);
			mesh.setColorAt(n, this.color.setRGB(.72 * shade, .55 * shade, .38 * shade));
			n++;
		}
		mesh.count = n;
		mesh.instanceMatrix.needsUpdate = true;
		if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
		this.scene.add(mesh);
		return mesh;
	}
	banners() {
		const geo = new PlaneGeometry(1.3, 2.4);
		const mat = new MeshStandardMaterial({
			color: 16777215,
			roughness: .7,
			side: 2
		});
		const mesh = new InstancedMesh(geo, mat, 16);
		const colors = [
			14172949,
			16748288,
			7019805,
			13214247
		];
		for (let i = 0; i < 12; i++) {
			const a = i / 12 * Math.PI * 2 + .2;
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
	innerWall() {
		const geo = new BoxGeometry(2.4, 1.15, .7);
		const mat = new MeshStandardMaterial({
			color: 4864560,
			roughness: .62,
			metalness: .28
		});
		const mesh = new InstancedMesh(geo, mat, 48);
		const segs = 36;
		for (let i = 0; i < segs; i++) {
			const a = i / segs * Math.PI * 2;
			this.dummy.position.set(Math.sin(a) * 37.4, .55, Math.cos(a) * 37.4);
			this.dummy.lookAt(0, .55, 0);
			this.dummy.updateMatrix();
			mesh.setMatrixAt(i, this.dummy.matrix);
		}
		mesh.count = segs;
		mesh.instanceMatrix.needsUpdate = true;
		this.scene.add(mesh);
		return mesh;
	}
	shafts() {
		const geo = new PlaneGeometry(2.2, 8);
		const mat = new MeshBasicMaterial({
			color: 16769208,
			transparent: true,
			opacity: .08,
			depthWrite: false,
			side: 2
		});
		const mesh = new InstancedMesh(geo, mat, 8);
		for (let i = 0; i < 8; i++) {
			const a = i / 8 * Math.PI * 2;
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
	makeCrowd() {
		const body = new InstancedMesh(new CapsuleGeometry(.28, .55, 3, 6), new MeshStandardMaterial({
			color: 16777215,
			roughness: .55,
			metalness: .35
		}), CROWD_MAX);
		const head = new InstancedMesh(new BoxGeometry(.36, .32, .32), new MeshStandardMaterial({
			color: 16777215,
			roughness: .45,
			metalness: .4
		}), CROWD_MAX);
		const eyes = new InstancedMesh(new BoxGeometry(.16, .06, .06), new MeshStandardMaterial({
			color: 16748288,
			emissive: 16748288,
			emissiveIntensity: .8,
			roughness: .3
		}), CROWD_MAX);
		for (const m of [
			body,
			head,
			eyes
		]) {
			m.frustumCulled = false;
			this.scene.add(m);
		}
		for (let i = 0; i < CROWD_MAX; i++) {
			const a = i / CROWD_MAX * Math.PI * 2;
			const ring = i % 2;
			this.crowd.push({
				a,
				r: 44.6 + ring * .85,
				phase: a * 3 + i,
				y: 5.15 + ring * .15,
				tint: new Color().setHSL(.07 + i % 5 * .015, .35, .32 + i % 4 * .04)
			});
		}
		return {
			body,
			head,
			eyes
		};
	}
	animateCrowd(time) {
		const n = Math.min(this.crowdN, CROWD_MAX);
		this.bodyIM.count = n;
		this.headIM.count = n;
		this.eyeIM.count = n;
		for (let i = 0; i < n; i++) {
			const c = this.crowd[i];
			const bob = Math.sin(time * 2.1 + c.phase) * .07;
			const jump = Math.sin(time * .8 + c.phase) > .96 ? .18 : 0;
			const y = c.y + bob + jump;
			const x = Math.sin(c.a) * c.r;
			const z = Math.cos(c.a) * c.r;
			this.place(this.bodyIM, i, x, y, z, c.a);
			this.place(this.headIM, i, x, y + .62, z, c.a);
			this.place(this.eyeIM, i, x * .985, y + .66, z * .985, c.a);
			this.bodyIM.setColorAt(i, c.tint);
			this.headIM.setColorAt(i, c.tint);
		}
		this.bodyIM.instanceMatrix.needsUpdate = true;
		this.headIM.instanceMatrix.needsUpdate = true;
		this.eyeIM.instanceMatrix.needsUpdate = true;
		if (this.bodyIM.instanceColor) this.bodyIM.instanceColor.needsUpdate = true;
		if (this.headIM.instanceColor) this.headIM.instanceColor.needsUpdate = true;
	}
	place(mesh, i, x, y, z, a) {
		this.dummy.position.set(x, y, z);
		this.dummy.lookAt(0, y, 0);
		this.dummy.rotateY(Math.PI);
		this.dummy.scale.set(1, 1, 1);
		this.dummy.updateMatrix();
		mesh.setMatrixAt(i, this.dummy.matrix);
	}
	drawCars(cars) {
		const meshes = [
			this.chassis,
			this.cabin,
			this.hood,
			this.turret,
			this.gun,
			this.wheels,
			this.lamps
		];
		for (const m of meshes) m.count = 0;
		cars.forEach((car, i) => {
			if (i > 15) return;
			const sunk = car.dead ? -.45 : 0;
			const rgtX = Math.cos(car.yaw);
			const rgtZ = -Math.sin(car.yaw);
			const lat = car.vx * rgtX + car.vz * rgtZ;
			const roll = Math.max(-.16, Math.min(.16, -lat * .035));
			this.compose(this.chassis, i, car, 0, .58 + sunk, 0, 0, 0, roll, car.scale[0], car.scale[1], car.scale[2], car.color);
			this.compose(this.cabin, i, car, 0, 1.02 + sunk, -.28, 0, 0, roll, car.scale[0], car.scale[1], car.scale[2], 1709074);
			this.compose(this.hood, i, car, 0, .78 + sunk, .95, 0, 0, roll, car.scale[0], car.scale[1], car.scale[2], car.color);
			const gunYaw = car.turret - car.yaw;
			this.compose(this.turret, i, car, 0, 1.28 + sunk, -.15, gunYaw, 0, 0, 1, 1, 1, 2366488);
			this.compose(this.gun, i, car, 0, 1.32 + sunk, .72, gunYaw, 0, 0, 1, 1, 1, 1709588);
			const spin = car.wheel;
			const wx = .78 * car.scale[0];
			const wz = 1.05 * car.scale[2];
			[
				[wx, wz],
				[-wx, wz],
				[wx, -wz],
				[-wx, -wz]
			].forEach((w, k) => {
				this.compose(this.wheels, i * 4 + k, car, w[0], .38 + sunk, w[1], 0, spin, 0, 1, 1, 1, 1315087);
			});
			this.compose(this.lamps, i * 2, car, .42, .62 + sunk, 1.55, 0, 0, 0, 1, 1, 1, 16769456);
			this.compose(this.lamps, i * 2 + 1, car, -.42, .62 + sunk, 1.55, 0, 0, 0, 1, 1, 1, 16769456);
			if (car.nitro && !car.dead) this.burst(car.x, car.z, 1, 16748288, .4, .4);
		});
		for (const m of meshes) m.instanceMatrix.needsUpdate = true;
	}
	compose(mesh, index, car, lx, ly, lz, lyaw, lpitch, lroll, sx, sy, sz, color) {
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
	drawLoot(frame) {
		this.lootIM.count = 0;
		const colors = {
			HEALTH: 9159498,
			AMMO: 16748288,
			NITRO: 14172949,
			ARMOR: 12887412,
			WEAPON: 16119285,
			REPAIR: 16757504
		};
		frame.loot.forEach((l, i) => {
			if (!l.active) return;
			const bob = Math.sin(frame.time * 2.4 + i) * .12;
			this.dummy.position.set(l.x, .85 + bob, l.z);
			this.dummy.rotation.set(frame.time, frame.time * .8, 0);
			this.dummy.scale.set(1, 1, 1);
			this.dummy.updateMatrix();
			const slot = this.lootIM.count;
			this.lootIM.setMatrixAt(slot, this.dummy.matrix);
			this.lootIM.setColorAt(slot, this.color.set(colors[l.kind] ?? 16777215));
			this.lootIM.count = slot + 1;
		});
		this.lootIM.instanceMatrix.needsUpdate = true;
		if (this.lootIM.instanceColor) this.lootIM.instanceColor.needsUpdate = true;
	}
	drawBullets(bullets) {
		this.bulletIM.count = 0;
		for (const b of bullets) {
			if (!b.alive) continue;
			const slot = this.bulletIM.count;
			if (slot >= 96) break;
			this.dummy.position.set(b.x, b.kind === "rocket" ? .9 : 1.15, b.z);
			this.dummy.rotation.set(0, b.yaw + Math.PI, 0);
			const len = b.kind === "rocket" ? 1.4 : b.kind === "shell" ? 1.1 : .7;
			this.dummy.scale.set(b.kind === "pellet" ? .6 : 1, 1, len);
			this.dummy.updateMatrix();
			this.bulletIM.setMatrixAt(slot, this.dummy.matrix);
			const col = b.kind === "emp" ? 16748288 : b.kind === "rocket" ? 14172949 : 16769162;
			this.bulletIM.setColorAt(slot, this.color.set(col));
			this.bulletIM.count = slot + 1;
		}
		this.bulletIM.instanceMatrix.needsUpdate = true;
		if (this.bulletIM.instanceColor) this.bulletIM.instanceColor.needsUpdate = true;
	}
	stepPuffs(dt) {
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
		for (let i = n; i < 240; i++) this.puffPos[i * 3 + 1] = -10;
		geo.getAttribute("position").needsUpdate = true;
		geo.getAttribute("color").needsUpdate = true;
		geo.setDrawRange(0, n);
	}
};
var KEY = "derby-arena-save-v1";
var FRESH = {
	v: 1,
	scrap: 80,
	engine: 1,
	armor: 1,
	turret: 1,
	magnet: 1,
	wins: 0,
	bestKills: 0,
	quality: "AUTO",
	sensitivity: 1,
	sound: true
};
function loadSave() {
	try {
		const raw = localStorage.getItem(KEY);
		if (!raw) return { ...FRESH };
		const parsed = JSON.parse(raw);
		if (parsed.v !== 1) return { ...FRESH };
		return {
			...FRESH,
			...parsed,
			v: 1
		};
	} catch {
		return { ...FRESH };
	}
}
function writeSave(data) {
	try {
		localStorage.setItem(KEY, JSON.stringify(data));
	} catch {}
}
function resolveQuality(q) {
	if (q !== "AUTO") return q;
	if (typeof navigator === "undefined") return "MEDIUM";
	const cores = navigator.hardwareConcurrency || 4;
	if ((window.matchMedia?.("(pointer: coarse)")?.matches ?? false) && cores <= 6) return "MEDIUM";
	if (cores <= 4) return "LOW";
	return "HIGH";
}
var ROSTER = [
	{
		name: "CRUSHER",
		weapon: "MG",
		hp: 110,
		color: 7234908,
		scale: [
			1.08,
			1,
			1.05
		],
		armor: 0
	},
	{
		name: "VIPER",
		weapon: "MG",
		hp: 88,
		color: 12868646,
		scale: [
			.92,
			.9,
			1.02
		],
		armor: 0
	},
	{
		name: "TITAN",
		weapon: "SHOTGUN",
		hp: 150,
		color: 9268835,
		scale: [
			1.28,
			1.12,
			1.18
		],
		armor: 36
	},
	{
		name: "GHOST",
		weapon: "EMP",
		hp: 76,
		color: 11904122,
		scale: [
			.86,
			.78,
			1.08
		],
		armor: 0
	},
	{
		name: "REAPER",
		weapon: "ROCKET",
		hp: 102,
		color: 8007204,
		scale: [
			1.05,
			1,
			1.12
		],
		armor: 10
	},
	{
		name: "CHAOS",
		weapon: "MG",
		hp: 94,
		color: 13214247,
		scale: [
			1,
			.95,
			1
		],
		armor: 0
	},
	{
		name: "DREAD",
		weapon: "CANNON",
		hp: 124,
		color: 4864818,
		scale: [
			1.12,
			1.05,
			1.2
		],
		armor: 18
	}
];
var EMPTY$1 = {
	throttle: 0,
	steer: 0,
	nitro: false,
	handbrake: false,
	fire: false,
	aimYaw: null,
	reload: false,
	cycle: false
};
function finite(n) {
	return Number.isFinite(n);
}
var DerbySim = class {
	save;
	obstacles = [];
	loot = [];
	cars = [];
	bullets = [];
	events = [];
	player;
	status = "menu";
	time = 0;
	grace = 0;
	countdown = 0;
	kills = 0;
	scrapRun = 0;
	cause = "";
	committed = false;
	fault = "";
	diff = "scrap";
	zone = {
		x: 0,
		z: 0,
		r: 36,
		targetR: 36,
		tx: 0,
		tz: 0,
		phase: 0,
		timer: 28,
		shrinking: false,
		dps: 4,
		final: false
	};
	shrinkFrom = 36;
	shrinkFromX = 0;
	shrinkFromZ = 0;
	shrinkDur = 1;
	banner = "";
	bannerT = 0;
	hitPulse = 0;
	hitStop = 0;
	outside = false;
	pairCd = /* @__PURE__ */ new Map();
	rng = Math.random;
	constructor(save) {
		this.save = save;
		this.obstacles = makeObstacles();
		this.loot = makeLoot(this.obstacles);
		this.player = this.makePlayer();
		this.cars = [this.player];
	}
	makePlayer() {
		return this.blankCar(0, "SEN", true, 14172949, [
			1,
			1,
			1.05
		], 100, 0, "MG");
	}
	blankCar(id, name, isPlayer, color, scale, hp, armor, weapon) {
		const spec = WEAPONS[weapon];
		return {
			id,
			name,
			x: 0,
			z: 0,
			vx: 0,
			vz: 0,
			yaw: 0,
			speed: 0,
			hp,
			maxHp: hp,
			armorPts: armor,
			dead: false,
			color,
			scale,
			wheel: 0,
			turret: 0,
			nitro: 100,
			nitroOn: false,
			isPlayer,
			isBoss: false,
			weapon,
			owned: [weapon],
			ammo: spec.mag,
			reserve: spec.mag * 3,
			mag: spec.mag,
			cooldown: 0,
			reload: 0,
			heat: 0,
			overheated: false,
			stun: 0,
			stuck: 0,
			state: "SEARCH",
			stateT: 2.8,
			target: "point",
			targetId: -1,
			tx: 0,
			tz: 0,
			strafe: 1,
			hurt: 0
		};
	}
	start(diffId) {
		const diff = DIFFS.find((d) => d.id === diffId) ?? DIFFS[0];
		this.diff = diff.id;
		this.rng = Math.random;
		this.obstacles = makeObstacles();
		this.loot = makeLoot(this.obstacles);
		this.bullets = [];
		this.events = [];
		this.pairCd.clear();
		this.time = 0;
		this.grace = 8.2;
		this.countdown = 3.25;
		this.kills = 0;
		this.scrapRun = 0;
		this.cause = "";
		this.committed = false;
		this.fault = "";
		this.status = "play";
		this.banner = "3";
		this.bannerT = .7;
		this.hitPulse = 0;
		this.hitStop = 0;
		this.outside = false;
		this.zone = {
			x: 0,
			z: 0,
			r: 36,
			targetR: 36,
			tx: 0,
			tz: 0,
			phase: 0,
			timer: PHASES[0].wait * diff.zone,
			shrinking: false,
			dps: 4,
			final: false
		};
		const hpBonus = 1 + (this.save.armor - 1) * .04;
		this.player = this.blankCar(0, "SEN", true, 14172949, [
			1,
			1,
			1.05
		], 100 * hpBonus, 0, "MG");
		this.player.maxHp = 100 * hpBonus;
		this.player.hp = this.player.maxHp;
		const spawn = this.findClear(0, 13.5, 3.2);
		this.player.x = spawn.x;
		this.player.z = spawn.z;
		this.player.yaw = yawToward(spawn.x, spawn.z, 0, 0);
		const enemies = [];
		const total = diff.count;
		for (let i = 0; i < total; i++) {
			const boss = diff.boss && i === 0;
			const proto = boss ? {
				name: "WAR RIG",
				weapon: "CANNON",
				hp: 340,
				color: 9048080,
				scale: [
					1.45,
					1.18,
					1.72
				],
				armor: 80
			} : ROSTER[i % ROSTER.length];
			const hp = proto.hp * diff.hp;
			const car = this.blankCar(i + 1, proto.name, false, proto.color, proto.scale, hp, proto.armor, proto.weapon);
			car.isBoss = boss;
			car.owned = [proto.weapon];
			car.ammo = 999;
			car.reserve = 9999;
			const placed = this.placeEnemy(i, total, enemies);
			car.x = placed.x;
			car.z = placed.z;
			car.yaw = yawToward(placed.x, placed.z, 0, 0);
			car.turret = car.yaw;
			car.state = "SEARCH";
			car.stateT = 2.6 + i % 5 * .18;
			car.target = "point";
			const a = this.rng() * Math.PI * 2;
			const rr = 4 + this.rng() * 10;
			car.tx = Math.cos(a) * rr;
			car.tz = Math.sin(a) * rr;
			enemies.push(car);
		}
		this.cars = [this.player, ...enemies];
		this.checkFinite();
	}
	findClear(x, z, extra) {
		let best = {
			x,
			z
		};
		let bestD = -1;
		for (let ring = 0; ring < 8; ring++) {
			const rad = ring * 1.4;
			const steps = ring === 0 ? 1 : 10;
			for (let s = 0; s < steps; s++) {
				const a = s / steps * Math.PI * 2;
				const px = x + Math.sin(a) * rad;
				const pz = z + Math.cos(a) * rad;
				if (Math.hypot(px, pz) > 32) continue;
				let gap = 36;
				for (const o of this.obstacles) gap = Math.min(gap, Math.hypot(o.x - px, o.z - pz) - o.r);
				if (gap > bestD) {
					bestD = gap;
					best = {
						x: px,
						z: pz
					};
				}
				if (gap > extra) return {
					x: px,
					z: pz
				};
			}
		}
		return best;
	}
	placeEnemy(i, total, existing) {
		for (let attempt = 0; attempt < 24; attempt++) {
			const a = i / total * Math.PI * 2 + attempt * .31 + .4;
			const rad = 26 + i % 3 * 1.4 - attempt * .15;
			const x = Math.sin(a) * rad;
			const z = Math.cos(a) * rad;
			if (Math.hypot(x, z) > 32.5) continue;
			if (Math.hypot(x - this.player.x, z - this.player.z) < 11) continue;
			if (existing.some((e) => Math.hypot(e.x - x, e.z - z) < 4.2)) continue;
			if (this.obstacles.some((o) => Math.hypot(o.x - x, o.z - z) < o.r + 2.6)) continue;
			return {
				x,
				z
			};
		}
		const a = i / total * Math.PI * 2 + Math.PI;
		return this.findClear(Math.sin(a) * 20, Math.cos(a) * 20, 2.2);
	}
	buy(key) {
		const level = this.save[key];
		if (level >= 5) return "MAKSİMUM";
		const cost = UPGRADE_COST[level - 1];
		if (this.save.scrap < cost) return "HURDA YETMEZ";
		this.save.scrap -= cost;
		this.save[key] = level + 1;
		return null;
	}
	fixed(dt, input) {
		if (this.status !== "play") return;
		if (this.hitStop > 0) {
			this.hitStop = Math.max(0, this.hitStop - dt);
			this.hitPulse = 1;
			return;
		}
		this.time += dt;
		this.grace = Math.max(0, this.grace - dt);
		this.countdown = Math.max(-1, this.countdown - dt);
		this.bannerT = Math.max(0, this.bannerT - dt);
		this.hitPulse = Math.max(0, this.hitPulse - dt * 1.6);
		this.player.hurt = Math.max(0, this.player.hurt - dt);
		if (this.countdown > 2.2) this.setBanner("3");
		else if (this.countdown > 1.2) this.setBanner("2");
		else if (this.countdown > .2) this.setBanner("1");
		else if (this.countdown > -.55) this.setBanner("SAVAŞ");
		for (const [k, v] of this.pairCd) {
			const n = v - dt;
			if (n <= 0) this.pairCd.delete(k);
			else this.pairCd.set(k, n);
		}
		this.stepZone(dt);
		this.stepPlayer(dt, input);
		this.stepEnemies(dt);
		this.stepBullets(dt);
		this.stepLoot(dt);
		this.zoneDamage(dt);
		this.finishCheck();
		this.checkFinite();
	}
	setBanner(text) {
		if (this.banner === text && this.bannerT > .05) return;
		if (this.banner === text) return;
		this.banner = text;
		this.bannerT = text === "SAVAŞ" ? .7 : .95;
	}
	stepZone(dt) {
		const z = this.zone;
		const diff = DIFFS.find((d) => d.id === this.diff);
		z.timer -= dt;
		if (z.shrinking) {
			const t = clamp(1 - z.timer / this.shrinkDur, 0, 1);
			const e = t * t * (3 - 2 * t);
			z.r = this.shrinkFrom + (z.targetR - this.shrinkFrom) * e;
			z.x = this.shrinkFromX + (z.tx - this.shrinkFromX) * e;
			z.z = this.shrinkFromZ + (z.tz - this.shrinkFromZ) * e;
			if (z.timer <= 0) {
				z.r = z.targetR;
				z.x = z.tx;
				z.z = z.tz;
				z.shrinking = false;
				z.phase += 1;
				if (z.phase < PHASES.length) {
					z.timer = PHASES[z.phase].wait * diff.zone;
					this.setBanner("BÖLGE BEKLEMEDE");
				} else {
					z.final = true;
					z.timer = 999;
					this.setBanner("SON DAİRE");
				}
			}
		} else if (z.timer <= 0 && z.phase < PHASES.length && !z.final) {
			const p = PHASES[z.phase];
			this.shrinkFrom = z.r;
			this.shrinkFromX = z.x;
			this.shrinkFromZ = z.z;
			this.shrinkDur = p.shrink * diff.zone;
			z.targetR = p.radius;
			const maxDrift = Math.max(0, (z.r - p.radius) * .55);
			const a = this.rng() * Math.PI * 2;
			const d = this.rng() * maxDrift;
			let tx = z.x + Math.cos(a) * d;
			let tz = z.z + Math.sin(a) * d;
			const lim = Math.max(0, 36 - p.radius - 1.5);
			const md = Math.hypot(tx, tz);
			if (md > lim && md > .001) {
				tx *= lim / md;
				tz *= lim / md;
			}
			z.tx = tx;
			z.tz = tz;
			z.dps = p.dps;
			z.timer = this.shrinkDur;
			z.shrinking = true;
			this.setBanner("BÖLGE DARALIYOR");
		}
	}
	reduction() {
		return clamp(.08 * this.save.armor, 0, .42);
	}
	engineAccel() {
		return 11.5 * (1 + (this.save.engine - 1) * .1);
	}
	engineMax() {
		return 15.5 * (1 + (this.save.engine - 1) * .08);
	}
	turretMul() {
		return 1 + (this.save.turret - 1) * .12;
	}
	magnetR() {
		return 2.35 + (this.save.magnet - 1) * .72;
	}
	stepPlayer(dt, input) {
		const p = this.player;
		if (p.dead) {
			p.speed *= Math.exp(-1.6 * dt);
			p.vx *= Math.exp(-1.6 * dt);
			p.vz *= Math.exp(-1.6 * dt);
			p.x += p.vx * dt;
			p.z += p.vz * dt;
			return;
		}
		if (input.cycle) this.cycleWeapon(p);
		if (input.reload) this.startReload(p);
		this.tickWeapon(p, dt, false);
		const drive = p.stun > 0 ? {
			...input,
			throttle: input.throttle * .35,
			steer: input.steer * .45,
			nitro: false
		} : input;
		if (p.stun > 0) p.stun = Math.max(0, p.stun - dt);
		this.integrate(p, drive, dt, {
			accel: this.engineAccel(),
			max: this.engineMax(),
			grip: 13,
			turn: 2.35 * this.save.sensitivity,
			nitro: true
		});
		this.keepInside(p, dt, true);
		this.collideCars(p, dt);
		this.collideObstacles(p, dt, true);
		let aim = input.aimYaw;
		if (aim == null) aim = this.autoAim(p);
		const diff = wrapAngle(aim - p.turret);
		p.turret += clamp(diff, -4.5 * dt, 4.5 * dt);
		if (input.fire) this.tryFire(p, p.turret, true, this.turretMul());
		p.wheel += p.speed * dt * 2.4;
	}
	autoAim(p) {
		const spec = WEAPONS[p.weapon];
		const f = forward(p.yaw);
		let best = 0;
		let bestYaw = p.yaw;
		for (const e of this.cars) {
			if (e.dead || e === p) continue;
			const dx = e.x - p.x;
			const dz = e.z - p.z;
			const dist = Math.hypot(dx, dz);
			if (dist > spec.range || dist < .001) continue;
			const dot = dx / dist * f.x + dz / dist * f.z;
			if (dot < .42) continue;
			const score = dot * 2 + (1 - dist / spec.range);
			if (score > best) {
				best = score;
				bestYaw = yawToward(p.x, p.z, e.x, e.z);
			}
		}
		return bestYaw;
	}
	integrate(body, input, dt, opts) {
		if (opts.nitro && input.nitro && body.nitro > 0 && input.throttle >= 0) {
			body.nitroOn = true;
			body.nitro = Math.max(0, body.nitro - 24 * dt);
		} else {
			body.nitroOn = false;
			body.nitro = Math.min(100, body.nitro + 14 * dt);
		}
		const speedFactor = clamp(Math.abs(body.speed) / 5.2, .2, 1);
		const reverse = body.speed < -.45 ? -1 : 1;
		const turn = opts.turn * (input.handbrake ? 1.55 : 1);
		body.yaw = wrapAngle(body.yaw + input.steer * turn * speedFactor * reverse * dt);
		const max = opts.max * (body.nitroOn ? 1.55 : 1);
		const accel = opts.accel * (body.nitroOn ? 2.25 : 1);
		if (input.throttle > .05) body.speed = Math.min(max, body.speed + input.throttle * accel * dt);
		else if (input.throttle < -.05) {
			if (body.speed > .7) body.speed += input.throttle * accel * 2.5 * dt;
			else body.speed = Math.max(-max * .55, body.speed + input.throttle * accel * .85 * dt);
		} else {
			const drag = input.handbrake ? 1.7 : .72;
			body.speed *= Math.exp(-drag * dt);
			if (Math.abs(body.speed) < .12) body.speed = 0;
		}
		const f = forward(body.yaw);
		const tvx = f.x * body.speed;
		const tvz = f.z * body.speed;
		const grip = input.handbrake ? 3.1 : opts.grip;
		const t = 1 - Math.exp(-grip * dt);
		body.vx += (tvx - body.vx) * t;
		body.vz += (tvz - body.vz) * t;
		body.x += body.vx * dt;
		body.z += body.vz * dt;
	}
	keepInside(body, dt, hurts) {
		const dist = Math.hypot(body.x, body.z);
		if (dist <= 36 || dist < .001) return;
		const nx = body.x / dist;
		const nz = body.z / dist;
		body.x = nx * 36;
		body.z = nz * 36;
		const out = body.vx * nx + body.vz * nz;
		if (out > 0) {
			body.vx -= nx * out * 1.55;
			body.vz -= nz * out * 1.55;
			body.speed *= -.32;
			if (hurts && out > 9) this.impactDamage(body, (out - 9) * .85, `bar:${body.id}`, dt);
		}
	}
	impactDamage(body, amount, key, _dt) {
		if (this.pairCd.has(key)) return;
		this.pairCd.set(key, .42);
		this.applyDamage(body, amount, body.isPlayer ? "bariyer" : "");
	}
	applyDamage(body, amount, cause) {
		if (body.dead || amount <= 0) return;
		if (body.isPlayer && this.grace > 0) return;
		let d = amount;
		if (body.isPlayer) d *= 1 - this.reduction();
		else d *= body.isBoss ? .82 : 1;
		if (body.armorPts > 0) {
			const absorb = Math.min(body.armorPts, d * .72);
			body.armorPts -= absorb;
			d -= absorb;
		}
		body.hp -= d;
		if (body.isPlayer) {
			this.hitPulse = 1;
			this.player.hurt = 1;
			if (cause) this.cause = cause;
			if (cause !== "bölge" && amount >= 6) {
				this.hitStop = .055;
				this.events.push({ t: "player-hit" });
			}
		}
		if (body.hp <= 0) this.kill(body, cause || "darbe");
	}
	kill(body, cause) {
		if (body.dead) return;
		body.dead = true;
		body.hp = 0;
		body.nitroOn = false;
		this.events.push({
			t: "boom-soft",
			x: body.x,
			z: body.z
		});
		if (body.isPlayer) {
			this.cause = cause;
			this.status = "dead";
			this.banner = "ELENDİN";
			this.bannerT = 2;
			return;
		}
		this.kills += 1;
		this.scrapRun += body.isBoss ? 60 : 22;
		this.banner = `${body.name} DÜŞTÜ`;
		this.bannerT = 1.15;
		if (this.rng() < .35) {
			const pad = this.loot.find((l) => !l.active);
			if (pad) {
				pad.x = body.x;
				pad.z = body.z;
				pad.kind = "HEALTH";
				pad.weapon = null;
				pad.active = true;
				pad.respawn = 0;
			}
		}
	}
	collideCars(body, dt) {
		for (const o of this.cars) {
			if (o === body || o.dead && body.dead) continue;
			const dx = body.x - o.x;
			const dz = body.z - o.z;
			const d = Math.hypot(dx, dz) || 1e-4;
			const min = CAR_R * (body.scale[0] + o.scale[0]) * .72;
			if (d >= min) continue;
			const nx = dx / d;
			const nz = dz / d;
			const push = (min - d) * .5;
			if (!body.dead) {
				body.x += nx * push;
				body.z += nz * push;
			}
			if (!o.dead) {
				o.x -= nx * push;
				o.z -= nz * push;
			}
			const rel = (body.vx - o.vx) * nx + (body.vz - o.vz) * nz;
			if (rel < 0) {
				const bounce = Math.min(rel, 0);
				if (!body.dead) {
					body.vx -= nx * bounce * .85;
					body.vz -= nz * bounce * .85;
				}
				if (!o.dead) {
					o.vx += nx * bounce * .85;
					o.vz += nz * bounce * .85;
				}
				const impact = -rel;
				if (impact > 8 && !body.dead && !o.dead) {
					const key = body.id < o.id ? `c:${body.id}:${o.id}` : `c:${o.id}:${body.id}`;
					if (!this.pairCd.has(key)) {
						this.pairCd.set(key, .38);
						const dmg = (impact - 8) * 1.35;
						this.applyDamage(body, dmg * (o.isBoss ? 1.25 : 1), "çarpışma");
						this.applyDamage(o, dmg * (body.isBoss ? 1.25 : 1), "çarpışma");
						this.events.push({
							t: "hit",
							x: (body.x + o.x) / 2,
							z: (body.z + o.z) / 2
						});
					}
				}
			}
		}
	}
	collideObstacles(body, _dt, hurts) {
		if (body.dead) return;
		const rad = CAR_R * body.scale[0];
		for (const o of this.obstacles) {
			if (o.dead) continue;
			const dx = body.x - o.x;
			const dz = body.z - o.z;
			const d = Math.hypot(dx, dz) || 1e-4;
			const min = rad + o.r;
			if (d >= min) continue;
			const nx = dx / d;
			const nz = dz / d;
			body.x += nx * (min - d);
			body.z += nz * (min - d);
			const into = body.vx * nx + body.vz * nz;
			if (into < 0) {
				body.vx -= nx * into * 1.35;
				body.vz -= nz * into * 1.35;
				body.speed *= .82;
				const impact = -into;
				if (hurts && impact > 8) this.impactDamage(body, (impact - 8) * (o.explosive ? .4 : 1.1), `o:${body.id}:${o.id}`, 0);
				if (o.explosive && (impact > 7 || o.hp <= 0)) this.explodeBarrel(o);
				else if (!o.explosive) {
					o.hp -= impact * .8;
					if (o.hp <= 0) o.dead = true;
				}
			}
		}
		for (const o of this.cars) {
			if (!o.dead || o === body) continue;
			const dx = body.x - o.x;
			const dz = body.z - o.z;
			const d = Math.hypot(dx, dz) || 1e-4;
			const min = rad + 1.15;
			if (d >= min) continue;
			const nx = dx / d;
			const nz = dz / d;
			body.x += nx * (min - d);
			body.z += nz * (min - d);
			const into = body.vx * nx + body.vz * nz;
			if (into < 0) {
				body.vx -= nx * into * 1.2;
				body.vz -= nz * into * 1.2;
				body.speed *= .9;
			}
		}
	}
	explodeBarrel(o) {
		if (o.dead) return;
		o.dead = true;
		o.hp = 0;
		this.events.push({
			t: "boom",
			x: o.x,
			z: o.z,
			r: 6.4
		});
		for (const c of this.cars) {
			if (c.dead) continue;
			const d = Math.hypot(c.x - o.x, c.z - o.z);
			if (d > 6.4) continue;
			const fall = 1 - d / 6.4;
			this.applyDamage(c, 48 * fall, "patlama");
			const nx = (c.x - o.x) / (d || 1);
			const nz = (c.z - o.z) / (d || 1);
			c.vx += nx * 10 * fall;
			c.vz += nz * 10 * fall;
		}
	}
	stepEnemies(dt) {
		for (const e of this.cars) {
			if (e.isPlayer) continue;
			if (e.dead) {
				e.vx *= Math.exp(-2 * dt);
				e.vz *= Math.exp(-2 * dt);
				e.speed *= Math.exp(-2 * dt);
				e.x += e.vx * dt;
				e.z += e.vz * dt;
				continue;
			}
			this.tickWeapon(e, dt, true);
			if (e.stun > 0) {
				e.stun -= dt;
				e.speed *= Math.exp(-1.4 * dt);
				this.integrate(e, {
					...EMPTY$1,
					throttle: .1
				}, dt, {
					accel: 8,
					max: 10,
					grip: 10,
					turn: 1.6,
					nitro: false
				});
				this.keepInside(e, dt, false);
				continue;
			}
			e.stateT -= dt;
			if (e.stateT <= 0) this.chooseState(e);
			const nav = this.navigate(e);
			let throttle = nav.throttle;
			let steer = nav.steer;
			const moved = Math.hypot(e.vx, e.vz);
			if (throttle > .25 && moved < .7) e.stuck += dt;
			else e.stuck = Math.max(0, e.stuck - dt * .5);
			if (e.stuck > .6) {
				throttle = -.85;
				if (e.stuck > 1) {
					e.stuck = 0;
					e.yaw = wrapAngle(e.yaw + 1.35 * e.strafe);
				}
			}
			this.integrate(e, {
				...EMPTY$1,
				throttle,
				steer,
				handbrake: e.state === "DODGE"
			}, dt, {
				accel: e.isBoss ? 9 : 10,
				max: e.isBoss ? 13.5 : 14.2,
				grip: 11,
				turn: e.isBoss ? 1.55 : 2.05,
				nitro: false
			});
			this.keepInside(e, dt, false);
			this.collideObstacles(e, dt, false);
			const tgt = this.resolveTarget(e);
			if (tgt && (e.state === "ATTACK" || e.state === "CHASE" || e.state === "DODGE")) {
				const dist = Math.hypot(tgt.x - e.x, tgt.z - e.z);
				const diff = wrapAngle(yawToward(e.x, e.z, tgt.x, tgt.z) - e.turret);
				e.turret += clamp(diff, -2.4 * dt, 2.4 * dt);
				const spec = WEAPONS[e.weapon];
				const blocked = this.segmentBlocked(e.x, e.z, tgt.x, tgt.z, e.id);
				if (!(tgt === this.player && this.grace > 0) && !blocked && dist < spec.range * .92 && Math.abs(diff) < .16 && e.cooldown <= 0) {
					const jitter = (this.rng() - .5) * .08;
					this.tryFire(e, e.turret + jitter, false, e.isBoss ? 1.15 : .85);
					e.cooldown = spec.rate * (e.isBoss ? 2.1 : 3.1) + this.rng() * .35;
				}
			} else e.turret += wrapAngle(e.yaw - e.turret) * Math.min(1, dt * 3);
			e.wheel += e.speed * dt * 2.2;
		}
		this.separateLiving();
	}
	separateLiving() {
		const live = this.cars.filter((c) => !c.dead);
		for (let i = 0; i < live.length; i++) for (let j = i + 1; j < live.length; j++) {
			const a = live[i];
			const b = live[j];
			let dx = b.x - a.x;
			let dz = b.z - a.z;
			let d = Math.hypot(dx, dz);
			const min = 2.5;
			if (d < .001) {
				dx = .2;
				dz = .1;
				d = Math.hypot(dx, dz);
			}
			if (d < min) {
				const push = (min - d) * .35;
				const nx = dx / d;
				const nz = dz / d;
				a.x -= nx * push;
				a.z -= nz * push;
				b.x += nx * push;
				b.z += nz * push;
			}
		}
	}
	chooseState(e) {
		const hpF = e.hp / e.maxHp;
		if (!this.insideZone(e.x, e.z, 1.6)) {
			e.state = "ZONE";
			e.stateT = 1.6;
			return;
		}
		if (hpF < .3) {
			const heal = this.nearestLoot(e, (l) => l.kind === "HEALTH" || l.kind === "REPAIR" || l.kind === "ARMOR");
			if (heal && Math.hypot(heal.x - e.x, heal.z - e.z) < 18) {
				e.state = "LOOT";
				e.target = "loot";
				e.targetId = heal.id;
				e.stateT = 3.2;
				return;
			}
			if (this.rng() < .55) {
				e.state = "RETREAT";
				e.stateT = 1.6;
				return;
			}
		}
		let best = -1;
		let kind = "player";
		let id = -1;
		if (!this.player.dead) {
			best = 18 / (Math.hypot(this.player.x - e.x, this.player.z - e.z) + 4) + .55 + (1 - this.player.hp / this.player.maxHp) * .4;
			kind = "player";
		}
		for (const o of this.cars) {
			if (o === e || o.dead || o.isPlayer) continue;
			const score = 18 / (Math.hypot(o.x - e.x, o.z - e.z) + 4) + (1 - o.hp / o.maxHp) * .7 + (o.isBoss ? -.25 : 0) + this.rng() * .25;
			if (score > best) {
				best = score;
				kind = "enemy";
				id = o.id;
			}
		}
		if (best < 0) {
			e.state = "SEARCH";
			this.randomPoint(e);
			e.stateT = 2.2;
			return;
		}
		e.target = kind;
		e.targetId = id;
		if (e.weapon === "MG") {
			const gun = this.nearestLoot(e, (l) => l.kind === "WEAPON");
			if (gun && Math.hypot(gun.x - e.x, gun.z - e.z) < 12 && this.rng() < .45) {
				e.state = "LOOT";
				e.target = "loot";
				e.targetId = gun.id;
				e.stateT = 2.8;
				return;
			}
		}
		e.state = "CHASE";
		e.strafe = this.rng() < .5 ? -1 : 1;
		e.stateT = 1.8 + this.rng() * 1.6;
	}
	navigate(e) {
		let tx = e.tx;
		let tz = e.tz;
		let throttle = .75;
		let strafe = 0;
		const tgt = this.resolveTarget(e);
		if (e.state === "CHASE" || e.state === "ATTACK" || e.state === "DODGE") {
			if (!tgt) e.stateT = 0;
			else {
				tx = tgt.x;
				tz = tgt.z;
				const d = Math.hypot(tx - e.x, tz - e.z);
				const range = WEAPONS[e.weapon].range;
				if (e.state === "ATTACK" || e.state === "CHASE" && d < range * .62) {
					e.state = "ATTACK";
					strafe = e.strafe;
					throttle = d < range * .34 ? .45 : .8;
				}
			}
		} else if (e.state === "RETREAT" && tgt) {
			tx = e.x - (tgt.x - e.x);
			tz = e.z - (tgt.z - e.z);
			throttle = 1;
		} else if (e.state === "LOOT") {
			const loot = this.loot.find((l) => l.id === e.targetId && l.active);
			if (!loot) e.stateT = 0;
			else {
				tx = loot.x;
				tz = loot.z;
				throttle = .9;
			}
		} else if (e.state === "ZONE") {
			tx = this.zone.x;
			tz = this.zone.z;
			throttle = 1;
		} else if (Math.hypot(e.tx - e.x, e.tz - e.z) < 3) e.stateT = 0;
		const diff = wrapAngle(yawToward(e.x, e.z, tx, tz) + strafe * .95 + this.avoidYaw(e) - e.yaw);
		let steer = clamp(diff / .7, -1, 1);
		if (Math.abs(diff) > 2.1 && Math.abs(e.speed) < 2) throttle = -.55;
		else throttle *= 1 - Math.abs(steer) * .28;
		return {
			steer,
			throttle
		};
	}
	resolveTarget(e) {
		if (e.target === "player") return this.player.dead ? null : this.player;
		if (e.target === "enemy") return this.cars.find((c) => c.id === e.targetId && !c.dead) ?? null;
		return null;
	}
	randomPoint(e) {
		const a = this.rng() * Math.PI * 2;
		const r = this.rng() * this.zone.r * .75;
		e.tx = this.zone.x + Math.cos(a) * r;
		e.tz = this.zone.z + Math.sin(a) * r;
		e.target = "point";
	}
	nearestLoot(e, pred) {
		let best = null;
		let bd = 1e9;
		for (const l of this.loot) {
			if (!l.active || !pred(l)) continue;
			const d = Math.hypot(l.x - e.x, l.z - e.z);
			if (d < bd) {
				bd = d;
				best = l;
			}
		}
		return best;
	}
	avoidYaw(e) {
		const f = forward(e.yaw);
		const rgt = right(e.yaw);
		let push = 0;
		const look = (x, z, rad, weight) => {
			const dx = x - e.x;
			const dz = z - e.z;
			const dist = Math.hypot(dx, dz) - rad;
			if (dist > 7 || dist < -1) return;
			if (dx * f.x + dz * f.z < -.4) return;
			const side = dx * rgt.x + dz * rgt.z;
			const strength = clamp(1 - dist / 7, 0, 1) * weight;
			push += (side >= 0 ? -1 : 1) * strength;
		};
		for (const o of this.obstacles) if (!o.dead) look(o.x, o.z, o.r, 1);
		for (const c of this.cars) if (c !== e && !c.dead) look(c.x, c.z, CAR_R, .85);
		const centerD = Math.hypot(e.x, e.z);
		if (centerD > 31) {
			const inX = -e.x / (centerD || 1);
			const inZ = -e.z / (centerD || 1);
			const side = inX * rgt.x + inZ * rgt.z;
			push += clamp(side, -1, 1) * 1.2;
		}
		return clamp(push, -1.35, 1.35);
	}
	segmentBlocked(x, z, tx, tz, ignoreId) {
		const dx = tx - x;
		const dz = tz - z;
		const len = Math.hypot(dx, dz);
		if (len < .001) return false;
		const steps = Math.ceil(len / 1.4);
		for (let i = 1; i < steps; i++) {
			const t = i / steps;
			const px = x + dx * t;
			const pz = z + dz * t;
			for (const o of this.obstacles) {
				if (o.dead || o.kind === "tires" || o.kind === "barrel") continue;
				if (Math.hypot(o.x - px, o.z - pz) < o.r * .82) return true;
			}
			for (const c of this.cars) {
				if (c.dead || c.id === ignoreId) continue;
				if (Math.hypot(c.x - px, c.z - pz) < 1.42 * .7) return true;
			}
		}
		return false;
	}
	tickWeapon(c, dt, ai) {
		c.cooldown = Math.max(0, c.cooldown - dt);
		if (c.reload > 0) {
			c.reload = Math.max(0, c.reload - dt);
			if (c.reload === 0) {
				const spec = WEAPONS[c.weapon];
				const take = ai ? spec.mag : Math.min(spec.mag, c.reserve);
				c.ammo = take;
				if (!ai) c.reserve -= take;
				c.mag = spec.mag;
			}
		}
		c.heat = Math.max(0, c.heat - .72 * dt);
		if (c.overheated && c.heat < .35) c.overheated = false;
	}
	startReload(c) {
		if (c.reload > 0) return;
		const spec = WEAPONS[c.weapon];
		if (c.ammo >= spec.mag) return;
		if (c.reserve <= 0 && c.reserve < 9e3) return;
		c.reload = spec.reload;
	}
	cycleWeapon(c) {
		if (c.owned.length < 2 || c.reload > 0) return;
		const i = c.owned.indexOf(c.weapon);
		const next = c.owned[(i + 1) % c.owned.length];
		this.equip(c, next, false);
	}
	equip(c, id, refill) {
		const spec = WEAPONS[id];
		c.weapon = id;
		c.mag = spec.mag;
		if (refill) {
			c.ammo = spec.mag;
			c.reserve = Math.max(c.reserve, spec.mag * 2);
		} else if (c.ammo > spec.mag) c.ammo = spec.mag;
		c.reload = 0;
		c.cooldown = .15;
	}
	tryFire(c, yaw, player, mul) {
		const spec = WEAPONS[c.weapon];
		if (c.cooldown > 0 || c.reload > 0 || c.overheated) return;
		if (!player && c.ammo > 900) {} else if (c.ammo <= 0) {
			this.startReload(c);
			return;
		} else {
			c.ammo -= 1;
			if (c.ammo <= 0) this.startReload(c);
		}
		c.cooldown = Math.max(c.cooldown, spec.rate);
		c.heat += spec.heat;
		if (c.heat >= 1) c.overheated = true;
		const origin = forward(yaw);
		const muzzleX = c.x + origin.x * (1.5 * c.scale[2]);
		const muzzleZ = c.z + origin.z * (1.5 * c.scale[2]);
		this.events.push({
			t: "muzzle",
			x: muzzleX,
			z: muzzleZ,
			yaw,
			kind: spec.kind,
			player
		});
		const pellets = spec.pellets;
		for (let i = 0; i < pellets; i++) {
			const dir = forward(yaw + (this.rng() - .5) * spec.spread * 2);
			this.spawnBullet({
				x: muzzleX,
				z: muzzleZ,
				vx: dir.x * spec.speed + c.vx * .25,
				vz: dir.z * spec.speed + c.vz * .25,
				damage: spec.damage * mul,
				life: spec.range / spec.speed,
				kind: spec.kind,
				owner: c.id,
				splash: spec.splash,
				stun: spec.stun,
				radius: spec.kind === "rocket" ? .7 : spec.kind === "shell" ? .45 : .28
			});
		}
	}
	spawnBullet(b) {
		let slot = this.bullets.find((x) => !x.alive);
		if (!slot) {
			if (this.bullets.length >= 96) slot = this.bullets[0];
			else {
				slot = {
					...b,
					alive: true
				};
				this.bullets.push(slot);
				return;
			}
		}
		Object.assign(slot, b, { alive: true });
	}
	stepBullets(dt) {
		for (const b of this.bullets) {
			if (!b.alive) continue;
			b.life -= dt;
			b.x += b.vx * dt;
			b.z += b.vz * dt;
			if (b.life <= 0 || Math.hypot(b.x, b.z) > 37.2) {
				if (Math.hypot(b.x, b.z) > 37.2) this.events.push({ t: "shield" });
				if (b.splash > 0) this.splash(b);
				b.alive = false;
				continue;
			}
			let hit = false;
			for (const o of this.obstacles) {
				if (o.dead) continue;
				if (Math.hypot(o.x - b.x, o.z - b.z) < o.r + b.radius) {
					hit = true;
					if (o.explosive) this.explodeBarrel(o);
					else {
						o.hp -= b.damage;
						if (o.hp <= 0) o.dead = true;
					}
					break;
				}
			}
			if (!hit) for (const c of this.cars) {
				if (c.dead || c.id === b.owner) continue;
				const rad = CAR_R * c.scale[0] * .85 + b.radius;
				if (Math.hypot(c.x - b.x, c.z - b.z) < rad) {
					hit = true;
					if (b.splash > 0) this.splash(b);
					else this.applyDamage(c, b.damage, b.owner === 0 ? "mermi" : "mermi");
					if (b.stun > 0) c.stun = Math.max(c.stun, b.stun * (c.isPlayer ? .75 : 1));
					if (b.owner === 0 && !c.isPlayer) this.events.push({
						t: "hit",
						x: b.x,
						z: b.z
					});
					break;
				}
			}
			if (hit) {
				if (b.kind === "rocket" || b.kind === "shell" || b.kind === "emp") this.events.push({
					t: "boom",
					x: b.x,
					z: b.z,
					r: Math.max(1.4, b.splash)
				});
				else this.events.push({
					t: "hit",
					x: b.x,
					z: b.z
				});
				b.alive = false;
			}
		}
	}
	splash(b) {
		for (const c of this.cars) {
			if (c.dead || c.id === b.owner) continue;
			const d = Math.hypot(c.x - b.x, c.z - b.z);
			if (d > b.splash) continue;
			const fall = 1 - d / b.splash;
			this.applyDamage(c, b.damage * fall, "patlama");
			if (b.stun > 0) c.stun = Math.max(c.stun, b.stun * fall);
			const nx = (c.x - b.x) / (d || 1);
			const nz = (c.z - b.z) / (d || 1);
			c.vx += nx * 6 * fall;
			c.vz += nz * 6 * fall;
		}
	}
	stepLoot(dt) {
		for (const l of this.loot) {
			if (!l.active) {
				l.respawn -= dt;
				if (l.respawn <= 0) l.active = true;
				continue;
			}
			for (const c of this.cars) {
				if (c.dead) continue;
				const reach = c.isPlayer ? this.magnetR() : 1.7;
				if (Math.hypot(c.x - l.x, c.z - l.z) > reach) continue;
				this.takeLoot(c, l);
				l.active = false;
				l.respawn = 16;
				break;
			}
		}
	}
	takeLoot(c, l) {
		if (l.kind === "HEALTH") c.hp = Math.min(c.maxHp, c.hp + 36);
		else if (l.kind === "REPAIR") c.hp = Math.min(c.maxHp, c.hp + Math.max(28, (c.maxHp - c.hp) * .45));
		else if (l.kind === "ARMOR") c.armorPts = Math.min(100, c.armorPts + 40);
		else if (l.kind === "NITRO") c.nitro = Math.min(100, c.nitro + 55);
		else if (l.kind === "AMMO") {
			const spec = WEAPONS[c.weapon];
			if (c.reserve < 9e3) c.reserve += spec.mag * 2;
			if (c.ammo === 0) this.startReload(c);
		} else if (l.kind === "WEAPON" && l.weapon) {
			if (!c.owned.includes(l.weapon)) c.owned.push(l.weapon);
			this.equip(c, l.weapon, true);
		}
		if (c.isPlayer) {
			this.scrapRun += 6;
			this.events.push({ t: "pick" });
			this.banner = l.kind === "WEAPON" && l.weapon ? WEAPONS[l.weapon].label : l.kind === "HEALTH" ? "+CAN" : l.kind === "ARMOR" ? "+ZIRH" : l.kind === "NITRO" ? "+NİTRO" : l.kind === "AMMO" ? "+MERMİ" : "+TAMİR";
			this.bannerT = .8;
		}
	}
	insideZone(x, z, margin = 0) {
		return Math.hypot(x - this.zone.x, z - this.zone.z) <= this.zone.r - margin;
	}
	zoneDamage(dt) {
		for (const c of this.cars) {
			if (c.dead) continue;
			const outside = Math.hypot(c.x - this.zone.x, c.z - this.zone.z) - this.zone.r;
			if (c.isPlayer) this.outside = outside > 0;
			if (outside <= 0) continue;
			if (c.isPlayer && this.grace > 0) continue;
			const dmg = (this.zone.dps + outside * .22) * dt;
			this.applyDamage(c, dmg, "bölge");
		}
	}
	finishCheck() {
		if (this.status !== "play") return;
		const alive = this.cars.filter((c) => !c.dead && !c.isPlayer).length;
		if (!this.player.dead && alive === 0 && this.time > 1.2) {
			this.status = "victory";
			this.banner = "ZAFER";
			this.bannerT = 3;
			this.scrapRun += 80;
		}
	}
	commit() {
		if (this.committed || this.status === "menu" || this.status === "play") return 0;
		this.committed = true;
		const earned = Math.max(0, Math.round(this.scrapRun));
		this.save.scrap += earned;
		if (this.status === "victory") this.save.wins += 1;
		if (this.kills > this.save.bestKills) this.save.bestKills = this.kills;
		return earned;
	}
	aliveCount() {
		return this.cars.filter((c) => !c.dead).length;
	}
	place() {
		if (this.status === "victory") return 1;
		return this.cars.filter((c) => !c.dead && !c.isPlayer).length + 1;
	}
	checkFinite() {
		for (const c of this.cars) if (!finite(c.x) || !finite(c.z) || !finite(c.yaw) || !finite(c.hp) || !finite(c.speed)) {
			this.fault = `NaN ${c.name}`;
			c.x = 0;
			c.z = 0;
			c.vx = 0;
			c.vz = 0;
			c.speed = 0;
			c.yaw = 0;
		}
	}
	report() {
		return {
			status: this.status,
			time: Number(this.time.toFixed(1)),
			hp: Number(this.player.hp.toFixed(1)),
			alive: this.aliveCount(),
			kills: this.kills,
			zoneR: Number(this.zone.r.toFixed(2)),
			fault: this.fault,
			nan: Boolean(this.fault)
		};
	}
};
var DerbyRuntime = class {
	sim;
	view;
	sfx = new Sfx();
	held = /* @__PURE__ */ new Set();
	scripted = null;
	touch = {
		throttle: 0,
		steer: 0,
		nitro: false,
		handbrake: false,
		brake: false,
		fire: false
	};
	aimPoint = null;
	aimMode = false;
	firePointer = false;
	cycleLatch = false;
	reloadLatch = false;
	paused = false;
	mode = "menu";
	raf = 0;
	last = 0;
	acc = 0;
	hudAt = 0;
	shield = 0;
	earned = 0;
	note = "";
	mini = null;
	onHud;
	ray = new Raycaster();
	ndc = new Vector2();
	usingMouse = false;
	padWasPause = false;
	dust = 0;
	plane = new Plane(new Vector3(0, 1, 0), 0);
	hit = new Vector3();
	smooth = {
		x: 14,
		y: 7,
		z: 16
	};
	smoothLook = {
		x: 0,
		y: 1,
		z: 0
	};
	constructor(canvas, onHud) {
		this.onHud = onHud;
		this.sim = new DerbySim(loadSave());
		this.sfx.enabled = this.sim.save.sound;
		this.view = new ArenaView(canvas);
		this.view.setObstacles(this.sim.obstacles);
		this.view.setQuality(resolveQuality(this.sim.save.quality));
		this.bindKeys();
		window.__controlsTest = {
			getYaw: () => this.sim.player.yaw,
			getSpeed: () => this.sim.player.speed,
			setKeys: (codes) => {
				this.scripted = new Set(codes);
			},
			setSteer: (v) => {
				this.touch.steer = v;
			}
		};
		window.__derby = this;
	}
	startLoop() {
		this.last = performance.now();
		this.emit(true);
		const loop = (now) => {
			const dt = Math.min(.05, (now - this.last) / 1e3);
			this.last = now;
			if (this.mode === "battle" && !this.paused) {
				this.acc += dt;
				const base = this.readInput();
				let guard = 0;
				let first = true;
				while (this.acc >= 1 / 60 && guard < 5) {
					const input = first ? base : {
						...base,
						cycle: false,
						reload: false
					};
					first = false;
					this.sim.fixed(1 / 60, input);
					this.acc -= 1 / 60;
					guard++;
				}
				this.flushEvents();
				this.kickDust(dt);
				this.sfx.engine(Math.abs(this.sim.player.speed), this.sim.player.nitroOn);
				if (this.sim.status !== "play") {
					this.mode = "result";
					this.earned = this.sim.commit();
					writeSave(this.sim.save);
					if (this.sim.status === "victory") this.sfx.win();
					else this.sfx.lose();
					this.emit(true);
				}
			} else this.sfx.engine(this.mode === "menu" ? 1.6 : 0, false);
			this.shield = Math.max(0, this.shield - dt * .8);
			if (this.sim.outside && this.mode === "battle") this.sfx.zone(this.sim.time);
			this.view.draw(this.frame(dt));
			this.drawMini();
			this.emit(false);
			this.raf = requestAnimationFrame(loop);
		};
		this.raf = requestAnimationFrame(loop);
	}
	dispose() {
		cancelAnimationFrame(this.raf);
		window.removeEventListener("keydown", this.onKeyDown);
		window.removeEventListener("keyup", this.onKeyUp);
		window.removeEventListener("blur", this.onBlur);
		this.sfx.stop();
		this.view.dispose();
	}
	attachMini(canvas) {
		this.mini = canvas;
	}
	pointer(canvas, ev, down, button) {
		if (this.mode !== "battle") return;
		if (window.matchMedia("(pointer: coarse)").matches) return;
		const rect = canvas.getBoundingClientRect();
		this.ndc.x = (ev.clientX - rect.left) / rect.width * 2 - 1;
		this.ndc.y = -((ev.clientY - rect.top) / rect.height) * 2 + 1;
		this.usingMouse = true;
		if (button === 2) this.aimMode = down;
		if (button === 0) this.firePointer = down;
		this.aimFromCamera();
	}
	pointerMove(canvas, ev) {
		if (this.mode !== "battle" || !this.usingMouse) return;
		if (window.matchMedia("(pointer: coarse)").matches) return;
		const rect = canvas.getBoundingClientRect();
		this.ndc.x = (ev.clientX - rect.left) / rect.width * 2 - 1;
		this.ndc.y = -((ev.clientY - rect.top) / rect.height) * 2 + 1;
		this.aimFromCamera();
	}
	setTouch(partial) {
		Object.assign(this.touch, partial);
	}
	startMatch(diff) {
		this.sfx.unlock();
		this.paused = false;
		this.earned = 0;
		this.note = "";
		this.sim.start(diff);
		this.view.setObstacles(this.sim.obstacles);
		const p = this.sim.player;
		const f = forward(p.yaw);
		this.smooth.x = p.x - f.x * 9.6;
		this.smooth.z = p.z - f.z * 9.6;
		this.smooth.y = 4.5;
		this.smoothLook.x = p.x;
		this.smoothLook.z = p.z;
		this.mode = "battle";
		this.acc = 0;
		this.emit(true);
	}
	backToMenu() {
		if (this.mode === "result") this.sim.commit();
		this.mode = "menu";
		this.paused = false;
		this.sim.obstacles = makeObstacles();
		this.sim.loot = makeLoot(this.sim.obstacles);
		this.view.setObstacles(this.sim.obstacles);
		writeSave(this.sim.save);
		this.emit(true);
	}
	togglePause() {
		if (this.mode !== "battle") return;
		this.paused = !this.paused;
		this.emit(true);
	}
	buy(key) {
		this.note = this.sim.buy(key) ?? "";
		writeSave(this.sim.save);
		this.emit(true);
	}
	setQuality(q) {
		this.sim.save.quality = q;
		this.view.setQuality(resolveQuality(q));
		writeSave(this.sim.save);
		this.emit(true);
	}
	setSensitivity(v) {
		this.sim.save.sensitivity = v;
		writeSave(this.sim.save);
		this.emit(true);
	}
	toggleSound() {
		this.sim.save.sound = !this.sim.save.sound;
		this.sfx.enabled = this.sim.save.sound;
		writeSave(this.sim.save);
		this.emit(true);
	}
	resize() {
		this.view.resize();
	}
	onKeyDown = (ev) => {
		if (ev.code === "Space" || ev.code.startsWith("Arrow")) ev.preventDefault();
		this.held.add(ev.code);
		if (ev.code === "KeyQ") this.cycleLatch = true;
		if (ev.code === "KeyR") this.reloadLatch = true;
		if (ev.code === "Escape" || ev.code === "KeyP") this.togglePause();
	};
	onKeyUp = (ev) => {
		this.held.delete(ev.code);
	};
	onBlur = () => {
		this.held.clear();
		this.firePointer = false;
		this.aimMode = false;
	};
	bindKeys() {
		window.addEventListener("keydown", this.onKeyDown);
		window.addEventListener("keyup", this.onKeyUp);
		window.addEventListener("blur", this.onBlur);
	}
	keys() {
		return this.scripted ?? this.held;
	}
	readInput() {
		const keys = this.keys();
		let throttle = 0;
		let steer = 0;
		if (keys.has("KeyW") || keys.has("ArrowUp")) throttle += 1;
		if (keys.has("KeyS") || keys.has("ArrowDown")) throttle -= 1;
		if (keys.has("KeyA") || keys.has("ArrowLeft")) steer += 1;
		if (keys.has("KeyD") || keys.has("ArrowRight")) steer -= 1;
		const pad = this.gamepad();
		if (pad) {
			const ax = pad.axes[0] ?? 0;
			const ay = pad.axes[1] ?? 0;
			if (Math.abs(ax) > .16) steer -= ax;
			if (Math.abs(ay) > .16) throttle -= ay;
		}
		throttle = Math.max(-1, Math.min(1, throttle + this.touch.throttle + (this.touch.brake ? -1 : 0)));
		steer = Math.max(-1, Math.min(1, steer + this.touch.steer));
		const aimYaw = this.aimPoint && this.usingMouse ? angleTo(this.sim.player.x, this.sim.player.z, this.aimPoint.x, this.aimPoint.z) : null;
		const input = {
			throttle,
			steer,
			nitro: keys.has("Space") || this.touch.nitro || Boolean(pad && (pad.buttons[6]?.pressed || pad.buttons[5]?.pressed)),
			handbrake: keys.has("ShiftLeft") || keys.has("ShiftRight") || this.touch.handbrake || Boolean(pad?.buttons[1]?.pressed),
			fire: this.firePointer || this.touch.fire || keys.has("KeyF") || Boolean(pad && (pad.buttons[7]?.pressed || pad.buttons[0]?.pressed)),
			aimYaw,
			reload: this.reloadLatch,
			cycle: this.cycleLatch
		};
		this.reloadLatch = false;
		this.cycleLatch = false;
		const start = Boolean(pad?.buttons[9]?.pressed);
		if (start && !this.padWasPause) this.togglePause();
		this.padWasPause = start;
		return input;
	}
	gamepad() {
		const pads = navigator.getGamepads?.();
		if (!pads) return null;
		for (const pad of pads) if (pad && pad.connected) return pad;
		return null;
	}
	kickDust(dt) {
		const p = this.sim.player;
		if (p.dead) return;
		this.dust -= dt;
		if (Math.abs(p.speed) < 7 || this.dust > 0) return;
		this.dust = .09;
		const f = forward(p.yaw);
		this.view.burst(p.x - f.x * 1.7, p.z - f.z * 1.7, p.nitroOn ? 3 : 2, 12887412, p.nitroOn ? 2.2 : 1.1, .55);
	}
	aimFromCamera() {
		this.ray.setFromCamera(this.ndc, this.view.camera);
		if (this.ray.ray.intersectPlane(this.plane, this.hit)) this.aimPoint = {
			x: this.hit.x,
			z: this.hit.z
		};
	}
	flushEvents() {
		for (const e of this.sim.events) if (e.t === "muzzle") {
			this.sfx.shot(e.kind);
			this.view.burst(e.x, e.z, e.player ? 4 : 2, 16769162, 3, 1);
		} else if (e.t === "boom") {
			this.sfx.boom();
			this.view.burst(e.x, e.z, 16, 14172949, 7, 4);
		} else if (e.t === "boom-soft") {
			this.sfx.boom();
			this.view.burst(e.x, e.z, 10, 16748288, 4, 2);
		} else if (e.t === "hit") {
			this.sfx.hit();
			this.view.burst(e.x, e.z, 4, 16773584, 2, 1);
		} else if (e.t === "player-hit") {
			this.sfx.crunch();
			this.sfx.buzz(26);
		} else if (e.t === "shield") this.shield = 1;
		else if (e.t === "pick") this.sfx.pick();
		this.sim.events.length = 0;
	}
	frame(dt) {
		const t = performance.now() / 1e3;
		if (this.mode !== "battle") {
			const ang = t * .18;
			const dist = 16;
			return {
				mode: "showcase",
				time: t,
				cars: [{
					x: 0,
					z: 0,
					yaw: t * .25,
					vx: 0,
					vz: 0,
					speed: 0,
					turret: t * .25,
					wheel: t,
					color: 14172949,
					scale: [
						1,
						1,
						1.05
					],
					dead: false,
					nitro: false,
					player: true
				}],
				bullets: [],
				loot: this.sim.loot.map((l) => ({
					x: l.x,
					z: l.z,
					kind: l.kind,
					active: true
				})),
				obstacles: this.sim.obstacles,
				zone: {
					x: 0,
					z: 0,
					r: 36 * .92,
					shrinking: false
				},
				shield: .25 + Math.sin(t * .7) * .1,
				cam: {
					x: Math.sin(ang) * dist,
					y: 7.2,
					z: Math.cos(ang) * dist,
					lx: 0,
					ly: .8,
					lz: 0,
					fov: 58
				},
				quality: resolveQuality(this.sim.save.quality)
			};
		}
		const p = this.sim.player;
		const f = forward(p.yaw);
		const r = right(p.yaw);
		const aim = this.aimMode;
		const dist = aim ? 6.4 : 9.6;
		const height = aim ? 3.3 : 4.5;
		const shoulder = aim ? 1.5 : .35;
		let cx = p.x - f.x * dist + r.x * shoulder;
		let cz = p.z - f.z * dist + r.z * shoulder;
		const cy = Math.max(1.6, height + (p.nitroOn ? .3 : 0));
		const cd = Math.hypot(cx, cz);
		if (cd > 34) {
			cx *= 34 / cd;
			cz *= 34 / cd;
		}
		const shake = this.sim.hitPulse * .28;
		const sx = (Math.random() - .5) * shake;
		const sz = (Math.random() - .5) * shake;
		const look = 3.2 + Math.abs(p.speed) * .12;
		const k = 1 - Math.exp(-4.4 * Math.max(dt, .016));
		this.smooth.x += (cx + sx - this.smooth.x) * k;
		this.smooth.y += (cy - this.smooth.y) * k;
		this.smooth.z += (cz + sz - this.smooth.z) * k;
		const lx = p.x + f.x * look;
		const lz = p.z + f.z * look;
		this.smoothLook.x += (lx - this.smoothLook.x) * k;
		this.smoothLook.y += (1.1 - this.smoothLook.y) * k;
		this.smoothLook.z += (lz - this.smoothLook.z) * k;
		const fov = 58 + Math.min(12, Math.abs(p.speed) * .55) + (p.nitroOn ? 6 : 0);
		const cars = this.sim.cars.map((c) => ({
			x: c.x,
			z: c.z,
			yaw: c.yaw,
			vx: c.vx,
			vz: c.vz,
			speed: c.speed,
			turret: c.turret,
			wheel: c.wheel,
			color: c.color,
			scale: c.scale,
			dead: c.dead,
			nitro: c.nitroOn,
			player: c.isPlayer
		}));
		const bullets = this.sim.bullets.filter((b) => b.alive).map((b) => ({
			x: b.x,
			z: b.z,
			yaw: Math.atan2(-b.vx, -b.vz),
			kind: b.kind,
			alive: true
		}));
		return {
			mode: "battle",
			time: this.sim.time,
			cars,
			bullets,
			loot: this.sim.loot.map((l) => ({
				x: l.x,
				z: l.z,
				kind: l.kind,
				active: l.active
			})),
			obstacles: this.sim.obstacles,
			zone: {
				x: this.sim.zone.x,
				z: this.sim.zone.z,
				r: this.sim.zone.r,
				shrinking: this.sim.zone.shrinking
			},
			shield: this.shield,
			cam: {
				x: this.smooth.x,
				y: this.smooth.y,
				z: this.smooth.z,
				lx: this.smoothLook.x,
				ly: this.smoothLook.y,
				lz: this.smoothLook.z,
				fov
			},
			quality: resolveQuality(this.sim.save.quality)
		};
	}
	drawMini() {
		const canvas = this.mini;
		if (!canvas || this.mode === "menu") return;
		const ctx = canvas.getContext("2d");
		if (!ctx) return;
		const w = canvas.width;
		const h = canvas.height;
		ctx.clearRect(0, 0, w, h);
		ctx.fillStyle = "rgba(18,18,18,0.72)";
		ctx.fillRect(0, 0, w, h);
		const map = (x, z) => ({
			px: w / 2 + x / 40 * (w * .46),
			py: h / 2 + z / 40 * (h * .46)
		});
		ctx.strokeStyle = "#c4a574";
		ctx.lineWidth = 2;
		ctx.beginPath();
		ctx.arc(w / 2, h / 2, w * .42, 0, Math.PI * 2);
		ctx.stroke();
		const zc = map(this.sim.zone.x, this.sim.zone.z);
		ctx.strokeStyle = this.sim.zone.shrinking ? "#d32f2f" : "#ff8f00";
		ctx.beginPath();
		ctx.arc(zc.px, zc.py, this.sim.zone.r / 40 * (w * .46), 0, Math.PI * 2);
		ctx.stroke();
		ctx.fillStyle = "#8a847c";
		for (const o of this.sim.obstacles) {
			if (o.dead) continue;
			const pt = map(o.x, o.z);
			ctx.fillRect(pt.px - 1.5, pt.py - 1.5, 3, 3);
		}
		ctx.fillStyle = "#ff8f00";
		for (const l of this.sim.loot) {
			if (!l.active) continue;
			const pt = map(l.x, l.z);
			ctx.fillRect(pt.px - 1.5, pt.py - 1.5, 3, 3);
		}
		for (const c of this.sim.cars) {
			const pt = map(c.x, c.z);
			if (c.isPlayer) {
				ctx.save();
				ctx.translate(pt.px, pt.py);
				ctx.rotate(c.yaw);
				ctx.fillStyle = "#f5f5f5";
				ctx.beginPath();
				ctx.moveTo(0, -5);
				ctx.lineTo(3.5, 4);
				ctx.lineTo(-3.5, 4);
				ctx.fill();
				ctx.restore();
			} else if (!c.dead) {
				ctx.fillStyle = c.isBoss ? "#d32f2f" : "#d84315";
				ctx.fillRect(pt.px - 2, pt.py - 2, 4, 4);
			}
		}
	}
	emit(force) {
		const now = performance.now();
		if (!force && now - this.hudAt < 120) return;
		this.hudAt = now;
		const p = this.sim.player;
		const spec = WEAPONS[p.weapon];
		const snap = {
			mode: this.mode,
			paused: this.paused,
			hp: p.hp,
			maxHp: p.maxHp,
			armor: p.armorPts,
			nitro: p.nitro,
			speed: Math.abs(p.speed) * 8,
			ammo: p.ammo > 900 ? spec.mag : p.ammo,
			mag: spec.mag,
			reserve: p.reserve > 900 ? 99 : p.reserve,
			weapon: spec.label,
			reload: p.reload > 0 ? 1 - p.reload / spec.reload : 0,
			heat: p.heat,
			alive: this.mode === "menu" ? 0 : this.sim.aliveCount(),
			kills: this.sim.kills,
			zoneTime: Math.max(0, Math.ceil(this.sim.zone.timer > 100 ? 0 : this.sim.zone.timer)),
			shrinking: this.sim.zone.shrinking,
			outside: this.sim.outside,
			hurt: p.hurt,
			banner: this.sim.banner,
			bannerOn: this.sim.bannerT > 0,
			scrap: this.sim.save.scrap,
			levels: {
				engine: this.sim.save.engine,
				armor: this.sim.save.armor,
				turret: this.sim.save.turret,
				magnet: this.sim.save.magnet
			},
			wins: this.sim.save.wins,
			bestKills: this.sim.save.bestKills,
			quality: this.sim.save.quality,
			sensitivity: this.sim.save.sensitivity,
			sound: this.sim.save.sound,
			place: this.sim.place(),
			earned: this.earned,
			cause: this.sim.cause,
			win: this.sim.status === "victory",
			note: this.note
		};
		this.onHud(snap);
	}
};
function angleTo(x, z, tx, tz) {
	const dx = tx - x;
	const dz = tz - z;
	const d = Math.hypot(dx, dz) || 1;
	return Math.atan2(-dx / d, -dz / d);
}
var EMPTY = {
	mode: "menu",
	paused: false,
	hp: 100,
	maxHp: 100,
	armor: 0,
	nitro: 100,
	speed: 0,
	ammo: 80,
	mag: 80,
	reserve: 240,
	weapon: "MAKİNELİ",
	reload: 0,
	heat: 0,
	alive: 0,
	kills: 0,
	zoneTime: 28,
	shrinking: false,
	outside: false,
	hurt: 0,
	banner: "",
	bannerOn: false,
	scrap: 0,
	levels: {
		engine: 1,
		armor: 1,
		turret: 1,
		magnet: 1
	},
	wins: 0,
	bestKills: 0,
	quality: "AUTO",
	sensitivity: 1,
	sound: true,
	place: 1,
	earned: 0,
	cause: "",
	win: false,
	note: ""
};
function DerbyGame() {
	const canvasRef = (0, import_react.useRef)(null);
	const rtRef = (0, import_react.useRef)(null);
	const [snap, setSnap] = (0, import_react.useState)(EMPTY);
	const [panel, setPanel] = (0, import_react.useState)("menu");
	const [touchUi, setTouchUi] = (0, import_react.useState)(false);
	const [portrait, setPortrait] = (0, import_react.useState)(false);
	const [diff, setDiff] = (0, import_react.useState)("scrap");
	const [coach, setCoach] = (0, import_react.useState)(false);
	(0, import_react.useEffect)(() => {
		const canvas = canvasRef.current;
		if (!canvas) return;
		const rt = new DerbyRuntime(canvas, setSnap);
		rtRef.current = rt;
		rt.startLoop();
		const onResize = () => {
			rt.resize();
			const coarse = window.matchMedia("(pointer: coarse)").matches;
			setTouchUi(coarse || window.innerWidth < 1100);
			setPortrait(window.innerHeight > window.innerWidth + 80);
		};
		onResize();
		window.addEventListener("resize", onResize);
		const mini = document.getElementById("derby-mini");
		rt.attachMini(mini);
		return () => {
			window.removeEventListener("resize", onResize);
			rt.dispose();
			rtRef.current = null;
		};
	}, []);
	(0, import_react.useEffect)(() => {
		if (snap.mode !== "battle") return;
		setCoach(true);
		const t = window.setTimeout(() => setCoach(false), 6500);
		return () => window.clearTimeout(t);
	}, [snap.mode]);
	(0, import_react.useEffect)(() => {
		const mini = document.getElementById("derby-mini");
		rtRef.current?.attachMini(mini);
	}, [snap.mode]);
	const inFight = snap.mode === "battle";
	const inResult = snap.mode === "result";
	return /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
		className: "safe-pad fixed inset-0 overflow-hidden bg-surface font-body text-fg",
		children: [
			/* @__PURE__ */ (0, import_jsx_runtime.jsx)("canvas", {
				ref: canvasRef,
				className: "absolute inset-0 h-full w-full touch-none",
				onContextMenu: (e) => e.preventDefault(),
				onPointerDown: (e) => {
					if (e.button === 0 || e.button === 2) canvasRef.current && rtRef.current?.pointer(canvasRef.current, e.nativeEvent, true, e.button);
				},
				onPointerUp: (e) => {
					canvasRef.current && rtRef.current?.pointer(canvasRef.current, e.nativeEvent, false, e.button);
				},
				onPointerMove: (e) => {
					canvasRef.current && rtRef.current?.pointerMove(canvasRef.current, e.nativeEvent);
				}
			}),
			snap.hurt > 0 && inFight ? /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
				className: "pointer-events-none absolute inset-0",
				style: { boxShadow: `inset 0 0 80px rgba(211,47,47,${.55 * snap.hurt})` }
			}) : null,
			snap.outside && inFight ? /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
				className: "pointer-events-none absolute inset-0",
				style: { boxShadow: "inset 0 0 90px rgba(211,47,47,0.45)" }
			}) : null,
			snap.mode === "menu" ? /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
				className: "pointer-events-none absolute inset-0 flex items-stretch",
				children: /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("section", {
					className: "pointer-events-auto flex w-full max-w-md flex-col justify-between bg-surface/90 p-6 sm:p-8",
					children: [
						/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { children: [
							/* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
								className: "font-display text-sm tracking-[0.35em] text-sand",
								children: "KOLEZYUM"
							}),
							/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("h1", {
								className: "font-display text-6xl leading-none tracking-wide text-fg sm:text-7xl",
								children: ["DERBY", /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", {
									className: "block text-brand",
									children: "ARENA"
								})]
							}),
							/* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
								className: "mt-3 max-w-xs text-base leading-snug text-muted",
								children: "Daralan arenada son araç kazanır. Hasar çarpışmadan ve silahtan gelir. Ödül hurda, hurda garaja gider."
							}),
							/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("p", {
								className: "mt-4 font-display text-lg text-amber",
								children: ["HURDA ", snap.scrap]
							}),
							/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("p", {
								className: "text-sm text-muted",
								children: [
									"ZAFER ",
									snap.wins,
									" · EN İYİ LEŞ ",
									snap.bestKills
								]
							})
						] }),
						panel === "menu" ? /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("nav", {
							className: "mt-8 flex flex-col gap-2",
							children: [
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
									label: "SAVAŞA GİR",
									icon: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Swords, { className: "size-5" }),
									primary: true,
									onClick: () => setPanel("arena")
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
									label: "GARAJ",
									icon: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Wrench, { className: "size-5" }),
									onClick: () => setPanel("garage")
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
									label: "AYARLAR",
									icon: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Settings, { className: "size-5" }),
									onClick: () => setPanel("settings")
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
									className: "mt-4 text-sm text-muted",
									children: "W gaz · S fren · A sola · D sağa · SPACE nitro · SHIFT drift · F ateş · Q silah · R doldur"
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
									className: "text-sm text-muted",
									children: "Telefonda sol çubuk sürer. Gamepad: çubuk sür, LT nitro, B drift, RT ateş."
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
									className: "text-xs text-muted",
									children: "Hurda ve yükseltmeler bu cihazda kalır."
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
									className: "text-xs tracking-widest text-line",
									children: "© LENSTEDREAL · ALL RIGHTS RESERVED"
								})
							]
						}) : null,
						panel === "arena" ? /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
							className: "mt-6 flex flex-col gap-2",
							children: [
								DIFFS.map((d) => /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("button", {
									type: "button",
									onClick: () => setDiff(d.id),
									className: `border px-3 py-3 text-left ${diff === d.id ? "border-brand bg-brand text-fg" : "border-line bg-surface-2 text-fg"}`,
									children: [/* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", {
										className: "block font-display text-xl tracking-wide",
										children: d.title
									}), /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", {
										className: `block text-sm ${diff === d.id ? "text-fg" : "text-muted"}`,
										children: d.blurb
									})]
								}, d.id)),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
									label: "ARENAYA ÇIK",
									primary: true,
									icon: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Swords, { className: "size-5" }),
									onClick: () => rtRef.current?.startMatch(diff)
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
									label: "GERİ",
									onClick: () => setPanel("menu")
								})
							]
						}) : null,
						panel === "garage" ? /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
							className: "mt-6 flex flex-col gap-2",
							children: [
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Upgrade, {
									title: "MOTOR",
									level: snap.levels.engine,
									note: snap.note,
									onBuy: () => rtRef.current?.buy("engine")
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Upgrade, {
									title: "ZIRH",
									level: snap.levels.armor,
									onBuy: () => rtRef.current?.buy("armor")
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Upgrade, {
									title: "TARET",
									level: snap.levels.turret,
									onBuy: () => rtRef.current?.buy("turret")
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Upgrade, {
									title: "MIKNATIS",
									level: snap.levels.magnet,
									onBuy: () => rtRef.current?.buy("magnet")
								}),
								snap.note ? /* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
									className: "text-sm text-amber",
									children: snap.note
								}) : null,
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
									label: "GERİ",
									onClick: () => setPanel("menu")
								})
							]
						}) : null,
						panel === "settings" ? /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
							className: "mt-6 flex flex-col gap-3",
							children: [
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
									className: "font-display text-sm tracking-widest text-muted",
									children: "KALİTE"
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
									className: "grid grid-cols-2 gap-2",
									children: [
										"AUTO",
										"LOW",
										"MEDIUM",
										"HIGH",
										"ULTRA"
									].map((q) => /* @__PURE__ */ (0, import_jsx_runtime.jsx)("button", {
										type: "button",
										onClick: () => rtRef.current?.setQuality(q),
										className: `border px-2 py-2 font-display tracking-wide ${snap.quality === q ? "border-brand bg-brand text-fg" : "border-line bg-surface-2 text-fg"}`,
										children: q === "AUTO" ? "OTO" : q === "LOW" ? "DÜŞÜK" : q === "MEDIUM" ? "ORTA" : q === "HIGH" ? "YÜKSEK" : "ULTRA"
									}, q))
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("label", {
									className: "text-sm text-muted",
									children: [
										"DİREKSİYON ",
										snap.sensitivity.toFixed(2),
										/* @__PURE__ */ (0, import_jsx_runtime.jsx)("input", {
											className: "mt-2 w-full accent-brand",
											type: "range",
											min: .7,
											max: 1.4,
											step: .05,
											value: snap.sensitivity,
											onChange: (e) => rtRef.current?.setSensitivity(Number(e.target.value))
										})
									]
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
									className: "text-sm leading-snug text-muted",
									children: "Düşük: gölge yok, az robot. Orta: daha kalabalık tribün. Yüksek: gölge. Ultra: en yoğun tribün ve keskin görüntü."
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("button", {
									type: "button",
									onClick: () => rtRef.current?.toggleSound(),
									className: "flex items-center gap-2 border border-line bg-surface-2 px-3 py-3 font-display tracking-wide",
									children: [
										snap.sound ? /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Volume2, { className: "size-5" }) : /* @__PURE__ */ (0, import_jsx_runtime.jsx)(VolumeX, { className: "size-5" }),
										"SES ",
										snap.sound ? "AÇIK" : "KAPALI"
									]
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
									label: "GERİ",
									onClick: () => setPanel("menu")
								})
							]
						}) : null
					]
				})
			}) : null,
			inFight || inResult ? /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
				className: "pointer-events-none absolute inset-0",
				children: [
					/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("header", {
						className: "flex items-start justify-between gap-3 p-3 sm:p-4",
						children: [/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
							className: "w-48 max-w-[46vw]",
							children: [
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Bar, {
									label: "CAN",
									value: snap.hp / snap.maxHp,
									color: "bg-danger"
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Bar, {
									label: "ZIRH",
									value: snap.armor / 100,
									color: "bg-sand"
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Bar, {
									label: "NİTRO",
									value: snap.nitro / 100,
									color: "bg-amber"
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("p", {
									className: "mt-1 font-display text-sm tracking-wide text-fg",
									children: [
										snap.weapon,
										" ",
										snap.ammo,
										"/",
										snap.mag,
										/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("span", {
											className: "text-muted",
											children: [" · ", snap.reserve]
										})
									]
								}),
								snap.reload > 0 ? /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
									className: "mt-1 h-1 bg-surface-3",
									children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
										className: "h-full bg-amber",
										style: { width: `${snap.reload * 100}%` }
									})
								}) : null
							]
						}), /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
							className: "flex flex-col items-end gap-2",
							children: [
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)("canvas", {
									id: "derby-mini",
									width: 160,
									height: 160,
									className: "size-24 border border-line bg-surface"
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("p", {
									className: "font-display text-sm tracking-wide text-fg",
									children: [
										"KALAN ",
										snap.alive,
										" · LEŞ ",
										snap.kills
									]
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("p", {
									className: `font-display text-sm ${snap.shrinking ? "text-danger" : "text-amber"}`,
									children: [
										snap.shrinking ? "DARALIYOR" : "BÖLGE",
										" ",
										snap.zoneTime > 0 ? snap.zoneTime : ""
									]
								})
							]
						})]
					}),
					/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
						className: "absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 text-center",
						children: [snap.bannerOn ? /* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
							className: "font-display text-5xl tracking-widest text-fg drop-shadow-none",
							children: snap.banner
						}) : null, /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", { className: "mx-auto mt-2 size-3 border border-fg/80" })]
					}),
					/* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
						className: `absolute left-1/2 -translate-x-1/2 ${touchUi ? "bottom-36" : "bottom-4"} text-center`,
						children: /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("p", {
							className: "font-display text-2xl tracking-widest text-fg",
							children: [/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Gauge, { className: "mr-2 inline size-5 text-amber" }), Math.round(snap.speed)]
						})
					}),
					touchUi && inFight ? /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
						className: "pointer-events-auto absolute inset-x-0 bottom-0 flex items-end justify-between p-3",
						children: [/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Stick, { onChange: (steer, throttle) => rtRef.current?.setTouch({
							steer,
							throttle
						}) }), /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
							className: "grid grid-cols-2 gap-2",
							children: [
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(HoldBtn, {
									label: "NİTRO",
									onHold: (v) => rtRef.current?.setTouch({ nitro: v }),
									children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Flame, { className: "size-5" })
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(HoldBtn, {
									label: "DRIFT",
									onHold: (v) => rtRef.current?.setTouch({ handbrake: v }),
									children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Gauge, { className: "size-5" })
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(HoldBtn, {
									label: "FREN",
									onHold: (v) => rtRef.current?.setTouch({ brake: v }),
									children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Pause, { className: "size-5" })
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(HoldBtn, {
									label: "ATEŞ",
									hot: true,
									onHold: (v) => rtRef.current?.setTouch({ fire: v }),
									children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Crosshair, { className: "size-6" })
								})
							]
						})]
					}) : null,
					coach && inFight && !snap.paused ? /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
						className: "pointer-events-none absolute bottom-40 left-1/2 w-full max-w-md -translate-x-1/2 border border-line bg-surface/90 p-3 text-center",
						children: [/* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
							className: "font-display text-lg tracking-widest text-amber",
							children: "İLK TUR"
						}), /* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
							className: "text-sm text-fg",
							children: "Gaz ver, düşmana çarp ya da ateş et. Turuncu halkanın içinde kal. Yeşil kutu can, beyaz kutu silah."
						})]
					}) : null,
					portrait && inFight ? /* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
						className: "pointer-events-none absolute top-14 left-1/2 -translate-x-1/2 bg-surface/80 px-2 py-1 text-center font-display text-sm tracking-widest text-sand",
						children: "YATAY DAHA RAHAT"
					}) : null,
					inFight ? /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("button", {
						type: "button",
						className: "pointer-events-auto absolute top-3 left-1/2 flex -translate-x-1/2 items-center gap-1 border border-line bg-surface/80 px-3 py-2 font-display text-sm tracking-widest",
						onClick: () => rtRef.current?.togglePause(),
						children: [/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Pause, { className: "size-4" }), snap.paused ? "DEVAM" : "DUR"]
					}) : null
				]
			}) : null,
			snap.paused && inFight ? /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
				className: "absolute inset-0 grid place-items-center bg-surface/80",
				children: /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
					className: "flex w-72 flex-col gap-2 border border-line bg-surface-2 p-5",
					children: [
						/* @__PURE__ */ (0, import_jsx_runtime.jsx)("h2", {
							className: "font-display text-4xl tracking-wide",
							children: "DURAKLATILDI"
						}),
						/* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
							label: "DEVAM",
							primary: true,
							onClick: () => rtRef.current?.togglePause()
						}),
						/* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
							label: "ÇEKİL",
							onClick: () => rtRef.current?.backToMenu()
						})
					]
				})
			}) : null,
			inResult ? /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
				className: "absolute inset-0 grid place-items-center bg-surface/80 p-4",
				children: /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
					className: "w-full max-w-sm border border-line bg-surface-2 p-6",
					children: [
						/* @__PURE__ */ (0, import_jsx_runtime.jsx)("p", {
							className: "font-display text-sm tracking-[0.3em] text-sand",
							children: snap.win ? "KOLEZYUM SENİN" : snap.cause.toUpperCase() || "ELENDİN"
						}),
						/* @__PURE__ */ (0, import_jsx_runtime.jsx)("h2", {
							className: `font-display text-6xl leading-none ${snap.win ? "text-brand" : "text-danger"}`,
							children: snap.win ? "ZAFER" : "ELENDİN"
						}),
						/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("dl", {
							className: "mt-4 grid grid-cols-3 gap-2 text-center",
							children: [
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Stat, {
									k: "SIRA",
									v: `#${snap.place}`
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Stat, {
									k: "LEŞ",
									v: String(snap.kills)
								}),
								/* @__PURE__ */ (0, import_jsx_runtime.jsx)(Stat, {
									k: "HURDA",
									v: `+${snap.earned}`
								})
							]
						}),
						/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
							className: "mt-5 flex flex-col gap-2",
							children: [/* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
								label: "TEKRAR",
								primary: true,
								icon: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Swords, { className: "size-5" }),
								onClick: () => rtRef.current?.startMatch(diff)
							}), /* @__PURE__ */ (0, import_jsx_runtime.jsx)(MenuBtn, {
								label: "GARAJ",
								icon: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(Shield, { className: "size-5" }),
								onClick: () => {
									rtRef.current?.backToMenu();
									setPanel("garage");
								}
							})]
						})
					]
				})
			}) : null
		]
	});
}
function MenuBtn({ label, onClick, primary, icon }) {
	return /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("button", {
		type: "button",
		onClick,
		className: `flex min-h-11 items-center gap-3 border px-4 py-3 font-display text-xl tracking-widest ${primary ? "border-brand bg-brand text-fg" : "border-line bg-surface-2 text-fg"}`,
		children: [icon, label]
	});
}
function Upgrade({ title, level, onBuy }) {
	const cost = level >= 5 ? 0 : UPGRADE_COST[level - 1];
	return /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("button", {
		type: "button",
		onClick: onBuy,
		className: "flex items-center justify-between border border-line bg-surface-2 px-3 py-3 text-left",
		children: [/* @__PURE__ */ (0, import_jsx_runtime.jsxs)("span", { children: [/* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", {
			className: "block font-display text-lg tracking-wide",
			children: title
		}), /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", {
			className: "flex gap-1",
			children: [
				1,
				2,
				3,
				4,
				5
			].map((n) => /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", { className: `inline-block h-2 w-4 ${n <= level ? "bg-brand" : "bg-surface-3"}` }, n))
		})] }), /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", {
			className: "font-display text-amber",
			children: level >= 5 ? "MAX" : `${cost} HURDA`
		})]
	});
}
function Bar({ label, value, color }) {
	const w = `${Math.max(0, Math.min(1, value)) * 100}%`;
	return /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
		className: "mb-1",
		children: [/* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
			className: "flex justify-between font-display text-xs tracking-widest text-muted",
			children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", { children: label })
		}), /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
			className: "h-2 bg-surface-3",
			children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
				className: `bar-seg h-full ${color}`,
				style: { width: w }
			})
		})]
	});
}
function Stat({ k, v }) {
	return /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", {
		className: "border border-line bg-surface px-2 py-2",
		children: [/* @__PURE__ */ (0, import_jsx_runtime.jsx)("dt", {
			className: "text-xs tracking-widest text-muted",
			children: k
		}), /* @__PURE__ */ (0, import_jsx_runtime.jsx)("dd", {
			className: "font-display text-2xl text-fg",
			children: v
		})]
	});
}
function Stick({ onChange }) {
	const ref = (0, import_react.useRef)(null);
	const [knob, setKnob] = (0, import_react.useState)({
		x: 0,
		y: 0
	});
	const move = (clientX, clientY) => {
		const el = ref.current;
		if (!el) return;
		const r = el.getBoundingClientRect();
		const cx = r.left + r.width / 2;
		const cy = r.top + r.height / 2;
		let dx = clientX - cx;
		let dy = clientY - cy;
		const max = r.width * .38;
		const len = Math.hypot(dx, dy) || 1;
		if (len > max) {
			dx = dx / len * max;
			dy = dy / len * max;
		}
		setKnob({
			x: dx,
			y: dy
		});
		const steer = Math.max(-1, Math.min(1, -dx / max));
		const throttle = Math.max(-1, Math.min(1, -dy / max));
		onChange(Math.abs(steer) < .12 ? 0 : steer, Math.abs(throttle) < .12 ? 0 : throttle);
	};
	return /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", {
		ref,
		className: "stick-zone relative size-28 rounded-full border border-line bg-surface/80",
		onPointerDown: (e) => {
			e.currentTarget.setPointerCapture(e.pointerId);
			move(e.clientX, e.clientY);
		},
		onPointerMove: (e) => {
			if (e.currentTarget.hasPointerCapture(e.pointerId)) move(e.clientX, e.clientY);
		},
		onPointerUp: (e) => {
			if (e.currentTarget.hasPointerCapture(e.pointerId)) e.currentTarget.releasePointerCapture(e.pointerId);
			setKnob({
				x: 0,
				y: 0
			});
			onChange(0, 0);
		},
		children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", {
			className: "absolute top-1/2 left-1/2 size-11 -translate-x-1/2 -translate-y-1/2 border border-sand bg-surface-3",
			style: { transform: `translate(calc(-50% + ${knob.x}px), calc(-50% + ${knob.y}px))` }
		})
	});
}
function HoldBtn({ label, onHold, children, hot }) {
	return /* @__PURE__ */ (0, import_jsx_runtime.jsx)("button", {
		type: "button",
		"aria-label": label,
		className: `stick-zone grid size-14 place-items-center border font-display text-xs tracking-wide ${hot ? "border-brand bg-brand text-fg" : "border-line bg-surface/85 text-fg"}`,
		onPointerDown: (e) => {
			e.currentTarget.setPointerCapture(e.pointerId);
			onHold(true);
		},
		onPointerUp: () => onHold(false),
		onPointerCancel: () => onHold(false),
		children
	});
}
var SplitComponent = DerbyGame;
//#endregion
export { SplitComponent as component };
