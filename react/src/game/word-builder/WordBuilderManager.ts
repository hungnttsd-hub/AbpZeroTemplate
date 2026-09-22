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
  private inventory: LetterToken[] = [];
  private phase: 'hunt' | 'build' = 'hunt';
  private regions: Phaser.GameObjects.Graphics;
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
    this.slots = units.map((char, i) => new LetterSlot(s, i, '', this.sequence.slots[i]));
    const Action = completionActions[c.completionAction.type]; this.action = new Action(s, this.effects, c);
    this.completion = new WordCompletionController(ctx, this.effects, event => this.emit(event));
    this.collector = new LetterCollector(this.effects, token => this.onCollected(token));
    this.hintArrow = s.add.graphics().setDepth(45);
    this.regions = s.add.graphics().setDepth(1);
    this.labels.push(s.add.text(0, 25, c.mission, { fontFamily: 'Nunito, Arial', fontSize: '22px', fontStyle: 'bold', color: '#64825a', align: 'center', wordWrap: { width: s.scale.width - 40 } }).setOrigin(.5, 0));
    this.labels.push(s.add.text(0, 0, '', { fontFamily: 'Nunito, Arial', fontSize: '20px', color: '#8c9c7c' }).setOrigin(.5));
    this.labels[1].setText('1 · SĂN CHỮ');
    this.bind(); this.reflow(); this.emit('WORD_BUILDER_STARTED');
    ctx.instruction(c.difficulty === 5 ? 'Listen and build.' : c.instruction.text);
    ctx.feedback('Săn chữ trong đồ vật. Chữ thu được sẽ bay về khay.');
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
      if (token.state === 'hidden') { this.busy = true; if (!this.collector.collect(token)) this.busy = false; } else if (this.phase === 'build') this.select(token);
    });
    this.slots.forEach(slot => slot.view.on('pointerdown', () => { if (this.selected && !this.busy) this.place(this.selected, slot.index); }));
    const drag = (_p: Phaser.Input.Pointer, object: Phaser.GameObjects.GameObject, x: number, y: number) => {
      const token = this.tokens.find(t => t.view === object);
      if (token?.state === 'hidden' && token.definition.spawn === 'pulley' && !this.busy && !this.done && y - token.home.y > 35) {
        this.busy = true; if (!this.collector.collect(token, true)) this.busy = false; return;
      }
      if (!token || token.state !== 'available' || this.busy || this.done || this.phase !== 'build') return;
      this.select(token); token.view.setPosition(x, y).setDepth(40);
    };
    const end = (_p: Phaser.Input.Pointer, object: Phaser.GameObjects.GameObject) => {
      const token = this.tokens.find(t => t.view === object); if (!token || token.state !== 'available' || this.busy || this.done || this.phase !== 'build') return;
      const slot = this.slots.find(slot => slot.contains(token.view.x, token.view.y));
      if (slot) this.place(token, slot.index); else this.returnHome(token);
    };
    const resize = () => { if (!this.busy) this.reflow(); };
    const pause = () => { if (this.selected?.state === 'available') this.returnHome(this.selected); };
    s.input.on('drag', drag); s.input.on('dragend', end); s.scale.on('resize', resize); s.events.on('pause', pause);
    this.cleanups.push(() => { s.input.off('drag', drag); s.input.off('dragend', end); s.scale.off('resize', resize); s.events.off('pause', pause); });
  }
  private inventoryPosition(index: number) {
    const w = this.ctx.scene.scale.width, h = this.ctx.scene.scale.height, cols = w < 600 ? 3 : 8;
    const rows = Math.ceil(this.tokens.length / cols), row = Math.floor(index / cols);
    const count = Math.min(cols, this.tokens.length - row * cols);
    const slotRows = Math.ceil(this.slots.length / (w < 600 ? 3 : 8));
    return { x: w / 2 + (index % cols - (count - 1) / 2) * 115, y: h - 300 - (slotRows - 1) * 120 - (rows - 1 - row) * 125 };
  }
  private onCollected(token: LetterToken) {
    if (this.disposed || this.done) return;
    this.busy = true; this.inventory.push(token); token.state = 'flying';
    this.emit('LETTER_COLLECTED', token); void this.ctx.audio.enqueueSpeech(token.definition.char);
    this.action.onCollected?.(this.inventory.filter(t => this.config.letters.some(d => d.id === t.definition.id)).length, this.config.letters.length);
    this.effects.fly(token.view, this.inventoryPosition(this.inventory.length - 1), () => {
      if (this.disposed) return;
      token.state = 'available'; this.busy = false;
      if (this.config.letters.every(def => this.inventory.some(t => t.definition.id === def.id))) {
        this.phase = 'build'; this.labels[1].setText('2 · XẾP CHỮ VÀ XÂY');
        this.ctx.feedback('Đủ chữ rồi! Chạm chữ trong khay rồi chạm ô, hoặc kéo để xếp đúng thứ tự.');
        void this.ctx.audio.enqueueSpeech('Great! Build the word.');
      } else this.ctx.feedback(`Đã tìm ${this.inventory.filter(t => this.config.letters.some(d => d.id === t.definition.id)).length}/${this.config.letters.length} chữ. Tiếp tục khám phá nhé!`);
      this.reflow();
    });
  }
  private select(token: LetterToken) {
    this.selected?.select(false); this.selected = token; token.select(true);
  }
  private place(token: LetterToken, index: number) {
    if (this.done || this.busy || this.phase !== 'build' || token.state !== 'available') return;
    this.attempts++; const result = this.sequence.place(token.definition.id, index);
    if (!result.ok) {
      // Filled positions are benign: dropping near one does not penalize learning.
      if (result.reason !== 'occupied') {
        this.errors++; this.ctx.tracker.record(this.level.targetVocabulary[0], false); this.emit('LETTER_WRONG', token, index);
        void this.ctx.audio.enqueueSpeech(`${token.definition.char}. Almost! Try again.`);
        this.ctx.feedback('Gần đúng rồi! Bé thử vị trí khác nhé.');
        this.action.onWrong?.();
      }
      this.returnHome(token); this.effects.shake(token.view); if (this.errors >= 2) this.hint(token); return;
    }
    this.busy = true; token.state = 'flying'; this.selected = undefined; token.select(false); this.hintArrow.clear();
    const slot = this.slots[index];
    this.effects.fly(token.view, slot.view, () => {
      if (this.disposed) return;
      token.state = 'placed'; token.view.setVisible(false).disableInteractive(); slot.fill(token.definition.char); this.effects.bounce(slot.view);
      this.emit('LETTER_PLACED', token, index); this.action.onPlaced?.(index); this.busy = false; this.reflow();
      if (result.complete) this.finish(); else this.ctx.feedback('Một phần công trình đã hoàn thành! Bé xếp tiếp nhé.');
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
        const letter = this.ctx.scene.add.text(0, 0, match.definition.char, { fontFamily: 'Nunito, Arial', fontSize: '48px', color: '#bc9947', fontStyle: 'bold' }).setOrigin(.5).setResolution(2);
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
    const w = this.ctx.scene.scale.width, h = this.ctx.scene.scale.height, compact = w < 600;
    this.action.view.setPosition(compact || this.phase === 'build' ? w / 2 : 205, compact ? 175 : 230).setScale(!compact && this.phase === 'build' ? 1.15 : 1);
    this.labels[0].setPosition(w / 2, 18).setWordWrapWidth(w - 68).setFontSize(compact ? 18 : 22).setDepth(5);
    this.labels[1].setPosition(w / 2, 76).setFontSize(18).setDepth(5);
    const inventoryRows = Math.ceil(this.tokens.length / (compact ? 3 : 8));
    const top = this.inventoryPosition(0).y - 64;
    this.regions.clear().fillStyle(0xfffbeb, .94).fillRoundedRect(24, 7, w - 48, 99, 25)
      .lineStyle(3, 0xe0c791).strokeRoundedRect(24, 7, w - 48, 99, 25)
      .fillStyle(0xfff9e7, .83).fillRoundedRect(compact ? 22 : this.phase === 'build' ? w / 2 - 202 : 18, 122, compact ? w - 44 : 404, 255, 28)
      .fillStyle(0x8f714b, .18).fillRoundedRect(12, top + 6, w - 24, inventoryRows * 125, 22)
      .fillStyle(0xfff7dd, .96).fillRoundedRect(12, top, w - 24, inventoryRows * 125, 22)
      .lineStyle(3, 0xd6ba82).strokeRoundedRect(12, top, w - 24, inventoryRows * 125, 22);
    const cols = 3;
    this.tokens.forEach((t, i) => {
      if (t.state === 'placed' || t.state === 'flying') return;
      const inventoryIndex = this.inventory.indexOf(t);
      if (inventoryIndex >= 0) { const p = this.inventoryPosition(inventoryIndex); t.position(p.x, p.y, this.ctx.reducedMotion); return; }
      t.view.setVisible(this.phase === 'hunt');
      const row = Math.floor(i / cols), count = Math.min(cols, this.tokens.length - row * cols);
      t.position((compact ? w / 2 : 685) + (i % cols - (count - 1) / 2) * (compact ? 145 : 165), (compact ? 490 : 225) + row * 180, this.ctx.reducedMotion);
    });
    const slotCols = compact ? 3 : Math.min(8, this.slots.length), rows = Math.ceil(this.slots.length / slotCols);
    this.slots.forEach((slot, i) => {
      const row = Math.floor(i / slotCols), count = Math.min(slotCols, this.slots.length - row * slotCols);
      slot.view.setVisible(this.phase === 'build');
      slot.view.setPosition(w / 2 + (i % slotCols - (count - 1) / 2) * 115, h - 155 - (rows - 1 - row) * 120);
    });
    this.completion.resize();
  }
  dispose() {
    this.disposed = true; this.cleanups.forEach(fn => fn()); this.completion.dispose(); this.effects.dispose(); this.collector.dispose();
    this.tokens.forEach(t => t.destroy()); this.slots.forEach(s => s.destroy()); this.action.dispose();
    this.regions.destroy(); this.labels.forEach(l => l.destroy()); this.hintArrow.destroy(); this.hintGhost?.destroy(); this.events.clear(); this.ctx.audio.stopAll();
  }
}
