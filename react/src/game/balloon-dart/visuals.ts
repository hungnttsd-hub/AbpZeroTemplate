import Phaser from 'phaser';
import { character } from '../theme';
import type { BalloonDartConfig, BalloonOption } from './model';
import { positionAt, clamp } from './motion';
export const palette: Record<string, number> = { blue: 0x74c9ff, coral: 0xff9d87, yellow: 0xffd768, violet: 0xc8a0ff, green: 0x94d985, red: 0xf07778, pink: 0xf5a5ca, orange: 0xffb06a, purple: 0xc8a0ff, white: 0xfffcf2, black: 0x5a6270, brown: 0xb78b6c };
export function text(s: Phaser.Scene, x: number, y: number, value: string, size = 28, color = '#24483f') { return s.add.text(x, y, value, { fontFamily: 'Nunito, Arial', fontSize: `${size}px`, fontStyle: 'bold', color, align: 'center' }).setOrigin(.5); }

export class BalloonTarget {
  view: Phaser.GameObjects.Container; body: MatterJS.BodyType; halo: Phaser.GameObjects.Ellipse;
  popped = false; previous = { x: 0, y: 0 };
  readonly radiusX = 86; readonly radiusY = 105;
  constructor(private scene: Phaser.Scene, readonly config: BalloonOption, mode: BalloonDartConfig['learningMode']) {
    const color = palette[config.semantic.color ?? config.balloonSkin] ?? palette.blue;
    const string = scene.add.graphics().lineStyle(3, 0xb69360, .85);
    string.beginPath().moveTo(0, 105).lineTo(-7, 128).lineTo(7, 145).lineTo(0, 163).strokePath();
    this.halo = scene.add.ellipse(0, 0, 195, 238, 0xfff1a6, .5).setVisible(false);
    const skin = `art:balloon:${color.toString(16).padStart(6, '0')}`;
    const body = scene.textures.exists(skin) ? scene.add.image(0, 13, skin).setDisplaySize(220, 280) : scene.add.ellipse(0, 0, 180, 220, color).setStrokeStyle(4, 0xffffff, .7);
    const objects: Phaser.GameObjects.GameObject[] = [string, this.halo, body];
    const semantic = config.semantic;
    if (mode !== 'colorRecognition') {
      const key = scene.textures.exists(`bd:${semantic.imageKey}`) ? `bd:${semantic.imageKey}` : scene.textures.exists(`bd:shape:${semantic.shape}:${semantic.color}`) ? `bd:shape:${semantic.shape}:${semantic.color}` : `word:${semantic.shape ?? semantic.word ?? semantic.id}`;
      const size = semantic.size === 'small' ? 63 : semantic.size === 'big' ? 119 : 99;
      if (scene.textures.exists(key)) objects.push(scene.add.image(0, 0, key).setDisplaySize(size, size));
    }
    // Balloons show pictures only, including older configs with visible/fading labels.
    this.view = scene.add.container(0, 0, objects).setDepth(10);
    // A scaled circle supplies an ellipse sensor; sweep testing supplements Matter at high velocity.
    this.body = scene.matter.add.circle(0, 0, this.radiusX, { isStatic: true, isSensor: true, collisionFilter: { category: 2, mask: 4 } });
    scene.matter.body.scale(this.body, 1, this.radiusY / this.radiusX);
  }
  move(x: number, y: number, elapsed: number, reduced: boolean) {
    this.previous = { x: this.view.x, y: this.view.y };
    if (this.popped) return;
    this.view.setPosition(x, y).setAngle(reduced ? 0 : Math.sin(elapsed / 900) * 1.2);
    this.scene.matter.body.setPosition(this.body, { x, y });
  }
  contains(x: number, y: number) { return ((x - this.view.x) / 96) ** 2 + ((y - this.view.y) / 116) ** 2 <= 1; }
  dispose() { this.scene.tweens.killTweensOf(this.view); this.scene.matter.world.remove(this.body); this.view.destroy(); }
}

export class BalloonManager {
  targets: BalloonTarget[] = [];
  constructor(private scene: Phaser.Scene, private reduced: boolean) {}
  spawn(options: BalloonOption[], mode: BalloonDartConfig['learningMode'], seed: number) {
    this.clear(); this.targets = options.map(o => new BalloonTarget(this.scene, o, mode)); this.update(0, seed);
    this.targets.forEach(b => { b.previous = { x: b.view.x, y: b.view.y }; b.view.setAlpha(0); this.scene.tweens.add({ targets: b.view, alpha: 1, duration: this.reduced ? 150 : 400 }); });
  }
  update(ms: number, seed: number) {
    const positions = this.targets.map((b, i) => positionAt(b.config, i, this.targets.length, ms, seed, this.reduced));
    // Resolve overlap without changing semantic order or pinning an answer to a skin.
    for (let pass = 0; pass < 8; pass++) for (let i = 0; i < positions.length; i++) for (let j = i + 1; j < positions.length; j++) {
      const a = positions[i], b = positions[j], dx = b.x - a.x, dy = b.y - a.y;
      const distance = Math.hypot(dx, dy), minimum = 218;
      if (distance >= minimum) continue;
      const nx = distance > 1 ? dx / distance : 1, ny = distance > 1 ? dy / distance : 0;
      const push = (minimum - distance) / 2;
      a.x = clamp(a.x - nx * push, 190, 1250); b.x = clamp(b.x + nx * push, 190, 1250);
      a.y = clamp(a.y - ny * push, 270, 445); b.y = clamp(b.y + ny * push, 270, 445);
    }
    this.targets.forEach((b, i) => b.move(positions[i].x, positions[i].y, ms, this.reduced));
  }
  clear() { this.targets.forEach(b => b.dispose()); this.targets = []; }
}

