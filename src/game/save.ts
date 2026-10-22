import type { Quality, SaveData } from "./types";

const KEY = "derby-arena-save-v1";

export const FRESH: SaveData = {
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
  sound: true,
};

export function loadSave(): SaveData {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return { ...FRESH };
    const parsed = JSON.parse(raw) as Partial<SaveData>;
    if (parsed.v !== 1) return { ...FRESH };
    return { ...FRESH, ...parsed, v: 1 };
  } catch {
    return { ...FRESH };
  }
}

export function writeSave(data: SaveData) {
  try {
    localStorage.setItem(KEY, JSON.stringify(data));
  } catch {
    /* private mode */
  }
}

export function resolveQuality(q: Quality | "AUTO"): Quality {
  if (q !== "AUTO") return q;
  if (typeof navigator === "undefined") return "MEDIUM";
  const cores = navigator.hardwareConcurrency || 4;
  const coarse = window.matchMedia?.("(pointer: coarse)")?.matches ?? false;
  if (coarse && cores <= 6) return "MEDIUM";
  if (cores <= 4) return "LOW";
  return "HIGH";
}
