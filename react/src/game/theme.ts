import Phaser from 'phaser';
import { artUrl, friends, type Friend } from './artManifest';
export { artUrl, friends } from './artManifest';
const dataUrl = (svg: string) => `data:image/svg+xml;base64,${btoa(Array.from(new TextEncoder().encode(svg), b => String.fromCharCode(b)).join(''))}`;
export function balloonSvg(color: string) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="220" height="280" viewBox="0 0 220 280"><defs><radialGradient id="b" cx="35%" cy="24%" r="80%"><stop stop-color="#ffffff" stop-opacity=".8"/><stop offset=".4" stop-color="${color}"/><stop offset="1" stop-color="${color}"/></radialGradient><linearGradient id="shade" x2="1" y2="1"><stop stop-color="#ffffff" stop-opacity=".15"/><stop offset="1" stop-color="#203e6f" stop-opacity=".18"/></linearGradient></defs><ellipse cx="110" cy="127" rx="91" ry="110" fill="url(#b)" stroke="${color}" stroke-width="5"/><ellipse cx="110" cy="127" rx="86" ry="105" fill="url(#shade)" stroke="#fff" stroke-opacity=".48" stroke-width="3"/><ellipse cx="67" cy="61" rx="13" ry="35" fill="#fff" opacity=".75" transform="rotate(33 67 61)"/><ellipse cx="163" cy="64" rx="8" ry="22" fill="#fff" opacity=".4" transform="rotate(-24 163 64)"/><path d="m110 235-12 19q12 10 24 0Z" fill="${color}" stroke="#fff" stroke-opacity=".5" stroke-width="3"/></svg>`;
}
export const balloonColors = ['#74c9ff', '#ff9d87', '#ffd768', '#c8a0ff', '#94d985', '#f07778', '#f5a5ca', '#ffb06a', '#fffcf2', '#5a6270', '#b78b6c'];
export function preloadTheme(scene: Phaser.Scene, worldId: string, theme?: string) {
  const setting = worldId === 'W02' || theme === 'island' || theme === 'jungle' ? 'forest' : worldId === 'W03' || theme === 'home' || theme === 'kitchen' || theme === 'school' ? 'home' : 'meadow';
  scene.load.image('art:backdrop', artUrl(`${setting}-v1.png`));
  scene.load.image('art:friends', artUrl('friends-v1.png'));
  scene.load.image('art:props', artUrl('props-v1.png'));
  for (const color of balloonColors) scene.load.svg(`art:balloon:${color.slice(1)}`, dataUrl(balloonSvg(color)));
}
export function prepareTheme(scene: Phaser.Scene) {
  if (scene.textures.exists('art:props')) {
    const props = scene.textures.get('art:props'); const source = props.getSourceImage();
    const size = source.width / 2;
    ['crate', 'bush', 'spring', 'pulley'].forEach((name, i) => { if (!props.has(name)) props.add(name, 0, (i % 2) * size, Math.floor(i / 2) * size, size, size); });
  }
  if (!scene.textures.exists('art:friends')) return;
  const atlas = scene.textures.get('art:friends');
  for (const [name, f] of Object.entries(friends)) if (!atlas.has(name)) atlas.add(name, 0, f.x, f.y, f.w, f.h);
}
export function character(scene: Phaser.Scene, who: Friend, x: number, y: number, height: number) {
  const frame = friends[who];
  if (scene.textures.exists('art:friends')) return scene.add.image(x, y, 'art:friends', who).setDisplaySize(height * frame.w / frame.h, height);
  // A small local fallback keeps the game readable if an optional art request fails.
  const key = `art:fallback:${who}`;
  if (!scene.textures.exists(key)) {
    const art = scene.add.graphics();
    art.fillStyle({ pip: 0xf7d370, poki: 0xfff5df, lulu: 0xf7eddf, momo: 0xa5c880 }[who]).fillEllipse(48, 81, 57, 68).fillCircle(48, 36, 34);
    art.fillStyle(0x345749).fillCircle(36, 34, 4).fillCircle(59, 34, 4);
    art.lineStyle(3, 0x345749).lineBetween(42, 48, 49, 52).lineBetween(49, 52, 56, 48);
    art.fillStyle(0xfff1be).fillEllipse(49, 87, 31, 36);
    art.generateTexture(key, 96, 125); art.destroy();
  }
  return scene.add.image(x, y, key).setDisplaySize(height * 96 / 125, height);
}
export function gameBackdrop(scene: Phaser.Scene, width: number, height: number) {
  if (scene.textures.exists('art:backdrop')) {
    const backdrop = scene.add.image(0, 0, 'art:backdrop').setOrigin(0).setDepth(-10);
    const fit = () => {
      // Cover the stage without stretching painted scenery, including tall builder layouts.
      const w = scene.scale.width, h = scene.scale.height;
      const scale = Math.max(w / backdrop.frame.realWidth, h / backdrop.frame.realHeight);
      backdrop.setScale(scale).setPosition((w - backdrop.displayWidth) / 2, (h - backdrop.displayHeight) / 2);
    };
    fit(); scene.scale.on('resize', fit);
    backdrop.once('destroy', () => scene.scale.off('resize', fit));
    return backdrop;
  }
  return scene.add.rectangle(width / 2, height / 2, width, height, 0xb9e7ed).setDepth(-10);
}
/** Soft bevelled wood panel. Interactive hitboxes remain separate from ornament. */
export function woodPanel(scene: Phaser.Scene, width: number, height: number, color = 0xf6d49c) {
  const g = scene.add.graphics();
  g.fillStyle(0x6b573e, .18).fillRoundedRect(-width / 2, -height / 2 + 7, width, height, 18);
  g.fillStyle(0xae7f4f).fillRoundedRect(-width / 2, -height / 2, width, height, 16);
  g.fillStyle(color).fillRoundedRect(-width / 2 + 5, -height / 2 + 4, width - 10, height - 12, 12);
  g.lineStyle(2, 0xffffff, .45).strokeRoundedRect(-width / 2 + 9, -height / 2 + 8, width - 18, height - 23, 9);
  return g;
}
