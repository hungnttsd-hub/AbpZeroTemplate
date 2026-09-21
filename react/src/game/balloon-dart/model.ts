import { z } from 'zod';

const semanticSchema = z.object({ id: z.string().min(1), word: z.string().optional(), imageKey: z.string().optional(), audioKey: z.string().optional(), shape: z.string().optional(), color: z.string().optional(), size: z.enum(['small', 'medium', 'big']).optional() });
const ruleSchema = z.object({ semanticId: z.string().optional(), requiredWord: z.string().optional(), requiredShape: z.string().optional(), requiredColor: z.string().optional(), requiredSize: z.enum(['small', 'medium', 'big']).optional() }).refine(r => Object.values(r).some(Boolean), 'Target needs at least one attribute.');
export type Semantic = z.infer<typeof semanticSchema>;
export type TargetRule = z.infer<typeof ruleSchema>;
export function matches(s: Semantic, r: TargetRule): boolean {
  return (!r.semanticId || s.id === r.semanticId) && (!r.requiredWord || s.word === r.requiredWord) && (!r.requiredShape || s.shape === r.requiredShape) && (!r.requiredColor || s.color === r.requiredColor) && (!r.requiredSize || s.size === r.requiredSize);
}
export const movementSchema = z.object({ pattern: z.enum(['gentleBob', 'driftHorizontal', 'driftVertical', 'figureEight', 'crossLane']), amplitudeX: z.number().min(0).max(400).default(80), amplitudeY: z.number().min(0).max(180).default(18), periodMs: z.number().min(1000).default(4200), phase: z.number().default(0), speed: z.number().min(0).max(300).optional(), lane: z.number().int().min(0).max(2).optional() });
const optionSchema = z.object({ instanceId: z.string().min(1), semantic: semanticSchema, movement: movementSchema, initialPosition: z.object({ x: z.number().min(0).max(1), y: z.number().min(0).max(1) }).optional(), balloonSkin: z.string().default('blue'), labelMode: z.enum(['visible', 'fade', 'hidden']).default('visible') });
const roundSchema = z.object({ id: z.string().min(1), instructionText: z.string().min(1), instructionAudioKey: z.string().default(''), target: ruleSchema, balloons: z.array(optionSchema).min(3).max(5), aimAssist: z.enum(['long', 'medium', 'short']).default('long'), seed: z.number().int().default(1) }).superRefine((r, ctx) => {
  if (new Set(r.balloons.map(b => b.instanceId)).size !== r.balloons.length) ctx.addIssue({ code: 'custom', message: 'Balloon instance IDs must be unique.' });
  if (r.balloons.filter(b => matches(b.semantic, r.target)).length !== 1) ctx.addIssue({ code: 'custom', message: 'Each round needs exactly one matching balloon.' });
  if (r.balloons.filter(b => b.movement.pattern === 'crossLane').length > 2) ctx.addIssue({ code: 'custom', message: 'At most two cross-lane balloons.' });
});
export const balloonDartSchema = z.object({ id: z.string(), mechanic: z.literal('balloon_dart'), title: z.string().optional(), difficulty: z.number().int().min(1).max(5), learningMode: z.enum(['pictureListening', 'wordRecognition', 'colorRecognition', 'compoundListening', 'sizeAndShape']), inputMode: z.enum(['drag', 'tap', 'both']), rounds: z.array(roundSchema).length(3), reward: z.object({ stars: z.literal(3) }) }).superRefine((c, ctx) => {
  if (new Set(c.rounds.map(r => r.id)).size !== 3) ctx.addIssue({ code: 'custom', message: 'Round IDs must be unique.' });
  if (c.difficulty < 4 && c.rounds.some(r => r.balloons.some(b => b.movement.pattern === 'figureEight'))) ctx.addIssue({ code: 'custom', message: 'Figure-eight needs difficulty 4+.' });
});
export type BalloonDartConfig = z.infer<typeof balloonDartSchema>;
export type BalloonOption = z.infer<typeof optionSchema>;
export type BalloonRound = z.infer<typeof roundSchema>;
export interface BalloonSummary { mechanic: 'balloon_dart'; roundsCompleted: number; shots: number; wrongHits: number; misses: number; hintCount: number; durationSeconds: number; stars: 3 }
export class RoundProgress {
  round = 0; shots = 0; wrongHits = 0; misses = 0; errors = 0; locked = false;
  fire() { if (this.locked || this.round >= 3) return false; this.shots++; return true; }
  hit(correct: boolean) { if (this.locked || this.round >= 3) return false; if (!correct) { this.wrongHits++; this.errors++; return false; } this.locked = true; return true; }
  miss() { if (!this.locked) { this.misses++; this.errors++; } }
  advance() { if (!this.locked || this.round >= 3) return false; this.round++; this.errors = 0; this.locked = this.round === 3; return true; }
}

/** Old campaign JSON is adapted without changing IDs, unlocking or backend content. */
export function resolveBalloonDart(level: { id: string; title: string; difficulty: number; instruction: string; targets: { value: string; correct: boolean }[]; targetVocabulary: string[]; balloonDart?: BalloonDartConfig }): BalloonDartConfig {
  if (level.balloonDart) return balloonDartSchema.parse(level.balloonDart);
  const terms = [...new Set([...level.targets.map(t => t.value), ...level.targetVocabulary])].slice(0, 5);
  const target = level.targetVocabulary[0];
  const words = [target, terms.find(t => t !== target) ?? target, target];
  return balloonDartSchema.parse({ id: level.id, mechanic: 'balloon_dart', title: level.title, difficulty: level.difficulty, learningMode: 'pictureListening', inputMode: 'both', reward: { stars: 3 }, rounds: words.map((word, round) => ({
    id: `${level.id}-R${round + 1}`, instructionText: round === 0 ? level.instruction : `Pop the ${word}.`, target: { semanticId: word }, aimAssist: level.difficulty <= 2 ? 'long' : 'medium', seed: round + [...level.id].reduce((n, c) => n + c.charCodeAt(0), 0),
    balloons: (terms.length < 3 ? [...terms, ...Array.from({ length: 3 - terms.length }, () => terms.find(t => t !== word) ?? terms[0])] : terms).map((value, i, all) => ({ instanceId: `option_${i}`, semantic: { id: value, word: value }, balloonSkin: ['coral', 'blue', 'yellow', 'violet', 'green'][(i + round) % 5], labelMode: level.difficulty < 3 ? 'visible' : level.difficulty === 3 ? 'fade' : 'hidden', initialPosition: { x: (i + 1) / (all.length + 1), y: .39 + (i % 2) * .08 }, movement: { pattern: level.difficulty <= 2 ? 'gentleBob' : i % 2 ? 'driftVertical' : 'driftHorizontal', amplitudeX: level.difficulty <= 2 ? 12 : 80, amplitudeY: level.difficulty <= 2 ? 18 : 60, phase: i * .8, periodMs: 4300 + i * 400 } }))
  })) });
}
