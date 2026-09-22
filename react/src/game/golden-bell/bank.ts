import { questionSchema, questionTypes, profile, type Bank, type BellHistory, type Question, type QuestionMeta } from './model';

const root = `${import.meta.env.BASE_URL}content/golden-bell/v1/`;
let manifest: Promise<Bank> | undefined;
export function loadBank(): Promise<Bank> {
  return manifest ??= fetch(`${root}manifest.json`, { signal: AbortSignal.timeout(15000) }).then(async r => {
    if (!r.ok) throw new Error('Chưa tải được ngân hàng Chuông Sao. Hãy tải lại trang.');
    const bank = await r.json() as Bank;
    if (bank.count !== 1000 || bank.questions.length !== 1000 || new Set(bank.questions.map(q => q.id)).size !== 1000 || bank.questions.some(q => !questionTypes.includes(q.questionType))) throw new Error('Ngân hàng câu hỏi chưa đủ hoặc sai phiên bản. Hãy chạy lại import nội dung.');
    return bank;
  }).catch(error => { manifest = undefined; throw error; });
}
export async function loadQuestion(id: string): Promise<Question> {
  if (!/^GB-\d{4}$/.test(id)) throw new Error('Mã câu hỏi không hợp lệ.');
  const r = await fetch(`${root}${id}.json`, { signal: AbortSignal.timeout(15000) });
  if (!r.ok) throw new Error(`Chưa tải được câu ${id}. Hãy thử lại.`);
  return questionSchema.parse(await r.json());
}
export const commonWords = new Set('a an the is are am has have it i you we he she they can cannot not do does to of and or on in under behind between next to in front of small big one two three four five six seven eight nine ten first then pip poki lulu momo foxy 1 2 3 4 5 6 7 8 9 10'.split(' '));
export const wordKey = (word: string) => word.toLowerCase().replace(/s$/, '');
function contentWords(q: QuestionMeta) { return q.targetVocabulary.map(wordKey).filter(w => !commonWords.has(w)); }
function hash(seed: number, text: string) { let h = seed >>> 0; for (const c of text) h = Math.imul(h ^ c.charCodeAt(0), 16777619) >>> 0; return h / 4294967296; }
export interface Selection { seed: number; maxDifficulty: number; allowedVocabulary?: string[]; difficulty?: number; firstId?: string }
export function chooseQuestion(bank: Bank, history: BellHistory, config: Selection, prefix: QuestionMeta[], excluded: string[], desired: number, preferredSkill?: string) {
  const allowed = config.allowedVocabulary ? new Set(config.allowedVocabulary.map(wordKey)) : undefined;
  const recentWords = new Set(prefix.slice(-2).flatMap(contentWords));
  const last = prefix.at(-1), before = prefix.at(-2);
  const candidates = bank.questions.filter(q => q.difficulty <= config.maxDifficulty && !excluded.includes(q.id)
    && (!allowed || contentWords(q).every(w => allowed.has(w)))
    && !(last?.questionType === q.questionType && before?.questionType === q.questionType)
    && !(last?.questionType === 'memory_scene' && q.questionType === 'memory_scene')
    && !contentWords(q).some(w => recentWords.has(w)));
  if (!candidates.length) throw new Error('Chưa đủ từ đã học cho một lượt 12 câu khác nhau. Bé hãy chọn Khám phá tự do hoặc học thêm một màn nhé.');
  const finalTypes = ['final_reasoning', 'multi_clue_scene', 'memory_scene', 'short_story'];
  const final = prefix.length === 11 ? candidates.filter(q => finalTypes.includes(q.questionType) && q.difficulty === Math.min(10, config.maxDifficulty)) : [];
  const pool = final.length ? final : candidates;
  // Every fourth position is reserved for review when matching questions are available.
  const wantReview = (prefix.length + config.seed % 4) % 4 === 0;
  return pool.map(q => ({ q, weight: Math.abs(q.difficulty - desired) * 1000
    + (preferredSkill && q.skill !== preferredSkill ? 30 : 0)
    + (wantReview && !contentWords(q).some(w => (history.review[w] ?? 0) > 0) ? 40 : 0)
    + Math.min(history.seen[q.id] ?? 0, 50) * 3 + hash(config.seed + prefix.length, q.id) }))
    .sort((a, b) => a.weight - b.weight)[0].q;
}
export function selectSession(bank: Bank, history: BellHistory, config: Selection) {
  const selected: QuestionMeta[] = [];
  for (let i = 0; i < 12; i++) {
    const first = i === 0 && config.firstId ? bank.questions.find(q => q.id === config.firstId && q.difficulty <= config.maxDifficulty) : undefined;
    selected.push(first ?? chooseQuestion(bank, history, config, selected, selected.map(q => q.id), Math.min(config.difficulty ?? profile[i], config.maxDifficulty)));
  }
  return selected.map(q => q.id);
}
