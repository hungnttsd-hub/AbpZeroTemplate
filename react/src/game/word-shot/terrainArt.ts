import type Phaser from 'phaser';
import { terrainOutline, type TerrainStyle } from './terrainShapes';

const PAD = 28;
// Native proportions keep grain, knots and individual boulders readable in the stage.
const sizes: Record<TerrainStyle, [number, number]> = {
  tower: [154, 312], spire: [148, 310], roof: [176, 64], wall: [52, 238], base: [174, 88],
  trunk: [54, 275], post: [38, 262], beam: [156, 38], shield: [76, 172], crate: [96, 102],
};
const path = (d: string, fill: string, stroke = 'none', width = 2, extra = '') =>
  `<path d="${d}" fill="${fill}" stroke="${stroke}" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round" ${extra}/>`;
const ellipse = (x: number, y: number, rx: number, ry: number, fill: string, extra = '') =>
  `<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${ry}" fill="${fill}" ${extra}/>`;
const poly = (points: number[][], fill: string, extra = '') =>
  `<polygon points="${points.map(p => p.join(',')).join(' ')}" fill="${fill}" ${extra}/>`;
const defs = `<defs>
  <linearGradient id="stone" x1="0" y1="0" x2="1" y2=".85"><stop stop-color="#dce7d9"/><stop offset=".3" stop-color="#b5c8be"/><stop offset=".68" stop-color="#88a5a3"/><stop offset="1" stop-color="#587b83"/></linearGradient>
  <linearGradient id="stoneTop" x2=".3" y2="1"><stop stop-color="#edf0d8"/><stop offset="1" stop-color="#b1c8bb"/></linearGradient>
  <linearGradient id="stoneSide" x2="1" y2=".4"><stop stop-color="#8eaca9"/><stop offset="1" stop-color="#4a747c"/></linearGradient>
  <linearGradient id="moss" x2=".2" y2="1"><stop stop-color="#c3de75"/><stop offset=".4" stop-color="#95bd59"/><stop offset="1" stop-color="#4f8c54"/></linearGradient>
  <linearGradient id="wood" x2="1" y2=".3"><stop stop-color="#f4d79c"/><stop offset=".3" stop-color="#d9aa65"/><stop offset=".7" stop-color="#be864c"/><stop offset="1" stop-color="#8e5b37"/></linearGradient>
  <linearGradient id="rope" x2="0" y2="1"><stop stop-color="#fff0bd"/><stop offset=".5" stop-color="#dfb977"/><stop offset="1" stop-color="#aa7947"/></linearGradient>
  <linearGradient id="metal" x2=".1" y2="1"><stop stop-color="#91c3ae"/><stop offset=".5" stop-color="#568e84"/><stop offset="1" stop-color="#356966"/></linearGradient>
  <linearGradient id="crystal" x2="1" y2="1"><stop stop-color="#d7fbef"/><stop offset=".4" stop-color="#8bdbd4"/><stop offset="1" stop-color="#448da7"/></linearGradient>
</defs>`;

