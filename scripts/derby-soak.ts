import { DerbySim } from "../src/game/sim.ts";
import { FRESH } from "../src/game/save.ts";

function run(label: string, diff: "scrap" | "iron" | "war", drive: boolean, seconds: number) {
  const sim = new DerbySim({ ...FRESH });
  sim.start(diff);
  let diedAt = -1;
  for (let i = 0; i < 60 * seconds; i++) {
    const t = i / 60;
    sim.fixed(1 / 60, {
      throttle: drive ? 0.8 : 0,
      steer: drive ? Math.sin(t * 0.6) : 0,
      nitro: false,
      handbrake: false,
      fire: drive && i % 10 === 0,
      aimYaw: null,
      reload: false,
      cycle: false,
    });
    if (sim.fault) throw new Error(label + " " + sim.fault);
    if (sim.status !== "play") {
      diedAt = sim.time;
      break;
    }
  }
  const r = sim.report();
  console.log(label, JSON.stringify({ ...r, diedAt, cause: sim.cause }));
  if (!sim.cars.every((c) => Number.isFinite(c.x) && Number.isFinite(c.yaw))) throw new Error(label + " nan");
  return { ...r, diedAt, cause: sim.cause };
}

const idle = run("idle-scrap", "scrap", false, 25);
if (idle.diedAt > 0 && idle.diedAt < 12) {
  console.error("idle player melted", idle.cause);
  process.exit(1);
}
const drive = run("drive-war", "war", true, 40);
if (drive.diedAt > 0 && drive.diedAt < 8) {
  console.error("died too fast", drive.cause);
  process.exit(1);
}
console.log("SOAK_OK");
