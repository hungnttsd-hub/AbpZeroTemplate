import { WordBuilderSceneController } from './word-builder/WordBuilderSceneController';
import { BalloonDartController } from './balloon-dart/BalloonDartController';
import { WordShotMechanic } from './word-shot/WordShotMechanic';
import Phaser from 'phaser';
import type { LevelDefinition, MechanicId } from '../types';
import type { AudioService, AttemptTracker } from './services';

export interface MechanicContext {
  scene: Phaser.Scene;
  audio: AudioService;
  tracker: AttemptTracker;
  reducedMotion: boolean;
  instruction: (text: string) => void;
  feedback: (text: string) => void;
  complete: () => void;
}
export interface GameMechanic { readonly id: MechanicId; mount(ctx: MechanicContext, level: LevelDefinition): void; dispose(): void }
type Card = Phaser.GameObjects.Container;
export abstract class BaseMechanic implements GameMechanic {
  abstract readonly id: MechanicId;
  protected ctx!: MechanicContext;
  protected level!: LevelDefinition;
  protected objects: Phaser.GameObjects.GameObject[] = [];
  protected cards: Card[] = [];
  protected cleanups: Array<() => void> = [];
  protected busy = false;
  private wrong = 0;
  private hinted = false;
  mount(ctx: MechanicContext, level: LevelDefinition) { this.ctx = ctx; this.level = level; }
  protected add<T extends Phaser.GameObjects.GameObject>(object: T): T { this.objects.push(object); return object; }
  protected label(x: number, y: number, text: string, size = 28) {
    return this.add(this.ctx.scene.add.text(x, y, text, { fontFamily: 'Arial, sans-serif', fontSize: `${size}px`, color: '#345b50', align: 'center', fontStyle: 'bold' }).setOrigin(.5));
  }
  protected card(x: number, y: number, value: string, correct: boolean): Card {
    const s = this.ctx.scene;
    const background = s.add.rectangle(0, 0, 232, 262, 0xffffff).setStrokeStyle(4, 0xd4e6d8);
    const art = s.textures.exists(`word:${value}`) ? s.add.image(0, -24, `word:${value}`).setDisplaySize(170, 170) : s.add.text(0, -25, '?', { fontSize: '80px', color: '#79ab92' }).setOrigin(.5);
    const text = s.add.text(0, 90, value, { fontFamily: 'Arial, sans-serif', fontSize: '30px', color: '#36574c', fontStyle: 'bold' }).setOrigin(.5);
    const card = this.add(s.add.container(x, y, [background, art, text]).setSize(240, 274).setInteractive({ useHandCursor: true }));
    card.setData({ value, correct, homeX: x, homeY: y }); this.cards.push(card);
    return card;
  }
  protected listen(event: string, fn: (...args: never[]) => void) {
    this.ctx.scene.input.on(event, fn); this.cleanups.push(() => this.ctx.scene.input.off(event, fn));
  }
  protected keyboard(fn: (index: number) => void) {
    const handler = (event: KeyboardEvent) => { if (/^[1-6]$/.test(event.key)) fn(Number(event.key) - 1); };
    this.ctx.scene.input.keyboard?.on('keydown', handler);
    this.cleanups.push(() => this.ctx.scene.input.keyboard?.off('keydown', handler));
  }
  protected answer(card: Card, done = this.ctx.complete) {
    if (this.busy) return;
    const correct = card.getData('correct') as boolean;
    const value = card.getData('value') as string;
    this.ctx.tracker.record(value, correct);
    if (correct) {
      this.busy = true; this.ctx.feedback('Great job!'); void this.ctx.audio.speak(`${value}. Great job!`);
      this.ctx.scene.tweens.add({ targets: card, scale: 1.12, duration: 180, yoyo: true });
      this.ctx.scene.time.delayedCall(650, done);
    } else {
      this.mistake(card, value);
    }
  }
  protected mistake(card: Card, spoken: string) {
    this.wrong++; this.ctx.feedback('Try again. You can do it!');
    void this.ctx.audio.speak(`${spoken}. Try again. ${this.level.instruction}`);
    this.ctx.scene.tweens.add({ targets: card, angle: { from: -4, to: 4 }, duration: 90, yoyo: true, repeat: 2, onComplete: () => card.setAngle(0) });
    if (this.wrong >= this.level.hintPolicy.afterWrongAttempts) this.hint();
  }
  protected hint() {
    if (!this.hinted) { this.ctx.tracker.hintCount++; this.hinted = true; }
    const card = this.cards.find(c => c.active && c.getData('correct'));
    if (card) {
      const background = card.list[0];
      if (background instanceof Phaser.GameObjects.Rectangle) background.setStrokeStyle(8, 0xedb747);
    }
    this.ctx.feedback('Look for the golden glow!');
  }
  dispose() {
    for (const fn of this.cleanups) fn();
    for (const object of this.objects) { this.ctx.scene.tweens.killTweensOf(object); object.destroy(); }
    this.cleanups = []; this.objects = []; this.cards = [];
  }
  protected targetCards(y = 370) {
    const targets = Phaser.Utils.Array.Shuffle([...this.level.targets]);
    return targets.map((t, i) => this.card(800 + (i - (targets.length - 1) / 2) * 280, y, t.value, t.correct));
  }
}

