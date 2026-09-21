import Phaser from 'phaser';
import type { GameMechanic, MechanicContext } from '../mechanics';
import type { LevelDefinition } from '../../types';
import { WordBuilderEffects } from '../word-builder/WordBuilderEffects';
import { BalloonManager, BalloonTarget, DartPool, Launcher, landscape, text } from './visuals';
import { matches, resolveBalloonDart, RoundProgress, type BalloonDartConfig, type BalloonRound } from './model';
import { aimAngle, segmentHit } from './motion';

export class BalloonDartController implements GameMechanic {
  readonly id = 'balloon_dart' as const;
  private ctx!: MechanicContext; private level!: LevelDefinition; private config!: BalloonDartConfig;
  private balloons!: BalloonManager; private dart!: DartPool; private launcher!: Launcher; private fx!: WordBuilderEffects;
  private progress = new RoundProgress(); private round!: BalloonRound;
  private objects: Phaser.GameObjects.GameObject[] = []; private cleanups: Array<() => void> = [];
  private guide!: Phaser.GameObjects.Graphics; private stars: Phaser.GameObjects.Text[] = []; private caption!: Phaser.GameObjects.Text;
  private elapsed = 0; private total = 0; private cooldown = 0; private idle = 0;
  private angle = -Math.PI / 2; private desired = -Math.PI / 2; private pointer?: number;
  private tapPending = false; private tapRemaining = 0; private tapTarget?: BalloonTarget;
  private disposed = false; private ready = false; private replayedIdle = false; private shownIdle = false;
  private roundHints = new Set<string>(); private hintedUntil = 0;
  private roundStart = { shots: 0, wrongHits: 0, misses: 0, hints: 0 };
  private audioContext?: AudioContext;
  private own<T extends Phaser.GameObjects.GameObject>(o: T): T { this.objects.push(o); return o; }
  private emit(name: string, extra: Record<string, unknown> = {}) { this.ctx.scene.events.emit(`BALLOON_DART_${name}`, { levelId: this.level.id, roundId: this.round?.id, timestamp: Date.now(), ...extra }); }
  mount(ctx: MechanicContext, level: LevelDefinition) {
    this.ctx = ctx; this.level = level; this.config = resolveBalloonDart(level);
    const s = ctx.scene;
    this.fx = new WordBuilderEffects(s, ctx.reducedMotion);
    this.own(landscape(s)); this.guide = this.own(s.add.graphics().setDepth(8));
    this.launcher = new Launcher(s); this.dart = new DartPool(s); this.balloons = new BalloonManager(s, ctx.reducedMotion);
    this.own(s.add.rectangle(1203, 71, 290, 85, 0xfff8e8, .92).setStrokeStyle(2, 0xffffff).setDepth(12));
    this.stars = [0, 1, 2].map(i => this.own(text(s, 1127 + i * 76, 65, '☆', 54, '#c2b98e').setDepth(13)));
    this.caption = this.own(text(s, 720, 774, 'Kéo để ngắm · Thả để bắn · Hoặc chạm bóng', 25).setDepth(23));
    this.own(text(s, 223, 70, 'BALLOON GARDEN', 25, '#477d73').setDepth(12));
    this.bind(); this.emit('LEVEL_STARTED'); this.startRound();
  }
  private startRound() {
    if (this.disposed) return;
    this.round = this.config.rounds[this.progress.round]; this.elapsed = 0; this.idle = 0;
    this.replayedIdle = false; this.shownIdle = false; this.roundHints.clear(); this.hintedUntil = 0; this.ready = true;
    this.roundStart = { shots: this.progress.shots, wrongHits: this.progress.wrongHits, misses: this.progress.misses, hints: this.ctx.tracker.hintCount };
    this.balloons.spawn(this.round.balloons, this.config.learningMode, this.round.seed);
    this.ctx.instruction(this.round.instructionText); this.ctx.feedback(`Lượt ${this.progress.round + 1} / 3 · Nghe, ngắm và bắn!`);
    this.caption.setText(this.config.inputMode === 'tap' ? 'Chạm bóng để ngắm và bắn' : 'Kéo để ngắm · Thả để bắn');
    void this.ctx.audio.enqueueSpeech(this.round.instructionText, this.voiceKey(this.round.instructionAudioKey));
    this.emit('ROUND_STARTED', { target: this.round.target });
  }
  private canFire() { return this.ready && !this.progress.locked && !this.dart.active && !this.tapPending && this.cooldown <= 0 && !this.disposed; }
  private bind() {
    const s = this.ctx.scene;
    const down = (p: Phaser.Input.Pointer) => {
      if (!this.canFire() || this.pointer !== undefined || p.y < 135) return;
      this.pointer = p.id; this.desired = aimAngle(p.x, p.y);
      const target = this.balloons.targets.find(b => !b.popped && b.contains(p.x, p.y));
      if (target && this.config.inputMode !== 'drag') {
        this.pointer = undefined; this.tapPending = true; this.tapRemaining = 160; this.tapTarget = target;
      } else if (this.config.inputMode === 'tap') this.pointer = undefined;
    };
    const move = (p: Phaser.Input.Pointer) => { if (this.progress.locked || this.tapPending) return; if (p.id === this.pointer || (!p.isDown && this.config.inputMode !== 'tap')) this.desired = aimAngle(p.x, p.y); };
    const up = (p: Phaser.Input.Pointer) => { if (p.id !== this.pointer) return; this.pointer = undefined; if (this.config.inputMode !== 'tap') { this.desired = aimAngle(p.x, p.y); this.angle = this.desired; this.fire(); } };
    const cancel = () => { this.pointer = undefined; this.tapPending = false; this.tapTarget = undefined; };
    const key = (e: KeyboardEvent) => {
      if (s.scene.isPaused()) return;
      if (e.key === 'ArrowLeft') this.desired = Math.max(-Math.PI * 160 / 180, this.desired - .06);
      if (e.key === 'ArrowRight') this.desired = Math.min(-Math.PI * 20 / 180, this.desired + .06);
      if (e.code === 'Space' && !e.repeat) { e.preventDefault(); this.angle = this.desired; this.fire(); }
      if (/^[1-5]$/.test(e.key) && this.canFire() && this.config.inputMode !== 'drag') {
        const target = [...this.balloons.targets].sort((a, b) => a.view.x - b.view.x)[Number(e.key) - 1];
        if (target) { this.tapPending = true; this.tapRemaining = 160; this.tapTarget = target; }
      }
    };
    const update = (_time: number, delta: number) => this.update(Math.min(50, delta));
    s.input.on('pointerdown', down); s.input.on('pointermove', move); s.input.on('pointerup', up); s.input.on('pointerupoutside', cancel);
    s.events.on('pause', cancel); s.events.on('update', update); s.input.keyboard?.on('keydown', key);
    s.game.canvas.addEventListener('pointercancel', cancel);
    this.cleanups.push(() => { s.input.off('pointerdown', down); s.input.off('pointermove', move); s.input.off('pointerup', up); s.input.off('pointerupoutside', cancel); s.events.off('pause', cancel); s.events.off('update', update); s.input.keyboard?.off('keydown', key); s.game.canvas.removeEventListener('pointercancel', cancel); });
  }
  private fire() {
    if (!this.canFire() || !this.progress.fire()) return;
    this.idle = 0; this.dart.fire(this.angle); this.sound('fire');
    this.emit('DART_FIRED', { shotIndex: this.progress.shots, angle: this.angle });
    if (!this.ctx.reducedMotion) this.fx.tween({ targets: this.launcher.barrel, x: -Math.cos(this.angle) * 7, y: -Math.sin(this.angle) * 7, duration: 85, yoyo: true });
    this.caption.setText('');
  }
  private update(delta: number) {
    if (this.disposed) return;
    this.elapsed += delta; this.total += delta; this.cooldown -= delta;
    if (!this.progress.locked) this.balloons.update(this.elapsed, this.round.seed);
    if (this.tapPending && this.tapTarget) {
      this.desired = aimAngle(this.tapTarget.view.x, this.tapTarget.view.y); this.tapRemaining -= delta;
      if (this.tapRemaining <= 0) { this.angle = this.desired; this.tapPending = false; this.tapTarget = undefined; this.fire(); }
    }
    this.angle += (this.desired - this.angle) * Math.min(1, delta / 65); this.launcher.barrel.setRotation(this.angle);
    this.drawGuide();
    if (this.dart.active) {
      const ax = this.dart.x, ay = this.dart.y;
      this.dart.move(delta / 1000);
      let target: BalloonTarget | undefined; let first = 2;
      for (const b of this.balloons.targets) {
        if (b.popped) continue;
        const t = segmentHit(ax - b.previous.x + b.view.x, ay - b.previous.y + b.view.y, this.dart.x, this.dart.y, b.view.x, b.view.y, b.radiusX + 8, b.radiusY + 8);
        if (t !== undefined && t < first) { first = t; target = b; }
      }
      if (target) { this.dart.view.setPosition(ax + (this.dart.x - ax) * first, ay + (this.dart.y - ay) * first); this.hit(target); }
      else if (this.dart.y < 135 || this.dart.x < 30 || this.dart.x > 1410) {
        this.dart.recycle(); this.cooldown = 280; this.progress.miss(); this.sound('miss'); this.emit('MISS', { shotIndex: this.progress.shots }); this.hints();
      }
    }
    if (!this.progress.locked && !this.dart.active) {
      this.idle += delta;
      if (this.idle >= 8000 && !this.replayedIdle) { this.replayedIdle = true; void this.ctx.audio.enqueueSpeech(this.round.instructionText); }
      if (this.idle >= 15000 && !this.shownIdle) { this.shownIdle = true; this.caption.setText('Chạm một bóng, hoặc kéo để ngắm rồi thả'); if (!this.ctx.reducedMotion) this.fx.tween({ targets: this.launcher.mascot, angle: -9, duration: 350, yoyo: true, repeat: 2 }); }
    }
    const glow = this.roundHints.has('halo') && !this.progress.locked;
    for (const b of this.balloons.targets) { const on = glow && matches(b.config.semantic, this.round.target) && this.elapsed % 2500 < 1200; b.halo.setVisible(on).setAlpha(this.ctx.reducedMotion ? .5 : .3 + .25 * Math.sin(this.elapsed / 200)); }
  }
  private drawGuide() {
    this.guide.clear(); if (this.dart.active || this.progress.locked) return;
    const length = { long: 460, medium: 285, short: 170 }[this.round.aimAssist];
    for (let n = 104; n < length; n += 29) this.guide.fillStyle(0xffffff, .8 * (1 - n / (length + 140))).fillCircle(720 + Math.cos(this.angle) * n, 705 + Math.sin(this.angle) * n, 5);
    if (this.hintedUntil > this.elapsed) {
      const b = this.balloons.targets.find(b => matches(b.config.semantic, this.round.target));
      if (b) { const broad = Math.round(aimAngle(b.view.x, b.view.y) / .3) * .3; for (let n = 115; n < 245; n += 30) this.guide.fillStyle(0xffdf82, .65).fillCircle(720 + Math.cos(broad) * n, 705 + Math.sin(broad) * n, 6); }
    }
  }
  private hit(b: BalloonTarget) {
    const correct = matches(b.config.semantic, this.round.target);
    const dx = this.dart.view.x, dy = this.dart.view.y, rotation = this.dart.view.rotation;
    this.dart.recycle(); this.cooldown = 340;
    const word = b.config.semantic.word ?? b.config.semantic.id;
    this.ctx.tracker.record(word, correct);
    this.emit(correct ? 'CORRECT_HIT' : 'WRONG_HIT', { balloonInstanceId: b.config.instanceId, semanticId: b.config.semantic.id, shotIndex: this.progress.shots });
    if (!this.progress.hit(correct)) {
      this.sound('wrong'); this.fx.tween({ targets: b.view, scaleX: .93, scaleY: .9, duration: 95, yoyo: true, ease: 'Sine.easeInOut' });
      this.dart.view.setPosition(dx, dy).setRotation(rotation).setAlpha(1).setVisible(true);
      this.fx.tween({ targets: this.dart.view, y: dy + 65, angle: this.dart.view.angle + 70, alpha: 0, duration: 300, onComplete: () => this.dart.view.setVisible(false) });
      void this.ctx.audio.enqueueSpeech(word, this.voiceKey(b.config.semantic.audioKey)); this.ctx.feedback(`${word} · Bé thử tiếp nhé!`); this.hints(); return;
    }
    this.ready = false; this.pointer = undefined; b.popped = true; b.halo.setVisible(false);
    this.fx.tween({ targets: b.view, scaleX: 1.05, scaleY: .9, duration: 90, yoyo: true });
    this.fx.later(155, () => {
      this.sound('pop');
      if (!this.ctx.reducedMotion) this.fx.sparkle(b.view.x, b.view.y);
      this.fx.tween({ targets: b.view, alpha: 0, scale: this.ctx.reducedMotion ? 1 : 1.1, duration: 170 });
      const star = this.stars[this.progress.round]; star.setText('★').setColor('#e9b943');
      if (!this.ctx.reducedMotion) this.fx.tween({ targets: star, scale: 1.23, duration: 150, yoyo: true });
      this.fx.tween({ targets: this.launcher.mascot, y: this.ctx.reducedMotion ? 691 : 675, duration: 180, yoyo: true, repeat: this.ctx.reducedMotion ? 0 : 1 });
    });
    // Correct vocabulary supersedes queued wrong words. AudioService guarantees one voice.
    this.ctx.audio.stopAll(); void this.ctx.audio.enqueueSpeech(word, this.voiceKey(b.config.semantic.audioKey)); this.ctx.feedback('Great job! Thêm một ngôi sao!');
    this.fx.later(1150, () => {
      this.emit('ROUND_COMPLETED', { shots: this.progress.shots - this.roundStart.shots, wrongHits: this.progress.wrongHits - this.roundStart.wrongHits, misses: this.progress.misses - this.roundStart.misses, hintCount: this.ctx.tracker.hintCount - this.roundStart.hints, durationMs: Math.round(this.elapsed) });
      this.progress.advance();
      if (this.progress.round === 3) {
        const summary = { mechanic: 'balloon_dart' as const, roundsCompleted: 3, shots: this.progress.shots, wrongHits: this.progress.wrongHits, misses: this.progress.misses, hintCount: this.ctx.tracker.hintCount, durationSeconds: Math.round(this.total / 1000), stars: 3 as const };
        this.ctx.tracker.balloonDart = summary; this.emit('LEVEL_COMPLETED', { ...summary }); this.ctx.complete();
      } else this.startRound();
    });
  }
  private hints() {
    const n = this.progress.errors;
    const show = (kind: string, action: () => void) => { if (this.roundHints.has(kind)) return; this.roundHints.add(kind); this.ctx.tracker.hintCount++; this.emit('HINT_SHOWN', { hintType: kind, errorCount: n }); action(); };
    if (n >= 2) show('replay', () => { void this.ctx.audio.enqueueSpeech(this.round.instructionText); });
    if (n >= 3) show('halo', () => this.ctx.feedback('Bé nhìn ánh sáng dịu quanh bóng nhé.'));
    if (n >= 5) show('direction', () => { this.hintedUntil = this.elapsed + 2200; });
  }
  private sound(kind: 'fire' | 'miss' | 'wrong' | 'pop') {
    if (this.ctx.audio.muted) return;
    try {
      this.audioContext ??= new AudioContext(); const audio = this.audioContext;
      if (audio.state === 'suspended') void audio.resume().catch(() => {});
      const oscillator = audio.createOscillator(), gain = audio.createGain(); oscillator.type = 'sine';
      const f = { fire: 520, miss: 330, wrong: 240, pop: 690 }[kind];
      oscillator.frequency.setValueAtTime(f, audio.currentTime); oscillator.frequency.exponentialRampToValueAtTime(f * .52, audio.currentTime + .13);
      gain.gain.setValueAtTime(.025, audio.currentTime); gain.gain.exponentialRampToValueAtTime(.0001, audio.currentTime + .16);
      oscillator.connect(gain); gain.connect(audio.destination); oscillator.start(); oscillator.stop(audio.currentTime + .17);
      oscillator.onended = () => { oscillator.disconnect(); gain.disconnect(); };
    } catch { /* Spoken feedback remains available when Web Audio is unavailable. */ }
  }
  private voiceKey(key?: string) {
    if (!key) return undefined;
    if (key.startsWith('instruction.')) return `audio/voice/instructions/${key.slice(12)}.mp3`;
    if (key.startsWith('word.')) return `audio/voice/words/${key.slice(5)}.mp3`;
    return key;
  }
  dispose() {
    this.disposed = true; this.cleanups.forEach(fn => fn()); this.cleanups = []; this.fx.dispose();
    this.ctx.scene.tweens.killTweensOf(this.launcher.barrel); this.ctx.scene.tweens.killTweensOf(this.launcher.mascot);
    this.balloons.clear(); this.dart.dispose(); this.launcher.dispose(); this.objects.forEach(o => o.destroy());
    this.objects = []; this.ctx.audio.stopAll(); void this.audioContext?.close().catch(() => {});
  }
}
