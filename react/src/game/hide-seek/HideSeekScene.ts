import Phaser from 'phaser';
import { AudioService, AssetResolver } from '../services';
import { artUrl, character, prepareTheme } from '../theme';
import { coverFrames, leafSvg, objectFrames, spotNames, svgUrl, toolArt, toolLabels } from './art';
import { swipeDistance } from './content';
import { forestLayout, type SpotLayout } from './layout';
import type { HideSeekController } from './controller';

interface SpotView { layout: SpotLayout; cover: Phaser.GameObjects.Image; opened: Phaser.GameObjects.Image; object: Phaser.GameObjects.Image; zone: Phaser.GameObjects.Zone; baseScale: number; hollow: boolean }
export class HideSeekScene extends Phaser.Scene {
  private views = new Map<string, SpotView>();
  private audio: AudioService;
  private background?: Phaser.GameObjects.Image;
  private momo?: Phaser.GameObjects.Image;
  private disposeSubscription?: () => void;
  private lastPhaseKey = '';
  private generation = 0;
  private gesture?: { id: number; spotId: string; origin: number; direction: number };
  private effects: Phaser.GameObjects.GameObject[] = [];
  private initialized = false;
  private failed = false;
  private audioUnlocked = false;
  private previousPaused = false;
  private momoHomeY = 0;
  private reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
  tapToOpen = false;
  constructor(readonly controller: HideSeekController, muted: boolean,
    private callbacks: { ready: (scene: HideSeekScene) => void; hint: (text: string) => void; audioUnavailable: () => void; error: (text: string) => void }) {
    super('hide-seek');
    this.audio = new AudioService(new AssetResolver(), muted, callbacks.audioUnavailable);
  }
  preload() {
    this.load.image('hs:forest', artUrl('hide-seek-forest-v1.png'));
    this.load.image('hs:covers', artUrl('hide-seek-covers-v1.png'));
    this.load.image('hs:open-covers', artUrl('hide-seek-open-covers-v1.png'));
    this.load.image('hs:objects', artUrl('hide-seek-objects-v1.png'));
    this.load.image('art:friends', artUrl('friends-v1.png'));
    for (const [key, value] of Object.entries(toolArt)) this.load.svg(`hs:${key}`, svgUrl(value));
    this.load.svg('hs:leaf', svgUrl(leafSvg));
    this.load.on('loaderror', () => { this.failed = true; });
  }
  create() {
    if (this.failed || ['hs:forest', 'hs:covers', 'hs:open-covers', 'hs:objects', 'art:friends'].some(k => !this.textures.exists(k))) {
      this.callbacks.error('Chưa tải đủ hình của khu rừng. Hãy kết nối lại rồi mở màn; lượt chơi vẫn được giữ.'); return;
    }
    this.prepareAtlas('hs:covers', 3, 2, ['0', '1', '2', '3', '4', '5']);
    this.prepareAtlas('hs:open-covers', 3, 2, ['0', '1', '2', '3', '4', '5']);
    this.prepareAtlas('hs:objects', 4, 2, objectFrames);
    prepareTheme(this);
    this.initialized = true;
    this.background = this.add.image(0, 0, 'hs:forest').setOrigin(0).setDepth(0);
    this.drawWorld();
    this.disposeSubscription = this.controller.subscribe(() => this.reflectState());
    this.input.on('pointermove', this.pointerMove, this);
    this.input.on('pointerup', this.pointerUp, this);
    this.input.on('pointerupoutside', this.cancelGesture, this);
    this.game.canvas.addEventListener('pointercancel', this.cancelGesture);
    this.game.canvas.addEventListener('touchcancel', this.cancelGesture);
    this.scale.on('resize', this.resizeWorld, this);
    const cleanup = () => {
      if (!this.initialized) return;
      this.initialized = false; this.generation++; this.audio.stopAll(); this.disposeSubscription?.();
      this.game.canvas.removeEventListener('pointercancel', this.cancelGesture);
      this.game.canvas.removeEventListener('touchcancel', this.cancelGesture);
      this.scale.off('resize', this.resizeWorld, this);
      this.input.off('pointermove', this.pointerMove, this); this.input.off('pointerup', this.pointerUp, this);
      this.input.off('pointerupoutside', this.cancelGesture, this);
    };
    this.events.once('shutdown', cleanup);
    this.events.once('destroy', cleanup);
    this.reflectState(); this.callbacks.ready(this); this.replay();
  }
  private prepareAtlas(key: string, columns: number, rows: number, names: string[]) {
    const texture = this.textures.get(key), source = texture.getSourceImage();
    const w = Math.floor(source.width / columns), h = Math.floor(source.height / rows);
    names.forEach((name, i) => { if (!texture.has(name)) texture.add(name, 0, i % columns * w, Math.floor(i / columns) * h, w, h); });
  }
  private fit(image: Phaser.GameObjects.Image, size: number) { image.setScale(size / Math.max(image.frame.realWidth, image.frame.realHeight)); return image; }
  private drawWorld() {
    this.generation++; this.tweens.killAll(); this.time.removeAllEvents(); this.effects.forEach(e => e.destroy()); this.effects = [];
    for (const view of this.views.values()) { view.cover.destroy(); view.opened.destroy(); view.object.destroy(); view.zone.destroy(); }
    this.views.clear(); this.momo?.destroy();
    const w = this.scale.width, h = this.scale.height, layout = forestLayout(w, h, this.controller.level);
    const bg = this.background!; bg.setScale(Math.max(w / bg.frame.realWidth, h / bg.frame.realHeight)); bg.setPosition((w - bg.displayWidth) / 2, (h - bg.displayHeight) / 2);
    this.momo = character(this, 'momo', layout.momo.x, layout.momo.y, layout.momo.height).setDepth(50);
    this.momoHomeY = this.momo.y;
    for (const rect of layout.spots) {
      const spot = this.controller.level.spots.find(s => s.id === rect.id)!;
      if (!objectFrames.includes(spot.entityId)) { this.callbacks.error(`Chưa có hình cho nội dung ${spot.entityId}.`); return; }
      // Hidden entities are invisible and have no interactive hitbox at any time.
      const hollow = spot.kind === 'tree_hollow' || spot.kind === 'hollow_log';
      const object = this.fit(this.add.image(rect.x + rect.width / 2, rect.y + rect.height * (hollow ? .59 : .5), 'hs:objects', spot.entityId), rect.width * (hollow ? .69 : .84)).setDepth(20).setVisible(false);
      const opened = this.fit(this.add.image(rect.x + rect.width / 2, rect.y + rect.height / 2, 'hs:open-covers', String(coverFrames[spot.kind])), rect.width * 1.10).setDepth(19).setVisible(false);
      const cover = this.fit(this.add.image(rect.x + rect.width / 2, rect.y + rect.height / 2, 'hs:covers', String(coverFrames[spot.kind])), rect.width * 1.10).setDepth(30);
      const zone = this.add.zone(rect.x + rect.width / 2, rect.y + rect.height / 2, Math.max(56, rect.width * .88), Math.max(56, rect.height * .88)).setDepth(31).setInteractive({ useHandCursor: true });
      const view = { layout: rect, object, cover, opened, zone, baseScale: cover.scaleX, hollow }; this.views.set(spot.id, view);
      zone.on('pointerdown', (pointer: Phaser.Input.Pointer) => this.pointerDown(spot.id, pointer));
    }
    this.lastPhaseKey = ''; this.reflectState();
  }
  private resizeWorld() { if (this.initialized) { this.cancelGesture(); this.drawWorld(); } }
  private available() { return !this.controller.state.paused && !this.controller.error && this.controller.state.phase === 'searching'; }
  private pointerDown(spotId: string, pointer: Phaser.Input.Pointer) {
    if (!this.available() || this.gesture || this.controller.state.revealedSpotIds.includes(spotId)) return;
    this.unlockAudio();
    const spot = this.controller.level.spots.find(s => s.id === spotId)!;
    this.tweens.killTweensOf(this.views.get(spotId)!.cover);
    if (!spot.allowedTools.includes(this.controller.session.selectedTool)) { this.wrongTool(spotId); return; }
    if (this.controller.session.selectedTool !== 'swipe_leaves' || this.tapToOpen) { this.controller.reveal(spotId); return; }
    this.gesture = { id: pointer.id, spotId, origin: pointer.x, direction: 1 };
  }
  private pointerMove(pointer: Phaser.Input.Pointer) {
    const gesture = this.gesture;
    if (!gesture || gesture.id !== pointer.id || !pointer.isDown || !this.available()) return;
    const view = this.views.get(gesture.spotId)!;
    // Convert to design units so the swipe threshold stays comfortable on phones.
    const scale = view.layout.width / 280, distance = swipeDistance(gesture.origin / scale, pointer.x / scale, 280);
    gesture.direction = Math.sign(distance.displacement) || 1;
    view.cover.x = view.layout.x + view.layout.width / 2 + distance.displacement * scale * .5;
    view.cover.angle = distance.displacement / 96 * 9;
    if (distance.complete) { this.gesture = undefined; this.controller.reveal(gesture.spotId); }
  }
  private pointerUp(pointer: Phaser.Input.Pointer) { if (pointer.id === this.gesture?.id) this.cancelGesture(); }
  cancelGesture = () => {
    const gesture = this.gesture; this.gesture = undefined;
    if (!gesture) return;
    const view = this.views.get(gesture.spotId); if (!view) return;
    // A cancelled gesture never changes rules state or awards a reveal.
    const x = view.layout.x + view.layout.width / 2, y = view.layout.y + view.layout.height / 2;
    this.tweens.killTweensOf(view.cover);
    if (this.reduced || this.controller.state.paused) view.cover.setPosition(x, y).setAngle(0);
    else this.tweens.add({ targets: view.cover, x, y, angle: 0, duration: 180, ease: 'Sine.easeOut' });
  };
  openAccessible(spotId: string) {
    if (!this.available()) return; this.unlockAudio();
    if (!this.controller.reveal(spotId)) this.wrongTool(spotId);
  }
  private wrongTool(spotId: string) {
    const spot = this.controller.level.spots.find(s => s.id === spotId)!;
    this.callbacks.hint(`${spotNames[spot.kind]}: dùng ${spot.allowedTools.map(t => toolLabels[t].vi.toLowerCase()).join(' hoặc ')}.`);
    const cover = this.views.get(spotId)?.cover;
    if (cover && !this.reduced) this.tweens.add({ targets: cover, angle: { from: -3, to: 3 }, duration: 110, yoyo: true, repeat: 1, onComplete: () => cover.setAngle(0) });
  }
  private reflectState() {
    if (!this.initialized) return;
    const state = this.controller.state;
    this.input.enabled = !state.paused && !this.controller.error;
    if (state.paused || this.controller.error) { this.cancelGesture(); this.tweens.pauseAll(); this.time.paused = true; this.audio.stopAll(); }
    else { this.tweens.resumeAll(); this.time.paused = false; }
    const resumed = this.previousPaused && !state.paused; this.previousPaused = state.paused;
    for (const [id, view] of this.views) {
      const visible = state.revealedSpotIds.includes(id);
      view.object.setVisible(visible);
      if (view.zone.input) view.zone.input.enabled = this.available() && !visible;
      if (visible) {
        view.cover.setVisible(false); view.opened.setVisible(true).setAlpha(1);
        view.object.setDepth(35);
      }
    }
    const key = `${state.phase}:${state.pending?.id ?? ''}:${state.feedback?.answerEventId ?? ''}`;
    if (key === this.lastPhaseKey) { if (resumed) this.replay(); return; }
    this.lastPhaseKey = key;
    if (state.phase === 'revealing' && state.pending) this.animateReveal(state.pending.id, state.pending.spotId);
    if (['asking', 'assisted_retry', 'feedback'].includes(state.phase) && !state.paused) this.replay();
    if (state.phase === 'searching') this.callbacks.hint(toolLabels[this.controller.session.selectedTool].hint);
    if (state.phase === 'feedback' && this.momo && !this.reduced) {
      this.tweens.killTweensOf(this.momo);
      this.tweens.add({ targets: this.momo, y: this.momoHomeY - (state.feedback?.correct ? 18 : 6), angle: { from: -2, to: 2 },
        duration: 240, yoyo: true, repeat: state.feedback?.correct ? 1 : 0, ease: 'Sine.easeInOut', onComplete: () => this.momo?.setY(this.momoHomeY).setAngle(0) });
    }
    if (state.phase === 'completed') this.celebrate();
  }
  private animateReveal(revealId: string, spotId: string) {
    const view = this.views.get(spotId)!, generation = this.generation, tool = this.controller.session.revealTool!;
    const rect = view.layout, x = rect.x + rect.width / 2, y = rect.y + rect.height / 2;
    view.opened.setVisible(true).setAlpha(0);
    const origin = { x: this.scale.width / 2, y: this.scale.height - 70 };
    const icon = this.fit(this.add.image(origin.x, origin.y, `hs:${tool}`), Math.max(52, rect.width * .42)).setDepth(42).setVisible(!this.reduced);
    this.effects.push(icon);
    const particles = Array.from({ length: tool === 'blast_berry' ? 4 : 5 }, (_, i) => {
      const particle = tool === 'blast_berry'
        ? this.add.ellipse(x, y, rect.width * .16, rect.width * .12, [0xd6d9c3, 0xb6c3a4, 0xf0e4be, 0x879c8c][i]).setVisible(false).setDepth(41)
        : this.fit(this.add.image(x, y, 'hs:leaf'), rect.width * .17).setVisible(false).setDepth(41);
      this.effects.push(particle); return particle;
    });
    view.cover.setAngle(0);
    this.tweens.addCounter({ from: 0, to: 1, duration: this.reduced ? 220 : tool === 'blast_berry' ? 980 : tool === 'throw_net' ? 920 : 760,
      onUpdate: tween => {
        if (generation !== this.generation) return;
        const t = tween.getValue() ?? 0;
        view.opened.setAlpha(t);
        if (this.reduced) { view.cover.setAlpha(1 - t); return; }
        const flight = Math.min(1, t / .37), lift = Math.max(0, Math.min(1, (t - .37) / .50));
        icon.setPosition(Phaser.Math.Linear(origin.x, x, flight), Phaser.Math.Linear(origin.y, y - rect.height * .1, flight) - Math.sin(flight * Math.PI) * rect.height * .8);
        icon.setRotation(tool === 'blast_berry' ? t * 5 : -.4 + t * .8);
        icon.setAlpha(t < .60 ? 1 : Math.max(0, 1 - (t - .6) / .18));
        if (tool === 'swipe_leaves') {
          icon.setVisible(false); view.cover.setPosition(x + (view.hollow ? 0 : rect.width * .54 * t), y + (view.hollow ? 0 : Math.sin(t * Math.PI) * 12)).setAngle(view.hollow ? 0 : t * 17).setAlpha(1 - t);
        } else if (tool === 'throw_net') {
          view.cover.setY(y - (view.hollow ? 0 : rect.height * .5 * lift)).setAlpha(1 - lift);
          if (t > .37) icon.setY(y - rect.height * .5 * lift - rect.height * .22);
        } else { view.cover.setScale(view.baseScale * (1 + Math.sin(lift * Math.PI) * .06)).setAlpha(1 - lift); }
        particles.forEach((particle, i) => {
          const spread = tool === 'swipe_leaves' ? t : lift;
          particle.setVisible(spread > .05); particle.setPosition(x + Math.cos(i * 1.9) * rect.width * spread * .7, y + Math.sin(i * 1.9) * rect.height * spread * .5 - spread * 20);
          particle.setAlpha(Math.max(0, 1 - spread)).setRotation(spread * (i % 2 ? 2 : -2));
        });
      }, onComplete: () => {
        icon.destroy(); particles.forEach(p => p.destroy());
        if (generation === this.generation && this.initialized) this.controller.finish(revealId);
      } });
    if (this.controller.state.paused) this.tweens.pauseAll();
  }
  private celebrate() {
    const momo = this.momo!;
    const targetSpot = this.controller.level.spots.find(s => s.entityId === this.controller.level.targetEntityId)!;
    const view = this.views.get(targetSpot.id)!;
    const x = Math.max(momo.displayWidth / 2, view.object.x - view.object.displayWidth * .8);
    if (!this.reduced) {
      this.tweens.add({ targets: momo, x, y: view.object.y + view.layout.height * .2, duration: 850, ease: 'Sine.easeInOut' });
      this.tweens.add({ targets: view.object, angle: { from: -7, to: 7 }, yoyo: true, repeat: 2, duration: 200, onComplete: () => view.object.setAngle(0) });
    } else momo.setPosition(x, view.object.y);
    if (this.audioUnlocked && !this.controller.state.paused) void this.audio.speak('You found it!');
  }
  unlockAudio() { this.audioUnlocked = true; }
  replay() {
    if (!this.initialized || this.controller.state.paused) return;
    this.unlockAudio();
    const state = this.controller.state;
    const text = state.phase === 'feedback' ? state.feedback!.phrase : ['asking', 'assisted_retry'].includes(state.phase) ? this.controller.target.question : this.controller.target.quest;
    void this.audio.speak(text).catch(this.callbacks.audioUnavailable);
  }
  setMuted(value: boolean) { this.audio.muted = value; if (value) this.audio.stopAll(); }
  update(_time: number, delta: number) {
    if (!this.initialized || this.controller.state.paused) return;
    this.controller.tick(delta);
    if (this.reduced || this.controller.state.phase !== 'searching' || this.gesture) return;
    if (this.momo) this.momo.y = this.momoHomeY + Math.sin(this.time.now / 650) * 2;
    let i = 0;
    for (const [id, view] of this.views) {
      if (!this.controller.state.revealedSpotIds.includes(id)) view.cover.angle = Math.sin(this.time.now / 1900 + i * 1.6) * .75;
      i++;
    }
  }
}
