import { lazy, Suspense, useCallback, useEffect, useRef, useState } from 'react';
import { ArrowLeft, ArrowRight, Check, ChevronRight, Cloud, CloudOff, Compass, Flag, Headphones, Home, LockKeyhole, Map, Play, Plus, Settings2, ShieldCheck, Sparkles, Star, Trophy, Volume2, VolumeX, X } from 'lucide-react';
import { Pip, Island } from './components/Illustrations';
import { ApiError, GameRepository, getSession, type Session } from './services/repository';
import { storage } from './services/storage';
import { type Attempt, type Child, type Dashboard, type LevelDefinition, type Progress, type World, unlocked, starsFor } from './types';
import { wordImage } from './game/art';
const BalloonDartGallery = lazy(() => import('./features/balloon-dart/BalloonDartGallery'));
const WordBuilderDemoGallery = lazy(() => import('./features/word-builder/WordBuilderDemoGallery'));
const GameContainer = lazy(() => import('./game/GameContainer'));
type Page = 'welcome' | 'children' | 'map' | 'play' | 'dashboard' | 'admin' | 'word-builder' | 'balloon-dart';
const mechanics: Record<string, string> = { word_shot: 'Kéo ná tìm từ', balloon_pop: 'Vườn bóng phi tiêu', balloon_dart: 'Vườn bóng phi tiêu', drag_sort: 'Chiếc giỏ kỳ diệu', letter_puzzle: 'Xưởng chữ phiêu lưu', word_builder: 'Xưởng chữ phiêu lưu', boss_challenge: 'Giải cứu Word Star' };
const worldNotes = ['Sắc màu & hình khối', 'Những người bạn nhỏ', 'Ngôi nhà thân quen'];
const avatars = ['pip', 'poki', 'lulu', 'momo'];
const message = (error: unknown) => error instanceof Error ? error.message : 'Pip chưa thực hiện được. Hãy thử lại nhé.';

