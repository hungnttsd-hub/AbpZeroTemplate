// Original vocabulary illustrations with shared soft lighting. Shapes and semantic colors remain readable.
const colors: Record<string, string> = { red: '#f16b65', blue: '#60a5fa', yellow: '#ffd259', green: '#6cc796', orange: '#ffa553', pink: '#f497bb', purple: '#ac8be4', black: '#384552', white: '#fafbf6', brown: '#b17a56' };
const eyes = '<g fill="#304443"><circle cx="77" cy="84" r="5"/><circle cx="123" cy="84" r="5"/></g><path d="M92 103q8 9 16 0" fill="none"/>';
const animal = (color: string, ears: string, extra = '') => `${ears}<ellipse cx="100" cy="100" rx="63" ry="58" fill="${color}"/>${extra}${eyes}`;
const shapes: Record<string, string> = {
  'orange-fruit': '<circle cx="100" cy="112" r="66" fill="#ffa84e"/><path d="M101 48q-2-19 9-30" fill="none" stroke="#8c7148" stroke-width="8"/><path d="M104 43q27-37 60-16-22 32-60 16Z" fill="#8fbb6c"/><ellipse cx="73" cy="91" rx="12" ry="23" fill="#ffda93" stroke="none"/><path d="m130 138 2 1m-16 16 2 1m22-37 2 1" stroke="#e88e3b"/>',
  heart: '<path d="M100 166 34 103C-7 48 58 4 100 57c42-53 107-9 66 46Z" fill="#f2a5bc"/>',
  moon: '<path d="M140 24C71 37 57 114 124 155 47 182 8 111 36 59 57 20 103 9 140 24Z" fill="#ffdb86"/>',
  circle: '<circle cx="100" cy="95" r="62" fill="#83c6e7"/>', square: '<rect x="40" y="35" width="120" height="120" rx="14" fill="#a28ae6"/>',
  triangle: '<path d="M100 30 171 155H29Z" fill="#ffcd65" stroke-linejoin="round"/>',
  star: '<path d="m100 22 23 48 53 8-38 38 9 53-47-25-47 25 9-53-38-38 53-8Z" fill="#ffd259"/>',
  cat: animal('#efae6e', '<path d="m43 75-5-49 42 23m40 0 42-23-5 49" fill="#efae6e"/>', '<path d="m62 109-30-8m30 19-30 6m106-17 30-8m-30 19 30 6"/>'),
  dog: animal('#d7a174', '<ellipse cx="40" cy="85" rx="25" ry="49" fill="#946d56"/><ellipse cx="160" cy="85" rx="25" ry="49" fill="#946d56"/>', '<ellipse cx="100" cy="115" rx="25" ry="21" fill="#ffebd2"/><ellipse cx="100" cy="105" rx="10" ry="7" fill="#304443"/>'),
  rabbit: animal('#fff3ea', '<ellipse cx="70" cy="40" rx="20" ry="38" fill="#fff3ea"/><ellipse cx="130" cy="40" rx="20" ry="38" fill="#fff3ea"/><path d="M70 20v35m60-35v35" stroke="#efacc4" stroke-width="12"/>'),
  bear: animal('#ba8d68', '<circle cx="48" cy="43" r="26" fill="#ba8d68"/><circle cx="152" cy="43" r="26" fill="#ba8d68"/>'),
  monkey: animal('#b8825e', '<circle cx="35" cy="92" r="28" fill="#edc8a3"/><circle cx="165" cy="92" r="28" fill="#edc8a3"/>', '<ellipse cx="100" cy="101" rx="48" ry="42" fill="#f4d4ad"/>'),
  cow: animal('#fffcf2', '<path d="m50 52-10-27 30 15m60 0 30-15-10 27" fill="#ffcf70"/>', '<ellipse cx="60" cy="78" rx="18" ry="25" fill="#4b595c"/><ellipse cx="100" cy="122" rx="38" ry="22" fill="#f3b3b4"/>'),
  frog: animal('#81c881', '<circle cx="65" cy="52" r="25" fill="#81c881"/><circle cx="135" cy="52" r="25" fill="#81c881"/>'),
  horse: animal('#bd956e', '<path d="m55 63 0-44 31 31m28 0 31-31v44" fill="#bd956e"/>', '<path d="M82 44q20-30 40 0l-10 30Z" fill="#6c574d"/><ellipse cx="100" cy="134" rx="35" ry="23" fill="#e9be91"/>'),
  lion: '<circle cx="100" cy="96" r="80" fill="#b8804e"/>' + animal('#efc574', ''),
  tiger: animal('#f6b363', '<circle cx="50" cy="48" r="24" fill="#f6b363"/><circle cx="150" cy="48" r="24" fill="#f6b363"/>', '<path d="m80 45 8 22m32-22-8 22M42 92l20 6m-17 18 20-4m93-20-20 6m17 18-20-4" stroke-width="9"/>'),
  elephant: '<ellipse cx="43" cy="95" rx="35" ry="49" fill="#a7bbd1"/><ellipse cx="157" cy="95" rx="35" ry="49" fill="#a7bbd1"/>' + animal('#b9cadb', '', '<path d="M100 106v48q0 30 32 11" stroke="#a7bbd1" stroke-width="26" fill="none"/>'),
  giraffe: '<path d="M82 160V87h38v73" fill="#f4cd77"/>' + animal('#f4cd77', '<path d="M72 49V23m56 26V23" stroke-width="10"/>', '<circle cx="56" cy="101" r="12" fill="#bf9066"/><circle cx="145" cy="113" r="10" fill="#bf9066"/>'),
  fish: '<path d="m45 100-28-40v80Z" fill="#f5b56d"/><ellipse cx="109" cy="100" rx="68" ry="46" fill="#73bed1"/><circle cx="145" cy="87" r="6" fill="#304443"/><path d="m100 100-22 18v-36Z" fill="#b3e0e8"/>',
  bird: '<path d="m123 93 52 13-41 19" fill="#ffca68"/><ellipse cx="90" cy="109" rx="60" ry="46" fill="#79b9e4"/><circle cx="110" cy="75" r="32" fill="#79b9e4"/><circle cx="120" cy="69" r="5" fill="#304443"/><path d="M58 111q20 44 44-2M78 151v17m29-17v17" fill="#b8dbed"/>',
  duck: '<path d="m128 72 47 8-40 15" fill="#f5a45f"/><ellipse cx="88" cy="121" rx="62" ry="38" fill="#ffdc75"/><circle cx="110" cy="75" r="35" fill="#ffdc75"/><circle cx="121" cy="66" r="5" fill="#304443"/><path d="M56 116q22 43 47 0" fill="#efc15d"/>',
  snake: '<path d="M35 145q120 35 95-15T85 80q-8-30 36-36" fill="none" stroke="#86c785" stroke-width="29"/><ellipse cx="135" cy="42" rx="28" ry="21" fill="#86c785"/><circle cx="143" cy="36" r="4" fill="#304443"/>',
  crocodile: '<path d="m23 130 34-56 26 16 22-20 17 21 51 4v38Z" fill="#82bc85"/><circle cx="133" cy="90" r="12" fill="#a0d092"/><circle cx="136" cy="87" r="4" fill="#304443"/><path d="M134 115h39"/>',
  bee: '<ellipse cx="72" cy="65" rx="24" ry="38" fill="#d3eef0"/><ellipse cx="123" cy="65" rx="24" ry="38" fill="#d3eef0"/><ellipse cx="100" cy="111" rx="62" ry="39" fill="#fbd36c"/><path d="M80 77v68m28-71v76" stroke-width="14"/><circle cx="140" cy="100" r="5" fill="#304443"/>',
  butterfly: '<ellipse cx="59" cy="78" rx="34" ry="47" fill="#c6a1e6"/><ellipse cx="141" cy="78" rx="34" ry="47" fill="#c6a1e6"/><ellipse cx="64" cy="135" rx="30" ry="32" fill="#e4b5d2"/><ellipse cx="136" cy="135" rx="30" ry="32" fill="#e4b5d2"/><path d="M100 66v89m0-89L82 42m18 24 18-24" stroke-width="10"/>',
  bed: '<path d="M26 67v95m148-43v43" stroke-width="12"/><rect x="31" y="83" width="142" height="57" rx="9" fill="#87bfbb"/><rect x="38" y="74" width="43" height="29" rx="8" fill="#fffdf3"/><path d="M92 87v49"/>',
  chair: '<rect x="60" y="27" width="80" height="81" rx="14" fill="#f0bf76"/><path d="M59 123v49m81-49v49" stroke-width="11"/><rect x="46" y="105" width="108" height="24" rx="8" fill="#c89b6c"/>',
  table: '<path d="M46 96v75m108-75v75" stroke-width="15"/><rect x="21" y="65" width="158" height="37" rx="10" fill="#daa978"/>',
  sofa: '<rect x="40" y="54" width="120" height="73" rx="22" fill="#b2a1d9"/><rect x="25" y="98" width="150" height="54" rx="16" fill="#9686bc"/><path d="M43 149v18m114-18v18M100 58v64"/>',
  lamp: '<path d="M100 94v65" stroke-width="11"/><path d="m68 30-30 68h124l-30-68Z" fill="#ffcc76"/><ellipse cx="100" cy="163" rx="39" ry="10" fill="#d7a473"/>',
  clock: '<circle cx="100" cy="95" r="68" fill="#f3b287"/><circle cx="100" cy="95" r="55" fill="#fff9e6"/><path d="M100 54v43l28 18" stroke-width="8"/><circle cx="100" cy="95" r="6" fill="#304443"/>',
  mirror: '<ellipse cx="100" cy="88" rx="54" ry="69" fill="#e4bc87"/><ellipse cx="100" cy="88" rx="42" ry="57" fill="#c8e9ed"/><path d="m75 92 38-38m-17 62 27-28" stroke="white" stroke-width="9"/><path d="M100 158v17" stroke-width="12"/>',
  door: '<rect x="48" y="20" width="104" height="159" rx="8" fill="#c4946f"/><rect x="64" y="36" width="72" height="63" rx="6" fill="#e0b58a"/><circle cx="130" cy="120" r="7" fill="#f8d573"/>',
  window: '<rect x="28" y="28" width="144" height="133" rx="10" fill="#eedac1"/><rect x="40" y="40" width="120" height="109" fill="#a8dce9"/><path d="M100 38v112M40 94h120" stroke="#eedac1" stroke-width="11"/>',
  toy: '<rect x="35" y="84" width="131" height="48" rx="18" fill="#f18b77"/><path d="m65 83 15-31h48l22 31" fill="#92c7d0"/><circle cx="63" cy="136" r="20" fill="#45535c"/><circle cx="139" cy="136" r="20" fill="#45535c"/>',
  book: '<path d="M100 52q-38-23-75-7v109q39-17 75 7 36-24 75-7V45q-37-16-75 7Z" fill="#fff8e5"/><path d="M100 53v105M41 69l41 8m-41 14 41 8m35-22 40-8m-40 30 40-8" stroke="#83aca1"/>',
  box: '<path d="m35 63 64-31 66 31v90l-66 22-64-22Z" fill="#dcb47a"/><path d="m35 63 64 25 66-25M99 88v87m-36-126 67 27v39" fill="none"/>',
  ball: '<circle cx="100" cy="96" r="65" fill="#8dc8d4"/><path d="M37 90h126M76 37q62 50 0 120" fill="none" stroke="#fff8eb" stroke-width="16"/>',
  kite: '<path d="m100 24 54 65-54 53-54-53Z" fill="#f4b187"/><path d="M100 24v119M46 89h108m-54 53q-27 17 0 33" fill="none"/>',
  flower: '<path d="M100 85v90m0-21q-38-1-39-26 32-1 39 26m0-8q38-1 39-26-32-1-39 26" fill="#92c88b"/><g fill="#eea6b7"><circle cx="75" cy="61" r="28"/><circle cx="123" cy="61" r="28"/><circle cx="100" cy="37" r="28"/><circle cx="100" cy="85" r="28"/></g><circle cx="100" cy="62" r="21" fill="#ffda7c"/>',
  kitchen: '<rect x="26" y="40" width="148" height="124" rx="10" fill="#e4d1b5"/><rect x="45" y="94" width="64" height="54" rx="6" fill="#91babe"/><circle cx="61" cy="68" r="13" fill="#576b6e"/><circle cx="97" cy="68" r="13" fill="#576b6e"/><path d="M130 58v82m-8-14h16"/>',
  bathroom: '<path d="M30 89h140l-15 59H50Z" fill="#b0dce0"/><path d="M42 90V47q0-28 30-13m-14 115-5 19m88-19 5 19" fill="none" stroke-width="9"/>',
};
shapes.teddy = shapes.bear;
shapes.doll = '<circle cx="100" cy="53" r="30" fill="#efc5a7"/><path d="m75 85-24 65h98l-24-65Z" fill="#df9dbb"/><path d="M79 152v25m42-25v25" stroke-width="13"/>';
shapes.bedroom = '<path d="M14 87 100 16l86 71v97H14Z" fill="#f2e0cc"/>' + '<g transform="translate(30 45) scale(.7)">' + shapes.bed + '</g>';
shapes.garden = '<path d="M15 153q83-56 170 0v25H15" fill="#8cc58b"/>' + '<g transform="translate(32 0) scale(.8)">' + shapes.flower + '</g>';
shapes.in = shapes.box + '<circle cx="100" cy="78" r="28" fill="#ee9278"/><path d="m35 92 64 25 66-25v61l-66 22-64-22Z" fill="#dcb47a"/>';
shapes.big = '<circle cx="102" cy="98" r="74" fill="#95cbb0"/>';
shapes.small = '<circle cx="102" cy="98" r="30" fill="#95cbb0"/>';
shapes.apple = '<path d="M101 58q-12-31 8-43" fill="none" stroke="#987457" stroke-width="10"/><path d="M106 45q4-32 39-23-3 30-39 23" fill="#96bc7e"/><path d="M101 63C39 29 18 95 50 151q26 43 51 17 26 26 51-17 31-57 10-89-21-26-61 1Z" fill="#ec8d7f"/><path d="M62 80q-15 16-6 40" stroke="#ffd5bc" stroke-width="10" fill="none"/>';
shapes.sun = '<g stroke="#e8bd69" stroke-width="9"><path d="M100 13v17m0 140v17M13 100h17m140 0h17M37 37l13 13m100 100 13 13M37 163l13-13m100-100 13-13"/></g><circle cx="100" cy="100" r="52" fill="#f5d27e"/><circle cx="83" cy="92" r="5" fill="#425853"/><circle cx="117" cy="92" r="5" fill="#425853"/><path d="M85 117q15 14 30 0" fill="none"/>';
shapes.train = '<path d="M27 144h149" stroke-width="10"/><rect x="29" y="87" width="86" height="51" rx="9" fill="#8cbcb3"/><rect x="110" y="48" width="58" height="90" rx="8" fill="#91acc7"/><rect x="121" y="61" width="33" height="30" fill="#f6dfa4"/><rect x="48" y="58" width="22" height="31" fill="#bd9780"/><circle cx="58" cy="145" r="18" fill="#526b68"/><circle cx="135" cy="145" r="18" fill="#526b68"/>';
/** Paint each solid fill with a gentle bevel without altering its semantic hue. */
function paintIllustration(art: string, override?: string) {
  const source = override && /^#[0-9a-f]{6}$/i.test(override) ? art.replace(/fill="#[0-9a-f]{6}"/gi, `fill="${override}"`) : art;
  const fills = new Map<string, number>();
  const painted = source.replace(/fill="(#[0-9a-f]{6})"/gi, (_, color: string) => {
    if (!fills.has(color)) fills.set(color, fills.size);
    return `fill="url(#paint${fills.get(color)})"`;
  });
  const shade = (color: string, delta: number) => '#' + [1, 3, 5].map(i => {
    const channel = Number.parseInt(color.slice(i, i + 2), 16);
    return Math.round(delta > 0 ? channel + (255 - channel) * delta : channel * (1 + delta)).toString(16).padStart(2, '0');
  }).join('');
  const gradients = [...fills].map(([color, i]) => `<linearGradient id="paint${i}" x1="0" y1="0" x2=".3" y2="1"><stop stop-color="${shade(color, .18)}"/><stop offset=".45" stop-color="${color}"/><stop offset="1" stop-color="${shade(color, -.13)}"/></linearGradient>`).join('');
  return `<defs>${gradients}<filter id="soft-shadow" x="-20%" y="-20%" width="140%" height="145%"><feDropShadow dx="0" dy="3" stdDeviation="2" flood-color="#284d3a" flood-opacity=".2"/></filter></defs><g filter="url(#soft-shadow)" stroke="#425853" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round">${painted}</g>`;
}
export function wordSvg(term: string, fill?: string): string {
  const art = colors[term] ? `<path d="M100 20C78 52 41 75 41 113a59 59 0 0 0 118 0c0-38-37-61-59-93Z" fill="${colors[term]}"/><ellipse cx="77" cy="98" rx="10" ry="19" fill="white" opacity=".5"/>` : shapes[term];
  return `<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200" viewBox="0 0 200 200">${paintIllustration(art ?? '<rect x="40" y="40" width="120" height="120" rx="28" fill="#d9e9e0"/><path d="m70 100 22 22 42-48" fill="none"/>', fill)}</svg>`;
}
export const wordImage = (term: string) => `data:image/svg+xml;charset=utf-8,${encodeURIComponent(wordSvg(term))}`;
export const hasWordArt = (term: string) => !!(colors[term] || shapes[term]);

// Phaser's XHRLoader calls atob for every data: URL. Unlike an HTML img,
// its SVG loader requires actual Base64 rather than percent-encoded XML.
export function wordTextureUrl(term: string, fill?: string): string {
  const bytes = new TextEncoder().encode(wordSvg(term, fill));
  return `data:image/svg+xml;base64,${btoa(Array.from(bytes, byte => String.fromCharCode(byte)).join(''))}`;
}
