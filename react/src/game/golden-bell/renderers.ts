import Phaser from 'phaser';
import type { AudioService } from '../services';
import { cardPanel, friend, miniScene, picture } from './art';
import { missingOption, resolved, type AnswerInput, type Option, type Question, type QuestionRecord } from './model';

export interface RenderContext {
  scene: Phaser.Scene; audio: AudioService; reduced: boolean;
  locked: () => boolean; record: () => QuestionRecord;
  submit: (input: AnswerInput, x?: number, y?: number) => void;
  remember: (patch: { memorySeen?: boolean; draft?: string[] }) => Promise<boolean>;
  replayMemory: () => Promise<boolean>;
  prompt: (text: string) => void; feedback: (text: string) => void;
  speak: (texts: string[]) => Promise<void>;
}
export interface QuestionRenderer {
  mount(): void; dispose(): void; showHint(level: number): void; reveal(): void;
  key(key: string): void; replay(): void; isTiming(): boolean;
}
export const label = (s: Phaser.Scene, x: number, y: number, value: string, size = 28, width?: number) =>
  s.add.text(x, y, value, { fontFamily: 'Nunito, Arial', fontSize: `${size}px`, fontStyle: 'bold', color: '#345e51', align: 'center', wordWrap: width ? { width, useAdvancedWrap: true } : undefined }).setOrigin(.5).setResolution(2);

class BaseRenderer implements QuestionRenderer {
  protected root: Phaser.GameObjects.Container;
  protected timers: Phaser.Time.TimerEvent[] = [];
  protected tweens: Phaser.Tweens.Tween[] = [];
  protected cleanups: Array<() => void> = [];
  protected cards: Phaser.GameObjects.Container[] = [];
  protected alive = true;
  protected ready = true;
  protected clockReady = false;
  protected get s() { return this.ctx.scene; }
  protected get compact() { return this.s.scale.width < 1000; }
  protected get center() { return this.compact ? 450 : 620; }
  protected get width() { return this.compact ? 808 : 1110; }
  protected get bottom() { return this.compact ? 1150 : 747; }
  protected get stimulusY() { return this.compact ? 435 : 332; }
  constructor(protected q: Question, protected ctx: RenderContext) { this.root = ctx.scene.add.container(0, 0).setDepth(8); }
  protected add<T extends Phaser.GameObjects.GameObject>(object: T): T { this.root.add(object); return object; }
  protected tween(config: Phaser.Types.Tweens.TweenBuilderConfig) { if (!this.ctx.reduced) this.tweens.push(this.s.tweens.add(config)); }
  protected later(ms: number, callback: () => void) { this.timers.push(this.s.time.delayedCall(ms, () => { if (this.alive) callback(); })); }
  protected canInput() { return this.alive && this.ready && !this.ctx.locked() && !resolved(this.ctx.record()); }
  isTiming() { return this.alive && this.ready && this.clockReady; }
  protected async introduction(texts: string[]) {
    this.clockReady = false;
    await this.ctx.speak(texts);
    if (this.alive) this.clockReady = true;
  }
  protected button(x: number, y: number, w: number, h: number, text: string, callback: () => void) {
    const c = this.add(this.s.add.container(x, y, [cardPanel(this.s, w, h), label(this.s, 0, -2, text, 26, w - 25)]).setSize(w, h).setInteractive({ useHandCursor: true }));
    c.on('pointerdown', () => { if (this.canInput()) callback(); }); return c;
  }
  mount() {}
  key(_key: string) {}
  replay() { void this.ctx.speak([this.q.audioText]); }
  reveal() {
    if (this.q.answer.type !== 'option') return;
    const answer = this.q.answer.value, card = this.cards[this.q.options.findIndex(o => o.id === answer)];
    if (card) card.add(this.s.add.graphics().lineStyle(6, 0x7ea270).strokeRoundedRect(-card.width / 2 + 3, -card.height / 2 + 3, card.width - 6, card.height - 6, 24));
  }
  showHint(level: number) {
    this.ctx.feedback(this.q.hint.text);
    if (level >= 3 && this.q.answer.type === 'option') {
      const answer = this.q.answer.value, index = this.q.options.findIndex(o => o.id === answer), card = this.cards[index];
      if (card) { const halo = this.s.add.graphics().lineStyle(5, 0xc49e4f).strokeRoundedRect(-card.width / 2 + 3, -card.height / 2 + 3, card.width - 6, card.height - 6, 24); card.add(halo); }
    }
  }
  dispose() { this.alive = false; this.cleanups.forEach(fn => fn()); this.timers.forEach(t => t.remove(false)); this.tweens.forEach(t => t.remove()); this.root.destroy(true); }
}

