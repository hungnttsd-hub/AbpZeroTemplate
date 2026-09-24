# ABP DOMAIN MODEL

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
