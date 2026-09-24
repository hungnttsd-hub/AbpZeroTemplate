import json, csv, os, textwrap, itertools, zipfile
from pathlib import Path

ROOT = Path('/mnt/data/wordy_wings_gdd')

mechanics = [
    {
        'id':'word_shot','name':'Word Shot','core':'Nghe/đọc yêu cầu rồi kéo-thả ná để bắn đúng mục tiêu.',
        'skills':['listening','vocabulary','visual discrimination'],
        'fail':'Bắn sai: mục tiêu đọc tên của nó, rung nhẹ; không mất mạng; cho thử lại.',
        'mvp':True
    },
    {
        'id':'balloon_pop','name':'Balloon Pop','core':'Chạm/bắn bong bóng mang hình hoặc từ đúng trước khi chúng trôi khỏi vùng chơi.',
        'skills':['listening','rapid recognition','attention'],
        'fail':'Chọn sai: bong bóng xẹp vui nhộn, đọc từ đúng của vật vừa chọn; câu hỏi được lặp lại.',
        'mvp':True
    },
    {
        'id':'drag_sort','name':'Drag & Sort','core':'Kéo đồ vật vào giỏ/nhóm/địa điểm phù hợp.',
        'skills':['categorisation','prepositions','vocabulary'],
        'fail':'Sai nhóm: vật bật trở lại vị trí cũ và có gợi ý bằng animation.',
        'mvp':True
    },
    {
        'id':'letter_puzzle','name':'Letter Puzzle','core':'Ghép chữ cái hoặc âm tiết thành từ mục tiêu; có audio mẫu.',
        'skills':['spelling','letter recognition','phonological awareness'],
        'fail':'Chữ sai chỉ rung, không khóa tiến trình; sau 2 lần sai highlight chữ đầu.',
        'mvp':True
    },
    {
        'id':'rescue_mission','name':'Rescue Mission','core':'Tìm đúng nhân vật/đồ vật qua mô tả rồi giải cứu bằng chạm, đường đi hoặc thao tác vật lý.',
        'skills':['comprehension','adjectives','simple sentences'],
        'fail':'Sai: narrator mô tả lại clue ngắn hơn, thêm highlight môi trường.',
        'mvp':False
    },
    {
        'id':'adventure_commands','name':'Adventure Commands','core':'Điều khiển nhân vật theo lệnh: go left, jump, open, put in/on/under...',
        'skills':['commands','directions','prepositions'],
        'fail':'Lệnh sai tạo animation vui, sau đó phát lại câu lệnh chậm hơn.',
        'mvp':False
    },
    {
        'id':'boss_challenge','name':'Boss Challenge','core':'Chuỗi 4-6 thử thách tổng hợp cuối world, không có combat bạo lực; mục tiêu là lấy lại Word Star.',
        'skills':['review','mixed comprehension','recall'],
        'fail':'Checkpoint theo từng câu; sai không reset toàn boss; hint tăng dần.',
        'mvp':True
    }
]

worlds = [
    {'id':'W01','slug':'rainbow-valley','name':'Rainbow Valley','vi':'Thung lũng Cầu Vồng','theme':'colors, shapes, size, basic objects','hero':'Pip','palette':'sunny sky, rainbow, grass, crystals','goal':'Khôi phục 7 sắc màu bị Mr. Mumble hút khỏi cầu vồng.'},
    {'id':'W02','slug':'animal-island','name':'Animal Island','vi':'Đảo Muông Thú','theme':'animals, simple descriptions, habitats','hero':'Poki','palette':'jungle, waterfalls, bamboo, river','goal':'Giải cứu các bạn thú bị giấu nhãn tên trong các chiếc hộp mây.'},
    {'id':'W03','slug':'happy-home','name':'Happy Home','vi':'Ngôi Nhà Vui Vẻ','theme':'rooms, furniture, toys, prepositions','hero':'Momo','palette':'cozy home, garden, warm wood, pastel textiles','goal':'Giúp Momo sắp xếp lại ngôi nhà sau cơn gió làm đồ vật bay khắp nơi.'},
    {'id':'W04','slug':'yummy-land','name':'Yummy Land','vi':'Xứ Sở Ngon Lành','theme':'food, drink, likes, quantities','hero':'Momo','palette':'fruit orchards, bakery, milk river, picnic','goal':'Nấu bữa tiệc lớn để đánh thức Word Tree đang đói.'},
    {'id':'W05','slug':'magic-school','name':'Magic School','vi':'Trường Học Phép Thuật','theme':'school objects, classroom actions, letters','hero':'Lulu','palette':'treehouse school, books, pencils, art room','goal':'Sửa cỗ máy chữ cái của Lulu để mở thư viện Word Star.'},
    {'id':'W06','slug':'action-jungle','name':'Body & Action Jungle','vi':'Rừng Vận Động','theme':'body, clothes, actions, abilities','hero':'Foxy','palette':'jungle obstacle course, vines, bridges','goal':'Vượt giải hội thao rừng và thu hồi các Action Stars.'},
    {'id':'W07','slug':'adventure-city','name':'Adventure City','vi':'Thành Phố Phiêu Lưu','theme':'transport, places, directions, position','hero':'Foxy','palette':'toy city, canal, station, airport, roads','goal':'Tìm đường qua thành phố để giao 5 bưu kiện chứa Lost Words.'},
    {'id':'W08','slug':'weather-kingdom','name':'Weather Kingdom','vi':'Vương Quốc Thời Tiết','theme':'weather, clothes, hot/cold, day/night','hero':'Lulu','palette':'floating weather islands, windmills, clouds','goal':'Sửa Weather Wheel để mỗi vùng có lại thời tiết đúng.'},
    {'id':'W09','slug':'space-numbers','name':'Space Numbers','vi':'Không Gian Con Số','theme':'numbers 1-20, counting, quantity, comparisons','hero':'Pip','palette':'moon islands, rockets, stars, nebula','goal':'Thu thập 20 Number Stars để khởi động tên lửa trở về Story Castle.'},
    {'id':'W10','slug':'story-castle','name':'Story Castle','vi':'Lâu Đài Câu Chuyện','theme':'family, daily life, simple sentences, integrated review','hero':'All','palette':'storybook castle, floating pages, sunset, portals','goal':'Ghép lại cuốn Great Word Book và giúp Mr. Mumble hiểu rằng từ ngữ vui hơn khi được chia sẻ.'},
]

# Curated Pre-A1 vocabulary aligned to early learner / Starters-like topics; not a reproduction of a proprietary wordlist.
vocab_by_world = {
'W01': {
'colors':['red','blue','yellow','green','orange','pink','purple','black','white','brown','grey'],
'shapes':['circle','square','triangle','star','heart','line'],
'size_quality':['big','small','long','short','new','old','good','nice','clean','dirty'],
'objects':['ball','box','kite','toy','door','book','flower']},
'W02': {
'animals':['cat','dog','bird','fish','duck','chicken','cow','horse','sheep','goat','mouse','rabbit','frog','monkey','tiger','lion','elephant','giraffe','crocodile','snake','spider','bee','butterfly','bear'],
'features':['tail','wing','leg','eye','ear','mouth'],
'qualities':['young','fast','slow','happy','angry','funny']},
'W03': {
'rooms':['house','home','bedroom','bathroom','kitchen','living room','garden'],
'furniture':['bed','chair','table','sofa','lamp','clock','mirror','cupboard','shelf','door','window'],
'home_objects':['toy','doll','teddy','picture','radio','television','phone','computer','box'],
'prepositions':['in','on','under','behind','next to']},
'W04': {
'fruit':['apple','banana','orange','lemon','lime','pear','grape','watermelon','mango','pineapple','strawberry'],
'food':['bread','cake','egg','rice','meat','chicken','fish','cheese','burger','sandwich','ice cream','soup'],
'drink':['water','milk','juice'],
'meal_words':['breakfast','lunch','dinner','hungry','thirsty','like','want']},
'W05': {
'school':['school','classroom','teacher','student','friend','desk','board','book','notebook','pen','pencil','ruler','eraser','bag','paper','picture','question','answer'],
'actions':['read','write','draw','colour','listen','look','open','close','sit','stand','spell','say']},
'W06': {
'body':['head','hair','face','eye','ear','nose','mouth','tooth','neck','arm','hand','finger','leg','foot'],
'actions':['run','walk','jump','hop','swim','dance','sing','clap','wave','sleep','eat','drink','play','catch','throw'],
'clothes':['shirt','T-shirt','dress','skirt','trousers','shorts','shoe','sock','hat','jacket'],
'ability':['can','cannot']},
'W07': {
'transport':['car','bus','bike','train','plane','boat','ship','helicopter'],
'places':['street','road','park','shop','school','hospital','station','airport','zoo','beach'],
'directions':['left','right','up','down','straight','here','there'],
'position':['near','between','in front of','behind','next to']},
'W08': {
'weather':['sun','sunny','rain','rainy','cloud','cloudy','wind','windy','snow','hot','cold','warm'],
'day_time':['day','night','morning','afternoon','evening','today'],
'weather_items':['umbrella','coat','boots','hat','glasses'],
'nature':['sky','tree','flower','river','sea']},
'W09': {
'numbers':['one','two','three','four','five','six','seven','eight','nine','ten','eleven','twelve','thirteen','fourteen','fifteen','sixteen','seventeen','eighteen','nineteen','twenty'],
'quantity':['many','more','less','all','some','one','two'],
'comparison':['big','small','long','short','same','different'],
'space':['moon','star','planet','rocket','sky']},
'W10': {
'family':['family','mother','father','mum','dad','brother','sister','baby','grandmother','grandfather'],
'people':['boy','girl','child','children','friend','man','woman'],
'routine':['wake up','get up','wash','eat','go','come','play','read','sleep'],
'question_words':['what','where','who','how','which'],
'function':['this','that','these','those','my','your','his','her','is','are','has','have','and','but','yes','no','please','thank you']},
}