class ChoiceRenderer extends BaseRenderer {
  protected masked?: Phaser.GameObjects.Text;
  protected optionsLayer?: Phaser.GameObjects.Container;
  protected async narrative() {
    if (this.q.questionType !== 'short_story') { await this.introduction([this.q.audioText]); return; }
    this.ready = false; this.ctx.prompt('Listen to the story.'); this.cards.forEach(c => c.setVisible(false));
    await this.introduction([this.q.audioText, this.q.promptText]);
    if (!this.alive) return;
    this.ctx.prompt(this.q.promptText); this.ready = true; this.cards.forEach(c => c.setVisible(true));
  }
  mount() { this.stimulus(); this.options(); void this.narrative(); }
  protected stimulus() {
    const q = this.q, stimulus = q.stimulus, y = this.stimulusY;
    const renderers: Record<string, () => void> = {
      image: () => this.add(picture(this.s, stimulus.assetKey!, this.center, y, 188)),
      word: () => {
        this.add(picture(this.s, stimulus.meaningAssetKey!, this.center - this.width * .32, y, 165));
        this.masked = this.add(label(this.s, this.center + 80, y, stimulus.maskedWord!, this.compact ? 44 : 55, this.width * .65));
      },
      sentence: () => {
        this.add(cardPanel(this.s, this.width - 60, 157).setPosition(this.center, y));
        this.add(label(this.s, this.center, y, stimulus.text!, 39, this.width - 105));
      },
      scene: () => {
        const objects = (stimulus.objects ?? []).filter(o => typeof o !== 'string');
        const all = objects.flatMap(o => Array.from({ length: o.count ?? 1 }, () => ({ asset: o.assetKey!, color: o.color ?? undefined })));
        const cols = Math.min(all.length, 5), rows = Math.ceil(all.length / cols);
        all.forEach((o, i) => this.add(picture(this.s, o.asset, this.center + (i % cols - (cols - 1) / 2) * 97, y + (Math.floor(i / cols) - (rows - 1) / 2) * 86, 79, o.color)));
      },
      weather: () => this.add(picture(this.s, `weather_${stimulus.weather}`, this.center, y, 173)),
      story: () => {
        const w = this.width - 30, h = this.compact ? 230 : 172;
        this.add(cardPanel(this.s, w, h).setPosition(this.center, y));
        this.add(friend(this.s, stimulus.character ?? 'pip', this.center - w / 2 + 70, y, 122));
        this.add(label(this.s, this.center + 59, y, stimulus.text ?? q.audioText, this.compact ? 26 : 27, w - 194));
      },
      characterSituation: () => this.add(friend(this.s, 'lulu', this.center, y, 175)),
    };
    renderers[stimulus.type]?.();
  }
  protected options() {
    const q = this.q, scenes = q.options.some(o => o.scene), count = q.options.length;
    const cols = scenes || this.compact ? Math.min(2, count) : count, rows = Math.ceil(count / cols);
    const w = (this.width - (cols - 1) * 20) / cols;
    const h = scenes ? (this.compact ? 279 : 227) : this.compact ? 224 : 245;
    const gap = this.compact ? 22 : 20;
    const top = this.bottom - (h * rows + gap * (rows - 1));
    this.cards = q.options.map((option, index) => {
      const x = this.center + (index % cols - (cols - 1) / 2) * (w + 20), y = top + Math.floor(index / cols) * (h + gap) + h / 2;
      const card = this.add(this.s.add.container(x, y, [cardPanel(this.s, w, h)]).setSize(w, h).setInteractive({ useHandCursor: true }));
      card.add(this.s.add.circle(-w / 2 + 28, -h / 2 + 28, 15, 0x86aa91));
      card.add(label(this.s, -w / 2 + 28, -h / 2 + 28, option.id, 18).setColor('#fffcef'));
      this.optionArt(card, option, w, h);
      card.on('pointerdown', () => this.choose(index));
      return card;
    });
  }
  protected optionArt(card: Phaser.GameObjects.Container, option: Option, w: number, h: number) {
    if (option.scene) { card.add(miniScene(this.s, option.scene, w - 35, h - 50).setPosition(0, 12)); return; }
    if (option.assets) { option.assets.forEach((asset, i) => card.add(picture(this.s, asset, (i - .5) * Math.min(w * .45, 100), 0, Math.min(w * .43, 103)))); return; }
    if (option.count !== undefined && option.assetKey) {
      const count = option.count, cols = Math.min(count, 5), rows = Math.ceil(count / cols), size = Math.min(66, (w - 45) / cols, (h - 45) / rows);
      for (let i = 0; i < count; i++) card.add(picture(this.s, option.assetKey, (i % cols - (cols - 1) / 2) * size, (Math.floor(i / cols) - (rows - 1) / 2) * size + 8, size - 4));
      return;
    }
    if (option.imageAssetKey) {
      card.add(picture(this.s, option.imageAssetKey, 0, -26, 125)); card.add(label(this.s, 0, h / 2 - 44, option.word!, 29, w - 30)); return;
    }
    if (option.assetKey) {
      const size = option.size === 'small' ? 84 : Math.min(163, w - 54, h - 50);
      const image = picture(this.s, option.assetKey, 0, 7, size, option.displayColor); card.add(image);
      if (/^actions?_/.test(option.assetKey)) {
        const action = option.assetKey.split('_').at(-1);
        if (['jump', 'fly'].includes(action!)) this.tween({ targets: image, y: -8, duration: 500, yoyo: true, repeat: -1, ease: 'Sine.easeInOut' });
        if (['dance', 'run'].includes(action!)) this.tween({ targets: image, angle: { from: -4, to: 4 }, duration: 380, yoyo: true, repeat: -1 });
      }
      return;
    }
    if (['PIP', 'POKI', 'LULU', 'MOMO', 'FOXY'].includes(option.label)) {
      card.add(friend(this.s, option.label, 0, -17, 133)); card.add(label(this.s, 0, h / 2 - 36, option.label, 25)); return;
    }
    const text = qIsSpelling(this.q) ? missingOption(this.q, option).split('').join('  ') : option.label;
    card.add(label(this.s, 0, 5, text, text.length <= 3 ? 50 : 30, w - 43));
  }
  protected choose(index: number) {
    if (!this.canInput() || !this.q.options[index]) return;
    const card = this.cards[index]; this.ctx.submit({ type: 'option', value: this.q.options[index].id }, card.x, card.y);
  }
  key(key: string) { if (/^[1-4]$/.test(key)) this.choose(Number(key) - 1); }
  replay() { void this.ctx.speak(this.q.questionType === 'short_story' ? [this.q.audioText, this.q.promptText] : [this.q.audioText]); }
  reveal() { super.reveal(); if (this.masked) this.masked.setText(this.q.targetVocabulary[0].toUpperCase().split('').join(' ')); }
}
const qIsSpelling = (q: Question) => q.questionType === 'missing_letter';

