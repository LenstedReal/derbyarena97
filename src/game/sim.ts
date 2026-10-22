import {
  BARRIER,
  CAR_R,
  DIFFS,
  PHASES,
  PLAY,
  UPGRADE_COST,
  WEAPONS,
  clamp,
  forward,
  makeLoot,
  makeObstacles,
  right,
  wrapAngle,
  yawToward,
} from "./constants";
import type { SaveData } from "./types";
import type {
  DiffId,
  DriveInput,
  LootPad,
  Obstacle,
  Report,
  SimBullet,
  SimCar,
  SimEvent,
  UpgradeKey,
  WeaponId,
} from "./types";

const ROSTER: { name: string; weapon: WeaponId; hp: number; color: number; scale: [number, number, number]; armor: number }[] = [
  { name: "CRUSHER", weapon: "MG", hp: 110, color: 0x6e655c, scale: [1.08, 1, 1.05], armor: 0 },
  { name: "VIPER", weapon: "MG", hp: 88, color: 0xc45c26, scale: [0.92, 0.9, 1.02], armor: 0 },
  { name: "TITAN", weapon: "SHOTGUN", hp: 150, color: 0x8d6e63, scale: [1.28, 1.12, 1.18], armor: 36 },
  { name: "GHOST", weapon: "EMP", hp: 76, color: 0xb5a47a, scale: [0.86, 0.78, 1.08], armor: 0 },
  { name: "REAPER", weapon: "ROCKET", hp: 102, color: 0x7a2e24, scale: [1.05, 1, 1.12], armor: 10 },
  { name: "CHAOS", weapon: "MG", hp: 94, color: 0xc9a227, scale: [1, 0.95, 1], armor: 0 },
  { name: "DREAD", weapon: "CANNON", hp: 124, color: 0x4a3b32, scale: [1.12, 1.05, 1.2], armor: 18 },
];

const EMPTY: DriveInput = {
  throttle: 0,
  steer: 0,
  nitro: false,
  handbrake: false,
  fire: false,
  aimYaw: null,
  reload: false,
  cycle: false,
};

function finite(n: number) {
  return Number.isFinite(n);
}

export class DerbySim {
  save: SaveData;
  obstacles: Obstacle[] = [];
  loot: LootPad[] = [];
  cars: SimCar[] = [];
  bullets: SimBullet[] = [];
  events: SimEvent[] = [];
  player!: SimCar;
  status: "menu" | "play" | "victory" | "dead" = "menu";
  time = 0;
  grace = 0;
  countdown = 0;
  kills = 0;
  scrapRun = 0;
  cause = "";
  committed = false;
  fault = "";
  diff: DiffId = "scrap";
  zone = { x: 0, z: 0, r: PLAY, targetR: PLAY, tx: 0, tz: 0, phase: 0, timer: 28, shrinking: false, dps: 4, final: false };
  private shrinkFrom = PLAY;
  private shrinkFromX = 0;
  private shrinkFromZ = 0;
  private shrinkDur = 1;
  banner = "";
  bannerT = 0;
  hitPulse = 0;
  hitStop = 0;
  outside = false;
  private pairCd = new Map<string, number>();
  private rng = Math.random;

  constructor(save: SaveData) {
    this.save = save;
    this.obstacles = makeObstacles();
    this.loot = makeLoot(this.obstacles);
    this.player = this.makePlayer();
    this.cars = [this.player];
  }

  private makePlayer(): SimCar {
    return this.blankCar(0, "SEN", true, 0x1a2230, [1, 1, 1.05], 100, 0, "MG");
  }

