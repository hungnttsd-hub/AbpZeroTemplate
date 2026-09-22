import Phaser from 'phaser';
import { ammoKinds, FLOOR, type AmmoKind, type Obstacle, type ShotTarget } from './physics';
import { preloadTerrainArt, terrainImage } from './terrainArt';

const svgUrl = (body: string, size = 128) => {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 128 128">${body}</svg>`;
  return `data:image/svg+xml;base64,${btoa(Array.from(new TextEncoder().encode(svg), b => String.fromCharCode(b)).join(''))}`;
};
const star = '<path d="m64 32 9 20 22 3-16 15 4 22-19-10-19 10 4-22-16-15 22-3Z"/>';
const snow = '<path d="M64 30v68M35 47l58 34M35 81l58-34M54 36l10 9 10-9M54 92l10-9 10 9M37 58l14-4-2-13m42 29-14 4 2 13M49 87l2-13-14-4m42-29-2 13 14 4" fill="none" stroke-linecap="round" stroke-linejoin="round"/>';
const gradient = (a: string, b: string, c: string) => `<defs><radialGradient id="orb" cx="32%" cy="25%" r="80%"><stop stop-color="${a}"/><stop offset=".48" stop-color="${b}"/><stop offset="1" stop-color="${c}"/></radialGradient><linearGradient id="metal" x2=".6" y2="1"><stop stop-color="#edffff"/><stop offset=".45" stop-color="#8bdde9"/><stop offset="1" stop-color="#347990"/></linearGradient></defs>`;
const shine = '<ellipse cx="45" cy="39" rx="9" ry="16" fill="#fff9da" opacity=".7" transform="rotate(37 45 39)"/>';
const ammoArt: Record<AmmoKind, string> = {
  normal: gradient('#fff6b6', '#f8c652', '#d58b2f') + '<circle cx="64" cy="66" r="43" fill="url(#orb)" stroke="#c79038" stroke-width="4"/><path d="M54 28q8-20 33-13-6 18-29 15" fill="#96bf73" stroke="#64894e" stroke-width="3"/><g fill="#fff3bc" stroke="#d3a344" stroke-width="2">' + star + '</g>' + shine,
  explosive: gradient('#ffd6a1', '#f68d67', '#be5251') + '<path d="M59 27q-8-19 10-22" fill="none" stroke="#956244" stroke-width="7"/><path d="m75 3 5 7 9-1-6 8 4 9-10-3-7 6 1-10-7-6 9-2Z" fill="#ffdc72"/><circle cx="64" cy="70" r="42" fill="url(#orb)" stroke="#b66146" stroke-width="4"/><path d="M28 65q35 23 72 0M62 30q-13 44 7 81" fill="none" stroke="#f7c377" stroke-width="7"/><circle cx="65" cy="70" r="15" fill="#ffe1a0" stroke="#cc7950" stroke-width="3"/><g transform="translate(46 49) scale(.3)" fill="#e79750">' + star + '</g>' + shine,
  piercing: gradient('#d1fcff', '#67cada', '#387c9a') + '<path d="m17 43 32 6 14 15-14 15-32 6 9-21Z" fill="#e6c78b" stroke="#9c835c" stroke-width="3"/><path d="M30 64h48" stroke="#f5e9bd" stroke-width="15"/><path d="m56 40 58 24-58 24 10-24Z" fill="url(#metal)" stroke="#427d95" stroke-width="4"/><path d="m64 48 36 16H70" fill="#f2ffff"/><path d="m61 83 39-19H70" fill="#5facc4"/><path d="M30 60h31" stroke="#fffce6" stroke-width="3"/>',
  bouncy: gradient('#f6d9ff', '#b68af0', '#7759b9') + '<circle cx="64" cy="64" r="44" fill="url(#orb)" stroke="#725798" stroke-width="4"/><ellipse cx="64" cy="66" rx="43" ry="17" fill="none" stroke="#ffdf9b" stroke-width="8" transform="rotate(-26 64 66)"/><path d="M44 25q-8 45 36 76" fill="none" stroke="#ead8ff" stroke-width="5"/><circle cx="81" cy="55" r="11" fill="#ffeab9"/><path d="m77 54 4-4 4 4m-4-4v10" fill="none" stroke="#9070bc" stroke-width="3"/>' + shine,
  frost: gradient('#efffff', '#9de8ff', '#49a8d4') + '<path d="m64 12 32 18 18 34-18 35-32 17-32-17-18-35 18-34Z" fill="url(#orb)" stroke="#4dabc5" stroke-width="4"/><path d="M32 30 64 12l-8 40-42 12Zm64 0 18 34-35-7Z" fill="#fff" opacity=".48"/><g stroke="#fff" stroke-width="5">' + snow + '</g>',
  meteor: gradient('#efd6a1', '#c79669', '#765b53') + '<path d="m38 21 40-6 29 25 4 38-26 30-45 1-24-29 1-36Z" fill="url(#orb)" stroke="#765a4d" stroke-width="5"/><path d="m38 21 10 26-30-3m89-4-28 14 32 24M40 109l12-26-36-3" fill="none" stroke="#8b6a50" stroke-width="5"/><g fill="#ffe0a0" stroke="#c28e51" stroke-width="3">' + star + '</g><path d="M35 31 27 43m55-16 13 12" stroke="#fff0c8" stroke-width="5" stroke-linecap="round"/>'
};