class MemoryRenderer extends ChoiceRenderer {
  private memory?: Phaser.GameObjects.Container;
  private memoryTimer?: Phaser.Time.TimerEvent;
  private showing = false;
  mount() {
    this.options();
    if (this.ctx.record().memorySeen) { this.ctx.prompt(this.q.promptText); void this.introduction([this.q.promptText]); }
    else this.present();
  }
  private present() {
    this.memory?.destroy(); this.ready = false; this.clockReady = false; this.showing = true; this.cards.forEach(c => c.setVisible(false));
    this.ctx.prompt('Look carefully. Remember what you see.');
    this.memory = this.add(this.s.add.container(this.center, this.compact ? 635 : 458));
    const people = (this.q.stimulus.objects ?? []).filter(o => typeof o !== 'string');
    const w = Math.min(310, (this.width - 44) / people.length);
    people.forEach((object, i) => {
      const x = (i - (people.length - 1) / 2) * (w + 15);
      this.memory!.add(cardPanel(this.s, w, 320).setPosition(x, 0));
      this.memory!.add(friend(this.s, object.character ?? 'pip', x - 29, -25, 188));
      this.memory!.add(picture(this.s, `vocab_${object.item}`, x + 65, 35, 109, object.color ?? undefined));
      this.memory!.add(label(this.s, x, 122, object.character ?? '', 25));
    });
    const seconds = this.add(label(this.s, this.center, this.compact ? 870 : 669, 'Nhìn kỹ nhé…', 27));
    this.memoryTimer = this.s.time.delayedCall(this.q.stimulus.showMs ?? 4500, () => {
      void this.ctx.remember({ memorySeen: true }).then(saved => {
        if (!this.alive || !saved) return;
        this.memory?.destroy(); seconds.destroy(); this.showing = false; this.ready = true;
        this.cards.forEach(c => c.setVisible(true)); this.ctx.prompt(this.q.promptText); void this.introduction([this.q.promptText]);
      }).catch(() => { if (this.alive) this.ctx.feedback('Chưa lưu được. Chạm nghe lại để thử tiếp.'); });
    });
    this.timers.push(this.memoryTimer); void this.ctx.speak([this.q.audioText]);
  }
  showHint(level: number) {
    if (this.showing) return;
    super.showHint(level);
    if (this.ctx.record().memoryReplays === 0) void this.ctx.replayMemory().then(allowed => { if (allowed && this.alive) this.present(); });
  }
  replay() { void this.ctx.speak([this.showing ? this.q.audioText : this.q.promptText]); }
}

