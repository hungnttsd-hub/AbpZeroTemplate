import { useEffect, useState } from 'react';
import { ArrowLeft, ArrowRight, Leaf, Play, RotateCcw, Volume2, VolumeX } from 'lucide-react';
import { Pip } from '../../components/Illustrations';
import { hideSeekBank } from '../../game/hide-seek/bank';
import { HideSeekController, newSession } from '../../game/hide-seek/controller';
import { HideSeekStore } from '../../game/hide-seek/store';
import type { GameRepository } from '../../services/repository';
import type { Child } from '../../types';
import HideSeekGame, { HideSeekStars } from './HideSeekGame';
import './hide-seek.css';

export default function HideSeekPage({ child, repository, muted, onMute, onBack }: { child: Child; repository: GameRepository; muted: boolean; onMute: () => void; onBack: () => void }) {
  const [store] = useState(() => new HideSeekStore(repository, child.id, hideSeekBank));
  const [controller, setController] = useState<HideSeekController>(), [ready, setReady] = useState(false);
  const [error, setError] = useState(''), [busy, setBusy] = useState(false), [, refresh] = useState(0);
  useEffect(() => { let live = true;
    const sync = () => { void store.sync().then(() => { if (live) refresh(n => n + 1); }).catch(() => {}); };
    window.addEventListener('online', sync); const timer = setInterval(sync, 30000);
    void store.load().then(() => { if (live) { setReady(true); sync(); } }).catch(e => { if (live) setError(e.message); });
    return () => { live = false; window.removeEventListener('online', sync); clearInterval(timer); };
  }, [store]);
  useEffect(() => () => controller?.dispose(), [controller]);
  async function start(id: string, resume = false) {
    if (busy) return; setBusy(true); setError('');
    try {
      const level = hideSeekBank.levels.find(l => l.id === id)!;
      const session = resume && store.data.active?.levelId === id ? store.data.active : newSession(level, hideSeekBank.entities, child.id);
      await store.checkpoint(session);
      setController(new HideSeekController(session, level, hideSeekBank.entities, store.checkpoint));
    } catch (e) { setError(e instanceof Error ? e.message : 'Chưa mở được khu rừng.'); } finally { setBusy(false); }
  }
  if (controller) return <HideSeekGame key={controller.session.id} controller={controller} store={store} muted={muted} onMute={onMute}
    onBack={() => { setController(undefined); refresh(n => n + 1); }} onRestart={() => void start(controller.level.id)}
    onNext={() => { const index = hideSeekBank.levels.findIndex(l => l.id === controller.level.id); void start(hideSeekBank.levels[(index + 1) % hideSeekBank.levels.length].id); }}/>;
  return <main className="hs-lobby"><nav><button onClick={onBack}><ArrowLeft/> Bản đồ</button><span>WORDY WINGS · MOMO’S FOREST</span><button onClick={onMute} aria-label={muted ? 'Bật âm thanh' : 'Tắt âm thanh'}>{muted ? <VolumeX/> : <Volume2/>}</button></nav>
    <section className="hs-lobby-hero"><div><span className="eyebrow"><Leaf size={16}/> LITTLE DISCOVERIES, BIG SMILES</span><h1>Trốn tìm<br/><em>cùng Momo</em></h1><p>Vạch lá, tung lưới, mở đá phép thuật.<br/>Tìm đúng người bạn qua những câu hỏi YES / NO.</p><div className="hs-lobby-tags"><span>6 khu rừng nhỏ</span><span>3 công cụ khám phá</span><span>Chơi theo nhịp của bé</span></div></div><Pip friend="momo"/></section>
    {error && <p className="hs-lobby-error" role="alert">{error}</p>}
    {!ready ? <p className="hs-lobby-loading">Đang mở khu rừng…</p> : <><div className="hs-lobby-heading"><h2>Hôm nay mình tìm gì?</h2><span>Chọn một chuyến khám phá</span></div><div className="hs-level-grid">{hideSeekBank.levels.map((level, i) => {
      const entity = hideSeekBank.entities.find(e => e.id === level.targetEntityId)!, record = store.data.history[level.id];
      const resume = store.data.active?.levelId === level.id && store.data.active.state.phase !== 'completed';
      return <button key={level.id} className="hs-level-card" disabled={busy} onClick={() => void start(level.id, resume)}><div className="hs-level-top"><span className="hs-level-number">{String(i + 1).padStart(2, '0')}</span><HideSeekStars count={record?.bestStars ?? 0}/></div><h3>{entity.quest}</h3><p>{level.spots.length} chỗ ẩn · Mức khám phá {level.difficulty}</p><span className="hs-level-action">{resume ? <><RotateCcw size={18}/> Tiếp tục lượt đang chơi</> : record ? <><Play size={18}/> Khám phá lần nữa</> : <><Leaf size={18}/> Vào khu rừng</>}<ArrowRight size={18}/></span></button>;
    })}</div><p className="hs-lobby-note">Mỗi lượt bắt đầu với 3 sao. Mở chỗ ẩn và thử công cụ không mất sao. Chỉ câu trả lời nhận diện sai mới giảm 1 sao; bé vẫn chơi tiếp khi còn 0 sao.</p></>}
  </main>;
}
