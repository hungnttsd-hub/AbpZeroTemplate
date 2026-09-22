import { hasWordArt, wordSvg } from '../art';

export const bellColors: Record<string, string> = { red: '#ed7970', blue: '#6eb9e5', yellow: '#f6cf62', green: '#7dbd91', orange: '#f8ab63', pink: '#efa5bd', purple: '#ad97df', black: '#3a4856', white: '#fffdf4', brown: '#b58560' };
const eyes = '<circle cx="84" cy="75" r="4" fill="#36574f"/><circle cx="116" cy="75" r="4" fill="#36574f"/><path d="M92 90q8 8 16 0" fill="none"/>';
const wheels = '<g fill="#52716e"><circle cx="52" cy="146" r="20"/><circle cx="150" cy="146" r="20"/></g><g fill="#f8e2a4"><circle cx="52" cy="146" r="9"/><circle cx="150" cy="146" r="9"/></g>';
const glass = '<path d="M62 73h65l15 30H48Z" fill="#b7e5e4"/><path d="m65 80-9 14m31-14-9 14" stroke="#fff9de" stroke-width="6"/>';
const clothes = (body: string) => body + '<path d="M77 60q23 16 46 0" fill="none" stroke="#fff1c4" stroke-width="5"/>';
const cup = '<path d="M142 71h16q28 1 19 31-5 16-33 16" fill="none" stroke="#a3cdba" stroke-width="14"/><path d="M42 63h104v63q-2 28-51 28-51-2-53-28Z" fill="#8ebfaf"/><ellipse cx="94" cy="62" rx="52" ry="12" fill="#e8eee0"/><path d="M58 84v34" stroke="#d8edcf" stroke-width="8"/>';
const pencil = '<path d="m44 139 76-106 24 17-77 106-35 19Z" fill="#f4cc6f"/><path d="m120 33 7-11q9-12 22-2l8 6q9 7 2 18l-15 17Z" fill="#eda3ac"/><path d="m42 141 25 15-35 19Z" fill="#eed8aa"/><path d="m34 163-2 12 13-5Z" fill="#536b69"/><path d="m120 37 22 16" stroke="#e6ecdf" stroke-width="9"/><path d="m56 135 65-88" stroke="#fff0b6" stroke-width="5"/>';
const fruit = (fill: string, body: string) => `<path d="M100 46q-5-21 9-31" fill="none" stroke="#937443" stroke-width="8"/><path d="M106 37q23-25 47-10-20 24-47 10" fill="#83b97c"/><g fill="${fill}">${body}</g><path d="M67 81q-12 12-10 32" stroke="#fff4d3" stroke-width="8" fill="none" opacity=".65"/>`;
const extra: Record<string, string> = {
  rectangle: '<rect x="20" y="47" width="160" height="108" rx="13" fill="#f6ce70"/><path d="M34 64h124" stroke="#fff1b9" stroke-width="6"/>',
  oval: '<ellipse cx="100" cy="100" rx="80" ry="53" fill="#e5a9c4"/><path d="M42 82q22-25 66-25" fill="none" stroke="#ffe4ed" stroke-width="7"/>',
  turtle: '<ellipse cx="91" cy="117" rx="61" ry="39" fill="#81b77c"/><ellipse cx="156" cy="105" rx="27" ry="25" fill="#a6cd87"/><path d="M58 148v15m62-15v15" stroke="#81b77c" stroke-width="17"/><path d="m67 91 38-6 22 30-22 24-34-8-17-24Zm0 0-12-8m50 56 5 15m17-39 23 2" fill="#a7c886"/><circle cx="166" cy="97" r="4" fill="#36574f"/>',
  zebra: '<path d="m57 49-5-33 31 25m35 0 30-25-4 34" fill="#f8f4df"/><ellipse cx="100" cy="94" rx="57" ry="59" fill="#f8f4df"/><path d="m81 40 8 26m30-26-8 26M48 76l24 9m-29 12 27 6m84-27-24 9m27 12-27 6" stroke="#496064" stroke-width="9"/><ellipse cx="100" cy="125" rx="36" ry="22" fill="#b6c4bf"/>'+eyes,
  chicken: '<path d="M92 48q-30-39-3-37 19-26 23 4 35-3 12 37" fill="#e89080"/><ellipse cx="100" cy="112" rx="66" ry="61" fill="#fff1c5"/><path d="m84 94 17-13 17 13-17 19Z" fill="#efb75d"/><circle cx="77" cy="77" r="5" fill="#36574f"/><circle cx="122" cy="77" r="5" fill="#36574f"/><path d="M61 122q12 25 27 1m25 0q15 23 27-1" fill="none"/>',
  banana: '<path d="M30 39q32 87 131 39-39 113-114 66Q12 117 30 39Z" fill="#f5d270"/><path d="M36 55q29 99 117 33" fill="none" stroke="#ffe9a0" stroke-width="11"/><path d="m28 36 7 13m122 31 10-6" stroke="#9c7d48" stroke-width="10"/>',
  pear: fruit('#b4cd79','<path d="M83 43q17-11 32 2l11 42q47 41 26 69-18 28-56 22-56 5-59-35-1-30 34-59Z"/>'),
  tomato: fruit('#ef8973','<circle cx="100" cy="113" r="65"/><path d="m100 60-35-6 18 20-24 10 32 1 11 25 10-25 31-2-21-16 11-18-26 11Z" fill="#6dab70"/>'),
  grape: '<path d="M105 37q6-22 23-24" fill="none" stroke="#897442" stroke-width="8"/><path d="M110 32q-42-38-69-5 22 24 63 12" fill="#95bd72"/>'+[ [70,66],[112,63],[143,86],[51,97],[93,96],[71,132],[116,129],[95,162] ].map(([x,y])=>`<circle cx="${x}" cy="${y}" r="23" fill="#ad94d3"/><path d="M${x-11} ${y-5}q2-7 8-8" stroke="#dfc8f2" stroke-width="5"/>`).join(''),
  watermelon: '<path d="M21 61h158q-5 111-79 116Q27 169 21 61Z" fill="#83b980"/><path d="M34 61h133q-4 91-67 102-64-11-66-102Z" fill="#ec8c83"/><path d="M34 65q8 91 66 98 62-11 66-98" fill="none" stroke="#d8e8a3" stroke-width="7"/><g fill="#594e52"><ellipse cx="69" cy="96" rx="3" ry="7"/><ellipse cx="129" cy="96" rx="3" ry="7"/><ellipse cx="100" cy="137" rx="3" ry="7"/></g>',
  carrot: '<path d="M102 54q2-39 20-41m-17 40q-26-32-25-40m30 43q31-25 42-22" fill="none" stroke="#84b16d" stroke-width="13"/><path d="M58 88q-8-28 32-37 48-6 51 30L56 176Z" fill="#f3ae6c"/><path d="m71 98 28 10m-40 20 21 10m21-68 23 7" stroke="#d58c4d" stroke-width="4"/>',
  potato: '<path d="M47 57q51-46 98-1 43 48 12 87-23 41-80 21Q10 142 47 57Z" fill="#d5b789"/><g fill="#a9875d"><ellipse cx="73" cy="84" rx="4" ry="7"/><ellipse cx="129" cy="123" rx="5" ry="3"/><ellipse cx="83" cy="142" rx="4" ry="3"/></g><path d="M55 96q-3-26 21-38" fill="none" stroke="#ecd1a0" stroke-width="8"/>',
  egg: '<path d="M100 23C72 24 39 93 42 132q7 47 58 47 49 0 58-47 4-43-58-109Z" fill="#fff4d9"/><ellipse cx="79" cy="107" rx="10" ry="24" fill="#fffdf0" stroke="none"/>',
  bread: '<path d="M49 78C12 34 63 14 100 32c39-18 88 6 51 47v88H49Z" fill="#c5945f"/><path d="M59 76C34 47 72 28 101 46c30-19 68 4 40 31v79H59Z" fill="#f8dba6"/><g fill="#d6b680"><circle cx="80" cy="92" r="4"/><circle cx="118" cy="125" r="4"/><circle cx="79" cy="140" r="3"/></g>',
  cookie: '<circle cx="100" cy="100" r="72" fill="#d3a06b"/><circle cx="98" cy="96" r="65" fill="#edc78c"/>'+[[63,69],[108,53],[143,93],[83,110],[120,139],[54,125]].map(([x,y])=>`<rect x="${x-7}" y="${y-7}" width="14" height="15" rx="4" fill="#926b51" transform="rotate(18 ${x} ${y})"/>`).join(''),
  cake: '<ellipse cx="100" cy="159" rx="82" ry="17" fill="#b9d4c4"/><path d="M30 77h140v72q-70 33-140 0Z" fill="#edc997"/><path d="M30 107q70 31 140 0v18q-70 32-140 0Z" fill="#c3897d"/><ellipse cx="100" cy="76" rx="70" ry="24" fill="#f7bcca"/><path d="M30 78q12 22 25 8 14 30 29 11 14 22 28-1 14 23 29-9 14 12 29-9" fill="#f7bcca"/><path d="M100 58V25" stroke="#84baca" stroke-width="9"/><path d="M100 12q17 18 0 23-15-5 0-23" fill="#f9cf68"/>',
  rice: '<path d="M27 103h146q-6 64-73 66-64-3-73-66Z" fill="#a9c6d8"/><ellipse cx="100" cy="103" rx="73" ry="30" fill="#fff6da"/>'+Array.from({length:14},(_,i)=>`<ellipse cx="${47+(i*31)%103}" cy="${90+(i*13)%25}" rx="7" ry="3" fill="#e2d8b4" stroke="none"/>`).join(''),
  water: '<path d="M54 34h92l-13 135H68Z" fill="#d7edf0"/><path d="m60 84 9 79h63l8-79q-40 13-80 0Z" fill="#87c6de"/><path d="M69 50l5 58" stroke="#fff" stroke-width="6"/>',
  milk: '<path d="m61 52 20-29h51l14 29v123H55V52Z" fill="#fff5dc"/><path d="m61 53 21-14h51l12 14" fill="#93c6d7"/><path d="M57 103h87v50H57Z" fill="#96c9da"/><path d="M97 110q-25 27-6 31 28 6 6-31Z" fill="#fff8df"/>',
  juice: '<path d="m52 52 20-30h66l12 30v125H52Z" fill="#f8ce78"/><path d="M52 54h98v31H52Z" fill="#9cbd7b"/><circle cx="100" cy="125" r="32" fill="#f3a768"/><path d="M109 90q11-21 32-8-13 17-32 8" fill="#84b270"/><path d="M118 72V14h31" fill="none" stroke="#f1a1b9" stroke-width="7"/>',
  cup,
  plate: '<ellipse cx="100" cy="106" rx="82" ry="62" fill="#a5cbbb"/><ellipse cx="100" cy="101" rx="68" ry="48" fill="#fdf5db"/><ellipse cx="100" cy="101" rx="47" ry="30" fill="#f5eccb"/><path d="M42 83q21-34 63-27" fill="none" stroke="#fff" stroke-width="6"/>',
  spoon: '<ellipse cx="100" cy="57" rx="33" ry="43" fill="#b1d0d1"/><path d="M100 96v74" stroke="#91b3bd" stroke-width="19"/><path d="M85 35q-12 18-3 38" fill="none" stroke="#eef5dd" stroke-width="7"/>',
  fork: '<path d="M71 21v51q29 28 58 0V21m-39 0v55m20-55v55M100 88v84" fill="none" stroke="#99bfc3" stroke-width="15"/><path d="M95 111v45" stroke="#e6f5e9" stroke-width="4"/>',
  car: '<path d="M36 95 60 61h71l29 37 21 12v32H20v-27Z" fill="#e5a2a0"/>'+glass+wheels,
  bus: '<rect x="18" y="43" width="166" height="104" rx="21" fill="#f0cc75"/><path d="M33 59h133v48H33Z" fill="#a6d5dc"/><path d="M68 59v48m36-48v48m31-48v72" stroke="#edc16a" stroke-width="7"/>'+wheels,
  truck: '<rect x="16" y="45" width="103" height="98" rx="11" fill="#8db59c"/><path d="M120 83h35l27 30v31h-62Z" fill="#e7b277"/><path d="M133 91h16l22 23h-38Z" fill="#b8dfe0"/>'+wheels,
  boat: '<path d="M18 129h163l-31 37H53Z" fill="#d8a475"/><path d="M97 24v107" stroke="#ab8558" stroke-width="7"/><path d="m89 34-49 81h49Z" fill="#fff3ca"/><path d="m105 52 49 63h-49Z" fill="#e99ea1"/><path d="M15 181q18-14 38 0t38 0 38 0 38 0" fill="none" stroke="#8ac5d2" stroke-width="7"/>',
  plane: '<path d="M90 29q10-21 20 0l6 52 66 33-3 16-64-14-1 38 25 16-2 10-36-8-39 8-2-10 25-16-1-38-63 14-3-16 65-33Z" fill="#f7dfb2"/><path d="M96 49h10v38H96Z" fill="#90c8d8"/><path d="m115 94 53 28m-83-28-51 28" stroke="#eda486" stroke-width="7"/>',
  bike: '<circle cx="47" cy="135" r="33" fill="#d9e8d9"/><circle cx="153" cy="135" r="33" fill="#d9e8d9"/><path d="m47 135 32-59 37 59H47l40-40h45l21 40-25-71h22m-83 10h28" fill="none" stroke="#72aead" stroke-width="8"/><circle cx="116" cy="135" r="8" fill="#ecc482"/>',
  shirt: clothes('<path d="m69 34 31 12 31-12 48 31-24 39-19-11 4 80H59l5-80-20 11-23-39Z" fill="#87bed1"/>'),
  coat: clothes('<path d="m76 27 24 18 24-18 32 25 27 94-24 11-25-62 7 87H59l7-87-25 62-25-11 27-94Z" fill="#d3af82"/><path d="M100 48v130m-17-138 17 31 17-31M68 128h19m26 0h19" fill="none"/>'),
  dress: clothes('<path d="m73 23 27 20 27-20 27 34-25 24 45 96H26l45-96-25-24Z" fill="#d7a6ce"/><path d="M71 84h57" stroke="#f8e3b5" stroke-width="9"/><path d="M83 102 61 162m56-60 21 60" stroke="#ba87b3" stroke-width="3"/>'),
  hat: '<path d="M43 105q5-78 62-79 58 0 57 91Z" fill="#81b9b6"/><ellipse cx="100" cy="119" rx="82" ry="25" fill="#74a6a0"/><path d="M46 88q54 24 107-1v23q-54 23-111-3Z" fill="#efcc88"/><path d="M72 43q-13 8-15 25" stroke="#b9d6b9" stroke-width="8"/>',
  shoes: '<path d="M27 94h49l17 29 20 8v29H20Z" fill="#8fbccc"/><path d="M100 64h46l21 32 21 8v28h-90Z" fill="#a8c8d6"/><path d="M20 149h93m-15-27h90" stroke="#f7e9bc" stroke-width="12"/><path d="m43 113 30-1m38-30h29" stroke="#fff4db" stroke-width="7"/>',
  socks: '<path d="M40 27h46v81q-30 7-39 42-31 25-35-8 1-27 28-46Z" fill="#e9adc3"/><path d="M111 27h47v87q-27 10-35 43-30 24-38-8 0-29 26-51Z" fill="#a7bfe0"/><path d="M40 45h45m26 0h47" stroke="#fff0d5" stroke-width="12"/>',
  pencil,
  pen: pencil.replace(/#f4cc6f/g, '#80bad0').replace(/#eda3ac/g, '#a4cad7'),
  eraser: '<path d="m27 92 67-63 82 46-51 80H55Z" fill="#edb0c2"/><path d="m91 31 85 44-28 44-89-52Z" fill="#a7cbcf"/><path d="m27 92 30 18 67 45H55Z" fill="#d293a9"/>',
  ruler: '<path d="m17 125 140-78 26 42-139 79Z" fill="#eac784"/>'+Array.from({length:9},(_,i)=>`<path d="m${35+i*15} ${121-i*8.5}  ${i%2?5:10} ${i%2?9:17}" stroke="#94774d" stroke-width="3"/>`).join(''),
  bag: '<path d="M77 42q0-28 24-27 23 0 23 27" fill="none" stroke="#759891" stroke-width="10"/><rect x="41" y="37" width="120" height="142" rx="34" fill="#8fbdad"/><path d="M42 77h118" stroke="#d9e7ba" stroke-width="7"/><rect x="60" y="108" width="82" height="53" rx="14" fill="#c9d9a1"/><path d="M73 123h55" stroke="#82a383" stroke-width="4"/><circle cx="114" cy="61" r="5" fill="#f6d482"/>',
  board: '<path d="m53 168-7 18m107-18 7 18" stroke="#b08759" stroke-width="9"/><rect x="18" y="35" width="164" height="132" rx="10" fill="#c7a676"/><rect x="30" y="47" width="140" height="107" rx="5" fill="#64978a"/><path d="M51 79h43m-43 26h84m-84 26h54" stroke="#e3eaca" stroke-width="6"/><path d="M148 149h12" stroke="#faf0d0" stroke-width="6"/>',
  balloon: '<ellipse cx="100" cy="77" rx="61" ry="67" fill="#edafc9"/><path d="m100 143-9 12h18Zm0 13q20 22-3 36" fill="#edafc9"/><ellipse cx="76" cy="53" rx="12" ry="24" fill="#ffe1ee" stroke="none"/>',
  foxy: '<path d="m49 75-8-55 45 32m28 0 44-32-7 56" fill="#dc9a6d"/><path d="M38 82q62-65 124 0l-10 54-52 38-52-38Z" fill="#e7ad76"/><path d="m42 89 58 33 58-33-8 47-50 34-50-34Z" fill="#fff1ce"/><circle cx="75" cy="97" r="5" fill="#36574f"/><circle cx="125" cy="97" r="5" fill="#36574f"/><path d="m88 131 12 12 12-12Z" fill="#36574f"/>',
};
extra.shoe = extra.shoes;
extra.tree = '<path d="M82 100h36l13 80H71Z" fill="#b79564"/><path d="M97 118V73m3 63 22-23" stroke="#8c7356" stroke-width="5"/><path d="M28 109C-7 75 30 46 58 49 40 7 105-6 120 29c45-11 64 28 45 48 43 38-11 62-45 46-35 27-80 12-92-14Z" fill="#91be80"/><path d="M39 79q13-17 32-13m18-25q14-10 25-4" fill="none" stroke="#c7dc9c" stroke-width="9"/>';
const person = (teacher: boolean) => `<path d="M62 160q4-60 38-60 37 0 40 60Z" fill="${teacher ? '#b099ce' : '#8fbccc'}"/><path d="M79 153v27m42-27v27" stroke="#686b78" stroke-width="12"/><circle cx="100" cy="63" r="38" fill="#f5d0a8"/><path d="M62 61q-9-51 40-47 44-5 38 50l-15-25q-16 20-46 13l-11 18Z" fill="#936d52"/>${eyes}${teacher ? '<path d="m137 127 35-63" stroke="#c59b61" stroke-width="5"/><path d="M77 67h18m10 0h18m-27 0h8" fill="none" stroke="#64716c" stroke-width="3"/>' : '<path d="m64 111 21 38-31 15-20-34Z" fill="#efc780"/>'}`;
extra.teacher = person(true); extra.student = person(false);

function actionArt(action: string) {
  const head = '<circle cx="101" cy="58" r="29" fill="#f5ce9e"/><path d="M73 50q10-37 38-25 20 2 21 27-28-18-59-2" fill="#a47b55"/><circle cx="94" cy="58" r="3" fill="#36574f"/><circle cx="114" cy="58" r="3" fill="#36574f"/><path d="M99 70q8 6 14-1" fill="none"/>';
  const body = '<path d="M81 88q21-9 41 0l9 44H72Z" fill="#8cbcab"/><path d="m83 89 20 14 19-14" fill="#f6d57f"/>';
  const poses: Record<string, string> = {
    run: '<path d="m78 96-28 19-15-12m88-6 27-19 17 12M86 132l-28 21-18-10m72-11 24 14 9 28" fill="none" stroke="#b79877" stroke-width="12"/><path d="M24 80h26M20 64h35" stroke="#9abcc0" stroke-width="5"/>',
    jump: '<path d="m79 98-29-40m73 40 30-40M84 132l-23 31m54-31 24 30" fill="none" stroke="#b79877" stroke-width="12"/><path d="m41 181 4-15m50 22v-15m52 7-5-14" stroke="#e6bb64" stroke-width="5"/>',
    stand: '<path d="M79 94 61 128m61-34 16 34M85 132v41m28-41v41" fill="none" stroke="#b79877" stroke-width="12"/>',
    sit: '<path d="M65 139h85m-70 3v35m61-35v35" stroke="#c2a16b" stroke-width="9"/><path d="M80 97 61 119m63-22 17 22M85 134l29 6v34m1-42 28 6v33" fill="none" stroke="#b79877" stroke-width="12"/>',
    swim: '<path d="m80 96-43 20m88-20 39 16" stroke="#b79877" stroke-width="12"/><path d="M15 137q17-20 34 0t34 0 34 0 34 0 34 0M14 164q17-20 34 0t34 0 34 0 34 0 34 0" fill="none" stroke="#85bfda" stroke-width="10"/>',
    fly: '<path d="M81 101Q22 21 15 70q5 41 66 52m41-20q59-81 66-32-4 41-66 52" fill="#d6e7dd"/><path d="m85 132-8 31m37-31 8 31" stroke="#b79877" stroke-width="12"/>',
    dance: '<path d="m79 94-30-12-11-27m85 38 35 14 16-12M85 132l-19 30-21 3m69-33 24 37 21-7" fill="none" stroke="#b79877" stroke-width="12"/><path d="M154 33v28m0-28 26-7v27" stroke="#ac98ce" stroke-width="5"/><ellipse cx="147" cy="64" rx="9" ry="6" fill="#ac98ce"/><ellipse cx="173" cy="55" rx="9" ry="6" fill="#ac98ce"/>',
    read: '<path d="M39 105q30-14 62 4 30-18 62-4v61q-34-13-62 3-30-15-62-3Z" fill="#f3d590"/><path d="M101 109v58m-47-41h27m-27 16h25m37-16h29m-29 16h27" stroke="#a79264"/>',
    write: '<path d="M26 136h151m-132 3v42m114-42v42" stroke="#c3a171" stroke-width="10"/><path d="m73 96 32 27 41-8" stroke="#b79877" stroke-width="12"/><path d="M87 131h62" stroke="#fffbde" stroke-width="7"/><path d="m133 123 23-31" stroke="#dfb856" stroke-width="7"/>',
    sleep: '<path d="M25 122h149v46H25Z" fill="#94bdd1"/><path d="M24 170v14m152-14v14" stroke="#aa906c" stroke-width="9"/><rect x="33" y="103" width="53" height="32" rx="11" fill="#fff5d7"/><text x="135" y="36" font-family="Arial" font-weight="bold" font-size="24" fill="#86b0c4" stroke="none">z</text><text x="155" y="64" font-family="Arial" font-size="18" fill="#86b0c4" stroke="none">z</text>',
  };
  return `<g ${action === 'sleep' ? 'transform="rotate(-65 100 115) translate(-7 5)"' : ''}>${head}${body}</g>${poses[action] ?? poses.stand}`;
}
function weatherArt(term: string) {
  if (term === 'sunny' || term === 'hot') return wordSvg('sun');
  const cloud = '<path d="M36 111C1 67 58 34 81 51c25-47 98-10 84 21 47 24 21 60-5 59H43Z" fill="#d2e8e4"/>';
  return wrap(cloud + (term === 'rainy' ? '<path d="m58 145-9 21m54-21-9 21m53-21-9 21" stroke="#84b6da" stroke-width="9"/>' : term === 'windy' ? '<path d="M31 145h102q24-3 12-17m-102 39h107q24-3 12-17" fill="none" stroke="#84b6c4" stroke-width="6"/>' : '<path d="M100 143v35m-15-27 30 18m-30 0 30-18" stroke="#87bbd3" stroke-width="5"/>'));
}
function wrap(body: string, color?: string) {
  if (color) body = body.replace(/fill="(#[0-9a-f]{6})"/gi, (full, c: string) => ['#36574f', '#fff1ce', '#fff5dc'].includes(c.toLowerCase()) ? full : `fill="${color}"`);
  const fills = new Map<string, number>();
  body = body.replace(/fill="(#[0-9a-f]{6})"/gi, (_, c: string) => { if (!fills.has(c)) fills.set(c, fills.size); return `fill="url(#v${fills.get(c)})"`; });
  const gradients = [...fills].map(([c, i]) => `<linearGradient id="v${i}" x2=".3" y2="1"><stop stop-color="${c}"/><stop offset=".5" stop-color="${c}"/><stop offset="1" stop-color="${c}" stop-opacity=".8"/></linearGradient>`).join('');
  return `<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200" viewBox="0 0 200 200"><defs>${gradients}<filter id="shadow"><feDropShadow dx="0" dy="3" stdDeviation="2" flood-color="#56736b" flood-opacity=".16"/></filter></defs><g filter="url(#shadow)" stroke="#607c70" stroke-width="3" stroke-linejoin="round" stroke-linecap="round">${body}</g></svg>`;
}
export function assetTerm(asset: string) {
  const term = asset.replace(/^(vocab|animals|food|transport|school|home|clothes|shape|object|color)_/, '').toLowerCase();
  if (term === 'desk') return 'table';
  if (term === 'orange' && !asset.startsWith('color_')) return 'orange-fruit';
  return term;
}
export function vocabularySvg(asset: string, color?: string) {
  if (/^actions?_/.test(asset)) return wrap(actionArt(asset.split('_').at(-1)!));
  if (asset.startsWith('weather_')) return weatherArt(asset.slice(8));
  const term = assetTerm(asset), fill = color ? bellColors[color] : undefined;
  if (extra[term]) return wrap(extra[term], fill);
  if (hasWordArt(term)) return wordSvg(term, fill);
  throw new Error(`Chưa có hình minh họa cho ${asset}.`);
}
export const vocabularyKey = (asset: string, color?: string) => `gb:vocab:${asset}:${color ?? ''}`;