class TwoStepRenderer extends BaseRenderer {
  private values: string[] = [];
  private chosen: string[] = [];
  private steps?: Phaser.GameObjects.Text;
  mount() {
    this.values = [...new Set(this.q.options.flatMap(o => o.sequence ?? []))];
    this.chosen = [...(this.ctx.record().draft ?? [])];
    if (this.chosen.length >= 2) this.chosen = [];
    this.steps = this.add(label(this.s, this.center, this.compact ? 412 : 310, this.stepText(), 34));
    const cols = this.compact ? 2 : Math.min(4, this.values.length), rows = Math.ceil(this.values.length / cols), w = (this.width - 24 * (cols - 1)) / cols, h = this.compact ? 239 : 225;
    this.values.forEach((value, index) => {
      const x = this.center + (index % cols - (cols - 1) / 2) * (w + 24), y = this.bottom - (rows - 1 - Math.floor(index / cols)) * (h + 20) - h / 2;
      const card = this.button(x, y, w, h, '', () => this.choose(index));
      const [color, shape] = value.split(' '); card.add(picture(this.s, `shape_${shape}`, 0, 0, Math.min(139, w - 44), color)); this.cards.push(card);
    });
    void this.introduction([this.q.audioText]);
  }
  private stepText() { return this.chosen.length ? '✓  1     →     2  …' : '1  …     →     2  …'; }
  private async choose(index: number) {
    if (!this.canInput() || !this.values[index]) return;
    const value = this.values[index], expected = this.q.answer.type === 'option' ? this.q.answer.sequence ?? [] : [];
    if (value !== expected[this.chosen.length]) {
      this.ctx.submit({ type: 'sequence', value: [...this.chosen, value] }, this.cards[index].x, this.cards[index].y); return;
    }
    const next = [...this.chosen, value];
    if (next.length === expected.length) { this.ctx.submit({ type: 'sequence', value: next }, this.cards[index].x, this.cards[index].y); return; }
    this.ready = false;
    try { if (!await this.ctx.remember({ draft: next }) || !this.alive) return; this.chosen = next; this.steps?.setText(this.stepText()); this.ctx.feedback('Đúng bước 1 rồi! Tiếp theo là hình nào?'); }
    finally { this.ready = true; }
  }
  key(key: string) { if (/^[1-6]$/.test(key)) void this.choose(Number(key) - 1); }
  reveal() { if (this.q.answer.type === 'option') this.steps?.setText(this.q.answer.sequence?.join(' → ') ?? '').setFontSize(this.compact ? 27 : 30); }
  showHint(level: number) { this.ctx.feedback(this.q.hint.text); if (level >= 3 && this.q.answer.type === 'option') { const i = this.values.indexOf(this.q.answer.sequence?.[this.chosen.length] ?? ''); const c = this.cards[i]; if (c) c.add(this.s.add.graphics().lineStyle(5, 0xc6a254).strokeRoundedRect(-c.width / 2 + 3, -c.height / 2 + 3, c.width - 6, c.height - 6, 25)); } }
}

