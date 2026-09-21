import type { BalloonOption } from './model';
export const clamp = (n: number, a: number, b: number) => Math.max(a, Math.min(b, n));
export function aimAngle(x: number, y: number) { return clamp(Math.atan2(y - 705, x - 720), -160 * Math.PI / 180, -20 * Math.PI / 180); }
export function positionAt(option: BalloonOption, index: number, count: number, ms: number, seed: number, reduced: boolean) {
  const m = option.movement, phase = m.phase + ((seed * 31 + index * 997) % 628) / 100;
  const t = ms / m.periodMs * Math.PI * 2 + phase, k = reduced ? .65 : 1;
  const base = option.initialPosition ?? { x: (index + 1) / (count + 1), y: .4 };
  let x = base.x * 1440, y = base.y * 810;
  switch (m.pattern) {
    case 'gentleBob': x += Math.sin(t * .7) * Math.min(18, m.amplitudeX) * k; y += Math.sin(t) * Math.min(24, m.amplitudeY) * k; break;
    case 'driftHorizontal': x += Math.sin(t) * m.amplitudeX * k; y += Math.sin(t * 2) * Math.min(18, m.amplitudeY) * k; break;
    case 'driftVertical': x += Math.sin(t) * Math.min(20, m.amplitudeX) * k; y += Math.sin(t) * m.amplitudeY * k; break;
    case 'figureEight': x += Math.sin(t) * m.amplitudeX * k; y += Math.sin(t * 2) * m.amplitudeY * k; break;
    case 'crossLane': {
      // Smooth ping-pong avoids a teleporting collision at a wrap boundary.
      const cycles = m.speed === undefined ? ms / m.periodMs : ms / 1000 * m.speed / 1040;
      const p = ((cycles + phase / (Math.PI * 2)) % 2 + 2) % 2;
      x = 200 + (p < 1 ? p : 2 - p) * 1040;
      if (reduced) x = 720 + (x - 720) * k;
      y = 290 + (m.lane ?? index % 2) * 90; break;
    }
  }
  return { x: clamp(x, 190, 1250), y: clamp(y, 270, 445) };
}
/** Swept relative segment/ellipse check: fast darts cannot tunnel at low FPS. */
export function segmentHit(ax: number, ay: number, bx: number, by: number, cx: number, cy: number, rx: number, ry: number): number | undefined {
  ax = (ax - cx) / rx; ay = (ay - cy) / ry; bx = (bx - cx) / rx; by = (by - cy) / ry;
  const dx = bx - ax, dy = by - ay, a = dx * dx + dy * dy, b = 2 * (ax * dx + ay * dy), c = ax * ax + ay * ay - 1;
  if (c <= 0) return 0;
  const disc = b * b - 4 * a * c; if (a === 0 || disc < 0) return undefined;
  const t = (-b - Math.sqrt(disc)) / (2 * a); return t >= 0 && t <= 1 ? t : undefined;
}
