import type { HideSeekLevel, Rect } from './contracts';
export interface SpotLayout extends Rect { id: string }
export function forestLayout(width: number, height: number, level: HideSeekLevel) {
  const compact = height < 550 || width < 900;
  const top = compact ? 76 : 140, bottom = compact ? 90 : 175;
  const area = Math.max(130, height - top - bottom);
  const locations: Record<string, [number, number]> = level.spots.length > 4 ? {
    tree: [.33, .27], bush: [.33, .78], rock: [.58, .76], grass: [.83, .78], log: [.83, .26], back_bush: [.58, .26]
  } : { tree: [.34, .30], bush: [.47, .78], rock: [.67, .30], grass: [.84, .78] };
  const size = Math.min(width * .21, area * .53, compact ? 170 : 310);
  const spots = level.spots.map((s, i) => {
    const pos = locations[s.id] ?? [.34 + (i % 3) * .25, i < 3 ? .27 : .78];
    return { id: s.id, x: width * pos[0] - size / 2, y: Math.max(top, Math.min(height - bottom - size, top + area * pos[1] - size / 2)), width: size, height: size };
  });
  return { compact, top, bottom, spots, momo: { x: width * .12, y: top + area * .73, height: Math.min(compact ? 166 : 335, area * .93) } };
}
