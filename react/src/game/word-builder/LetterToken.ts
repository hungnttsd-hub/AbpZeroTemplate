import Phaser from 'phaser';
import type { LetterDefinition } from './config';
import { woodPanel } from '../theme';
export class LetterToken {
  readonly view: Phaser.GameObjects.Container;
  readonly cover: Phaser.GameObjects.Graphics;
  readonly background: Phaser.GameObjects.Rectangle;
  state: 'hidden' | 'available' | 'flying' | 'placed' = 'hidden';
  home = { x: 0, y: 0 };
  motion?: Phaser.Tweens.Tween;
  touches = 0;
  readonly helper: Phaser.GameObjects.Text;
  readonly prop?: Phaser.GameObjects.Image;
  private tile: Phaser.GameObjects.Graphics;
  private body?: MatterJS.BodyType;
  constructor(private scene: Phaser.Scene, readonly definition: LetterDefinition) {
    this.background = scene.add.rectangle(0, 0, 108, 108, 0xfff8df).setStrokeStyle(4, 0xc6a879);
    const label = scene.add.text(0, 0, definition.char, { fontFamily: 'Nunito, Arial', fontStyle: 'bold', fontSize: `${definition.char.length > 1 ? 27 : 52}px`, color: '#3d614e' }).setOrigin(.5).setResolution(2);
    this.background.setVisible(false);
    this.tile = woodPanel(scene, 108, 108, 0xffedb4).setVisible(false);
    this.cover = scene.add.graphics(); this.decorate();
    const hints: Record<string, string> = { balloon: 'Chạm để bắn', crate: 'Gõ 3 lần', obstacle: 'Gõ 3 lần', platform: 'Bắt đúng lúc', moving_target: 'Bắt đúng lúc', bush: 'Vạch lá · 2 chạm', pulley: 'Kéo xuống / chạm 3 lần', spring: 'Nén rồi bật · 2 chạm' };
    this.helper = scene.add.text(0, 86, hints[definition.spawn] ?? 'Chạm để tìm chữ', { fontFamily: 'Nunito, Arial', fontSize: '13px', color: '#345a48', align: 'center', wordWrap: { width: 135 }, backgroundColor: '#fff8e2', padding: { x: 5, y: 3 } }).setOrigin(.5).setResolution(2);
    const kind = definition.spawn;
    if (scene.textures.exists('art:props') && ['crate', 'bush', 'spring', 'pulley'].includes(kind)) {
      this.cover.setVisible(false);
      this.prop = scene.add.image(0, kind === 'spring' ? 45 : kind === 'pulley' ? -14 : 0, 'art:props', kind).setDisplaySize(kind === 'pulley' ? 150 : 142, kind === 'spring' ? 100 : 142);
    } else if (kind === 'balloon' && scene.textures.exists('art:balloon:f5a5ca')) {
      this.cover.clear().lineStyle(3, 0xb59667).lineBetween(0, -14, 0, 14); this.prop = scene.add.image(0, -64, 'art:balloon:f5a5ca').setDisplaySize(88, 112);
    }
    if (!['crate', 'obstacle', 'bush', 'creature'].includes(kind)) {
      this.tile.setVisible(true).setScale(.66); label.setFontSize(definition.char.length > 1 ? 24 : 40);
      if (kind === 'balloon') { this.tile.setY(30); label.setY(30); }
      if (kind === 'spring') { this.tile.setY(-24); label.setY(-24); }
    }
    const props = this.prop ? [this.prop] : [];
    this.view = scene.add.container(0, 0, [this.tile, this.background, label, this.cover, ...props, this.helper]).setSize(144, 150).setDepth(12).setInteractive({ useHandCursor: true });
    // Container input coordinates include its display origin (72, 75).
    if (kind === 'balloon') this.view.input!.hitArea = new Phaser.Geom.Rectangle(0, -45, 144, 194);
    this.view.setData('label', label);
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
    } else if (spawn === 'bush') {
      g.fillStyle(0x82ad78).fillCircle(-31, 12, 36).fillCircle(28, 12, 39).fillCircle(0, -13, 37);
      g.fillStyle(0xb6ce8d).fillEllipse(-27, -10, 25, 12).fillEllipse(23, 5, 27, 14);
    } else if (spawn === 'pulley') {
      g.lineStyle(5, 0xb08a60).lineBetween(-48, -62, 48, -62).lineBetween(0, -62, 0, -32).lineBetween(45, -60, 45, 46);
      g.fillStyle(0xe3c990).fillCircle(0, -62, 12).fillRoundedRect(29, 43, 32, 15, 6);
    } else if (spawn === 'spring') {
      g.lineStyle(5, 0x7f9c95).beginPath().moveTo(-21, 30).lineTo(20, 39).lineTo(-20, 48).lineTo(20, 57).lineTo(-20, 66).lineTo(20, 72).strokePath();
      g.fillStyle(0xecc783).fillRoundedRect(-52, 24, 104, 12, 5).fillRoundedRect(-40, 74, 80, 8, 3);
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
    if (this.state === 'hidden' && !reduced && ['moving_target', 'balloon', 'cloud', 'platform', 'pulley'].includes(this.definition.spawn)) {
      this.motion = this.scene.tweens.add({ targets: this.view, x: ['platform', 'pulley', 'moving_target'].includes(this.definition.spawn) ? x + 18 : x, y: y - 10, duration: 1100, yoyo: true, repeat: -1 });
    }
  }
  reveal() {
    this.motion?.stop(); this.motion = undefined; this.cover.setVisible(false); this.prop?.setVisible(false); this.helper.setVisible(false);
    this.tile.setVisible(true).setScale(1).setY(0);
    (this.view.getData('label') as Phaser.GameObjects.Text).setY(0).setFontSize(this.definition.char.length > 1 ? 27 : 52);
    this.background.setVisible(true).setFillStyle(0xfff3d1, .2); this.view.setScale(1); this.state = 'available';
    this.view.input!.hitArea = new Phaser.Geom.Rectangle(16, 19, 112, 112);
  }
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
