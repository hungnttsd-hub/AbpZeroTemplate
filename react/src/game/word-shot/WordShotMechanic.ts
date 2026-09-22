import Phaser from 'phaser';
import { character, woodPanel } from '../theme';
import type { LevelDefinition } from '../../types';
import type { GameMechanic, MechanicContext } from '../mechanics';
import { ammoKinds, ammoRules, createLayout, FLOOR, MAX_PULL, ShotSimulation, STEP, type AmmoKind, type ShotEvent, type ShotLayout } from './physics';
import { TargetVisual, ObstacleVisual, type LabelBounds } from './visuals';
import { ShotEffects } from './effects';

type Container = Phaser.GameObjects.Container;
type AmmoButton = { view: Container; background: Phaser.GameObjects.Rectangle; count: Phaser.GameObjects.Text };
const font = (size: number, color = '#345347'): Phaser.Types.GameObjects.Text.TextStyle => ({ fontFamily: 'Nunito, Arial, sans-serif', fontSize: `${size}px`, color, fontStyle: 'bold' });
const clamp = Phaser.Math.Clamp;

/** Rendering and effects consume events; all hits and ammo abilities live in the fixed-step model. */
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
  private trailTimer = 0;
  private objects: Phaser.GameObjects.GameObject[] = [];
  private cleanups: Array<() => void> = [];
  private timers: Phaser.Time.TimerEvent[] = [];
  private targets = new Map<number, TargetVisual>();
  private obstacles = new Map<number, ObstacleVisual>();
  private buttons = new Map<AmmoKind, AmmoButton>();
  private sling!: Phaser.GameObjects.Graphics;
  private path!: Phaser.GameObjects.Graphics;
  private projectileArt!: Phaser.GameObjects.Image;
  private projectileGlow!: Phaser.GameObjects.Image;
  private player!: Container;
  private status!: Phaser.GameObjects.Text;
  private aimLabel!: Phaser.GameObjects.Text;
  private ammoHint!: Phaser.GameObjects.Text;
  private fx!: ShotEffects;
  private emptyOverlay?: Container;

  mount(ctx: MechanicContext, level: LevelDefinition) {
    this.ctx = ctx; this.level = level;
    this.layout = createLayout(level); this.simulation = new ShotSimulation(this.layout); this.stock = { ...this.layout.stock };
    this.fx = new ShotEffects(ctx.scene, ctx.reducedMotion);
    this.drawScene(); this.buildTargets(); this.buildControls(); this.bindInput(); this.updateHud(); this.drawAim();
    ctx.feedback('Chọn đạn · Kéo hạt ở ná xuống trái · Thả để giải cứu Word Stars');
  }
  private add<T extends Phaser.GameObjects.GameObject>(object: T): T { this.objects.push(object); return object; }
  private text(x: number, y: number, value: string, size = 24, color = '#345347') {
    return this.ctx.scene.add.text(x, y, value, font(size, color)).setOrigin(.5).setResolution(2);
  }
  private drawScene() {
    const s = this.ctx.scene;
    const ground = this.add(s.add.graphics());
    ground.fillStyle(0x92b976).fillRect(0, FLOOR, 1600, 44);
    ground.fillStyle(0xc6df97).fillRect(0, FLOOR, 1600, 7);
    ground.fillStyle(0xe7d5a5).fillRect(0, 788, 1600, 112);
    ground.fillStyle(0xfff5d8, .96).fillRect(0, 792, 1600, 108);
    for (let x = 12; x < 1600; x += 51) {
      ground.lineStyle(2, 0xc6db8d, .8).lineBetween(x, FLOOR + 9, x + 4, FLOOR + 2).lineBetween(x + 4, FLOOR + 2, x + 7, FLOOR + 8);
    }
    this.player = this.add(s.add.container(this.origin.x - 50, FLOOR - 48, [character(s, 'pip', 0, -10, 140)]));
    this.sling = this.add(s.add.graphics().setDepth(12));
    this.path = this.add(s.add.graphics().setDepth(13));
    this.projectileGlow = this.add(s.add.image(0, 0, 'shot:halo').setDepth(13));
    this.projectileArt = this.add(s.add.image(0, 0, 'shot:ammo:normal').setDepth(14));
    this.add(woodPanel(s, 620, 70, 0xfff3ce).setPosition(800, 40));
    this.status = this.add(this.text(800, 40, '', 27));
    const wind = this.layout.wind === 0 ? 'Lặng gió' : `${this.layout.wind > 0 ? '→' : '←'} ${Math.abs(this.layout.wind) > 40 ? 'Gió mạnh' : 'Gió nhẹ'}`;
    this.add(this.text(1370, 42, wind, 24, '#365f55'));
    this.add(this.text(190, 42, 'WORD STAR RESCUE', 22, '#365f55'));
    this.aimLabel = this.add(this.text(245, 466, '', 23));
    this.ammoHint = this.add(this.text(22, 774, '', 17, '#345f50').setOrigin(0, .5));
  }
  private buildTargets() {
    for (const target of this.simulation.targets) {
      const item = new TargetVisual(this.ctx.scene, target); this.add(item.view); this.targets.set(target.id, item);
    }
    for (const obstacle of this.simulation.obstacles) {
      const item = new ObstacleVisual(this.ctx.scene, obstacle); this.add(item.view); this.obstacles.set(obstacle.id, item);
    }
    this.layoutLabels();
  }
  private layoutLabels() {
    const occupied: LabelBounds[] = this.simulation.targets.filter(t => t.hp > 0)
      .map(t => ({ x: t.homeX - 61, y: t.homeY - t.motion - 61, w: 122, h: 122 + t.motion }));
    for (const t of this.simulation.targets) if (t.hp > 0) this.targets.get(t.id)?.layoutBadge(this.simulation.obstacles, occupied);
  }
  private buildControls() {
    const s = this.ctx.scene;
    ammoKinds.forEach((kind, i) => {
      const config = ammoRules[kind];
      const background = s.add.rectangle(0, 0, 232, 80, 0xfff8de, .4).setStrokeStyle(2, 0xddc592);
      const trim = woodPanel(s, 244, 96, 0xffedc3);
      const token = s.add.image(-85, -7, `shot:ammo:${kind}`).setDisplaySize(61, 61);
      const name = this.text(-48, -18, config.name, 21).setOrigin(0, .5);
      const label = this.text(-49, 15, config.description, 14, '#63806a').setOrigin(0, .5);
      // Key and stock are separate, keeping descriptions readable at tablet size.
      const badge = s.add.circle(-107, -35, 13, 0x6e8a6b);
      const key = this.text(-107, -35, `${i + 1}`, 14, '#fff9dd');
      const count = this.text(99, -34, '', 17, '#856c3b');
      const view = this.add(s.add.container(145 + i * 262, 845, [trim, background, token, name, label, badge, key, count]).setSize(244, 98).setDepth(20).setInteractive({ useHandCursor: true }));
      view.on('pointerdown', () => this.chooseAmmo(kind)); this.buttons.set(kind, { view, background, count });
    });
    this.add(this.text(1576, 774, '1–6: đạn   A/D: di chuyển   ← →: góc   ↑ ↓: lực   Space: bắn', 17, '#50785e').setOrigin(1, .5));
    for (const direction of [-1, 1]) {
      const x = direction === -1 ? 74 : 468;
      const bg = woodPanel(s, 90, 82, 0xfff2c5);
      const label = this.text(0, -3, direction < 0 ? '←' : '→', 38);
      const button = this.add(s.add.container(x, 687, [bg, label]).setDepth(15).setSize(90, 86).setInteractive({ useHandCursor: true }));
      button.on('pointerdown', () => this.move(direction * 30));
    }
  }
  private canAim() { return !this.disposed && !this.finished && !this.exhausted && !this.simulation.projectile; }
  private chooseAmmo(kind: AmmoKind) {
    if (!this.canAim() || this.stock[kind] === 0) return;
    this.selected = kind; this.updateHud(); this.drawAim(); this.ctx.feedback(ammoRules[kind].hint);
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
      const dx = Math.max(0, this.origin.x - p.x), dy = Math.max(0, p.y - this.origin.y);
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
      if (/^[1-6]$/.test(event.key)) this.chooseAmmo(ammoKinds[Number(event.key) - 1]);
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
    const cancelDrag = () => { this.dragging = undefined; this.drawAim(); };
    s.input.on('pointerdown', down); s.input.on('pointermove', move); s.input.on('pointerup', up); s.input.on('pointerupoutside', cancelDrag);
    s.input.keyboard?.on('keydown', key); s.events.on('update', update); s.events.on('pause', cancelDrag);
    s.game.canvas.addEventListener('pointercancel', cancelDrag);
    this.cleanups.push(() => {
      s.input.off('pointerdown', down); s.input.off('pointermove', move); s.input.off('pointerup', up); s.input.off('pointerupoutside', cancelDrag);
      s.input.keyboard?.off('keydown', key); s.events.off('update', update); s.events.off('pause', cancelDrag);
      s.game.canvas.removeEventListener('pointercancel', cancelDrag);
    });
  }
  private fire() {
    if (!this.canAim() || this.stock[this.selected] <= 0) return;
    this.dragging = undefined; this.stock[this.selected]--; this.shots++; this.trail = []; this.trailTimer = 0;
    this.simulation.launch(this.origin, this.angle, this.power, this.selected);
    this.fx.launch(this.origin.x, this.origin.y, this.selected);
    this.path.clear(); this.updateHud(); this.drawSling();
    this.ctx.feedback(`${ammoRules[this.selected].name} đang bay…`);
  }
  private update(delta: number) {
    if (this.disposed) return;
    this.fx.update(delta);
    if (this.finished || this.exhausted) return;
    this.accumulator += Math.min(delta / 1000, .1);
    while (this.accumulator >= STEP) {
      const events = this.simulation.step(STEP); this.accumulator -= STEP;
      for (const event of events) this.handle(event);
      if (this.finished || this.exhausted) break;
    }
    for (const t of this.simulation.targets) if (t.hp > 0) this.targets.get(t.id)?.update(t, this.simulation.elapsed);
    this.layoutLabels();
    const p = this.simulation.projectile;
    if (p && !this.finished) {
      this.trail.push({ x: p.x, y: p.y }); if (this.trail.length > 45) this.trail.shift();
      this.fx.trail(this.path, this.trail, p.kind);
      this.trailTimer += Math.min(delta, 50);
      if (this.trailTimer >= 45) { this.fx.flight(p.x, p.y, p.kind); this.trailTimer = 0; }
      this.drawProjectile(p.x, p.y, p.kind, Math.atan2(p.vy, p.vx));
    }
  }
  private handle(event: ShotEvent) {
    if (event.type === 'target') {
      const target = this.simulation.targets[event.id], item = this.targets.get(event.id);
      item?.update(target, this.simulation.elapsed);
      if (event.destroyed) {
        this.ctx.tracker.record(target.term, true); item?.hide();
        this.fx.rescue(target.x, target.y); void this.ctx.audio.speak(`${target.term}. Great shot!`);
      } else {
        this.fx.ring(target.x, target.y, 58, event.kind === 'frost' ? 0x9ce7ff : 0xf3cf79);
        void this.ctx.audio.speakWord(target.term);
      }
      this.updateHud();
      if (this.simulation.remaining === 0 && !this.finished) {
        this.finished = true; this.simulation.projectile = undefined; this.hideProjectile(); this.path.clear();
        this.ctx.feedback('All Word Stars rescued! Bé đã giải cứu hết mục tiêu!');
        this.timers.push(this.ctx.scene.time.delayedCall(1000, () => { if (!this.disposed) this.ctx.complete(); }));
      }
    } else if (event.type === 'obstacle') {
      const o = this.simulation.obstacles[event.id]; this.obstacles.get(o.id)?.damage(o);
      if (o.hp === 0) this.fx.breakObstacle(o);
    } else if (event.type === 'impact') this.fx.impact(event.x, event.y, event.kind, event.material);
    else if (event.type === 'blast' || event.type === 'frost') this.fx.blast(event.x, event.y, event.radius, event.type === 'frost');
    else if (event.type === 'bounce') this.fx.ring(event.x, event.y, 43, 0xd2b4f9);
    else if (event.type === 'settled') {
      this.hideProjectile(); this.path.clear();
      if (this.finished) return;
      if (!event.useful) {
        this.misses++; this.ctx.tracker.record(this.level.targetVocabulary[0], false);
        this.ctx.feedback('Thử đổi góc, lực hoặc loại đạn nhé!');
        if (this.misses >= this.level.hintPolicy.afterWrongAttempts && !this.hinted) {
          this.hinted = true; this.ctx.tracker.hintCount++;
          this.ctx.feedback('Thiên thạch phá đá; hạt nổ phá gỗ; hạt băng giữ mục tiêu đứng yên.');
          for (const [id, item] of this.targets) if (this.simulation.targets[id].hp > 0) item.halo.setVisible(true);
        }
      } else this.ctx.feedback(this.selected === 'frost' ? 'Băng giữ mục tiêu 5 giây. Chọn đạn tiếp theo nhé!' : 'Đường đã mở! Ngắm mục tiêu tiếp theo nhé.');
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
    this.ammoHint.setText(`${ammoRules[this.selected].name} · ${ammoRules[this.selected].description}`);
    for (const [kind, button] of this.buttons) {
      button.count.setText(`×${this.stock[kind]}`);
      button.background.setStrokeStyle(kind === this.selected ? 4 : 2, kind === this.selected ? 0x438d77 : 0xddc592);
      button.background.setFillStyle(kind === this.selected ? 0xe4efc7 : 0xfff8de, kind === this.selected ? .85 : .4);
      button.view.setAlpha(this.stock[kind] > 0 ? 1 : .4);
    }
  }
  private drawSling() {
    const { x, y } = this.origin, g = this.sling;
    g.clear().lineStyle(23, 0x815b40).lineBetween(x, FLOOR - 7, x, y + 54).lineBetween(x, y + 54, x - 34, y - 5).lineBetween(x, y + 54, x + 34, y - 5);
    g.lineStyle(15, 0xc79862).lineBetween(x - 2, FLOOR - 7, x - 2, y + 54).lineBetween(x - 2, y + 54, x - 34, y - 5).lineBetween(x - 2, y + 54, x + 34, y - 5);
    g.lineStyle(4, 0xf4ce91).lineBetween(x - 6, FLOOR - 13, x - 6, y + 59).lineBetween(x - 8, y + 39, x - 35, y - 4);
    for (let i = 0; i < 3; i++) g.lineStyle(5, 0x5c9c94).lineBetween(x - 11, y + 71 + i * 8, x + 11, y + 71 + i * 8);
    const pulling = this.dragging !== undefined;
    const px = pulling ? x - Math.cos(this.angle) * this.power * MAX_PULL : x;
    const py = pulling ? y + Math.sin(this.angle) * this.power * MAX_PULL : y;
    g.lineStyle(9, 0x795245).lineBetween(x - 34, y - 5, px, py).lineBetween(x + 34, y - 5, px, py);
    g.lineStyle(3, 0xc49a72).lineBetween(x - 34, y - 8, px, py - 3).lineBetween(x + 34, y - 8, px, py - 3);
    g.fillStyle(0xf4d291).fillCircle(x - 34, y - 5, 7).fillCircle(x + 34, y - 5, 7);
  }
  private drawAim() {
    if (!this.canAim()) return;
    this.drawSling(); this.path.clear();
    const dots = this.power >= .12 ? this.simulation.preview(this.origin, this.angle, this.power, this.selected) : [];
    dots.forEach((p, i) => {
      this.path.fillStyle(0xffffff, .75).fillCircle(p.x, p.y, this.hinted ? 6 : 5);
      this.path.fillStyle(ammoRules[this.selected].color, .55 + i / Math.max(1, dots.length) * .4).fillCircle(p.x, p.y, this.hinted ? 4 : 3);
    });
    const pulling = this.dragging !== undefined;
    this.drawProjectile(this.origin.x - (pulling ? Math.cos(this.angle) * this.power * MAX_PULL : 0), this.origin.y + (pulling ? Math.sin(this.angle) * this.power * MAX_PULL : 0), this.selected, -this.angle);
    this.aimLabel.setText(`${Math.round(Phaser.Math.RadToDeg(this.angle))}°   ·   Lực ${Math.round(this.power * 100)}%`);
  }
  private drawProjectile(x: number, y: number, kind: AmmoKind, rotation: number) {
    const size = ammoRules[kind].radius * 2.9;
    this.projectileArt.setTexture(`shot:ammo:${kind}`).setPosition(x, y).setRotation(rotation).setDisplaySize(size, size).setVisible(true);
    this.projectileGlow.setPosition(x, y).setDisplaySize(size * 1.65, size * 1.65).setTint(ammoRules[kind].color).setAlpha(kind === 'frost' ? .7 : .4).setVisible(true);
  }
  private hideProjectile() { this.projectileArt.setVisible(false); this.projectileGlow.setVisible(false); }
  private showExhausted() {
    this.exhausted = true; this.hideProjectile(); this.path.clear(); this.updateHud();
    this.ctx.feedback('Hết đạn rồi — đổi chiến thuật và thử lại nhé.');
    const s = this.ctx.scene;
    const shade = s.add.rectangle(800, 450, 1600, 900, 0x294336, .45).setInteractive();
    const card = woodPanel(s, 740, 360, 0xfff5d5).setPosition(800, 410);
    const title = this.text(800, 296, 'Một chiến thuật mới nhé!', 38);
    const subtitle = this.text(800, 355, `Đã cứu ${this.layout.targets.length - this.simulation.remaining}/${this.layout.targets.length} mục tiêu trong ${this.shots} phát.`, 26);
    const note = this.text(800, 403, 'Giữ nguyên địa hình · Nạp lại sáu loại đạn', 22, '#849373');
    const button = s.add.rectangle(800, 501, 385, 92, 0x468c74).setInteractive({ useHandCursor: true });
    const label = this.text(800, 501, '↻  Thử lại  (R)', 30, '#ffffff');
    button.on('pointerdown', () => this.retry());
    this.emptyOverlay = this.add(s.add.container(0, 0, [shade, card, title, subtitle, note, button, label]).setDepth(40));
  }
  private retry() {
    if (!this.exhausted || this.disposed) return;
    this.emptyOverlay?.destroy(); this.emptyOverlay = undefined; this.fx.clear();
    for (const item of this.targets.values()) item.view.destroy(); this.targets.clear();
    for (const item of this.obstacles.values()) item.view.destroy(); this.obstacles.clear();
    this.objects = this.objects.filter(o => o.active);
    this.simulation = new ShotSimulation(this.layout); this.stock = { ...this.layout.stock };
    this.exhausted = false; this.selected = 'normal'; this.shots = 0; this.misses = 0; this.hinted = false;
    this.accumulator = 0; this.trail = []; this.buildTargets(); this.updateHud(); this.drawAim();
    this.ctx.feedback('Sẵn sàng! Thử thiên thạch với đá hoặc hạt băng với mục tiêu di chuyển.');
  }
  dispose() {
    this.disposed = true;
    this.cleanups.forEach(fn => fn()); this.timers.forEach(t => t.remove(false)); this.fx.dispose();
    for (const object of this.objects) if (object.active) { this.ctx.scene.tweens.killTweensOf(object); object.destroy(); }
    this.objects = []; this.cleanups = []; this.targets.clear(); this.obstacles.clear(); this.buttons.clear();
  }
}