function grass(x: number, y: number, length: number, flowers = true) {
  let svg = '';
  // Overlapping scallops form a thick living moss cap, with lit tips and little tufts.
  for (let i = 0; i < 7; i++) {
    const cx = x + length * i / 6;
    svg += ellipse(cx, y + 2, length / 8 + 3, 6 + i % 3, '#557e4b');
    svg += ellipse(cx - 1, y - 1, length / 8 + 2, 5 + i % 3, 'url(#moss)');
    svg += path(`M${cx - 4} ${y - 3}q-5-7-3-10m3 10q1-9 6-13m-6 13q6-6 10-6`, 'none', i % 2 ? '#7caa51' : '#a8ce67', 2);
  }
  if (flowers) for (let i = 0; i < 2; i++) {
    const cx = x + length * (.23 + i * .56), cy = y - 12 - i * 4;
    svg += path(`M${cx} ${y}q4-8 0 ${cy - y}`, 'none', '#558358', 2);
    for (let p = 0; p < 5; p++) svg += ellipse(cx + Math.cos(p * 1.256) * 3, cy + Math.sin(p * 1.256) * 3, 2.7, 2.7, '#fff5c7');
    svg += ellipse(cx, cy, 2, 2, '#eeb956');
  }
  return svg;
}
function ivy(x: number, y: number, length: number) {
  let svg = path(`M${x} ${y}c-13 18 11 30-1 48s5 27-2 ${length - 48}`, 'none', '#4d7950', 3);
  for (let i = 0; i < 6; i++) {
    const cy = y + 9 + i * length / 7, side = i % 2 ? -1 : 1;
    svg += path(`M${x} ${cy}q${13 * side}-15 ${17 * side}-5q${-2 * side} 13 ${-17 * side} 5Z`, 'url(#moss)', '#59884e', 1);
    svg += path(`M${x} ${cy}l${11 * side}-4`, 'none', '#c0d982', 1);
  }
  return svg;
}
function crystal(x: number, y: number, size: number) {
  return path(`M${x} ${y}l${-size * .42} ${-size * .62}l${size * .18} ${-size * .55}l${size * .42} ${-size * .13}l${size * .39} ${size * .69}L${x + size * .34} ${y}Z`, 'url(#crystal)', '#609b9a', 1.5)
    + path(`M${x} ${y}l${size * .18} ${-size * 1.3}m0 0l${-size * .28} ${size * .61}l${-size * .32} ${size * .07}m.5-.5 ${size * .7} ${size * .12}`, 'none', '#d9fff1', 1.5);
}
function rope(x: number, y: number, width: number) {
  let svg = '';
  for (let r = 0; r < 3; r++) {
    const cy = y + r * 5;
    svg += path(`M${x} ${cy}q${width / 2} 5 ${width} 0`, 'none', '#8a653d', 7);
    svg += path(`M${x} ${cy - 1}q${width / 2} 5 ${width} 0`, 'none', 'url(#rope)', 5.5);
    for (let c = 3; c < width; c += 7) svg += path(`M${x + c} ${cy - 1}l-2 4`, 'none', '#b68b50', 1);
  }
  return svg + ellipse(x + width * .73, y + 6, 5, 8, '#e5bd7f', 'stroke="#a47b49" stroke-width="1.5"')
    + path(`M${x + width * .73} ${y + 13}q8 12 2 20`, 'none', '#dbc28c', 4);
}

