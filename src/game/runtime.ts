import * as THREE from "three";
import { Sfx } from "./audio";
import { PLAY, WEAPONS, forward, makeLoot, makeObstacles, right } from "./constants";
import { ArenaView, type DrawBullet, type DrawCar, type FrameState } from "./render";
import { loadSave, resolveQuality, writeSave } from "./save";
import { DerbySim } from "./sim";
import type { DiffId, DriveInput, Quality, UpgradeKey } from "./types";

export type HudSnap = {
  mode: "menu" | "battle" | "result";
  paused: boolean;
  hp: number;
  maxHp: number;
  armor: number;
  nitro: number;
  speed: number;
  ammo: number;
  mag: number;
  reserve: number;
  weapon: string;
  reload: number;
  heat: number;
  alive: number;
  kills: number;
  zoneTime: number;
  shrinking: boolean;
  outside: boolean;
  hurt: number;
  banner: string;
  bannerOn: boolean;
  scrap: number;
  levels: { engine: number; armor: number; turret: number; magnet: number };
  wins: number;
  bestKills: number;
  quality: Quality | "AUTO";
  sensitivity: number;
  sound: boolean;
  place: number;
  earned: number;
  cause: string;
  win: boolean;
  note: string;
};

declare global {
  interface Window {
    __controlsTest?: {
      getYaw: () => number;
      getSpeed: () => number;
      setKeys: (codes: string[]) => void;
      setSteer?: (v: number) => void;
    };
    __derby?: DerbyRuntime;
  }
}

export class DerbyRuntime {
  readonly sim: DerbySim;
  readonly view: ArenaView;
  readonly sfx = new Sfx();
  private held = new Set<string>();
  private scripted: Set<string> | null = null;
  private touch = { throttle: 0, steer: 0, nitro: false, handbrake: false, brake: false, fire: false };
  private aimPoint: { x: number; z: number } | null = null;
  private aimMode = false;
  private firePointer = false;
  private cycleLatch = false;
  private reloadLatch = false;
  private paused = false;
  private mode: "menu" | "battle" | "result" = "menu";
  private raf = 0;
  private last = 0;
  private acc = 0;
  private hudAt = 0;
  private shield = 0;
  private earned = 0;
  private note = "";
  private mini: HTMLCanvasElement | null = null;
  private onHud: (s: HudSnap) => void;
  private ray = new THREE.Raycaster();
  private ndc = new THREE.Vector2();
  private usingMouse = false;
  private padWasPause = false;
  private dust = 0;
  private plane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
  private hit = new THREE.Vector3();
  private smooth = { x: 14, y: 7, z: 16 };
  private smoothLook = { x: 0, y: 1, z: 0 };

