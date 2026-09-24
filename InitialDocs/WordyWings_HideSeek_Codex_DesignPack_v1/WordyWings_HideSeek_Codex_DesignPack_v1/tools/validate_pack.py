#!/usr/bin/env python3
"""Validate this handoff's content and references, not a running Phaser application."""
from pathlib import Path
import json
import sys
from PIL import Image
from jsonschema import Draft202012Validator

ROOT = Path(__file__).resolve().parents[1]

def load(path: str):
    return json.loads((ROOT/path).read_text(encoding='utf-8'))

def check(condition: bool, message: str):
    if not condition:
        raise ValueError(message)

def main():
    pack = load('data/hide-seek-levels.json')
    schema = load('data/hide-seek.schema.json')
    Draft202012Validator.check_schema(schema)
    Draft202012Validator(schema).validate(pack)
    entities = pack['entities']
    levels = pack['levels']
    ids = {e['id'] for e in entities}
    check(len(ids) == len(entities), 'Duplicate vocabulary ids')
    check(len({l['id'] for l in levels}) == len(levels), 'Duplicate level ids')
    for level in levels:
        spots = level['spots']
        check(len({s['id'] for s in spots}) == len(spots), f'Duplicate spots: {level["id"]}')
        check(sum(s['entityId'] == level['targetEntityId'] for s in spots) == 1,
              f'Exactly one target required: {level["id"]}')
        check(level['targetEntityId'] in ids, 'Unknown target')
        for spot in spots:
            check(spot['entityId'] in ids, 'Unknown occupant')
            check(bool(set(spot['allowedTools']) & set(level['tools'])), 'Unreachable spot')
            rect = spot['anchor']
            check(rect['x'] + rect['width'] <= 1600, 'Spot outside horizontal bounds')
            check(rect['y'] + rect['height'] <= 900, 'Spot outside vertical bounds')
    scissors = next(e for e in entities if e['id'] == 'scissors')
    check(scissors['question'] == 'Are these scissors?', 'Plural question mismatch')
    for id_ in ['apple','orange']:
        e = next(e for e in entities if e['id'] == id_)
        check(e['question'] == f'Is this an {id_}?', 'Article mismatch')
    image_map = load('design/image-map.json')
    refs = {x['id'] for x in image_map['crops'] + image_map['plates']} | {'A00'}
    for item in image_map['crops'] + image_map['plates']:
        path = ROOT/item['file']
        check(path.is_file(), f'Missing image: {path}')
        with Image.open(path) as im:
            im.verify()
        if 'source' in item:
            check((ROOT/item['source']).is_file(), f'Missing source: {item["source"]}')
        if 'sourceBoxPx' in item:
            x0,y0,x1,y1 = item['sourceBoxPx']
            w,h = item['sourceSize']
            check(0 <= x0 < x1 <= w and 0 <= y0 < y1 <= h, 'Crop outside source')
        if 'references' in item:
            check(set(item['references']) <= refs, 'Unknown detail reference')
    audio = load('data/audio-manifest.json')
    check(len({a['id'] for a in audio}) == len(audio), 'Duplicate audio ids')
    check(all(a['fileIncluded'] is False for a in audio), 'Audio availability incorrectly declared')
    assets = load('design/runtime-asset-checklist.json')
    check(len({a['key'] for a in assets}) == len(assets), 'Duplicate asset keys')
    check(all(a['runtimeFileIncluded'] is False for a in assets), 'Unexpected runtime asset claim')
    keys = {a['key'] for a in assets}
    check(all(e['assetKey'] in keys for e in entities), 'Vocabulary asset missing from production list')
    check(all(s['coverAssetKey'] in keys for l in levels for s in l['spots']), 'Cover missing from production list')
    check(all(set(a['references']) <= refs for a in assets), 'Unknown production reference')
    for path in ['README.md','START_HERE_CODEX.md','OPEN_DESIGN_GALLERY.html',
                 'design/01_VISUAL_SPEC.md','design/02_GAMEPLAY_RULES.md',
                 'engineering/contracts.ts','engineering/rules.ts','engineering/SOURCES.md',
                 'qa/ACCEPTANCE_CRITERIA.md','qa/rules.test.ts']:
        check((ROOT/path).is_file(), f'Missing required file: {path}')
    check(not any(p.suffix.lower() in {'.ttf','.otf','.woff','.woff2'} for p in ROOT.rglob('*') if p.is_file()),
          'Font files must not be bundled')
    report = {'ok':True, 'levelCount':len(levels), 'vocabularyEntityCount':len(entities),
              'detailedImages':len(image_map['crops']), 'annotationImages':len(image_map['plates'])+1,
              'contactSheets':len(image_map['indexes']), 'audioScripts':len(audio),
              'runtimeAssetSlotsRequiringProduction':len(assets),
              'testedScope':'Schema, content cross-references, reachable spots, image presence/crop bounds, declared availability.',
              'notTested':'Live game rendering, actual input, ABP endpoints, offline sync, device performance, audio playback.'}
    print(json.dumps(report,indent=2))
    if '--write-report' in sys.argv:
        (ROOT/'qa/CONTENT_VALIDATION.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')

if __name__ == '__main__':
    main()