# Flatten vocabulary preserving source/world and dedupe across worlds.
seen = {}
for wid, groups in vocab_by_world.items():
    for cat, words in groups.items():
        for word in words:
            key = word.lower()
            if key not in seen:
                seen[key] = {'term':word,'world':wid,'category':cat,'part_of_speech':'','notes':''}
            else:
                seen[key]['notes'] = (seen[key]['notes'] + '; review in '+wid).strip('; ')

# Add POS heuristics
for item in seen.values():
    t=item['term']
    if t in ['red','blue','yellow','green','orange','pink','purple','black','white','brown','grey','big','small','long','short','new','old','good','nice','clean','dirty','young','fast','slow','happy','angry','funny','hungry','thirsty','sunny','rainy','cloudy','windy','hot','cold','warm','same','different']:
        item['part_of_speech']='adjective'
    elif t in ['in','on','under','behind','next to','near','between','in front of']:
        item['part_of_speech']='preposition'
    elif t in ['read','write','draw','colour','listen','look','open','close','sit','stand','spell','say','run','walk','jump','hop','swim','dance','sing','clap','wave','sleep','eat','drink','play','catch','throw','like','want','wake up','get up','wash','go','come']:
        item['part_of_speech']='verb'
    elif t in ['what','where','who','how','which']:
        item['part_of_speech']='question word'
    elif t in ['this','that','these','those','my','your','his','her','is','are','has','have','and','but','yes','no','please','thank you','can','cannot']:
        item['part_of_speech']='function word'
    else:
        item['part_of_speech']='noun'

vocab = list(seen.values())

# World target pools for level generation
pools = {
'W01':['red','blue','yellow','green','orange','pink','purple','circle','square','triangle','star','big','small','ball','box','kite','flower','black','white','brown'],
'W02':['cat','dog','bird','fish','duck','cow','horse','rabbit','frog','monkey','tiger','lion','elephant','giraffe','crocodile','snake','bee','butterfly','bear','mouse'],
'W03':['bed','chair','table','sofa','lamp','clock','mirror','door','window','toy','doll','teddy','book','box','kitchen','bedroom','bathroom','garden','in','under'],
'W04':['apple','banana','orange','pear','grape','watermelon','mango','pineapple','strawberry','bread','cake','egg','rice','cheese','sandwich','ice cream','water','milk','juice','hungry'],
'W05':['teacher','student','desk','board','book','notebook','pen','pencil','ruler','eraser','bag','paper','read','write','draw','colour','listen','open','close','spell'],
'W06':['head','hair','eye','ear','nose','mouth','arm','hand','finger','leg','foot','run','walk','jump','swim','dance','clap','wave','shirt','hat'],
'W07':['car','bus','bike','train','plane','boat','street','park','shop','station','airport','left','right','up','down','straight','near','between','behind','zoo'],
'W08':['sun','sunny','rain','rainy','cloud','cloudy','wind','windy','snow','hot','cold','warm','day','night','morning','umbrella','coat','boots','sky','tree'],
'W09':['one','two','three','four','five','six','seven','eight','nine','ten','eleven','twelve','thirteen','fourteen','fifteen','sixteen','seventeen','eighteen','nineteen','twenty'],
'W10':['mother','father','brother','sister','baby','friend','boy','girl','wake up','wash','eat','go','play','read','sleep','what','where','who','my','your']
}

mechanic_cycle = ['word_shot','balloon_pop','drag_sort','word_shot','letter_puzzle','word_shot','drag_sort','balloon_pop','rescue_mission','boss_challenge',
                  'word_shot','adventure_commands','drag_sort','letter_puzzle','rescue_mission','balloon_pop','word_shot','adventure_commands','rescue_mission','boss_challenge']

def make_instruction(wid, idx, target, mechanic):
    num = idx+1
    if mechanic == 'word_shot':
        return f'Find the {target}.' if wid not in ['W06','W07','W09'] else (f'Find {target}.' if wid!='W09' else f'Find number {target}.')
    if mechanic == 'balloon_pop':
        return f'Pop the {target}.' if wid!='W09' else f'Pop {target}.'
    if mechanic == 'drag_sort':
        if wid=='W03': return f'Put the {target} in the right place.'
        if wid=='W04': return f'Put the {target} in the food basket.'
        if wid=='W05': return f'Put the {target} in the school bag.'
        if wid=='W06': return f'Put {target} with the matching picture.'
        if wid=='W07': return f'Put the {target} on the correct route.'
        if wid=='W08': return f'Put the {target} with the right weather.'
        if wid=='W09': return f'Put {target} stars in the box.'
        return f'Put the {target} in the right group.'
    if mechanic == 'letter_puzzle':
        return f'Build the word {target.upper()}.'
    if mechanic == 'rescue_mission':
        return f'Rescue the {target}.' if wid not in ['W06','W07','W08','W09','W10'] else f'Find and help: {target}.'
    if mechanic == 'adventure_commands':
        if wid=='W07': return f'Go {target}.' if target in ['left','right','up','down','straight'] else f'Go to the {target}.'
        if wid=='W06': return f'{target.capitalize()}!'
        if wid=='W03': return f'Go to the {target}.'
        if wid=='W08': return f'Go to the {target} place.'
        if wid=='W10': return f'{target.capitalize()} and continue.'
        return f'Go to {target}.'
    if mechanic == 'boss_challenge':
        return f'Boss review: find {target} and finish the Word Star challenge.'
    return f'Find {target}.'