  private blankCar(
    id: number,
    name: string,
    isPlayer: boolean,
    color: number,
    scale: [number, number, number],
    hp: number,
    armor: number,
    weapon: WeaponId,
  ): SimCar {
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
      hurt: 0,
    };
  }

  start(diffId: DiffId) {
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
    this.bannerT = 0.7;
    this.hitPulse = 0;
    this.hitStop = 0;
    this.outside = false;
    this.zone = {
      x: 0,
      z: 0,
      r: PLAY,
      targetR: PLAY,
      tx: 0,
      tz: 0,
      phase: 0,
      timer: PHASES[0].wait * diff.zone,
      shrinking: false,
      dps: 4,
      final: false,
    };

    const hpBonus = 1 + (this.save.armor - 1) * 0.04;
    this.player = this.blankCar(0, "SEN", true, 0x1a2230, [1, 1, 1.05], 100 * hpBonus, 0, "MG");
    this.player.maxHp = 100 * hpBonus;
    this.player.hp = this.player.maxHp;
    const spawn = this.findClear(0, 13.5, 3.2);
    this.player.x = spawn.x;
    this.player.z = spawn.z;
    this.player.yaw = yawToward(spawn.x, spawn.z, 0, 0);

    const enemies: SimCar[] = [];
    const total = diff.count;
    for (let i = 0; i < total; i++) {
      const boss = diff.boss && i === 0;
      const proto = boss
        ? { name: "WAR RIG", weapon: "CANNON" as WeaponId, hp: 340, color: 0x8a1010, scale: [1.45, 1.18, 1.72] as [number, number, number], armor: 80 }
        : ROSTER[i % ROSTER.length];
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
      car.stateT = 2.6 + (i % 5) * 0.18;
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

  private findClear(x: number, z: number, extra: number) {
    let best = { x, z };
    let bestD = -1;
    for (let ring = 0; ring < 8; ring++) {
      const rad = ring * 1.4;
      const steps = ring === 0 ? 1 : 10;
      for (let s = 0; s < steps; s++) {
        const a = (s / steps) * Math.PI * 2;
        const px = x + Math.sin(a) * rad;
        const pz = z + Math.cos(a) * rad;
        if (Math.hypot(px, pz) > PLAY - 4) continue;
        let gap = PLAY;
        for (const o of this.obstacles) {
          gap = Math.min(gap, Math.hypot(o.x - px, o.z - pz) - o.r);
        }
        if (gap > bestD) {
          bestD = gap;
          best = { x: px, z: pz };
        }
        if (gap > extra) return { x: px, z: pz };
      }
    }
    return best;
  }

  private placeEnemy(i: number, total: number, existing: SimCar[]) {
    for (let attempt = 0; attempt < 24; attempt++) {
      const a = (i / total) * Math.PI * 2 + attempt * 0.31 + 0.4;
      const rad = 26 + (i % 3) * 1.4 - attempt * 0.15;
      const x = Math.sin(a) * rad;
      const z = Math.cos(a) * rad;
      if (Math.hypot(x, z) > PLAY - 3.5) continue;
      if (Math.hypot(x - this.player.x, z - this.player.z) < 11) continue;
      if (existing.some((e) => Math.hypot(e.x - x, e.z - z) < 4.2)) continue;
      if (this.obstacles.some((o) => Math.hypot(o.x - x, o.z - z) < o.r + 2.6)) continue;
      return { x, z };
    }
    const a = (i / total) * Math.PI * 2 + Math.PI;
    return this.findClear(Math.sin(a) * 20, Math.cos(a) * 20, 2.2);
  }

  buy(key: UpgradeKey): string | null {
    const level = this.save[key];
    if (level >= 5) return "MAKSİMUM";
    const cost = UPGRADE_COST[level - 1];
    if (this.save.scrap < cost) return "HURDA YETMEZ";
    this.save.scrap -= cost;
    this.save[key] = level + 1;
    return null;
  }

  fixed(dt: number, input: DriveInput) {
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
    else if (this.countdown > 0.2) this.setBanner("1");
    else if (this.countdown > -0.55) this.setBanner("SAVAŞ");

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

  private setBanner(text: string) {
    if (this.banner === text && this.bannerT > 0.05) return;
    if (this.banner === text) return;
    this.banner = text;
    this.bannerT = text === "SAVAŞ" ? 0.7 : 0.95;
  }

  private stepZone(dt: number) {
    const z = this.zone;
    const diff = DIFFS.find((d) => d.id === this.diff)!;
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
      const maxDrift = Math.max(0, (z.r - p.radius) * 0.55);
      const a = this.rng() * Math.PI * 2;
      const d = this.rng() * maxDrift;
      let tx = z.x + Math.cos(a) * d;
      let tz = z.z + Math.sin(a) * d;
      const lim = Math.max(0, PLAY - p.radius - 1.5);
      const md = Math.hypot(tx, tz);
      if (md > lim && md > 0.001) {
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

  private reduction() {
    return clamp(0.08 * this.save.armor, 0, 0.42);
  }

  private engineAccel() {
    return 11.5 * (1 + (this.save.engine - 1) * 0.1);
  }

  private engineMax() {
    return 16 * (1 + (this.save.engine - 1) * 0.08);
  }

  private turretMul() {
    return 1 + (this.save.turret - 1) * 0.12;
  }

  private magnetR() {
    return 2.35 + (this.save.magnet - 1) * 0.72;
  }

  private stepPlayer(dt: number, input: DriveInput) {
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
    const drive: DriveInput = p.stun > 0 ? { ...input, throttle: input.throttle * 0.35, steer: input.steer * 0.45, nitro: false } : input;
    if (p.stun > 0) p.stun = Math.max(0, p.stun - dt);
    this.integrate(p, drive, dt, {
      accel: this.engineAccel(),
      max: this.engineMax(),
      grip: 12,
      turn: 2.35 * this.save.sensitivity,
      nitro: true,
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

  private autoAim(p: SimCar) {
    const spec = WEAPONS[p.weapon];
    const f = forward(p.yaw);
    let best = 0;
    let bestYaw = p.yaw;
    for (const e of this.cars) {
      if (e.dead || e === p) continue;
      const dx = e.x - p.x;
      const dz = e.z - p.z;
      const dist = Math.hypot(dx, dz);
      if (dist > spec.range || dist < 0.001) continue;
      const dot = (dx / dist) * f.x + (dz / dist) * f.z;
      if (dot < 0.42) continue;
      const score = dot * 2 + (1 - dist / spec.range);
      if (score > best) {
        best = score;
        bestYaw = yawToward(p.x, p.z, e.x, e.z);
      }
    }
    return bestYaw;
  }

  private integrate(
    body: SimCar,
    input: DriveInput,
    dt: number,
    opts: { accel: number; max: number; grip: number; turn: number; nitro: boolean },
  ) {
    if (opts.nitro && input.nitro && body.nitro > 0 && input.throttle >= 0) {
      body.nitroOn = true;
      body.nitro = Math.max(0, body.nitro - 24 * dt);
    } else {
      body.nitroOn = false;
      body.nitro = Math.min(100, body.nitro + 14 * dt);
    }
    const speedFactor = clamp(Math.abs(body.speed) / 5.2, 0.35, 1);
    const reverse = body.speed < -0.45 ? -1 : 1;
    const turn = opts.turn * (input.handbrake ? 1.55 : 1);
    body.yaw = wrapAngle(body.yaw + input.steer * turn * speedFactor * reverse * dt);

    const max = opts.max * (body.nitroOn ? 1.55 : 1);
    const accel = opts.accel * (body.nitroOn ? 2.25 : 1);
    if (input.throttle > 0.05) {
      body.speed = Math.min(max, body.speed + input.throttle * accel * dt);
    } else if (input.throttle < -0.05) {
      if (body.speed > 0.7) body.speed += input.throttle * accel * 2.5 * dt;
      else body.speed = Math.max(-max * 0.55, body.speed + input.throttle * accel * 0.85 * dt);
    } else {
      const drag = input.handbrake ? 1.7 : 0.72;
      body.speed *= Math.exp(-drag * dt);
      if (Math.abs(body.speed) < 0.12) body.speed = 0;
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

  private keepInside(body: SimCar, dt: number, hurts: boolean) {
    const dist = Math.hypot(body.x, body.z);
    if (dist <= PLAY || dist < 0.001) return;
    const nx = body.x / dist;
    const nz = body.z / dist;
    body.x = nx * PLAY;
    body.z = nz * PLAY;
    const out = body.vx * nx + body.vz * nz;
    if (out > 0) {
      body.vx -= nx * out * 1.55;
      body.vz -= nz * out * 1.55;
      body.speed *= -0.32;
      if (hurts && out > 9) this.impactDamage(body, (out - 9) * 0.85, `bar:${body.id}`, dt);
    }
  }

  private impactDamage(body: SimCar, amount: number, key: string, _dt: number) {
    if (this.pairCd.has(key)) return;
    this.pairCd.set(key, 0.42);
    this.applyDamage(body, amount, body.isPlayer ? "bariyer" : "");
  }

  private applyDamage(body: SimCar, amount: number, cause: string) {
    if (body.dead || amount <= 0) return;
    if (body.isPlayer && this.grace > 0) return;
    let d = amount;
    if (body.isPlayer) d *= 1 - this.reduction();
    else d *= body.isBoss ? 0.82 : 1;
    if (body.armorPts > 0) {
      const absorb = Math.min(body.armorPts, d * 0.72);
      body.armorPts -= absorb;
      d -= absorb;
    }
    body.hp -= d;
    if (body.isPlayer) {
      this.hitPulse = 1;
      this.player.hurt = 1;
      if (cause) this.cause = cause;
      if (cause !== "bölge" && amount >= 6) {
        this.hitStop = 0.055;
        this.events.push({ t: "player-hit" });
      }
    }
    if (body.hp <= 0) this.kill(body, cause || "darbe");
  }

  private kill(body: SimCar, cause: string) {
    if (body.dead) return;
    body.dead = true;
    body.hp = 0;
    body.nitroOn = false;
    this.events.push({ t: "boom-soft", x: body.x, z: body.z });
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
    if (this.rng() < 0.35) {
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

  private collideCars(body: SimCar, dt: number) {
    for (const o of this.cars) {
      if (o === body || (o.dead && body.dead)) continue;
      const dx = body.x - o.x;
      const dz = body.z - o.z;
      const d = Math.hypot(dx, dz) || 0.0001;
      const min = CAR_R * (body.scale[0] + o.scale[0]) * 0.72;
      if (d >= min) continue;
      const nx = dx / d;
      const nz = dz / d;
      const push = (min - d) * 0.5;
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
          body.vx -= nx * bounce * 0.85;
          body.vz -= nz * bounce * 0.85;
        }
        if (!o.dead) {
          o.vx += nx * bounce * 0.85;
          o.vz += nz * bounce * 0.85;
        }
        const impact = -rel;
        if (impact > 8 && !body.dead && !o.dead) {
          const key = body.id < o.id ? `c:${body.id}:${o.id}` : `c:${o.id}:${body.id}`;
          if (!this.pairCd.has(key)) {
            this.pairCd.set(key, 0.38);
            const dmg = (impact - 8) * 1.35;
            this.applyDamage(body, dmg * (o.isBoss ? 1.25 : 1), "çarpışma");
            this.applyDamage(o, dmg * (body.isBoss ? 1.25 : 1), "çarpışma");
            this.events.push({ t: "hit", x: (body.x + o.x) / 2, z: (body.z + o.z) / 2 });
          }
        }
      }
    }
    void dt;
  }

  private collideObstacles(body: SimCar, _dt: number, hurts: boolean) {
    if (body.dead) return;
    const rad = CAR_R * body.scale[0];
    for (const o of this.obstacles) {
      if (o.dead) continue;
      const dx = body.x - o.x;
      const dz = body.z - o.z;
      const d = Math.hypot(dx, dz) || 0.0001;
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
        body.speed *= 0.82;
        const impact = -into;
        if (hurts && impact > 8) this.impactDamage(body, (impact - 8) * (o.explosive ? 0.4 : 1.1), `o:${body.id}:${o.id}`, 0);
        if (o.explosive && (impact > 7 || o.hp <= 0)) this.explodeBarrel(o);
        else if (!o.explosive) {
          o.hp -= impact * 0.8;
          if (o.hp <= 0) o.dead = true;
        }
      }
    }
    for (const o of this.cars) {
      if (!o.dead || o === body) continue;
      const dx = body.x - o.x;
      const dz = body.z - o.z;
      const d = Math.hypot(dx, dz) || 0.0001;
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
        body.speed *= 0.9;
      }
    }
  }

  private explodeBarrel(o: Obstacle) {
    if (o.dead) return;
    o.dead = true;
    o.hp = 0;
    this.events.push({ t: "boom", x: o.x, z: o.z, r: 6.4 });
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

  private stepEnemies(dt: number) {
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
        this.integrate(e, { ...EMPTY, throttle: 0.1 }, dt, { accel: 8, max: 10, grip: 10, turn: 1.6, nitro: false });
        this.keepInside(e, dt, false);
        continue;
      }
      e.stateT -= dt;
      if (e.stateT <= 0) this.chooseState(e);
      const nav = this.navigate(e);
      let throttle = nav.throttle;
      let steer = nav.steer;
      const moved = Math.hypot(e.vx, e.vz);
      if (throttle > 0.25 && moved < 0.7) e.stuck += dt;
      else e.stuck = Math.max(0, e.stuck - dt * 0.5);
      if (e.stuck > 0.6) {
        throttle = -0.85;
        if (e.stuck > 1) {
          e.stuck = 0;
          e.yaw = wrapAngle(e.yaw + 1.35 * e.strafe);
        }
      }
      this.integrate(e, { ...EMPTY, throttle, steer, handbrake: e.state === "DODGE" }, dt, {
        accel: e.isBoss ? 9 : 10,
        max: e.isBoss ? 13.5 : 14.2,
        grip: 11,
        turn: e.isBoss ? 1.55 : 2.05,
        nitro: false,
      });
      this.keepInside(e, dt, false);
      this.collideObstacles(e, dt, false);
      const tgt = this.resolveTarget(e);
      if (tgt && (e.state === "ATTACK" || e.state === "CHASE" || e.state === "DODGE")) {
        const dist = Math.hypot(tgt.x - e.x, tgt.z - e.z);
        const aim = yawToward(e.x, e.z, tgt.x, tgt.z);
        const diff = wrapAngle(aim - e.turret);
        e.turret += clamp(diff, -2.4 * dt, 2.4 * dt);
        const spec = WEAPONS[e.weapon];
        const blocked = this.segmentBlocked(e.x, e.z, tgt.x, tgt.z, e.id);
        const playerTarget = tgt === this.player;
        const allow = !(playerTarget && this.grace > 0);
        if (allow && !blocked && dist < spec.range * 0.92 && Math.abs(diff) < 0.16 && e.cooldown <= 0) {
          const jitter = (this.rng() - 0.5) * 0.08;
          this.tryFire(e, e.turret + jitter, false, e.isBoss ? 1.15 : 0.85);
          e.cooldown = spec.rate * (e.isBoss ? 2.1 : 3.1) + this.rng() * 0.35;
        }
      } else {
        e.turret += wrapAngle(e.yaw - e.turret) * Math.min(1, dt * 3);
      }
      e.wheel += e.speed * dt * 2.2;
    }
    this.separateLiving();
  }

  private separateLiving() {
    const live = this.cars.filter((c) => !c.dead);
    for (let i = 0; i < live.length; i++) {
      for (let j = i + 1; j < live.length; j++) {
        const a = live[i];
        const b = live[j];
        let dx = b.x - a.x;
        let dz = b.z - a.z;
        let d = Math.hypot(dx, dz);
        const min = 2.5;
        if (d < 0.001) {
          dx = 0.2;
          dz = 0.1;
          d = Math.hypot(dx, dz);
        }
        if (d < min) {
          const push = (min - d) * 0.35;
          const nx = dx / d;
          const nz = dz / d;
          a.x -= nx * push;
          a.z -= nz * push;
          b.x += nx * push;
          b.z += nz * push;
        }
      }
    }
  }

  private chooseState(e: SimCar) {
    const hpF = e.hp / e.maxHp;
    if (!this.insideZone(e.x, e.z, 1.6)) {
      e.state = "ZONE";
      e.stateT = 1.6;
      return;
    }
    if (hpF < 0.3) {
      const heal = this.nearestLoot(e, (l) => l.kind === "HEALTH" || l.kind === "REPAIR" || l.kind === "ARMOR");
      if (heal && Math.hypot(heal.x - e.x, heal.z - e.z) < 18) {
        e.state = "LOOT";
        e.target = "loot";
        e.targetId = heal.id;
        e.stateT = 3.2;
        return;
      }
      if (this.rng() < 0.55) {
        e.state = "RETREAT";
        e.stateT = 1.6;
        return;
      }
    }
    let best = -1;
    let kind: SimCar["target"] = "player";
    let id = -1;
    if (!this.player.dead) {
      const d = Math.hypot(this.player.x - e.x, this.player.z - e.z);
      best = 18 / (d + 4) + 0.55 + (1 - this.player.hp / this.player.maxHp) * 0.4;
      kind = "player";
    }
    for (const o of this.cars) {
      if (o === e || o.dead || o.isPlayer) continue;
      const d = Math.hypot(o.x - e.x, o.z - e.z);
      const score = 18 / (d + 4) + (1 - o.hp / o.maxHp) * 0.7 + (o.isBoss ? -0.25 : 0) + this.rng() * 0.25;
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
      if (gun && Math.hypot(gun.x - e.x, gun.z - e.z) < 12 && this.rng() < 0.45) {
        e.state = "LOOT";
        e.target = "loot";
        e.targetId = gun.id;
        e.stateT = 2.8;
        return;
      }
    }
    e.state = "CHASE";
    e.strafe = this.rng() < 0.5 ? -1 : 1;
    e.stateT = 1.8 + this.rng() * 1.6;
  }

  private navigate(e: SimCar) {
    let tx = e.tx;
    let tz = e.tz;
    let throttle = 0.75;
    let strafe = 0;
    const tgt = this.resolveTarget(e);
    if (e.state === "CHASE" || e.state === "ATTACK" || e.state === "DODGE") {
      if (!tgt) e.stateT = 0;
      else {
        tx = tgt.x;
        tz = tgt.z;
        const d = Math.hypot(tx - e.x, tz - e.z);
        const range = WEAPONS[e.weapon].range;
        if (e.state === "ATTACK" || (e.state === "CHASE" && d < range * 0.62)) {
          e.state = "ATTACK";
          strafe = e.strafe;
          throttle = d < range * 0.34 ? 0.45 : 0.8;
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
        throttle = 0.9;
      }
    } else if (e.state === "ZONE") {
      tx = this.zone.x;
      tz = this.zone.z;
      throttle = 1;
    } else if (Math.hypot(e.tx - e.x, e.tz - e.z) < 3) {
      e.stateT = 0;
    }
    let desired = yawToward(e.x, e.z, tx, tz) + strafe * 0.95 + this.avoidYaw(e);
    const diff = wrapAngle(desired - e.yaw);
    let steer = clamp(diff / 0.7, -1, 1);
    if (Math.abs(diff) > 2.1 && Math.abs(e.speed) < 2) throttle = -0.55;
    else throttle *= 1 - Math.abs(steer) * 0.28;
    return { steer, throttle };
  }

  private resolveTarget(e: SimCar): SimCar | null {
    if (e.target === "player") return this.player.dead ? null : this.player;
    if (e.target === "enemy") return this.cars.find((c) => c.id === e.targetId && !c.dead) ?? null;
    return null;
  }

  private randomPoint(e: SimCar) {
    const a = this.rng() * Math.PI * 2;
    const r = this.rng() * this.zone.r * 0.75;
    e.tx = this.zone.x + Math.cos(a) * r;
    e.tz = this.zone.z + Math.sin(a) * r;
    e.target = "point";
  }

  private nearestLoot(e: SimCar, pred: (l: LootPad) => boolean) {
    let best: LootPad | null = null;
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

  private avoidYaw(e: SimCar) {
    const f = forward(e.yaw);
    const rgt = right(e.yaw);
    let push = 0;
    const look = (x: number, z: number, rad: number, weight: number) => {
      const dx = x - e.x;
      const dz = z - e.z;
      const dist = Math.hypot(dx, dz) - rad;
      if (dist > 7 || dist < -1) return;
      const ahead = dx * f.x + dz * f.z;
      if (ahead < -0.4) return;
      const side = dx * rgt.x + dz * rgt.z;
      const strength = clamp(1 - dist / 7, 0, 1) * weight;
      push += (side >= 0 ? -1 : 1) * strength;
    };
    for (const o of this.obstacles) {
      if (!o.dead) look(o.x, o.z, o.r, 1);
    }
    for (const c of this.cars) {
      if (c !== e && !c.dead) look(c.x, c.z, CAR_R, 0.85);
    }
    const centerD = Math.hypot(e.x, e.z);
    if (centerD > PLAY - 5) {
      const inX = -e.x / (centerD || 1);
      const inZ = -e.z / (centerD || 1);
      const side = inX * rgt.x + inZ * rgt.z;
      push += clamp(side, -1, 1) * 1.2;
    }
    return clamp(push, -1.35, 1.35);
  }

  private segmentBlocked(x: number, z: number, tx: number, tz: number, ignoreId: number) {
    const dx = tx - x;
    const dz = tz - z;
    const len = Math.hypot(dx, dz);
    if (len < 0.001) return false;
    const steps = Math.ceil(len / 1.4);
    for (let i = 1; i < steps; i++) {
      const t = i / steps;
      const px = x + dx * t;
      const pz = z + dz * t;
      for (const o of this.obstacles) {
        if (o.dead || o.kind === "tires" || o.kind === "barrel") continue;
        if (Math.hypot(o.x - px, o.z - pz) < o.r * 0.82) return true;
      }
      for (const c of this.cars) {
        if (c.dead || c.id === ignoreId) continue;
        if (Math.hypot(c.x - px, c.z - pz) < CAR_R * 0.7) return true;
      }
    }
    return false;
  }

  private tickWeapon(c: SimCar, dt: number, ai: boolean) {
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
    c.heat = Math.max(0, c.heat - 0.72 * dt);
    if (c.overheated && c.heat < 0.35) c.overheated = false;
  }

  private startReload(c: SimCar) {
    if (c.reload > 0) return;
    const spec = WEAPONS[c.weapon];
    if (c.ammo >= spec.mag) return;
    if (c.reserve <= 0 && c.reserve < 9000) return;
    c.reload = spec.reload;
  }

  private cycleWeapon(c: SimCar) {
    if (c.owned.length < 2 || c.reload > 0) return;
    const i = c.owned.indexOf(c.weapon);
    const next = c.owned[(i + 1) % c.owned.length];
    this.equip(c, next, false);
  }

  private equip(c: SimCar, id: WeaponId, refill: boolean) {
    const spec = WEAPONS[id];
    c.weapon = id;
    c.mag = spec.mag;
    if (refill) {
      c.ammo = spec.mag;
      c.reserve = Math.max(c.reserve, spec.mag * 2);
    } else if (c.ammo > spec.mag) c.ammo = spec.mag;
    c.reload = 0;
    c.cooldown = 0.15;
  }

  private tryFire(c: SimCar, yaw: number, player: boolean, mul: number) {
    const spec = WEAPONS[c.weapon];
    if (c.cooldown > 0 || c.reload > 0 || c.overheated) return;
    if (!player && c.ammo > 900) {
      /* AI ignores magazines */
    } else if (c.ammo <= 0) {
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
    this.events.push({ t: "muzzle", x: muzzleX, z: muzzleZ, yaw, kind: spec.kind, player });
    const pellets = spec.pellets;
    for (let i = 0; i < pellets; i++) {
      const spread = (this.rng() - 0.5) * spec.spread * 2;
      const dir = forward(yaw + spread);
      this.spawnBullet({
        x: muzzleX,
        z: muzzleZ,
        vx: dir.x * spec.speed + c.vx * 0.25,
        vz: dir.z * spec.speed + c.vz * 0.25,
        damage: spec.damage * mul,
        life: spec.range / spec.speed,
        kind: spec.kind,
        owner: c.id,
        splash: spec.splash,
        stun: spec.stun,
        radius: spec.kind === "rocket" ? 0.7 : spec.kind === "shell" ? 0.45 : 0.28,
      });
    }
  }

  private spawnBullet(b: Omit<SimBullet, "alive">) {
    let slot = this.bullets.find((x) => !x.alive);
    if (!slot) {
      if (this.bullets.length >= 96) slot = this.bullets[0];
      else {
        slot = { ...b, alive: true };
        this.bullets.push(slot);
        return;
      }
    }
    Object.assign(slot, b, { alive: true });
  }

  private stepBullets(dt: number) {
    for (const b of this.bullets) {
      if (!b.alive) continue;
      b.life -= dt;
      b.x += b.vx * dt;
      b.z += b.vz * dt;
      if (b.life <= 0 || Math.hypot(b.x, b.z) > BARRIER) {
        if (Math.hypot(b.x, b.z) > BARRIER) this.events.push({ t: "shield" });
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
      if (!hit) {
        for (const c of this.cars) {
          if (c.dead || c.id === b.owner) continue;
          const rad = CAR_R * c.scale[0] * 0.85 + b.radius;
          if (Math.hypot(c.x - b.x, c.z - b.z) < rad) {
            hit = true;
            if (b.splash > 0) this.splash(b);
            else this.applyDamage(c, b.damage, b.owner === 0 ? "mermi" : "mermi");
            if (b.stun > 0) c.stun = Math.max(c.stun, b.stun * (c.isPlayer ? 0.75 : 1));
            if (b.owner === 0 && !c.isPlayer) this.events.push({ t: "hit", x: b.x, z: b.z });
            break;
          }
        }
      }
      if (hit) {
        if (b.kind === "rocket" || b.kind === "shell" || b.kind === "emp") {
          this.events.push({ t: "boom", x: b.x, z: b.z, r: Math.max(1.4, b.splash) });
        } else this.events.push({ t: "hit", x: b.x, z: b.z });
        b.alive = false;
      }
    }
  }

  private splash(b: SimBullet) {
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

  private stepLoot(dt: number) {
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

  private takeLoot(c: SimCar, l: LootPad) {
    if (l.kind === "HEALTH") c.hp = Math.min(c.maxHp, c.hp + 36);
    else if (l.kind === "REPAIR") c.hp = Math.min(c.maxHp, c.hp + Math.max(28, (c.maxHp - c.hp) * 0.45));
    else if (l.kind === "ARMOR") c.armorPts = Math.min(100, c.armorPts + 40);
    else if (l.kind === "NITRO") c.nitro = Math.min(100, c.nitro + 55);
    else if (l.kind === "AMMO") {
      const spec = WEAPONS[c.weapon];
      if (c.reserve < 9000) c.reserve += spec.mag * 2;
      if (c.ammo === 0) this.startReload(c);
    } else if (l.kind === "WEAPON" && l.weapon) {
      if (!c.owned.includes(l.weapon)) c.owned.push(l.weapon);
      this.equip(c, l.weapon, true);
    }
    if (c.isPlayer) {
      this.scrapRun += 6;
      this.events.push({ t: "pick" });
      this.banner = l.kind === "WEAPON" && l.weapon ? WEAPONS[l.weapon].label : l.kind === "HEALTH" ? "+CAN" : l.kind === "ARMOR" ? "+ZIRH" : l.kind === "NITRO" ? "+NİTRO" : l.kind === "AMMO" ? "+MERMİ" : "+TAMİR";
      this.bannerT = 0.8;
    }
  }

  private insideZone(x: number, z: number, margin = 0) {
    return Math.hypot(x - this.zone.x, z - this.zone.z) <= this.zone.r - margin;
  }

  private zoneDamage(dt: number) {
    for (const c of this.cars) {
      if (c.dead) continue;
      const d = Math.hypot(c.x - this.zone.x, c.z - this.zone.z);
      const outside = d - this.zone.r;
      if (c.isPlayer) this.outside = outside > 0;
      if (outside <= 0) continue;
      if (c.isPlayer && this.grace > 0) continue;
      const dmg = (this.zone.dps + outside * 0.22) * dt;
      this.applyDamage(c, dmg, "bölge");
    }
  }

  private finishCheck() {
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

  private checkFinite() {
    for (const c of this.cars) {
      if (!finite(c.x) || !finite(c.z) || !finite(c.yaw) || !finite(c.hp) || !finite(c.speed)) {
        this.fault = `NaN ${c.name}`;
        c.x = 0;
        c.z = 0;
        c.vx = 0;
        c.vz = 0;
        c.speed = 0;
        c.yaw = 0;
      }
    }
  }

  report(): Report {
    return {
      status: this.status,
      time: Number(this.time.toFixed(1)),
      hp: Number(this.player.hp.toFixed(1)),
      alive: this.aliveCount(),
      kills: this.kills,
      zoneR: Number(this.zone.r.toFixed(2)),
      fault: this.fault,
      nan: Boolean(this.fault),
    };
  }
}
