# GAME DESIGN DOCUMENT — WORDY WINGS

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

### 1. Word Shot (`word_shot`)
- **Core:** Nghe/đọc yêu cầu rồi kéo-thả ná để bắn đúng mục tiêu.
- **Kỹ năng:** listening, vocabulary, visual discrimination
- **Sai:** Bắn sai: mục tiêu đọc tên của nó, rung nhẹ; không mất mạng; cho thử lại.
- **Có trong MVP:** Có

### 2. Balloon Pop (`balloon_pop`)
- **Core:** Chạm/bắn bong bóng mang hình hoặc từ đúng trước khi chúng trôi khỏi vùng chơi.
- **Kỹ năng:** listening, rapid recognition, attention
- **Sai:** Chọn sai: bong bóng xẹp vui nhộn, đọc từ đúng của vật vừa chọn; câu hỏi được lặp lại.
- **Có trong MVP:** Có

### 3. Drag & Sort (`drag_sort`)
- **Core:** Kéo đồ vật vào giỏ/nhóm/địa điểm phù hợp.
- **Kỹ năng:** categorisation, prepositions, vocabulary
- **Sai:** Sai nhóm: vật bật trở lại vị trí cũ và có gợi ý bằng animation.
- **Có trong MVP:** Có

### 4. Letter Puzzle (`letter_puzzle`)
- **Core:** Ghép chữ cái hoặc âm tiết thành từ mục tiêu; có audio mẫu.
- **Kỹ năng:** spelling, letter recognition, phonological awareness
- **Sai:** Chữ sai chỉ rung, không khóa tiến trình; sau 2 lần sai highlight chữ đầu.
- **Có trong MVP:** Có

### 5. Rescue Mission (`rescue_mission`)
- **Core:** Tìm đúng nhân vật/đồ vật qua mô tả rồi giải cứu bằng chạm, đường đi hoặc thao tác vật lý.
- **Kỹ năng:** comprehension, adjectives, simple sentences
- **Sai:** Sai: narrator mô tả lại clue ngắn hơn, thêm highlight môi trường.
- **Có trong MVP:** Không

### 6. Adventure Commands (`adventure_commands`)
- **Core:** Điều khiển nhân vật theo lệnh: go left, jump, open, put in/on/under...
- **Kỹ năng:** commands, directions, prepositions
- **Sai:** Lệnh sai tạo animation vui, sau đó phát lại câu lệnh chậm hơn.
- **Có trong MVP:** Không

### 7. Boss Challenge (`boss_challenge`)
- **Core:** Chuỗi 4-6 thử thách tổng hợp cuối world, không có combat bạo lực; mục tiêu là lấy lại Word Star.
- **Kỹ năng:** review, mixed comprehension, recall
- **Sai:** Checkpoint theo từng câu; sai không reset toàn boss; hint tăng dần.
- **Có trong MVP:** Có


## 7. World map — 10 world / 200 level