levels=[]
for wi,w in enumerate(worlds):
    pool=pools[w['id']]
    for i in range(20):
        target=pool[i % len(pool)]
        mech=mechanic_cycle[i]
        # For early MVP-friendly first 10 levels of first 3 worlds, avoid non-MVP mechanic except boss at L10.
        if wi < 3 and i < 10 and mech in ['rescue_mission','adventure_commands']:
            mech = 'word_shot' if i % 2 == 0 else 'drag_sort'
        # Compose distractors by rotating pool
        distractors=[]
        j=1
        while len(distractors)<3:
            c=pool[(i+j*3)%len(pool)]
            if c!=target and c not in distractors:
                distractors.append(c)
            j+=1
        difficulty = 1 if i < 5 else 2 if i < 10 else 3 if i < 15 else 4
        level = {
            'id':f"{w['id']}-L{i+1:02d}",
            'worldId':w['id'],
            'order':i+1,
            'title':f"{w['name']} {i+1}",
            'mechanic':mech,
            'difficulty':difficulty,
            'targetVocabulary':[target],
            'reviewVocabulary':distractors[:2],
            'instruction':make_instruction(w['id'],i,target,mech),
            'instructionAudioKey':f"audio/{w['slug']}/{i+1:02d}_instruction.mp3",
            'targets':[{'value':target,'correct':True}] + [{'value':d,'correct':False} for d in distractors],
            'stars':{
                '1':'complete with hints allowed',
                '2':'complete with <= 1 wrong attempt',
                '3':'complete first try without hint'
            },
            'hintPolicy':{'afterWrongAttempts':2,'audioReplay':True,'visualPulse':True},
            'estimatedSeconds':45 if mech not in ['boss_challenge','rescue_mission'] else 90,
            'learningObjective':f"Recognise and understand '{target}' in a short spoken/visual task.",
            'reward':{'wordStars':3 if mech=='boss_challenge' else 1,'coins':0},
            'isBoss':mech=='boss_challenge'
        }
        # enrich some advanced levels with phrase objectives
        if i>=10:
            if w['id']=='W01' and target in ['red','blue','yellow','green','orange','pink','purple','black','white','brown']:
                level['instruction']=f'Find the big {target} object.'
                level['learningObjective']=f"Combine size + color: understand 'big {target}'."
            elif w['id']=='W02':
                level['instruction']=f'Find the {target} near the tree.'
                level['learningObjective']=f"Recognise animal '{target}' inside a short location phrase."
            elif w['id']=='W03':
                level['instruction']=f'Put the toy {"on" if i%2==0 else "under"} the {target}.' if target not in ['in','under'] else f'Find what is {target} the table.'
            elif w['id']=='W04':
                level['instruction']=f'Momo wants {target}. Give it to Momo.'
            elif w['id']=='W05':
                level['instruction']=f'Listen and choose: {target}.'
            elif w['id']=='W06':
                level['instruction']=f'Show me: {target}.' if target in ['run','walk','jump','swim','dance','clap','wave'] else f'Touch your {target}.'
            elif w['id']=='W07':
                level['instruction']=f'Go to the {target}, then stop.'
            elif w['id']=='W08':
                level['instruction']=f'It is {target}. Choose the right place or item.'
            elif w['id']=='W09':
                level['instruction']=f'Count and choose {target} stars.'
            elif w['id']=='W10':
                level['instruction']=f'Listen to the mini story and choose: {target}.'
                level['learningObjective']='Understand a 1-2 sentence micro-story and retrieve a familiar word.'
        levels.append(level)

mvp_ids = {f'W01-L{i:02d}' for i in range(1,11)} | {f'W02-L{i:02d}' for i in range(1,11)} | {f'W03-L{i:02d}' for i in range(1,11)}
mvp_levels = [l for l in levels if l['id'] in mvp_ids]

# ---------- README ----------
readme = """# Wordy Wings – Game Design & MVP Build Pack

Bộ tài liệu này dùng để thiết kế và triển khai game học tiếng Anh cho trẻ khoảng 6 tuổi theo mô hình **play-first, learn-naturally**.

## Deliverables

- `01_GDD.md` – Game Design Document tổng thể.
- `02_TECH_ARCHITECTURE.md` – Kiến trúc chốt: ABP.io + React + Phaser + PostgreSQL + Capacitor.
- `03_CODEX_MVP_SPEC.md` – đặc tả triển khai MVP 30 màn.
- `04_CODEX_MASTER_PROMPT.md` – prompt có thể dán thẳng vào Codex.
- `05_CONTENT_GUIDE.md` – nguyên tắc nội dung cho trẻ 6 tuổi.
- `data/worlds.json` – dữ liệu 10 world.
- `data/levels_200.json` / `.csv` – 200 level seed-ready.
- `data/mvp_levels_30.json` – 30 màn MVP.
- `data/vocabulary_pre_a1.json` / `.csv` – vocabulary Pre-A1 tuyển chọn theo chủ đề.
- `schemas/*.json` – JSON schema cho level/world/progress.
- `backend/abp_domain_model.md` – domain/entity/API gợi ý cho ABP.
- `frontend/phaser_architecture.md` – cấu trúc React/Phaser.
- `deployment/render.md` – deploy Render + PostgreSQL + object storage.
- `qa/*.md` – acceptance criteria và test matrix.
- `prompts/content_generation_prompts.md` – prompt sinh thêm nội dung/asset/audio về sau.

## Chốt sản phẩm

- Trẻ chơi bằng nghe/nhìn/chạm/kéo/bắn; không phụ thuộc vào đọc hướng dẫn dài.
- Không có lives, energy, loot box, pay-to-win hoặc punishment khi trả lời sai.
- Một level thường 30–90 giây; một session khuyến nghị 10–15 phút.
- Sai -> feedback nhẹ + nghe lại + hint tăng dần.
- Backend API-first để web và app mobile dùng chung.
- Web: React + Phaser. Mobile sau này: Capacitor, giữ nguyên game core và ABP backend.

## Curriculum note

Danh sách vocabulary trong pack là **bộ Pre-A1 tuyển chọn riêng**, được tổ chức theo các chủ đề early learner/Pre A1 Starters phổ biến; không phải bản sao toàn bộ wordlist chính thức của Cambridge. Tham khảo nguồn chính thức: Cambridge English – Pre A1 Starters preparation và Wordlist Picture Book.
"""
(ROOT/'README.md').write_text(readme,encoding='utf-8')

# ---------- GDD ----------
world_sections=[]
for w in worlds:
    wl=[l for l in levels if l['worldId']==w['id']]
    target_terms=[]
    for l in wl:
        for t in l['targetVocabulary']:
            if t not in target_terms: target_terms.append(t)
    lines=[f"### {w['id']} — {w['name']} ({w['vi']})",
           f"- **Theme học:** {w['theme']}",
           f"- **Nhân vật dẫn:** {w['hero']}",
           f"- **Mỹ thuật:** {w['palette']}",
           f"- **Story goal:** {w['goal']}",
           f"- **Từ khóa trọng tâm:** {', '.join(target_terms[:20])}",
           "- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.",
           ""]
    world_sections.append('\n'.join(lines))