function stoneArt(style: TerrainStyle, w: number, h: number) {
  const point = (x: number, y: number) => [x * w / 100, y * h / 100];
  const facet = (points: number[][], fill: string, extra = '') => poly(points.map(([x, y]) => point(x, y)), fill, extra);
  let svg = '';
  if (style === 'tower') {
    // Three offset, chamfered boulders instead of a flat extruded rectangle.
    for (const [top, bottom, tilt] of [[0, 32, -4], [32, 65, 3], [65, 100, -3]]) {
      svg += facet([[0, top], [100, top + tilt], [100, bottom], [0, bottom + tilt]], 'url(#stone)');
      svg += facet([[3, top], [89, top - 2], [73, top + 8], [20, top + 10]], 'url(#stoneTop)');
      svg += facet([[82, top + 7], [100, top], [100, bottom], [72, bottom - 2]], 'url(#stoneSide)');
      svg += facet([[4, bottom - 4], [48, bottom - 1], [92, bottom - 5], [100, bottom + 1], [0, bottom + 2]], '#537b7d');
      svg += path(`M${w * .12} ${h * (top + 12) / 100}q${w * .26} 5 ${w * .48}-1`, 'none', '#eff1d8', 2, 'opacity=".6"');
      svg += path(`M${w * .35} ${h * (top + 14) / 100}l-6 11 10 8-4 13`, 'none', '#76948f', 1.7);
    }
  } else if (style === 'spire' || style === 'wall') {
    svg += facet([[24, 0], [82, 0], [63, 32], [76, 51], [48, 100], [0, 100], [20, 57], [11, 33]], 'url(#stone)');
    svg += facet([[82, 0], [100, 0], [100, 100], [48, 100], [76, 51], [63, 32]], 'url(#stoneSide)');
    svg += facet([[26, 0], [77, 0], [60, 14], [16, 22]], 'url(#stoneTop)');
    svg += facet([[10, 36], [48, 31], [65, 36], [53, 58], [9, 70]], '#b0c5b8', 'opacity=".55"');
    svg += path(`M${w * .2} ${h * .3}l${w * .3}-9 ${w * .2} 8m${-w * .07} 5-6 21 9 18m${-w * .4} ${h * .27}q${w * .4} 12 ${w * .72}-3`, 'none', '#608785', 2);
    svg += path(`M${w * .21} ${h * .31}l${w * .27}-8m${-w * .34} ${h * .37}q${w * .2} 8 ${w * .36} 4`, 'none', '#d8e2c9', 2);
  } else {
    svg += facet([[0, 0], [100, 0], [100, 100], [0, 100]], 'url(#stone)');
    svg += facet([[0, 0], [89, 0], [76, 31], [21, 37], [0, 20]], 'url(#stoneTop)');
    svg += facet([[85, 13], [100, 0], [100, 100], [65, 100], [72, 45]], 'url(#stoneSide)');
    svg += facet([[0, 75], [34, 89], [76, 74], [100, 60], [100, 100], [0, 100]], '#567d7c', 'opacity=".55"');
    svg += path(`M${w * .14} ${h * .44}q${w * .24} 10 ${w * .47}-3m${-w * .37} 15 ${w * .3} 3 ${w * .33}-9`, 'none', '#e3e6ca', 2, 'opacity=".65"');
    svg += path(`M${w * .44} ${h * .38}l-5 10 10 6-2 15`, 'none', '#78958c', 2);
  }
  // Small warm/cool mineral flecks break up broad faces without visual noise.
  for (let i = 0; i < 27; i++) {
    const x = w * (.08 + ((i * 37) % 83) / 100), y = h * (.12 + ((i * 29) % 78) / 100);
    svg += ellipse(x, y, 1.2 + i % 3, .6 + i % 2, i % 3 ? '#e9e8c8' : '#537e79', `opacity="${i % 3 ? .25 : .18}"`);
  }
  return svg;
}
function woodArt(style: TerrainStyle, w: number, h: number) {
  let svg = `<rect width="${w}" height="${h}" fill="url(#wood)"/>`;
  if (style === 'beam') {
    for (let i = 0; i < 5; i++) svg += path(`M2 ${6 + i * 7}Q${w * .3} ${i * 7 - 4} ${w * .54} ${8 + i * 6}T${w - 6} ${7 + i * 7}`, 'none', i % 2 ? '#f8dc9e' : '#a87540', 1.5);
    svg += ellipse(w * .62, h * .53, 13, 4, '#b5834b', 'stroke="#8e6339" stroke-width="1.5"');
    svg += ellipse(w - 8, h * .5, 9, h * .46, '#f0ca87', 'stroke="#865e36" stroke-width="2"');
    svg += ellipse(w - 7, h * .5, 5, h * .3, 'none', 'stroke="#b0874b" stroke-width="1.5"');
  } else {
    const planks = style === 'shield' || style === 'crate' ? 3 : 1;
    for (let p = 0; p < planks; p++) {
      const left = p * w / planks, width = w / planks;
      svg += `<rect x="${left}" width="${width}" height="${h}" fill="url(#wood)"/>`;
      if (p) svg += path(`M${left} 0v${h}`, 'none', '#795438', 2.5);
      for (let i = 0; i < 4; i++) {
        const x = left + 5 + width * i / 5;
        svg += path(`M${x} 5q-5 ${h * .13} 1 ${h * .28}t-1 ${h * .27}q-5 ${h * .2} 1 ${h * .4}`, 'none', i % 2 ? '#f7d693' : '#996536', i % 2 ? 1.2 : 1.5, 'opacity=".7"');
      }
      svg += ellipse(left + width * .55, h * (.32 + p * .12), width * .17, 12, '#b48449', 'stroke="#946432" stroke-width="1.5"');
      svg += ellipse(left + width * .55, h * (.32 + p * .12), width * .08, 6, '#926237');
    }
    if (style === 'shield') for (const cy of [h * .22, h * .79]) {
      svg += path(`M-2 ${cy}q${w * .5} 5 ${w + 4} 0v15q${-w * .5} 5 ${-w - 4} 0Z`, 'url(#metal)', '#466f65', 2);
      svg += path(`M3 ${cy + 3}q${w * .45} 4 ${w - 6} 0`, 'none', '#c1dcc0', 1.5);
      for (const cx of [w * .15, w * .5, w * .85]) svg += ellipse(cx, cy + 9, 3.5, 3.5, '#e9c789', 'stroke="#aa8044" stroke-width="1"');
    }
    if (style === 'crate') {
      svg += path(`M7 0v${h}M${w - 7} 0v${h}M0 7h${w}M0 ${h - 7}h${w}`, 'none', '#e7b776', 13);
      svg += path(`M10 ${h - 16}L${w - 10} 16`, 'none', '#8d613c', 18);
      svg += path(`M10 ${h - 18}L${w - 10} 14`, 'none', '#edc68b', 13);
      for (const x of [9, w - 9]) for (const y of [9, h - 9]) svg += ellipse(x, y, 3, 3, '#f5dfa2', 'stroke="#a6753e" stroke-width="1.5"');
    }
  }
  return svg;
}