export function preloadShotArt(scene: Phaser.Scene) {
  preloadTerrainArt(scene);
  for (const kind of ammoKinds) scene.load.svg(`shot:ammo:${kind}`, svgUrl(ammoArt[kind]));
  scene.load.svg('shot:halo', svgUrl('<defs><radialGradient id="g"><stop stop-color="#fffde4" stop-opacity=".85"/><stop offset=".5" stop-color="#ffe7a0" stop-opacity=".35"/><stop offset="1" stop-color="#ffe7a0" stop-opacity="0"/></radialGradient></defs><circle cx="64" cy="64" r="64" fill="url(#g)"/>'));
  scene.load.svg('shot:pod', svgUrl(gradient('#fffef0', '#f2f6df', '#b6d9ce') + '<circle cx="65" cy="68" r="55" fill="#326c73" opacity=".13"/><circle cx="64" cy="62" r="52" fill="url(#orb)" stroke="#81b9b6" stroke-width="5"/><circle cx="64" cy="62" r="46" fill="#fffcde" fill-opacity=".82" stroke="#fffbe1" stroke-width="3"/><path d="M25 46q9-23 32-26" fill="none" stroke="white" stroke-width="7" stroke-linecap="round" opacity=".9"/><path d="M35 103q30 17 59-3" fill="none" stroke="#dec07b" stroke-width="8" stroke-linecap="round"/><circle cx="64" cy="111" r="7" fill="#ffdd79" stroke="#bf9950" stroke-width="2"/>'));
  scene.load.svg('shot:spark', svgUrl('<g fill="#fff7bc">' + star + '</g>', 32));
  scene.load.svg('shot:snow', svgUrl('<g stroke="#d5faff" stroke-width="8">' + snow + '</g>', 32));
  scene.load.svg('shot:chip', svgUrl('<path d="m18 34 76-10 15 53-75 24Z" fill="#fff"/><path d="m29 45 61-10-4 17-59 8Z" fill="#d5c4a9"/>', 24));
  scene.load.svg('shot:pebble', svgUrl('<path d="m40 17 52 7 20 51-35 32-56-18-9-40Z" fill="#fff"/><path d="m40 17 15 44-34 28-9-40Z" fill="#b2bdb7"/>', 24));
  scene.load.svg('shot:puff', svgUrl('<circle cx="64" cy="64" r="47" fill="#fff" opacity=".75"/>', 32));
}

export interface LabelBounds { x: number; y: number; w: number; h: number }

