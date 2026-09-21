import rules from './rules.json';
import type { LevelDefinition } from '../../types';

export type AmmoKind = keyof typeof rules.ammo;
export const ammoKinds = Object.keys(rules.ammo) as AmmoKind[];
export const ammoRules = rules.ammo;
export const FLOOR = 748;
export const STEP = 1 / 120;
export const SPEED = 7.7;
export const MAX_PULL = 160;
export interface Vec { x: number; y: number }
export interface Obstacle { id: number; x: number; y: number; w: number; h: number; material: 'wood' | 'stone'; hp: number; maxHp: number }
export interface ShotTarget { id: number; x: number; y: number; homeX: number; homeY: number; radius: number; hp: number; maxHp: number; term: string; motion: number; phase: number; placement: string }
export interface ShotLayout { targets: ShotTarget[]; obstacles: Obstacle[]; gravity: number; wind: number; stock: Record<AmmoKind, number> }
export interface Projectile extends Vec { vx: number; vy: number; age: number; kind: AmmoKind; passes: number; bounces: number; touched: Set<string>; useful: boolean }
export type ShotEvent = { type: 'target'; id: number; destroyed: boolean; damage: number } | { type: 'obstacle'; id: number } | { type: 'blast'; x: number; y: number; radius: number } | { type: 'bounce'; x: number; y: number } | { type: 'settled'; useful: boolean };
const clamp = (n: number, lo: number, hi: number) => Math.max(lo, Math.min(hi, n));

/** Randomize within authored, spaced stations; retries reuse exactly the same layout. */
export function createLayout(level: LevelDefinition, random = Math.random): ShotLayout {
  const rank = (Number(level.worldId.slice(1)) - 1) * 10 + level.order - 1;
  const tier = [...rules.tiers].reverse().find(t => rank >= t.from) ?? rules.tiers[0];
  // Boss rounds remain short and leave their checkpoints intact.
  const count = level.isBoss ? 2 : tier.targets;
  const words = level.targetVocabulary;
  const layout: ShotLayout = { targets: [], obstacles: [], gravity: tier.gravity + (tier.wind ? Math.round(random() * 40 - 20) : 0),
    wind: tier.wind * (random() < .5 ? -1 : 1), stock: { normal: tier.normal, explosive: tier.explosive, piercing: tier.piercing, bouncy: tier.bouncy } };
  const obstacle = (x: number, y: number, w: number, h: number, material: Obstacle['material']) => {
    layout.obstacles.push({ id: layout.obstacles.length, x, y, w, h, material, hp: material === 'wood' ? 3 : Infinity, maxHp: material === 'wood' ? 3 : Infinity });
  };
  const placements = tier.cover ? ['branch', 'tree', 'ledge', 'cave', 'shield'] : ['branch', 'ledge'];
  // Shuffle stations, not arbitrary overlapping coordinates.
  for (let i = placements.length - 1; i > 0; i--) { const j = Math.floor(random() * (i + 1)); [placements[i], placements[j]] = [placements[j], placements[i]]; }
  for (let i = 0; i < count; i++) {
    const x = 710 + i * (680 / Math.max(count - 1, 1)) + random() * 20 - 10;
    const placement = placements[i % placements.length];
    let y = 390 + random() * 85;
    if (placement === 'tree' || placement === 'shield') y = 613 - random() * 25;
    if (placement === 'cave') y = 610;
    const radius = 44;
    layout.targets.push({ id: i, x, y, homeX: x, homeY: y, radius, hp: tier.health, maxHp: tier.health,
      term: words[i % words.length], motion: i < tier.moving ? 22 : 0, phase: random() * Math.PI * 2, placement });
    if (placement === 'branch') {
      obstacle(x - 63, y + 55, 126, 17, 'wood');
      obstacle(x + 46, y + 72, 25, FLOOR - y - 72, 'wood');
    } else if (placement === 'ledge') {
      obstacle(x - 60, y + 58, 120, FLOOR - y - 58, 'stone');
    } else if (placement === 'tree') {
      obstacle(x - 98, y - 95, 30, FLOOR - y + 95, 'wood');
    } else if (placement === 'cave') {
      // Left-facing opening: low-angle or piercing shots reach the interior.
      obstacle(x - 58, y - 112, 119, 28, 'stone');
      obstacle(x + 59, y - 112, 22, FLOOR - y + 112, 'stone');
      obstacle(x - 58, y + 58, 139, FLOOR - y - 58, 'stone');
    } else {
      obstacle(x - 95, y - 74, 23, 154, 'wood');
    }
  }
  return layout;
}

