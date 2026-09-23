import { lazy, Suspense, useEffect, useMemo, useState } from 'react';
import { ArrowLeft, ArrowRight, Bell, BookOpen, Check, ChevronLeft, ChevronRight, Compass, Play, Search, Sparkles, Star, Trophy, Volume2, VolumeX } from 'lucide-react';
import { Pip } from '../../components/Illustrations';
import type { Child, LevelDefinition, Progress } from '../../types';
import type { GameRepository } from '../../services/repository';
import { loadBank } from '../../game/golden-bell/bank';
import { GoldenBellStore } from '../../game/golden-bell/store';
import { questionNames, type Bank, type QuestionMeta } from '../../game/golden-bell/model';
import { bellSvg } from '../../game/golden-bell/art';
import { artUrl } from '../../game/artManifest';
import './golden-bell.css';
const GoldenBellGame = lazy(() => import('./GoldenBellGame'));
const difficultyNames = ['Những từ đầu tiên', 'Màu sắc quanh em', 'Tìm thêm gợi ý', 'Nghe và hiểu', 'Ghép những điều nhỏ', 'Làm theo hai bước', 'Câu chuyện của bạn', 'Thám tử tí hon', 'Trí nhớ siêu sao', 'Chuông Sao kỳ diệu'];
const colors = ['#8fbd9a','#91c8c9','#9dbadc','#b1a3d6','#dfa7b7','#dab68b','#99b99b','#94bfcf','#b6a5cc','#dbbf7e'];