class SentenceRenderer extends BaseRenderer {
  private tokens: Phaser.GameObjects.Container[] = [];
  private order: number[] = [];
  private homes: Array<{ x: number; y: number }> = [];
  private slots: Array<{ x: number; y: number }> = [];
  private tileWidth = 0;
  mount() {
    const tiles = this.q.stimulus.tiles ?? [];
    this.order = (this.ctx.record().draft ?? []).map(Number).filter((i, at, all) => Number.isInteger(i) && i >= 0 && i < tiles.length && all.indexOf(i) === at);
    const cols = this.compact ? Math.min(tiles.length > 6 ? 4 : 3, tiles.length) : tiles.length;
    this.tileWidth = Math.min(175, (this.width - 18 * (cols - 1)) / cols);
    const rows = Math.ceil(tiles.length / cols), slotTop = this.compact ? 453 : 332;
    tiles.forEach((word, i) => {
      const x = this.center + (i % cols - (cols - 1) / 2) * (this.tileWidth + 18), y = slotTop + Math.floor(i / cols) * 115;
      this.slots.push({ x, y }); this.homes.push({ x, y: y + rows * 127 + 28 });
      this.add(this.s.add.graphics().fillStyle(0xe3ead4, .8).fillRoundedRect(x - this.tileWidth / 2, y - 44, this.tileWidth, 88, 17).lineStyle(2, 0xaabd9c).strokeRoundedRect(x - this.tileWidth / 2, y - 44, this.tileWidth, 88, 17));
      this.add(label(this.s, x, y + 62, String(i + 1), 17).setColor('#819a7a'));
      const tile = this.add(this.s.add.container(this.homes[i].x, this.homes[i].y, [cardPanel(this.s, this.tileWidth, 88), label(this.s, 0, -3, word, 26, this.tileWidth - 15)]).setSize(this.tileWidth, 88).setInteractive({ useHandCursor: true }));
      tile.setData('token', i); this.s.input.setDraggable(tile); this.tokens.push(tile);
      tile.on('pointerup', (p: Phaser.Input.Pointer) => { if (p.getDistance() < 12 && this.canInput()) void this.tap(i); });
    });
    const start = (_p: Phaser.Input.Pointer, tile: Phaser.GameObjects.Container) => { if (this.tokens.includes(tile) && this.canInput()) tile.setDepth(25); };
    const drag = (_p: Phaser.Input.Pointer, tile: Phaser.GameObjects.Container, x: number, y: number) => { if (this.tokens.includes(tile) && this.canInput()) tile.setPosition(x, y); };
    const end = (_p: Phaser.Input.Pointer, tile: Phaser.GameObjects.Container) => {
      if (!this.tokens.includes(tile)) return;
      if (!this.canInput()) { this.arrange(); return; }
      const position = this.slots.findIndex(p => Math.abs(p.x - tile.x) < this.tileWidth / 2 + 10 && Math.abs(p.y - tile.y) < 64);
      const id = tile.getData('token') as number, next = this.order.filter(i => i !== id);
      if (position >= 0) next.splice(Math.min(position, next.length), 0, id);
      void this.keep(next);
    };
    this.s.input.on('dragstart', start).on('drag', drag).on('dragend', end);
    this.cleanups.push(() => { this.s.input.off('dragstart', start).off('drag', drag).off('dragend', end); });
    this.button(this.center - 155, this.bottom - 12, 265, 91, '↶  Xếp lại', () => void this.keep([]));
    this.button(this.center + 155, this.bottom - 12, 265, 91, 'Ghép câu  ✓', () => this.check());
    this.arrange(); void this.introduction([this.q.audioText]);
  }
  private arrange() { this.tokens.forEach((tile, id) => { const at = this.order.indexOf(id), p = at < 0 ? this.homes[id] : this.slots[at]; tile.setPosition(p.x, p.y).setDepth(0); }); }
  private async keep(next: number[]) { this.ready = false; try { if (!await this.ctx.remember({ draft: next.map(String) }) || !this.alive) return; this.order = next; this.arrange(); } finally { this.ready = true; } }
  private tap(id: number) { return this.keep(this.order.includes(id) ? this.order.filter(i => i !== id) : [...this.order, id]); }
  reveal() {
    if (this.q.answer.type !== 'sequence') return;
    const used = new Set<number>();
    this.order = this.q.answer.value.map(word => { const i = this.q.stimulus.tiles!.findIndex((tile, index) => tile === word && !used.has(index)); used.add(i); return i; });
    this.arrange();
  }
  private check() { if (!this.canInput()) return; if (this.order.length !== this.tokens.length) { this.ctx.feedback('Chọn đủ các thẻ chữ vào ô trước nhé.'); return; } this.ctx.submit({ type: 'sequence', value: this.order.map(i => this.q.stimulus.tiles![i]) }, this.center, this.slots[0].y); }
  key(key: string) { if (!this.canInput()) return; if (/^[1-9]$/.test(key) && this.tokens[Number(key) - 1]) void this.tap(Number(key) - 1); if (key === 'Enter') this.check(); if (key === 'Backspace') void this.keep(this.order.slice(0, -1)); }
  showHint(level: number) { this.ctx.feedback(this.q.hint.text); if (level >= 3 && this.q.answer.type === 'sequence') this.ctx.feedback(`Bắt đầu với: ${this.q.answer.value[0]}`); }
}

type Factory = (q: Question, ctx: RenderContext) => QuestionRenderer;
const choice: Factory = (q, ctx) => new ChoiceRenderer(q, ctx);
/** Every shipped type has an explicit registry entry; scenes never branch on question IDs. */
export const rendererRegistry: Record<Question['questionType'], Factory> = {
  action_recognition: choice, can_cannot: choice, color_object: choice, color_recognition: choice,
  compare_quantity: choice, count_objects: choice, final_reasoning: choice, function_question: choice,
  listen_find_picture: choice, memory_scene: (q, ctx) => new MemoryRenderer(q, ctx), missing_letter: choice,
  multi_clue_scene: choice, odd_one_out: choice, picture_to_word: choice, preposition_scene: choice,
  select_pair: choice, sentence_completion: choice, sentence_order: (q, ctx) => new SentenceRenderer(q, ctx),
  shape_recognition: choice, short_story: choice, simple_inference: choice, two_attribute_object: choice,
  two_step_instruction: (q, ctx) => new TwoStepRenderer(q, ctx), weather_choice: choice, word_picture_mismatch: choice,
};
