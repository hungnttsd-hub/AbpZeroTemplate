import { z } from 'zod';
import profiles from './worlds.json';
import meanings from './meanings.json';
export const builderModes = ['collect_letters', 'order_letters', 'missing_letter', 'build_to_unlock'] as const;
export const actionTypes = ['open_door', 'build_bridge', 'release_animal', 'start_vehicle', 'grow_plant', 'cook_food', 'power_machine', 'reveal_treasure'] as const;
export const spawnTypes = ['static', 'falling', 'balloon', 'crate', 'platform', 'hanging', 'moving_target', 'creature', 'cloud', 'obstacle', 'bush', 'pulley', 'spring'] as const;
const base = z.object({
  mode: z.enum(builderModes), targetWord: z.string().regex(/^[A-Z]+(?: [A-Z]+)*$/),
  unit: z.enum(['letter', 'word']).default('letter'), difficulty: z.number().int().min(1).max(6).default(1),
  instruction: z.object({ text: z.string().min(1), audio: z.string().default('') }),
  meaningAsset: z.object({ type: z.literal('image'), src: z.string() }).optional(),
  meaningWord: z.string().optional(),
  letters: z.array(z.object({ id: z.string().min(1), char: z.string().regex(/^[A-Z]+$/), spawn: z.enum(spawnTypes) })).min(1).max(10),
  slots: z.number().int().min(1).max(8), distractors: z.array(z.string().regex(/^[A-Z]+$/)).max(2).default([]),
  missingIndices: z.array(z.number().int().min(0)).default([]),
  completionAction: z.object({ type: z.enum(actionTypes), target: z.string().min(1) }),
  example: z.string().min(1), mission: z.string().min(1), theme: z.string().default('adventure'),
  ghostLetters: z.boolean().default(false), hideMeaningAfterMs: z.number().min(500).max(10000).optional()
});
export const wordBuilderSchema = base.superRefine((c, ctx) => {
  const units = c.unit === 'word' ? c.targetWord.split(' ') : [...c.targetWord];
  const fail = (message: string) => ctx.addIssue({ code: 'custom', message });
  if (c.unit === 'letter' && c.targetWord.includes(' ')) fail('Sentence content needs unit=word.');
  if (units.length !== c.slots) fail('slots must match target units.');
  if (new Set(c.letters.map(t => t.id)).size !== c.letters.length) fail('Letter IDs must be unique.');
  if (c.letters.some(t => /^distractor_\d+$/.test(t.id))) fail('distractor_N IDs are reserved for distractors.');
  if (c.missingIndices.some(i => i >= units.length) || new Set(c.missingIndices).size !== c.missingIndices.length) fail('Invalid missing positions.');
  if (c.mode === 'missing_letter' && !c.missingIndices.length) fail('Missing-letter mode needs missingIndices.');
  const needed = units.filter((_, i) => c.mode !== 'missing_letter' || c.missingIndices.includes(i));
  const available = c.letters.map(t => t.char);
  for (const char of needed) { const i = available.indexOf(char); if (i < 0) fail(`Missing a distinct ${char} token.`); else available.splice(i, 1); }
});
export type WordBuilderConfig = z.infer<typeof wordBuilderSchema>;
export type LetterDefinition = WordBuilderConfig['letters'][number];
export type CompletionActionType = typeof actionTypes[number];
export interface BuilderLevelSource { id: string; worldId: string; targetVocabulary: string[]; difficulty: number; wordBuilder?: WordBuilderConfig }

export function resolveWordBuilder(level: BuilderLevelSource): WordBuilderConfig {
  if (level.wordBuilder) return wordBuilderSchema.parse(level.wordBuilder);
  const all = profiles as Record<string, typeof profiles.W01>;
  const profile = all[level.worldId] ?? profiles.W01;
  const word = level.targetVocabulary[0].toUpperCase();
  const spawns = profile.spawns as Array<LetterDefinition['spawn']>;
  return wordBuilderSchema.parse({ mode: profile.mode, targetWord: word, difficulty: level.difficulty,
    instruction: { text: `Build the word ${word}.` }, letters: [...word].map((char, index) => ({ id: `letter_${index}`, char, spawn: spawns[index % spawns.length] })),
    slots: word.length, missingIndices: profile.mode === 'missing_letter' ? [Math.floor(word.length / 2)] : [], completionAction: { type: profile.action, target: profile.target }, mission: profile.mission,
    example: `Look! ${word.toLowerCase()}!`, meaningWord: (meanings as Record<string, string>)[word], theme: profile.theme, ghostLetters: false });
}
export function builderSize(config: WordBuilderConfig, parentWidth: number) {
  const compact = parentWidth < 600; const width = compact ? 480 : 960;
  if (!compact) return { width, height: Math.max(850, 230 + Math.ceil((config.letters.length + config.distractors.length) / 3) * 180 + Math.ceil((config.letters.length + config.distractors.length) / 8) * 125 + 190) };
  const rows = Math.ceil((config.letters.length + config.distractors.length) / (compact ? 3 : 6));
  const slotRows = Math.ceil(config.slots / (compact ? 3 : 8));
  return { width, height: 480 + rows * 180 + rows * 125 + slotRows * 120 + 210 };
}
