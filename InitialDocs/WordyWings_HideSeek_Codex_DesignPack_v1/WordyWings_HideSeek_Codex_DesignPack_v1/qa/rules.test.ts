import type { HideSeekLevel, HideSeekState, VocabularyEntity } from '../engineering/contracts';
import { createState, startReveal, finishReveal, submitAnswer, acknowledgeFeedback,
  setPaused, independentCorrectCount } from '../engineering/rules';

function assert(ok: unknown, message = 'Assertion failed'): asserts ok { if (!ok) throw new Error(message); }
function equal<T>(a: T, b: T): void { assert(a === b, `Expected ${String(b)}, got ${String(a)}`); }
function throws(fn: () => unknown): void { let did = false; try { fn(); } catch { did = true; } assert(did); }
let passed = 0;
function test(name: string, fn: () => void): void { fn(); passed++; console.log(`PASS ${String(passed).padStart(2,'0')} ${name}`); }
const catalog: VocabularyEntity[] = ['cat','dog','tiger','scissors'].map(id => ({
  id, label: id, grammar: id === 'scissors' ? 'plural' : 'singular', assetKey: 'object.'+id,
  quest: id === 'scissors' ? 'Find the scissors.' : `Find a ${id}.`,
  question: id === 'scissors' ? 'Are these scissors?' : `Is this a ${id}?`,
  affirmative: id === 'scissors' ? 'Yes! These are scissors.' : `Yes! It is a ${id}.`,
  negativeIdentification: id === 'scissors' ? 'No. These are scissors.' : `No. It is a ${id}.`
}));
const level: HideSeekLevel = {
  id:'HS-TEST', mechanic:'hide_seek', contentVersion:'1.0.0', targetEntityId:'cat',difficulty:1,
  initialStars:3,shuffleOccupants:false,tools:['swipe_leaves','throw_net','blast_berry'],
  spots:catalog.map((e,i)=>({id:e.id,kind:'bush',entityId:e.id,allowedTools:['swipe_leaves'],
    anchor:{x:200+i*220,y:300,width:200,height:200},coverAssetKey:'cover.bush'}))
};
const fresh = () => createState(level, catalog);
function asking(spot = 'cat', s: HideSeekState = fresh(), r = 'r-'+spot): HideSeekState {
  return finishReveal(startReveal(s,level,spot,'swipe_leaves',r),r);
}
function answer(spot: string, yes: boolean, s: HideSeekState = fresh(), event = 'a-'+spot): HideSeekState {
  return submitAnswer(asking(spot,s),level,catalog,yes,event);
}
function resolve(spot: string, yes: boolean, s: HideSeekState = fresh()): HideSeekState {
  return acknowledgeFeedback(answer(spot,yes,s));
}