export default function App() {
  const [page, setPage] = useState<Page>('welcome'); const [session, setSession] = useState<Session>();
  const [repo, setRepo] = useState<GameRepository>(); const [children, setChildren] = useState<Child[]>([]);
  const [child, setChild] = useState<Child>(); const [worlds, setWorlds] = useState<World[]>([]);
  const [levels, setLevels] = useState<LevelDefinition[]>([]); const [progress, setProgress] = useState<Progress[]>([]);
  const [worldIndex, setWorldIndex] = useState(0); const [level, setLevel] = useState<LevelDefinition>();
  const [muted, setMuted] = useState(false); const [busy, setBusy] = useState(false); const [error, setError] = useState('');
  const [result, setResult] = useState<Attempt>(); const [unsaved, setUnsaved] = useState<Attempt>();
  const [gate, setGate] = useState(false); const [gateAnswer, setGateAnswer] = useState(''); const [gateTarget, setGateTarget] = useState<Page>('dashboard');
  const [dashboard, setDashboard] = useState<Dashboard>(); const [syncState, setSyncState] = useState('');
  const [pending, setPending] = useState(0); const [creating, setCreating] = useState(false);
  const [nickname, setNickname] = useState(''); const [avatar, setAvatar] = useState('pip'); const [ageBand, setAgeBand] = useState('5_6');
  const enteredAt = useRef(Date.now()); const [breakTime, setBreakTime] = useState(false);
  const navigate = (next: Page) => { setPage(next); setError(''); location.hash = next === 'map' ? `/map/${child?.id ?? ''}` : `/${next}`; };
  const sync = useCallback(async (repository: GameRepository) => {
    if (repository.local) return;
    try { await repository.sync(); setSyncState('Đã đồng bộ'); }
    catch (e) { setSyncState(message(e)); }
    setPending((await repository.pending()).length);
  }, []);
  async function enter(repository: GameRepository) {
    setBusy(true); setError('');
    try {
      const [content, profiles] = await Promise.all([repository.content(), repository.children()]);
      setRepo(repository); setWorlds(content.worlds); setLevels(content.levels); setChildren(profiles); setCreating(profiles.length === 0);
      await storage.set('settings:lastRepository', { owner: repository.owner, local: repository.local });
      if (repository.local) {
        const url = new URL(location.href); url.searchParams.delete('account');
        history.replaceState(null, '', url);
      }
      const route = location.hash.slice(1).split('/');
      const selected = profiles.find(p => p.id === route[2]);
      if (selected && ['map', 'play'].includes(route[1])) {
        const p = await repository.progress(selected.id); setChild(selected); setProgress(p);
        const requested = content.levels.find(l => l.id === route[3]);
        if (route[1] === 'play' && requested && unlocked(requested, content.levels, p)) { setLevel(requested); setPage('play'); }
        else { setPage('map'); location.hash = `/map/${selected.id}`; }
      } else navigate('children');
      void sync(repository);
    } catch (e) { setError(message(e)); } finally { setBusy(false); }
  }
  useEffect(() => {
    let alive = true;
    void storage.get<boolean>('settings:muted').then(value => { if (alive) setMuted(value ?? false); }).catch(() => {});
    void (async () => {
      const previous = await storage.get<{ owner: string; local: boolean }>('settings:lastRepository');
      if (!alive) return;
      const accountRequested = new URLSearchParams(location.search).get('account') === '1';
      if (!accountRequested && (!previous || previous.local)) {
        if (previous?.local) await enter(new GameRepository('device', true));
        return;
      }
      try {
        const s = await getSession();
        if (alive) { setSession(s); if (s.userId) await enter(new GameRepository(s.userId, false)); else if (previous?.local) await enter(new GameRepository('device', true)); }
      } catch (e) {
        if (alive && accountRequested) setError('Chưa kết nối được tài khoản. Hãy chạy backend API hoặc chọn chơi trên thiết bị.');
        if (alive && previous && (previous.local || e instanceof TypeError || (e instanceof ApiError && e.status >= 500) || (e instanceof DOMException && e.name === 'TimeoutError'))) {
          await enter(new GameRepository(previous.owner, previous.local));
        }
      }
    })().catch(e => { if (alive) setError(message(e)); });
    return () => { alive = false; };
  }, []);
  useEffect(() => {
    if (!repo || repo.local) return;
    const reconnect = () => { void sync(repo); };
    window.addEventListener('online', reconnect); const timer = setInterval(reconnect, 30000);
    return () => { window.removeEventListener('online', reconnect); clearInterval(timer); };
  }, [repo, sync]);
  useEffect(() => {
    const back = () => {
      const route = location.hash.slice(1).split('/');
      if (route[1] === 'map' && child && route[2] === child.id) { setLevel(undefined); setResult(undefined); setPage('map'); }
      else if (route[1] === 'children' && repo) setPage('children');
      else if (route[1] === 'play' && child && route[2] === child.id) {
        const requested = levels.find(l => l.id === route[3]);
        if (requested && unlocked(requested, levels, progress)) { setLevel(requested); setPage('play'); }
      }
    };
    window.addEventListener('hashchange', back); return () => window.removeEventListener('hashchange', back);
  }, [repo, child, levels, progress]);
  useEffect(() => { const timer = setInterval(() => setBreakTime(Date.now() - enteredAt.current > 15 * 60000), 60000); return () => clearInterval(timer); }, []);
  async function selectChild(selected: Child) {
    if (!repo) return; setBusy(true); setError('');
    try { const p = await repo.progress(selected.id); setChild(selected); setProgress(p); setWorldIndex(0); enteredAt.current = Date.now(); setBreakTime(false); navigate('map'); location.hash = `/map/${selected.id}`; }
    catch (e) { setError(message(e)); } finally { setBusy(false); }
  }
  async function createChild(event: React.FormEvent) {
    event.preventDefault(); if (!repo || !nickname.trim()) return; setBusy(true); setError('');
    try { const selected = await repo.createChild({ nickname: nickname.trim(), avatarKey: avatar, ageBand }); setChildren(await repo.children()); setCreating(false); setNickname(''); await selectChild(selected); }
    catch (e) { setError(message(e)); } finally { setBusy(false); }
  }
  function start(l: LevelDefinition) {
    if (!unlocked(l, levels, progress)) return;
    setResult(undefined); setUnsaved(undefined); setLevel(l); navigate('play'); location.hash = `/play/${child?.id}/${l.id}`;
  }
  const completing = useRef(false);
  async function complete(attempt: Attempt) {
    if (!repo || !child || completing.current) return;
    completing.current = true; setUnsaved(attempt);
    try {
      await repo.save(attempt); setProgress(await repo.localProgress(child.id)); setUnsaved(undefined); setResult(attempt);
      void sync(repo);
    } catch (e) { setError(message(e)); } finally { completing.current = false; }
  }
  async function openParent(target: Page) { setGateTarget(target); setGateAnswer(''); setGate(true); }
  async function passGate(e: React.FormEvent) {
    e.preventDefault(); if (gateAnswer.trim() !== '21') { setGateAnswer(''); return; }
    setGate(false);
    if (gateTarget === 'dashboard' && child && repo) {
      setBusy(true); setDashboard(undefined);
      try { await sync(repo); setDashboard(await repo.dashboard(child.id)); navigate('dashboard'); }
      catch (e) { setError(message(e)); } finally { setBusy(false); }
    } else { navigate(gateTarget); }
  }
  function toggleMute() { const next = !muted; setMuted(next); void storage.set('settings:muted', next).catch(e => setError(message(e))); }
  const totalStars = progress.reduce((sum, p) => sum + p.bestStars, 0);
  const selectedWorld = worlds[worldIndex]; const worldLevels = levels.filter(l => l.worldId === selectedWorld?.id);
  const currentLevel = worldLevels.find(l => !progress.some(p => p.levelId === l.id) && unlocked(l, levels, progress));
  const worldOpen = (index: number) => { const first = levels.find(l => l.worldId === worlds[index]?.id); return !!first && unlocked(first, levels, progress); };
  const exitGame = () => { setLevel(undefined); setResult(undefined); navigate('map'); };

  return <div className={`app ${page === 'play' ? 'playing' : ''}`}>
    {page !== 'play' && <header className="site-header"><button className="brand" onClick={() => child ? navigate('map') : navigate('welcome')} aria-label="Wordy Wings — Trang chính"><span className="brand-mark"><Pip/></span><span>wordy<span className="brand-light">wings</span><small>Little words. Big adventures.</small></span></button>
      {child && <nav aria-label="Điều hướng"><button className={page === 'map' ? 'nav-item active' : 'nav-item'} onClick={() => navigate('map')}><Compass size={18}/> Khám phá</button><button className="nav-item" onClick={() => void openParent('dashboard')}><ShieldCheck size={18}/> Góc phụ huynh</button></nav>}
      <div className="header-right"><button className="icon-button" aria-label={muted ? 'Bật âm thanh' : 'Tắt âm thanh'} onClick={toggleMute}>{muted ? <VolumeX size={20}/> : <Volume2 size={20}/>}</button>{child && <><span className="star-pill"><Star size={18} fill="currentColor"/>{totalStars}</span><button className="profile-button" onClick={() => void openParent('children')}><Pip friend={child.avatarKey}/><span>{child.nickname}</span><ChevronRight size={15}/></button></>}</div>
    </header>}
    {error && <div className="alert" role="alert">{error}<button aria-label="Đóng thông báo" onClick={() => setError('')}><X size={18}/></button></div>}
    {page === 'welcome' && <main className="welcome"><div className="welcome-copy"><span className="eyebrow"><Sparkles size={15}/> MỖI TỪ MỚI, MỘT CUỘC PHIÊU LƯU</span><h1>Đôi cánh nhỏ.<br/><em>Thế giới thật to.</em></h1><p>Cùng Pip khám phá những hòn đảo kỳ diệu.<br/>Nghe, chạm, chơi — và tiếng Anh đến thật tự nhiên.</p><button disabled={busy} className="primary large" onClick={() => void enter(new GameRepository('device', true))}>Bắt đầu khám phá <ArrowRight size={21}/></button><p className="device-note">Chơi trên thiết bị · Không cần tài khoản</p><div className="parent-login"><ShieldCheck size={19}/><span>Phụ huynh muốn lưu trên nhiều thiết bị?<br/><a href="/Account/Login?ReturnUrl=%2Fwordy-wings%2Findex.html%3Faccount%3D1">Đăng nhập</a><span> hoặc </span><a href="/Account/Register?ReturnUrl=%2Fwordy-wings%2Findex.html%3Faccount%3D1">tạo tài khoản</a></span></div></div><div className="welcome-art"><span className="floating-note"><Headphones size={18}/> Nghe một chút. Vui thật nhiều.</span><Island/><Pip className="welcome-pip"/><span className="art-star star-one">✦</span><span className="art-star star-two">✧</span></div><div className="welcome-features"><span><Compass/> 3 hòn đảo kỳ diệu</span><span><Flag/> 30 thử thách nhỏ</span><span><ShieldCheck/> Chơi vui, không áp lực</span></div></main>}
    {page === 'children' && <main className="profiles-page"><span className="eyebrow">HELLO, LITTLE EXPLORER!</span><h1>Hôm nay, ai cùng Pip phiêu lưu?</h1><p className="subtle">Mỗi nhà thám hiểm có hành trình của riêng mình.</p><div className="profiles">{children.map(c => <button className="child-card" key={c.id} disabled={busy} onClick={() => void selectChild(c)}><Pip friend={c.avatarKey}/><strong>{c.nickname}</strong><span>Cùng đi nào <ArrowRight size={15}/></span></button>)}{!creating && <button className="child-card add-child" onClick={() => setCreating(true)}><Plus size={40}/><strong>Thêm nhà thám hiểm</strong></button>}</div>
      {creating && <form className="profile-form" onSubmit={e => void createChild(e)}><h2>Làm quen với Pip nhé!</h2><label>Biệt danh của bé<input value={nickname} onChange={e => setNickname(e.target.value)} maxLength={40} placeholder="Ví dụ: Bông" required autoComplete="off"/></label><div className="avatar-picker" aria-label="Chọn bạn đồng hành">{avatars.map(a => <button key={a} type="button" aria-pressed={avatar === a} className={avatar === a ? 'selected' : ''} onClick={() => setAvatar(a)}><Pip friend={a}/><span>{a}</span></button>)}</div><label>Nhóm tuổi<select value={ageBand} onChange={e => setAgeBand(e.target.value)}><option value="5_6">5–6 tuổi</option><option value="6_7">6–7 tuổi</option><option value="7_8">7–8 tuổi</option></select></label><button className="primary" disabled={busy || !nickname.trim()} type="submit">Sẵn sàng rồi! <ArrowRight size={19}/></button></form>}
      <p className="subtle privacy-note"><ShieldCheck size={15}/> Chỉ cần biệt danh. Không thu âm, không quảng cáo, không thu ngày sinh.</p></main>}
    {page === 'map' && child && selectedWorld && <main className="map-page"><div className="page-heading"><div><span className="eyebrow">YOUR LITTLE BIG ADVENTURE</span><h1>Đi thôi, {child.nickname}! <span className="sun-symbol">☀</span></h1><p>Một từ mới đang đợi bé ở phía bên kia cầu vồng.</p></div><div className="journey-stat"><span className="stat-icon"><Flag size={22}/></span><div><strong>{progress.length}<span> / {levels.length} màn</span></strong><small>Hành trình của bé</small></div></div></div>
      <div className="builder-entry"><div><strong>Vườn bóng phi tiêu</strong><p>Nghe, ngắm và bắn bóng · Ba lượt, ba ngôi sao.</p></div><button className="secondary" onClick={() => navigate('balloon-dart')}>Khám phá 8 màn bóng <ArrowRight size={18}/></button></div><div className="builder-entry"><div><strong>Xưởng chữ phiêu lưu</strong><p>Giải cứu bạn thú, mở cửa và khởi động đoàn tàu bằng những chữ nhỏ.</p></div><button className="secondary" onClick={() => navigate('word-builder')}>Khám phá 8 nhiệm vụ <ArrowRight size={18}/></button></div><div className="map-layout"><section className="adventure-panel"><div className="world-tabs">{worlds.map((w, i) => <button key={w.id} className={i === worldIndex ? 'world-tab selected' : 'world-tab'} onClick={() => setWorldIndex(i)}><span className="world-number">{worldOpen(i) ? `0${i + 1}` : <LockKeyhole size={14}/>}</span><span>{w.name}</span></button>)}</div>
        <div className={`world-scene world-${worldIndex}`}><div className="world-caption"><span className="eyebrow">WORLD 0{worldIndex + 1}</span><h2>{selectedWorld.vi}</h2><p>{worldNotes[worldIndex]}</p></div><Island type={worldIndex} className="map-island"/><div className="scene-cloud cloud-one"/><div className="scene-cloud cloud-two"/><span className="scene-spark sparkle-a">✧</span><span className="scene-spark sparkle-b">✦</span><span className="world-tag"><Pip friend={selectedWorld.hero.toLowerCase()}/><span>Cùng {selectedWorld.hero}</span></span></div>
        <div className="level-trail"><div className="trail-heading"><span><Flag size={17}/> CON ĐƯỜNG KHÁM PHÁ</span><span>{worldLevels.filter(l => progress.some(p => p.levelId === l.id)).length} / {worldLevels.length} màn</span></div><div className="level-nodes">{worldLevels.map((l, i) => { const p = progress.find(p => p.levelId === l.id); const open = unlocked(l, levels, progress); return <div className={`node-wrap ${l.id === currentLevel?.id ? 'current' : ''}`} key={l.id}><button aria-label={`Màn ${l.order}: ${mechanics[l.mechanic]}${p ? `, ${p.bestStars} sao` : open ? ', đã mở' : ', chưa mở'}`} disabled={!open} onClick={() => start(l)} className={`level-node ${p ? 'done' : open ? 'available' : 'locked'} ${l.isBoss ? 'boss' : ''}`}>{p ? <Check/> : !open ? <LockKeyhole size={17}/> : l.isBoss ? <Trophy size={23}/> : i + 1}</button><span className={`node-stars ${p ? 'earned' : ''}`}>{p ? '★'.repeat(p.bestStars) + '☆'.repeat(3 - p.bestStars) : l.isBoss ? 'Word Star' : `${i + 1}`}</span>{l.id === currentLevel?.id && <span className="you-are-here">Bé ở đây</span>}</div>; })}</div></div>
      </section><aside className="adventure-sidebar"><section className="next-card"><span className="eyebrow">{worldOpen(worldIndex) ? currentLevel ? 'CHUYẾN ĐI TIẾP THEO' : 'MỘT HÒN ĐẢO ĐÃ NỞ HOA' : 'SẮP ĐƯỢC KHÁM PHÁ'}</span><div className="pip-greeting"><Pip friend={selectedWorld.hero.toLowerCase()}/><span>{worldOpen(worldIndex) ? 'Let’s go!' : 'See you soon!'}</span></div><h2>{worldOpen(worldIndex) ? currentLevel ? mechanics[currentLevel.mechanic] : 'Bé làm tốt lắm!' : 'Một hòn đảo mới!'}</h2><p>{worldOpen(worldIndex) ? currentLevel ? `Màn ${currentLevel.order} · ${worldNotes[worldIndex]}` : 'Chơi lại để nghe những từ yêu thích, hoặc khám phá hòn đảo tiếp theo.' : `Hoàn thành Word Star của ${worlds[Math.max(0, worldIndex - 1)]?.name} để đến đây.`}</p>
        {worldOpen(worldIndex) && <button className="primary" onClick={() => { if (currentLevel) start(currentLevel); else if (worldIndex < worlds.length - 1) setWorldIndex(worldIndex + 1); else start(worldLevels[0]); }}><Play size={18} fill="currentColor"/> {currentLevel ? 'Chơi nào!' : worldIndex < worlds.length - 1 ? 'Đảo tiếp theo' : 'Chơi lại'}</button>}
        <span className="no-pressure"><Headphones size={15}/> Nghe · Khám phá · Mỉm cười</span></section><section className="tip-card"><span className="tip-icon"><Sparkles size={22}/></span><div><h3>Cứ thử nhé, bé ơi!</h3><p>Không mất mạng, không hết giờ. Pip luôn ở đây giúp bé.</p></div></section><section className="collection-card"><div><Star size={20} fill="#eec976" stroke="#d4a451"/><strong>Những ngôi sao nhỏ</strong><span>{totalStars} / {levels.length * 3}</span></div><progress value={totalStars} max={levels.length * 3}/><p>Mỗi lần khám phá là một điều đáng tự hào.</p></section></aside></div>
      {breakTime && <div className="break-banner"><Pip/><span>Bé đã khám phá thật nhiều! Cùng Pip nghỉ mắt và vươn vai một chút nhé.</span><button className="text-button" onClick={() => { enteredAt.current = Date.now(); setBreakTime(false); }}>Đã nghỉ rồi <Check size={16}/></button></div>}
      <footer className="map-footer"><span>{repo?.local ? <Home size={15}/> : pending ? <CloudOff size={15}/> : <Cloud size={15}/>} {repo?.local ? 'Tiến trình được lưu trên thiết bị này' : pending ? `${pending} lượt chơi đang chờ đồng bộ` : 'Hành trình được lưu cùng tài khoản'}</span><span>Made for little explorers, with a little magic <Sparkles size={14}/></span></footer>
    </main>}
    {page === 'play' && child && level && <><Suspense fallback={<div className="loading-state">Pip đang chuẩn bị chuyến đi…</div>}><GameContainer key={level.id} level={level} childId={child.id} muted={muted} onMute={toggleMute} onComplete={a => void complete(a)} onExit={exitGame}/></Suspense>
      {result && <div className="overlay"><section className="result-card"><span className="eyebrow">A LITTLE WORD. A BIG WIN.</span><div className="result-stars">{'★'.repeat(starsFor(result))}<span>{'☆'.repeat(3 - starsFor(result))}</span></div><Pip/><h1>Wonderful, {child.nickname}!</h1><p>Bé vừa mang một Word Star về hòn đảo.</p><div className="word-reward"><img src={wordImage(level.targetVocabulary[0])} alt=""/><strong>{level.targetVocabulary[0]}</strong></div><button className="primary" onClick={() => { const next = levels[levels.findIndex(l => l.id === level.id) + 1]; if (next) { const index = worlds.findIndex(w => w.id === next.worldId); setWorldIndex(index); start(next); } else exitGame(); }}>{levels[levels.length - 1].id === level.id ? 'Hoàn thành hành trình!' : 'Khám phá tiếp'} <ArrowRight size={19}/></button><button className="text-button" onClick={exitGame}><Map size={18}/> Về bản đồ</button><small>Đã lưu trên thiết bị{repo?.local ? '' : ' · Tự đồng bộ khi có kết nối'}</small></section></div>}
      {unsaved && error && <div className="overlay"><section className="modal"><h2>Giữ lại ngôi sao của bé</h2><p>{error}</p><button className="primary" onClick={() => void complete(unsaved)}>Thử lưu lại</button><p className="subtle">Giữ trang này mở để không mất lượt chơi.</p></section></div>}</>}
    {page === 'dashboard' && child && dashboard && <main className="dashboard-page"><button className="text-button" onClick={() => navigate('map')}><ArrowLeft size={17}/> Về bản đồ</button><span className="eyebrow">GÓC PHỤ HUYNH</span><h1>Từng bước nhỏ của {child.nickname}</h1><p className="subtle">Khuyến khích sự tò mò, trân trọng mỗi lần bé thử.</p><div className="dashboard-stats">{[[dashboard.completedLevels, 'Màn đã khám phá'], [dashboard.stars, 'Ngôi sao đã nhận'], [dashboard.words.length, 'Từ đã gặp'], [dashboard.minutes, 'Phút phiêu lưu']].map(([value, label]) => <section key={label}><strong>{value}</strong><span>{label}</span></section>)}</div><div className="dashboard-columns"><section className="panel"><h2>Khu vườn từ vựng</h2>{dashboard.words.length ? <div className="word-grid">{dashboard.words.map(w => <div key={w.term}><img src={wordImage(w.term)} alt=""/><strong>{w.term}</strong><progress value={w.masteryScore} max={100}/><small>{w.exposureCount} lần gặp · {w.masteryScore}%</small></div>)}</div> : <p>Chơi màn đầu tiên để khu vườn bắt đầu nở hoa.</p>}</section><aside><section className="panel"><h2>Cùng bé ôn lại</h2>{dashboard.review.length ? dashboard.review.map(w => <p key={w.term} className="review-word">{w.term}<span>{w.masteryScore}%</span></p>) : <p>Chưa có từ cần ôn. Hãy để bé chơi lại những màn yêu thích.</p>}<p className="subtle">Chỉ số ghi nhớ dựa trên số lần gặp và lựa chọn trong game; không phải điểm đánh giá năng lực.</p></section><section className="panel"><h2>Lưu hành trình</h2><p>{repo?.local ? 'Đang chơi trên thiết bị này. Dữ liệu không được đồng bộ lên tài khoản; xóa dữ liệu trình duyệt sẽ xóa hành trình.' : pending ? `${pending} lượt đang chờ. ${syncState}` : syncState || 'Tiến trình đã lưu với tài khoản.'}</p>{repo && !repo.local && <button className="secondary" onClick={() => void sync(repo)}>Đồng bộ ngay</button>}<a className="text-button" href="/Account/Login?ReturnUrl=%2Fwordy-wings%2Findex.html%3Faccount%3D1">Đăng nhập tài khoản</a>{session?.isAdmin && <button className="text-button" onClick={() => navigate('admin')}><Settings2 size={16}/> Quản lý nội dung</button>}<button className="text-button" onClick={() => navigate('children')}>Đổi hồ sơ bé</button></section></aside></div></main>}
    {page === 'balloon-dart' && child && <Suspense fallback={<p className="loading-state">Đang mở vườn bóng…</p>}><BalloonDartGallery childId={child.id} scope={(repo?.owner ?? 'device') + ':' + child.id} muted={muted} onMute={toggleMute} onBack={() => navigate('map')}/></Suspense>}
    {page === 'word-builder' && child && <Suspense fallback={<p className="loading-state">Pip đang chuẩn bị…</p>}><WordBuilderDemoGallery childId={child.id} scope={(repo?.owner ?? 'device') + ':' + child.id} muted={muted} onMute={toggleMute} onBack={() => navigate('map')}/></Suspense>}
    {page === 'admin' && repo && <AdminContent repository={repo} onBack={() => navigate('map')} onChanged={async () => { const c = await repo.content(); setLevels(c.levels); }}/>} 
    {gate && <div className="overlay"><form className="modal parent-gate" onSubmit={e => void passGate(e)}><button type="button" className="close-modal" aria-label="Đóng" onClick={() => setGate(false)}><X/></button><ShieldCheck size={35}/><span className="eyebrow">DÀNH CHO NGƯỜI LỚN</span><h2>Mời ba mẹ giúp bé</h2><p>Nhập kết quả để vào khu vực phụ huynh.</p><label>14 + 7 = ?<input value={gateAnswer} onChange={e => setGateAnswer(e.target.value)} inputMode="numeric" autoFocus required aria-label="Kết quả 14 cộng 7"/></label><button type="submit" className="primary">Tiếp tục <ChevronRight size={18}/></button></form></div>}
    {busy && <div className="busy-indicator" role="status"><span/> Pip đang chuẩn bị…</div>}
  </div>;
}