  constructor(canvas: HTMLCanvasElement, onHud: (s: HudSnap) => void) {
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
      },
    };
    window.__derby = this;
  }

  startLoop() {
    this.last = performance.now();
    this.emit(true);
    const loop = (now: number) => {
      const dt = Math.min(0.05, (now - this.last) / 1000);
      this.last = now;
      if (this.mode === "battle" && !this.paused) {
        this.acc += dt;
        const base = this.readInput();
        let guard = 0;
        let first = true;
        while (this.acc >= 1 / 60 && guard < 5) {
          const input = first ? base : { ...base, cycle: false, reload: false };
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
      } else {
        this.sfx.engine(this.mode === "menu" ? 1.6 : 0, false);
      }
      this.shield = Math.max(0, this.shield - dt * 0.8);
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

  attachMini(canvas: HTMLCanvasElement | null) {
    this.mini = canvas;
  }

  pointer(canvas: HTMLCanvasElement, ev: PointerEvent, down: boolean, button: number) {
    if (this.mode !== "battle") return;
    if (window.matchMedia("(pointer: coarse)").matches) return;
    const rect = canvas.getBoundingClientRect();
    this.ndc.x = ((ev.clientX - rect.left) / rect.width) * 2 - 1;
    this.ndc.y = -((ev.clientY - rect.top) / rect.height) * 2 + 1;
    this.usingMouse = true;
    if (button === 2) this.aimMode = down;
    if (button === 0) this.firePointer = down;
    this.aimFromCamera();
  }

  pointerMove(canvas: HTMLCanvasElement, ev: PointerEvent) {
    if (this.mode !== "battle" || !this.usingMouse) return;
    if (window.matchMedia("(pointer: coarse)").matches) return;
    const rect = canvas.getBoundingClientRect();
    this.ndc.x = ((ev.clientX - rect.left) / rect.width) * 2 - 1;
    this.ndc.y = -((ev.clientY - rect.top) / rect.height) * 2 + 1;
    this.aimFromCamera();
  }

  setTouch(partial: Partial<typeof this.touch>) {
    Object.assign(this.touch, partial);
  }

  startMatch(diff: DiffId) {
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
    this.smooth.y = 2.5;
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

  buy(key: UpgradeKey) {
    this.note = this.sim.buy(key) ?? "";
    writeSave(this.sim.save);
    this.emit(true);
  }

  setQuality(q: Quality | "AUTO") {
    this.sim.save.quality = q;
    this.view.setQuality(resolveQuality(q));
    writeSave(this.sim.save);
    this.emit(true);
  }

  setSensitivity(v: number) {
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

  private onKeyDown = (ev: KeyboardEvent) => {
    if (ev.code === "Space" || ev.code.startsWith("Arrow")) ev.preventDefault();
    this.held.add(ev.code);
    if (ev.code === "KeyQ") this.cycleLatch = true;
    if (ev.code === "KeyR") this.reloadLatch = true;
    if (ev.code === "Escape" || ev.code === "KeyP") this.togglePause();
  };

  private onKeyUp = (ev: KeyboardEvent) => {
    this.held.delete(ev.code);
  };

  private onBlur = () => {
    this.held.clear();
    this.firePointer = false;
    this.aimMode = false;
  };

  private bindKeys() {
    window.addEventListener("keydown", this.onKeyDown);
    window.addEventListener("keyup", this.onKeyUp);
    window.addEventListener("blur", this.onBlur);
  }

  private keys() {
    return this.scripted ?? this.held;
  }

  private readInput(): DriveInput {
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
      if (Math.abs(ax) > 0.16) steer -= ax;
      if (Math.abs(ay) > 0.16) throttle -= ay;
    }
    throttle = Math.max(-1, Math.min(1, throttle + this.touch.throttle + (this.touch.brake ? -1 : 0)));
    steer = Math.max(-1, Math.min(1, steer + this.touch.steer));
    const aimYaw =
      this.aimPoint && this.usingMouse
        ? angleTo(this.sim.player.x, this.sim.player.z, this.aimPoint.x, this.aimPoint.z)
        : null;
    const input: DriveInput = {
      throttle,
      steer,
      nitro: keys.has("Space") || this.touch.nitro || Boolean(pad && (pad.buttons[6]?.pressed || pad.buttons[5]?.pressed)),
      handbrake: keys.has("ShiftLeft") || keys.has("ShiftRight") || this.touch.handbrake || Boolean(pad?.buttons[1]?.pressed),
      fire: this.firePointer || this.touch.fire || keys.has("KeyF") || Boolean(pad && (pad.buttons[7]?.pressed || pad.buttons[0]?.pressed)),
      aimYaw,
      reload: this.reloadLatch,
      cycle: this.cycleLatch,
    };
    this.reloadLatch = false;
    this.cycleLatch = false;
    const start = Boolean(pad?.buttons[9]?.pressed);
    if (start && !this.padWasPause) this.togglePause();
    this.padWasPause = start;
    return input;
  }

  private gamepad() {
    const pads = navigator.getGamepads?.();
    if (!pads) return null;
    for (const pad of pads) {
      if (pad && pad.connected) return pad;
    }
    return null;
  }

  private kickDust(dt: number) {
    const p = this.sim.player;
    if (p.dead) return;
    this.dust -= dt;
    const sp = Math.abs(p.speed);
    if (sp < 7 || this.dust > 0) return;
    this.dust = 0.09;
    const f = forward(p.yaw);
    this.view.burst(p.x - f.x * 1.7, p.z - f.z * 1.7, p.nitroOn ? 3 : 2, 0xc4a574, p.nitroOn ? 2.2 : 1.1, 0.55);
  }

  private aimFromCamera() {
    this.ray.setFromCamera(this.ndc, this.view.camera);
    const ok = this.ray.ray.intersectPlane(this.plane, this.hit);
    if (ok) this.aimPoint = { x: this.hit.x, z: this.hit.z };
  }

  private flushEvents() {
    for (const e of this.sim.events) {
      if (e.t === "muzzle") {
        this.sfx.shot(e.kind);
        this.view.burst(e.x, e.z, e.player ? 4 : 2, 0xffe08a, 3, 1);
      } else if (e.t === "boom") {
        this.sfx.boom();
        this.view.burst(e.x, e.z, 16, 0xd84315, 7, 4);
      } else if (e.t === "boom-soft") {
        this.sfx.boom();
        this.view.burst(e.x, e.z, 10, 0xff8f00, 4, 2);
      } else if (e.t === "hit") {
        this.sfx.hit();
        this.view.burst(e.x, e.z, 4, 0xfff1d0, 2, 1);
      } else if (e.t === "player-hit") {
        this.sfx.crunch();
        this.sfx.buzz(26);
      } else if (e.t === "shield") {
        this.shield = 1;
      } else if (e.t === "pick") {
        this.sfx.pick();
      }
    }
    this.sim.events.length = 0;
  }

  private frame(dt: number): FrameState {
    const t = performance.now() / 1000;
    if (this.mode !== "battle") {
      const ang = t * 0.18;
      const dist = 11;
      return {
        mode: "showcase",
        time: t,
        cars: [
          {
            x: 0,
            z: 0,
            yaw: Math.sin(t * 0.4) * 0.6,
            vx: 0,
            vz: 0,
            speed: 0,
            turret: Math.sin(t * 0.4) * 0.6,
            wheel: t * 4,
            color: 0x1a2230,
            scale: [1, 1, 1],
            dead: false,
            nitro: false,
            player: true,
          },
        ],
        bullets: [],
        loot: this.sim.loot.map((l) => ({ x: l.x, z: l.z, kind: l.kind, active: true })),
        obstacles: this.sim.obstacles,
        zone: { x: 0, z: 0, r: PLAY * 0.92, shrinking: false },
        shield: 0.25 + Math.sin(t * 0.7) * 0.1,
        cam: {
          x: Math.sin(ang) * dist,
          y: 4.2,
          z: Math.cos(ang) * dist,
          lx: 0,
          ly: 0.8,
          lz: 0,
          fov: 52,
        },
        quality: resolveQuality(this.sim.save.quality),
      };
    }
    const p = this.sim.player;
    const f = forward(p.yaw);
    const r = right(p.yaw);
    const aim = this.aimMode;
    const dist = aim ? 6.4 : 8.2;
    const height = aim ? 2.6 : 3.15;
    const shoulder = aim ? 1.5 : 0.35;
    let cx = p.x - f.x * dist + r.x * shoulder;
    let cz = p.z - f.z * dist + r.z * shoulder;
    const cy = Math.max(1.6, height + (p.nitroOn ? 0.3 : 0));
    const cd = Math.hypot(cx, cz);
    if (cd > 34) {
      cx *= 34 / cd;
      cz *= 34 / cd;
    }
    const shake = this.sim.hitPulse * 0.28;
    const sx = (Math.random() - 0.5) * shake;
    const sz = (Math.random() - 0.5) * shake;
    const look = 5.2 + Math.abs(p.speed) * 0.1;
    const k = 1 - Math.exp(-4.4 * Math.max(dt, 0.016));
    this.smooth.x += (cx + sx - this.smooth.x) * k;
    this.smooth.y += (cy - this.smooth.y) * k;
    this.smooth.z += (cz + sz - this.smooth.z) * k;
    const lx = p.x + f.x * look;
    const lz = p.z + f.z * look;
    this.smoothLook.x += (lx - this.smoothLook.x) * k;
    this.smoothLook.y += (0.9 - this.smoothLook.y) * k;
    this.smoothLook.z += (lz - this.smoothLook.z) * k;
    const fov = 66 + Math.min(10, Math.abs(p.speed) * 0.4) + (p.nitroOn ? 5 : 0);
    const cars: DrawCar[] = this.sim.cars.map((c) => ({
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
      player: c.isPlayer,
    }));
    const bullets: DrawBullet[] = this.sim.bullets
      .filter((b) => b.alive)
      .map((b) => ({
        x: b.x,
        z: b.z,
        yaw: Math.atan2(-b.vx, -b.vz),
        kind: b.kind,
        alive: true,
      }));
    return {
      mode: "battle",
      time: this.sim.time,
      cars,
      bullets,
      loot: this.sim.loot.map((l) => ({ x: l.x, z: l.z, kind: l.kind, active: l.active })),
      obstacles: this.sim.obstacles,
      zone: { x: this.sim.zone.x, z: this.sim.zone.z, r: this.sim.zone.r, shrinking: this.sim.zone.shrinking },
      shield: this.shield,
      cam: {
        x: this.smooth.x,
        y: this.smooth.y,
        z: this.smooth.z,
        lx: this.smoothLook.x,
        ly: this.smoothLook.y,
        lz: this.smoothLook.z,
        fov,
      },
      quality: resolveQuality(this.sim.save.quality),
    };
  }

  private drawMini() {
    const canvas = this.mini;
    if (!canvas || this.mode === "menu") return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    const w = canvas.width;
    const h = canvas.height;
    ctx.clearRect(0, 0, w, h);
    ctx.fillStyle = "rgba(18,18,18,0.72)";
    ctx.fillRect(0, 0, w, h);
    const map = (x: number, z: number) => ({
      px: w / 2 + (x / (PLAY + 4)) * (w * 0.46),
      py: h / 2 + (z / (PLAY + 4)) * (h * 0.46),
    });
    ctx.strokeStyle = "#c4a574";
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.arc(w / 2, h / 2, w * 0.42, 0, Math.PI * 2);
    ctx.stroke();
    const zc = map(this.sim.zone.x, this.sim.zone.z);
    ctx.strokeStyle = this.sim.zone.shrinking ? "#d32f2f" : "#ff8f00";
    ctx.beginPath();
    ctx.arc(zc.px, zc.py, (this.sim.zone.r / (PLAY + 4)) * (w * 0.46), 0, Math.PI * 2);
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

  private emit(force: boolean) {
    const now = performance.now();
    if (!force && now - this.hudAt < 120) return;
    this.hudAt = now;
    const p = this.sim.player;
    const spec = WEAPONS[p.weapon];
    const snap: HudSnap = {
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
        magnet: this.sim.save.magnet,
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
      note: this.note,
    };
    this.onHud(snap);
  }
}

function angleTo(x: number, z: number, tx: number, tz: number) {
  const dx = tx - x;
  const dz = tz - z;
  const d = Math.hypot(dx, dz) || 1;
  return Math.atan2(-dx / d, -dz / d);
}