export default function GoldenBellPage({ child, repository, levels, progress, muted, onMute, onBack }: { child: Child; repository: GameRepository; levels: LevelDefinition[]; progress: Progress[]; muted: boolean; onMute: () => void; onBack: () => void }) {
  const [store, setStore] = useState<GoldenBellStore>(), [bank, setBank] = useState<Bank>();
  const [playing, setPlaying] = useState(false), [busy, setBusy] = useState(false), [error, setError] = useState('');
  const [difficulty, setDifficulty] = useState(0), [query, setQuery] = useState(''), [page, setPage] = useState(0), [version, setVersion] = useState(0);
  const [sync, setSync] = useState('');
  const scope = `${repository.owner}:${child.id}`;
  useEffect(() => {
    let live = true;
    void loadBank().then(async b => { const s = new GoldenBellStore(scope, child.id, b, repository); await s.load(); if (!live) return; s.onChange = () => live && setVersion(n => n + 1); setBank(b); setStore(s); }).catch(e => live && setError(String(e)));
    return () => { live = false; };
  }, [scope, repository]);
  useEffect(() => {
    if (playing || !store || repository.local) return;
    const run = () => { setSync('Đang đồng bộ lượt đã hoàn thành…'); void store.sync().then(() => setSync('Lượt hoàn thành đã đồng bộ với tài khoản.')).catch(() => setSync('Lượt chơi đã lưu trên thiết bị; sẽ đồng bộ khi kết nối lại.')); };
    run(); window.addEventListener('online', run); return () => window.removeEventListener('online', run);
  }, [playing, store, repository]);
  const questions = useMemo(() => bank?.questions.filter(q => (!difficulty || q.difficulty === difficulty) && (!query || `${q.id} ${q.promptText} ${questionNames[q.questionType]}`.toLowerCase().includes(query.toLowerCase()))) ?? [], [bank, difficulty, query]);
  const completed = new Set(store?.history.completed ?? []);
  const pages = Math.max(1, Math.ceil(questions.length / 20));
  async function start(mode: 'adventure' | 'review' | 'practice', first?: QuestionMeta) {
    if (!store || busy) return; setBusy(true); setError('');
    try {
      const learned = levels.filter(l => progress.some(p => p.levelId === l.id));
      const maxWorld = Math.max(1, ...learned.map(l => Number(l.worldId.slice(1))));
      await store.start({ mode, maxDifficulty: first ? first.difficulty : mode === 'review' ? Math.min(10, maxWorld * 3 + 1) : 10,
        difficulty: first?.difficulty, firstId: first?.id,
        allowedVocabulary: mode === 'review' ? [...new Set(learned.flatMap(l => [...l.targetVocabulary, ...l.reviewVocabulary]))] : undefined });
      setPlaying(true);
    } catch (e) { setError(e instanceof Error ? e.message : 'Chưa chuẩn bị được lượt chơi. Hãy thử lại.'); }
    finally { setBusy(false); }
  }
  if (playing && store) return <Suspense fallback={<p className="loading-state">Đang mở cánh cửa Đảo Chuông…</p>}><GoldenBellGame key={store.session!.id} store={store} muted={muted} onMute={onMute} onBack={() => setPlaying(false)}/></Suspense>;
  return <main className="gb-lobby" style={{ '--gb-backdrop': `url("${artUrl('golden-bell-island-v1.png')}")` } as React.CSSProperties}>
    <div className="gb-lobby-bar"><button className="text-button" onClick={onBack}><ArrowLeft size={18}/> Bản đồ phiêu lưu</button><span><Bell size={17}/> ĐẢO CHUÔNG SAO</span><button className="gb-sound" onClick={onMute} aria-label={muted ? 'Bật âm thanh' : 'Tắt âm thanh'}>{muted ? <VolumeX/> : <Volume2/>}</button></div>
    <section className="gb-hero"><div className="gb-hero-copy"><span className="eyebrow"><Sparkles size={16}/> A THOUSAND LITTLE DISCOVERIES</span><h1>Đuổi hình bắt chữ.<br/><em>Đánh thức Chuông Sao!</em></h1><p>Nghe một gợi ý, tìm một bức hình, ghép một câu chuyện.<br/>1.000 câu hỏi đang đợi {child.nickname} khám phá.</p><div className="gb-start-actions"><button className="primary" disabled={!store || busy} onClick={() => void start('adventure')}><Play size={20} fill="currentColor"/>{busy ? 'Đang chuẩn bị 12 câu…' : 'Khám phá tự do'}</button>{store?.session && !store.session.bellRung && <button className="secondary" disabled={busy} onClick={() => setPlaying(true)}>Tiếp tục câu {store.session.index + 1} <ArrowRight size={18}/></button>}</div><div className="gb-hero-notes"><span><Star size={15}/> 12 câu mỗi lượt</span><span><Compass size={15}/> 10 mức khám phá</span><span><Check size={15}/> Sai vẫn được chơi tiếp</span></div></div><div className="gb-hero-art"><img src={`data:image/svg+xml;charset=utf-8,${encodeURIComponent(bellSvg)}`} alt="Chuông Sao bằng vàng trên khung gỗ"/><Pip friend="momo" className="gb-hero-momo"/><Pip friend="pip" className="gb-hero-pip"/><span className="gb-hero-spark">✦</span></div></section>
    {error && <p className="gb-notice" role="alert">{error}{!store && <button onClick={() => location.reload()}>Tải lại</button>}</p>}
    <section className="gb-stats" aria-label="Hành trình Chuông Sao"><div><BookOpen/><strong>{bank?.count ?? '…'}</strong><span>Câu hỏi sẵn sàng</span></div><div><Check/><strong>{completed.size}</strong><span>Câu trả lời đúng</span></div><div><Bell/><strong>{store?.history.tokens ?? 0}</strong><span>Bell Tokens</span></div><div><Trophy/><strong>{(store?.history.bestScore ?? 0).toLocaleString('vi-VN')}</strong><span>Kỷ lục điểm · {store?.history.sessions ?? 0} lượt</span></div></section>
    <p className="gb-rules">Mỗi câu có đồng hồ riêng. Đúng: 100 điểm + tối đa 50 điểm tốc độ. Chọn sai: giảm 25 điểm của câu; dùng gợi ý: giảm 20 điểm. Hết giờ: 0 điểm và chuyển câu tiếp theo.</p>
    <section className="gb-review"><Pip friend="lulu"/><div><h2>Mỗi lần chơi, nhớ thêm một chút</h2><p>Câu hỏi tăng dần độ khó. Những từ bé hay nhầm sẽ trở lại trong các lượt sau.</p><small>{repository.local ? 'Tiến trình được giữ riêng cho bé trên thiết bị này.' : sync}</small></div><button className="secondary" disabled={!store || busy || progress.length === 0} onClick={() => void start('review')}>Ôn từ đã học <ArrowRight size={18}/></button></section>
    <section className="gb-library"><div className="gb-library-heading"><div><span className="eyebrow">THE QUESTION GARDEN</span><h2>Khu vườn 1.000 câu hỏi</h2><p>Chọn một câu bất kỳ để bắt đầu lượt luyện cùng mức khó.</p></div><label className="gb-search"><Search size={18}/><input value={query} onChange={e => { setQuery(e.target.value); setPage(0); }} placeholder="Tìm câu hỏi hoặc mã GB-0001" aria-label="Tìm câu hỏi"/></label></div><div className="gb-difficulties"><button className={!difficulty ? 'selected' : ''} onClick={() => { setDifficulty(0); setPage(0); }}>Tất cả <small>1.000 câu</small></button>{difficultyNames.map((name, i) => <button key={name} style={{ '--chapter-color': colors[i] } as React.CSSProperties} title={name} className={difficulty === i + 1 ? 'selected' : ''} onClick={() => { setDifficulty(i + 1); setPage(0); }}><span>{i + 1}</span><strong>{name}</strong><small>100 câu · {bank?.questions.filter(q => q.difficulty === i + 1 && completed.has(q.id)).length ?? 0} đã khám phá</small></button>)}</div><div className="gb-question-grid">{questions.slice(page * 20, page * 20 + 20).map(q => <button key={q.id} disabled={busy} className={`gb-question-card ${completed.has(q.id) ? 'completed' : ''}`} onClick={() => void start('practice', q)}><span className="gb-question-code">{q.id}<span>{completed.has(q.id) ? <Check size={17}/> : `Mức ${q.difficulty}`}</span></span><strong>{questionNames[q.questionType]}</strong><p>{q.promptText}</p><span className="gb-card-action">Khám phá câu này <ArrowRight size={16}/></span></button>)}</div>{bank && !questions.length && <p className="gb-empty">Chưa tìm thấy câu phù hợp. Thử một từ khác nhé.</p>}<div className="gb-pagination"><span>{questions.length.toLocaleString('vi-VN')} câu · Trang {page + 1}/{pages}</span><button disabled={page === 0} onClick={() => setPage(n => n - 1)} aria-label="Trang trước"><ChevronLeft/></button><button disabled={page + 1 >= pages} onClick={() => setPage(n => n + 1)} aria-label="Trang sau"><ChevronRight/></button></div></section>
  </main>;
}
