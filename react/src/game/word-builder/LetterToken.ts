import Phaser from 'phaser';
import type { LetterDefinition } from './config';
export class LetterToken {
  readonly view: Phaser.GameObjects.Container;
  readonly cover: Phaser.GameObjects.Graphics;
  readonly background: Phaser.GameObjects.Rectangle;
  state: 'hidden' | 'available' | 'flying' | 'placed' = 'hidden';
  home = { x: 0, y: 0 };
  motion?: Phaser.Tweens.Tween;
  private body?: MatterJS.BodyType;
  constructor(private scene: Phaser.Scene, readonly definition: LetterDefinition) {
    this.background = scene.add.rectangle(0, 0, 108, 108, 0xfff8df).setStrokeStyle(4, 0xc6a879);
    const label = scene.add.text(0, 0, definition.char, { fontFamily: 'Nunito, Arial', fontStyle: 'bold', fontSize: `${definition.char.length > 1 ? 27 : 52}px`, color: '#3d614e' }).setOrigin(.5);
    this.cover = scene.add.graphics(); this.decorate();
    this.view = scene.add.container(0, 0, [this.background, label, this.cover]).setSize(120, 124).setDepth(12).setInteractive({ useHandCursor: true });
    scene.input.setDraggable(this.view);
  }
  private decorate() {
    const g = this.cover; const spawn = this.definition.spawn;
    if (spawn === 'crate' || spawn === 'obstacle') {
      g.fillStyle(spawn === 'crate' ? 0xc19a6e : 0xa5b2a2, .92).fillRoundedRect(-56, -57, 112, 114, 12);
      g.lineStyle(8, 0xe5c394).lineBetween(-39, -39, 39, 39).lineBetween(39, -39, -39, 39);
    } else if (spawn === 'balloon' || spawn === 'cloud') {
      g.fillStyle(spawn === 'balloon' ? 0xedbdce : 0xd4e9ed, .8).fillEllipse(0, -55, 90, 70);
      g.lineStyle(3, 0x9bac98).lineBetween(0, -20, 0, -5);
    } else if (spawn === 'hanging') g.lineStyle(5, 0xa9b893).lineBetween(0, -95, 0, -54);
    else if (spawn === 'creature') {
      g.fillStyle(0xa9c695).fillEllipse(0, 54, 115, 47);
      g.fillStyle(0x3f644d).fillCircle(-18, 48, 5).fillCircle(18, 48, 5);
    } else if (spawn === 'platform') g.fillStyle(0xbda885).fillRoundedRect(-61, 55, 122, 15, 5);
    else if (spawn === 'falling') g.lineStyle(4, 0xd9bb76).lineBetween(-28, -70, -28, -88).lineBetween(28, -70, 28, -88);
  }
  position(x: number, y: number, reduced: boolean) {
    this.home = { x, y }; this.motion?.stop(); this.motion = undefined;
    if (this.body) this.scene.matter.body.setPosition(this.body, { x, y });
    this.view.setPosition(x, y);
    if (this.state === 'hidden' && !reduced && ['moving_target', 'balloon', 'cloud'].includes(this.definition.spawn)) {
      this.motion = this.scene.tweens.add({ targets: this.view, y: y - 13, duration: 1100, yoyo: true, repeat: -1 });
    }
  }
  reveal() { this.motion?.stop(); this.motion = undefined; this.cover.setVisible(false); this.state = 'available'; }
  /** Only a released falling prop joins Matter. HUD letters and flight tweens never do. */
  fall(onLanded: () => void) {
    this.reveal(); this.state = 'flying';
    this.body = this.scene.matter.add.rectangle(this.view.x, this.view.y - 52, 90, 90, { restitution: .35, frictionAir: .02, collisionFilter: { group: -1 } });
    const floor = this.home.y;
    const update = () => {
      if (!this.body) return;
      if (this.body.position.y >= floor) {
        this.scene.matter.world.remove(this.body); this.body = undefined;
        this.scene.events.off('update', update); this.view.setPosition(this.home.x, floor); this.state = 'available'; onLanded();
      } else this.view.setPosition(this.body.position.x, this.body.position.y).setAngle(this.body.angle * 180 / Math.PI);
    };
    this.fallCleanup = () => this.scene.events.off('update', update);
    this.scene.events.on('update', update);
  }
  private fallCleanup?: () => void;
  select(on: boolean) { this.background.setStrokeStyle(on ? 7 : 4, on ? 0xe8b754 : 0xc6a879); }
  destroy() { this.motion?.stop(); this.fallCleanup?.(); if (this.body) this.scene.matter.world.remove(this.body); this.view.destroy(); }
}
