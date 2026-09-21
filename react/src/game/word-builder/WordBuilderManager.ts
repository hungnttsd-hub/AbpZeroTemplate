import Phaser from 'phaser';
import type { MechanicContext } from '../mechanics';
import type { LevelDefinition } from '../../types';
import { resolveWordBuilder, type WordBuilderConfig } from './config';
import { LetterSequenceManager } from './LetterSequenceManager';
import { LetterToken } from './LetterToken';
import { LetterSlot } from './LetterSlot';
import { LetterCollector } from './LetterCollector';
import { WordBuilderEffects } from './WordBuilderEffects';
import { completionActions, type CompletionAction } from './CompletionActions';
import { WordCompletionController } from './WordCompletionController';
import { WordBuilderEventBus, type BuilderEventName } from './events';

export class WordBuilderManager {
  readonly config: WordBuilderConfig;
  readonly events = new WordBuilderEventBus();
  private effects: WordBuilderEffects;
  private sequence: LetterSequenceManager;
  private tokens: LetterToken[];
  private slots: LetterSlot[];
  private collector: LetterCollector;
  private action: CompletionAction;
  private completion: WordCompletionController;
  private labels: Phaser.GameObjects.Text[] = [];
  private selected?: LetterToken;
  private busy = false;
  private done = false;
  private disposed = false;
  private errors = 0;
  private attempts = 0;
  private hintShown = false;
  private hintArrow: Phaser.GameObjects.Graphics;
  private hintGhost?: Phaser.GameObjects.Container;
  private cleanups: Array<() => void> = [];
  constructor(private ctx: MechanicContext, private level: LevelDefinition) {
    this.config = resolveWordBuilder(level);
    const c = this.config; const s = ctx.scene;
    this.effects = new WordBuilderEffects(s, ctx.reducedMotion);
    const units = c.unit === 'word' ? c.targetWord.split(' ') : [...c.targetWord];
    const prefilled = c.mode === 'missing_letter' ? units.map((_, i) => i).filter(i => !c.missingIndices.includes(i)) : [];
    const definitions = [...c.letters, ...c.distractors.map((char, i) => ({ id: `distractor_${i}`, char, spawn: 'static' as const }))];
    this.sequence = new LetterSequenceManager(units, definitions, prefilled);
    const shuffled = Phaser.Utils.Array.Shuffle([...definitions]);
    this.tokens = shuffled.map(def => new LetterToken(s, def));
    this.slots = units.map((char, i) => new LetterSlot(s, i, c.ghostLetters || c.difficulty === 1 ? char : '', this.sequence.slots[i]));
    const Action = completionActions[c.completionAction.type]; this.action = new Action(s, this.effects, c);
    this.completion = new WordCompletionController(ctx, this.effects, event => this.emit(event));
    this.collector = new LetterCollector(this.effects, token => this.onCollected(token));
    this.hintArrow = s.add.graphics().setDepth(45);
    this.labels.push(s.add.text(0, 25, c.mission, { fontFamily: 'Nunito, Arial', fontSize: '22px', fontStyle: 'bold', color: '#64825a', align: 'center', wordWrap: { width: s.scale.width - 40 } }).setOrigin(.5, 0));
    this.labels.push(s.add.text(0, 0, '', { fontFamily: 'Nunito, Arial', fontSize: '20px', color: '#8c9c7c' }).setOrigin(.5));
    this.labels[1].setText('KHÁM PHÁ → THU THẬP → KÍCH HOẠT');
    this.bind(); this.reflow(); this.emit('WORD_BUILDER_STARTED');
    ctx.instruction(c.difficulty === 5 ? 'Listen and build.' : c.instruction.text);
    ctx.feedback(c.mode === 'collect_letters' ? 'Chạm đồ vật để tìm chữ. Chữ đúng tự bay về mật mã!' : 'Tìm chữ trong đồ vật · Kéo đến vị trí, hoặc chạm chữ rồi chạm vị trí');
    void ctx.audio.speakSequence([c.instruction.text, c.targetWord]);
    if (c.hideMeaningAfterMs || c.difficulty === 5) this.effects.later(c.hideMeaningAfterMs ?? 2200, () => { if (!this.done) this.action.setMeaningVisible(false); });
  }
  private emit(type: BuilderEventName, token?: LetterToken, slot?: number) {
    const event = { type, levelId: this.level.id, word: this.config.targetWord, letter: token?.definition.char, tokenId: token?.definition.id, slot, attempt: this.attempts, timestamp: Date.now() };
    this.events.emit(event); this.ctx.scene.events.emit(type, event);
  }
  private bind() {
    const s = this.ctx.scene;
    for (const token of this.tokens) token.view.on('pointerdown', () => {
      if (this.done || this.busy || token.state === 'placed' || token.state === 'flying') return;
      if (token.state === 'hidden') { this.busy = true; this.collector.collect(token); } else this.select(token);
    });
    this.slots.forEach(slot => slot.view.on('pointerdown', () => { if (this.selected && !this.busy) this.place(this.selected, slot.index); }));
    const drag = (_p: Phaser.Input.Pointer, object: Phaser.GameObjects.GameObject, x: number, y: number) => {
      const token = this.tokens.find(t => t.view === object);
      if (!token || token.state !== 'available' || this.busy || this.done) return;
      this.select(token); token.view.setPosition(x, y).setDepth(40);
    };
    const end = (_p: Phaser.Input.Pointer, object: Phaser.GameObjects.GameObject) => {
      const token = this.tokens.find(t => t.view === object); if (!token || token.state !== 'available' || this.busy || this.done) return;
      const slot = this.slots.find(slot => slot.contains(token.view.x, token.view.y));
      if (slot) this.place(token, slot.index); else this.returnHome(token);
    };
    const resize = () => { if (!this.busy) this.reflow(); };
    const pause = () => { if (this.selected?.state === 'available') this.returnHome(this.selected); };
    s.input.on('drag', drag); s.input.on('dragend', end); s.scale.on('resize', resize); s.events.on('pause', pause);
    this.cleanups.push(() => { s.input.off('drag', drag); s.input.off('dragend', end); s.scale.off('resize', resize); s.events.off('pause', pause); });
  }
  private onCollected(token: LetterToken) {
    if (this.disposed || this.done) return;
    this.busy = false; this.reflow();
    this.emit('LETTER_COLLECTED', token); void this.ctx.audio.enqueueSpeech(token.definition.char);
    if (this.config.mode === 'collect_letters') {
      const index = this.sequence.matchingIndex(token.definition.id);
      if (index >= 0) { this.place(token, index); return; }
    }
    this.select(token);
  }
  private select(token: LetterToken) {
    this.selected?.select(false); this.selected = token; token.select(true);
  }
  private place(token: LetterToken, index: number) {
    if (this.done || this.busy || token.state !== 'available') return;
    this.attempts++; const result = this.sequence.place(token.definition.id, index);
    if (!result.ok) {
      // Filled positions are benign: dropping near one does not penalize learning.
      if (result.reason !== 'occupied') {
        this.errors++; this.ctx.tracker.record(this.level.targetVocabulary[0], false); this.emit('LETTER_WRONG', token, index);
        void this.ctx.audio.enqueueSpeech(`${token.definition.char}. Almost! Try again.`);
        this.ctx.feedback('Gần đúng rồi! Bé thử vị trí khác nhé.');
      }
      this.returnHome(token); this.effects.shake(token.view); if (this.errors >= 2) this.hint(token); return;
    }
    this.busy = true; token.state = 'flying'; this.selected = undefined; token.select(false); this.hintArrow.clear();
    const slot = this.slots[index];
    this.effects.fly(token.view, slot.view, () => {
      if (this.disposed) return;
      token.state = 'placed'; token.view.setVisible(false).disableInteractive(); slot.fill(token.definition.char); this.effects.bounce(slot.view);
      this.emit('LETTER_PLACED', token, index); this.busy = false; this.reflow();
      if (result.complete) this.finish(); else this.ctx.feedback('Một mảnh mật mã đã sáng lên! Tiếp tục khám phá nhé.');
    });
  }
  private returnHome(token: LetterToken) { token.view.setPosition(token.home.x, token.home.y).setDepth(12); }
  private hint(token: LetterToken) {
    if (!this.hintShown) { this.hintShown = true; this.ctx.tracker.hintCount++; }
    let index = this.sequence.matchingIndex(token.definition.id);
    let match = token;
    if (index < 0) { index = this.sequence.nextIndex; match = this.tokens.find(t => t.state !== 'placed' && t.definition.char === this.sequence.units[index]) ?? token; }
    const slot = this.slots[index]; if (!slot) return; slot.glow();
    this.ctx.feedback('Nhìn vị trí đang sáng nhé!');
    if (this.errors >= 3) {
      const g = this.hintArrow; const x = match.view.x; const y = match.view.y;
      g.clear().lineStyle(5, 0xe2b665, .65).lineBetween(x, y, slot.view.x, slot.view.y);
      g.fillStyle(0xe2b665).fillCircle(slot.view.x, slot.view.y - 55, 8);
      if (!this.ctx.reducedMotion) this.effects.tween({ targets: g, alpha: { from: .25, to: 1 }, duration: 400, yoyo: true, repeat: 2 });
      if (!this.hintGhost) {
        const letter = this.ctx.scene.add.text(0, 0, match.definition.char, { fontFamily: 'Nunito, Arial', fontSize: '48px', color: '#bc9947', fontStyle: 'bold' }).setOrigin(.5);
        const ghost = this.ctx.scene.add.container(x, y, [letter]).setAlpha(.55); this.hintGhost = ghost;
        this.effects.fly(ghost, slot.view, () => { ghost.destroy(); if (this.hintGhost === ghost) this.hintGhost = undefined; });
      }
      this.effects.later(2700, () => g.clear());
    }
  }
  private finish() {
    if (this.done) return; this.done = true;
    this.tokens.forEach(t => t.view.disableInteractive()); this.action.view.setAlpha(1);
    this.ctx.tracker.record(this.level.targetVocabulary[0], true);
    this.completion.start(this.config, this.slots, this.action);
  }
  private reflow() {
    if (this.disposed) return;
    const w = this.ctx.scene.scale.width; const h = this.ctx.scene.scale.height; const compact = w < 600;
    this.action.view.setPosition(compact ? w / 2 : 205, compact ? 176 : 225);
    this.labels[0].setPosition(w / 2, 20).setWordWrapWidth(w - 36);
    this.labels[1].setPosition(compact ? w / 2 : 205, compact ? 322 : 350).setFontSize(compact ? 17 : 16);
    const cols = compact ? 3 : Math.min(4, this.tokens.length);
    this.tokens.forEach((t, i) => {
      if (t.state === 'placed' || t.state === 'flying') return;
      const row = Math.floor(i / cols); const count = Math.min(cols, this.tokens.length - row * cols);
      t.position((compact ? w / 2 : 680) + ((i % cols) - (count - 1) / 2) * (compact ? 143 : 125), (compact ? 420 : 160) + row * (compact ? 145 : 135), this.ctx.reducedMotion);
    });
    const slotCols = compact ? 3 : Math.min(8, this.slots.length);
    const rows = Math.ceil(this.slots.length / slotCols);
    this.slots.forEach((slot, i) => {
      const row = Math.floor(i / slotCols); const count = Math.min(slotCols, this.slots.length - row * slotCols);
      slot.view.setPosition(w / 2 + ((i % slotCols) - (count - 1) / 2) * 115, h - (compact ? 170 : 150) - (rows - 1 - row) * 120);
    });
    this.completion.resize();
  }
  dispose() {
    this.disposed = true; this.cleanups.forEach(fn => fn()); this.completion.dispose(); this.effects.dispose();
    this.tokens.forEach(t => t.destroy()); this.slots.forEach(s => s.destroy()); this.action.dispose();
    this.labels.forEach(l => l.destroy()); this.hintArrow.destroy(); this.hintGhost?.destroy(); this.events.clear(); this.ctx.audio.stopAll();
  }
}
