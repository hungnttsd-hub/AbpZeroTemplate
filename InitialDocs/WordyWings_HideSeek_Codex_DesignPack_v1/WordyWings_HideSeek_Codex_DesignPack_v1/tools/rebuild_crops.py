#!/usr/bin/env python3
"""Recreate exact unmodified source crops from image-map.json. Does not generate art."""
from pathlib import Path
import json
from PIL import Image
root=Path(__file__).resolve().parents[1]
image_map=json.loads((root/'design/image-map.json').read_text(encoding='utf-8'))
count=0
for record in image_map['crops']:
    if 'sourceBoxPx' not in record:
        continue  # D36 contains an annotation; keep its supplied file.
    with Image.open(root/record['source']) as source:
        out=root/record['file']
        out.parent.mkdir(parents=True,exist_ok=True)
        source.crop(tuple(record['sourceBoxPx'])).save(out)
    count+=1
print(f'Rebuilt {count} exact native-resolution source crops. Annotation plates were not changed.')
