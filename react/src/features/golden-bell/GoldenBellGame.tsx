import { useEffect, useRef, useState } from 'react';
import Phaser from 'phaser';
import { ArrowLeft, Lightbulb, Pause, Play, RotateCcw, Timer, Trophy, Volume2, VolumeX } from 'lucide-react';
import { GoldenBellArena, type ArenaClock } from '../../game/golden-bell/GoldenBellArena';
import { sessionScore, timeLimitMs } from '../../game/golden-bell/model';
import type { GoldenBellStore } from '../../game/golden-bell/store';
import { Pip } from '../../components/Illustrations';

export default function GoldenBellGame({ store, muted, onMute, onBack }: { store: GoldenBellStore; muted: boolean; onMute: () => void; onBack: () => void }) {
  const host = useRef<HTMLDivElement>(null), scene = useRef<GoldenBellArena>();
  const [compact, setCompact] = useState(matchMedia('(max-width: 760px)').matches);
  const [paused, setPaused] = useState(false), [notice, setNotice] = useState(''), [status, setStatus] = useState(''), [prompt, setPrompt] = useState('');
  const [revision, setRevision] = useState(0), [summary, setSummary] = useState(false);
  const [syncState, setSyncState] = useState('Đã lưu kết quả trên thiết bị.');
  const [clock, setClock] = useState<ArenaClock>({ remainingMs: timeLimitMs(store.session!.questions[store.session!.index]), limitMs: timeLimitMs(store.session!.questions[store.session!.index]), score: sessionScore(store.session!), possibleScore: 150, running: false });
  useEffect(() => { const media = matchMedia('(max-width: 760px)'), update = () => setCompact(media.matches); media.addEventListener('change', update); return () => media.removeEventListener('change', update); }, []);
  useEffect(() => {
    if (!host.current) return;
    let alive = true;
    const arena = new GoldenBellArena(store, muted, { status: t => alive && setStatus(t), prompt: t => alive && setPrompt(t), error: t => alive && setNotice(t), changed: () => alive && setRevision(n => n + 1), clock: value => alive && setClock(value), ready: s => { scene.current = s; s.setPaused(paused); } });
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
  const correct = session.records.filter(r => r.completed).length, timeouts = session.records.filter(r => r.timedOut).length;
  const timed = session.scoringVersion === 1, seconds = Math.ceil(clock.remainingMs / 1000);
  const leave = async () => { if (!scene.current || await scene.current.leave()) onBack(); };
  return <main className={`gb-play ${compact ? 'compact' : ''}`}>
    <div className="gb-toolbar"><button onClick={() => void leave()} aria-label="Lưu và về Đảo Chuông"><ArrowLeft size={20}/><span>Đảo Chuông</span></button><div className="gb-toolbar-title"><strong>Đuổi hình bắt chữ</strong><span>{session.phase === 'intro' ? 'Cùng đánh thức Chuông Sao' : `Câu ${Math.min(session.index + 1, 12)} / 12 · ${session.questions[session.index].id}`}</span></div><div className="gb-tools"><button onClick={() => scene.current?.replay()} aria-label="Nghe lại câu hỏi"><Volume2 size={21}/></button><button onClick={() => scene.current?.hint()} aria-label="Gợi ý khi cần"><Lightbulb size={21}/></button><button onClick={() => setPaused(true)} aria-label="Tạm dừng"><Pause size={21}/></button><button onClick={onMute} aria-label={muted ? 'Bật âm thanh' : 'Tắt âm thanh'}>{muted ? <VolumeX size={21}/> : <Volume2 size={21}/>}</button></div></div>
    {timed ? <div className="gb-scoreboard">
      <div className="gb-score"><Trophy size={27}/><span><small>TỔNG ĐIỂM</small><strong>{clock.score.toLocaleString('vi-VN')} <em>/ 1.800</em></strong></span></div>
      <div className={`gb-countdown ${seconds <= 5 && session.phase === 'question' ? 'urgent' : ''}`}>
        <Timer size={25}/><div><span><strong role="timer" aria-label={`Còn ${seconds} giây trên ${clock.limitMs / 1000} giây`}>{seconds}<small> / {clock.limitMs / 1000}s</small></strong><small>{paused || notice ? 'TẠM DỪNG' : session.phase !== 'question' ? 'THỜI GIAN MỖI CÂU' : clock.running ? 'THỜI GIAN CÒN LẠI' : 'NGHE / XEM TRƯỚC'}</small></span><div className="gb-time-track"><i style={{ width: `${clock.remainingMs / clock.limitMs * 100}%` }}/></div></div>
      </div><span className="gb-score-potential">{session.phase === 'question' ? `Câu này: tối đa +${clock.possibleScore} điểm` : 'Đúng +100 · Nhanh +0–50'}</span>
    </div> : <p className="gb-legacy-round">Lượt lưu cũ chưa có tính điểm. Mở lượt mới để chơi với đồng hồ và điểm số.</p>}
    {compact && <p className="gb-rotate">Xoay ngang để nhìn hình lớn hơn. Bé vẫn có thể chơi ở đây.</p>}
    <div className="gb-canvas" style={{ aspectRatio: compact ? '9 / 13' : '16 / 9' }} ref={host} role="application" aria-label={`Chuông Sao. ${prompt}. Phím 1 đến 4 chọn đáp án; với xếp câu dùng 1 đến 6, Enter để ghép và Backspace để bỏ thẻ cuối.`}/>
    <p className="gb-access-status" role="status" aria-live="polite">{status}</p>
    <div className="gb-play-note"><span>{timed ? 'Sai: −25 điểm/câu · Gợi ý: −20 · Hết giờ: 0 điểm, chuyển câu' : 'Tiến trình được lưu sau mỗi câu trả lời.'}</span><span>1–4 chọn đáp án · Chạm hoặc kéo thẻ để xếp câu</span></div>
    {session.phase === 'bell' && <button className="gb-access-ring" onClick={() => scene.current?.ring()}>Rung Chuông Sao</button>}
    {(paused || notice) && <div className="overlay gb-overlay"><section className="modal"><Pip friend="poki"/><h2>{notice ? 'Giữ lại chuyến phiêu lưu' : 'Nghỉ một chút nhé!'}</h2><p>{notice || 'Chuông Sao vẫn đang đợi bé. Đồng hồ và lượt chơi đã được giữ lại.'}</p>{notice ? <button className="primary" onClick={() => { scene.current?.setPaused(false); scene.current?.retry(); }}><RotateCcw size={19}/> Thử lại</button> : <button className="primary" onClick={() => setPaused(false)}><Play size={19}/> Chơi tiếp</button>}<button className="text-button" onClick={() => void leave()}>Về Đảo Chuông</button></section></div>}
    {summary && <div className="overlay gb-overlay"><section className="result-card gb-summary">
      <span className="eyebrow">STAR BELL AWAKENED</span><div className="gb-token">✦</div><h1>Chuông Sao đã thức dậy!</h1>
      <p>Bé đã đi qua 12 câu hỏi và nhận <strong>1 Bell Token</strong>.</p>
      {timed && <div className="gb-final-score"><Trophy/><strong>{sessionScore(session).toLocaleString('vi-VN')}</strong><span>điểm / 1.800</span></div>}
      <div className="gb-summary-metrics"><span><strong>{correct}/12</strong>Trả lời đúng</span><span><strong>{session.records.filter(r => r.completed && r.wrong === 0).length}</strong>Đúng lần đầu</span><span><strong>{timeouts}</strong>Câu hết giờ</span></div>
      {(wrong > 0 || timeouts > 0) && <p>Mỗi lần thử đều giúp bé nhớ thêm. Những từ cần ôn đã được giữ lại cho chuyến sau.</p>}
      {timed && <div className="gb-round-scores" aria-label="Điểm từng câu">{session.records.map((r, i) => <span key={r.questionId} className={r.timedOut ? 'timed-out' : ''} title={`${r.questionId}: ${r.timedOut ? 'Hết giờ' : 'Đúng'} · ${((r.answerMs ?? 0) / 1000).toFixed(1)}s`}><small>Câu {i + 1}</small><strong>{r.score ?? 0}</strong><small>{r.timedOut ? 'Hết giờ' : 'điểm'}</small></span>)}</div>}
      <p className="gb-summary-hints">Đã dùng gợi ý ở {hints} câu.</p><small className="gb-sync-result" role="status">{syncState}</small>
      <button className="primary" onClick={() => void leave()}>Về Đảo Chuông <ArrowLeft size={18}/></button>
    </section></div>}
  </main>;
}
