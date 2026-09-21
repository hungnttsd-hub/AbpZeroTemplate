import type { Attempt, LevelDefinition, TargetResult } from '../types';

export class AssetResolver {
  private base = (import.meta.env.VITE_ASSET_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? '';
  image(key: string) { return `${this.base}/${key}`; }
  audio(key: string) { return `${this.base}/${key}`; }
  get hasRemoteAssets() { return this.base.length > 0; }
}
export class AudioService {
  private audio?: HTMLAudioElement;
  private generation = 0;
  private timer?: ReturnType<typeof setTimeout>;
  private queue: Promise<void> = Promise.resolve();
  private queuedStops = new Set<() => void>();
  constructor(private assets: AssetResolver, public muted: boolean, private unavailable: () => void = () => {}) {}
  playInstruction(level: LevelDefinition) { return this.play(level.instruction, level.instructionAudioKey); }
  speakWord(word: string) { return this.play(word, `audio/en/${word}.mp3`); }
  speak(text: string) { return this.play(text); }
  enqueueSpeech(text: string, key?: string) {
    const generation = this.generation;
    this.queue = this.queue.then(() => this.play(text, key, false, true, generation));
    return this.queue;
  }
  speakSequence(texts: string[], onItem?: (index: number) => void) {
    this.stopAll(); const generation = this.generation;
    this.queue = (async () => {
      for (let i = 0; i < texts.length; i++) {
        if (generation !== this.generation) return;
        onItem?.(i); await this.play(texts[i], undefined, false, true, generation);
      }
    })();
    return this.queue;
  }
  private waitForVoice(bind: (done: () => void) => void, timeout: number) {
    return new Promise<void>(resolve => {
      const done = () => { clearTimeout(timer); this.queuedStops.delete(done); resolve(); };
      const timer = setTimeout(done, timeout); this.queuedStops.add(done); bind(done);
    });
  }
  private async play(text: string, key?: string, replace = true, wait = false, expected?: number): Promise<void> {
    if (replace) this.stopAll();
    if (this.muted || (expected !== undefined && expected !== this.generation)) return;
    const generation = this.generation;
    if (key && this.assets.hasRemoteAssets) {
      const clip = new Audio(this.assets.audio(key));
      try {
        this.audio = clip;
        await Promise.race([clip.play(), new Promise<never>((_, reject) => { this.timer = setTimeout(() => reject(new Error('Audio timeout')), 2500); })]);
        clearTimeout(this.timer);
        if (wait && generation === this.generation) { await this.waitForVoice(done => { clip.onended = done; clip.onerror = done; if (clip.ended) done(); }, 7000); clip.pause(); }
        return;
      } catch { clip.pause(); }
    }
    if (generation !== this.generation) return;
    if (!('speechSynthesis' in window)) { this.unavailable(); return; }
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'en-US'; utterance.rate = .8;
    utterance.voice = speechSynthesis.getVoices().find(v => v.lang === 'en-US') ?? null;
    utterance.onerror = () => { if (generation === this.generation) this.unavailable(); };
    if (wait) {
      await this.waitForVoice(done => {
        utterance.onend = done;
        utterance.onerror = () => { if (generation === this.generation) this.unavailable(); done(); };
        speechSynthesis.speak(utterance);
      }, Math.min(8000, Math.max(1800, text.length * 150)));
      // Some voices never emit onend: don't let a stalled clip overlap the next one.
      if (generation === this.generation) speechSynthesis.cancel();
    } else speechSynthesis.speak(utterance);
  }
  stopAll() { this.generation++; clearTimeout(this.timer); this.queuedStops.forEach(done => done()); this.queuedStops.clear(); this.queue = Promise.resolve(); this.audio?.pause(); if ('speechSynthesis' in window) speechSynthesis.cancel(); }
}
export class AttemptTracker {
  balloonDart?: Attempt['balloonDart'];
  readonly id = crypto.randomUUID();
  readonly startedAt = new Date().toISOString();
  readonly results: TargetResult[] = [];
  wrongAttempts = 0; hintCount = 0;
  private lastAction = performance.now();
  record(term: string, correct: boolean) {
    this.results.push({ term, correct, responseMs: Math.min(86400000, Math.round(performance.now() - this.lastAction)) });
    this.lastAction = performance.now(); if (!correct) this.wrongAttempts++;
  }
  complete(childId: string, levelId: string): Attempt {
    return { attemptId: this.id, childId, levelId, startedAt: this.startedAt, completedAt: new Date().toISOString(), wrongAttempts: this.wrongAttempts, hintCount: this.hintCount, targetResults: this.results, ...(this.balloonDart ? { balloonDart: this.balloonDart } : {}) };
  }
}
