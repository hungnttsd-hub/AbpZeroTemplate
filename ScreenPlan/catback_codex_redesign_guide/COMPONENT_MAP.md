# COMPONENT MAP

Gợi ý component hoá để Codex triển khai.

```text
CatBackLayout
├── Sidebar
│   ├── Brand
│   ├── NavigationMenu
│   ├── SupportCard
│   └── UserMiniProfile (optional)
├── Topbar
│   ├── SearchBox
│   ├── NotificationButton
│   └── UserMenu
└── HomePage
    ├── HeroSection
    ├── MainGrid
    │   ├── LinkGeneratorCard
    │   └── WalletSummaryCard
    ├── BenefitStrip
    └── ContentGrid
        ├── RecentOrdersCard
        └── QuickGuideCard
```

## Shared components
- `AppCard`
- `AppButton`
- `StatusBadge`
- `SectionHeader`
- `EmptyState`
- `Skeleton`
- `MobileDrawer`

## Quy tắc
- Không nhồi toàn bộ page vào 1 file view/component.
- Không tạo component quá nhỏ chỉ để bọc 1 icon/text.
- Ưu tiên component theo nghiệp vụ.
- Dữ liệu truyền vào component qua props/model/viewmodel đang có.
