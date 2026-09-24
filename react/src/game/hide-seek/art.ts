import { artUrl } from '../artManifest';
import type { SpotKind, ToolId } from './contracts';

export const coverFrames: Record<SpotKind, number> = { bush: 0, rock: 1, tree_hollow: 2, tall_grass: 3, hollow_log: 4 };
export const objectFrames = ['cat', 'dog', 'tiger', 'scissors', 'apple', 'orange', 'ball', 'rabbit'];
export const objectAtlasUrl = () => artUrl('hide-seek-objects-v1.png');
const svg = (body: string) => `<svg xmlns="http://www.w3.org/2000/svg" width="160" height="160" viewBox="0 0 160 160"><defs><linearGradient id="leaf" x2=".4" y2="1"><stop stop-color="#b7e65c"/><stop offset="1" stop-color="#347c3e"/></linearGradient><radialGradient id="berry" cx=".3" cy=".25" r=".8"><stop stop-color="#c482f2"/><stop offset=".5" stop-color="#9142cc"/><stop offset="1" stop-color="#4c248c"/></radialGradient><linearGradient id="wood" x2="1" y2="1"><stop stop-color="#ffd293"/><stop offset="1" stop-color="#ba7135"/></linearGradient></defs>${body}</svg>`;
export const leafSvg = svg('<path d="M24 128Q-2 37 130 20q20 105-106 108Z" fill="url(#leaf)" stroke="#397844" stroke-width="3"/><path d="m24 128 88-87M49 103l-7-34m29 12 32-1m-13-18-2-27" fill="none" stroke="#d9efa0" stroke-width="3" opacity=".7"/>');
export const toolArt: Record<ToolId, string> = {
  swipe_leaves: svg('<path d="M22 110Q-7 28 89 30q67-4 52 82Q95 152 22 110Z" fill="url(#leaf)" stroke="#477742" stroke-width="4"/><path d="m24 108 91-63M48 86 40 53m30 13 1-28m12 32 44 14" stroke="#c5e585" stroke-width="3" fill="none"/><path d="M77 140 52 101q-12-18-1-23 8-3 19 14L54 56q-7-15 3-18 8-2 13 12l13 24-10-29q-4-12 6-14 8-1 13 12l10 24-6-19q-4-11 6-12 8 0 12 12l10 24q8-9 16-4 7 5 1 19l-13 34 8 15Z" fill="#ffd1a3" stroke="#b67542" stroke-width="3.5" stroke-linejoin="round"/>'),
  throw_net: svg('<path d="m28 138 51-50" stroke="#a96834" stroke-width="18" stroke-linecap="round"/><path d="m25 134 48-48" stroke="#ffbf62" stroke-width="9" stroke-linecap="round"/><path d="M64 48q-20 63 37 65l40-28Z" fill="#def7ff" fill-opacity=".65" stroke="#e6faff" stroke-width="4"/><g stroke="#fff" stroke-width="3" opacity=".95"><path d="m68 60 32 46m-40-25 55 13m-43 13 10-35m0 40 21-34"/></g><ellipse cx="105" cy="57" rx="29" ry="52" transform="rotate(-35 105 57)" fill="#b0eaff" fill-opacity=".25" stroke="#2678b0" stroke-width="9"/><ellipse cx="105" cy="57" rx="29" ry="52" transform="rotate(-35 105 57)" fill="none" stroke="#78d9ff" stroke-width="4"/><path d="m80 28 42 61M69 49l59 25M92 17l31 70M71 65l45-43m-28 72 43-39" stroke="#fff" stroke-width="3" opacity=".8"/>'),
  blast_berry: svg('<path d="M71 51q-11-21-4-34 21 3 26 24 5-27 25-29 12 22-17 38Z" fill="url(#leaf)" stroke="#397a40" stroke-width="3"/><path d="M53 52C9 82 31 149 81 143c58-7 63-76 25-88-20 13-26-14-53-3Z" fill="url(#berry)" stroke="#583284" stroke-width="4"/><ellipse cx="56" cy="76" rx="11" ry="16" fill="#edd1ff" opacity=".75" transform="rotate(38 56 76)"/><path d="m123 35 7-10m9 32 13-2m-24-11 13-9" stroke="#ffe18a" stroke-width="6" stroke-linecap="round"/>')
};
export const toolLabels: Record<ToolId, { en: string; vi: string; hint: string }> = {
  swipe_leaves: { en: 'Swipe Leaves', vi: 'Vạch lá', hint: 'Kéo lá sang một bên, hoặc bật Chạm để mở.' },
  throw_net: { en: 'Throw Net', vi: 'Tung lưới', hint: 'Chạm bụi cây, cỏ hoặc thân gỗ để nâng tán lá.' },
  blast_berry: { en: 'Blast Berry', vi: 'Quả mọng', hint: 'Chạm tảng đá để mở bằng quả mọng phép thuật.' }
};
export const spotNames: Record<SpotKind, string> = { tree_hollow: 'Hốc cây', bush: 'Bụi cây', rock: 'Tảng đá', tall_grass: 'Bụi cỏ', hollow_log: 'Thân gỗ' };
export const svgUrl = (source: string) => `data:image/svg+xml;base64,${btoa(Array.from(new TextEncoder().encode(source), b => String.fromCharCode(b)).join(''))}`;
export function atlasFrameStyle(index: number) {
  return { backgroundImage: `url("${objectAtlasUrl()}")`, backgroundSize: '400% 200%', backgroundPosition: `${index % 4 * 100 / 3}% ${Math.floor(index / 4) * 100}%` };
}
