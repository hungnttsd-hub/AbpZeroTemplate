import json, csv, random, os, textwrap, math, re
from collections import Counter, defaultdict
from pathlib import Path
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment
from openpyxl.utils import get_column_letter

ROOT = Path('/mnt/data/wordy_golden_bell_pack')
DATA = ROOT/'data'
DOCS = ROOT/'docs'
PROMPTS = ROOT/'prompts'
QA = ROOT/'qa'
BACKEND = ROOT/'backend'
FRONTEND = ROOT/'frontend'
for p in [DATA,DOCS,PROMPTS,QA,BACKEND,FRONTEND]: p.mkdir(parents=True, exist_ok=True)

random.seed(20260922)

VOCAB = {
    'animals': ['cat','dog','bird','fish','duck','chicken','cow','horse','monkey','tiger','elephant','rabbit','frog','turtle','bee','butterfly','lion','giraffe','zebra','bear'],
    'colors': ['red','blue','yellow','green','orange','pink','purple','black','white','brown'],
    'shapes': ['circle','square','triangle','star','heart','rectangle','oval'],
    'food': ['apple','banana','orange','cake','milk','rice','bread','egg','water','juice','carrot','potato','tomato','pear','grape','watermelon','cookie'],
    'home': ['bed','chair','table','door','window','lamp','sofa','cup','plate','spoon','fork','clock','box'],
    'school': ['book','pen','pencil','bag','desk','ruler','eraser','teacher','student','board'],
    'transport': ['car','bus','bike','train','plane','boat','truck'],
    'body': ['head','hair','eye','ear','nose','mouth','hand','foot','arm','leg'],
    'actions': ['run','jump','walk','eat','drink','sleep','swim','dance','read','write','sing','sit','stand','fly','open','close'],
    'weather': ['sunny','rainy','cloudy','windy','hot','cold'],
    'clothes': ['shirt','shoes','hat','dress','coat','socks'],
    'adjectives': ['big','small','long','short','happy','sad','fast','slow','tall'],
    'prepositions': ['in','on','under','next to','behind','in front of','between'],
    'numbers': [str(i) for i in range(1,21)]
}

EMOJI = {
    'cat':'🐱','dog':'🐶','bird':'🐦','fish':'🐟','duck':'🦆','chicken':'🐔','cow':'🐄','horse':'🐴','monkey':'🐵','tiger':'🐯','elephant':'🐘','rabbit':'🐰','frog':'🐸','turtle':'🐢','bee':'🐝','butterfly':'🦋','lion':'🦁','giraffe':'🦒','zebra':'🦓','bear':'🐻',
    'apple':'🍎','banana':'🍌','orange':'🍊','cake':'🍰','milk':'🥛','rice':'🍚','bread':'🍞','egg':'🥚','water':'💧','juice':'🧃','carrot':'🥕','potato':'🥔','tomato':'🍅','pear':'🍐','grape':'🍇','watermelon':'🍉','cookie':'🍪',
    'car':'🚗','bus':'🚌','bike':'🚲','train':'🚆','plane':'✈️','boat':'⛵','truck':'🚚',
    'book':'📘','pencil':'✏️','pen':'🖊️','bag':'🎒','ruler':'📏','eraser':'🧽',
    'bed':'🛏️','chair':'🪑','door':'🚪','window':'🪟','lamp':'💡','sofa':'🛋️','cup':'🥤','plate':'🍽️','spoon':'🥄','fork':'🍴','clock':'🕒','box':'📦',
    'hat':'🧢','shirt':'👕','shoes':'👟','dress':'👗','coat':'🧥','socks':'🧦',
    'star':'⭐','heart':'❤️','circle':'🔵','square':'🟦','triangle':'🔺','rectangle':'▭','oval':'🥚',
}

def asset(cat, item):
    slug = re.sub(r'[^a-z0-9]+','_', str(item).lower()).strip('_')
    return f'{cat}_{slug}'

def opt(label, cat=None, item=None, subtitle=None):
    o={'label':str(label)}
    if cat and item: o['assetKey']=asset(cat,item)
    if item in EMOJI: o['emoji']=EMOJI[item]
    if subtitle: o['subtitle']=subtitle
    return o

def shuffled_options(correct, distractors):
    vals=[correct]+distractors
    random.shuffle(vals)
    out=[]
    correct_id=None
    for i,v in enumerate(vals):
        oid=chr(65+i)
        out.append({'id':oid, **v})
        if v.get('_correct'):
            correct_id=oid
        v.pop('_correct',None)
    return out, correct_id

def make_q(diff, qtype, mechanic, skill, prompt, audio, stimulus, options, answer, hint, explanation, vocab, worlds, seconds=12, memory=False, notes=''):
    return {
        'difficulty': diff,
        'questionType': qtype,
        'mechanic': mechanic,
        'skill': skill,
        'promptText': prompt,
        'audioText': audio,
        'stimulus': stimulus,
        'options': options,
        'answer': answer,
        'hint': hint,
        'explanation': explanation,
        'targetVocabulary': vocab,
        'worldTags': worlds,
        'estimatedSeconds': seconds,
        'requiresMemoryPhase': memory,
        'notes': notes,
    }

# --- generators ---

def gen_listen_find(diff, idx):
    cat=random.choice(['animals','food','transport','school','home'])
    items=random.sample(VOCAB[cat],4)
    target=items[0]
    correct=opt(target,cat,target); correct['_correct']=True
    opts,cid=shuffled_options(correct,[opt(x,cat,x) for x in items[1:]])
    return make_q(diff,'listen_find_picture','listen_run','listening',
        f'Listen and find: {target.upper()}.', f'Find the {target}.',
        {'type':'none'}, opts, {'type':'option','value':cid},
        {'afterWrong':2,'text':f'Listen again: {target}.','strategy':'pulse_correct_asset'},
        f'The correct answer is {target}.', [target],[cat],8)

def gen_picture_to_word(diff, idx):
    cat=random.choice(['animals','food','transport','school','home'])
    items=random.sample(VOCAB[cat],4); target=items[0]
    correct={'label':target.upper(),'_correct':True}
    opts,cid=shuffled_options(correct,[{'label':x.upper()} for x in items[1:]])
    return make_q(diff,'picture_to_word','raise_board','vocabulary',
        'What is this?', 'What is this?',
        {'type':'image','assetKey':asset(cat,target),'emoji':EMOJI.get(target,'')}, opts, {'type':'option','value':cid},
        {'afterWrong':2,'text':'Look at the picture and listen again.','strategy':'replay_prompt'},
        f'This is {target}.',[target],[cat],10)

def gen_color_recognition(diff, idx):
    colors=random.sample(VOCAB['colors'],4); target=colors[0]
    correct={'label':target.upper(),'assetKey':asset('color',target),'_correct':True}
    opts,cid=shuffled_options(correct,[{'label':c.upper(),'assetKey':asset('color',c)} for c in colors[1:]])
    return make_q(diff,'color_recognition','answer_zone','vocabulary',
        f'Find {target.upper()}.', f'Find {target}.', {'type':'colorSwatches'}, opts, {'type':'option','value':cid},
        {'afterWrong':2,'text':f'Find the {target} color.','strategy':'soft_glow'}, f'The correct color is {target}.',[target],['colors'],8)

def gen_shape_recognition(diff, idx):
    shapes=random.sample(VOCAB['shapes'],4); target=shapes[0]
    correct=opt(target,'shape',target); correct['_correct']=True
    opts,cid=shuffled_options(correct,[opt(s,'shape',s) for s in shapes[1:]])
    return make_q(diff,'shape_recognition','answer_zone','vocabulary',
        f'Find the {target.upper()}.',f'Find the {target}.',{'type':'shapeSet'},opts,{'type':'option','value':cid},
        {'afterWrong':2,'text':f'Look for the {target}.','strategy':'outline_hint'},f'This shape is a {target}.',[target],['shapes'],8)

def gen_count(diff, idx, maxn=10, colored=False):
    if colored:
        item=random.choice(['star','circle','square','ball','balloon'])
    else:
        item=random.choice(VOCAB['animals'][:8]+VOCAB['food'][:8])
    n=random.randint(1,maxn)
    dnums=set()
    while len(dnums)<3:
        x=max(1,min(maxn,n+random.choice([-3,-2,-1,1,2,3])))
        if x!=n:dnums.add(x)
    nums=[n]+list(dnums)
    correct={'label':str(n),'_correct':True}
    opts,cid=shuffled_options(correct,[{'label':str(x)} for x in nums[1:]])
    color=random.choice(VOCAB['colors']) if colored else None
    desc=f'{n} {color+" " if color else ""}{item}s'
    prompt=f'How many {color+" " if color else ""}{item}s are there?'
    cat='shape' if item in VOCAB['shapes'] else ('animals' if item in VOCAB['animals'] else ('food' if item in VOCAB['food'] else 'object'))
    return make_q(diff,'count_objects','bell_choice','counting',prompt,prompt,
        {'type':'scene','objects':[{'assetKey':asset(cat,item),'count':n,'color':color}], 'description':desc},opts,{'type':'option','value':cid},
        {'afterWrong':2,'text':'Count slowly: one, two, three...','strategy':'count_bounce'},f'There are {n} {item}s.',[item,str(n)]+([color] if color else []),['numbers'],10)

