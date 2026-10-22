import type { DiffId, LootKind, Obstacle, ObstacleKind, WeaponId } from "./types";

export const PLAY = 36;
export const BARRIER = 37.2;
export const CAR_R = 1.42;
export const MAX_CARS = 16;

export type WeaponSpec = {
  id: WeaponId;
  label: string;
  damage: number;
  /** seconds between shots */
  rate: number;
  range: number;
  spread: number;
  mag: number;
  reload: number;
  heat: number;
  speed: number;
  pellets: number;
  splash: number;
  stun: number;
  kind: string;
};

export const WEAPONS: Record<WeaponId, WeaponSpec> = {
  MG: {
    id: "MG",
    label: "MAKİNELİ",
    damage: 9,
    rate: 0.1,
    range: 26,
    spread: 0.04,
    mag: 80,
    reload: 1.85,
    heat: 0.045,
    speed: 48,
    pellets: 1,
    splash: 0,
    stun: 0,
    kind: "tracer",
  },
  SHOTGUN: {
    id: "SHOTGUN",
    label: "SAÇMA",
    damage: 11,
    rate: 0.7,
    range: 12.5,
    spread: 0.2,
    mag: 8,
    reload: 2.5,
    heat: 0.12,
    speed: 40,
    pellets: 6,
    splash: 0,
    stun: 0,
    kind: "pellet",
  },
  ROCKET: {
    id: "ROCKET",
    label: "ROKET",
    damage: 58,
    rate: 1.25,
    range: 33,
    spread: 0.02,
    mag: 4,
    reload: 3,
    heat: 0.2,
    speed: 22,
    pellets: 1,
    splash: 5.4,
    stun: 0,
    kind: "rocket",
  },
  EMP: {
    id: "EMP",
    label: "EMP",
    damage: 14,
    rate: 1.5,
    range: 21,
    spread: 0.03,
    mag: 3,
    reload: 3.2,
    heat: 0.22,
    speed: 28,
    pellets: 1,
    splash: 4.6,
    stun: 0.85,
    kind: "emp",
  },
  CANNON: {
    id: "CANNON",
    label: "TOP",
    damage: 46,
    rate: 0.82,
    range: 36,
    spread: 0.012,
    mag: 6,
    reload: 2.8,
    heat: 0.18,
    speed: 58,
    pellets: 1,
    splash: 1.8,
    stun: 0,
    kind: "shell",
  },
};

export const ORDER: WeaponId[] = ["MG", "SHOTGUN", "ROCKET", "EMP", "CANNON"];

export type Diff = {
  id: DiffId;
  title: string;
  blurb: string;
  count: number;
  hp: number;
  zone: number;
  boss: boolean;
};

export const DIFFS: Diff[] = [
  {
    id: "scrap",
    title: "HURDA ÇUKURU",
    blurb: "9 sürücü. Bölge yavaş kapanır. Isınmak için.",
    count: 9,
    hp: 1,
    zone: 1,
    boss: false,
  },
  {
    id: "iron",
    title: "DEMİR HALKA",
    blurb: "12 sürücü. Kuşatma daha erken başlar.",
    count: 12,
    hp: 1.12,
    zone: 0.82,
    boss: false,
  },
  {
    id: "war",
    title: "SAVAŞ RİGİ",
    blurb: "War Rig ve 13 silahlı araç. Son daire acımasız.",
    count: 14,
    hp: 1.22,
    zone: 0.74,
    boss: true,
  },
];

export const PHASES: { wait: number; shrink: number; radius: number; dps: number }[] = [
  { wait: 28, shrink: 22, radius: 26.7, dps: 4 },
  { wait: 20, shrink: 20, radius: 18.5, dps: 6 },
  { wait: 16, shrink: 16, radius: 11.8, dps: 9 },
  { wait: 12, shrink: 14, radius: 6.7, dps: 14 },
  { wait: 10, shrink: 12, radius: 3.6, dps: 22 },
];

export const UPGRADE_COST = [50, 90, 150, 240, 360];

type RawObs = { x: number; z: number; r: number; kind: ObstacleKind; hp: number; explosive?: boolean };

