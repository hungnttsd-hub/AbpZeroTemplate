# Wallet / Withdrawal Implementation Notes

## Core customer concepts
- `Số dư trong ví` / `Số dư khả dụng`: amount the customer can withdraw now
- `Tổng tiền đã ghi nhận`: total amount that has been recorded into the system historically
- `Hoa hồng sắp ghi nhận`: amount expected to be credited later after reconciliation

## Withdrawal
The withdrawal page should feel simple and trustworthy:
- large available-balance card
- obvious amount field
- bank account clearly visible
- no unnecessary technical data
- a summary before submit
- a clear note that pending commission cannot be withdrawn yet

## Recommended UX details
- quick amount chips accelerate common actions
- `Toàn bộ` should auto-fill the current available balance
- show inline validation rather than modal alerts for amount errors
- after successful request, navigate to wallet history or show a success state