def gen_action(diff, idx):
    actions=random.sample(['run','jump','swim','dance','sleep','read','write','sit','stand','fly'],4)
    target=actions[0]
    gerunds={'run':'running','swim':'swimming','sit':'sitting','dance':'dancing','write':'writing','fly':'flying','sleep':'sleeping','read':'reading','stand':'standing','jump':'jumping'}
    g=gerunds[target]
    correct={'label':target.upper(),'assetKey':asset('action',target),'_correct':True}
    opts,cid=shuffled_options(correct,[{'label':a.upper(),'assetKey':asset('action',a)} for a in actions[1:]])
    return make_q(diff,'action_recognition','listen_run','listening',f'Who is {g}?',
        f'Who is {g}?',{'type':'actionCards'},opts,{'type':'option','value':cid},
        {'afterWrong':2,'text':f'Watch the actions. Find {g}.','strategy':'replay_motion'},f'The correct action is {target}.',[target],['actions'],10)

def gen_color_object(diff, idx):
    object_pool=[('shape',x) for x in VOCAB['shapes']]+[('transport',x) for x in ['car','bike','bus']]+[('object',x) for x in ['ball','balloon','kite','bag','hat']]
    objcat,obj=random.choice(object_pool)
    colors=random.sample(VOCAB['colors'],4); target=colors[0]
    correct={'label':f'{target} {obj}','assetKey':asset(objcat,obj),'displayColor':target,'_correct':True}
    opts,cid=shuffled_options(correct,[{'label':f'{c} {obj}','assetKey':asset(objcat,obj),'displayColor':c} for c in colors[1:]])
    return make_q(diff,'color_object','answer_zone','listening',f'Find the {target} {obj}.',f'Find the {target} {obj}.',{'type':'coloredObjects','object':obj},opts,{'type':'option','value':cid},
        {'afterWrong':2,'text':f'Look for {target}.','strategy':'color_word_replay'},f'Choose the {target} {obj}.',[target,obj],['colors'],10)

def gen_category(diff, idx, odd=True):
    cats=['animals','food','transport','school','home']
    main=random.choice(cats); other=random.choice([c for c in cats if c!=main])
    three=random.sample(VOCAB[main],3); outsider=random.choice(VOCAB[other])
    items=three+[outsider]; random.shuffle(items)
    options=[]; cid=None
    for i,item in enumerate(items):
        cat=main if item in three else other
        oid=chr(65+i); options.append({'id':oid,**opt(item,cat,item)})
        if item==outsider: cid=oid
    prompt=f'Which one is NOT a {main[:-1] if main.endswith("s") else main} item?'
    if main=='animals': prompt='Which one is NOT an animal?'
    elif main=='food': prompt='Which one is NOT food?'
    elif main=='transport': prompt='Which one is NOT transport?'
    elif main=='school': prompt='Which one does NOT belong at school?'
    elif main=='home': prompt='Which one does NOT belong at home?'
    return make_q(diff,'odd_one_out','bell_choice','categorization',prompt,prompt,{'type':'objectRow'},options,{'type':'option','value':cid},
        {'afterWrong':2,'text':f'Three belong together. One is different.','strategy':'group_three'},f'{outsider} belongs to {other}, not {main}.',[*three,outsider],[main,other],12)

def gen_size_color(diff, idx):
    object_pool=VOCAB['shapes']+['ball','balloon','kite','car','bike']
    obj=random.choice(object_pool)
    color=random.choice(VOCAB['colors'])
    size=random.choice(['big','small'])
    alt_color=random.choice([c for c in VOCAB['colors'] if c!=color])
    combos=[(size,color),('small' if size=='big' else 'big',color),(size,alt_color),('small' if size=='big' else 'big',alt_color)]
    random.shuffle(combos)
    options=[]; cid=None
    for i,(sz,co) in enumerate(combos):
        oid=chr(65+i); options.append({'id':oid,'label':f'{sz} {co} {obj}','assetKey':asset('object',obj),'displayColor':co,'size':sz})
        if sz==size and co==color: cid=oid
    return make_q(diff,'two_attribute_object','answer_zone','listening',f'Find the {size} {color} {obj}.',f'Find the {size} {color} {obj}.',{'type':'attributeObjects'},options,{'type':'option','value':cid},
        {'afterWrong':2,'text':f'Look for BOTH: {size} and {color}.','strategy':'highlight_prompt_words'},f'The answer must be {size} and {color}.',[size,color,obj],['colors','adjectives'],12)

def gen_preposition(diff, idx, advanced=False):
    animal=random.choice(['cat','dog','rabbit','frog','duck','monkey'])
    obj=random.choice(['box','chair','table','bed'])
    preps=['in','on','under'] if not advanced else ['next to','behind','in front of','between']
    target=random.choice(preps)
    obj2=random.choice([x for x in ['box','chair','table','bed'] if x!=obj])
    placements=random.sample(preps,len(preps)); random.shuffle(placements)
    options=[]; cid=None
    for i,p in enumerate(placements):
        oid=chr(65+i)
        if p=='between':
            label=f'{animal} between {obj} and {obj2}'
            scene={'animal':animal,'object':obj,'object2':obj2,'relation':p}
        else:
            label=f'{animal} {p} {obj}'
            scene={'animal':animal,'object':obj,'relation':p}
        options.append({'id':oid,'label':label,'scene':scene})
        if p==target: cid=oid
    if target=='between':
        sentence=f'The {animal} is between the {obj} and the {obj2}.'
    else:
        sentence=f'The {animal} is {target} the {obj}.'
    return make_q(diff,'preposition_scene','answer_zone','sentence_comprehension',f'Choose the picture: {sentence}',sentence,{'type':'sceneChoices'},options,{'type':'option','value':cid},
        {'afterWrong':2,'text':f'Listen to the position word: {target}.','strategy':'animate_relation'},sentence,[animal,obj,target]+([obj2] if target=='between' else []),['prepositions','animals'],14)

def gen_missing_letter(diff, idx, blanks=1):
    words=[w for cat in ['animals','food','home','school','transport','colors','shapes'] for w in VOCAB[cat] if len(w)>=3 and len(w)<=8]
    word=random.choice(words)
    chars=list(word.upper())
    pos=random.sample(range(len(chars)), min(blanks,len(chars)-1))
    missing=[chars[p] for p in pos]
    masked=chars[:]
    for p in pos: masked[p]='_'
    if blanks==1:
        correct=missing[0]
        letters=[correct]
        alphabet=list('ABCDEFGHIJKLMNOPQRSTUVWXYZ')
        while len(letters)<4:
            x=random.choice(alphabet)
            if x not in letters: letters.append(x)
        random.shuffle(letters)
        opts=[{'id':chr(65+i),'label':x} for i,x in enumerate(letters)]
        cid=next(o['id'] for o in opts if o['label']==correct)
        ans={'type':'option','value':cid}
    else:
        # options are pairs in correct order vs distractors
        correct=''.join(missing)
        pairs={correct}
        alphabet=list('ABCDEFGHIJKLMNOPQRSTUVWXYZ')
        while len(pairs)<4:
            cand=''.join(random.choice(alphabet) for _ in range(blanks))
            pairs.add(cand)
        pairs=list(pairs); random.shuffle(pairs)
        opts=[{'id':chr(65+i),'label':x} for i,x in enumerate(pairs)]
        cid=next(o['id'] for o in opts if o['label']==correct)
        ans={'type':'option','value':cid,'missingPositions':pos}
    return make_q(diff,'missing_letter','raise_board','spelling',f'Complete the word: {" ".join(masked)}',f'Complete the word {word}.',{'type':'word','maskedWord':' '.join(masked),'meaningAssetKey':asset('vocab',word)},opts,ans,
        {'afterWrong':2,'text':f'Listen: {word}.','strategy':'play_word_slow'},f'The word is {word.upper()}.',[word],['spelling'],12)