/** Segment vs expanded AABB, returning entry normal for continuous collision. */
function intersectBox(a: Vec, b: Vec, o: Obstacle, radius: number): { t: number; normal: Vec } | undefined {
  let near = 0; let far = 1; let normal = { x: 0, y: -1 };
  for (const axis of ['x', 'y'] as const) {
    const delta = b[axis] - a[axis];
    const low = o[axis] - radius; const high = o[axis] + (axis === 'x' ? o.w : o.h) + radius;
    if (Math.abs(delta) < 1e-8) { if (a[axis] < low || a[axis] > high) return; continue; }
    let t1 = (low - a[axis]) / delta; let t2 = (high - a[axis]) / delta;
    const sign = delta > 0 ? -1 : 1;
    if (t1 > t2) [t1, t2] = [t2, t1];
    if (t1 > near) { near = t1; normal = axis === 'x' ? { x: sign, y: 0 } : { x: 0, y: sign }; }
    far = Math.min(far, t2); if (near > far) return;
  }
  if (near >= 0 && near <= 1 && far >= 0) return { t: near, normal };
}
function intersectCircle(a: Vec, b: Vec, center: Vec, radius: number): number | undefined {
  const dx = b.x - a.x; const dy = b.y - a.y; const ox = a.x - center.x; const oy = a.y - center.y;
  const c = ox * ox + oy * oy - radius * radius;
  if (c <= 0) return 0;
  const aa = dx * dx + dy * dy; if (aa < 1e-9) return;
  const bb = 2 * (ox * dx + oy * dy); const d = bb * bb - 4 * aa * c; if (d < 0) return;
  const t = (-bb - Math.sqrt(d)) / (2 * aa); if (t >= 0 && t <= 1) return t;
}
const ground: Obstacle = { id: -1, x: -300, y: FLOOR, w: 2300, h: 300, material: 'stone', hp: Infinity, maxHp: Infinity };