interface AdminLevel { id: string; worldId: string; instruction: string; difficulty: number; mechanic: string; isPublished: boolean; contentVersion: number }
function AdminContent({ repository, onBack, onChanged }: { repository: GameRepository; onBack: () => void; onChanged: () => Promise<void> }) {
  const [items, setItems] = useState<AdminLevel[]>([]); const [filter, setFilter] = useState(''); const [editing, setEditing] = useState<AdminLevel>(); const [notice, setNotice] = useState(''); const [saving, setSaving] = useState(false);
  const load = useCallback(async () => { try { setItems(await repository.api<AdminLevel[]>('admin/levels')); } catch (e) { setNotice(message(e)); } }, [repository]);
  useEffect(() => { void load(); }, [load]);
  async function save(e: React.FormEvent) { e.preventDefault(); if (!editing) return; setSaving(true); try { await repository.api(`admin/levels/${editing.id}`, 'PUT', editing); await load(); await onChanged(); setEditing(undefined); setNotice('Đã lưu nội dung và tăng phiên bản.'); } catch (err) { setNotice(message(err)); } finally { setSaving(false); } }
  return <main className="dashboard-page"><button className="text-button" onClick={onBack}><ArrowLeft size={18}/> Bản đồ</button><h1>Quản lý nội dung</h1><p role="status">{notice}</p><label>Lọc thế giới<select value={filter} onChange={e => setFilter(e.target.value)}><option value="">Tất cả</option>{['W01', 'W02', 'W03'].map(w => <option key={w}>{w}</option>)}</select></label><div className="admin-list">{items.filter(l => !filter || l.worldId === filter).map(l => <button className="admin-row" key={l.id} onClick={() => setEditing({ ...l })}><strong>{l.id}</strong><span>{l.instruction}</span><span>{l.mechanic}</span><span>{l.isPublished ? 'Đã xuất bản' : 'Bản nháp'} · v{l.contentVersion}</span><Settings2 size={18}/></button>)}</div>{editing && <div className="overlay"><form className="modal" onSubmit={e => void save(e)}><h2>{editing.id}</h2><label>Hướng dẫn<input required maxLength={300} value={editing.instruction} onChange={e => setEditing({ ...editing, instruction: e.target.value })}/></label><label>Độ khó<input type="number" min={1} max={4} value={editing.difficulty} onChange={e => setEditing({ ...editing, difficulty: Number(e.target.value) })}/></label><label>Kiểu chơi<select value={editing.mechanic} onChange={e => setEditing({ ...editing, mechanic: e.target.value })}>{Object.entries(mechanics).map(([key, title]) => <option value={key} key={key}>{title}</option>)}</select></label><label className="check-label"><input type="checkbox" checked={editing.isPublished} onChange={e => setEditing({ ...editing, isPublished: e.target.checked })}/> Xuất bản</label><button className="primary" disabled={saving}>Lưu nội dung</button><button type="button" className="text-button" onClick={() => setEditing(undefined)}>Hủy</button></form></div>}</main>;
}



