import Phaser from 'phaser';
import type { LevelDefinition } from '../../types';
import type { GameMechanic, MechanicContext } from '../mechanics';
import { ammoKinds, ammoRules, createLayout, FLOOR, MAX_PULL, ShotSimulation, STEP, type AmmoKind, type ShotEvent, type ShotLayout } from './physics';

type Container = Phaser.GameObjects.Container;
type AmmoButton = { view: Container; background: Phaser.GameObjects.Rectangle; label: Phaser.GameObjects.Text };
const font = (size: number, color = '#345347'): Phaser.Types.GameObjects.Text.TextStyle => ({ fontFamily: 'Arial, sans-serif', fontSize: `${size}px`, color, fontStyle: 'bold' });
const clamp = Phaser.Math.Clamp;

/** Phaser owns rendering/input; the fixed-step model owns all damage and collisions. */
export class WordShotMechanic implements GameMechanic {
  readonly id = 'word_shot' as const;
  private ctx!: MechanicContext;
  private level!: LevelDefinition;
  private layout!: ShotLayout;
  private simulation!: ShotSimulation;
  private stock!: Record<AmmoKind, number>;
  private selected: AmmoKind = 'normal';
  private origin = { x: 240, y: 621 };
  private angle = Math.PI / 4;
  private power = .75;
  private dragging?: number;
  private accumulator = 0;
  private disposed = false;
  private finished = false;
  private exhausted = false;
  private shots = 0;
  private misses = 0;
  private hinted = false;
  private trail: Array<{ x: number; y: number }> = [];
  private objects: Phaser.GameObjects.GameObject[] = [];
  private cleanups: Array<() => void> = [];
  private timers: Phaser.Time.TimerEvent[] = [];
  private targets = new Map<number, { view: Container; health: Phaser.GameObjects.Graphics }>();
  private obstacles = new Map<number, Container>();
  private buttons = new Map<AmmoKind, AmmoButton>();
  private sling!: Phaser.GameObjects.Graphics;
  private path!: Phaser.GameObjects.Graphics;
  private projectileArt!: Phaser.GameObjects.Graphics;
  private player!: Container;
  private status!: Phaser.GameObjects.Text;
  private aimLabel!: Phaser.GameObjects.Text;
  private emptyOverlay?: Container;

