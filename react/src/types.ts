import { z } from 'zod';
import { wordBuilderSchema } from './game/word-builder/config';
import { balloonDartSchema, type BalloonSummary } from './game/balloon-dart/model';

export const mechanicIds = ['word_shot', 'balloon_pop', 'balloon_dart', 'drag_sort', 'letter_puzzle', 'word_builder', 'boss_challenge', 'rescue_mission', 'adventure_commands'] as const;
export type MechanicId = typeof mechanicIds[number];
export const levelSchema = z.object({
  id: z.string().regex(/^W\d{2}-L\d{2}$/), worldId: z.string().regex(/^W\d{2}$/), order: z.number().int().min(1).max(99), title: z.string(),
  mechanic: z.enum(mechanicIds), difficulty: z.union([z.literal(1), z.literal(2), z.literal(3), z.literal(4)]),
  targetVocabulary: z.array(z.string().min(1)).min(1), reviewVocabulary: z.array(z.string()).default([]),
  instruction: z.string().min(1), instructionAudioKey: z.string().default(''),
  targets: z.array(z.object({ value: z.string().min(1), correct: z.boolean(), assetKey: z.string().optional() })).min(2).max(6),
  hintPolicy: z.object({ afterWrongAttempts: z.number().int().min(1), audioReplay: z.boolean(), visualPulse: z.boolean() }),
  estimatedSeconds: z.number().min(10).max(300), learningObjective: z.string(), isBoss: z.boolean(), contentVersion: z.number().default(1), wordBuilder: wordBuilderSchema.optional(), balloonDart: balloonDartSchema.optional()
}).refine(l => l.targets.some(t => t.correct) && l.targets.some(t => !t.correct), 'Level needs correct and distractor targets');
export type LevelDefinition = z.infer<typeof levelSchema>;
export interface World { id: string; name: string; vi: string; hero: string; goal: string; theme: string }
export interface Child { id: string; nickname: string; avatarKey: string; ageBand: string }
export interface TargetResult { term: string; correct: boolean; responseMs: number }
export interface Attempt { attemptId: string; childId: string; levelId: string; startedAt: string; completedAt: string; wrongAttempts: number; hintCount: number; targetResults: TargetResult[]; balloonDart?: BalloonSummary }
export interface Progress { levelId: string; bestStars: number; completedCount: number }
export interface Mastery { term: string; exposureCount: number; correctCount: number; incorrectCount: number; masteryScore: number }
export interface Dashboard { completedLevels: number; stars: number; minutes: number; words: Mastery[]; review: Mastery[] }
export const starsFor = (a: Pick<Attempt, 'wrongAttempts' | 'hintCount' | 'balloonDart'>) => a.balloonDart?.roundsCompleted === 3 ? 3 : a.wrongAttempts === 0 && a.hintCount === 0 ? 3 : a.wrongAttempts <= 1 ? 2 : 1;
export function unlocked(level: LevelDefinition, levels: LevelDefinition[], progress: Progress[]): boolean {
  if (!levels.some(l => l.id === level.id)) return false;
  const world = Number(level.worldId.slice(1));
  if (world === 1 && level.order === 1) return true;
  // Refer to the actual predecessor, even when an admin temporarily unpublishes it.
  const previous = level.order > 1 ? `${level.worldId}-L${String(level.order - 1).padStart(2, '0')}` : `W${String(world - 1).padStart(2, '0')}-L10`;
  return progress.some(p => p.levelId === previous && p.bestStars >= 1);
}