export class ShotSimulation {
  targets: ShotTarget[]; obstacles: Obstacle[]; projectile?: Projectile;
  elapsed = 0;
  constructor(readonly layout: ShotLayout) { this.targets = layout.targets.map(t => ({ ...t })); this.obstacles = layout.obstacles.map(o => ({ ...o })); }
  launch(origin: Vec, angle: number, power: number, kind: AmmoKind) {
    const speed = clamp(power, .12, 1) * MAX_PULL * SPEED;
    this.projectile = { ...origin, vx: Math.cos(angle) * speed, vy: -Math.sin(angle) * speed, kind, age: 0,
      passes: ammoRules[kind].penetrations, bounces: ammoRules[kind].bounces, touched: new Set(), useful: false };
  }
  get remaining() { return this.targets.filter(t => t.hp > 0).length; }
  step(dt = STEP): ShotEvent[] {
    const events: ShotEvent[] = []; this.elapsed += dt;
    for (const t of this.targets) if (t.hp > 0 && t.motion) { t.y = t.homeY - Math.abs(Math.sin(this.elapsed * 1.5 + t.phase)) * t.motion; }
    const p = this.projectile; if (!p) return events;
    const ammo = ammoRules[p.kind];
    p.age += dt; p.vx += this.layout.wind * dt; p.vy += this.layout.gravity * dt;
    const start = { x: p.x, y: p.y }; const end = { x: p.x + p.vx * dt, y: p.y + p.vy * dt };
    const hits: Array<{ t: number; normal: Vec; target?: ShotTarget; obstacle?: Obstacle }> = [];
    for (const target of this.targets) {
      if (target.hp <= 0 || p.touched.has(`t${target.id}`)) continue;
      const t = intersectCircle(start, end, target, target.radius + ammo.radius);
      if (t !== undefined) hits.push({ t, normal: { x: 0, y: -1 }, target });
    }
    for (const obstacle of [...this.obstacles, ground]) {
      if (obstacle.hp <= 0 || p.touched.has(`o${obstacle.id}`)) continue;
      const hit = intersectBox(start, end, obstacle, ammo.radius); if (hit) hits.push({ ...hit, obstacle });
    }
    hits.sort((a, b) => a.t - b.t);
    for (const hit of hits) {
      p.x = start.x + (end.x - start.x) * hit.t; p.y = start.y + (end.y - start.y) * hit.t;
      if (p.kind === 'explosive') { this.explode(p, events); this.settle(events); return events; }
      if (hit.target) {
        p.touched.add(`t${hit.target.id}`); this.damageTarget(hit.target, ammo.damage, events);
      } else if (hit.obstacle) {
        const obstacle = hit.obstacle;
        if (obstacle.material === 'wood') {
          obstacle.hp = Math.max(0, obstacle.hp - ammo.damage); p.useful = true;
          events.push({ type: 'obstacle', id: obstacle.id });
        }
        if (p.kind === 'bouncy' && obstacle.material === 'stone' && p.bounces > 0) {
          const dot = p.vx * hit.normal.x + p.vy * hit.normal.y;
          p.vx = (p.vx - 2 * dot * hit.normal.x) * .86; p.vy = (p.vy - 2 * dot * hit.normal.y) * .86;
          p.x += hit.normal.x * 2; p.y += hit.normal.y * 2; p.bounces--;
          events.push({ type: 'bounce', x: p.x, y: p.y }); return events;
        }
        if (obstacle.id === -1) { this.settle(events); return events; }
        p.touched.add(`o${obstacle.id}`);
      }
      if (p.kind === 'piercing' && p.passes > 0) { p.passes--; p.vx *= .9; p.vy *= .9; continue; }
      this.settle(events); return events;
    }
    p.x = end.x; p.y = end.y;
    if (p.age > 9 || p.x < -120 || p.x > 1770 || p.y > FLOOR + 80 || p.y < -900) this.settle(events);
    return events;
  }
  private damageTarget(t: ShotTarget, damage: number, events: ShotEvent[]) {
    t.hp = Math.max(0, t.hp - damage); if (this.projectile) this.projectile.useful = true;
    events.push({ type: 'target', id: t.id, destroyed: t.hp === 0, damage });
  }
  private explode(p: Projectile, events: ShotEvent[]) {
    const radius = ammoRules.explosive.blastRadius;
    events.push({ type: 'blast', x: p.x, y: p.y, radius });
    for (const o of this.obstacles) {
      if (o.hp <= 0 || o.material !== 'wood') continue;
      const x = clamp(p.x, o.x, o.x + o.w); const y = clamp(p.y, o.y, o.y + o.h);
      if (Math.hypot(p.x - x, p.y - y) <= radius) { o.hp = 0; p.useful = true; events.push({ type: 'obstacle', id: o.id }); }
    }
    for (const t of this.targets) {
      if (t.hp <= 0 || Math.hypot(t.x - p.x, t.y - p.y) > radius + t.radius) continue;
      // Solid rock blocks blast propagation; penetrating ammo can reach these targets.
      const blocked = this.obstacles.some(o => o.hp > 0 && o.material === 'stone' && intersectBox(p, t, o, 0) !== undefined);
      if (!blocked) this.damageTarget(t, ammoRules.explosive.damage, events);
    }
  }
  private settle(events: ShotEvent[]) {
    if (!this.projectile) return;
    events.push({ type: 'settled', useful: this.projectile.useful }); this.projectile = undefined;
  }
  preview(origin: Vec, angle: number, power: number, kind: AmmoKind): Vec[] {
    // Clone the live state; preview and real flight use precisely the same integrator/collisions.
    const copy = new ShotSimulation(this.layout); copy.elapsed = this.elapsed;
    copy.targets = this.targets.map(t => ({ ...t })); copy.obstacles = this.obstacles.map(o => ({ ...o }));
    copy.launch(origin, angle, power, kind);
    const dots: Vec[] = [];
    for (let i = 0; i < 300 && copy.projectile; i++) {
      copy.step(); if (i % 12 === 0 && copy.projectile) dots.push({ x: copy.projectile.x, y: copy.projectile.y });
    }
    return dots;
  }
}