export class TargetVisual {
  readonly view: Phaser.GameObjects.Container;
  readonly halo: Phaser.GameObjects.Arc;
  private shell: Phaser.GameObjects.Image;
  private health: Phaser.GameObjects.Graphics;
  private cracks: Phaser.GameObjects.Graphics;
  private ice: Phaser.GameObjects.Graphics;
  private badge: Phaser.GameObjects.Container;
  private leader: Phaser.GameObjects.Graphics;
  private badgeWidth: number;
  private homeX: number;
  private homeY: number;
  private lastHp = -1;
  constructor(scene: Phaser.Scene, target: ShotTarget) {
    this.homeX = target.homeX; this.homeY = target.homeY - target.motion;
    this.halo = scene.add.circle(0, 0, 58, 0xffdb70, .15).setStrokeStyle(3, 0xffe8a1, .6).setVisible(false);
    this.shell = scene.add.image(0, 3, 'shot:pod').setDisplaySize(122, 122);
    const picture = scene.add.image(0, -1, `word:${target.term}`).setDisplaySize(80, 80);
    const label = scene.add.text(0, -7, target.term, { fontFamily: 'Nunito, Arial', fontSize: '24px', fontStyle: 'bold', color: '#315e52' }).setOrigin(.5).setResolution(2);
    if (label.width > 146) label.setFontSize(24 * 146 / label.width);
    this.badgeWidth = Math.max(134, label.width + 28);
    const plate = scene.add.graphics();
    plate.fillStyle(0x3a655a, .15).fillRoundedRect(-this.badgeWidth / 2 + 1, -22, this.badgeWidth, 52, 16);
    plate.fillStyle(0xfff9df).fillRoundedRect(-this.badgeWidth / 2, -26, this.badgeWidth, 52, 16);
    plate.lineStyle(2, 0xd8bd79).strokeRoundedRect(-this.badgeWidth / 2, -26, this.badgeWidth, 52, 16);
    plate.lineStyle(2, 0xffffff, .9).strokeRoundedRect(-this.badgeWidth / 2 + 4, -22, this.badgeWidth - 8, 44, 12);
    this.health = scene.add.graphics(); this.cracks = scene.add.graphics(); this.ice = scene.add.graphics();
    this.ice.lineStyle(3, 0xc4f6ff, .95).strokeCircle(0, 0, 52);
    for (let i = 0; i < 8; i++) { const a = i * Math.PI / 4; this.ice.lineBetween(Math.cos(a) * 46, Math.sin(a) * 46, Math.cos(a) * 61, Math.sin(a) * 61); }
    // The shell stays behind cover; only its readable word/health badge lives above it.
    this.view = scene.add.container(target.x, target.y, [this.halo, this.shell, picture, this.cracks, this.ice]).setDepth(3);
    this.leader = scene.add.graphics().setDepth(2);
    this.badge = scene.add.container(target.x, target.y - 95, [plate, label, this.health]).setDepth(26);
    this.view.once('destroy', () => { this.badge.destroy(); this.leader.destroy(); });
    this.update(target, 0);
  }
  update(target: ShotTarget, time: number) {
    this.view.setPosition(target.x, target.y);
    const frozen = target.frozenUntil > time;
    this.ice.setVisible(frozen); this.shell.setTint(frozen ? 0x9de7fc : 0xffffff);
    if (this.lastHp === target.hp) return;
    this.lastHp = target.hp; this.health.clear(); this.cracks.clear();
    for (let i = 0; i < target.maxHp; i++) {
      const x = (i - (target.maxHp - 1) / 2) * 17;
      this.health.fillStyle(0x977947).fillCircle(x, 14, 5).fillStyle(i < target.hp ? 0xffd474 : 0xd4d6c4).fillCircle(x, 13, 3.5);
    }
    if (target.hp < target.maxHp) this.cracks.lineStyle(2, 0x648f99, .7).beginPath().moveTo(33, -38).lineTo(26, -18).lineTo(38, -5).lineTo(29, 9).strokePath();
  }
  layoutBadge(obstacles: readonly Obstacle[], occupied: LabelBounds[]) {
    if (!this.view.visible) return;
    const w = this.badgeWidth, h = 56;
    const x = Phaser.Math.Clamp(this.homeX, w / 2 + 20, 1580 - w / 2);
    let y = this.homeY - 98;
    const blockers: LabelBounds[] = [...occupied];
    for (const o of obstacles) {
      if (o.hp <= 0) continue;
      // Include decorative moss, rope and tree canopy when finding clear text space.
      blockers.push({ x: o.x - 9, y: o.y - (o.material === 'stone' ? 26 : 10), w: o.w + 18, h: o.h + 32 });
      if (o.style === 'trunk') blockers.push({ x: o.x + o.w / 2 - 84, y: o.y - 102, w: 168, h: 128 });
    }
    // Each move goes above a blocker, so the search terminates without oscillation.
    for (let pass = 0; pass <= blockers.length; pass++) {
      const overlaps = blockers.filter(r => x + w / 2 + 8 > r.x && x - w / 2 - 8 < r.x + r.w && y + h / 2 + 8 > r.y && y - h / 2 - 8 < r.y + r.h);
      if (!overlaps.length) break;
      y = Math.min(...overlaps.map(r => r.y - h / 2 - 10));
    }
    y = Math.max(115, y);
    this.badge.setPosition(x, y);
    this.leader.clear();
    const bottom = y + 29, targetTop = this.view.y - 60;
    if (targetTop > bottom + 6) {
      this.leader.lineStyle(4, 0xfff7db, .85).lineBetween(x, bottom, this.view.x, targetTop);
      this.leader.lineStyle(1.5, 0x728f68, .8).lineBetween(x, bottom, this.view.x, targetTop);
      this.leader.fillStyle(0xffedb0).fillCircle(this.view.x, targetTop, 4);
    }
    occupied.push({ x: x - w / 2, y: y - h / 2, w, h });
  }
  hide() { this.view.setVisible(false); this.badge.setVisible(false); this.leader.setVisible(false); }
}

