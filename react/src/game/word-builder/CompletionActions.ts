import Phaser from 'phaser';
import type { CompletionActionType, WordBuilderConfig } from './config';
import { WordBuilderEffects } from './WordBuilderEffects';

export interface CompletionAction {
  readonly view: Phaser.GameObjects.Container;
  play(done: () => void): void;
  setMeaningVisible(visible: boolean): void;
  dispose(): void;
}
function friend(scene: Phaser.Scene) {
  const g = scene.add.graphics();
  g.fillStyle(0xaac998).fillEllipse(0, 12, 56, 68).fillCircle(0, -24, 31);
  g.fillStyle(0xe6efd2).fillEllipse(0, 21, 28, 36);
  g.fillStyle(0x45654b).fillCircle(-11, -29, 4).fillCircle(11, -29, 4);
  g.fillStyle(0xe7c075).fillTriangle(-21, -49, -13, -69, -4, -49).fillTriangle(6, -49, 18, -67, 25, -46);
  return g;
}
abstract class ActionBase implements CompletionAction {
  readonly view: Phaser.GameObjects.Container;
  protected actor: Phaser.GameObjects.Graphics;
  protected picture: Phaser.GameObjects.Image | Phaser.GameObjects.Text;
  constructor(protected scene: Phaser.Scene, protected effects: WordBuilderEffects, config: WordBuilderConfig) {
    this.actor = friend(scene).setPosition(-130, 40);
    const texture = scene.textures.exists('wb:meaning') ? 'wb:meaning' : `word:${config.targetWord.toLowerCase()}`;
    this.picture = scene.textures.exists(texture) ? scene.add.image(115, 25, texture).setDisplaySize(84, 84) : scene.add.text(105, 20, '★', { fontSize: '60px', color: '#dfba69' }).setOrigin(.5);
    const ground = scene.add.rectangle(0, 97, 360, 18, 0x9fbe8b).setOrigin(.5);
    this.view = scene.add.container(0, 0, [ground, this.actor, this.picture]).setDepth(2);
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
  setMeaningVisible(visible: boolean) { this.picture.setVisible(visible); }
  dispose() { this.view.destroy(); }
}
export class OpenDoorAction extends ActionBase {
  private door = this.add(this.scene.add.rectangle(55, 12, 110, 152, 0xb99473).setStrokeStyle(6, 0xd5b58a));
  play(done: () => void) { this.animate({ targets: this.door, scaleX: .05, alpha: .3, duration: this.effects.reduced ? 150 : 1100 }); this.animate({ targets: this.actor, x: 100, duration: 1700 }); this.celebrate(done); }
}
export class BuildBridgeAction extends ActionBase {
  private planks: Phaser.GameObjects.Rectangle[] = [];
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c);
    this.add(s.add.rectangle(0, 105, 150, 38, 0xa8d9e1));
    for (let i = 0; i < 5; i++) this.planks.push(this.add(s.add.rectangle(-60 + i * 30, 87, 26, 15, [0xeea3a0, 0xf1c571, 0xa9c587, 0x8ab9c9, 0xb59fd7][i]).setAlpha(.1)));
  }
  play(done: () => void) {
    this.planks.forEach((p, i) => this.effects.later(i * 180, () => this.animate({ targets: p, alpha: 1, y: 77, duration: this.effects.reduced ? 100 : 400 })));
    this.effects.later(1100, () => this.animate({ targets: this.actor, x: 100, duration: this.effects.reduced ? 150 : 1400 })); this.celebrate(done);
  }
}
export class ReleaseAnimalAction extends ActionBase {
  private cage: Phaser.GameObjects.Graphics;
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c); this.cage = this.add(s.add.graphics());
    this.cage.lineStyle(7, 0xa1a789).strokeRoundedRect(50, -47, 133, 129, 18);
    for (let x = 70; x < 180; x += 25) this.cage.lineBetween(x, -45, x, 81);
  }
  play(done: () => void) { this.animate({ targets: this.cage, y: -135, alpha: 0, duration: 1000 }); this.effects.later(1000, () => this.animate({ targets: this.picture, x: -38, duration: this.effects.reduced ? 100 : 1400 })); this.celebrate(done); }
}
export class StartVehicleAction extends ActionBase {
  private vehicle: Phaser.GameObjects.Container;
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c); const art = s.add.graphics().fillStyle(0x93b8ab).fillRoundedRect(-75, -12, 160, 65, 12).fillRect(5, -60, 65, 70);
    art.fillStyle(0xf7e8b7).fillRect(15, -50, 38, 30).fillStyle(0x566f69).fillCircle(-40, 55, 17).fillCircle(48, 55, 17);
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
    this.picture.setPosition(60, 36).setVisible(false);
  }
  play(done: () => void) { this.picture.setVisible(true); this.animate({ targets: this.picture, y: -34, duration: this.effects.reduced ? 150 : 1400 }); this.effects.sparkle(this.view.x + 65, this.view.y); this.celebrate(done); }
}
export class PowerMachineAction extends ActionBase {
  private light = this.add(this.scene.add.circle(80, 0, 92, 0xffdf83, .04));
  play(done: () => void) { this.view.sendToBack(this.light); this.animate({ targets: this.light, alpha: .65, scale: 1.2, duration: this.effects.reduced ? 150 : 1800 }); this.animate({ targets: this.picture, y: -12, duration: 800, yoyo: true }); this.celebrate(done); }
}
export class RevealTreasureAction extends ActionBase {
  private lid: Phaser.GameObjects.Rectangle;
  constructor(s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) {
    super(s, e, c); this.add(s.add.rectangle(90, 55, 140, 70, 0xc29970).setStrokeStyle(5, 0xa68562));
    this.lid = this.add(s.add.rectangle(90, 7, 144, 28, 0xe0ba81).setStrokeStyle(4, 0xa68562)); this.picture.setAlpha(0);
  }
  play(done: () => void) { this.animate({ targets: this.lid, y: -55, angle: -15, duration: 800 }); this.animate({ targets: this.picture, alpha: 1, y: -40, duration: this.effects.reduced ? 150 : 1700 }); this.celebrate(done); }
}
export const completionActions: Record<CompletionActionType, new (s: Phaser.Scene, e: WordBuilderEffects, c: WordBuilderConfig) => CompletionAction> = {
  open_door: OpenDoorAction, build_bridge: BuildBridgeAction, release_animal: ReleaseAnimalAction, start_vehicle: StartVehicleAction,
  grow_plant: GrowPlantAction, cook_food: CookFoodAction, power_machine: PowerMachineAction, reveal_treasure: RevealTreasureAction
};