def gen_function(diff, idx):
    names=['Pip','Momo','Lulu','Poki','Foxy']
    name=names[idx % len(names)]
    mapping=[
        ('What do we use to write?','pencil','school',['book','bag','ruler']),
        ('What do we use to drink?','cup','home',['plate','fork','chair']),
        ('What do we use when it rains?','coat','clothes',['hat','socks','shirt']),
        ('Where do we sleep?','bed','home',['chair','table','door']),
        ('What do we read?','book','school',['pen','bag','ruler']),
        ('What do we use to eat soup?','spoon','home',['fork','plate','cup']),
        ('What do we wear on our feet?','shoes','clothes',['hat','shirt','coat']),
        ('What takes us on a railway?','train','transport',['boat','car','bike']),
        ('What can fly in the sky?','plane','transport',['car','boat','bike']),
    ]
    base_prompt,target,cat,dist=random.choice(mapping)
    variants=[base_prompt, f'{name} needs help. {base_prompt}', f'Help {name}: {base_prompt}', f'{name} asks: {base_prompt}']
    prompt=variants[(idx // len(names)) % len(variants)]
    correct=opt(target,cat,target); correct['_correct']=True
    opts,cid=shuffled_options(correct,[opt(x,cat,x) for x in dist])
    return make_q(diff,'function_question','bell_choice','meaning',prompt,prompt,{'type':'none'},opts,{'type':'option','value':cid},
        {'afterWrong':2,'text':'Think about what the object is used for.','strategy':'show_context_icon'},f'The correct answer is {target}.',[target],[cat],12)

def gen_weather_clothes(diff, idx):
    names=['Pip','Momo','Lulu','Poki','Foxy']
    name=names[idx % len(names)]
    mapping=[('rainy','coat'),('cold','coat'),('sunny','hat'),('hot','hat')]
    weather,target=random.choice(mapping)
    dist=random.sample([x for x in VOCAB['clothes'] if x!=target],3)
    correct=opt(target,'clothes',target); correct['_correct']=True
    opts,cid=shuffled_options(correct,[opt(x,'clothes',x) for x in dist])
    prompt=f'It is {weather}. What should {name} wear?'
    return make_q(diff,'weather_choice','bell_choice','sentence_comprehension',prompt,prompt,{'type':'weather','weather':weather},opts,{'type':'option','value':cid},
        {'afterWrong':2,'text':f'It is {weather}. Think about the weather.','strategy':'weather_animation'},f'{target} is a good choice for {weather} weather.',[weather,target],['weather','clothes'],12)

def gen_sentence_completion(diff, idx):
    mapping=[
        ('The fish can ____.','swim',['fly','read','dance']),
        ('The bird can ____.','fly',['read','write','sleep']),
        ('We ____ water.','drink',['read','jump','write']),
        ('We ____ a book.','read',['drink','swim','fly']),
        ('We ____ with a pencil.','write',['swim','eat','fly']),
        ('The rabbit can ____.','jump',['read','write','drink']),
        ('At night, we ____.','sleep',['fly','write','swim']),
        ('On a chair, we ____.','sit',['fly','swim','sing']),
    ]
    prompt,target,dist=random.choice(mapping)
    correct={'label':target.upper(),'_correct':True}
    opts,cid=shuffled_options(correct,[{'label':x.upper()} for x in dist])
    return make_q(diff,'sentence_completion','raise_board','grammar',prompt,prompt,{'type':'sentence','text':prompt},opts,{'type':'option','value':cid},
        {'afterWrong':2,'text':'Listen to the whole sentence again.','strategy':'replay_sentence'},f'The sentence needs {target}.',[target],['actions'],14)

def gen_compare(diff, idx):
    item=random.choice(['apple','duck','star','ball'])
    a=random.randint(1,5); b=random.randint(1,5)
    while b==a: b=random.randint(1,5)
    ask_more=random.choice([True,False])
    if ask_more:
        prompt=f'Which group has MORE {item}s?'; correct='A' if a>b else 'B'
    else:
        prompt=f'Which group has FEWER {item}s?'; correct='A' if a<b else 'B'
    options=[{'id':'A','label':f'{a} {item}s','count':a,'assetKey':asset('vocab',item)},{'id':'B','label':f'{b} {item}s','count':b,'assetKey':asset('vocab',item)}]
    return make_q(diff,'compare_quantity','answer_zone','counting',prompt,prompt,{'type':'twoGroups','item':item,'a':a,'b':b},options,{'type':'option','value':correct},
        {'afterWrong':2,'text':'Count each group slowly.','strategy':'count_bounce'},f'Group {correct} has the requested amount.',[item,'more' if ask_more else 'fewer'],['numbers'],14)

def gen_two_step(diff, idx):
    shapes=random.sample(VOCAB['shapes'][:5],2); colors=random.sample(VOCAB['colors'],2)
    first=f'{colors[0]} {shapes[0]}'; second=f'{colors[1]} {shapes[1]}'
    seqs=[(first,second),(second,first),(f'{colors[0]} {shapes[1]}',second),(first,f'{colors[1]} {shapes[0]}')]
    random.shuffle(seqs)
    options=[]; cid=None
    for i,s in enumerate(seqs):
        oid=chr(65+i); options.append({'id':oid,'label':f'{s[0]} → {s[1]}','sequence':list(s)})
        if s==(first,second): cid=oid
    prompt=f'First choose the {first}. Then choose the {second}.'
    return make_q(diff,'two_step_instruction','listen_run','listening',prompt,prompt,{'type':'twoStepObjects','objects':[first,second]},options,{'type':'option','value':cid,'sequence':[first,second]},
        {'afterWrong':1,'text':'Remember: FIRST ... THEN ...','strategy':'repeat_in_two_chunks'},f'The correct order is {first}, then {second}.',[colors[0],shapes[0],colors[1],shapes[1]],['shapes','colors'],18)

def gen_word_picture_mismatch(diff, idx):
    cat=random.choice(['animals','food','transport'])
    words=random.sample(VOCAB[cat],4)
    wrong_word=random.choice(words)
    alt=random.choice([x for x in VOCAB[cat] if x not in words])
    options=[]; cid=None
    for i,w in enumerate(words):
        label=w.upper() if w!=wrong_word else alt.upper()
        oid=chr(65+i)
        options.append({'id':oid,'label':f'{EMOJI.get(w,w)}  {label}','imageAssetKey':asset(cat,w),'word':label})
        if w==wrong_word: cid=oid
    random.shuffle(options)
    for i,o in enumerate(options): o['id']=chr(65+i)
    cid=next(o['id'] for o in options if o['imageAssetKey']==asset(cat,wrong_word))
    return make_q(diff,'word_picture_mismatch','bell_choice','reading', 'Which pair is WRONG?', 'Which picture and word do not match?', {'type':'pairs'},options,{'type':'option','value':cid},
        {'afterWrong':2,'text':'Check each picture and word.','strategy':'read_pairs_one_by_one'},f'The {wrong_word} picture is labeled {alt}.',[wrong_word,alt],[cat,'reading'],15)

def gen_can_cant(diff, idx, negative=False):
    names=['Pip','Momo','Lulu','Poki','Foxy']
    name=names[idx % len(names)]
    can_map={
        'fly':['bird','butterfly','bee','plane'],
        'swim':['fish','duck','frog','turtle'],
        'run':['dog','rabbit','horse','tiger'],
        'jump':['rabbit','frog','monkey','tiger'],
        'read':['student','teacher','Pip','Lulu']
    }
    cannot_map={
        'fly':['fish','car','chair','book'],
        'swim':['book','chair','bus','pencil'],
        'run':['book','chair','bus','fish'],
        'jump':['book','chair','bus','fish'],
        'read':['fish','car','chair','duck']
    }
    action=random.choice(list(can_map))
    if negative:
        target_obj=random.choice(cannot_map[action])
        others=random.sample(can_map[action],3)
        candidates=[target_obj]+others
        random.shuffle(candidates)
        options=[]; cid=None
        for i,x in enumerate(candidates):
            oid=chr(65+i); options.append({'id':oid,'label':x.upper(),'assetKey':asset('vocab',x)})
            if x==target_obj: cid=oid
        prompt=random.choice([f'Which one CANNOT {action}?', f'{name} asks: Which one cannot {action}?', f'Find one that cannot {action}.'])
        exp=f'{target_obj} cannot {action}.'
    else:
        target_obj=random.choice(can_map[action])
        others=random.sample(cannot_map[action],3)
        candidates=[target_obj]+others
        random.shuffle(candidates)
        options=[]; cid=None
        for i,x in enumerate(candidates):
            oid=chr(65+i); options.append({'id':oid,'label':x.upper(),'assetKey':asset('vocab',x)})
            if x==target_obj: cid=oid
        prompt=random.choice([f'Which one can {action}?', f'{name} asks: Which one can {action}?', f'Find one that can {action}.'])
        exp=f'{target_obj} can {action}.'
    return make_q(diff,'can_cannot','bell_choice','meaning',prompt,prompt,{'type':'none'},options,{'type':'option','value':cid},
        {'afterWrong':2,'text':f'Listen carefully to {"cannot" if negative else "can"}.','strategy':'emphasize_modal'},exp,[target_obj,action],['actions'],14)

def gen_story(diff, idx, facts=1):
    names=['Pip','Momo','Lulu','Poki','Foxy']
    name=random.choice(names)
    animal=random.choice(['cat','dog','bird','duck','rabbit','monkey','frog','turtle'])
    color=random.choice(VOCAB['colors'][:8])
    liked_food=random.choice(VOCAB['food'][:12])
    count_food=random.choice(['apple','banana','orange','cookie','carrot','egg','pear','grape'])
    number=random.randint(1,5)
    place=random.choice(['home','school','garden'])
    place_phrase={'home':'at home','school':'at school','garden':'in the garden'}[place]
    story_parts=[f'{name} has a {color} {animal}.',f'The {animal} likes {liked_food}.',f'{name} has {number} {count_food}s.',f'They are {place_phrase}.']
    story=' '.join(story_parts[:facts+1])
    ask=random.choice(list(range(min(facts+1,4))))
    if ask==0:
        prompt=f'What color is the {animal}?'; correct=color; pool=VOCAB['colors'];
    elif ask==1:
        prompt=f'What does the {animal} like?'; correct=liked_food; pool=VOCAB['food'];
    elif ask==2:
        prompt=f'How many {count_food}s does {name} have?'; correct=str(number); pool=[str(i) for i in range(1,7)];
    else:
        prompt=f'Where are {name} and the {animal}?'; correct=place; pool=['home','school','garden','park'];
    distractors=random.sample([x for x in pool if x!=correct],3)
    c={'label':str(correct).upper(),'_correct':True}
    opts,cid=shuffled_options(c,[{'label':str(x).upper()} for x in distractors])
    return make_q(diff,'short_story','raise_board','listening',prompt,story,{'type':'story','text':story,'character':name},opts,{'type':'option','value':cid},
        {'afterWrong':1,'text':'Listen to the story one more time.','strategy':'replay_story'},f'The story says {correct}.',[animal,color,liked_food,count_food,str(number),place],['story'],18+facts*2)

def gen_sentence_order(diff, idx):
    short=[['THE','CAT','IS','BIG'],['THE','DOG','CAN','RUN'],['I','LIKE','RED','APPLES'],['THE','FISH','CAN','SWIM'],['MOMO','HAS','TWO','BOOKS']]
    long=[['THE','BIRD','IS','ON','THE','TREE'],['PIP','HAS','A','BLUE','KITE'],['THE','CAT','IS','UNDER','THE','TABLE'],['MOMO','HAS','TWO','RED','APPLES'],['THE','SMALL','DOG','IS','BEHIND','THE','CHAIR']]
    sentences=long if diff>=9 else short+long
    words=random.choice(sentences)
    shuffled=words[:]
    while shuffled==words: random.shuffle(shuffled)
    return make_q(diff,'sentence_order','quick_match','reading', 'Put the words in the correct order.', 'Put the words in the correct order.',
        {'type':'wordTiles','tiles':shuffled},[],{'type':'sequence','value':words},
        {'afterWrong':2,'text':'Start with the word that begins the sentence.','strategy':'pulse_first_word'},' '.join(words).capitalize()+'.',[w.lower() for w in words],['reading','sentence'],18)

def gen_multi_clue(diff, idx, final=False):
    animal=random.choice(['cat','dog','bird','duck','rabbit','monkey','frog','turtle'])
    color=random.choice(VOCAB['colors'][:8]); size=random.choice(['big','small']); relation=random.choice(['on','under','next to','behind'])
    if relation=='on' and animal in ['bird','monkey']:
        obj=random.choice(['tree','box','chair'])
    else:
        obj=random.choice(['table','chair','box'])
    candidates=[]
    correct_index=random.randrange(4)
    for i in range(4):
        if i==correct_index:
            cand={'animal':animal,'color':color,'size':size,'relation':relation,'object':obj}
        else:
            cand={'animal':animal if random.random()<0.6 else random.choice([x for x in ['cat','dog','bird','duck','rabbit','monkey','frog','turtle'] if x!=animal]),
                  'color':color if random.random()<0.5 else random.choice([x for x in VOCAB['colors'][:8] if x!=color]),
                  'size':size if random.random()<0.5 else ('small' if size=='big' else 'big'),
                  'relation':relation if random.random()<0.45 else random.choice([x for x in ['on','under','next to','behind'] if x!=relation]),
                  'object':obj}
            if cand=={'animal':animal,'color':color,'size':size,'relation':relation,'object':obj}:
                cand['color']=random.choice([x for x in VOCAB['colors'][:8] if x!=color])
        candidates.append(cand)
    options=[]
    for i,c in enumerate(candidates): options.append({'id':chr(65+i),'label':f"{c['size']} {c['color']} {c['animal']} {c['relation']} {c['object']}",'scene':c})
    cid=chr(65+correct_index)
    prompt=f'Find the {size} {color} {animal} {relation} the {obj}.'
    return make_q(diff,'multi_clue_scene','answer_zone','sentence_comprehension',prompt,prompt,{'type':'sceneChoices'},options,{'type':'option','value':cid},
        {'afterWrong':1 if final else 2,'text':f'Remember all clues: {size}, {color}, {animal}, {relation}.','strategy':'highlight_clues_sequentially'},f'The correct picture matches all four clues.',[size,color,animal,relation,obj],['final_reasoning' if final else 'prepositions'],18)

def gen_memory(diff, idx):
    chars=random.sample(['Pip','Momo','Lulu','Poki','Foxy'],3)
    colors=random.sample(VOCAB['colors'][:8],3)
    items=random.sample(['ball','kite','book','apple','hat','bag'],3)
    scene=[{'character':chars[i],'color':colors[i],'item':items[i]} for i in range(3)]
    ask_i=random.randrange(3); ask_kind=random.choice(['color','item','character'])
    s=scene[ask_i]
    if ask_kind=='color':
        prompt=f'What color was {s["character"]}\'s {s["item"]}?'; correct=s['color']; pool=VOCAB['colors'][:8]
    elif ask_kind=='item':
        prompt=f'What did {s["character"]} have?'; correct=s['item']; pool=['ball','kite','book','apple','hat','bag']
    else:
        prompt=f'Who had the {s["color"]} {s["item"]}?'; correct=s['character']; pool=['Pip','Momo','Lulu','Poki','Foxy']
    dist=random.sample([x for x in pool if x!=correct],3)
    c={'label':correct.upper(),'_correct':True}
    opts,cid=shuffled_options(c,[{'label':x.upper()} for x in dist])
    return make_q(diff,'memory_scene','bell_choice','memory',prompt,'Look carefully. Remember what you see.',{'type':'memoryScene','showMs':4500,'objects':scene},opts,{'type':'option','value':cid},
        {'afterWrong':1,'text':'Show the scene again for a moment.','strategy':'reveal_memory_scene_once'},f'The correct answer is {correct}.',[s['character'],s['color'],s['item']],['memory'],20,True)

def gen_inference(diff, idx):
    cases=[
        ('Momo is thirsty. What should Momo choose?','water',['book','hat','ball']),
        ('Pip is tired. What should Pip do?','sleep',['run','jump','dance']),
        ('It is raining. What should Lulu take?','coat',['book','apple','ball']),
        ('Poki wants to write. What does Poki need?','pencil',['cup','shoe','apple']),
        ('Foxy wants to read. What does Foxy need?','book',['spoon','hat','car']),
        ('Momo is hungry. What should Momo choose?','apple',['book','ruler','shoe']),
    ]
    prompt,target,dist=random.choice(cases)
    cat=next((c for c,l in VOCAB.items() if target in l), 'vocab')
    correct=opt(target,cat,target); correct['_correct']=True
    dopts=[]
    for x in dist:
        c2=next((c for c,l in VOCAB.items() if x in l),'vocab'); dopts.append(opt(x,c2,x))
    opts,cid=shuffled_options(correct,dopts)
    return make_q(diff,'simple_inference','bell_choice','reasoning',prompt,prompt,{'type':'characterSituation'},opts,{'type':'option','value':cid},
        {'afterWrong':2,'text':'Think about what the character needs.','strategy':'character_reaction'},f'The best answer is {target}.',[target],['reasoning'],16)

def gen_select_pair(diff, idx):
    color=random.choice(VOCAB['colors'][:8]); animal=random.choice(VOCAB['animals'][:10]); food=random.choice(VOCAB['food'][:10])
    # task choose a pair matching sentence
    correct_pair=(animal,food)
    pairs=[correct_pair]
    while len(pairs)<4:
        p=(random.choice(VOCAB['animals'][:10]),random.choice(VOCAB['food'][:10]))
        if p not in pairs: pairs.append(p)
    random.shuffle(pairs)
    options=[]; cid=None
    for i,p in enumerate(pairs):
        oid=chr(65+i); options.append({'id':oid,'label':f'{p[0]} + {p[1]}','assets':[asset('animals',p[0]),asset('food',p[1])]})
        if p==correct_pair: cid=oid
    prompt=f'Choose the pair: {animal} and {food}.'
    return make_q(diff,'select_pair','quick_match','listening',prompt,prompt,{'type':'pairGrid'},options,{'type':'option','value':cid},
        {'afterWrong':2,'text':'Find BOTH words from the sentence.','strategy':'repeat_pair_words'},f'The pair is {animal} and {food}.',[animal,food],['mixed'],14)

def gen_final_reasoning(diff, idx):
    cases=[]
    # possession transfer
    item=random.choice(['book','apple','kite','ball'])
    a,b=random.sample(['Pip','Momo','Lulu','Poki','Foxy'],2)
    cases.append({
        'prompt':f'{a} has a {item}. {a} gives it to {b}. Who has the {item} now?',
        'audio':f'{a} has a {item}. {a} gives it to {b}. Who has the {item} now?',
        'options':[a,b,'Lulu' if 'Lulu' not in [a,b] else 'Poki','Foxy' if 'Foxy' not in [a,b] else 'Pip'],
        'correct':b,'explanation':f'{a} gave the {item} to {b}, so {b} has it now.','vocab':[a,b,item]
    })
    # simple arithmetic comprehension
    fruit=random.choice(['apple','orange','banana'])
    start=random.randint(2,5); give=random.randint(1,start-1); left=start-give
    cases.append({
        'prompt':f'Momo has {start} {fruit}s. Momo gives {give} to Pip. How many are left?',
        'audio':f'Momo has {start} {fruit}s. Momo gives {give} to Pip. How many are left?',
        'options':[str(left),str(start),str(give),str(min(6,left+1))],
        'correct':str(left),'explanation':f'{start} minus {give} leaves {left}.','vocab':['Momo','Pip',fruit,str(start),str(give)]
    })
    # weather inference
    cases.append({
        'prompt':'Pip is wearing a coat and holding an umbrella. What is the weather probably like?',
        'audio':'Pip is wearing a coat and holding an umbrella. What is the weather probably like?',
        'options':['rainy','sunny','hot','windy'],'correct':'rainy','explanation':'A coat and umbrella are clues for rainy weather.','vocab':['coat','umbrella','rainy']
    })
    # relation chain
    cases.append({
        'prompt':'The blue star is next to the red circle. The yellow heart is next to the blue star. Which shape is next to the circle?',
        'audio':'The blue star is next to the red circle. The yellow heart is next to the blue star. Which shape is next to the circle?',
        'options':['blue star','yellow heart','red circle','green square'],'correct':'blue star','explanation':'The first sentence says the blue star is next to the red circle.','vocab':['blue','star','red','circle','yellow','heart','next to']
    })
    c=cases[idx % len(cases)]
    vals=[]
    for x in c['options']:
        if x not in vals: vals.append(x)
    # guarantee 4 options
    filler=['Poki','Foxy','green square','cloudy','6','1']
    for x in filler:
        if len(vals)>=4: break
        if x not in vals: vals.append(x)
    vals=vals[:4]
    random.shuffle(vals)
    opts=[{'id':chr(65+i),'label':str(x).upper()} for i,x in enumerate(vals)]
    cid=next(o['id'] for o in opts if o['label']==str(c['correct']).upper())
    return make_q(diff,'final_reasoning','bell_choice','reasoning',c['prompt'],c['audio'],{'type':'finalReasoningScene'},opts,{'type':'option','value':cid},
        {'afterWrong':1,'text':'Listen again and use all the clues.','strategy':'replay_in_chunks'},c['explanation'],[str(x) for x in c['vocab']],['final_bell','reasoning'],22)

GENS_BY_DIFF = {
1:[gen_listen_find,gen_picture_to_word,gen_color_recognition,gen_shape_recognition,lambda d,i:gen_count(d,i,5),gen_action],
2:[gen_color_object,lambda d,i:gen_count(d,i,10),gen_category,gen_action,gen_picture_to_word,gen_listen_find],
3:[gen_size_color,lambda d,i:gen_preposition(d,i,False),lambda d,i:gen_missing_letter(d,i,1),gen_category,gen_function],
4:[gen_size_color,lambda d,i:gen_count(d,i,10,True),gen_weather_clothes,gen_sentence_completion,gen_function],
5:[lambda d,i:gen_preposition(d,i,True),gen_sentence_completion,gen_compare,lambda d,i:gen_missing_letter(d,i,1),gen_select_pair],
6:[gen_two_step,gen_word_picture_mismatch,lambda d,i:gen_can_cant(d,i,False),lambda d,i:gen_can_cant(d,i,True),lambda d,i:gen_missing_letter(d,i,1)],
7:[lambda d,i:gen_story(d,i,1),lambda d,i:gen_multi_clue(d,i,False),gen_sentence_order,gen_select_pair,gen_two_step],
8:[lambda d,i:gen_story(d,i,2),gen_sentence_order,lambda d,i:gen_missing_letter(d,i,2),lambda d,i:gen_can_cant(d,i,True),lambda d,i:gen_multi_clue(d,i,False)],
9:[lambda d,i:gen_story(d,i,3),gen_memory,lambda d,i:gen_multi_clue(d,i,True),gen_two_step,gen_inference],
10:[gen_memory,lambda d,i:gen_multi_clue(d,i,True),gen_sentence_order,lambda d,i:gen_story(d,i,3),gen_final_reasoning],
}

questions=[]
seen=set()
for diff in range(1,11):
    print('Generating difficulty', diff, flush=True)
    gens=GENS_BY_DIFF[diff]
    attempts=0
    while sum(1 for q in questions if q['difficulty']==diff) < 100:
        idx=sum(1 for q in questions if q['difficulty']==diff)
        gen=gens[idx % len(gens)]
        q=gen(diff,idx)
        key=(q['questionType'],q['promptText'],json.dumps(q['stimulus'],sort_keys=True),json.dumps(q['options'],sort_keys=True))
        attempts+=1
        if key in seen:
            if attempts>10000: raise RuntimeError(f'too many duplicates diff {diff}')
            continue
        seen.add(key)
        q['id']=f'GB-{len(questions)+1:04d}'
        questions.append(q)

# canonical validation
assert len(questions)==1000
assert len({q['id'] for q in questions})==1000
assert Counter(q['difficulty'] for q in questions)==Counter({i:100 for i in range(1,11)})
for q in questions:
    assert q['answer']['type'] in {'option','sequence'}
    if q['answer']['type']=='option':
        ids={o['id'] for o in q['options']}
        assert q['answer']['value'] in ids, q['id']

# write JSON
with open(DATA/'golden_bell_questions_1000.json','w',encoding='utf-8') as f:
    json.dump({'version':'1.0','count':len(questions),'questions':questions},f,ensure_ascii=False,indent=2)

# CSV flattened
fields=['id','difficulty','questionType','mechanic','skill','promptText','audioText','optionA','optionB','optionC','optionD','correctAnswer','stimulusJson','optionsJson','answerJson','hintJson','explanation','targetVocabulary','worldTags','estimatedSeconds','requiresMemoryPhase','notes']
with open(DATA/'golden_bell_questions_1000.csv','w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=fields); w.writeheader()
    for q in questions:
        om={o.get('id'):o.get('label','') for o in q['options']}
        if q['answer']['type']=='option':
            correct_display=om.get(q['answer']['value'],'')
        else:
            correct_display=' '.join(q['answer']['value'])
        w.writerow({
            'id':q['id'],'difficulty':q['difficulty'],'questionType':q['questionType'],'mechanic':q['mechanic'],'skill':q['skill'],
            'promptText':q['promptText'],'audioText':q['audioText'],'optionA':om.get('A',''),'optionB':om.get('B',''),'optionC':om.get('C',''),'optionD':om.get('D',''),'correctAnswer':correct_display,
            'stimulusJson':json.dumps(q['stimulus'],ensure_ascii=False),'optionsJson':json.dumps(q['options'],ensure_ascii=False),'answerJson':json.dumps(q['answer'],ensure_ascii=False),
            'hintJson':json.dumps(q['hint'],ensure_ascii=False),'explanation':q['explanation'],'targetVocabulary':'|'.join(q['targetVocabulary']),
            'worldTags':'|'.join(q['worldTags']),'estimatedSeconds':q['estimatedSeconds'],'requiresMemoryPhase':q['requiresMemoryPhase'],'notes':q['notes']
        })

# schema
schema={
  '$schema':'https://json-schema.org/draft/2020-12/schema',
  'title':'Wordy Wings Golden Bell Question',
  'type':'object','required':['id','difficulty','questionType','mechanic','skill','promptText','audioText','stimulus','options','answer','hint','targetVocabulary','worldTags'],
  'properties':{
    'id':{'type':'string','pattern':'^GB-[0-9]{4}$'},
    'difficulty':{'type':'integer','minimum':1,'maximum':10},
    'questionType':{'type':'string'},'mechanic':{'type':'string'},'skill':{'type':'string'},
    'promptText':{'type':'string','minLength':1},'audioText':{'type':'string'},
    'stimulus':{'type':'object'},'options':{'type':'array'},'answer':{'type':'object'},'hint':{'type':'object'},
    'explanation':{'type':'string'},'targetVocabulary':{'type':'array','items':{'type':'string'}},'worldTags':{'type':'array','items':{'type':'string'}},
    'estimatedSeconds':{'type':'integer','minimum':5,'maximum':60},'requiresMemoryPhase':{'type':'boolean'},'notes':{'type':'string'}
  }
}
with open(DATA/'question_schema.json','w',encoding='utf-8') as f: json.dump(schema,f,indent=2)

# question type catalog
qtypes=[]
for t,c in sorted(Counter(q['questionType'] for q in questions).items()):
    qtypes.append({'questionType':t,'count':c,'difficulties':sorted(set(q['difficulty'] for q in questions if q['questionType']==t)),'mechanics':sorted(set(q['mechanic'] for q in questions if q['questionType']==t))})
with open(DATA/'question_types.json','w',encoding='utf-8') as f: json.dump(qtypes,f,indent=2)

# sample sessions: 30 sessions, each 12 questions progressive
bydiff=defaultdict(list)
for q in questions: bydiff[q['difficulty']].append(q['id'])
for d in bydiff: random.shuffle(bydiff[d])
ptr=defaultdict(int)
profiles=[[1,1,2,2,3,4,5,6,7,8,9,10],[1,2,2,3,3,4,5,6,7,8,9,10],[2,2,3,3,4,4,5,6,7,8,9,10]]
sessions=[]
for s in range(30):
    prof=profiles[s%len(profiles)]
    ids=[]
    for d in prof:
        ids.append(bydiff[d][ptr[d]%len(bydiff[d])]); ptr[d]+=1
    sessions.append({'id':f'GBS-{s+1:03d}','questionIds':ids,'difficultyProfile':prof,'title':f'Golden Bell Challenge {s+1}'})
with open(DATA/'sample_sessions_30.json','w',encoding='utf-8') as f: json.dump(sessions,f,indent=2)

# XLSX review file (no derived formulas, only data + field guide)
wb=Workbook(); ws=wb.active; ws.title='Questions'
headers=['ID','Difficulty','Type','Mechanic','Skill','Prompt','Audio','A','B','C','D','Correct Answer','Stimulus JSON','Options JSON','Answer JSON','Hint JSON','Explanation','Vocabulary','World Tags','Seconds','Memory Phase']
ws.append(headers)
header_fill=PatternFill('solid',fgColor='203864'); header_font=Font(color='FFFFFF',bold=True)
for cell in ws[1]: cell.fill=header_fill; cell.font=header_font; cell.alignment=Alignment(horizontal='center',vertical='center',wrap_text=True)
for q in questions:
    om={o.get('id'):o.get('label','') for o in q['options']}
    correct_display=om.get(q['answer'].get('value'),'') if q['answer']['type']=='option' else ' '.join(q['answer']['value'])
    ws.append([q['id'],q['difficulty'],q['questionType'],q['mechanic'],q['skill'],q['promptText'],q['audioText'],om.get('A',''),om.get('B',''),om.get('C',''),om.get('D',''),correct_display,json.dumps(q['stimulus'],ensure_ascii=False),json.dumps(q['options'],ensure_ascii=False),json.dumps(q['answer'],ensure_ascii=False),json.dumps(q['hint'],ensure_ascii=False),q['explanation'],', '.join(q['targetVocabulary']),', '.join(q['worldTags']),q['estimatedSeconds'],q['requiresMemoryPhase']])
ws.freeze_panes='A2'; ws.auto_filter.ref=ws.dimensions
widths=[12,10,24,18,18,42,42,22,22,22,22,28,48,60,28,38,42,36,24,10,12]
for i,wid in enumerate(widths,1): ws.column_dimensions[get_column_letter(i)].width=wid
for row in ws.iter_rows(min_row=2):
    for c in row: c.alignment=Alignment(vertical='top',wrap_text=True)
# difficulty color bands subtle
fills={1:'E2F0D9',2:'E2F0D9',3:'FFF2CC',4:'FFF2CC',5:'FCE4D6',6:'FCE4D6',7:'DDEBF7',8:'DDEBF7',9:'E4DFEC',10:'E4DFEC'}
for r in range(2,ws.max_row+1): ws.cell(r,2).fill=PatternFill('solid',fgColor=fills[ws.cell(r,2).value])

g=wb.create_sheet('Field Guide')
guide_rows=[
['Field','Meaning'],['Difficulty','1–10. 1 = recognition; 10 = multi-clue / memory / reasoning.'],['QuestionType','Reusable content template / renderer contract.'],['Mechanic','Phaser interaction pattern: answer_zone, raise_board, listen_run, quick_match, bell_choice.'],['Stimulus JSON','Scene or image/audio data needed to render the question.'],['Options JSON','Selectable options. Some sequence questions have no options and use tiles.'],['Answer JSON','Canonical answer. option = option id; sequence = ordered word list.'],['Hint JSON','When and how to give a non-punitive hint.'],['Memory Phase','If TRUE, show the scene briefly, hide it, then ask.']]
for r in guide_rows: g.append(r)
for c in g[1]: c.fill=header_fill; c.font=header_font
g.column_dimensions['A'].width=22; g.column_dimensions['B'].width=90
for row in g.iter_rows():
    for c in row: c.alignment=Alignment(vertical='top',wrap_text=True)
wb.save(DATA/'golden_bell_questions_1000.xlsx')

# docs strings
README = '''# Wordy Wings — Golden Bell Challenge Pack\n\nBộ này chứa **1000 câu hỏi** tăng dần từ Difficulty 1 đến 10, cùng specification để Codex thêm game mode Golden Bell Challenge vào dự án Wordy Wings hiện tại.\n\n## File quan trọng\n\n- `data/golden_bell_questions_1000.json` — nguồn dữ liệu canonical cho code/seed.\n- `data/golden_bell_questions_1000.csv` — dễ review/filter.\n- `data/golden_bell_questions_1000.xlsx` — bản review bằng Excel.\n- `data/question_schema.json` — schema dữ liệu.\n- `data/sample_sessions_30.json` — 30 lượt chơi mẫu, mỗi lượt 12 câu tăng khó.\n- `docs/01_GAME_MODE_GDD.md` — cốt truyện, gameplay loop, progression.\n- `docs/02_DIFFICULTY_CONTENT_SYSTEM.md` — hệ độ khó 1–10.\n- `docs/03_QUESTION_RENDERERS.md` — cách render từng dạng câu hỏi.\n- `backend/ABP_BACKEND_SPEC.md` — entity/API/seed/import.\n- `frontend/PHASER_IMPLEMENTATION_SPEC.md` — scene/state/event architecture.\n- `prompts/CODEX_MASTER_PROMPT.md` — prompt chính để đưa cho Codex.\n- `qa/ACCEPTANCE_CRITERIA.md` — tiêu chí nghiệm thu.\n- `qa/VALIDATION_REPORT.md` — kiểm tra dữ liệu.\n\n## Nguyên tắc\n\n- Không sao chép branding/chương trình truyền hình. “Golden Bell” chỉ là mechanic quiz theo chặng với payoff rung chuông.\n- Không loại trẻ khỏi game khi sai. Sai = feedback nhẹ + second chance + hint.\n- Câu hỏi cuối khó hơn nhờ **nhiều điều kiện**, không nhờ từ vựng vượt quá lứa tuổi.\n- Nội dung tăng dần từ recognition → 2 clues → sentence comprehension → memory → multi-step reasoning.\n'''
(ROOT/'README.md').write_text(README,encoding='utf-8')

GDD='''# 01 — Golden Bell Challenge: Game Design Document\n\n## 1. Vai trò trong Wordy Wings\nGolden Bell Challenge là **màn ôn tập/boss stage** xuất hiện sau mỗi 1–2 World. Bé cùng Poki, Lulu, Foxy, Pip và Momo đi tới Bell Island để đánh thức **Star Bell**. Mỗi câu đúng nạp một Word Star vào chuông. Khi đủ năng lượng, bé trực tiếp kéo dây hoặc chạm búa để rung chuông.\n\n## 2. Cốt truyện ngắn\nMr. Mumble làm Star Bell mất tiếng. Lulu phát hiện chuông chỉ thức dậy khi thu đủ Word Stars. Cả đội bước vào thử thách gồm 12 câu. Sau mỗi checkpoint, một đoạn đường lên tháp chuông sáng lên. Câu 12 là Final Bell.\n\n## 3. Session chuẩn — 12 câu\n- Q1–2: Difficulty 1–2 — warm up.\n- Q3–4: Difficulty 2–3 — recognition + 1 thuộc tính.\n- Q5–6: Difficulty 4–5 — 2 clues / sentence.\n- Q7–8: Difficulty 6–7 — two-step / mismatch / short story.\n- Q9–10: Difficulty 8 — negative/detail/spelling 2 blanks.\n- Q11: Difficulty 9 — memory hoặc 3 facts.\n- Q12: Difficulty 10 — Final Bell, nhiều điều kiện hoặc reasoning nhẹ.\n\n## 4. Gameplay loop\n1. Camera pan tới Bell Arena.\n2. Pip/Lulu giới thiệu câu bằng audio.\n3. Stimulus xuất hiện.\n4. Bé trả lời bằng mechanic phù hợp.\n5. Đúng: Word Star bay vào chuông, đường sáng thêm một đoạn, nhân vật phản ứng.\n6. Sai: bounce/shake nhẹ, “Almost!”, không mất mạng. Sau 2 sai hoặc chờ lâu, hint.\n7. Checkpoint ở Q4 và Q8: mini celebration 2–3 giây.\n8. Q12 xong: chuông đầy năng lượng → kéo dây/chạm búa → big payoff.\n\n## 5. Mechanics sử dụng\n- `answer_zone`: chạm/vào khu vực A/B/C/D.\n- `raise_board`: chọn đáp án rồi nhân vật giơ bảng.\n- `listen_run`: nghe lệnh, nhân vật chạy tới mục tiêu.\n- `quick_match`: ghép cặp / xếp từ nhanh.\n- `bell_choice`: lựa chọn 2–4 đáp án lớn.\n\n## 6. Không Game Over\nMục tiêu là kiểm tra và củng cố. Không dùng lives, red X lớn, loại khỏi sàn, hay timer gây áp lực. Sau sai lần 1 chỉ feedback. Sai lần 2: hint nhẹ. Sai lần 3: visual guide rõ hơn nhưng vẫn để trẻ tự chạm đáp án.\n\n## 7. Reward\n- 1 Star Energy cho mỗi câu hoàn thành.\n- 3 cosmetic sparkles cho streak 3/6/9 câu.\n- Bell Token khi hoàn thành session.\n- Từ hay nhầm được đưa vào `Review Queue`.\n\n## 8. Thời lượng\nMột session: 5–8 phút. Q1–6 khoảng 8–14 giây/câu, Q7–12 khoảng 14–25 giây/câu.\n'''
(DOCS/'01_GAME_MODE_GDD.md').write_text(GDD,encoding='utf-8')

DIFF='''# 02 — Difficulty & Content System\n\n## Difficulty 1\nNhận biết trực tiếp: nghe từ chọn hình, nhìn hình chọn từ, màu, shape, đếm 1–5.\n\n## Difficulty 2\nThêm object + color, category cơ bản, đếm tới 10, action.\n\n## Difficulty 3\nHai thuộc tính đơn giản; in/on/under; missing letter 1 ký tự; odd-one-out; function.\n\n## Difficulty 4\nColor + count, weather/clothes, sentence completion, function trong ngữ cảnh.\n\n## Difficulty 5\nnext to/behind/in front of/between; more/fewer; sentence; spelling; select pair.\n\n## Difficulty 6\nTwo-step instruction; word-picture mismatch; can/cannot; spelling từ dài.\n\n## Difficulty 7\nShort story 1–2 facts; multi-clue scene; sentence order; sequence listening.\n\n## Difficulty 8\nStory 2–3 facts; 2 missing letters; negative detail; multi-clue.\n\n## Difficulty 9\nMemory scene; story 3+ facts; inference nhẹ; 4-clue location; two-step.\n\n## Difficulty 10\nFinal Bell: memory + 3/4 clues, sentence assembly, simple inference, story detail.\n\n## Quy tắc tăng khó\nƯu tiên tăng **số điều kiện cần xử lý** hơn tăng độ hiếm của từ. Ví dụ:\n- D1: Find the star.\n- D3: Find the yellow star.\n- D5: Find the small yellow star.\n- D8: Find the small yellow star next to the blue circle.\n\n## Adaptive selection\n- 4 câu đúng liên tiếp: giữ vocabulary hiện tại nhưng tăng distractor quality hoặc +1 difficulty tối đa bằng mức đã unlock.\n- 2 sai trong 3 câu: giảm 1 difficulty cho câu tiếp theo và ưu tiên loại câu cùng kỹ năng để luyện lại.\n- Một từ sai 2 lần trong session: thêm vào Review Queue, không hỏi lại ngay trong 2 câu kế tiếp.\n- Không dùng cùng `questionType` quá 2 câu liên tiếp.\n'''
(DOCS/'02_DIFFICULTY_CONTENT_SYSTEM.md').write_text(DIFF,encoding='utf-8')

RENDER='''# 03 — Question Renderers\n\n## Renderer contract\nMỗi question phải được render từ dữ liệu, không hard-code theo ID. `questionType` chọn renderer; `mechanic` chọn kiểu tương tác.\n\n### listen_find_picture / picture_to_word\n4 đáp án hình hoặc chữ lớn; target >= 88px; audio replay bằng nút loa.\n\n### color_object / two_attribute_object\nTất cả đáp án cùng nhóm để distractor có ý nghĩa. Không highlight đáp án đúng trước.\n\n### preposition_scene / multi_clue_scene\nRender 3–4 mini-scene có cùng asset nhưng khác relation/attribute.\n\n### missing_letter\nWord lớn giữa màn. Một hoặc hai blank. Sau đúng, đọc từng chữ rồi cả từ.\n\n### two_step_instruction\nĐáp án được xử lý theo sequence. UI giữ dấu check nhỏ cho step 1 rồi chờ step 2.\n\n### short_story\nStory audio 1 lần tự động; replay tối đa tự do. Question xuất hiện sau story.\n\n### memory_scene\nShow scene 4–5 giây → fade → hỏi. Nếu sai, cho xem lại 1 lần như hint.\n\n### sentence_order\nTile chữ lớn. Hỗ trợ drag và tap-to-slot. Không cần exact drag.\n\n### word_picture_mismatch\n4 cặp hình + chữ. Bé tìm 1 cặp sai.\n\n## Feedback\n- Correct: bounce + sparkle + voice “Great!” + answer audio.\n- Wrong: soft shake 250ms + “Almost!”; không đỏ chói.\n- Hint: sau 2 sai hoặc timeout mềm 12–18s tùy difficulty.\n'''
(DOCS/'03_QUESTION_RENDERERS.md').write_text(RENDER,encoding='utf-8')

AUTHOR='''# 04 — Content Authoring Rules\n\n1. Mỗi câu chỉ có một mục tiêu học chính.\n2. Distractor phải cùng miền nghĩa khi có thể. Ví dụ hỏi màu thì dùng 4 màu, không trộn màu với con vật.\n3. Negative question (`NOT`, `CANNOT`) chỉ dùng từ Difficulty 6 trở lên và audio phải nhấn từ phủ định.\n4. Không dùng mẹo đánh đố. Khó = nhiều clue hơn, không phải wording mơ hồ.\n5. Story tối đa 2–4 câu ngắn.\n6. Memory scene chỉ giữ 3–4 object quan trọng.\n7. Câu cuối có thể 4 clues nhưng vẫn dùng từ quen thuộc.\n8. Không dùng timer loại người chơi.\n9. Asset keys phải đi qua mapping layer, không hard-code file path trong content.\n10. Mỗi question phải chạy được bằng touch và mouse.\n'''
(DOCS/'04_CONTENT_AUTHORING_RULES.md').write_text(AUTHOR,encoding='utf-8')

ABP='''# ABP Backend Spec — Golden Bell\n\n## Module\nKhông tách microservice mới ở MVP. Thêm module/application service vào backend ABP hiện tại.\n\n## Entities đề xuất\n### GoldenBellQuestion\n- Id (Guid)\n- Code (`GB-0001`) unique\n- Difficulty (int 1..10)\n- QuestionType\n- Mechanic\n- Skill\n- PromptText\n- AudioText\n- StimulusJson\n- OptionsJson\n- AnswerJson\n- HintJson\n- Explanation\n- TargetVocabularyJson\n- WorldTagsJson\n- EstimatedSeconds\n- RequiresMemoryPhase\n- IsActive\n- Version\n\n### GoldenBellSession\n- Id\n- ChildProfileId\n- StartedAt / CompletedAt\n- CurrentQuestionIndex\n- DifficultyProfileJson\n- TotalCorrect\n- TotalWrongAttempts\n- BellRung\n\n### GoldenBellAttempt\n- SessionId\n- QuestionCode\n- AttemptCount\n- IsCorrect\n- HintUsed\n- DurationMs\n- SelectedAnswerJson\n- CreatedAt\n\n## API\n- `POST /api/game/golden-bell/session/start`\n- `GET /api/game/golden-bell/session/{id}`\n- `POST /api/game/golden-bell/session/{id}/answer`\n- `POST /api/game/golden-bell/session/{id}/complete`\n- `GET /api/game/golden-bell/questions/{code}` admin/debug\n- `POST /api/game/golden-bell/admin/import` admin only\n\n## Session start response\nServer chọn 12 câu hoặc trả seed + question payload. Client không được tự tin tưởng `correctAnswer` nếu leaderboard/reward có giá trị; MVP gia đình có thể gửi full payload để đơn giản.\n\n## Question selector\n1. Resolve unlocked vocabulary/worlds của child.\n2. Lấy difficulty profile.\n3. Không lặp questionType >2 lần liên tiếp.\n4. Không lặp targetVocabulary trong 2 câu kế tiếp, trừ review mode.\n5. Ưu tiên 20–30% câu từ Review Queue.\n6. Dùng deterministic seed để resume session không đổi bộ câu.\n\n## Seed/import\nCanonical source là `data/golden_bell_questions_1000.json`. Import phải upsert theo `Code`, validate JSON schema và transaction toàn bộ batch.\n'''
(BACKEND/'ABP_BACKEND_SPEC.md').write_text(ABP,encoding='utf-8')

PHASER='''# Phaser / React Implementation Spec\n\n## Boundary\nReact: route, auth, loading shell, parent dashboard, session summary.\nPhaser: Bell Arena, character animations, question renderers, input, audio, feedback, bell payoff.\n\n## Scene proposal\n- `GoldenBellBootScene` — preload minimal common assets.\n- `GoldenBellArenaScene` — persistent arena + characters + bell energy.\n- `GoldenBellQuestionLayer` — mounts/unmounts question renderer.\n- `GoldenBellCelebrationLayer` — checkpoints + final bell.\n\n## Services\n- `GoldenBellSessionController`\n- `QuestionRendererRegistry`\n- `GoldenBellAudioManager` (reuse existing AudioManager)\n- `HintController`\n- `AnswerEvaluator`\n- `BellEnergyController`\n- `GoldenBellEventBus`\n\n## Registry\n`questionType -> renderer`. Không switch khổng lồ trong scene. Mỗi renderer implement:\n- `mount(question, context)`\n- `submit(input)`\n- `showHint(level)`\n- `dispose()`\n\n## Events\n- GOLDEN_BELL_SESSION_STARTED\n- QUESTION_PRESENTED\n- ANSWER_SUBMITTED\n- ANSWER_CORRECT\n- ANSWER_INCORRECT\n- HINT_SHOWN\n- CHECKPOINT_REACHED\n- FINAL_BELL_READY\n- FINAL_BELL_RUNG\n- SESSION_COMPLETED\n\n## State machine\n`INTRO -> PRESENTING -> WAITING_INPUT -> FEEDBACK -> CHECKPOINT? -> NEXT -> FINAL_BELL -> SUMMARY`\n\n## Mobile\nLandscape-first. Khi viewport portrait: hiện rotate suggestion nhưng không block nếu vẫn đủ chiều rộng. Touch targets >= 48 CSS px, đáp án chính >= 72px.\n\n## Performance\nKhông preload 1000 câu/assets. Mỗi session preload common + assets của 12 câu. Dispose renderer và listeners giữa câu. Object pool cho sparkle/confetti.\n'''
(FRONTEND/'PHASER_IMPLEMENTATION_SPEC.md').write_text(PHASER,encoding='utf-8')

PROMPT='''# CODEX MASTER PROMPT — Add Golden Bell Challenge\n\nBạn đang làm project Wordy Wings hiện tại với ABP.io + React + TypeScript + Phaser + PostgreSQL. Hãy thêm game mode **Golden Bell Challenge** mà không rewrite architecture hiện tại.\n\n## Ground truth files\nĐọc trước:\n- `README.md`\n- `docs/01_GAME_MODE_GDD.md`\n- `docs/02_DIFFICULTY_CONTENT_SYSTEM.md`\n- `docs/03_QUESTION_RENDERERS.md`\n- `backend/ABP_BACKEND_SPEC.md`\n- `frontend/PHASER_IMPLEMENTATION_SPEC.md`\n- `data/question_schema.json`\n- `data/golden_bell_questions_1000.json`\n- `qa/ACCEPTANCE_CRITERIA.md`\n\n## Goal\nTạo một boss/review stage 12 câu, difficulty tăng dần, nhiều renderer khác nhau, kết thúc bằng việc bé trực tiếp rung Star Bell. Sai không loại khỏi game.\n\n## Trước khi code\n1. Inspect codebase hiện tại.\n2. Tìm GameContainer, Phaser bootstrap, SceneManager, AudioManager, progress API, ABP application services, localization và asset loader hiện tại.\n3. Liệt kê file sẽ reuse/refactor/new.\n4. Không đổi route/auth/progress hiện tại nếu không cần.\n\n## Backend\nImplement entity + application service tối thiểu theo `ABP_BACKEND_SPEC.md`. Tạo importer/upsert từ JSON. Thêm selector 12 câu với deterministic seed, difficulty profile và anti-repeat rules. Nếu MVP chưa cần lưu toàn bộ questions trong DB, vẫn tạo interface repository để sau này đổi từ JSON sang DB mà không thay frontend contract.\n\n## Frontend/Phaser\nImplement `GoldenBellArenaScene` + renderer registry. Bắt đầu với các renderer tối thiểu:\n- listen_find_picture / picture_to_word\n- color_object / two_attribute_object\n- count_objects\n- preposition_scene / multi_clue_scene\n- missing_letter\n- two_step_instruction\n- short_story\n- memory_scene\n- sentence_order\n- word_picture_mismatch\n\nKhông tạo scene riêng cho từng question.\n\n## UX\n- 16:9 landscape, responsive mobile.\n- Wordy Wings visual language hiện có.\n- Q1–Q12 tăng khó rõ ràng.\n- Đúng: star bay vào bell.\n- Sai: shake nhẹ, `Almost!`, không Game Over.\n- Hint sau 2 lần sai hoặc timeout mềm.\n- Q4/Q8 có checkpoint animation <= 3s.\n- Q12 xong: user phải kéo dây/chạm búa để rung chuông; không auto-finish.\n\n## Data\nCanonical bank là `data/golden_bell_questions_1000.json`. Không hard-code question text trong renderer. Validate duplicate IDs, difficulty 1..10, valid answers.\n\n## Audio\nReuse AudioManager. Queue audio để prompt/story/feedback không chồng nhau. Nút replay luôn khả dụng. Negative questions phải emphasize `NOT/CANNOT` bằng clip hoặc prosody metadata nếu asset có.\n\n## Analytics/progress\nMỗi câu log attempt count, correct, hint used, duration. Chỉ sync server theo answer/checkpoint hoặc session complete; không gửi event cosmetic.\n\n## Tests\n- unit: selector progression, anti-repeat, answer evaluator, duplicate-letter spelling, sequence answers.\n- integration: start -> answer 12 -> bell -> complete.\n- scene: no listener leaks after 30 questions; restart/resume stable.\n\n## Delivery\nCuối cùng báo:\n1. File changed.\n2. Architecture summary.\n3. Cách import 1000 questions.\n4. Cách thêm questionType mới.\n5. Cách tạo một question mới chỉ bằng JSON.\n6. Test commands và kết quả.\n7. Limitations còn lại.\n'''
(PROMPTS/'CODEX_MASTER_PROMPT.md').write_text(PROMPT,encoding='utf-8')

GENPROMPT='''# Prompt tạo thêm câu hỏi sau này\n\nTạo thêm N câu hỏi cho Wordy Wings Golden Bell Challenge. Tuân thủ `data/question_schema.json` và `docs/04_CONTENT_AUTHORING_RULES.md`. Phân bố difficulty được chỉ định. Không trùng `promptText + stimulus + options` với bank hiện tại. Khó hơn bằng nhiều clue, memory, sequence hoặc sentence comprehension; không dùng từ quá hiếm. Output JSON array only. Sau khi tạo, validate answer tồn tại, options hợp lệ, negative wording rõ ràng và không có câu mơ hồ.\n'''
(PROMPTS/'QUESTION_GENERATION_PROMPT.md').write_text(GENPROMPT,encoding='utf-8')

AC='''# Acceptance Criteria — Golden Bell Challenge\n\n1. Start session trả đúng 12 câu theo profile tăng khó.\n2. Không questionType nào lặp >2 lần liên tiếp.\n3. Renderer hoàn toàn data-driven.\n4. Mouse/touch đều chơi được.\n5. Replay audio hoạt động ở mọi câu có audio.\n6. Sai không mất session/progress.\n7. Hint chỉ xuất hiện theo rule.\n8. Memory question có show/hide phase và replay hint tối đa theo config.\n9. Sequence question validate đúng order.\n10. Missing-letter support từ có chữ lặp.\n11. Q4/Q8 checkpoint không làm mất state.\n12. Q12 mở Final Bell, không tự complete.\n13. Bell interaction tạo final payoff và gửi complete một lần.\n14. Resume session giữ nguyên 12 câu và index.\n15. Resize desktop/mobile không crop content chính.\n16. Không duplicate listener/object sau 3 lần restart.\n17. 1000-question import idempotent.\n18. JSON invalid bị reject với message rõ.\n19. Analytics attempt/duration/hint chính xác.\n20. Existing Wordy Wings modes không regression.\n'''
(QA/'ACCEPTANCE_CRITERIA.md').write_text(AC,encoding='utf-8')

# validation report
counts_type=Counter(q['questionType'] for q in questions)
counts_mech=Counter(q['mechanic'] for q in questions)
rep=['# Validation Report','',f'- Total questions: **{len(questions)}**',f'- Unique IDs: **{len({q["id"] for q in questions})}**','- Difficulty distribution: **100 questions at each level 1–10**','- Invalid answer references: **0**','- Duplicate canonical question keys: **0**','', '## Question type distribution']
for k,v in sorted(counts_type.items()): rep.append(f'- `{k}`: {v}')
rep += ['', '## Mechanic distribution']
for k,v in sorted(counts_mech.items()): rep.append(f'- `{k}`: {v}')
rep += ['', '## Automated checks', '- All option-answer questions reference an existing option id.', '- All IDs follow `GB-0001` ... `GB-1000`.', '- Difficulty is always 1..10.', '- Canonical JSON/CSV/XLSX were generated from the same in-memory source.']
(QA/'VALIDATION_REPORT.md').write_text('\n'.join(rep),encoding='utf-8')

# manifest
manifest='''# Manifest\n\n- 1000-question canonical JSON\n- CSV review copy\n- XLSX review copy\n- JSON schema\n- Question type catalog\n- 30 sample 12-question sessions\n- Game mode GDD\n- Difficulty/content system\n- Renderer spec\n- Content authoring rules\n- ABP backend spec\n- Phaser implementation spec\n- Codex master prompt\n- Future question-generation prompt\n- Acceptance criteria\n- Validation report\n'''
(ROOT/'MANIFEST.md').write_text(manifest,encoding='utf-8')

print('Generated', len(questions), 'questions')
print('Type counts:', dict(sorted(counts_type.items())))
print('Mechanics:', dict(sorted(counts_mech.items())))
print('Output root:', ROOT)