  mount(ctx: MechanicContext, level: LevelDefinition) {
    this.ctx = ctx; this.level = level;
    this.layout = createLayout(level); this.simulation = new ShotSimulation(this.layout); this.stock = { ...this.layout.stock };
    this.drawScene(); this.buildTargets(); this.buildControls(); this.bindInput(); this.updateHud(); this.drawAim();
    ctx.feedback('Kéo hạt ở ná xuống trái, thả để bắn · Phá hết các mục tiêu để cứu Word Stars');
  }
  private add<T extends Phaser.GameObjects.GameObject>(object: T): T { this.objects.push(object); return object; }
  private text(x: number, y: number, value: string, size = 24, color = '#345347') {
    return this.ctx.scene.add.text(x, y, value, font(size, color)).setOrigin(.5);
  }
  private drawScene() {
    const s = this.ctx.scene;
    const ground = this.add(s.add.graphics());
    ground.fillStyle(0xb7cda1).fillRect(0, FLOOR, 1600, 50);
    ground.fillStyle(0xe8ecd9).fillRect(0, 790, 1600, 110);
    ground.lineStyle(5, 0x93b27e).lineBetween(0, FLOOR, 1600, FLOOR);
    // Original explorer: a little chick with a leaf hat and scarf.
    const art = s.add.graphics();
    art.fillStyle(0xe7b452).fillEllipse(0, 42, 72, 15);
    art.fillStyle(0xffd16c).fillEllipse(0, -2, 76, 83);
    art.fillStyle(0xffedba).fillEllipse(0, 16, 44, 36);
    art.fillStyle(0x3d5543).fillCircle(-13, -16, 4).fillCircle(14, -16, 4);
    art.fillStyle(0xe2994e).fillTriangle(-7, -5, 9, -5, 2, 4);
    art.fillStyle(0x81a478).fillTriangle(-40, -40, 30, -40, -12, -66).fillRect(-44, -42, 86, 9);
    art.fillStyle(0x729e8d).fillTriangle(-29, 8, 29, 8, 14, 25);
    this.player = this.add(s.add.container(this.origin.x - 50, FLOOR - 48, [art]));
    this.sling = this.add(s.add.graphics().setDepth(12));
    this.path = this.add(s.add.graphics().setDepth(13));
    this.projectileArt = this.add(s.add.graphics().setDepth(14));
    this.status = this.add(this.text(800, 40, '', 27));
    const wind = this.layout.wind === 0 ? 'Lặng gió' : `${this.layout.wind > 0 ? '→' : '←'} Gió ${Math.abs(this.layout.wind)}`;
    this.add(this.text(1370, 42, `${wind} · g ${this.layout.gravity}`, 22, '#71866a'));
    this.add(this.text(190, 42, 'WORD STAR RESCUE', 22, '#809269'));
    this.aimLabel = this.add(this.text(270, 470, '', 23));
  }
  private buildTargets() {
    const s = this.ctx.scene;
    // Targets are drawn first; physical cover remains visibly in front of them.
    for (const t of this.simulation.targets) {
      const backing = s.add.circle(0, 0, t.radius + 6, 0xfffdf3).setStrokeStyle(5, 0xc7bbdf);
      const image = s.textures.exists(`word:${t.term}`) ? s.add.image(0, -3, `word:${t.term}`).setDisplaySize(70, 70) : this.text(0, 0, '?', 50);
      const label = this.text(0, -78, t.term, 27);
      const health = s.add.graphics();
      const view = this.add(s.add.container(t.x, t.y, [backing, image, label, health]).setDepth(3));
      this.targets.set(t.id, { view, health });
      this.drawHealth(t.id);
    }
    for (const o of this.simulation.obstacles) {
      const art = s.add.graphics();
      if (o.material === 'wood') {
        art.fillStyle(0xb98d62).fillRect(0, 0, o.w, o.h);
        art.lineStyle(3, 0x916d4d).strokeRect(0, 0, o.w, o.h);
        if (o.h > o.w) {
          art.lineStyle(3, 0xd3ad7e).lineBetween(o.w * .35, 10, o.w * .35, o.h - 10);
          if (o.h > 120) { // Noncolliding foliage is visibly soft and above the trunk.
            art.fillStyle(0x99be86, .7).fillEllipse(o.w / 2, -26, 94, 70);
          }
        } else art.lineStyle(3, 0xd3ad7e).lineBetween(8, o.h / 2, o.w - 8, o.h / 2);
      } else {
        art.fillStyle(0x929e9b).fillRect(0, 0, o.w, o.h);
        art.lineStyle(4, 0x7c8986).strokeRect(0, 0, o.w, o.h);
        art.fillStyle(0xb1beb2).fillRect(0, 0, o.w, Math.min(o.h, 10));
        if (o.h > 80 && o.w > 60) art.lineStyle(3, 0x7b8985).lineBetween(20, 30, 40, 60).lineBetween(40, 60, 28, 83);
      }
      this.obstacles.set(o.id, this.add(s.add.container(o.x, o.y, [art]).setDepth(4)));
    }
  }
  private drawHealth(id: number) {
    const t = this.simulation.targets[id]; const item = this.targets.get(id); if (!item) return;
    item.health.clear();
    for (let i = 0; i < t.maxHp; i++) item.health.fillStyle(i < t.hp ? 0xe2b95f : 0xdadfd2).fillCircle((i - (t.maxHp - 1) / 2) * 17, -54, 5);
  }
  private buildControls() {
    const s = this.ctx.scene;
    ammoKinds.forEach((kind, i) => {
      const config = ammoRules[kind];
      const background = s.add.rectangle(0, 0, 236, 94, 0xfffdf5).setStrokeStyle(3, 0xd4dfca);
      const token = s.add.circle(-84, -17, 17, config.color).setStrokeStyle(3, 0xffffff);
      const name = this.text(25, -21, `${i + 1} · ${config.name}`, 23);
      const label = this.text(18, 17, '', 20, '#7e8f70');
      const view = this.add(s.add.container(150 + i * 252, 844, [background, token, name, label]).setSize(236, 94).setDepth(20).setInteractive({ useHandCursor: true }));
      view.on('pointerdown', () => this.chooseAmmo(kind)); this.buttons.set(kind, { view, background, label });
    });
    this.add(this.text(1255, 809, '1–4: đạn · A/D: di chuyển', 20, '#7e8f70'));
    this.add(this.text(1255, 842, '← →: góc · ↑ ↓: lực', 20, '#7e8f70'));
    this.add(this.text(1255, 874, 'Space: bắn · R: thử lại khi hết đạn', 18, '#7e8f70'));
    for (const direction of [-1, 1]) {
      const x = direction === -1 ? 74 : 468;
      const bg = s.add.rectangle(0, 0, 90, 86, 0xfaf7e7).setStrokeStyle(3, 0xd6dec5);
      const label = this.text(0, 0, direction < 0 ? '←' : '→', 38);
      const button = this.add(s.add.container(x, 687, [bg, label]).setDepth(15).setSize(90, 86).setInteractive({ useHandCursor: true }));
      button.on('pointerdown', () => this.move(direction * 30));
    }
  }
  private canAim() { return !this.disposed && !this.finished && !this.exhausted && !this.simulation.projectile; }
  private chooseAmmo(kind: AmmoKind) {
    if (!this.canAim() || this.stock[kind] === 0) return;
    this.selected = kind; this.updateHud(); this.drawAim(); this.ctx.feedback(ammoRules[kind].description);
  }
  private move(dx: number) {
    if (!this.canAim() || this.dragging !== undefined) return;
    this.origin.x = clamp(this.origin.x + dx, 185, 375);
    this.player.x = this.origin.x - 50; this.drawAim();
  }
  private bindInput() {
    const s = this.ctx.scene;
    const down = (p: Phaser.Input.Pointer, over: Phaser.GameObjects.GameObject[]) => {
      if (over.length || !this.canAim() || this.dragging !== undefined || Math.hypot(p.x - this.origin.x, p.y - this.origin.y) > 76) return;
      this.dragging = p.id; this.power = 0; this.drawAim();
    };
    const move = (p: Phaser.Input.Pointer) => {
      if (this.dragging !== p.id || !this.canAim()) return;
      const dx = Math.max(0, this.origin.x - p.x); const dy = Math.max(0, p.y - this.origin.y);
      this.power = clamp(Math.hypot(dx, dy) / MAX_PULL, 0, 1);
      this.angle = clamp(Math.atan2(dy, Math.max(dx, 1)), .07, 1.48); this.drawAim();
    };
    const up = (p: Phaser.Input.Pointer) => {
      if (this.dragging !== p.id) return;
      this.dragging = undefined;
      if (this.power >= .12) this.fire(); else { this.power = .75; this.drawAim(); }
    };
    const key = (event: KeyboardEvent) => {
      if (!s.sys.isActive() || event.target instanceof HTMLInputElement || event.target instanceof HTMLSelectElement || event.target instanceof HTMLButtonElement) return;
      if (/^[1-4]$/.test(event.key)) this.chooseAmmo(ammoKinds[Number(event.key) - 1]);
      else if (event.code === 'KeyR' && this.exhausted) this.retry();
      else if (this.canAim()) {
        if (event.code === 'KeyA') this.move(-25);
        if (event.code === 'KeyD') this.move(25);
        if (event.code === 'ArrowLeft') this.angle = Math.min(1.48, this.angle + .035);
        if (event.code === 'ArrowRight') this.angle = Math.max(.07, this.angle - .035);
        if (event.code === 'ArrowUp') this.power = Math.min(1, this.power + .035);
        if (event.code === 'ArrowDown') this.power = Math.max(.12, this.power - .035);
        if (event.code === 'Space') this.fire();
        if (event.code.startsWith('Arrow') || event.code === 'Space') event.preventDefault();
        this.drawAim();
      }
    };
    const update = (_time: number, delta: number) => this.update(delta);
    const cancelDrag = () => { this.dragging = undefined; };
    s.input.on('pointerdown', down); s.input.on('pointermove', move); s.input.on('pointerup', up); s.input.on('pointerupoutside', up);
    s.input.keyboard?.on('keydown', key); s.events.on('update', update); s.events.on('pause', cancelDrag);
    this.cleanups.push(() => {
      s.input.off('pointerdown', down); s.input.off('pointermove', move); s.input.off('pointerup', up); s.input.off('pointerupoutside', up);
      s.input.keyboard?.off('keydown', key); s.events.off('update', update); s.events.off('pause', cancelDrag);
    });
  }
  private fire() {
    if (!this.canAim() || this.stock[this.selected] <= 0) return;
    this.dragging = undefined; this.stock[this.selected]--; this.shots++; this.trail = [];
    this.simulation.launch(this.origin, this.angle, this.power, this.selected);
    this.path.clear(); this.updateHud(); this.drawSling();
    this.ctx.feedback(`${ammoRules[this.selected].name} đang bay…`);
  }
  private update(delta: number) {
    if (this.disposed || this.finished || this.exhausted) return;
    // Bound catch-up after tab suspension; fixed substeps avoid tunneling through thin planks.
    this.accumulator += Math.min(delta / 1000, .1);
    while (this.accumulator >= STEP) {
      const events = this.simulation.step(STEP); this.accumulator -= STEP;
      for (const event of events) this.handle(event);
      if (this.finished || this.exhausted) break;
    }
    for (const t of this.simulation.targets) if (t.hp > 0) this.targets.get(t.id)?.view.setPosition(t.x, t.y);
    const p = this.simulation.projectile;
    if (p) {
      this.trail.push({ x: p.x, y: p.y }); if (this.trail.length > 70) this.trail.shift();
      this.path.clear().fillStyle(ammoRules[p.kind].color, .35);
      this.trail.forEach((point, i) => { if (i % 3 === 0) this.path.fillCircle(point.x, point.y, 3); });
      this.drawProjectile(p.x, p.y, p.kind, Math.atan2(p.vy, p.vx));
    }
  }
  private handle(event: ShotEvent) {
    if (event.type === 'target') {
      const target = this.simulation.targets[event.id]; this.drawHealth(target.id);
      if (event.destroyed) {
        this.ctx.tracker.record(target.term, true); this.targets.get(target.id)?.view.setVisible(false);
        this.ring(target.x, target.y, 62, 0xffd576);
        void this.ctx.audio.speak(`${target.term}. Great shot!`);
      } else {
        this.ring(target.x, target.y, 48, 0xf3cf79); void this.ctx.audio.speakWord(target.term);
      }
      this.updateHud();
      if (this.simulation.remaining === 0 && !this.finished) {
        this.finished = true; this.ctx.feedback('All Word Stars rescued! Bé đã giải cứu hết mục tiêu!');
        this.timers.push(this.ctx.scene.time.delayedCall(800, () => { if (!this.disposed) this.ctx.complete(); }));
      }
    } else if (event.type === 'obstacle') {
      const o = this.simulation.obstacles[event.id];
      this.obstacles.get(o.id)?.setAlpha(o.hp > 0 ? .55 : 0);
      if (o.hp === 0) this.ring(o.x + o.w / 2, o.y + Math.min(o.h / 2, 50), 48, 0xc89d70);
    } else if (event.type === 'blast') this.ring(event.x, event.y, event.radius, 0xf5a76d);
    else if (event.type === 'bounce') this.ring(event.x, event.y, 35, 0xb29ce1);
    else if (event.type === 'settled' && !this.finished) {
      if (!event.useful) {
        this.misses++; this.ctx.tracker.record(this.level.targetVocabulary[0], false);
        this.ctx.feedback('Thử đổi góc, lực hoặc loại đạn nhé!');
        if (this.misses >= this.level.hintPolicy.afterWrongAttempts && !this.hinted) {
          this.hinted = true; this.ctx.tracker.hintCount++;
          this.ctx.feedback('Mũi xuyên đi qua vật cản. Hạt nổ phá gỗ; đá che được vụ nổ.');
          for (const [id, item] of this.targets) if (this.simulation.targets[id].hp > 0) (item.view.list[0] as Phaser.GameObjects.Arc).setStrokeStyle(7, 0xeac064);
        }
      } else this.ctx.feedback('Đường đã mở! Ngắm mục tiêu tiếp theo nhé.');
      if (this.totalAmmo() === 0) this.showExhausted();
      else {
        if (this.stock[this.selected] === 0) this.selected = ammoKinds.find(k => this.stock[k] > 0)!;
        this.updateHud(); this.drawAim();
      }
    }
  }
  private totalAmmo() { return ammoKinds.reduce((n, kind) => n + this.stock[kind], 0); }
  private updateHud() {
    this.status.setText(`★ ${this.layout.targets.length - this.simulation.remaining}/${this.layout.targets.length} đã cứu    ·    ${this.totalAmmo()} đạn`);
    for (const [kind, button] of this.buttons) {
      button.label.setText(`${ammoRules[kind].description} · ×${this.stock[kind]}`);
      button.label.setFontSize(17);
      button.background.setStrokeStyle(kind === this.selected ? 5 : 2, kind === this.selected ? 0x73965c : 0xd4dfca);
      button.view.setAlpha(this.stock[kind] > 0 ? 1 : .4);
    }
  }
  private drawSling() {
    const { x, y } = this.origin;
    this.sling.clear().lineStyle(18, 0x9f754f).lineBetween(x, FLOOR - 7, x, y + 54).lineBetween(x, y + 54, x - 34, y - 5).lineBetween(x, y + 54, x + 34, y - 5);
    const pulling = this.dragging !== undefined;
    const px = pulling ? x - Math.cos(this.angle) * this.power * MAX_PULL : x;
    const py = pulling ? y + Math.sin(this.angle) * this.power * MAX_PULL : y;
    this.sling.lineStyle(7, 0x6d5951).lineBetween(x - 34, y - 5, px, py).lineBetween(x + 34, y - 5, px, py);
  }
  private drawAim() {
    if (!this.canAim()) return;
    this.drawSling();
    this.path.clear().fillStyle(0x739773, .65);
    const dots = this.power >= .12 ? this.simulation.preview(this.origin, this.angle, this.power, this.selected) : [];
    for (const p of dots) this.path.fillCircle(p.x, p.y, this.hinted ? 5 : 4);
    const pulling = this.dragging !== undefined;
    this.drawProjectile(this.origin.x - (pulling ? Math.cos(this.angle) * this.power * MAX_PULL : 0), this.origin.y + (pulling ? Math.sin(this.angle) * this.power * MAX_PULL : 0), this.selected, -this.angle);
    this.aimLabel.setText(`${Math.round(Phaser.Math.RadToDeg(this.angle))}°   ·   Lực ${Math.round(this.power * 100)}%`);
  }
  private drawProjectile(x: number, y: number, kind: AmmoKind, rotation: number) {
    const a = ammoRules[kind]; const g = this.projectileArt;
    g.clear().setPosition(x, y).setRotation(rotation);
    if (kind === 'piercing') g.fillStyle(a.color).fillTriangle(27, 0, -17, -12, -17, 12).lineStyle(3, 0xffffff).lineBetween(-13, 0, 13, 0);
    else {
      g.fillStyle(a.color).fillCircle(0, 0, a.radius).lineStyle(3, 0xfff9e6).strokeCircle(0, 0, a.radius);
      if (kind === 'explosive') g.lineStyle(4, 0xffffff).lineBetween(-7, 0, 7, 0).lineBetween(0, -7, 0, 7);
      else if (kind === 'bouncy') g.lineStyle(3, 0xffffff).strokeCircle(0, 0, 8);
      else g.fillStyle(0xfff3cd).fillCircle(-4, -5, 5);
    }
  }
  private ring(x: number, y: number, radius: number, color: number) {
    const ring = this.add(this.ctx.scene.add.circle(x, y, radius, color, .16).setStrokeStyle(4, color, .8).setDepth(10));
    this.ctx.scene.tweens.add({ targets: ring, scale: { from: .25, to: 1 }, alpha: 0, duration: this.ctx.reducedMotion ? 100 : 440,
      onComplete: () => { this.objects = this.objects.filter(o => o !== ring); ring.destroy(); } });
  }
  private showExhausted() {
    this.exhausted = true; this.projectileArt.clear(); this.path.clear(); this.updateHud();
    this.ctx.feedback('Hết đạn rồi — đổi chiến thuật và thử lại nhé.');
    const s = this.ctx.scene;
    const shade = s.add.rectangle(800, 450, 1600, 900, 0x294336, .45).setInteractive();
    const card = s.add.rectangle(800, 410, 740, 360, 0xfffcf0).setStrokeStyle(5, 0xdbe4c9);
    const title = this.text(800, 296, 'Một chiến thuật mới nhé!', 38);
    const subtitle = this.text(800, 355, `Đã cứu ${this.layout.targets.length - this.simulation.remaining}/${this.layout.targets.length} mục tiêu trong ${this.shots} phát.`, 26);
    const note = this.text(800, 403, 'Giữ nguyên địa hình · Nạp lại đạn · Thử góc khác', 22, '#849373');
    const button = s.add.rectangle(800, 501, 385, 92, 0x78996b).setInteractive({ useHandCursor: true });
    const label = this.text(800, 501, '↻  Thử lại  (R)', 30, '#ffffff');
    button.on('pointerdown', () => this.retry());
    this.emptyOverlay = this.add(s.add.container(0, 0, [shade, card, title, subtitle, note, button, label]).setDepth(40));
  }
  private retry() {
    if (!this.exhausted || this.disposed) return;
    this.emptyOverlay?.destroy(); this.emptyOverlay = undefined;
    for (const item of this.targets.values()) item.view.destroy(); this.targets.clear();
    for (const item of this.obstacles.values()) item.destroy(); this.obstacles.clear();
    this.objects = this.objects.filter(o => o.active);
    this.simulation = new ShotSimulation(this.layout); this.stock = { ...this.layout.stock };
    this.exhausted = false; this.selected = 'normal'; this.shots = 0; this.misses = 0; this.hinted = false;
    this.accumulator = 0; this.trail = []; this.buildTargets(); this.updateHud(); this.drawAim();
    this.ctx.feedback('Sẵn sàng! Thử đạn xuyên với đá, đạn nổ với nhóm mục tiêu.');
  }
  dispose() {
    this.disposed = true;
    this.cleanups.forEach(fn => fn()); this.timers.forEach(t => t.remove(false));
    for (const object of this.objects) if (object.active) { this.ctx.scene.tweens.killTweensOf(object); object.destroy(); }
    this.objects = []; this.cleanups = []; this.targets.clear(); this.obstacles.clear(); this.buttons.clear();
  }
}