export function terrainSvg(style: TerrainStyle) {
  const [w, h] = sizes[style], wood = ['trunk', 'post', 'beam', 'shield', 'crate'].includes(style);
  const outline = terrainOutline(style, w, h).map(p => `${p.x},${p.y}`).join(' ');
  let decoration = '';
  if (!wood) {
    decoration += grass(w * (style === 'spire' ? .35 : .18), 0, w * (style === 'wall' ? .35 : .53), style !== 'wall');
    if (style === 'tower') decoration += grass(w * .1, h * .65, w * .28, false);
    if (style === 'spire' || style === 'roof') decoration += ivy(w * .78, 2, Math.min(100, h * .72));
    if (style === 'base') decoration += crystal(15, h - 2, 27) + crystal(31, h, 18);
    if (style === 'wall') decoration += ivy(w * .65, h * .16, 103);
  } else if (style === 'post' || style === 'trunk') {
    decoration += ellipse(w * .45, 2, w * .34, 5, '#edce91', 'stroke="#986d3e" stroke-width="2"');
    decoration += ellipse(w * .45, 2, w * .2, 2.5, 'none', 'stroke="#bb8c4a" stroke-width="1.5"');
    decoration += rope(w * .03, 21, w * .88) + rope(w * .04, h - 46, w * .85);
    if (style === 'trunk') decoration += ivy(w * .5, h * .36, 100);
  } else if (style === 'beam') {
    // The wrap crosses the beam vertically; each strand catches a little light.
    for (const x of [w * .15, w * .74]) for (let i = 0; i < 3; i++) decoration += path(`M${x + i * 5} 1q-4 ${h / 2} 0 ${h - 2}`, 'none', '#eed5a0', 4.5);
  }
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w + PAD * 2}" height="${h + PAD * 2}" viewBox="${-PAD} ${-PAD} ${w + PAD * 2} ${h + PAD * 2}">${defs}
    <defs><clipPath id="body"><polygon points="${outline}"/></clipPath></defs>
    <polygon points="${outline}" fill="#315851" opacity=".15" transform="translate(3 5)"/>
    <polygon points="${outline}" fill="${wood ? 'url(#wood)' : 'url(#stone)'}"/>
    <g clip-path="url(#body)">${wood ? woodArt(style, w, h) : stoneArt(style, w, h)}</g>
    <polygon points="${outline}" fill="none" stroke="${wood ? '#8e673f' : '#648680'}" stroke-width="2.5" stroke-linejoin="round"/>
    ${decoration}</svg>`;
}

export function preloadTerrainArt(scene: Phaser.Scene) {
  for (const style of Object.keys(sizes) as TerrainStyle[]) {
    const url = `data:image/svg+xml;base64,${btoa(terrainSvg(style))}`;
    scene.load.svg(`shot:terrain:${style}`, url, { scale: 2 });
  }
}

export function terrainImage(scene: Phaser.Scene, style: TerrainStyle, width: number, height: number) {
  const [w, h] = sizes[style], sx = width / w, sy = height / h;
  return scene.add.image(-PAD * sx, -PAD * sy, `shot:terrain:${style}`).setOrigin(0)
    .setDisplaySize((w + PAD * 2) * sx, (h + PAD * 2) * sy);
}