const RAW: RawObs[] = [
  { x: 0, z: -1.5, r: 2.15, kind: "wreck", hp: 520 },
  { x: -10, z: -16.5, r: 1.65, kind: "column", hp: 900 },
  { x: -3.4, z: -18.2, r: 1.65, kind: "column", hp: 900 },
  { x: 3.6, z: -18, r: 1.65, kind: "column", hp: 900 },
  { x: 10.2, z: -16.2, r: 1.65, kind: "column", hp: 900 },
  { x: 16.4, z: -4.2, r: 1.35, kind: "concrete", hp: 460 },
  { x: 18.6, z: 1.6, r: 1.35, kind: "concrete", hp: 460 },
  { x: 16.2, z: 7.4, r: 1.35, kind: "concrete", hp: 460 },
  { x: 12.4, z: -10.5, r: 1.35, kind: "concrete", hp: 420 },
  { x: -16.2, z: -0.4, r: 0.95, kind: "tires", hp: 160 },
  { x: -18.4, z: 4.6, r: 0.95, kind: "tires", hp: 160 },
  { x: -14.2, z: 5.8, r: 0.95, kind: "tires", hp: 160 },
  { x: -17.5, z: -6.2, r: 0.95, kind: "tires", hp: 160 },
  { x: -8.2, z: 15.6, r: 1.55, kind: "rubble", hp: 280 },
  { x: 8.4, z: 15.4, r: 1.55, kind: "rubble", hp: 280 },
  { x: -14.5, z: 11.2, r: 1.5, kind: "rubble", hp: 260 },
  { x: 14.2, z: 11.6, r: 1.5, kind: "rubble", hp: 260 },
  { x: 11.5, z: 16.8, r: 2, kind: "wreck", hp: 380 },
  { x: -12.4, z: -8.2, r: 2.05, kind: "wreck", hp: 400 },
  { x: 9.2, z: -8.6, r: 1.9, kind: "wreck", hp: 340 },
  { x: -22, z: -12, r: 1.9, kind: "wreck", hp: 360 },
  { x: 22.5, z: 12.5, r: 1.85, kind: "wreck", hp: 340 },
  { x: 12.2, z: -14.6, r: 0.62, kind: "barrel", hp: 28, explosive: true },
  { x: -11.4, z: 8.4, r: 0.62, kind: "barrel", hp: 28, explosive: true },
  { x: 20.4, z: -8.8, r: 0.62, kind: "barrel", hp: 28, explosive: true },
  { x: -5.5, z: 19.2, r: 0.62, kind: "barrel", hp: 28, explosive: true },
  { x: 0.4, z: -22.4, r: 0.62, kind: "barrel", hp: 28, explosive: true },
  { x: 24, z: -16, r: 1.45, kind: "rubble", hp: 240 },
  { x: -24.5, z: 14.5, r: 1.4, kind: "concrete", hp: 400 },
];

function hypot(x: number, z: number) {
  return Math.hypot(x, z);
}

/** Push overlaps apart and keep every prop inside the driveable sand. */
export function makeObstacles(): Obstacle[] {
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
    rot: (id * 1.7) % Math.PI,
  }));
  for (let iter = 0; iter < 14; iter++) {
    for (let i = 0; i < items.length; i++) {
      for (let j = i + 1; j < items.length; j++) {
        const a = items[i];
        const b = items[j];
        let dx = b.x - a.x;
        let dz = b.z - a.z;
        let d = hypot(dx, dz);
        const min = a.r + b.r + 1.15;
        if (d < 0.0001) {
          dx = 1;
          dz = 0.2;
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
      const max = PLAY - a.r - 1.35;
      if (d > max && d > 0.001) {
        a.x *= max / d;
        a.z *= max / d;
      }
    }
  }
  return items;
}

const LOOT_RAW: { x: number; z: number; kind: LootKind; weapon?: WeaponId }[] = [
  { x: 6.2, z: 5.4, kind: "HEALTH" },
  { x: -6.4, z: 4.8, kind: "AMMO" },
  { x: 5.6, z: -6.2, kind: "NITRO" },
  { x: -5.2, z: -6.6, kind: "ARMOR" },
  { x: 0.2, z: -12.4, kind: "WEAPON", weapon: "SHOTGUN" },
  { x: 15.5, z: 3.2, kind: "REPAIR" },
  { x: -15.2, z: -3.4, kind: "AMMO" },
  { x: 9.5, z: 18.2, kind: "WEAPON", weapon: "ROCKET" },
  { x: -9.2, z: 18.6, kind: "HEALTH" },
  { x: 21, z: -4.5, kind: "NITRO" },
  { x: -21.2, z: 6.5, kind: "WEAPON", weapon: "EMP" },
  { x: 2.2, z: 9.4, kind: "WEAPON", weapon: "CANNON" },
];

export function makeLoot(obstacles: Obstacle[]) {
  return LOOT_RAW.map((spot, id) => {
    let x = spot.x;
    let z = spot.z;
    for (let n = 0; n < 8; n++) {
      const hit = obstacles.find((o) => hypot(o.x - x, o.z - z) < o.r + 1.8);
      if (!hit) break;
      const dx = x - hit.x || 1;
      const dz = z - hit.z || 0.3;
      const d = hypot(dx, dz) || 1;
      x += (dx / d) * 0.8;
      z += (dz / d) * 0.8;
    }
    const d = hypot(x, z);
    const max = PLAY - 3;
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
      respawn: 0,
    };
  });
}

export function forward(yaw: number) {
  return { x: -Math.sin(yaw), z: -Math.cos(yaw) };
}

export function right(yaw: number) {
  return { x: Math.cos(yaw), z: -Math.sin(yaw) };
}

export function yawToward(x: number, z: number, tx: number, tz: number) {
  const dx = tx - x;
  const dz = tz - z;
  const d = Math.hypot(dx, dz) || 1;
  return Math.atan2(-dx / d, -dz / d);
}

export function wrapAngle(a: number) {
  let w = a;
  while (w > Math.PI) w -= Math.PI * 2;
  while (w < -Math.PI) w += Math.PI * 2;
  return w;
}

export function clamp(v: number, a: number, b: number) {
  return Math.max(a, Math.min(b, v));
}
