import { useEffect, useRef, useState, type ReactNode } from "react";
import { Crosshair, Flame, Gauge, Pause, Settings, Shield, Swords, Volume2, VolumeX, Wrench } from "lucide-react";
import { DIFFS, UPGRADE_COST } from "@/game/constants";
import { DerbyRuntime, type HudSnap } from "@/game/runtime";
import type { DiffId, UpgradeKey } from "@/game/types";

const EMPTY: HudSnap = {
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
  levels: { engine: 1, armor: 1, turret: 1, magnet: 1 },
  wins: 0,
  bestKills: 0,
  quality: "AUTO",
  sensitivity: 1,
  sound: true,
  place: 1,
  earned: 0,
  cause: "",
  win: false,
  note: "",
};

type Panel = "menu" | "garage" | "arena" | "settings";

export function DerbyGame() {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const rtRef = useRef<DerbyRuntime | null>(null);
  const [snap, setSnap] = useState<HudSnap>(EMPTY);
  const [panel, setPanel] = useState<Panel>("menu");
  const [touchUi, setTouchUi] = useState(false);
  const [portrait, setPortrait] = useState(false);
  const [diff, setDiff] = useState<DiffId>("scrap");
  const [coach, setCoach] = useState(false);

  useEffect(() => {
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
    const mini = document.getElementById("derby-mini") as HTMLCanvasElement | null;
    rt.attachMini(mini);
    return () => {
      window.removeEventListener("resize", onResize);
      rt.dispose();
      rtRef.current = null;
    };
  }, []);

  useEffect(() => {
    if (snap.mode !== "battle") return;
    setCoach(true);
    const t = window.setTimeout(() => setCoach(false), 6500);
    return () => window.clearTimeout(t);
  }, [snap.mode]);

  useEffect(() => {
    const mini = document.getElementById("derby-mini") as HTMLCanvasElement | null;
    rtRef.current?.attachMini(mini);
  }, [snap.mode]);

  const inFight = snap.mode === "battle";
  const inResult = snap.mode === "result";

  return (
    <div className="safe-pad fixed inset-0 overflow-hidden bg-surface font-body text-fg">
      <canvas
        ref={canvasRef}
        className="absolute inset-0 h-full w-full touch-none"
        onContextMenu={(e) => e.preventDefault()}
        onPointerDown={(e) => {
          if (e.button === 0 || e.button === 2) canvasRef.current && rtRef.current?.pointer(canvasRef.current, e.nativeEvent, true, e.button);
        }}
        onPointerUp={(e) => {
          canvasRef.current && rtRef.current?.pointer(canvasRef.current, e.nativeEvent, false, e.button);
        }}
        onPointerMove={(e) => {
          canvasRef.current && rtRef.current?.pointerMove(canvasRef.current, e.nativeEvent);
        }}
      />

      {snap.hurt > 0 && inFight ? (
        <div
          className="pointer-events-none absolute inset-0"
          style={{ boxShadow: `inset 0 0 80px rgba(211,47,47,${0.55 * snap.hurt})` }}
        />
      ) : null}
      {snap.outside && inFight ? (
        <div className="pointer-events-none absolute inset-0" style={{ boxShadow: "inset 0 0 90px rgba(211,47,47,0.45)" }} />
      ) : null}

      {snap.mode === "menu" ? (
        <div className="pointer-events-none absolute inset-0 flex items-stretch">
          <section className="pointer-events-auto flex w-full max-w-md flex-col justify-between bg-surface/90 p-6 sm:p-8">
            <div>
              <p className="font-display text-sm tracking-[0.35em] text-sand">KOLEZYUM</p>
              <h1 className="font-display text-6xl leading-none tracking-wide text-fg sm:text-7xl">
                DERBY
                <span className="block text-brand">ARENA</span>
              </h1>
              <p className="mt-3 max-w-xs text-base leading-snug text-muted">
                Kumtaşı kolezyum. Arabayı sür, çatıdaki namluyla vur, son kalan kazanır.
              </p>
              <p className="mt-4 font-display text-lg text-amber">HURDA {snap.scrap}</p>
              <p className="text-sm text-muted">
                ZAFER {snap.wins} · EN İYİ LEŞ {snap.bestKills}
              </p>
            </div>

            {panel === "menu" ? (
              <nav className="mt-8 flex flex-col gap-2">
                <MenuBtn label="SAVAŞA GİR" icon={<Swords className="size-5" />} primary onClick={() => setPanel("arena")} />
                <MenuBtn label="GARAJ" icon={<Wrench className="size-5" />} onClick={() => setPanel("garage")} />
                <MenuBtn label="AYARLAR" icon={<Settings className="size-5" />} onClick={() => setPanel("settings")} />
                <p className="mt-4 text-sm text-muted">
                  W gaz · S fren · A sola · D sağa · SPACE nitro · F ateş · Q silah · R doldur
                </p>
                <p className="text-sm text-muted">Telefonda sol çubuk sürer. Gamepad: çubuk sür, LT nitro, B drift, RT ateş.</p>
                <p className="text-xs text-muted">Hurda ve yükseltmeler bu cihazda kalır.</p>
                <p className="text-xs tracking-widest text-line">© LENSTEDREAL · ALL RIGHTS RESERVED</p>
              </nav>
            ) : null}

            {panel === "arena" ? (
              <div className="mt-6 flex flex-col gap-2">
                {DIFFS.map((d) => (
                  <button
                    key={d.id}
                    type="button"
                    onClick={() => setDiff(d.id)}
                    className={`border px-3 py-3 text-left ${diff === d.id ? "border-brand bg-brand text-fg" : "border-line bg-surface-2 text-fg"}`}
                  >
                    <span className="block font-display text-xl tracking-wide">{d.title}</span>
                    <span className={`block text-sm ${diff === d.id ? "text-fg" : "text-muted"}`}>{d.blurb}</span>
                  </button>
                ))}
                <MenuBtn
                  label="ARENAYA ÇIK"
                  primary
                  icon={<Swords className="size-5" />}
                  onClick={() => rtRef.current?.startMatch(diff)}
                />
                <MenuBtn label="GERİ" onClick={() => setPanel("menu")} />
              </div>
            ) : null}

            {panel === "garage" ? (
              <div className="mt-6 flex flex-col gap-2">
                <Upgrade
                  title="MOTOR"
                  level={snap.levels.engine}
                  note={snap.note}
                  onBuy={() => rtRef.current?.buy("engine" as UpgradeKey)}
                />
                <Upgrade title="ZIRH" level={snap.levels.armor} onBuy={() => rtRef.current?.buy("armor")} />
                <Upgrade title="TARET" level={snap.levels.turret} onBuy={() => rtRef.current?.buy("turret")} />
                <Upgrade title="MIKNATIS" level={snap.levels.magnet} onBuy={() => rtRef.current?.buy("magnet")} />
                {snap.note ? <p className="text-sm text-amber">{snap.note}</p> : null}
                <MenuBtn label="GERİ" onClick={() => setPanel("menu")} />
              </div>
            ) : null}

            {panel === "settings" ? (
              <div className="mt-6 flex flex-col gap-3">
                <p className="font-display text-sm tracking-widest text-muted">KALİTE</p>
                <div className="grid grid-cols-2 gap-2">
                  {(["AUTO", "LOW", "MEDIUM", "HIGH", "ULTRA"] as const).map((q) => (
                    <button
                      key={q}
                      type="button"
                      onClick={() => rtRef.current?.setQuality(q)}
                      className={`border px-2 py-2 font-display tracking-wide ${snap.quality === q ? "border-brand bg-brand text-fg" : "border-line bg-surface-2 text-fg"}`}
                    >
                      {q === "AUTO" ? "OTO" : q === "LOW" ? "DÜŞÜK" : q === "MEDIUM" ? "ORTA" : q === "HIGH" ? "YÜKSEK" : "ULTRA"}
                    </button>
                  ))}
                </div>
                <label className="text-sm text-muted">
                  DİREKSİYON {snap.sensitivity.toFixed(2)}
                  <input
                    className="mt-2 w-full accent-brand"
                    type="range"
                    min={0.7}
                    max={1.4}
                    step={0.05}
                    value={snap.sensitivity}
                    onChange={(e) => rtRef.current?.setSensitivity(Number(e.target.value))}
                  />
                </label>
                <p className="text-sm leading-snug text-muted">
                  Düşük: gölge yok, az robot. Orta: daha kalabalık tribün. Yüksek: gölge. Ultra: en yoğun tribün ve keskin görüntü.
                </p>
                <button
                  type="button"
                  onClick={() => rtRef.current?.toggleSound()}
                  className="flex items-center gap-2 border border-line bg-surface-2 px-3 py-3 font-display tracking-wide"
                >
                  {snap.sound ? <Volume2 className="size-5" /> : <VolumeX className="size-5" />}
                  SES {snap.sound ? "AÇIK" : "KAPALI"}
                </button>
                <MenuBtn label="GERİ" onClick={() => setPanel("menu")} />
              </div>
            ) : null}
          </section>
        </div>
      ) : null}

      {inFight || inResult ? (
        <div className="pointer-events-none absolute inset-0">
          <header className="flex items-start justify-between gap-3 p-3 sm:p-4">
            <div className="w-48 max-w-[46vw]">
              <Bar label="CAN" value={snap.hp / snap.maxHp} color="bg-danger" />
              <Bar label="ZIRH" value={snap.armor / 100} color="bg-sand" />
              <Bar label="NİTRO" value={snap.nitro / 100} color="bg-amber" />
              <p className="mt-1 font-display text-sm tracking-wide text-fg">
                {snap.weapon} {snap.ammo}/{snap.mag}
                <span className="text-muted"> · {snap.reserve}</span>
              </p>
              {snap.reload > 0 ? <div className="mt-1 h-1 bg-surface-3"><div className="h-full bg-amber" style={{ width: `${snap.reload * 100}%` }} /></div> : null}
            </div>
            <div className="flex flex-col items-end gap-2">
              <canvas id="derby-mini" width={160} height={160} className="size-24 border border-line bg-surface" />
              <p className="font-display text-sm tracking-wide text-fg">
                KALAN {snap.alive} · LEŞ {snap.kills}
              </p>
              <p className={`font-display text-sm ${snap.shrinking ? "text-danger" : "text-amber"}`}>
                {snap.shrinking ? "DARALIYOR" : "BÖLGE"} {snap.zoneTime > 0 ? snap.zoneTime : ""}
              </p>
            </div>
          </header>

          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 text-center">
            {snap.bannerOn ? <p className="font-display text-5xl tracking-widest text-fg drop-shadow-none">{snap.banner}</p> : null}
            <div className="mx-auto mt-2 size-3 border border-fg/80" />
          </div>

          <div className={`absolute left-1/2 -translate-x-1/2 ${touchUi ? "bottom-36" : "bottom-4"} text-center`}>
            <p className="font-display text-2xl tracking-widest text-fg">
              <Gauge className="mr-2 inline size-5 text-amber" />
              {Math.round(snap.speed)}
            </p>
          </div>

          {touchUi && inFight ? (
            <div className="pointer-events-auto absolute inset-x-0 bottom-0 flex items-end justify-between p-3">
              <Stick
                onChange={(steer, throttle) => rtRef.current?.setTouch({ steer, throttle })}
              />
              <div className="grid grid-cols-2 gap-2">
                <HoldBtn label="NİTRO" onHold={(v) => rtRef.current?.setTouch({ nitro: v })}>
                  <Flame className="size-5" />
                </HoldBtn>
                <HoldBtn label="DRIFT" onHold={(v) => rtRef.current?.setTouch({ handbrake: v })}>
                  <Gauge className="size-5" />
                </HoldBtn>
                <HoldBtn label="FREN" onHold={(v) => rtRef.current?.setTouch({ brake: v })}>
                  <Pause className="size-5" />
                </HoldBtn>
                <HoldBtn label="ATEŞ" hot onHold={(v) => rtRef.current?.setTouch({ fire: v })}>
                  <Crosshair className="size-6" />
                </HoldBtn>
              </div>
            </div>
          ) : null}

          {coach && inFight && !snap.paused ? (
            <div className="pointer-events-none absolute bottom-40 left-1/2 w-full max-w-md -translate-x-1/2 border border-line bg-surface/90 p-3 text-center">
              <p className="font-display text-lg tracking-widest text-amber">İLK TUR</p>
              <p className="text-sm text-fg">W gaz, A sola, D sağa. Space nitro. F veya ateş. Kırmızı halkanın içinde kal.</p>
            </div>
          ) : null}
          {portrait && inFight ? (
            <p className="pointer-events-none absolute top-14 left-1/2 -translate-x-1/2 bg-surface/80 px-2 py-1 text-center font-display text-sm tracking-widest text-sand">
              YATAY DAHA RAHAT
            </p>
          ) : null}
          {inFight ? (
            <button
              type="button"
              className="pointer-events-auto absolute top-3 left-1/2 flex -translate-x-1/2 items-center gap-1 border border-line bg-surface/80 px-3 py-2 font-display text-sm tracking-widest"
              onClick={() => rtRef.current?.togglePause()}
            >
              <Pause className="size-4" />
              {snap.paused ? "DEVAM" : "DUR"}
            </button>
          ) : null}
        </div>
      ) : null}

      {snap.paused && inFight ? (
        <div className="absolute inset-0 grid place-items-center bg-surface/80">
          <div className="flex w-72 flex-col gap-2 border border-line bg-surface-2 p-5">
            <h2 className="font-display text-4xl tracking-wide">DURAKLATILDI</h2>
            <MenuBtn label="DEVAM" primary onClick={() => rtRef.current?.togglePause()} />
            <MenuBtn label="ÇEKİL" onClick={() => rtRef.current?.backToMenu()} />
          </div>
        </div>
      ) : null}

      {inResult ? (
        <div className="absolute inset-0 grid place-items-center bg-surface/80 p-4">
          <div className="w-full max-w-sm border border-line bg-surface-2 p-6">
            <p className="font-display text-sm tracking-[0.3em] text-sand">{snap.win ? "KOLEZYUM SENİN" : snap.cause.toUpperCase() || "ELENDİN"}</p>
            <h2 className={`font-display text-6xl leading-none ${snap.win ? "text-brand" : "text-danger"}`}>{snap.win ? "ZAFER" : "ELENDİN"}</h2>
            <dl className="mt-4 grid grid-cols-3 gap-2 text-center">
              <Stat k="SIRA" v={`#${snap.place}`} />
              <Stat k="LEŞ" v={String(snap.kills)} />
              <Stat k="HURDA" v={`+${snap.earned}`} />
            </dl>
            <div className="mt-5 flex flex-col gap-2">
              <MenuBtn label="TEKRAR" primary icon={<Swords className="size-5" />} onClick={() => rtRef.current?.startMatch(diff)} />
              <MenuBtn
                label="GARAJ"
                icon={<Shield className="size-5" />}
                onClick={() => {
                  rtRef.current?.backToMenu();
                  setPanel("garage");
                }}
              />
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function MenuBtn({
  label,
  onClick,
  primary,
  icon,
}: {
  label: string;
  onClick: () => void;
  primary?: boolean;
  icon?: ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex min-h-11 items-center gap-3 border px-4 py-3 font-display text-xl tracking-widest ${primary ? "border-brand bg-brand text-fg" : "border-line bg-surface-2 text-fg"}`}
    >
      {icon}
      {label}
    </button>
  );
}

function Upgrade({ title, level, onBuy }: { title: string; level: number; note?: string; onBuy: () => void }) {
  const cost = level >= 5 ? 0 : UPGRADE_COST[level - 1];
  return (
    <button type="button" onClick={onBuy} className="flex items-center justify-between border border-line bg-surface-2 px-3 py-3 text-left">
      <span>
        <span className="block font-display text-lg tracking-wide">{title}</span>
        <span className="flex gap-1">
          {[1, 2, 3, 4, 5].map((n) => (
            <span key={n} className={`inline-block h-2 w-4 ${n <= level ? "bg-brand" : "bg-surface-3"}`} />
          ))}
        </span>
      </span>
      <span className="font-display text-amber">{level >= 5 ? "MAX" : `${cost} HURDA`}</span>
    </button>
  );
}

function Bar({ label, value, color }: { label: string; value: number; color: string }) {
  const w = `${Math.max(0, Math.min(1, value)) * 100}%`;
  return (
    <div className="mb-1">
      <div className="flex justify-between font-display text-xs tracking-widest text-muted">
        <span>{label}</span>
      </div>
      <div className="h-2 bg-surface-3">
        <div className={`bar-seg h-full ${color}`} style={{ width: w }} />
      </div>
    </div>
  );
}

function Stat({ k, v }: { k: string; v: string }) {
  return (
    <div className="border border-line bg-surface px-2 py-2">
      <dt className="text-xs tracking-widest text-muted">{k}</dt>
      <dd className="font-display text-2xl text-fg">{v}</dd>
    </div>
  );
}

function Stick({ onChange }: { onChange: (steer: number, throttle: number) => void }) {
  const ref = useRef<HTMLDivElement>(null);
  const [knob, setKnob] = useState({ x: 0, y: 0 });
  const move = (clientX: number, clientY: number) => {
    const el = ref.current;
    if (!el) return;
    const r = el.getBoundingClientRect();
    const cx = r.left + r.width / 2;
    const cy = r.top + r.height / 2;
    let dx = clientX - cx;
    let dy = clientY - cy;
    const max = r.width * 0.38;
    const len = Math.hypot(dx, dy) || 1;
    if (len > max) {
      dx = (dx / len) * max;
      dy = (dy / len) * max;
    }
    setKnob({ x: dx, y: dy });
    const steer = Math.max(-1, Math.min(1, -dx / max));
    const throttle = Math.max(-1, Math.min(1, -dy / max));
    onChange(Math.abs(steer) < 0.12 ? 0 : steer, Math.abs(throttle) < 0.12 ? 0 : throttle);
  };
  return (
    <div
      ref={ref}
      className="stick-zone relative size-28 rounded-full border border-line bg-surface/80"
      onPointerDown={(e) => {
        e.currentTarget.setPointerCapture(e.pointerId);
        move(e.clientX, e.clientY);
      }}
      onPointerMove={(e) => {
        if (e.currentTarget.hasPointerCapture(e.pointerId)) move(e.clientX, e.clientY);
      }}
      onPointerUp={(e) => {
        if (e.currentTarget.hasPointerCapture(e.pointerId)) e.currentTarget.releasePointerCapture(e.pointerId);
        setKnob({ x: 0, y: 0 });
        onChange(0, 0);
      }}
    >
      <span className="absolute top-1/2 left-1/2 size-11 -translate-x-1/2 -translate-y-1/2 border border-sand bg-surface-3" style={{ transform: `translate(calc(-50% + ${knob.x}px), calc(-50% + ${knob.y}px))` }} />
    </div>
  );
}

function HoldBtn({ label, onHold, children, hot }: { label: string; onHold: (v: boolean) => void; children: ReactNode; hot?: boolean }) {
  return (
    <button
      type="button"
      aria-label={label}
      className={`stick-zone grid size-14 place-items-center border font-display text-xs tracking-wide ${hot ? "border-brand bg-brand text-fg" : "border-line bg-surface/85 text-fg"}`}
      onPointerDown={(e) => {
        e.currentTarget.setPointerCapture(e.pointerId);
        onHold(true);
      }}
      onPointerUp={() => onHold(false)}
      onPointerCancel={() => onHold(false)}
    >
      {children}
    </button>
  );
}