export class BalloonPopMechanic extends BaseMechanic {
  readonly id = 'balloon_pop' as const;
  mount(ctx: MechanicContext, level: LevelDefinition) {
    super.mount(ctx, level); ctx.feedback('Tap a balloon · Chạm vào bóng');
    const cards = this.targetCards(385);
    cards.forEach((card, i) => {
      (card.list[2] as Phaser.GameObjects.Text).setVisible(false);
      const picture = card.list[1];
      if (picture instanceof Phaser.GameObjects.Image) picture.setY(0);
      else if (picture instanceof Phaser.GameObjects.Text) picture.setVisible(false);
      this.add(ctx.scene.add.line(0, 0, card.x, card.y + 125, card.x + 15, card.y + 245, 0x99b4ab).setOrigin(0));
      (card.list[0] as Phaser.GameObjects.Rectangle).setFillStyle([0xfff0dc, 0xe4f0ff, 0xf5e7ff, 0xe4f6e9][i % 4]);
      if (!ctx.reducedMotion) ctx.scene.tweens.add({ targets: card, y: card.y - 18, duration: 1600 + i * 150, yoyo: true, repeat: -1 });
      card.on('pointerdown', () => this.answer(card));
    });
    this.keyboard(i => { if (cards[i]) this.answer(cards[i]); });
  }
}
export class DragSortMechanic extends BaseMechanic {
  readonly id = 'drag_sort' as const;
  mount(ctx: MechanicContext, level: LevelDefinition) {
    super.mount(ctx, level); ctx.feedback('Drag into the basket · Kéo vào giỏ hoặc chạm hình rồi chạm giỏ');
    const s = ctx.scene; const cards = this.targetCards(290);
    const basket = this.add(s.add.rectangle(800, 665, 425, 212, 0xf4dfb5).setStrokeStyle(6, 0xc79e65).setInteractive({ useHandCursor: true }));
    this.label(800, 665, level.targetVocabulary[0], 42);
    this.label(800, 728, '↓', 45);
    let selected: Card | undefined;
    cards.forEach(c => { s.input.setDraggable(c); c.on('pointerdown', () => { selected?.setScale(1); selected = c; c.setScale(1.04); }); });
    const submit = (card: Card) => { card.setPosition(card.getData('homeX') as number, card.getData('homeY') as number); card.setScale(1); this.answer(card); selected = undefined; };
    basket.on('pointerdown', () => { if (selected) submit(selected); });
    const drag = (_p: Phaser.Input.Pointer, object: Card, x: number, y: number) => { if (!this.busy && cards.includes(object)) object.setPosition(x, y).setDepth(5); };
    const end = (_p: Phaser.Input.Pointer, object: Card) => {
      if (!cards.includes(object)) return;
      if (Phaser.Geom.Rectangle.Contains(basket.getBounds(), object.x, object.y)) submit(object);
      else object.setPosition(object.getData('homeX') as number, object.getData('homeY') as number).setDepth(0);
    };
    s.input.on('drag', drag); s.input.on('dragend', end);
    this.cleanups.push(() => { s.input.off('drag', drag); s.input.off('dragend', end); });
    this.keyboard(i => { if (cards[i]) submit(cards[i]); });
  }
}
export class BossChallengeMechanic implements GameMechanic {
  readonly id = 'boss_challenge' as const;
  private current?: GameMechanic;
  mount(ctx: MechanicContext, level: LevelDefinition) {
    let round = 0;
    const words = [level.targetVocabulary[0], ...level.reviewVocabulary.slice(0, 2), level.targetVocabulary[0]];
    while (words.length < 4) words.splice(1, 0, level.targetVocabulary[0]);
    const mechanics = [WordShotMechanic, BalloonPopMechanic, DragSortMechanic, WordShotMechanic];
    const next = () => {
      this.current?.dispose();
      if (round === 4) { ctx.complete(); return; }
      const word = words[round];
      const values = [...new Set([word, ...level.targets.map(t => t.value), ...level.reviewVocabulary])].slice(0, 4);
      const instruction = `${round === 1 ? 'Pop' : 'Find'} the ${word}.`;
      ctx.instruction(`★ ${round + 1} / 4 · ${instruction}`); void ctx.audio.speak(instruction);
      const micro: LevelDefinition = { ...level, instruction, instructionAudioKey: '', targetVocabulary: [word], targets: values.map(value => ({ value, correct: value === word })) };
      this.current = new mechanics[round](); round++;
      this.current.mount({ ...ctx, complete: next }, micro);
    };
    next();
  }
  dispose() { this.current?.dispose(); }
}
// Reserved contracts only; intentionally not selectable in the MVP.
export const futureMechanics: Partial<Record<MechanicId, null>> = { rescue_mission: null, adventure_commands: null };
const registry: Partial<Record<MechanicId, new () => GameMechanic>> = {
  word_shot: WordShotMechanic, balloon_pop: BalloonDartController, balloon_dart: BalloonDartController, drag_sort: DragSortMechanic,
  letter_puzzle: WordBuilderSceneController, word_builder: WordBuilderSceneController, boss_challenge: BossChallengeMechanic
};
export function createMechanic(id: MechanicId): GameMechanic {
  const Constructor = registry[id]; if (!Constructor) throw new Error('Màn chơi này đang được chuẩn bị.');
  return new Constructor();
}
