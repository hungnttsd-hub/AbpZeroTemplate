import { useEffect, useRef, useState, useSyncExternalStore } from 'react';
import Phaser from 'phaser';
import { ArrowLeft, Check, Hand, Pause, Play, RotateCcw, Smartphone, Star, Volume2, VolumeX } from 'lucide-react';
import { Pip } from '../../components/Illustrations';
import { HideSeekScene } from '../../game/hide-seek/HideSeekScene';
import { HideSeekController } from '../../game/hide-seek/controller';
import { atlasFrameStyle, objectFrames, spotNames, svgUrl, toolArt, toolLabels } from '../../game/hide-seek/art';
import { forestLayout } from '../../game/hide-seek/layout';
import type { HideSeekStore } from '../../game/hide-seek/store';

export function HideSeekStars({ count, large = false }: { count: number; large?: boolean }) {
  return <span className={`hs-stars ${large ? 'large' : ''}`} role="img" aria-label={`${count} trên 3 sao`}>{[0, 1, 2].map(i => <Star key={i} className={i < count ? 'earned' : ''} aria-hidden="true"/>)}</span>;
}
function Portrait({ id }: { id: string }) {
  return <div className="hs-portrait" role="img" aria-label="Đối tượng vừa tìm thấy"><span style={atlasFrameStyle(objectFrames.indexOf(id))}/></div>;
}
export default function HideSeekGame({ controller: c, store, muted, onMute, onBack, onNext, onRestart }: {
  controller: HideSeekController; store: HideSeekStore; muted: boolean; onMute: () => void;
  onBack: () => void; onNext: () => void; onRestart: () => void;
}) {
  useSyncExternalStore(c.subscribe, c.getSnapshot);
  const host = useRef<HTMLDivElement>(null), arena = useRef<HideSeekScene>(), panel = useRef<HTMLElement>(null);
  const pausePanel = useRef<HTMLElement>(null), successPanel = useRef<HTMLElement>(null);
  const [size, setSize] = useState({ width: window.innerWidth, height: window.innerHeight });
  const [ready, setReady] = useState(false), [assetError, setAssetError] = useState('');
  const [hint, setHint] = useState('Chọn công cụ, cùng Momo khám phá nhé!');
  const [audioFailed, setAudioFailed] = useState(false), [tap, setTap] = useState(false), [feedbackReady, setFeedbackReady] = useState(false);
  const [syncText, setSyncText] = useState('Đã lưu trên thiết bị.');
  const [celebrated, setCelebrated] = useState(false);
  const portrait = size.height > size.width, state = c.state;
  const hasQuiz = ['asking', 'feedback', 'assisted_retry'].includes(state.phase);
  const blocked = state.paused || portrait || !!c.error || !!assetError || !ready;
  const layout = forestLayout(size.width, size.height, c.level);
  useEffect(() => {
    if (!host.current) return;
    let alive = true;
    const scene = new HideSeekScene(c, muted, { ready: value => { arena.current = value; if (alive) setReady(true); }, hint: text => alive && setHint(text), audioUnavailable: () => alive && setAudioFailed(true), error: text => alive && setAssetError(text) });
    const game = new Phaser.Game({ type: Phaser.AUTO, parent: host.current, width: host.current.clientWidth, height: host.current.clientHeight,
      scene, transparent: true, scale: { mode: Phaser.Scale.NONE }, render: { antialias: true }, audio: { noAudio: true }, input: { activePointers: 2 } });
    const observer = new ResizeObserver(entries => {
      const rect = entries[0].contentRect; if (!rect.width || !rect.height) return;
      scene.cancelGesture(); game.scale.resize(rect.width, rect.height); setSize({ width: rect.width, height: rect.height });
    }); observer.observe(host.current);
    const blur = () => { scene.cancelGesture(); c.pause(true); };
    const hidden = () => { if (document.hidden) blur(); };
    window.addEventListener('blur', blur); window.addEventListener('pagehide', blur); document.addEventListener('visibilitychange', hidden);
    return () => { alive = false; observer.disconnect(); window.removeEventListener('blur', blur); window.removeEventListener('pagehide', blur); document.removeEventListener('visibilitychange', hidden); scene.cancelGesture(); arena.current = undefined; game.destroy(true); };
  }, [c]);
  useEffect(() => { arena.current?.setMuted(muted); }, [muted, ready]);
  useEffect(() => { if (arena.current) arena.current.tapToOpen = tap; }, [tap, ready]);
  useEffect(() => { if (portrait) c.pause(true); }, [portrait, c]);
  useEffect(() => {
    if (state.phase !== 'feedback' || state.paused) { setFeedbackReady(false); return; }
    const timer = setTimeout(() => setFeedbackReady(true), 1200); return () => clearTimeout(timer);
  }, [state.phase, state.feedback?.answerEventId, state.paused]);
  useEffect(() => {
    if (!hasQuiz || blocked) return;
    const preferred = panel.current?.querySelector<HTMLButtonElement>('button[data-answer],button.hs-continue:not(:disabled)');
    (preferred ?? panel.current?.querySelector<HTMLButtonElement>('button:not(:disabled)'))?.focus({ preventScroll: true });
  }, [state.phase, blocked, hasQuiz, feedbackReady]);
  useEffect(() => {
    if (state.phase !== 'completed' || blocked) return;
    const timer = setTimeout(() => setCelebrated(true), matchMedia('(prefers-reduced-motion: reduce)').matches ? 200 : 1500);
    return () => clearTimeout(timer);
  }, [state.phase, blocked]);
  useEffect(() => {
    const activePanel = blocked ? pausePanel.current : celebrated ? successPanel.current : null;
    activePanel?.querySelector<HTMLButtonElement>('button:not(:disabled)')?.focus({ preventScroll: true });
  }, [blocked, celebrated, c.saving]);
  useEffect(() => {
    if (state.phase !== 'completed' || c.saving || c.error || store.repository.local) return;
    let live = true;
    void store.sync().then(() => { if (live) setSyncText('Đã đồng bộ kết quả với tài khoản.'); }).catch(() => { if (live) setSyncText('Đã lưu trên thiết bị · Sẽ đồng bộ khi có kết nối.'); });
    return () => { live = false; };
  }, [state.phase, c.saving, c.error, store]);
  const leave = async () => { c.pause(true); if (await c.checkpoint()) onBack(); };
  const trap = (event: React.KeyboardEvent<HTMLElement>) => {
    if (event.key === 'Escape') { c.pause(true); return; }
    if (event.key !== 'Tab') return;
    const buttons = [...event.currentTarget.querySelectorAll<HTMLButtonElement>('button:not(:disabled)')];
    const first = buttons[0], last = buttons[buttons.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
  };
  return <main className={`hs-game ${layout.compact ? 'hs-compact' : ''}`}>
    <div className="hs-stage" ref={host} aria-hidden="true"/>
    <header className="hs-hud">
      <button className="hs-brand" onClick={() => c.pause(true)} aria-label="Wordy Wings, mở menu tạm dừng"><b>Wordy</b><strong>Wings</strong><small>HIDE & SEEK</small></button>
      <div className="hs-quest"><button className="hs-round hs-listen" onClick={() => arena.current?.replay()} disabled={blocked} aria-label="Nghe lại nhiệm vụ hoặc câu hỏi"><Volume2/></button><h1>{c.target.quest}</h1></div>
      <div className="hs-status"><HideSeekStars count={state.stars}/><button className="hs-round" onClick={() => c.pause(true)} aria-label="Tạm dừng"><Pause/></button></div>
    </header>
    {!hasQuiz && state.phase !== 'completed' && <div className="hs-momo-bubble"><span>{state.stars === 0 ? 'Let’s try together!' : 'Let’s find it together!'}</span></div>}
    <div className="hs-spot-access" aria-label="Các chỗ ẩn trong khu rừng" hidden={blocked || hasQuiz || state.phase !== 'searching'}>
      {layout.spots.map((rect, i) => { const spot = c.level.spots.find(s => s.id === rect.id)!; return <button key={rect.id} className="hs-access-spot" style={{ left: rect.x, top: rect.y, width: rect.width, height: rect.height }}
        disabled={state.revealedSpotIds.includes(rect.id)} onClick={e => { if (e.detail === 0) arena.current?.openAccessible(rect.id); }} aria-label={`${spotNames[spot.kind]} ${i + 1}. Dùng công cụ đã chọn để mở.`}><span>{spotNames[spot.kind]}</span></button>; })}
    </div>
    <footer className="hs-tool-area" aria-hidden={hasQuiz || state.phase === 'completed' || blocked}>
      <div className="hs-tool-tray">{c.level.tools.map(tool => <button key={tool} className={`hs-tool ${c.session.selectedTool === tool ? 'selected' : ''}`} aria-pressed={c.session.selectedTool === tool}
        disabled={blocked || state.phase !== 'searching'} onClick={() => { arena.current?.cancelGesture(); arena.current?.unlockAudio(); c.selectTool(tool); setHint(toolLabels[tool].hint); }}>
        <img src={svgUrl(toolArt[tool])} alt=""/><strong>{toolLabels[tool].en}</strong><small>{toolLabels[tool].vi}</small><span className="hs-tool-check"><Check size={14}/></span></button>)}</div>
      <label className="hs-tap-option"><input type="checkbox" checked={tap} disabled={blocked || state.phase !== 'searching'} onChange={e => { arena.current?.cancelGesture(); setTap(e.target.checked); }}/><Hand size={17}/><span>Chạm để mở lá</span></label>
      <p className="hs-help" role="status">{hint}</p>
    </footer>
    {hasQuiz && c.occupant && !blocked && <div className="hs-quiz-scrim"><section className={`hs-quiz ${state.phase === 'feedback' ? 'hs-feedback' : ''}`} ref={panel} role="dialog" aria-modal="true" aria-labelledby="hs-question" onKeyDown={trap}>
      <div className="hs-quiz-top"><span>LOOK & LISTEN</span><div><button className="hs-small-icon" onClick={() => arena.current?.replay()} aria-label="Nghe lại câu hỏi hoặc giải thích"><Volume2/></button><button className="hs-small-icon" onClick={() => c.pause(true)} aria-label="Tạm dừng"><Pause/></button></div></div>
      <Portrait id={c.occupant.id}/><div className="hs-quiz-content"><h2 id="hs-question">{c.target.question}</h2>
        {state.phase === 'feedback' ? <><p className="hs-identification" role="status">{state.feedback!.phrase}</p><button className="hs-continue" disabled={!feedbackReady} onClick={() => c.continue()}>{state.feedback!.nextPhase === 'completed' ? 'You found it!' : state.feedback!.nextPhase === 'assisted_retry' ? 'Thử lại cùng Momo' : 'Keep looking!'} <Play size={19}/></button></>
          : <><div className="hs-answers"><button className={`hs-yes ${state.phase === 'assisted_retry' ? 'assisted' : ''}`} data-answer="yes" onClick={() => c.answer(true)}>YES</button><button className="hs-no" data-answer="no" onClick={() => c.answer(false)}>NO</button></div>{state.phase === 'assisted_retry' && <p className="hs-assisted">{c.occupant.affirmative} Chọn YES cùng Momo nhé.</p>}</>}
      </div><p className="hs-quiz-note"><Star/>{state.phase === 'feedback' ? state.feedback!.deltaStars === -1 ? 'Mình học thêm một từ rồi. Cùng thử tiếp nhé!' : state.feedback!.nextPhase === 'searching' ? 'Đúng rồi, cùng tìm tiếp nào!' : 'Momo luôn đồng hành cùng bé.' : state.stars === 0 ? 'Cứ thử nhé, Momo sẽ giúp bé.' : 'Nhìn kỹ rồi chọn YES hoặc NO nhé.'}</p>
    </section></div>}
    {state.phase === 'completed' && celebrated && !blocked && <div className="hs-success-wrap"><section ref={successPanel} className="hs-success" role="dialog" aria-modal="true" aria-label="Kết quả màn Trốn tìm" onKeyDown={trap}>
      <span className="eyebrow">HIDE & SEEK · {c.level.id}</span><h2>You found it!</h2><HideSeekStars count={state.stars} large/><p>{c.target.affirmative}</p>
      <small>{state.attempts.some(a => a.assisted) ? 'Bé đã tìm thấy với sự trợ giúp của Momo.' : 'Một cuộc khám phá thật đáng nhớ!'}</small>
      <div className="hs-success-actions"><button className="hs-continue" disabled={c.saving} onClick={onNext}>Khám phá tiếp <Play size={20}/></button><button className="hs-replay" disabled={c.saving} onClick={onRestart}><RotateCcw size={18}/> Chơi lại</button></div>
      <button className="hs-back" disabled={c.saving} onClick={() => void leave()}><ArrowLeft size={17}/> Về khu rừng</button><small role="status">{c.saving ? 'Đang lưu…' : syncText}</small>
    </section></div>}
    {(blocked && (state.paused || portrait || c.error || assetError)) && <div className="hs-pause-scrim"><section ref={pausePanel} className="hs-pause" role="dialog" aria-modal="true" aria-label="Tạm dừng" onKeyDown={trap}>
      {portrait ? <Smartphone className="hs-rotate-icon"/> : <Pip friend="momo"/>}<h2>{portrait ? 'Xoay ngang để khám phá' : c.error || assetError ? 'Giữ lại cuộc phiêu lưu' : 'Nghỉ một chút nhé!'}</h2>
      <p>{c.error || assetError || 'Vị trí các bạn, câu hỏi và ngôi sao vẫn được giữ nguyên.'}</p>
      {!portrait && !assetError && <button className="hs-continue" disabled={c.saving} onClick={async () => { if (!c.error || await c.checkpoint()) { c.pause(false); arena.current?.replay(); } }}><Play/> {c.error ? 'Thử lưu lại' : 'Chơi tiếp'}</button>}
      <button className="hs-replay" onClick={onMute}>{muted ? <VolumeX/> : <Volume2/>}{muted ? 'Bật âm thanh' : 'Tắt âm thanh'}</button>
      <button className="hs-back" onClick={() => void leave()}><ArrowLeft size={18}/> Lưu và về khu rừng</button>
    </section></div>}
    {!ready && !assetError && <div className="hs-loading"><Pip friend="momo"/><p>Momo đang chuẩn bị khu rừng…</p></div>}
    {audioFailed && <span className="hs-audio-notice" role="status">Chưa phát được giọng đọc · Bé vẫn chơi bằng hình và chữ.</span>}
  </main>;
}