gdd = f"""# GAME DESIGN DOCUMENT — WORDY WINGS

**Tên tạm:** Wordy Wings – Đảo Tiếng Anh Kỳ Diệu  
**Thể loại:** 2D/2.5D educational adventure game  
**Đối tượng chính:** trẻ khoảng 5–7 tuổi; MVP tối ưu cho 6 tuổi  
**Nền tảng:** web trước, Android/iOS sau  
**Core principle:** *Chơi trước – học tự nhiên sau.*

## 1. Product vision

Wordy Wings là game phiêu lưu học tiếng Anh, trong đó kiến thức không xuất hiện như một bài kiểm tra tách biệt mà trở thành luật chơi. Trẻ nghe một từ/câu ngắn, quan sát thế giới, sau đó kéo, chạm, bắn, xếp hoặc điều khiển nhân vật để hoàn thành nhiệm vụ. Mỗi lần hoàn thành, game phát lại từ/câu đúng, animation ăn mừng và trao Word Star.

### North-star experience

Một bé 6 tuổi chưa đọc tiếng Anh tốt vẫn có thể tự hiểu cách chơi nhờ voice, animation bàn tay và visual cue. Sau 10–15 phút, bé phải cảm thấy mình vừa phiêu lưu, không phải vừa làm bài tập.

## 2. Pillars

1. **Visual first:** hình lớn, tương phản tốt, ít text, mỗi màn chỉ một mục tiêu chính.
2. **Audio first:** mọi instruction chính đều có voice; có nút nghe lại.
3. **Safe failure:** không mất mạng; sai là feedback học tập.
4. **Short loops:** level 30–90 giây; boss 2–3 phút.
5. **Spaced review:** từ cũ quay lại trong world sau và trong boss.
6. **Meaningful reward:** Word Stars, đồ trang trí, nhân vật/pet; không monetization gây áp lực.
7. **Parent visibility:** phụ huynh xem từ đã học, accuracy, thời lượng và nhóm từ cần ôn.

## 3. Story

Tại quần đảo Wordy Wings, mọi vật đều được giữ tên bởi các **Word Stars**. Mr. Mumble – một đám mây tím tinh nghịch – nghĩ rằng thế giới sẽ yên tĩnh hơn nếu ít từ hơn, nên hút các Word Stars vào những bong bóng mây và làm tên đồ vật biến mất.

Pip phát hiện mình nhìn thấy một quả táo nhưng không còn nhớ từ “apple”. Lulu sửa chiếc Word Compass, Poki mang ba lô thám hiểm, Foxy tìm các đường tắt còn Momo – rồng con hiền lành – vô tình hắt hơi làm lộ ra một Word Star. Cả nhóm bắt đầu đi qua 10 vùng đất để lấy lại các từ.

Ở Story Castle, đội bạn phát hiện Mr. Mumble không thực sự xấu; hắn không hiểu cách dùng từ để kết bạn. Final boss không phải đánh bại hắn mà là hoàn thành chuỗi câu chuyện, trả lại Great Word Book và mời Mr. Mumble tham gia đội. Ending mở ra English Garden, nơi mọi từ bé học được trở thành đồ vật sống trong khu vườn cá nhân.

## 4. Characters

- **Pip – yellow chick explorer:** năng lượng cao, hướng dẫn thao tác cơ bản, mascot của tutorial.
- **Poki – panda explorer:** tò mò, thân thiện, dẫn các màn khám phá và động vật.
- **Lulu – rabbit inventor:** thông minh, xây máy chữ, dẫn puzzle/spelling/school.
- **Foxy – playful fox:** nhanh nhẹn, tinh nghịch, dẫn action/direction/city.
- **Momo – baby dragon:** hiền, hơi vụng, mê đồ ăn, tạo cảm xúc và các mission chăm sóc.
- **Mr. Mumble – purple cloud:** phản diện vui nhộn, không đáng sợ; về cuối trở thành bạn.

## 5. Core loop

**Listen → Observe → Act → Feedback → Repeat word → Reward → Next micro-challenge**

- Instruction 1–5 giây.
- Trẻ thực hiện 1 hành động chính.
- Đúng: audio từ/câu + animation + Word Star.
- Sai: phát tên item vừa chọn, không phạt, cho thử lại.
- Sau 2 lần sai: visual hint; sau 3 lần: simplified clue.

## 6. Seven gameplay mechanics

""" + '\n'.join([f"### {i+1}. {m['name']} (`{m['id']}`)\n- **Core:** {m['core']}\n- **Kỹ năng:** {', '.join(m['skills'])}\n- **Sai:** {m['fail']}\n- **Có trong MVP:** {'Có' if m['mvp'] else 'Không'}\n" for i,m in enumerate(mechanics)]) + f"""

## 7. World map — 10 world / 200 level

{''.join(world_sections)}

## 8. Difficulty model

- **D1:** 2–3 lựa chọn, từ đơn, hình rất khác nhau.
- **D2:** 3–4 lựa chọn, distractor cùng category, thêm chữ viết lớn.
- **D3:** cụm 2–5 từ: `big red ball`, `under the table`, `three ducks`.
- **D4:** câu ngắn hoặc micro-story 1–2 câu, mixed review.

Không tăng khó bằng tốc độ quá nhanh. Với trẻ 6 tuổi, tăng khó chủ yếu bằng **ngôn ngữ + số lượng thông tin cần giữ**, không phải phản xạ.

## 9. 200-level content model

File `data/levels_200.json` chứa đủ 200 record. Mỗi level có:

- `id`, `worldId`, `order`, `title`
- `mechanic`
- `difficulty`
- `targetVocabulary`, `reviewVocabulary`
- `instruction`, `instructionAudioKey`
- `targets[]`
- `stars`, `hintPolicy`, `estimatedSeconds`
- `learningObjective`, `reward`, `isBoss`

Level phải data-driven; không tạo `Level1.ts ... Level200.ts`.

## 10. Vocabulary strategy

Bộ dữ liệu `data/vocabulary_pre_a1.*` là vocabulary Pre-A1 tuyển chọn, chia theo world/category. Quy tắc:

- Từ mới được giới thiệu bằng audio + hình trước khi yêu cầu spelling.
- Một từ mục tiêu phải xuất hiện tối thiểu 4 lần trong 2–4 tuần chơi: nhận diện, nghe câu, mixed review, boss/story.
- Không dùng bản dịch Việt trên gameplay mặc định; translation có thể nằm trong parent mode hoặc tap-and-hold trợ giúp.
- Ưu tiên British hoặc American pronunciation thống nhất; MVP chọn một accent duy nhất.

## 11. Reward & progression

### Word Stars

- Mỗi level thường cho 1 Word Star; boss 3.
- Rating 1–3 sao chỉ để khuyến khích mastery, không khóa cốt truyện nếu bé chỉ đạt 1 sao.

### Cosmetic rewards

- Mũ, balo, sticker, màu lều, cây/hoa, pet props.
- Không random loot box.

### My English Garden

Mỗi vocabulary mastered có thể mở một garden object. Chạm object sẽ nghe audio từ. Garden là nơi review tự do, không có fail state.

## 12. Session design

Session khuyến nghị 10–15 phút:

1. 1 warm-up review.
2. 3–4 level mới.
3. 1 mixed review.
4. 1 reward/garden moment.

Game có thể hiện lời chào kết thúc nhẹ nhàng sau ~15 phút thay vì thúc chơi vô hạn.

## 13. Parent mode

Phụ huynh có thể xem:

- total words introduced/mastered;
- accuracy 7 ngày / 30 ngày;
- words needing review;
- session time;
- category progress;
- pronunciation feature status nếu sau này bật microphone.

Parent area phải có parental gate (ví dụ giữ nút 3 giây hoặc câu hỏi người lớn đơn giản), không đặt link ngoài trong khu vực trẻ.

## 14. Child safety & privacy by design

- Child profile chỉ cần nickname, avatar, age band; tránh thu thập ngày sinh đầy đủ nếu không cần.
- Không quảng cáo hành vi, không chat công khai, không link ra web từ kid mode.
- Không ghi âm/upload giọng nói ở MVP.
- Nếu thêm pronunciation về sau: explicit parental consent, retention policy rõ ràng và thiết kế có thể chấm tại thiết bị khi khả thi.

## 15. Audio & localization

- `instructionAudioKey` trỏ đến audio asset; không hard-code URL.
- MVP có thể dùng dev fallback SpeechSynthesis nhưng production nên dùng audio được duyệt để chất lượng ổn định.
- UI architecture chuẩn bị i18n; kid gameplay ưu tiên English, parent/admin có Vietnamese.

## 16. Art direction

- Toy-like 2.5D, hình khối bo tròn, vật liệu mềm, ánh sáng sáng sủa.
- Không sao chép nhân vật/level/IP của game thương mại khác.
- Mỗi world có silhouette và palette khác nhau nhưng dùng chung material language.
- Target object phải dễ nhận ra trên màn 6–7 inch.

## 17. Camera & input

- Landscape 16:9 là layout gốc cho gameplay.
- Mobile/tablet ưu tiên touch; desktop hỗ trợ pointer/mouse.
- Hit target tối thiểu khoảng 44 CSS px; target gameplay chính nên lớn hơn đáng kể.
- Không yêu cầu multi-touch.

## 18. Success metrics cho MVP

- Bé có thể vào game và chơi level 1 mà không cần người lớn giải thích quá 30 giây.
- ≥80% thao tác tutorial hiểu được sau lần hướng dẫn đầu.
- 30 level chạy ổn trên Chrome desktop + Android Chrome.
- Save progress đúng khi reload/offline tạm thời.
- Average level duration 30–90 giây.
- Không có soft lock nếu audio lỗi hoặc asset thiếu.

## 19. MVP scope — 30 level

MVP dùng 3 world đầu, mỗi world 10 level:

- W01 Rainbow Valley L01–L10
- W02 Animal Island L01–L10
- W03 Happy Home L01–L10

Mechanic MVP: Word Shot, Balloon Pop, Drag & Sort, Letter Puzzle và Boss Challenge. `mvp_levels_30.json` là seed source of truth.

## 20. Out of scope MVP

- Speech recognition.
- Real-time AI.
- Multiplayer/social.
- Paid shop/subscription.
- Full English Garden.
- Push notification.
- Native store build.
- 200 level production assets.

## 21. Definition of done

MVP hoàn thành khi một phụ huynh có thể đăng nhập, tạo child profile, chọn child, vào map, chơi 30 level, nghe audio, nhận star, thoát app, quay lại đúng tiến trình; parent dashboard xem được số level hoàn thành và từ cần ôn. Backend/API không phụ thuộc web client để sau này Capacitor dùng lại.
"""
(ROOT/'01_GDD.md').write_text(gdd,encoding='utf-8')

