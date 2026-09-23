import { z } from 'zod';

export const questionTypes = ['action_recognition','can_cannot','color_object','color_recognition','compare_quantity','count_objects','final_reasoning','function_question','listen_find_picture','memory_scene','missing_letter','multi_clue_scene','odd_one_out','picture_to_word','preposition_scene','select_pair','sentence_completion','sentence_order','shape_recognition','short_story','simple_inference','two_attribute_object','two_step_instruction','weather_choice','word_picture_mismatch'] as const;
export const mechanicTypes = ['answer_zone','raise_board','listen_run','quick_match','bell_choice'] as const;
const sceneSchema = z.object({ animal: z.string(), object: z.string(), relation: z.string(), color: z.string().optional(), size: z.string().optional() });
export const optionSchema = z.object({ id: z.string(), label: z.string(), assetKey: z.string().optional(), imageAssetKey: z.string().optional(), word: z.string().optional(), emoji: z.string().optional(), displayColor: z.string().optional(), size: z.string().optional(), scene: sceneSchema.optional(), sequence: z.array(z.string()).optional(), assets: z.array(z.string()).optional(), count: z.number().optional() });
export const stimulusSchema = z.object({ type: z.string(), assetKey: z.string().optional(), meaningAssetKey: z.string().optional(), maskedWord: z.string().optional(), text: z.string().optional(), character: z.string().optional(), weather: z.string().optional(), item: z.string().optional(), object: z.string().optional(), a: z.number().optional(), b: z.number().optional(), tiles: z.array(z.string()).optional(), showMs: z.number().optional(), objects: z.array(z.union([z.string(), z.object({ assetKey: z.string().optional(), count: z.number().optional(), color: z.string().nullable().optional(), character: z.string().optional(), item: z.string().optional() })])).optional() });
export const questionSchema = z.object({ id: z.string().regex(/^GB-\d{4}$/), difficulty: z.number().int().min(1).max(10), questionType: z.enum(questionTypes), mechanic: z.enum(mechanicTypes), skill: z.string(), promptText: z.string(), audioText: z.string(), stimulus: stimulusSchema, options: z.array(optionSchema), answer: z.discriminatedUnion('type', [z.object({ type: z.literal('option'), value: z.string(), sequence: z.array(z.string()).optional(), missingPositions: z.array(z.number()).optional() }), z.object({ type: z.literal('sequence'), value: z.array(z.string()) })]), hint: z.object({ afterWrong: z.number(), text: z.string(), strategy: z.string() }), explanation: z.string(), targetVocabulary: z.array(z.string()), worldTags: z.array(z.string()), estimatedSeconds: z.number(), requiresMemoryPhase: z.boolean(), notes: z.string() });
export type Question = z.infer<typeof questionSchema>;
export type Option = z.infer<typeof optionSchema>;
export type SceneDescription = z.infer<typeof sceneSchema>;
export type QuestionMeta = Pick<Question, 'id' | 'difficulty' | 'questionType' | 'skill' | 'promptText' | 'targetVocabulary' | 'worldTags'>;
export interface Bank { version: string; count: number; distribution: number[]; questions: QuestionMeta[] }
export type AnswerInput = { type: 'option'; value: string } | { type: 'sequence'; value: string[] } | { type: 'timeout'; value: null };
export type Phase = 'intro' | 'question' | 'checkpoint' | 'bell' | 'summary';
export interface QuestionRecord { questionId: string; attempts: number; wrong: number; hintUsed: boolean; durationMs: number; completed: boolean; memoryReplays: number; memorySeen?: boolean; draft?: string[]; answerMs?: number; timedOut?: boolean; score?: number }
export interface AnswerLog { id: string; questionId: string; questionIndex: number; input: AnswerInput; correct: boolean; hintUsed: boolean; durationMs: number; createdAt: string; answerMs?: number }
export interface BellSession { id: string; childId: string; bankVersion: string; seed: number; mode: 'adventure' | 'review' | 'practice'; maxDifficulty: number; allowedVocabulary?: string[]; startedAt: string; completedAt?: string; questions: Question[]; index: number; phase: Phase; records: QuestionRecord[]; answers: AnswerLog[]; bellRung: boolean; checkpointSeen: number; syncedAnswers: number; serverStarted?: boolean; serverCompleted?: boolean; scoringVersion?: number }
export interface BellHistory { seen: Record<string, number>; completed: string[]; review: Record<string, number>; tokens: number; sessions: number; minutes: number; bestScore?: number }
export const resolved = (record: QuestionRecord) => record.completed || !!record.timedOut;
export const sessionScore = (session: BellSession) => session.records.reduce((sum, r) => sum + (r.score ?? 0), 0);
/** Version 1 rules are mirrored on the server; old saved rounds retain their original rules. */
export const timeLimitMs = (q: Pick<Question, 'estimatedSeconds' | 'difficulty'>) => Math.min(60, Math.max(15, Math.ceil((q.estimatedSeconds + q.difficulty) / 5) * 5)) * 1000;
export function pointsFor(q: Question, answerMs: number, wrong: number, hintUsed: boolean) {
  const limit = timeLimitMs(q);
  if (answerMs >= limit) return 0;
  return Math.max(20, 100 + Math.floor(50 * Math.max(0, limit - answerMs) / limit) - wrong * 25 - (hintUsed ? 20 : 0));
}
export const emptyHistory = (): BellHistory => ({ seen: {}, completed: [], review: {}, tokens: 0, sessions: 0, minutes: 0 });
export const profile = [1, 2, 2, 3, 4, 5, 6, 7, 8, 8, 9, 10];
export const questionNames: Record<Question['questionType'], string> = {
  action_recognition: 'Đoán hành động', can_cannot: 'Có thể hay không?', color_object: 'Tìm đúng màu', color_recognition: 'Sắc màu', compare_quantity: 'Nhiều hơn, ít hơn', count_objects: 'Cùng đếm nào', final_reasoning: 'Thử thách Chuông Sao', function_question: 'Đồ vật dùng làm gì?', listen_find_picture: 'Nghe và tìm hình', memory_scene: 'Trí nhớ siêu sao', missing_letter: 'Chữ còn thiếu', multi_clue_scene: 'Thám tử hình ảnh', odd_one_out: 'Tìm hình khác nhóm', picture_to_word: 'Nhìn hình đoán chữ', preposition_scene: 'Bạn ấy ở đâu?', select_pair: 'Tìm cặp hình', sentence_completion: 'Hoàn thành câu', sentence_order: 'Xếp câu kỳ diệu', shape_recognition: 'Hình khối', short_story: 'Nghe chuyện cùng bạn', simple_inference: 'Đoán từ gợi ý', two_attribute_object: 'Hai điều cần tìm', two_step_instruction: 'Làm theo hai bước', weather_choice: 'Thời tiết hôm nay', word_picture_mismatch: 'Tìm cặp chưa khớp',
};

export function evaluate(question: Question, input: AnswerInput) {
  if (question.questionType === 'two_step_instruction') return input.type === 'sequence' && question.answer.type === 'option' && JSON.stringify(input.value) === JSON.stringify(question.answer.sequence);
  return question.answer.type === input.type && JSON.stringify(question.answer.value) === JSON.stringify(input.value);
}
/** Data may encode blanks right-to-left. Show every candidate left-to-right, consistently. */
export function missingOption(question: Question, option: Option) {
  if (question.answer.type !== 'option' || !question.answer.missingPositions) return option.label;
  return question.answer.missingPositions.map((position, i) => ({ position, letter: option.label[i] ?? '' })).sort((a, b) => a.position - b.position).map(p => p.letter).join('');
}
