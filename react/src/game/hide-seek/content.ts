import { z } from 'zod';
import type { HideSeekLevel, VocabularyEntity } from './contracts';
import { createState } from './rules';

const text = z.string().min(1).max(160);
const tool = z.enum(['swipe_leaves', 'throw_net', 'blast_berry']);
const unique = <T>(values: T[]) => new Set(values).size === values.length;
const entity = z.object({ id: text, label: text, grammar: z.enum(['singular', 'plural']), assetKey: text,
  quest: text, question: text, affirmative: text, negativeIdentification: text }).strict();
const level = z.object({ id: z.string().regex(/^HS-\d{2}$/), mechanic: z.literal('hide_seek'), contentVersion: text,
  targetEntityId: text, difficulty: z.union([z.literal(1), z.literal(2), z.literal(3)]), initialStars: z.literal(3),
  shuffleOccupants: z.boolean(), tools: z.array(tool).min(1).refine(unique),
  spots: z.array(z.object({ id: text, kind: z.enum(['tree_hollow', 'bush', 'rock', 'tall_grass', 'hollow_log']),
    entityId: text, allowedTools: z.array(tool).min(1).refine(unique), coverAssetKey: text,
    anchor: z.object({ x: z.number().min(0).max(1600), y: z.number().min(0).max(900), width: z.number().positive().max(1600), height: z.number().positive().max(900) }).strict()
  }).strict()).min(2).max(6) }).strict();
export interface HideSeekBank { schemaVersion: '1.0.0'; entities: VocabularyEntity[]; levels: HideSeekLevel[] }
export function parseContent(raw: unknown): HideSeekBank {
  const bank = z.object({ schemaVersion: z.literal('1.0.0'), entities: z.array(entity).min(1), levels: z.array(level).min(1) }).strict().parse(raw);
  if (!unique(bank.levels.map(l => l.id))) throw new Error('Mã màn Trốn tìm bị trùng.');
  for (const l of bank.levels) {
    createState(l, bank.entities);
    for (const s of l.spots) if (s.anchor.x + s.anchor.width > 1600 || s.anchor.y + s.anchor.height > 900)
      throw new Error('Chỗ ẩn nằm ngoài khu rừng.');
  }
  return bank;
}
/** All demo occupants fit the common portrait footprint of every authored spot. */
export function materializeLevel(level: HideSeekLevel, seed: number): HideSeekLevel {
  if (!Number.isInteger(seed) || seed < 1 || seed > 0x7fffffff) throw new Error('Seed không hợp lệ.');
  const ids = level.spots.map(s => s.entityId);
  let state = seed >>> 0;
  if (level.shuffleOccupants) for (let i = ids.length - 1; i > 0; i--) {
    state ^= state << 13; state ^= state >>> 17; state ^= state << 5; state >>>= 0;
    const j = state % (i + 1); [ids[i], ids[j]] = [ids[j], ids[i]];
  }
  return { ...level, spots: level.spots.map((s, i) => ({ ...s, entityId: ids[i] })) };
}
export function swipeDistance(origin: number, current: number, width: number) {
  const displacement = current - origin, threshold = Math.min(width * .55, 96);
  return { displacement: Math.max(-threshold, Math.min(threshold, displacement)), complete: Math.abs(displacement) >= threshold };
}