# ---------- Architecture ----------
arch = """# TECH ARCHITECTURE — ABP.IO + React + Phaser + Capacitor

## 1. Chốt stack

| Layer | Technology |
|---|---|
| Backend | ABP.io / ASP.NET Core |
| API | ABP Application Services exposed as REST |
| Auth | ABP Identity + OpenIddict/OIDC |
| ORM | EF Core |
| Database | PostgreSQL |
| Web app | React + TypeScript |
| Game runtime | Phaser |
| Physics | Phaser Matter only where mechanic needs physics |
| Build | Use toolchain of current ABP React template; keep game package TypeScript-first |
| Assets | Cloudflare R2 / S3-compatible object storage + CDN |
| Web deploy | Render Static Site |
| API deploy | Render Web Service via Docker |
| Mobile later | Capacitor Android/iOS |
| Local/offline | IndexedDB on web; adapter can switch to SQLite-capable storage in Capacitor |

## 2. High-level topology

```text
React + Phaser (Web) ─────┐
                          ├── HTTPS/REST ──> ABP.IO API ──> PostgreSQL
Capacitor (Android/iOS) ──┘                       │
                                                 └──> R2/S3 asset metadata + signed/public CDN URLs
```

Backend is the source of truth for user/progress. Client maintains a local progress queue so play can continue through temporary connectivity loss.

## 3. Monorepo recommendation

```text
WordyWings/
  aspnet-core/
    src/
      WordyWings.Domain.Shared/
      WordyWings.Domain/
      WordyWings.Application.Contracts/
      WordyWings.Application/
      WordyWings.EntityFrameworkCore/
      WordyWings.HttpApi/
      WordyWings.HttpApi.Host/
      WordyWings.DbMigrator/
  react/
    src/
      app/
      features/parent/
      features/child-profile/
      features/world-map/
      game/
        bootstrap/
        scenes/
        mechanics/
        services/
        components/
        types/
      generated-api/
  content/
    worlds.json
    levels.json
    vocabulary.json
  mobile/
    # add later with Capacitor; do not fork game logic
```

## 4. Boundary rules

- React owns application shell, auth, parent screens, route transitions, settings.
- Phaser owns moment-to-moment gameplay, scene lifecycle, physics, game input and in-game feedback.
- React must not implement physics/game loop.
- Phaser must not own authentication or direct database logic.
- All content is data-driven from JSON/API.
- Client never decides mastery authoritatively; it submits attempts, backend calculates persistent mastery.

## 5. ABP modules / capabilities

Use built-in ABP facilities where practical:

- Identity / users / roles.
- OpenIddict authentication.
- Permission Management for admin/content roles.
- Setting Management for game/content flags.
- Blob metadata abstraction if useful, but binary assets stay in object storage/CDN.

Suggested roles:

- `Admin`
- `ContentEditor`
- `Parent`

Children are **ChildProfile entities owned by a Parent user**, not independent login accounts in MVP.

## 6. API design

```http
GET    /api/game/worlds
GET    /api/game/worlds/{worldId}/levels
GET    /api/game/levels/{levelId}
GET    /api/game/children
POST   /api/game/children
GET    /api/game/children/{childId}/progress
POST   /api/game/attempts
POST   /api/game/progress/sync
GET    /api/game/children/{childId}/review-queue
GET    /api/game/children/{childId}/dashboard
```

Attempt DTO minimum:

```json
{
  "attemptId": "client-generated-guid",
  "childId": "guid",
  "levelId": "W01-L01",
  "startedAt": "ISO-8601",
  "completedAt": "ISO-8601",
  "wrongAttempts": 1,
  "hintCount": 0,
  "stars": 2,
  "targetResults": [
    { "term": "red", "correct": true, "responseMs": 3200 }
  ]
}
```

`attemptId` makes sync idempotent.

## 7. Offline strategy

Client maintains:

- cached content manifest;
- cached level JSON;
- local progress snapshot;
- pending attempt queue.

On reconnect:

1. authenticate/refresh token;
2. POST pending attempts using idempotent IDs;
3. backend merges progress;
4. client pulls canonical progress.

MVP may implement browser IndexedDB adapter only, but interfaces must allow a Capacitor implementation later.

## 8. Content versioning

Every published level has `contentVersion` and `updatedAt`.

Client caches by version. A content change must never corrupt an in-progress session. Download new version on next level entry or next session.

## 9. Asset strategy

Store only metadata/keys in PostgreSQL:

```text
assets/worlds/w01/background.webp
assets/vocab/apple/card.webp
audio/en/apple.mp3
audio/en/w01/01_instruction.mp3
```

Use a configurable `AssetBaseUrl`. Never save production uploads to ephemeral Render filesystem.

## 10. Mobile migration

Capacitor wraps the same React + Phaser web build. Mobile work later is limited mainly to:

- native project bootstrap;
- secure token storage adapter;
- offline storage adapter;
- app icon/splash/store metadata;
- push notification if added;
- mobile deep link/OIDC redirect configuration.

No new game backend is required.
"""
(ROOT/'02_TECH_ARCHITECTURE.md').write_text(arch,encoding='utf-8')

# ---------- Codex MVP spec ----------
mvp_table='\n'.join([f"| {l['id']} | {l['mechanic']} | {l['instruction']} | {l['targetVocabulary'][0]} | D{l['difficulty']} |" for l in mvp_levels])

