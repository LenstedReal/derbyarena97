export class Sfx {
  private ctx: AudioContext | null = null;
  enabled = true;
  private lastZone = 0;
  private engineOsc: OscillatorNode | null = null;
  private engineOsc2: OscillatorNode | null = null;
  private engineGain: GainNode | null = null;

  unlock() {
    if (!this.enabled) return;
    const ctx = this.ensure();
    if (ctx.state === "suspended") void ctx.resume();
    this.ensureEngine();
  }

  stop() {
    try {
      this.engineOsc?.stop();
      this.engineOsc2?.stop();
    } catch {
      /* already stopped */
    }
    this.engineOsc = null;
    this.engineOsc2 = null;
    this.engineGain = null;
  }

  private ensure() {
    if (!this.ctx) {
      const AC = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
      this.ctx = new AC();
    }
    return this.ctx;
  }

  private ensureEngine() {
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
    g.gain.value = 0.0001;
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
  engine(speed: number, nitro: boolean) {
    if (!this.enabled) {
      if (this.engineGain && this.ctx) this.engineGain.gain.setTargetAtTime(0.0001, this.ctx.currentTime, 0.05);
      return;
    }
    const ctx = this.ensure();
    if (ctx.state === "suspended") return;
    this.ensureEngine();
    if (!this.engineOsc || !this.engineOsc2 || !this.engineGain) return;
    const rpm = 42 + Math.min(110, Math.abs(speed) * 8.5) + (nitro ? 36 : 0);
    const t = ctx.currentTime;
    this.engineOsc.frequency.setTargetAtTime(rpm, t, 0.08);
    this.engineOsc2.frequency.setTargetAtTime(rpm * 0.5, t, 0.08);
    const gain = 0.012 + Math.min(0.045, Math.abs(speed) * 0.0032) + (nitro ? 0.02 : 0);
    this.engineGain.gain.setTargetAtTime(gain, t, 0.06);
  }

  private tone(freq: number, dur: number, type: OscillatorType, gain: number, slide = 0) {
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
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    osc.connect(g);
    g.connect(ctx.destination);
    osc.start(t);
    osc.stop(t + dur + 0.02);
  }

  shot(kind: string) {
    if (kind === "rocket") this.tone(140, 0.28, "sawtooth", 0.05, -80);
    else if (kind === "shell") this.tone(90, 0.22, "square", 0.05, -40);
    else if (kind === "pellet") this.tone(220, 0.12, "square", 0.04, -100);
    else if (kind === "emp") this.tone(520, 0.18, "sine", 0.04, -300);
    else this.tone(640, 0.05, "square", 0.03, -200);
  }

  boom() {
    this.tone(90, 0.4, "sawtooth", 0.07, -60);
  }

  hit() {
    this.tone(180, 0.08, "triangle", 0.04, -80);
  }

  crunch() {
    this.tone(70, 0.16, "square", 0.06, -40);
  }

  pick() {
    this.tone(520, 0.09, "square", 0.03, 180);
  }

  zone(now: number) {
    if (now - this.lastZone < 0.75) return;
    this.lastZone = now;
    this.tone(160, 0.14, "sawtooth", 0.04, -40);
  }

  win() {
    this.tone(440, 0.12, "square", 0.04, 0);
    setTimeout(() => this.tone(660, 0.18, "square", 0.04, 0), 120);
  }

  lose() {
    this.tone(180, 0.3, "sawtooth", 0.05, -100);
  }

  buzz(ms: number) {
    if (!this.enabled) return;
    const nav = navigator as Navigator & { vibrate?: (pattern: number | number[]) => boolean };
    nav.vibrate?.(ms);
  }
}