export class ObstacleVisual {
  readonly view: Phaser.GameObjects.Container;
  private cracks: Phaser.GameObjects.Graphics;
  constructor(scene: Phaser.Scene, obstacle: Obstacle) {
    const { w, h, material } = obstacle;
    const style = obstacle.style ?? (material === 'wood' ? 'post' : 'tower');
    const body = terrainImage(scene, style, w, h);
    this.cracks = scene.add.graphics();
    this.view = scene.add.container(obstacle.x, obstacle.y, [body, this.cracks]).setDepth(4);
    if (Math.abs(obstacle.y + h - FLOOR) < 1) {
      const shadow = scene.add.ellipse(w / 2, h - 2, w + 14, 12, 0x355c48, .18);
      this.view.addAt(shadow, 0);
    }
    if (style === 'trunk' && scene.textures.exists('art:props')) {
      const leaves = scene.add.image(w / 2, -32, 'art:props', 'bush').setDisplaySize(160, 132);
      this.view.addAt(leaves, 0);
    }
  }
  damage(obstacle: Obstacle) {
    this.cracks.clear();
    if (obstacle.hp <= 0) { this.view.setVisible(false); return; }
    const { w, h } = obstacle;
    for (const [width, color, offset] of [[5, 0xffe1a4, 1.5], [2.5, obstacle.material === 'wood' ? 0x735239 : 0x4f717b, 0]]) {
      this.cracks.lineStyle(width, color, .85).beginPath()
        .moveTo(w * .58 + offset, 9).lineTo(w * .37 + offset, h * .3).lineTo(w * .7 + offset, h * .49).lineTo(w * .4 + offset, h * .73).lineTo(w * .51 + offset, h - 9).strokePath();
    }
  }
}
