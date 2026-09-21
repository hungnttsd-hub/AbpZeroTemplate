import { lazy, Suspense, useEffect, useState } from 'react';
import { ArrowLeft, Play, Star } from 'lucide-react';
import demos from '../../content/balloon-dart-demos.json';
import { balloonDartSchema, matches } from '../../game/balloon-dart/model';
import { levelSchema, starsFor, type Attempt, type LevelDefinition } from '../../types';
import { storage } from '../../services/storage';
import { wordImage } from '../../game/art';
const GameContainer = lazy(() => import('../../game/GameContainer'));
const levels = demos.map((raw, index) => {
  const config = balloonDartSchema.parse(raw);
  const vocabulary = [...new Set(config.rounds.map(r => { const s = r.balloons.find(b => matches(b.semantic, r.target))!.semantic; return s.word ?? s.id; }))];
  const words = [...new Set(config.rounds.flatMap(r => r.balloons.map(b => b.semantic.word ?? b.semantic.id)))];
  return levelSchema.parse({ id: `W91-L${String(index + 1).padStart(2, '0')}`, worldId: 'W91', order: index + 1, title: config.title,
    mechanic: 'balloon_dart', difficulty: Math.min(4, config.difficulty), instruction: config.rounds[0].instructionText, targetVocabulary: vocabulary,
    reviewVocabulary: words.filter(w => !vocabulary.includes(w)), targets: [{ value: vocabulary[0], correct: true }, { value: words.find(w => w !== vocabulary[0]) ?? 'balloon', correct: false }],
    hintPolicy: { afterWrongAttempts: 3, audioReplay: true, visualPulse: true }, estimatedSeconds: 90, learningObjective: 'Listen, aim and pop.', isBoss: false, balloonDart: config });
});
const modeNames: Record<string, string> = { pictureListening: 'Nghe và tìm hình', wordRecognition: 'Nhận diện từ', colorRecognition: 'Sắc màu', compoundListening: 'Màu và hình', sizeAndShape: 'Lớn và nhỏ' };
export default function BalloonDartGallery({ childId, scope, muted, onMute, onBack }: { childId: string; scope: string; muted: boolean; onMute: () => void; onBack: () => void }) {
  const [current, setCurrent] = useState<LevelDefinition>(); const [result, setResult] = useState<Attempt>();
  const [scores, setScores] = useState<Record<string, number>>({}); const [saving, setSaving] = useState(false); const [error, setError] = useState('');
  const prefix = `balloon-dart-demo:${scope}:`;
  async function refresh() { const rows = await storage.entries<Attempt>(prefix); const scores: Record<string, number> = {}; rows.forEach(({ value }) => { scores[value.levelId] = Math.max(scores[value.levelId] ?? 0, starsFor(value)); }); setScores(scores); }
  useEffect(() => { void refresh().catch(() => setError('Chưa đọc được sao đã lưu.')); }, [prefix]);
  async function save(attempt: Attempt) {
    setSaving(true); setResult(attempt); setError('');
    try { await storage.set(`${prefix}${attempt.attemptId}`, attempt); await refresh(); } catch { setError('Chưa lưu được ngôi sao. Bé thử lưu lại nhé.'); } finally { setSaving(false); }
  }
  const leave = () => { setCurrent(undefined); setResult(undefined); };
  if (current) return <><Suspense fallback={<p className="loading-state">Đang mở vườn bóng…</p>}><GameContainer level={current} childId={childId} muted={muted} onMute={onMute} onComplete={a => void save(a)} onExit={leave}/></Suspense>{result && <div className="overlay"><section className="result-card"><span className="eyebrow">BALLOON GARDEN</span><div className="result-stars">★★★</div><h1>Ba lượt, ba ngôi sao!</h1><p>{current.title}</p><p className="subtle">Sao khu thử được lưu riêng trên thiết bị.</p>{saving ? <p role="status">Đang lưu ngôi sao…</p> : error ? <><p role="alert">{error}</p><button className="primary" onClick={() => void save(result)}>Lưu lại</button></> : <button className="primary" onClick={leave}>Khám phá màn khác</button>}</section></div>}</>;
  return <main className="dashboard-page"><button className="text-button" onClick={onBack}><ArrowLeft size={18}/> Về bản đồ</button><span className="eyebrow">BALLOON GARDEN</span><h1>Nghe một từ, thả một niềm vui</h1><p className="subtle">8 màn tự chọn · Mỗi màn 3 lượt · Chơi trên điện thoại nằm ngang</p>{error && <p role="alert">{error}</p>}<div className="builder-demo-grid">{levels.map(level => <button className="builder-demo-card" key={level.id} onClick={() => { setError(''); setCurrent(level); }}><span className="eyebrow">{level.balloonDart!.id} · {modeNames[level.balloonDart!.learningMode]}</span><img src={wordImage(level.targetVocabulary[0])} alt=""/><h2>{level.title}</h2><p>{level.instruction}</p><span className="demo-play"><Play size={18}/> Vào vườn bóng <span><Star size={15}/> {scores[level.id] ?? 0}/3</span></span></button>)}</div></main>;
}
