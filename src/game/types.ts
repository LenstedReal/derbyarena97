export type WeaponId = "MG" | "SHOTGUN" | "ROCKET" | "EMP" | "CANNON";

export type Quality = "LOW" | "MEDIUM" | "HIGH" | "ULTRA";

export type DiffId = "scrap" | "iron" | "war";

export type UpgradeKey = "engine" | "armor" | "turret" | "magnet";

export type ObstacleKind = "column" | "concrete" | "rubble" | "tires" | "wreck" | "barrel";

export type LootKind = "AMMO" | "HEALTH" | "ARMOR" | "NITRO" | "WEAPON" | "REPAIR";

export type DriveInput = {
  throttle: number;
  steer: number;
  nitro: boolean;
  handbrake: boolean;
  fire: boolean;
  aimYaw: number | null;
  reload: boolean;
  cycle: boolean;
};

export type SaveData = {
  v: 1;
  scrap: number;
  engine: number;
  armor: number;
  turret: number;
  magnet: number;
  wins: number;
  bestKills: number;
  quality: Quality | "AUTO";
  sensitivity: number;
  sound: boolean;
};

export type Obstacle = {
  id: number;
  x: number;
  z: number;
  r: number;
  kind: ObstacleKind;
  hp: number;
  maxHp: number;
  explosive: boolean;
  dead: boolean;
  rot: number;
};

export type LootPad = {
  id: number;
  x: number;
  z: number;
  kind: LootKind;
  weapon: WeaponId | null;
  active: boolean;
  respawn: number;
};

export type SimCar = {
  id: number;
  name: string;
  x: number;
  z: number;
  vx: number;
  vz: number;
  yaw: number;
  speed: number;
  hp: number;
  maxHp: number;
  armorPts: number;
  dead: boolean;
  color: number;
  scale: [number, number, number];
  wheel: number;
  turret: number;
  nitro: number;
  nitroOn: boolean;
  isPlayer: boolean;
  isBoss: boolean;
  weapon: WeaponId;
  owned: WeaponId[];
  ammo: number;
  reserve: number;
  mag: number;
  cooldown: number;
  reload: number;
  heat: number;
  overheated: boolean;
  stun: number;
  stuck: number;
  state: "SEARCH" | "CHASE" | "ATTACK" | "DODGE" | "RETREAT" | "LOOT" | "ZONE";
  stateT: number;
  target: "player" | "enemy" | "loot" | "point";
  targetId: number;
  tx: number;
  tz: number;
  strafe: number;
  hurt: number;
};

export type SimBullet = {
  alive: boolean;
  x: number;
  z: number;
  vx: number;
  vz: number;
  damage: number;
  life: number;
  kind: string;
  owner: number;
  splash: number;
  stun: number;
  radius: number;
};

export type SimEvent =
  | { t: "muzzle"; x: number; z: number; yaw: number; kind: string; player: boolean }
  | { t: "boom"; x: number; z: number; r: number }
  | { t: "hit"; x: number; z: number }
  | { t: "player-hit" }
  | { t: "shield" }
  | { t: "pick" }
  | { t: "boom-soft"; x: number; z: number };

export type Report = {
  status: string;
  time: number;
  hp: number;
  alive: number;
  kills: number;
  zoneR: number;
  fault: string;
  nan: boolean;
};
