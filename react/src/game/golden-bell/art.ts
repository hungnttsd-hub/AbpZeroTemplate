import Phaser from 'phaser';
import { character } from '../theme';
import { friends, type Friend } from '../artManifest';
import type { Question, SceneDescription } from './model';
import { assetTerm, vocabularyKey, vocabularySvg } from './vocabularyArt';

export const svgData = (svg: string) => `data:image/svg+xml;base64,${btoa(Array.from(new TextEncoder().encode(svg), b => String.fromCharCode(b)).join(''))}`;
export const bellSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="360" height="470" viewBox="0 0 360 470"><defs><linearGradient id="gold" x2="1" y2=".3"><stop stop-color="#ad7636"/><stop offset=".22" stop-color="#f9d888"/><stop offset=".48" stop-color="#fff1b7"/><stop offset=".73" stop-color="#e6b55c"/><stop offset="1" stop-color="#a87839"/></linearGradient><linearGradient id="wood" x2="1" y2="0"><stop stop-color="#dec295"/><stop offset=".5" stop-color="#b68755"/><stop offset="1" stop-color="#896b49"/></linearGradient><linearGradient id="jade" x2=".4" y2="1"><stop stop-color="#b8d5b5"/><stop offset="1" stop-color="#679588"/></linearGradient></defs><ellipse cx="180" cy="443" rx="155" ry="18" fill="#38594a" opacity=".18"/><g stroke="#8d754c" stroke-width="3"><path d="M37 431V75h30v356m225 0V75h30v356" fill="url(#wood)"/><path d="M18 65Q180-10 341 65v30Q180 30 18 95Z" fill="url(#wood)"/><path d="M28 66q152-65 304 0" fill="none" stroke="#f1d6a7" stroke-width="6"/><path d="m46 103 44-19m180 0 45 19" stroke="#789887" stroke-width="12"/><path d="M167 68h26v45h-26Z" fill="#d5ad67"/><path d="M137 132q-5-62 43-62 48 0 43 62" fill="none" stroke="#b1874d" stroke-width="14"/><path d="M137 132q-5-59 43-59 46 0 40 59" fill="none" stroke="#ffe3a0" stroke-width="6"/><path d="M79 290c39-29 14-170 101-170s62 141 101 170l23 29q-124 57-248 0Z" fill="url(#gold)"/><path d="M116 220q0-55 32-72" fill="none" stroke="#fff3bf" stroke-width="10" opacity=".8"/><path d="M77 290q103 40 206 0" fill="none" stroke="#a77b39" stroke-width="7"/><ellipse cx="180" cy="317" rx="124" ry="28" fill="url(#gold)"/><ellipse cx="180" cy="327" rx="101" ry="15" fill="#976d39"/><ellipse cx="180" cy="324" rx="100" ry="10" fill="#bd8b44"/><circle cx="180" cy="333" r="18" fill="#f0cc7b"/><path d="M180 349v59" fill="none" stroke="#e9cc96" stroke-width="8"/><path d="m169 402 22 0 9 28-20 14-20-14Z" fill="url(#jade)"/><circle cx="180" cy="421" r="8" fill="#efd185"/><path d="m180 164 13 27 30 4-22 22 5 30-26-14-26 14 5-30-22-22 30-4Z" fill="#ffedaa" stroke="#be8d46"/><path d="M23 431h314v18H23Z" fill="url(#jade)"/></g><g fill="#e8d19a" stroke="#997947" stroke-width="2"><circle cx="50" cy="83" r="6"/><circle cx="309" cy="83" r="6"/></g><g fill="#9cbd84"><path d="M35 170q-43-14-18-34 27 4 18 34m286 32q43-14 18-34-27 4-18 34M34 249q-41-9-20-31 29 4 20 31"/></g></svg>`;

export function questionAssets(q: Question) {
  const assets = new Map<string, { asset: string; color?: string }>();
  const add = (asset?: string, color?: string) => { if (asset && !(assetTerm(asset) in friends)) assets.set(vocabularyKey(asset, color), { asset, color }); };
  add(q.stimulus.assetKey); add(q.stimulus.meaningAssetKey);
  if (q.stimulus.weather) add(`weather_${q.stimulus.weather}`);
  for (const object of q.stimulus.objects ?? []) if (typeof object !== 'string') { add(object.assetKey, object.color ?? undefined); if (object.item) add(`vocab_${object.item}`, object.color ?? undefined); if (object.character?.toLowerCase() === 'foxy') add('vocab_foxy'); }
  if (q.stimulus.character?.toLowerCase() === 'foxy') add('vocab_foxy');
  for (const option of q.options) {
    add(option.assetKey, option.displayColor); add(option.imageAssetKey);
    option.assets?.forEach(asset => add(asset));
    if (option.scene) { add(`vocab_${option.scene.animal}`, option.scene.color); add(`vocab_${option.scene.object}`); }
    if (option.label.toLowerCase() === 'foxy') add('vocab_foxy');
    for (const phrase of option.sequence ?? []) { const [color, noun] = phrase.split(' '); add(`shape_${noun}`, color); }
  }
  return assets;
}
export function queueQuestionArt(scene: Phaser.Scene, q: Question) {
  let count = 0;
  for (const [key, { asset, color }] of questionAssets(q)) if (!scene.textures.exists(key) && !scene.load.isLoading()) { scene.load.svg(key, svgData(vocabularySvg(asset, color)), { scale: 1.5 }); count++; }
  return count;
}
export function picture(scene: Phaser.Scene, asset: string, x: number, y: number, size: number, color?: string) {
  const name = assetTerm(asset);
  if (name in friends) return character(scene, name as Friend, x, y, size);
  return scene.add.image(x, y, vocabularyKey(asset, color)).setDisplaySize(size, size);
}
export function friend(scene: Phaser.Scene, name: string, x: number, y: number, height: number) {
  return name.toLowerCase() === 'foxy' ? picture(scene, 'vocab_foxy', x, y, height) : character(scene, name.toLowerCase() in friends ? name.toLowerCase() as Friend : 'pip', x, y, height);
}
export function miniScene(scene: Phaser.Scene, description: SceneDescription, width: number, height: number) {
  const { relation, animal, object, color, size } = description;
  const result = scene.add.container(0, 0);
  const scale = Math.min(width / 265, height / 185), petSize = size === 'small' ? 57 : size === 'big' ? 99 : 77;
  result.add(scene.add.ellipse(0, 71, 245, 24, 0xa9c899, .4));
  const pet = (x: number, y: number) => picture(scene, `vocab_${animal}`, x, y, petSize, color);
  const prop = (x: number, y: number, s = 144) => picture(scene, `vocab_${object}`, x, y, s);
  const relations: Record<string, () => void> = {
    on: () => result.add([prop(0, 26), pet(0, -39)]),
    under: () => result.add([prop(0, -12, 170), pet(0, 51)]),
    behind: () => result.add([pet(8, -20), prop(0, 24, 155)]),
    'in front of': () => result.add([prop(0, -13), pet(0, 47)]),
    'next to': () => result.add([prop(54, 13, 132), pet(-64, 40)]),
    between: () => result.add([prop(-88, 17, 102), prop(88, 17, 102), pet(0, 38)]),
    in: () => {
      result.add([prop(0, 22, 165), pet(0, 3)]);
      const front = scene.add.graphics().fillStyle(object === 'bed' ? 0x8cbcb5 : 0xd9b985).fillRoundedRect(-49, 32, 98, 32, 8);
      front.lineStyle(3, 0xf4dfb0).lineBetween(-47, 32, 47, 32); result.add(front);
    },
  };
  (relations[relation] ?? relations['next to'])(); result.setScale(scale); return result;
}
export function cardPanel(scene: Phaser.Scene, width: number, height: number, selected = false) {
  const g = scene.add.graphics();
  g.fillStyle(0x826645, .19).fillRoundedRect(-width / 2 + 2, -height / 2 + 9, width, height, 26);
  g.fillStyle(selected ? 0xf5d891 : 0xe7d3a5).fillRoundedRect(-width / 2, -height / 2, width, height, 26);
  g.fillStyle(selected ? 0xfff1bd : 0xfffdf0).fillRoundedRect(-width / 2 + 4, -height / 2 + 4, width - 8, height - 12, 22);
  g.lineStyle(2, 0xffffff, .9).strokeRoundedRect(-width / 2 + 9, -height / 2 + 9, width - 18, height - 23, 18);
  return g;
}