export class Launcher {
  view: Phaser.GameObjects.Container; barrel: Phaser.GameObjects.Container; mascot: Phaser.GameObjects.Container;
  constructor(scene: Phaser.Scene) {
    const base = scene.add.graphics().fillStyle(0xd8a66a).fillRoundedRect(-66, -6, 132, 45, 18).fillStyle(0x8d6749).fillCircle(-45, 33, 23).fillCircle(45, 33, 23).fillStyle(0xf8d878).fillCircle(-45, 33, 12).fillCircle(45, 33, 12);
    const tube = scene.add.graphics().fillStyle(0x619fc7).fillRoundedRect(-22, -31, 110, 62, 22).lineStyle(4, 0x477d98).strokeRoundedRect(-22, -31, 110, 62, 22).fillStyle(0xffd46e).fillRoundedRect(61, -39, 24, 78, 9).fillStyle(0xf5fbef).fillCircle(14, 0, 13);
    base.fillStyle(0xffe8b0, .8).fillRoundedRect(-55, -5, 110, 9, 4);
    base.lineStyle(3, 0xbb8253).lineBetween(-29, 19, 28, 19);
    base.fillStyle(0xfff0be).fillCircle(-45, 29, 6).fillCircle(45, 29, 6);
    tube.fillStyle(0xb5edee, .8).fillRoundedRect(-10, -23, 65, 11, 5);
    tube.fillStyle(0x2c697f, .3).fillRoundedRect(-12, 17, 66, 9, 4);
    tube.fillStyle(0xffefac).fillRoundedRect(65, -34, 6, 65, 3);
    tube.lineStyle(2, 0xfff2ba).strokeCircle(14, 0, 18);
    this.barrel = scene.add.container(0, 0, [tube]).setRotation(-Math.PI / 2);
    this.view = scene.add.container(720, 705, [this.barrel, base]).setDepth(22);
    const bird = character(scene, 'poki', 0, -7, 155);
    this.mascot = scene.add.container(564, 691, [bird]).setDepth(21);
  }
  dispose() { this.view.destroy(); this.mascot.destroy(); }
}

export class DartPool {
  readonly view: Phaser.GameObjects.Container; readonly body: MatterJS.BodyType;
  active = false; x = 0; y = 0; vx = 0; vy = 0;
  readonly trail: Phaser.GameObjects.Arc[];
  constructor(private scene: Phaser.Scene) {
    const shaft = scene.add.rectangle(-9, 0, 42, 13, 0x5e91d1).setStrokeStyle(2, 0x417fa9);
    const cup = scene.add.ellipse(17, 0, 15, 29, 0xffd267).setStrokeStyle(2, 0xc79540);
    this.view = scene.add.container(0, 0, [shaft, cup]).setDepth(30).setVisible(false);
    this.body = scene.matter.add.circle(-100, -100, 8, { isSensor: true, ignoreGravity: true, collisionFilter: { category: 4, mask: 0 } });
    this.trail = Array.from({ length: 5 }, (_, i) => scene.add.circle(0, 0, 5 - i * .55, 0xffffff, .65 - i * .1).setDepth(25).setVisible(false));
  }
  fire(angle: number) {
    this.active = true; this.x = 720 + Math.cos(angle) * 82; this.y = 705 + Math.sin(angle) * 82;
    this.vx = Math.cos(angle) * 960; this.vy = Math.sin(angle) * 960;
    this.scene.tweens.killTweensOf(this.view); this.view.setPosition(this.x, this.y).setRotation(angle).setAlpha(1).setVisible(true);
    this.body.collisionFilter.mask = 2; this.scene.matter.body.setPosition(this.body, { x: this.x, y: this.y });
  }
  move(dt: number) {
    this.x += this.vx * dt; this.y += this.vy * dt; this.view.setPosition(this.x, this.y);
    this.scene.matter.body.setPosition(this.body, { x: this.x, y: this.y });
    this.trail.forEach((p, i) => p.setVisible(true).setPosition(this.x - this.vx * (i + 1) * .014, this.y - this.vy * (i + 1) * .014));
  }
  recycle() { this.active = false; this.view.setVisible(false); this.trail.forEach(p => p.setVisible(false)); this.body.collisionFilter.mask = 0; this.scene.matter.body.setPosition(this.body, { x: -100, y: -100 }); }
  dispose() { this.recycle(); this.scene.tweens.killTweensOf(this.view); this.scene.matter.world.remove(this.body); this.view.destroy(); this.trail.forEach(p => p.destroy()); }
}

export function landscape(scene: Phaser.Scene) {
  // The scene already owns the world backdrop. Only add the soft HUD/footer shading here.
  return scene.add.graphics().fillStyle(0xfff8e4, .82).fillRoundedRect(343, 746, 754, 51, 24).setDepth(1);
}
