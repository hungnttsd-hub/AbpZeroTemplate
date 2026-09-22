import Phaser from 'phaser';
import { character, woodPanel } from '../theme';
import type { CompletionActionType, WordBuilderConfig } from './config';
import { WordBuilderEffects } from './WordBuilderEffects';

export interface CompletionAction {
  readonly view: Phaser.GameObjects.Container;
  play(done: () => void): void;
  setMeaningVisible(visible: boolean): void;
  onCollected?(count: number, total: number): void;
  onPlaced?(index: number): void;
  onWrong?(): void;
  dispose(): void;
}
function friend(scene: Phaser.Scene) { return character(scene, 'momo', 0, 0, 125); }
abstract class ActionBase implements CompletionAction {
  readonly view: Phaser.GameObjects.Container;
  protected actor: Phaser.GameObjects.Image;
  protected picture: Phaser.GameObjects.Image | Phaser.GameObjects.Text;
  private pointing: Phaser.GameObjects.Graphics;
  constructor(protected scene: Phaser.Scene, protected effects: WordBuilderEffects, config: WordBuilderConfig) {
    this.actor = friend(scene).setPosition(-130, 40);
    const texture = scene.textures.exists('wb:meaning') ? 'wb:meaning' : `word:${config.meaningWord ?? config.targetWord.toLowerCase()}`;
    this.picture = scene.textures.exists(texture) ? scene.add.image(115, 25, texture).setDisplaySize(84, 84) : scene.add.text(105, 20, '★', { fontSize: '60px', color: '#dfba69' }).setOrigin(.5);
    const ground = scene.add.graphics();
    ground.fillStyle(0x466850, .15).fillEllipse(0, 123, 358, 25);
    ground.fillStyle(0xbb9163).fillRoundedRect(-180, 91, 360, 28, 12);
    ground.fillStyle(0x77aa68).fillRoundedRect(-181, 87, 362, 16, 8);
    ground.fillStyle(0xb7d985).fillRoundedRect(-173, 88, 346, 6, 3);
    ground.fillStyle(0xe5bd85).fillCircle(-153, 110, 3).fillCircle(155, 112, 3).fillCircle(-98, 108, 2);
    this.view = scene.add.container(0, 0, [ground, this.actor, this.picture]).setDepth(2);
    this.pointing = scene.add.graphics().lineStyle(6, 0xe2b864).lineBetween(-82, 5, -30, 5).fillStyle(0xe2b864).fillTriangle(-25, 5, -40, -4, -40, 14).setVisible(false);
    this.view.add(this.pointing);
    this.view.setData('target', config.completionAction.target);
  }
  protected add<T extends Phaser.GameObjects.GameObject>(object: T): T { this.view.add(object); return object; }
  protected animate(config: Phaser.Types.Tweens.TweenBuilderConfig) {
    if (!this.effects.reduced) return this.effects.tween(config);
    // Apply the final world state immediately, then fade instead of travelling.
    const tween = this.effects.tween({ ...config, duration: 0, repeat: 0, yoyo: false });
    this.view.setAlpha(.65);
    this.effects.tween({ targets: this.view, alpha: 1, duration: 150 });
    return tween;
  }
  protected celebrate(done: () => void) {
    this.effects.sparkle(this.view.x + 115, this.view.y);
    this.animate({ targets: this.actor, y: 20, duration: this.effects.reduced ? 150 : 260, yoyo: true, repeat: this.effects.reduced ? 0 : 2 });
    this.effects.later(3000, done);
  }
  abstract play(done: () => void): void;
  onCollected(count: number, total: number) {
    this.pointing.setVisible(count === total - 1);
    this.effects.bounce(this.view);
    this.effects.tween({ targets: this.actor, angle: count === total - 1 ? 12 : -5, duration: 180, yoyo: true });
  }
  onPlaced(_index: number) { this.pointing.setVisible(false); this.effects.tween({ targets: this.actor, y: 25, duration: 150, yoyo: true }); }
  onWrong() { this.effects.tween({ targets: this.actor, angle: -4, duration: 120, yoyo: true }); }
  setMeaningVisible(visible: boolean) { this.picture.setVisible(visible); }
  dispose() { this.view.destroy(); }
}
export class OpenDoorAction extends ActionBase {
  private door: Phaser.GameObjects.Container;
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c);
    this.add(woodPanel(s, 126, 174, 0xebd2a0).setPosition(55, 4));
    this.add(s.add.graphics().fillStyle(0x406e64).fillRoundedRect(5, -58, 100, 144, 10));
    const surface = woodPanel(s, 104, 150, 0x7daf9d);
    const detail = s.add.graphics().lineStyle(3, 0x4f8876).strokeRoundedRect(-35, -58, 70, 77, 9);
    detail.fillStyle(0xffdf83).fillCircle(34, 34, 8).fillStyle(0xfff5ce).fillCircle(31, 31, 3);
    this.door = this.add(s.add.container(55, 12, [surface, detail]));
  }
  play(done: () => void) { this.animate({ targets: this.door, scaleX: .05, alpha: .3, duration: this.effects.reduced ? 150 : 1100 }); this.animate({ targets: this.actor, x: 100, duration: 1700 }); this.celebrate(done); }
}
export class BuildBridgeAction extends ActionBase {
  private planks: Phaser.GameObjects.Graphics[] = [];
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c);
    const river = s.add.graphics().fillStyle(0x6bbfcf).fillRoundedRect(-84, 80, 168, 66, 10);
    river.fillStyle(0xa4e0e5).fillRoundedRect(-84, 80, 168, 15, 7);
    river.lineStyle(3, 0xe1f8ed, .75).lineBetween(-67, 125, -27, 125).lineBetween(11, 137, 57, 137).lineBetween(23, 119, 69, 119);
    this.add(river);
    const posts = s.add.graphics().fillStyle(0xb18a61).fillRoundedRect(-91, 74, 10, 52, 4).fillRoundedRect(81, 74, 10, 52, 4);
    posts.fillStyle(0xf0ce89).fillCircle(-86, 77, 6).fillCircle(86, 77, 6); this.add(posts);
    for (let i = 0; i < c.slots; i++) {
      const width = 156 / c.slots - 3;
      const plank = s.add.graphics().fillStyle(0x8e6944).fillRoundedRect(-width / 2, -5, width, 20, 3);
      plank.fillStyle([0xedb48c, 0xf4d389, 0xb7d99c, 0xa4d3d6, 0xccb7e3][i % 5]).fillRoundedRect(-width / 2, -9, width, 17, 3);
      plank.lineStyle(2, 0xfff2cc, .8).lineBetween(-width / 2 + 3, -5, width / 2 - 3, -5);
      this.planks.push(this.add(plank.setPosition(-78 + (i + .5) * 156 / c.slots, 88).setAlpha(0)));
    }
    this.view.bringToTop(this.actor); this.view.bringToTop(this.picture);
  }
  onPlaced(index: number) {
    const plank = this.planks[index]; if (!plank) return;
    this.effects.tween({ targets: plank, alpha: 1, duration: 220 });
    super.onPlaced(index);
  }
  onCollected(count: number, total: number) {
    super.onCollected(count, total);
    this.animate({ targets: this.actor, x: -112, duration: 250 });
  }
  play(done: () => void) {
    this.planks.forEach(p => p.setAlpha(1));
    this.animate({ targets: this.actor, x: 112, duration: 1300, onComplete: () => {
      this.picture.setVisible(false);
      this.effects.tween({ targets: this.actor, y: this.effects.reduced ? 36 : 12, duration: 220, yoyo: true, repeat: this.effects.reduced ? 0 : 1 });
      this.effects.sparkle(this.view.x + 112, this.view.y + 15);
      this.effects.later(1000, done);
    } });
  }
}
export class ReleaseAnimalAction extends ActionBase {
  private cage: Phaser.GameObjects.Graphics;
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c); this.cage = this.add(s.add.graphics());
    this.cage.lineStyle(7, 0x719895).strokeRoundedRect(50, -47, 133, 129, 18);
    for (let x = 70; x < 180; x += 25) this.cage.lineBetween(x, -45, x, 81);
    this.cage.lineStyle(3, 0xbbe1c9).strokeRoundedRect(48, -49, 133, 129, 18);
    this.cage.fillStyle(0xf6d792).fillRoundedRect(106, 5, 27, 32, 7).fillStyle(0x957852).fillCircle(119, 19, 4);
  }
  play(done: () => void) { this.animate({ targets: this.cage, y: -135, alpha: 0, duration: 1000 }); this.effects.later(1000, () => this.animate({ targets: this.picture, x: -38, duration: this.effects.reduced ? 100 : 1400 })); this.celebrate(done); }
}
export class StartVehicleAction extends ActionBase {
  private vehicle: Phaser.GameObjects.Container;
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c); const art = s.add.graphics().fillStyle(0x93b8ab).fillRoundedRect(-75, -12, 160, 65, 12).fillRect(5, -60, 65, 70);
    art.fillStyle(0xf7e8b7).fillRect(15, -50, 38, 30).fillStyle(0x566f69).fillCircle(-40, 55, 17).fillCircle(48, 55, 17);
    art.fillStyle(0xc6e8c7).fillRoundedRect(-67, -9, 65, 9, 4);
    art.fillStyle(0xe9c77b).fillCircle(-40, 55, 10).fillCircle(48, 55, 10);
    art.fillStyle(0xffffdd, .8).fillCircle(-43, 51, 4).fillCircle(45, 51, 4);
    this.vehicle = this.add(s.add.container(50, 20, [art])); this.picture.setPosition(55, -54).setScale(.3);
  }
  play(done: () => void) { this.animate({ targets: [this.vehicle, this.picture], x: '+=115', duration: this.effects.reduced ? 150 : 2300 }); this.celebrate(done); }
}
export class GrowPlantAction extends ActionBase {
  private plant: Phaser.GameObjects.Graphics;
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c); this.plant = this.add(s.add.graphics().setPosition(20, 80).setScale(.15));
    this.plant.lineStyle(10, 0x7ba175).lineBetween(0, 0, 0, -125).fillStyle(0xaecf8c).fillEllipse(-22, -45, 48, 25).fillEllipse(22, -77, 48, 25);
    this.plant.fillStyle(0xedb0bf).fillCircle(-18, -130, 23).fillCircle(18, -130, 23).fillCircle(0, -150, 23).fillStyle(0xf4d079).fillCircle(0, -130, 18);
  }
  play(done: () => void) { this.animate({ targets: this.plant, scale: 1, duration: this.effects.reduced ? 150 : 2000 }); this.celebrate(done); }
}
export class CookFoodAction extends ActionBase {
  private pot: Phaser.GameObjects.Graphics;
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c); this.pot = this.add(s.add.graphics().fillStyle(0xb2bac6).fillRoundedRect(-20, 20, 160, 67, 20).lineStyle(8, 0x8494a5).lineBetween(-30, 18, 151, 18));
    this.pot.fillStyle(0xe8eee9, .7).fillRoundedRect(-9, 29, 17, 39, 8).fillStyle(0x6c96a0).fillRoundedRect(12, 74, 104, 8, 4);
    this.picture.setPosition(60, 36).setVisible(false);
  }
  play(done: () => void) { this.picture.setVisible(true); this.animate({ targets: this.picture, y: -34, duration: this.effects.reduced ? 150 : 1400 }); this.effects.sparkle(this.view.x + 65, this.view.y); this.celebrate(done); }
}
export class PowerMachineAction extends ActionBase {
  private light = this.add(this.scene.add.circle(80, 0, 92, 0xffdf83, .04));
  play(done: () => void) { this.view.sendToBack(this.light); this.animate({ targets: this.light, alpha: .65, scale: 1.2, duration: this.effects.reduced ? 150 : 1800 }); this.animate({ targets: this.picture, y: -12, duration: 800, yoyo: true }); this.celebrate(done); }
}
export class RevealTreasureAction extends ActionBase {
  private lid: Phaser.GameObjects.Graphics;
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c); this.add(woodPanel(s, 140, 70, 0xc79965).setPosition(90, 55));
    this.lid = this.add(woodPanel(s, 144, 36, 0xf1d291).setPosition(90, 7)); this.picture.setAlpha(0);
  }
  play(done: () => void) { this.animate({ targets: this.lid, y: -55, angle: -15, duration: 800 }); this.animate({ targets: this.picture, alpha: 1, y: -40, duration: this.effects.reduced ? 150 : 1700 }); this.celebrate(done); }
}
export const completionActions: Record<CompletionActionType, new (s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) => CompletionAction> = {
  open_door: OpenDoorAction, build_bridge: BuildBridgeAction, release_animal: ReleaseAnimalAction, start_vehicle: StartVehicleAction,
  grow_plant: GrowPlantAction, cook_food: CookFoodAction, power_machine: PowerMachineAction, reveal_treasure: RevealTreasureAction
};
