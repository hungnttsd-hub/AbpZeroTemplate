import Phaser from 'phaser';
export class WordBuilderEffects {
  private tweens = new Set<Phaser.Tweens.Tween>();
  private timers = new Set<Phaser.Time.TimerEvent>();
  private particles: Phaser.GameObjects.Arc[];
  constructor(readonly scene: Phaser.Scene, readonly reduced: boolean) {
    this.particles = Array.from({ length: 16 }, () => scene.add.circle(0, 0, 4, 0xf4c66a).setDepth(50).setVisible(false));
  }
  tween(config: Phaser.Types.Tweens.TweenBuilderConfig) {
    const tween = this.scene.tweens.add({ ...config, onComplete: (completed: Phaser.Tweens.Tween, targets: unknown[]) => {
      this.tweens.delete(tween); if (typeof config.onComplete === 'function') config.onComplete(completed, targets);
    } }); this.tweens.add(tween); return tween;
  }
  later(ms: number, fn: () => void) {
    const timer = this.scene.time.delayedCall(ms, () => { this.timers.delete(timer); fn(); }); this.timers.add(timer); return timer;
  }
  sparkle(x: number, y: number) {
    const particles = this.particles.filter(p => !p.visible).slice(0, this.reduced ? 3 : 8);
    particles.forEach((p, i) => {
      const angle = i / particles.length * Math.PI * 2;
      p.setPosition(x, y).setVisible(true).setAlpha(1);
      this.tween({ targets: p, x: x + Math.cos(angle) * 42, y: y + Math.sin(angle) * 42, alpha: 0, duration: this.reduced ? 150 : 450, onComplete: () => p.setVisible(false) });
    });
  }
  bounce(object: Phaser.GameObjects.Container) {
    this.tween({ targets: object, scale: this.reduced ? 1.025 : 1.12, duration: 130, yoyo: true });
  }
  shake(object: Phaser.GameObjects.Container) {
    this.tween({ targets: object, angle: { from: this.reduced ? -1 : -5, to: this.reduced ? 1 : 5 }, duration: 85, yoyo: true, repeat: 1, onComplete: () => object.setAngle(0) });
  }
  fly(object: Phaser.GameObjects.Container, target: { x: number; y: number }, done: () => void) {
    const start = new Phaser.Math.Vector2(object.x, object.y);
    const curve = new Phaser.Curves.QuadraticBezier(start, new Phaser.Math.Vector2((start.x + target.x) / 2, Math.min(start.y, target.y) - 85), new Phaser.Math.Vector2(target.x, target.y));
    const state = { t: 0 }; object.setDepth(40); this.sparkle(start.x, start.y);
    if (this.reduced) {
      object.setPosition(target.x, target.y).setAlpha(.4);
      this.tween({ targets: object, alpha: 1, duration: 150, onComplete: done }); return;
    }
    this.tween({ targets: state, t: 1, duration: 450, onUpdate: () => {
      const p = curve.getPoint(state.t); object.setPosition(p.x, p.y).setScale(1 + Math.sin(state.t * Math.PI) * .15).setAngle(Math.sin(state.t * Math.PI) * 9);
    }, onComplete: () => { object.setPosition(target.x, target.y).setScale(1).setAngle(0); done(); } });
  }
  dispose() { this.tweens.forEach(t => t.stop()); this.tweens.clear(); this.timers.forEach(t => t.remove(false)); this.timers.clear(); this.particles.forEach(p => p.destroy()); }
}