test('Initial state: searching and three stars',()=>{ const s=fresh();equal(s.phase,'searching');equal(s.stars,3); });
test('Initial occupants are not revealed',()=>equal(fresh().revealedSpotIds.length,0));
test('Target YES completes after feedback',()=>{const s=answer('cat',true);equal(s.phase,'feedback');equal(s.feedback?.nextPhase,'completed');equal(acknowledgeFeedback(s).phase,'completed');});
test('Target NO costs one star and requests assisted retry',()=>{const s=answer('cat',false);equal(s.stars,2);equal(acknowledgeFeedback(s).phase,'assisted_retry');});
test('Distractor NO keeps stars and continues search',()=>{const s=resolve('dog',false);equal(s.stars,3);equal(s.phase,'searching');});
test('Distractor YES costs a star and continues search',()=>{const s=resolve('dog',true);equal(s.stars,2);equal(s.phase,'searching');});
test('Correct NO never completes the level',()=>equal(answer('tiger',false).feedback?.nextPhase,'searching'));
test('Invalid tool has no side effect',()=>{const s=fresh();equal(startReveal(s,level,'cat','blast_berry','r'),s);});
test('Unknown spot has no side effect',()=>{const s=fresh();equal(startReveal(s,level,'missing','swipe_leaves','r'),s);});
test('Object remains hidden while cover is revealing',()=>{const s=startReveal(fresh(),level,'cat','swipe_leaves','r');equal(s.phase,'revealing');equal(s.revealedSpotIds.length,0);});
test('Reveal completion exposes exactly one occupant',()=>equal(asking().revealedSpotIds.length,1));
test('Stale reveal callback is ignored',()=>{const s=startReveal(fresh(),level,'cat','swipe_leaves','r');equal(finishReveal(s,'old'),s);});
test('Cannot submit answer before reveal completes',()=>{const s=startReveal(fresh(),level,'cat','swipe_leaves','r');equal(submitAnswer(s,level,catalog,true,'e'),s);});
test('Cannot start another reveal while a quiz is open',()=>{const s=asking();equal(startReveal(s,level,'dog','swipe_leaves','r2'),s);});
test('Answer double tap in feedback is ignored',()=>{const s=answer('dog',true);equal(submitAnswer(s,level,catalog,true,'second'),s);equal(s.stars,2);});
test('Duplicate answer event across different quizzes is ignored',()=>{const old=resolve('dog',false);const s=asking('cat',old);equal(submitAnswer(s,level,catalog,true,'a-dog'),s);});
test('Resolved distractor cannot be reopened for farming',()=>{const s=resolve('dog',false);equal(startReveal(s,level,'dog','swipe_leaves','new'),s);});
test('Repeated assisted NO never deducts another star',()=>{let s=resolve('cat',false);s=acknowledgeFeedback(submitAnswer(s,level,catalog,false,'retry-no'));equal(s.stars,2);equal(s.phase,'assisted_retry');});
test('Assisted YES completes while retaining penalty',()=>{const s=submitAnswer(resolve('cat',false),level,catalog,true,'retry-yes');equal(s.stars,2);equal(acknowledgeFeedback(s).phase,'completed');});
test('Assisted success is not independent learning evidence',()=>{const s=submitAnswer(resolve('cat',false),level,catalog,true,'retry-yes');equal(independentCorrectCount(s),0);equal(s.attempts[1].assisted,true);});
test('Three wrong distractors clamp stars at zero',()=>{let s=fresh();for(const id of ['dog','tiger','scissors'])s=resolve(id,true,s);equal(s.stars,0);equal(s.phase,'searching');});
test('Target can still be completed at zero stars',()=>{let s=fresh();for(const id of ['dog','tiger','scissors'])s=resolve(id,true,s);s=resolve('cat',true,s);equal(s.phase,'completed');equal(s.stars,0);});
test('Wrong answer at zero cannot create negative stars',()=>{let s=fresh();for(const id of ['dog','tiger','scissors'])s=resolve(id,true,s);s=answer('cat',false,s);equal(s.stars,0);equal(s.feedback?.deltaStars,0);});
test('Pause preserves and blocks a pending question',()=>{const s=setPaused(asking(),true);equal(submitAnswer(s,level,catalog,true,'x'),s);equal(s.pending?.spotId,'cat');});
test('Pause blocks a reveal completion callback',()=>{const s=setPaused(startReveal(fresh(),level,'cat','swipe_leaves','r'),true);equal(finishReveal(s,'r'),s);});
test('Resume accepts the original pending question',()=>{const s=setPaused(setPaused(asking(),true),false);equal(submitAnswer(s,level,catalog,true,'x').feedback?.nextPhase,'completed');});
test('Rules do not mutate original state',()=>{const s=asking('dog');submitAnswer(s,level,catalog,true,'x');equal(s.stars,3);equal(s.processedAnswerIds.length,0);});
test('Scissors feedback uses plural grammar',()=>equal(answer('scissors',false).feedback?.phrase,'No. These are scissors.'));
test('Completed state rejects new actions',()=>{const s=resolve('cat',true);equal(startReveal(s,level,'dog','swipe_leaves','x'),s);equal(submitAnswer(s,level,catalog,false,'x'),s);});
test('Reject missing target entity',()=>throws(()=>createState({...level,targetEntityId:'unknown'},catalog)));
test('Reject two copies of the target',()=>throws(()=>createState({...level,spots:[...level.spots,{...level.spots[0],id:'cat2'}]},catalog)));
test('Reject duplicate spot ids',()=>throws(()=>createState({...level,spots:[level.spots[0],{...level.spots[1],id:'cat'}]},catalog)));
test('Reject unreachable spot',()=>throws(()=>createState({...level,spots:level.spots.map(s=>({...s,allowedTools:[]}))},catalog)));
test('Reject duplicate vocabulary ids',()=>throws(()=>createState(level,[...catalog,catalog[0]])));
test('Empty reveal id cannot start a reveal',()=>{const s=fresh();equal(startReveal(s,level,'cat','swipe_leaves',''),s);});
test('Empty answer event id cannot submit a decision',()=>{const s=asking();equal(submitAnswer(s,level,catalog,true,''),s);});
test('Acknowledge feedback is idempotent once searching',()=>{const s=resolve('dog',false);equal(acknowledgeFeedback(s),s);});
test('Target identity is data driven, not hard coded to cat',()=>{const l={...level,targetEntityId:'dog'};let s=createState(l,catalog);s=startReveal(s,l,'dog','swipe_leaves','rd');s=finishReveal(s,'rd');s=submitAnswer(s,l,catalog,true,'ad');equal(s.feedback?.nextPhase,'completed');});
test('A used reveal id cannot be reused at another spot',()=>{const s=resolve('dog',false);equal(startReveal(s,level,'cat','swipe_leaves','r-dog'),s);});
test('Completion preserves the remaining star value',()=>{let s=resolve('dog',true);s=resolve('cat',true,s);equal(s.stars,2);equal(s.phase,'completed');});
console.log(`\n${passed} / ${passed} RULE TESTS PASSED. No Phaser rendering or device tests are included in this result.`);
