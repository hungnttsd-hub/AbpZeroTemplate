import Phaser from 'phaser';
import { ammoRules, type AmmoKind, type Obstacle, type Vec } from './physics';

type Particle = { image: Phaser.GameObjects.Image; vx: number; vy: number; gravity: number; spin: number; remaining: number; life: number; size: number; opacity: number };

/** A bounded pool keeps particle effects inexpensive on touch devices. */
export class ShotEffects {
  private particles: Particle[];
  private transient = new Set<Phaser.GameObjects.GameObject>();
  constructor(private scene: Phaser.Scene, readonly reduced: boolean) {
    this.particles = Array.from({ length: reduced ? 12 : 96 }, () => ({ image: scene.add.image(0, 0, 'shot:spark').setDepth(18).setVisible(false), vx: 0, vy: 0, gravity: 0, spin: 0, remaining: 0, life: 0, size: 0, opacity: 1 }));
  }
  update(ms: number) {
    const dt = Math.min(ms, 50) / 1000;
    for (const p of this.particles) {
      if (p.remaining <= 0) continue;
      p.remaining -= dt;
      if (p.remaining <= 0) { p.image.setVisible(false); continue; }
      p.vy += p.gravity * dt;
      p.image.x += p.vx * dt; p.image.y += p.vy * dt; p.image.rotation += p.spin * dt;
      const t = p.remaining / p.life;
      p.image.setAlpha(p.opacity * Math.min(1, t * 2)).setDisplaySize(p.size * (.4 + .6 * t), p.size * (.4 + .6 * t));
    }
  }
  private emit(x: number, y: number, texture: string, color: number, count: number, speed: number, size: number, gravity = 180) {
    const amount = this.reduced ? Math.min(count, 2) : count;
    for (let i = 0; i < amount; i++) {
      const p = this.particles.find(p => p.remaining <= 0); if (!p) break;
      const angle = Math.PI * 2 * (i / amount) + Math.random() * .4;
      const force = speed * (.45 + Math.random() * .55);
      p.vx = this.reduced ? 0 : Math.cos(angle) * force; p.vy = this.reduced ? 0 : Math.sin(angle) * force - speed * .3;
      p.gravity = this.reduced ? 0 : gravity; p.spin = this.reduced ? 0 : (Math.random() - .5) * 8;
      p.life = this.reduced ? .18 : .35 + Math.random() * .4; p.remaining = p.life;
      p.size = size * (.7 + Math.random() * .5); p.opacity = texture === 'shot:puff' ? .5 : 1;
      p.image.setTexture(texture).setTint(color).setPosition(x, y).setRotation(angle).setAlpha(p.opacity).setDisplaySize(p.size, p.size).setVisible(true);
    }
  }
  ring(x: number, y: number, radius: number, color: number, duration = 380) {
    const ring = this.scene.add.circle(x, y, radius, color, .06).setStrokeStyle(4, color, .85).setDepth(16);
    this.transient.add(ring);
    this.scene.tweens.add({ targets: ring, scale: { from: this.reduced ? 1 : .2, to: 1 }, alpha: 0, duration: this.reduced ? 120 : duration, onComplete: () => { this.transient.delete(ring); ring.destroy(); } });
  }
  launch(x: number, y: number, kind: AmmoKind) {
    this.ring(x, y, 37, ammoRules[kind].color, 250);
    this.emit(x, y, 'shot:puff', 0xffefd2, 5, 55, 25, -10);
  }
  flight(x: number, y: number, kind: AmmoKind) {
    if (this.reduced) return;
    const texture = kind === 'frost' ? 'shot:snow' : kind === 'meteor' || kind === 'explosive' ? 'shot:puff' : 'shot:spark';
    this.emit(x, y, texture, ammoRules[kind].color, 1, 12, kind === 'meteor' ? 29 : 13, -30);
  }
  trail(g: Phaser.GameObjects.Graphics, points: Vec[], kind: AmmoKind) {
    g.clear(); if (points.length < 2) return;
    const color = ammoRules[kind].color;
    for (let i = 1; i < points.length; i++) {
      const t = i / points.length, a = points[i - 1], b = points[i];
      if (this.reduced) { if (i % 7 === 0) g.fillStyle(color, .35).fillCircle(b.x, b.y, 3); continue; }
      g.lineStyle((kind === 'meteor' ? 15 : 9) * t, color, t * .45).lineBetween(a.x, a.y, b.x, b.y);
      if (kind === 'piercing' || kind === 'frost') g.lineStyle(2 * t, 0xf4ffff, t * .85).lineBetween(a.x, a.y, b.x, b.y);
      if (i % 8 === 0) g.fillStyle(0xfff2c9, t * .9).fillCircle(b.x, b.y, 3 * t);
    }
  }
  impact(x: number, y: number, kind: AmmoKind, material: string) {
    const color = material === 'wood' ? 0xd9a467 : material === 'stone' || material === 'ground' ? 0xabbcb5 : ammoRules[kind].color;
    const texture = material === 'wood' ? 'shot:chip' : material === 'stone' || material === 'ground' ? 'shot:pebble' : 'shot:spark';
    this.emit(x, y, texture, color, kind === 'meteor' ? 14 : 7, 140, 12);
    this.ring(x, y, kind === 'meteor' ? 63 : 32, color, 260);
  }
  blast(x: number, y: number, radius: number, frozen = false) {
    const color = frozen ? 0x91e6ff : 0xffc16b;
    this.ring(x, y, radius, color, 540);
    if (!this.reduced) this.ring(x, y, radius * .65, frozen ? 0xefffff : 0xfff1b0, 370);
    this.emit(x, y, frozen ? 'shot:snow' : 'shot:spark', color, 20, 265, 23, frozen ? 25 : 90);
    if (!frozen) this.emit(x, y, 'shot:puff', 0xf7b98a, 10, 115, 60, -80);
  }
  breakObstacle(o: Obstacle) {
    const wood = o.material === 'wood';
    const count = this.reduced ? 1 : 3;
    for (let i = 0; i < count; i++) this.emit(o.x + o.w * (.25 + i * .25), o.y + o.h * (.2 + i * .28), wood ? 'shot:chip' : 'shot:pebble', wood ? 0xd7ae78 : 0xa1b5b4, 7, 170, wood ? 18 : 23, 360);
    this.emit(o.x + o.w / 2, o.y + o.h / 2, 'shot:puff', wood ? 0xf4d7a6 : 0xd4e0d6, 5, 70, 45, -50);
  }
  rescue(x: number, y: number) {
    this.emit(x, y, 'shot:spark', 0xffdf7b, 17, 180, 23, 70);
    this.ring(x, y, 80, 0xffedaa, 500);
    const star = this.scene.add.image(x, y, 'shot:spark').setDisplaySize(61, 61).setDepth(24);
    this.transient.add(star);
    this.scene.tweens.add({ targets: star, x: this.reduced ? x : 800, y: this.reduced ? y : 43, scale: this.reduced ? star.scale : .5, alpha: 0, duration: this.reduced ? 150 : 700, ease: 'Cubic.easeIn', onComplete: () => { this.transient.delete(star); star.destroy(); } });
  }
  clear() {
    this.particles.forEach(p => { p.remaining = 0; p.image.setVisible(false); });
    for (const object of this.transient) { this.scene.tweens.killTweensOf(object); object.destroy(); }
    this.transient.clear();
  }
  dispose() { this.clear(); this.particles.forEach(p => p.image.destroy()); }
}