spec=f"""# CODEX MVP SPEC — 30 LEVELS

## Mission

Build a working MVP of **Wordy Wings** using the agreed architecture: **ABP.io + React + TypeScript + Phaser + PostgreSQL**, deployable to Render, with future **Capacitor** mobile reuse.

The MVP is not a mock. It must be playable end-to-end with placeholder/original SVG assets and audio fallbacks.

## Functional scope

### Parent / app shell

1. Register/login parent via ABP/OpenIddict.
2. Create/select one or more child profiles: nickname, avatarKey, ageBand.
3. World map showing three MVP worlds and locked/unlocked state.
4. Open a level.
5. Return from Phaser to map after completion.
6. Parent dashboard: levels completed, stars, vocabulary attempted, review list.

### Game

Implement these mechanics:

- `word_shot`
- `balloon_pop`
- `drag_sort`
- `letter_puzzle`
- `boss_challenge`

The other two mechanics must exist as TypeScript interface/registry placeholders only, not production gameplay in MVP.

### Progress

- Save attempt on level completion.
- Unlock next level in same world.
- Unlock next world after MVP boss of previous world.
- Reload app and preserve state.
- Queue attempt locally if request fails, retry later.

## UX rules

- Landscape gameplay baseline 16:9; responsive down to phone landscape.
- Every instruction has speaker replay control.
- No game-over/lives.
- Wrong answer: gentle bounce/shake + pronounce selected item, then retry.
- Hint after two wrong attempts.
- Main touch targets large; no critical hover-only action.
- Pause overlay provides Resume / Restart / Exit Level.

## MVP level list

| ID | Mechanic | Instruction | Target | Difficulty |
|---|---|---|---|---|
{mvp_table}

Source of truth: `data/mvp_levels_30.json`.

## Phaser architecture

```text
GameBootstrap
  SceneRegistry
    BootScene
    PreloadScene
    LevelScene
    ResultScene
  MechanicRegistry
    WordShotMechanic
    BalloonPopMechanic
    DragSortMechanic
    LetterPuzzleMechanic
    BossChallengeMechanic
  Services
    LevelContentService
    AudioService
    AssetService
    AttemptTracker
    HintService
    GameEventBus
```

`LevelScene` must not branch on world-specific IDs. It loads a `LevelDefinition` and delegates to a mechanic implementation.

## Required shared TypeScript contracts

```ts
export type MechanicId =
  | 'word_shot'
  | 'balloon_pop'
  | 'drag_sort'
  | 'letter_puzzle'
  | 'rescue_mission'
  | 'adventure_commands'
  | 'boss_challenge';

export interface LevelDefinition {{
  id: string;
  worldId: string;
  order: number;
  title: string;
  mechanic: MechanicId;
  difficulty: 1 | 2 | 3 | 4;
  targetVocabulary: string[];
  reviewVocabulary: string[];
  instruction: string;
  instructionAudioKey: string;
  targets: Array<{{ value: string; correct: boolean; assetKey?: string }}>;
  hintPolicy: {{ afterWrongAttempts: number; audioReplay: boolean; visualPulse: boolean }};
  estimatedSeconds: number;
  learningObjective: string;
  isBoss: boolean;
}}
```

## Placeholder art policy

MVP must not depend on external copyrighted game assets. Generate simple original SVG/shape assets for:

- fruit, shapes, basic animals, furniture;
- Poki/Pip/Momo simplified avatar tokens;
- world backgrounds as layered gradients + original vector decoration.

Create an asset mapping layer so production assets can replace placeholders without changing mechanics.

## Audio

Interface:

```ts
interface AudioService {{
  playInstruction(level: LevelDefinition): Promise<void>;
  speakWord(word: string): Promise<void>;
  stopAll(): void;
}}
```

Order:

1. Try packaged/cached audio asset.
2. If missing in dev/MVP, use browser `speechSynthesis` English voice.
3. Never block gameplay forever if audio fails.

## Backend entities

See `backend/abp_domain_model.md`. Minimum MVP entities:

- ChildProfile
- GameWorld
- GameLevel
- VocabularyTerm
- LevelAttempt
- WordMastery
- PlayerProgress

Content may initially be seeded from JSON into DB.

## MVP admin

Admin UI can be basic CRUD. Must support:

- world list;
- level list/filter by world;
- edit instruction, difficulty, mechanic, publish flag;
- validate a level before publish.

Do not build a visual level editor in MVP.

## Error handling

- Missing level -> friendly error + Back to Map.
- Missing target asset -> generic labeled placeholder.
- Missing audio -> speech synthesis fallback.
- API offline -> queue attempt locally.
- Expired auth -> preserve pending attempt, refresh/login, sync afterward.

## Testing

At minimum:

- unit test level schema parsing;
- unit test unlock calculation;
- unit test star calculation;
- unit test idempotent sync DTO mapping;
- Playwright/e2e: login -> select child -> play one level -> complete -> map shows next level unlocked;
- responsive smoke test desktop and mobile landscape viewport.

## Deliver in milestones

### M1 — Skeleton

ABP entities/API + React routes + Phaser canvas boot + load JSON level.

### M2 — Core mechanic

Word Shot fully playable, attempt save, stars, result screen.

### M3 — 4 more mechanics

Balloon Pop, Drag Sort, Letter Puzzle, Boss Challenge.

### M4 — 30 levels

Seed 30 definitions, world map/unlocking, dashboard.

### M5 — Offline/resilience

IndexedDB queue, retries, audio fallback, missing asset fallback.

### M6 — Deploy

Docker backend on Render, React static site on Render, PostgreSQL connection, environment configs.

## Acceptance

Use `qa/acceptance_criteria.md` as release gate. Do not declare MVP done while any P0 acceptance item fails.
"""
(ROOT/'03_CODEX_MVP_SPEC.md').write_text(spec,encoding='utf-8')

master_prompt="""# CODEX MASTER PROMPT

Copy/paste the prompt below into Codex from the repository root.

---

You are implementing the MVP of a children's English-learning adventure game named **Wordy Wings**.

Read these files before changing code:

1. `README.md`
2. `01_GDD.md`
3. `02_TECH_ARCHITECTURE.md`
4. `03_CODEX_MVP_SPEC.md`
5. `backend/abp_domain_model.md`
6. `frontend/phaser_architecture.md`
7. `qa/acceptance_criteria.md`
8. `data/mvp_levels_30.json`
9. `schemas/level.schema.json`

## Non-negotiable architecture

- Backend: ABP.io / ASP.NET Core / EF Core / PostgreSQL.
- Auth: ABP Identity + OpenIddict.
- Frontend: React + TypeScript.
- Gameplay: Phaser isolated behind a React `GameContainer`.
- Content: data-driven JSON/API; never hard-code individual levels in source files.
- Mobile future: preserve the React + Phaser game so it can be wrapped by Capacitor; do not introduce browser-hostile dependencies into game core without an adapter.
- Assets: use asset keys/base URL; never persist uploads to local Render filesystem.

## Product constraints

The player is around six years old. Gameplay must be understandable primarily by audio, pictures and animation. No lives, no game-over punishment, no loot boxes, no ads in child mode. Wrong answers should teach and allow immediate retry.

## First task

Inspect the existing repository and adapt to its current ABP version/template. Do not overwrite existing infrastructure blindly. Then produce a short implementation checklist and start implementing M1 immediately.

If the repository is empty, scaffold the current supported ABP React solution using the installed/current ABP tooling. Do not guess obsolete CLI flags; check local help/tooling. Keep the generated ABP project boundaries intact.

## Implementation order

1. Domain entities and EF mappings for MVP.
2. Database migration and seed from `data/mvp_levels_30.json`.
3. Application contracts/services and REST endpoints.
4. React child selection + world map routes.
5. Phaser bootstrap + `LevelDefinition` parsing.
6. Implement `word_shot` first end-to-end.
7. Attempt persistence + star/unlock logic.
8. Implement remaining MVP mechanics.
9. Local IndexedDB pending-attempt queue.
10. Parent dashboard.
11. Automated tests.
12. Render deployment files/config documentation.

## Code quality

- TypeScript strict mode.
- Small testable services; avoid giant Phaser scenes.
- No `any` in shared game contracts unless unavoidable and documented.
- Validate level JSON at runtime before launching a scene.
- All network calls behind a service/repository abstraction.
- All storage behind an adapter interface.
- Make sync idempotent using client-generated attempt IDs.
- Log technical detail server-side, show child-friendly errors in game.

## Definition of done

A parent can authenticate, create/select a child, enter World 1, play all MVP mechanics, complete levels, see stars and unlocks persist, close/reopen and retain progress, and view a basic dashboard. Temporary API failure must not erase a completed attempt; it should sync later.

Work iteratively and keep the application runnable after each milestone. Never claim completion without running the relevant tests/builds.
---
"""
(ROOT/'04_CODEX_MASTER_PROMPT.md').write_text(master_prompt,encoding='utf-8')

content_guide="""# CONTENT GUIDE — AGE ~6 / PRE-A1

## Language rules

- One new concept per early level.
- Instruction target: usually 2–6 English words in Worlds 1–5.
- Avoid idiom, sarcasm, phrasal verbs beyond very common routines.
- Prefer concrete nouns and actions that can be pictured.
- Keep negative sentences rare in early worlds.
- Introduce plural/quantity after singular recognition is secure.
- Use consistent wording for a mechanic before introducing synonyms.

## Feedback language

Correct examples:

- “Apple! Great job!”
- “Yes! It’s a dog.”
- “Three stars. Well done!”

Wrong-choice examples:

- “Banana. Try again. Find the apple.”
- “That’s blue. Find red.”

Avoid:

- “Wrong!”
- “You failed.”
- countdown pressure for basic learning tasks.

## Repetition model

For each core word:

1. **Exposure:** hear + see.
2. **Recognition:** choose picture from 2–3.
3. **Discrimination:** choose among same-category distractors.
4. **Phrase:** target inside short phrase.
5. **Recall/spelling:** only after several exposures.
6. **Review:** reappear in later world/boss/story.

## Visual distractors

Early levels: visually distinct distractors.  
Later levels: semantically close distractors (cat/dog/rabbit) or attribute differences (big red ball vs small red ball).

## Audio production

- One narrator accent consistently across MVP.
- Moderate speed, natural prosody, no baby talk.
- Leave ~300–500 ms silence around isolated vocabulary in production files.
- Never use loud failure SFX.

## Curriculum mapping

The topic grouping is aligned to common Pre-A1/young learner themes such as body, zoo/animals, clothes, food, home, school, street, numbers and simple everyday language. The pack deliberately curates its own vocabulary rather than reproducing an entire proprietary wordlist.
"""
(ROOT/'05_CONTENT_GUIDE.md').write_text(content_guide,encoding='utf-8')

