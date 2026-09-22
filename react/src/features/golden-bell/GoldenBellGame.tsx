import { useEffect, useRef, useState } from 'react';
import Phaser from 'phaser';
import { ArrowLeft, Lightbulb, Pause, Play, RotateCcw, Volume2, VolumeX } from 'lucide-react';
import { GoldenBellArena } from '../../game/golden-bell/GoldenBellArena';
import type { GoldenBellStore } from '../../game/golden-bell/store';
import { Pip } from '../../components/Illustrations';

export default function GoldenBellGame({ store, muted, onMute, onBack }: { store: GoldenBellStore; muted: boolean; onMute: () => void; onBack: () => void }) {
  const host = useRef<HTMLDivElement>(null), scene = useRef<GoldenBellArena>();
  const [compact, setCompact] = useState(matchMedia('(max-width: 760px)').matches);
  const [paused, setPaused] = useState(false), [notice, setNotice] = useState(''), [status, setStatus] = useState(''), [prompt, setPrompt] = useState('');
  const [revision, setRevision] = useState(0), [summary, setSummary] = useState(false);
  const [syncState, setSyncState] = useState('Đã lưu kết quả trên thiết bị.');
  useEffect(() => { const media = matchMedia('(max-width: 760px)'), update = () => setCompact(media.matches); media.addEventListener('change', update); return () => media.removeEventListener('change', update); }, []);
  useEffect(() => {
    if (!host.current) return;
    let alive = true;
    const arena = new GoldenBellArena(store, muted, { status: t => alive && setStatus(t), prompt: t => alive && setPrompt(t), error: t => alive && setNotice(t), changed: () => alive && setRevision(n => n + 1), ready: s => { scene.current = s; s.setPaused(paused); } });
    const game = new Phaser.Game({ type: Phaser.AUTO, parent: host.current, width: compact ? 900 : 1600, height: compact ? 1300 : 900, backgroundColor: '#d8eee6', scene: [arena], scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH }, render: { antialias: true, roundPixels: false }, audio: { noAudio: true }, input: { activePointers: 2 } });
    return () => { alive = false; scene.current = undefined; game.destroy(true); };
  }, [store, compact]);
  useEffect(() => { scene.current?.setMuted(muted); }, [muted]);
  useEffect(() => { scene.current?.setPaused(paused || !!notice); }, [paused, notice]);
  useEffect(() => { const hidden = () => { if (document.hidden) setPaused(true); }; document.addEventListener('visibilitychange', hidden); return () => document.removeEventListener('visibilitychange', hidden); }, []);
  const session = store.session!;
  useEffect(() => {
    if (session.phase !== 'summary' || store.repository?.local) return;
    let live = true;
    setSyncState('Đã lưu trên thiết bị. Đang đồng bộ tài khoản…');
    void store.sync().then(() => { if (live) setSyncState('Kết quả đã đồng bộ với tài khoản.'); }).catch(() => { if (live) setSyncState('Đã lưu trên thiết bị. Kết quả sẽ đồng bộ khi kết nối lại.'); });
    return () => { live = false; };
  }, [session.phase, store]);
  useEffect(() => {
    if (session.phase !== 'summary') return;
    const timer = setTimeout(() => setSummary(true), 1900); return () => clearTimeout(timer);
  }, [session.phase, revision]);
  const wrong = session.records.reduce((n, r) => n + r.wrong, 0), hints = session.records.filter(r => r.hintUsed).length;
  return <main className={`gb-play ${compact ? 'compact' : ''}`}>
    <div className="gb-toolbar"><button onClick={onBack} aria-label="Lưu và về Đảo Chuông"><ArrowLeft size={20}/><span>Đảo Chuông</span></button><div className="gb-toolbar-title"><strong>Đuổi hình bắt chữ</strong><span>{session.phase === 'intro' ? 'Cùng đánh thức Chuông Sao' : `Câu ${Math.min(session.index + 1, 12)} / 12 · ${session.questions[session.index].id}`}</span></div><div className="gb-tools"><button onClick={() => scene.current?.replay()} aria-label="Nghe lại câu hỏi"><Volume2 size={21}/></button><button onClick={() => scene.current?.hint()} aria-label="Gợi ý khi cần"><Lightbulb size={21}/></button><button onClick={() => setPaused(true)} aria-label="Tạm dừng"><Pause size={21}/></button><button onClick={onMute} aria-label={muted ? 'Bật âm thanh' : 'Tắt âm thanh'}>{muted ? <VolumeX size={21}/> : <Volume2 size={21}/>}</button></div></div>
    {compact && <p className="gb-rotate">Xoay ngang để nhìn hình lớn hơn. Bé vẫn có thể chơi ở đây.</p>}
    <div className="gb-canvas" style={{ aspectRatio: compact ? '9 / 13' : '16 / 9' }} ref={host} role="application" aria-label={`Chuông Sao. ${prompt}. Phím 1 đến 4 chọn đáp án; với xếp câu dùng 1 đến 6, Enter để ghép và Backspace để bỏ thẻ cuối.`}/>
    <p className="gb-access-status" role="status" aria-live="polite">{status}</p>
    <div className="gb-play-note"><span>Tiến trình được lưu sau mỗi câu trả lời.</span><span>1–4 chọn đáp án · Chạm hoặc kéo thẻ để xếp câu</span></div>
    {session.phase === 'bell' && <button className="gb-access-ring" onClick={() => scene.current?.ring()}>Rung Chuông Sao</button>}
    {(paused || notice) && <div className="overlay gb-overlay"><section className="modal"><Pip friend="poki"/><h2>{notice ? 'Giữ lại chuyến phiêu lưu' : 'Nghỉ một chút nhé!'}</h2><p>{notice || 'Chuông Sao vẫn đang đợi bé. Lượt chơi đã được giữ lại.'}</p>{notice ? <button className="primary" onClick={() => { scene.current?.setPaused(false); scene.current?.retry(); }}><RotateCcw size={19}/> Thử lại</button> : <button className="primary" onClick={() => setPaused(false)}><Play size={19}/> Chơi tiếp</button>}<button className="text-button" onClick={onBack}>Về Đảo Chuông</button></section></div>}
    {summary && <div className="overlay gb-overlay"><section className="result-card gb-summary"><span className="eyebrow">STAR BELL AWAKENED</span><div className="gb-token">✦</div><h1>Chuông Sao đã thức dậy!</h1><p>Bé đã tìm đủ 12 Word Stars và nhận <strong>1 Bell Token</strong>.</p><div className="gb-summary-metrics"><span><strong>12/12</strong>Câu hoàn thành</span><span><strong>{session.records.filter(r => r.wrong === 0).length}</strong>Đúng lần đầu</span><span><strong>{hints}</strong>Lần được gợi ý</span></div>{wrong > 0 && <p>Mỗi lần thử đều giúp bé nhớ thêm. Những từ cần ôn đã được giữ lại cho chuyến sau.</p>}<Pip friend="momo"/><small className="gb-sync-result" role="status">{syncState}</small><button className="primary" onClick={onBack}>Về Đảo Chuông <ArrowLeft size={18}/></button></section></div>}
  </main>;
}