### W01 — Rainbow Valley (Thung lũng Cầu Vồng)
- **Theme học:** colors, shapes, size, basic objects
- **Nhân vật dẫn:** Pip
- **Mỹ thuật:** sunny sky, rainbow, grass, crystals
- **Story goal:** Khôi phục 7 sắc màu bị Mr. Mumble hút khỏi cầu vồng.
- **Từ khóa trọng tâm:** red, blue, yellow, green, orange, pink, purple, circle, square, triangle, star, big, small, ball, box, kite, flower, black, white, brown
- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.
### W02 — Animal Island (Đảo Muông Thú)
- **Theme học:** animals, simple descriptions, habitats
- **Nhân vật dẫn:** Poki
- **Mỹ thuật:** jungle, waterfalls, bamboo, river
- **Story goal:** Giải cứu các bạn thú bị giấu nhãn tên trong các chiếc hộp mây.
- **Từ khóa trọng tâm:** cat, dog, bird, fish, duck, cow, horse, rabbit, frog, monkey, tiger, lion, elephant, giraffe, crocodile, snake, bee, butterfly, bear, mouse
- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.
### W03 — Happy Home (Ngôi Nhà Vui Vẻ)
- **Theme học:** rooms, furniture, toys, prepositions
- **Nhân vật dẫn:** Momo
- **Mỹ thuật:** cozy home, garden, warm wood, pastel textiles
- **Story goal:** Giúp Momo sắp xếp lại ngôi nhà sau cơn gió làm đồ vật bay khắp nơi.
- **Từ khóa trọng tâm:** bed, chair, table, sofa, lamp, clock, mirror, door, window, toy, doll, teddy, book, box, kitchen, bedroom, bathroom, garden, in, under
- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.
### W04 — Yummy Land (Xứ Sở Ngon Lành)
- **Theme học:** food, drink, likes, quantities
- **Nhân vật dẫn:** Momo
- **Mỹ thuật:** fruit orchards, bakery, milk river, picnic
- **Story goal:** Nấu bữa tiệc lớn để đánh thức Word Tree đang đói.
- **Từ khóa trọng tâm:** apple, banana, orange, pear, grape, watermelon, mango, pineapple, strawberry, bread, cake, egg, rice, cheese, sandwich, ice cream, water, milk, juice, hungry
- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.
### W05 — Magic School (Trường Học Phép Thuật)
- **Theme học:** school objects, classroom actions, letters
- **Nhân vật dẫn:** Lulu
- **Mỹ thuật:** treehouse school, books, pencils, art room
- **Story goal:** Sửa cỗ máy chữ cái của Lulu để mở thư viện Word Star.
- **Từ khóa trọng tâm:** teacher, student, desk, board, book, notebook, pen, pencil, ruler, eraser, bag, paper, read, write, draw, colour, listen, open, close, spell
- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.
### W06 — Body & Action Jungle (Rừng Vận Động)
- **Theme học:** body, clothes, actions, abilities
- **Nhân vật dẫn:** Foxy
- **Mỹ thuật:** jungle obstacle course, vines, bridges
- **Story goal:** Vượt giải hội thao rừng và thu hồi các Action Stars.
- **Từ khóa trọng tâm:** head, hair, eye, ear, nose, mouth, arm, hand, finger, leg, foot, run, walk, jump, swim, dance, clap, wave, shirt, hat
- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.
### W07 — Adventure City (Thành Phố Phiêu Lưu)
- **Theme học:** transport, places, directions, position
- **Nhân vật dẫn:** Foxy
- **Mỹ thuật:** toy city, canal, station, airport, roads
- **Story goal:** Tìm đường qua thành phố để giao 5 bưu kiện chứa Lost Words.
- **Từ khóa trọng tâm:** car, bus, bike, train, plane, boat, street, park, shop, station, airport, left, right, up, down, straight, near, between, behind, zoo
- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.
### W08 — Weather Kingdom (Vương Quốc Thời Tiết)
- **Theme học:** weather, clothes, hot/cold, day/night
- **Nhân vật dẫn:** Lulu
- **Mỹ thuật:** floating weather islands, windmills, clouds
- **Story goal:** Sửa Weather Wheel để mỗi vùng có lại thời tiết đúng.
- **Từ khóa trọng tâm:** sun, sunny, rain, rainy, cloud, cloudy, wind, windy, snow, hot, cold, warm, day, night, morning, umbrella, coat, boots, sky, tree
- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.
### W09 — Space Numbers (Không Gian Con Số)
- **Theme học:** numbers 1-20, counting, quantity, comparisons
- **Nhân vật dẫn:** Pip
- **Mỹ thuật:** moon islands, rockets, stars, nebula
- **Story goal:** Thu thập 20 Number Stars để khởi động tên lửa trở về Story Castle.
- **Từ khóa trọng tâm:** one, two, three, four, five, six, seven, eight, nine, ten, eleven, twelve, thirteen, fourteen, fifteen, sixteen, seventeen, eighteen, nineteen, twenty
- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.
### W10 — Story Castle (Lâu Đài Câu Chuyện)
- **Theme học:** family, daily life, simple sentences, integrated review
- **Nhân vật dẫn:** All
- **Mỹ thuật:** storybook castle, floating pages, sunset, portals
- **Story goal:** Ghép lại cuốn Great Word Book và giúp Mr. Mumble hiểu rằng từ ngữ vui hơn khi được chia sẻ.
- **Từ khóa trọng tâm:** mother, father, brother, sister, baby, friend, boy, girl, wake up, wash, eat, go, play, read, sleep, what, where, who, my, your
- **Cấu trúc 20 màn:** 1–5 nhận diện; 6–10 củng cố + mini boss; 11–15 cụm từ/câu ngắn; 16–19 mixed challenge; 20 boss tổng kết.


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