# ---------- Backend ----------
backend="""# ABP DOMAIN MODEL

## Aggregates / entities

### ChildProfile

- Id: Guid
- ParentUserId: Guid
- Nickname: string(40)
- AvatarKey: string(80)
- AgeBand: enum (`5_6`, `6_7`, `7_8`)
- IsActive: bool
- CreationTime / audit fields

Invariant: only owner parent (or admin) can access child progress.

### GameWorld

- Id: string (`W01`)
- Slug
- Name
- DisplayOrder
- Theme
- IsPublished
- ContentVersion

### GameLevel

- Id: string (`W01-L01`)
- WorldId
- DisplayOrder
- Mechanic
- Difficulty
- DefinitionJson (JSONB acceptable for mechanic-specific payload)
- Instruction
- InstructionAudioKey
- IsBoss
- IsPublished
- ContentVersion

Recommendation: keep common searchable columns relational; mechanic-specific configuration in JSONB.

### VocabularyTerm

- Id: Guid
- NormalizedTerm
- DisplayTerm
- Category
- PartOfSpeech
- AudioKey
- ImageKey
- IsActive

### LevelVocabulary

- LevelId
- VocabularyTermId
- Role (`Target`, `Review`, `Distractor`)

### LevelAttempt

- Id: Guid (client-generated accepted)
- ChildProfileId
- LevelId
- StartedAt
- CompletedAt
- WrongAttempts
- HintCount
- Stars
- DurationMs
- PayloadJson

Unique index on attempt Id provides sync idempotency.

### PlayerProgress

- ChildProfileId
- LevelId
- BestStars
- CompletedCount
- FirstCompletedAt
- LastCompletedAt
- BestDurationMs nullable

Unique `(ChildProfileId, LevelId)`.

### WordMastery

- ChildProfileId
- VocabularyTermId
- ExposureCount
- CorrectCount
- IncorrectCount
- MasteryScore decimal
- LastSeenAt
- NextReviewAt nullable

MVP mastery can be simple weighted accuracy + exposure threshold; do not add complex ML.

## Application services

- `ChildProfileAppService`
- `GameContentAppService`
- `GameProgressAppService`
- `ParentDashboardAppService`
- `GameAdminAppService`

## Permission names

- `WordyWings.GameAdmin`
- `WordyWings.Content.View`
- `WordyWings.Content.Edit`
- `WordyWings.Content.Publish`
- Parent endpoints use ownership checks, not broad admin permissions.

## Progress rules

1. Complete level -> upsert best star score.
2. Next level unlocked if previous level completed >=1 star.
3. Next world unlocked when previous world's L10 boss (MVP) is completed.
4. 1-star completion never blocks narrative progress.
5. Retry can improve best stars; never reduce them.

## Mastery MVP formula

Keep transparent:

```text
exposureWeight = min(exposureCount / 4, 1)
accuracy = correctCount / max(correctCount + incorrectCount, 1)
masteryScore = round(100 * (0.4 * exposureWeight + 0.6 * accuracy))
```

Suggested review when `exposureCount >= 2 && masteryScore < 70`.

This formula is a product heuristic, not an assessment claim.
"""
(ROOT/'backend'/'abp_domain_model.md').write_text(backend,encoding='utf-8')

# ---------- Frontend ----------
frontend="""# REACT + PHASER ARCHITECTURE

## React ownership

Routes:

```text
/login
/children
/map/:childId
/play/:childId/:levelId
/parent/:childId/dashboard
/admin/content
```

`/play/...` renders `GameContainer` and hands a validated `LevelDefinition` + services into Phaser.

## Phaser contract

```ts
interface GameMechanic {
  readonly id: MechanicId;
  mount(ctx: MechanicContext, level: LevelDefinition): Promise<void> | void;
  dispose(): void;
}
```

`MechanicContext` exposes only:

- Phaser scene;
- audio service;
- asset resolver;
- attempt tracker;
- hint controller;
- event bus.

Mechanics do not call REST APIs directly.

## Event bridge

Phaser -> React events:

- `LEVEL_READY`
- `LEVEL_COMPLETED`
- `LEVEL_EXIT_REQUESTED`
- `GAME_ERROR`

React -> Phaser commands:

- `PAUSE`
- `RESUME`
- `RESTART`
- `MUTE_CHANGED`

## Scaling

Use Phaser Scale Manager with FIT + CENTER_BOTH around a logical 1600x900 gameplay space. UI safe area margins must account for small landscape phones and later Capacitor safe-area insets.

## Mechanics MVP notes

### Word Shot

Use drag pointer to set launch vector, preview dotted trajectory, release. Physics can use Matter or simpler ballistic tween in early MVP. Educational correctness is based on target collision, not physical destruction.

### Balloon Pop

Spawn 3–5 balloons with stable readable target images. Tap/click to pop. No harsh timer in first 10 levels.

### Drag & Sort

Pointer drag object to large destination zone. Provide click-select + click-destination fallback if drag becomes inaccessible.

### Letter Puzzle

Letter tiles are shuffled but target word remains 3–8 letters in MVP. After two errors, first correct tile pulses.

### Boss Challenge

Orchestrator runs 4 micro-rounds using existing mechanic modules. One completion payload; checkpoints inside boss prevent full reset.

## Asset resolution

```ts
interface AssetResolver {
  image(key: string): string;
  audio(key: string): string;
}
```

All production URLs derive from environment/config `ASSET_BASE_URL`.
"""
(ROOT/'frontend'/'phaser_architecture.md').write_text(frontend,encoding='utf-8')

# ---------- Deployment ----------
deployment="""# DEPLOYMENT — RENDER

## Services

### 1. Backend

Render Web Service using Docker.

Environment variables (names can adapt to actual ABP configuration):

- ASPNETCORE_ENVIRONMENT=Production
- ConnectionStrings__Default=<PostgreSQL connection>
- App__SelfUrl=https://api.example.com
- App__CorsOrigins=https://game.example.com
- AuthServer__Authority=https://api.example.com
- AuthServer__RequireHttpsMetadata=true
- Assets__BaseUrl=https://cdn.example.com

Run migrations through DbMigrator as a controlled release step; do not blindly run concurrent migrations from every API instance.

### 2. Frontend

Render Static Site from React build output.

- API base URL points to backend.
- Configure SPA rewrite fallback to `/index.html`.
- Set immutable cache for hashed game bundles; content manifest should use version-aware cache policy.

### 3. Database

Recommended: existing Supabase PostgreSQL or managed paid PostgreSQL. Do not rely on temporary/free DB for production child progress.

### 4. Assets

Cloudflare R2/S3-compatible storage. Public CDN for non-sensitive game assets. Do not store user-sensitive uploads in the same public bucket.

## Docker backend checklist

- multi-stage .NET build;
- expose configured port;
- bind to `0.0.0.0`;
- health endpoint;
- no local persistent assumption;
- production logging;
- data protection keys configured appropriately for multi-instance deployment if needed.

## Mobile later

Android/iOS Capacitor clients continue calling the same HTTPS API and CDN. Add OIDC redirect/deep-link settings and secure token storage adapter; backend topology stays unchanged.
"""
(ROOT/'deployment'/'render.md').write_text(deployment,encoding='utf-8')

# ---------- QA ----------
acceptance="""# MVP ACCEPTANCE CRITERIA

## P0 — must pass

- [ ] Parent can register/login.
- [ ] Parent can create a child profile.
- [ ] Child map loads W01–W03.
- [ ] Exactly 30 MVP level definitions validate and load.
- [ ] Five MVP mechanics are playable with mouse and touch.
- [ ] Speaker/replay works or gracefully falls back.
- [ ] Wrong answer never causes hard fail/game-over.
- [ ] Two wrong attempts trigger hint.
- [ ] Completing level persists attempt and unlocks next level.
- [ ] Retry cannot reduce best stars.
- [ ] Reload preserves progression.
- [ ] API outage after level completion queues pending attempt locally.
- [ ] Reconnect syncs pending attempt without duplicate progress.
- [ ] Missing asset does not crash level.
- [ ] Missing audio does not block completion.
- [ ] World 2 unlocks after W01-L10; World 3 after W02-L10.
- [ ] Parent dashboard shows completed levels, best stars and review terms.
- [ ] Build succeeds in production mode.

## P1 — should pass

- [ ] Chrome desktop current stable.
- [ ] Android Chrome landscape.
- [ ] iPad/Safari landscape smoke test.
- [ ] No console error during normal 5-level session.
- [ ] Average asset request path uses CDN/base URL abstraction.
- [ ] Gameplay target areas remain comfortably tappable at 667x375 CSS viewport.

## P2 — polish

- [ ] Reduced-motion preference disables nonessential looping animation.
- [ ] Mute state persists locally.
- [ ] Level result animation completes within ~2.5 seconds or can be skipped.
"""
(ROOT/'qa'/'acceptance_criteria.md').write_text(acceptance,encoding='utf-8')

