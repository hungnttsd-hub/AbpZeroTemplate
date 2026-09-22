# ABP Backend Spec — Golden Bell

## Module
Không tách microservice mới ở MVP. Thêm module/application service vào backend ABP hiện tại.

## Entities đề xuất
### GoldenBellQuestion
- Id (Guid)
- Code (`GB-0001`) unique
- Difficulty (int 1..10)
- QuestionType
- Mechanic
- Skill
- PromptText
- AudioText
- StimulusJson
- OptionsJson
- AnswerJson
- HintJson
- Explanation
- TargetVocabularyJson
- WorldTagsJson
- EstimatedSeconds
- RequiresMemoryPhase
- IsActive
- Version

### GoldenBellSession
- Id
- ChildProfileId
- StartedAt / CompletedAt
- CurrentQuestionIndex
- DifficultyProfileJson
- TotalCorrect
- TotalWrongAttempts
- BellRung

### GoldenBellAttempt
- SessionId
- QuestionCode
- AttemptCount
- IsCorrect
- HintUsed
- DurationMs
- SelectedAnswerJson
- CreatedAt

## API
- `POST /api/game/golden-bell/session/start`
- `GET /api/game/golden-bell/session/{id}`
- `POST /api/game/golden-bell/session/{id}/answer`
- `POST /api/game/golden-bell/session/{id}/complete`
- `GET /api/game/golden-bell/questions/{code}` admin/debug
- `POST /api/game/golden-bell/admin/import` admin only

## Session start response
Server chọn 12 câu hoặc trả seed + question payload. Client không được tự tin tưởng `correctAnswer` nếu leaderboard/reward có giá trị; MVP gia đình có thể gửi full payload để đơn giản.

## Question selector
1. Resolve unlocked vocabulary/worlds của child.
2. Lấy difficulty profile.
3. Không lặp questionType >2 lần liên tiếp.
4. Không lặp targetVocabulary trong 2 câu kế tiếp, trừ review mode.
5. Ưu tiên 20–30% câu từ Review Queue.
6. Dùng deterministic seed để resume session không đổi bộ câu.

## Seed/import
Canonical source là `data/golden_bell_questions_1000.json`. Import phải upsert theo `Code`, validate JSON schema và transaction toàn bộ batch.
