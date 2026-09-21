# Wordy Wings – Game Design & MVP Build Pack

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