test_matrix="""# TEST MATRIX

| Area | Case | Expected |
|---|---|---|
| Auth | Expired token during save | attempt stays queued; user can recover after auth |
| Content | Invalid mechanic ID | level rejected before scene launch |
| Content | Missing imageKey | generic placeholder shown |
| Audio | MP3 404 | fallback voice or silent playable state |
| Progress | same attempt submitted twice | one logical attempt, no duplicate reward |
| Progress | replay with fewer stars | best star unchanged |
| Offline | complete 3 levels offline | all 3 queue and sync later |
| Input | touch drag slingshot | launch vector updates smoothly |
| Input | mouse drag | same outcome as touch |
| Balloon | rapid double tap | single scoring event |
| Sort | drop outside zone | object returns safely |
| Puzzle | repeated wrong letter | no lock; hint after threshold |
| Boss | fail round 3 | restart at round 3/checkpoint per design |
| Responsive | 667x375 | no clipped primary controls |
| Responsive | 1366x768 | canvas centered, no stretched distortion |
| Child safety | external link in kid mode | none present |
"""
(ROOT/'qa'/'test_matrix.md').write_text(test_matrix,encoding='utf-8')

# ---------- prompts ----------
prompts="""# CONTENT / ASSET / AUDIO PROMPTS

These prompts are for future production; MVP may use original SVG placeholders.

## Generate a new level definition

> Create one child-safe English learning level for a 6-year-old at Pre-A1. Output JSON only matching `schemas/level.schema.json`. Use one target word, 2–3 distractors, a 2–6 word spoken instruction where possible, no punishment mechanic, and one clear learning objective. Mechanic: {{mechanic}}. World: {{world}}. Available vocabulary: {{vocabulary}}. Avoid introducing any word outside the supplied vocabulary list.

## Generate review levels

> Given the child's weak terms {{terms}}, create 5 varied review level definitions using at least 3 different mechanics. Do not introduce new vocabulary. Increase hints rather than speed. Each target must appear in at least two different contexts across the five levels.

## Voice script QA

> Review these English instructions for a 6-year-old Pre-A1 learner. Keep grammar natural, vocabulary concrete, instructions short and unambiguous. Return corrected lines only. Do not add idioms, slang or culturally specific references.

## Production art direction prompt template

> Original children’s educational adventure game art, rounded toy-like 2.5D forms, bright friendly daylight, clear silhouette, large readable target object, uncluttered gameplay lane, soft materials, expressive but non-scary characters, no text embedded in art, no logos, no copyrighted character resemblance. World theme: {{world}}. Required objects: {{objects}}. Camera: 16:9 landscape game scene with safe empty space for UI.

## SFX brief

> Create a gentle 0.3–0.8 second game UI sound for a six-year-old learning game. Positive, soft, no startling transient, no casino-like reward cue. Event: {{event}}.
"""
(ROOT/'prompts'/'content_generation_prompts.md').write_text(prompts,encoding='utf-8')

# ---------- data ----------
(ROOT/'data'/'worlds.json').write_text(json.dumps(worlds,ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'data'/'levels_200.json').write_text(json.dumps(levels,ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'data'/'mvp_levels_30.json').write_text(json.dumps(mvp_levels,ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'data'/'vocabulary_pre_a1.json').write_text(json.dumps(vocab,ensure_ascii=False,indent=2),encoding='utf-8')

with (ROOT/'data'/'levels_200.csv').open('w',newline='',encoding='utf-8-sig') as f:
    wr=csv.writer(f)
    wr.writerow(['id','worldId','order','mechanic','difficulty','instruction','targetVocabulary','reviewVocabulary','estimatedSeconds','isBoss'])
    for l in levels:
        wr.writerow([l['id'],l['worldId'],l['order'],l['mechanic'],l['difficulty'],l['instruction'],'|'.join(l['targetVocabulary']),'|'.join(l['reviewVocabulary']),l['estimatedSeconds'],l['isBoss']])

with (ROOT/'data'/'vocabulary_pre_a1.csv').open('w',newline='',encoding='utf-8-sig') as f:
    wr=csv.DictWriter(f,fieldnames=['term','world','category','part_of_speech','notes'])
    wr.writeheader(); wr.writerows(vocab)

# ---------- schemas ----------
level_schema={
 '$schema':'https://json-schema.org/draft/2020-12/schema','title':'WordyWings LevelDefinition','type':'object',
 'required':['id','worldId','order','mechanic','difficulty','targetVocabulary','instruction','targets','hintPolicy','learningObjective','isBoss'],
 'properties':{
  'id':{'type':'string','pattern':'^W[0-9]{2}-L[0-9]{2}$'},
  'worldId':{'type':'string','pattern':'^W[0-9]{2}$'},
  'order':{'type':'integer','minimum':1,'maximum':99},
  'title':{'type':'string'},
  'mechanic':{'enum':[m['id'] for m in mechanics]},
  'difficulty':{'type':'integer','minimum':1,'maximum':4},
  'targetVocabulary':{'type':'array','minItems':1,'items':{'type':'string'}},
  'reviewVocabulary':{'type':'array','items':{'type':'string'}},
  'instruction':{'type':'string','minLength':1},
  'instructionAudioKey':{'type':'string'},
  'targets':{'type':'array','minItems':2,'items':{'type':'object','required':['value','correct'],'properties':{'value':{'type':'string'},'correct':{'type':'boolean'},'assetKey':{'type':'string'}}}},
  'stars':{'type':'object'},
  'hintPolicy':{'type':'object','required':['afterWrongAttempts','audioReplay','visualPulse']},
  'estimatedSeconds':{'type':'integer','minimum':10,'maximum':300},
  'learningObjective':{'type':'string'},
  'reward':{'type':'object'},
  'isBoss':{'type':'boolean'}
 },
 'additionalProperties':True
}
world_schema={'$schema':'https://json-schema.org/draft/2020-12/schema','title':'WordyWings World','type':'object','required':['id','slug','name','vi','theme','hero','goal'],'properties':{'id':{'type':'string'},'slug':{'type':'string'},'name':{'type':'string'},'vi':{'type':'string'},'theme':{'type':'string'},'hero':{'type':'string'},'palette':{'type':'string'},'goal':{'type':'string'}}}
progress_schema={'$schema':'https://json-schema.org/draft/2020-12/schema','title':'WordyWings ProgressSync','type':'object','required':['attemptId','childId','levelId','startedAt','completedAt','stars'],'properties':{'attemptId':{'type':'string'},'childId':{'type':'string'},'levelId':{'type':'string'},'startedAt':{'type':'string'},'completedAt':{'type':'string'},'wrongAttempts':{'type':'integer','minimum':0},'hintCount':{'type':'integer','minimum':0},'stars':{'type':'integer','minimum':1,'maximum':3},'targetResults':{'type':'array'}}}
(ROOT/'schemas'/'level.schema.json').write_text(json.dumps(level_schema,indent=2),encoding='utf-8')
(ROOT/'schemas'/'world.schema.json').write_text(json.dumps(world_schema,indent=2),encoding='utf-8')
(ROOT/'schemas'/'progress.schema.json').write_text(json.dumps(progress_schema,indent=2),encoding='utf-8')

# Mechanics data
(ROOT/'data'/'mechanics.json').write_text(json.dumps(mechanics,ensure_ascii=False,indent=2),encoding='utf-8')

# Basic stats file
stats={
 'worlds':len(worlds),'levels':len(levels),'mvpLevels':len(mvp_levels),'mechanics':len(mechanics),'vocabularyTerms':len(vocab),
 'levelDistribution':{w['id']:sum(1 for l in levels if l['worldId']==w['id']) for w in worlds}
}
(ROOT/'PACK_STATS.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2),encoding='utf-8')

print(json.dumps(stats,ensure_ascii=False,indent=2))
