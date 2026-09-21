import { lazy, Suspense, useEffect, useState } from 'react';
import { ArrowLeft, Play, Star } from 'lucide-react';
import demos from '../../content/word-builder-demos.json';
import { wordBuilderSchema } from '../../game/word-builder/config';
import { levelSchema, starsFor, type Attempt, type LevelDefinition } from '../../types';
import { storage } from '../../services/storage';
import { wordImage } from '../../game/art';
const GameContainer = lazy(() => import('../../game/GameContainer'));
const levels = demos.map((d, index) => levelSchema.parse({
  id: `W90-L${String(index + 1).padStart(2, '0')}`, worldId: d.worldId, order: index + 1, title: d.title,
  mechanic: 'word_builder', difficulty: Math.min(d.difficulty, 4), instruction: d.instruction.text,
  targetVocabulary: [d.targetWord.toLowerCase()], reviewVocabulary: [], targets: [{ value: d.targetWord.toLowerCase(), correct: true }, { value: 'word star', correct: false }],
  hintPolicy: { afterWrongAttempts: 2, audioReplay: true, visualPulse: true }, estimatedSeconds: 90,
  learningObjective: 'Build words to help the world.', isBoss: false, wordBuilder: wordBuilderSchema.parse(d)
}));
const names = { collect_letters: 'Tìm chữ', order_letters: 'Xếp mật mã', missing_letter: 'Chữ thất lạc', build_to_unlock: 'Kích hoạt thế giới' };
export default function WordBuilderDemoGallery({ childId, scope, muted, onMute, onBack }: { childId: string; scope: string; muted: boolean; onMute: () => void; onBack: () => void }) {
  const [saving, setSaving] = useState(false);
  const [current, setCurrent] = useState<LevelDefinition>(); const [result, setResult] = useState<Attempt>();
  const [scores, setScores] = useState<Record<string, number>>({}); const [error, setError] = useState('');
  const prefix = `word-builder-demo:${scope}:`;
  const refresh = async () => {
    const rows = await storage.entries<Attempt>(prefix); const scores: Record<string, number> = {};
    rows.forEach(({ value }) => { scores[value.levelId] = Math.max(scores[value.levelId] ?? 0, starsFor(value)); }); setScores(scores);
  };
  useEffect(() => { void refresh().catch(e => setError(String(e))); }, [prefix]);
  async function save(attempt: Attempt) {
    setResult(attempt); setSaving(true);
    try { await storage.set(`${prefix}${attempt.attemptId}`, attempt); await refresh(); setError(''); }
    catch { setError('Chưa lưu được ngôi sao. Giữ trang này mở và thử lại.'); }
    finally { setSaving(false); }
  }
  if (current) return <><Suspense fallback={<p className="loading-state">Đang chuẩn bị chuyến phiêu lưu…</p>}><GameContainer level={current} childId={childId} muted={muted} onMute={onMute} onComplete={a => void save(a)} onExit={() => { setCurrent(undefined); setResult(undefined); }}/></Suspense>{result && <div className="overlay"><section className="result-card"><span className="eyebrow">WORD BUILDER ADVENTURE</span><div className="result-stars">{'★'.repeat(starsFor(result))}</div><h1>Thế giới đã thức dậy!</h1><p>{current.title}</p><p className="subtle">{error || 'Ngôi sao khu thử được lưu riêng trên thiết bị.'}</p>{saving ? <p role="status">Đang lưu ngôi sao…</p> : error ? <button className="primary" onClick={() => void save(result)}>Thử lưu lại</button> : <button className="primary" onClick={() => { setCurrent(undefined); setResult(undefined); }}>Khám phá nhiệm vụ khác</button>}</section></div>}</>;
  return <main className="dashboard-page"><button className="text-button" onClick={onBack}><ArrowLeft size={18}/> Về bản đồ</button><span className="eyebrow">WORD BUILDER ADVENTURE</span><h1>Mỗi từ mở một điều kỳ diệu</h1><p className="subtle">8 chuyến phiêu lưu tự chọn · Ngôi sao lưu riêng trên thiết bị · Không cần mở khóa</p>{error && <p role="alert">{error}</p>}<div className="builder-demo-grid">{levels.map((level, i) => <button className="builder-demo-card" key={level.id} onClick={() => setCurrent(level)}><span className="eyebrow">{demos[i].id} · {names[level.wordBuilder!.mode]}</span><img src={wordImage(level.targetVocabulary[0])} alt=""/><h2>{level.title}</h2><p>{level.wordBuilder!.mission}</p><span className="demo-play"><Play size={18}/> Khám phá <span><Star size={15}/> {scores[level.id] ?? 0}/3</span></span></button>)}</div></main>;
}

