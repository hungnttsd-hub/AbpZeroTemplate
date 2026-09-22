# Validation Report

- Total questions: **1000**
- Unique IDs: **1000**
- Difficulty distribution: **100 questions at each level 1–10**
- Invalid answer references: **0**
- Duplicate canonical question keys: **0**

## Question type distribution
- `action_recognition`: 33
- `can_cannot`: 60
- `color_object`: 17
- `color_recognition`: 17
- `compare_quantity`: 20
- `count_objects`: 53
- `final_reasoning`: 20
- `function_question`: 40
- `listen_find_picture`: 33
- `memory_scene`: 40
- `missing_letter`: 80
- `multi_clue_scene`: 80
- `odd_one_out`: 37
- `picture_to_word`: 33
- `preposition_scene`: 40
- `select_pair`: 40
- `sentence_completion`: 40
- `sentence_order`: 60
- `shape_recognition`: 17
- `short_story`: 80
- `simple_inference`: 20
- `two_attribute_object`: 40
- `two_step_instruction`: 60
- `weather_choice`: 20
- `word_picture_mismatch`: 20

## Mechanic distribution
- `answer_zone`: 231
- `bell_choice`: 310
- `listen_run`: 126
- `quick_match`: 100
- `raise_board`: 233

## Automated checks
- All option-answer questions reference an existing option id.
- All IDs follow `GB-0001` ... `GB-1000`.
- Difficulty is always 1..10.
- Canonical JSON/CSV/XLSX were generated from the same in-memory source.