using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace WebHoanTien.Affiliates;

[Authorize]
[RemoteService(IsEnabled = false)]
public class CustomerWalletAppService : WebHoanTienAppService, ICustomerWalletAppService
{
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<WithdrawalRequest, Guid> _withdrawals;
    private readonly IRepository<WithdrawalPaymentProof, Guid> _proofs;
    private readonly IRepository<UserPayoutAccount, Guid> _payoutAccounts;
    private readonly WalletBalanceCalculator _balanceCalculator;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public CustomerWalletAppService(IRepository<AffiliateConversion, Guid> conversions,
        IRepository<AffiliateOrder, Guid> orders, IRepository<WithdrawalRequest, Guid> withdrawals,
        IRepository<WithdrawalPaymentProof, Guid> proofs, IRepository<UserPayoutAccount, Guid> payoutAccounts,
        WalletBalanceCalculator balanceCalculator, IUnitOfWorkManager unitOfWorkManager)
    {
        _conversions = conversions;
        _orders = orders;
        _withdrawals = withdrawals;
        _proofs = proofs;
        _payoutAccounts = payoutAccounts;
        _balanceCalculator = balanceCalculator;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<CustomerWalletOverviewDto> GetOverviewAsync()
    {
        var userId = CurrentUser.GetId();
        var payoutAccount = await _payoutAccounts.FindAsync(x => x.UserId == userId);
        var withdrawals = await _withdrawals.GetListAsync(x => x.UserId == userId);
        var proofRequestIds = await GetProofRequestIdsAsync(withdrawals.Select(x => x.Id));
        var pending = withdrawals.OrderByDescending(x => x.CreationTime)
            .FirstOrDefault(x => x.Status == WithdrawalRequestStatus.Pending);
        var overview = await BuildOverviewAsync(userId, payoutAccount is not null, withdrawals, proofRequestIds);
        overview.PendingWithdrawal = pending is null ? null : MapWithdrawal(pending, proofRequestIds.Contains(pending.Id));
        overview.RecentMovements = (await BuildMovementsAsync(userId, withdrawals, proofRequestIds)).Take(5).ToList();
        return overview;
    }

    public async Task<WithdrawalPreparationDto> GetWithdrawalPreparationAsync()
    {
        var userId = CurrentUser.GetId();
        var payoutAccount = await _payoutAccounts.FindAsync(x => x.UserId == userId);
        var withdrawals = await _withdrawals.GetListAsync(x => x.UserId == userId);
        var proofRequestIds = await GetProofRequestIdsAsync(withdrawals.Select(x => x.Id));
        return new WithdrawalPreparationDto
        {
            Wallet = await BuildOverviewAsync(userId, payoutAccount is not null, withdrawals, proofRequestIds),
            PayoutAccount = payoutAccount is null ? null : MapPayoutAccount(payoutAccount),
            FeeAmount = WebHoanTienConsts.WithdrawalFeeAmount,
            RecentWithdrawals = withdrawals.OrderByDescending(x => x.CreationTime).Take(5)
                .Select(x => MapWithdrawal(x, proofRequestIds.Contains(x.Id))).ToList()
        };
    }

    public async Task<PagedResultDto<WalletMovementDto>> GetHistoryAsync(WalletHistoryInput input)
    {
        var userId = CurrentUser.GetId();
        var withdrawals = await _withdrawals.GetListAsync(x => x.UserId == userId);
        var proofRequestIds = await GetProofRequestIdsAsync(withdrawals.Select(x => x.Id));
        var movements = await BuildMovementsAsync(userId, withdrawals, proofRequestIds);
        if (input.Kind.HasValue) movements = movements.Where(x => x.Kind == input.Kind.Value).ToList();
        return new PagedResultDto<WalletMovementDto>(movements.Count,
            movements.Skip(input.SkipCount).Take(input.MaxResultCount).ToList());
    }

    public async Task<WithdrawalRequestDto> CreateWithdrawalRequestAsync(CreateWithdrawalRequestInput input)
    {
        try
        {
            using var unitOfWork = _unitOfWorkManager.Begin(new AbpUnitOfWorkOptions
            {
                IsTransactional = true,
                IsolationLevel = IsolationLevel.Serializable
            }, requiresNew: true);
            var amount = input.Amount;
            if (amount != decimal.Truncate(amount)) throw new UserFriendlyException("Số tiền rút phải là số nguyên đồng.");
            if (amount < WebHoanTienConsts.MinimumWithdrawalAmount)
                throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalBelowMinimum);

            var userId = CurrentUser.GetId();
            if (await _withdrawals.AnyAsync(x => x.UserId == userId && x.Status == WithdrawalRequestStatus.Pending))
                throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalPendingExists);
            var payoutAccount = await _payoutAccounts.FindAsync(x => x.UserId == userId)
                ?? throw new BusinessException(WebHoanTienDomainErrorCodes.PayoutAccountRequired);
            var balance = await _balanceCalculator.GetAsync(userId);
            if (amount > balance.AvailableBalance)
                throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalInsufficientBalance);

            var id = GuidGenerator.Create();
            var requestCode = $"CB{Clock.Now:yyyyMMdd}{id:N}"[..20].ToUpperInvariant();
            var request = new WithdrawalRequest(id, userId, requestCode, payoutAccount, amount,
                WebHoanTienConsts.WithdrawalFeeAmount);
            await _withdrawals.InsertAsync(request, autoSave: true);
            await unitOfWork.CompleteAsync();
            return MapWithdrawal(request, false);
        }
        catch (Exception exception) when (ContainsDatabaseMarker(exception,
                   "IX_WithdrawalRequest_UserId", "could not serialize access"))
        {
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalPendingExists);
        }
    }

    [UnitOfWork]
    public async Task<WithdrawalRequestDto> CancelWithdrawalRequestAsync(Guid id)
    {
        try
        {
            var request = await _withdrawals.GetAsync(id);
            request.Cancel(CurrentUser.GetId(), Clock.Now);
            await _withdrawals.UpdateAsync(request, autoSave: true);
            return MapWithdrawal(request, await _proofs.AnyAsync(x => x.WithdrawalRequestId == id));
        }
        catch (Exception exception) when (ContainsDatabaseMarker(exception, "Concurrency"))
        {
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalInvalidState);
        }
    }

    public async Task<WithdrawalProofDto> GetProofAsync(Guid id)
    {
        var request = await _withdrawals.GetAsync(id);
        if (request.UserId != CurrentUser.GetId())
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalNotOwned);
        return MapProof(await _proofs.GetAsync(x => x.WithdrawalRequestId == id));
    }

    private async Task<CustomerWalletOverviewDto> BuildOverviewAsync(Guid userId, bool hasPayoutAccount,
        IReadOnlyCollection<WithdrawalRequest> withdrawals, HashSet<Guid> proofRequestIds)
    {
        var balance = await _balanceCalculator.GetAsync(userId);
        var pending = withdrawals.OrderByDescending(x => x.CreationTime)
            .FirstOrDefault(x => x.Status == WithdrawalRequestStatus.Pending);
        return new CustomerWalletOverviewDto
        {
            AvailableBalance = balance.AvailableBalance,
            TotalRecordedAmount = balance.ConfirmedAmount,
            PendingCommissionAmount = balance.PendingAmount,
            MinimumWithdrawalAmount = WebHoanTienConsts.MinimumWithdrawalAmount,
            HasPayoutAccount = hasPayoutAccount,
            PendingWithdrawal = pending is null ? null : MapWithdrawal(pending, proofRequestIds.Contains(pending.Id))
        };
    }

    private async Task<List<WalletMovementDto>> BuildMovementsAsync(Guid userId,
        IReadOnlyCollection<WithdrawalRequest> withdrawals, HashSet<Guid> proofRequestIds)
    {
        var conversions = await _conversions.GetListAsync(x => x.UserId == userId);
        var conversionById = conversions.ToDictionary(x => x.Id);
        var conversionIds = conversionById.Keys.ToList();
        var orders = conversionIds.Count == 0
            ? new List<AffiliateOrder>()
            : await _orders.GetListAsync(x => conversionIds.Contains(x.ConversionId));
        var movements = orders.Select(order => MapOrderMovement(order, conversionById[order.ConversionId])).ToList();
        movements.AddRange(withdrawals.Select(x => MapWithdrawalMovement(x, proofRequestIds.Contains(x.Id))));
        return movements.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).ToList();
    }

    private async Task<HashSet<Guid>> GetProofRequestIdsAsync(IEnumerable<Guid> requestIds)
    {
        var ids = requestIds.Distinct().ToList();
        if (ids.Count == 0) return new HashSet<Guid>();
        return (await _proofs.GetListAsync(x => ids.Contains(x.WithdrawalRequestId)))
            .Select(x => x.WithdrawalRequestId).ToHashSet();
    }

    private static WalletMovementDto MapOrderMovement(AffiliateOrder order, AffiliateConversion conversion)
    {
        var (label, css, amount) = order.Status switch
        {
            AffiliateOrderStatus.Settled => ("Đã ghi nhận", "confirmed", order.PayableUserCommission),
            AffiliateOrderStatus.Completed => ("Chờ Shopee thanh toán", "pending", order.UserCommissionSnapshot),
            AffiliateOrderStatus.Unpaid or AffiliateOrderStatus.Pending => ("Sắp ghi nhận", "pending", order.UserCommissionSnapshot),
            AffiliateOrderStatus.Refunded => ("Đã hoàn tiền", "cancelled", 0m),
            AffiliateOrderStatus.Cancelled => ("Đã hủy", "cancelled", 0m),
            _ => ("Không được ghi nhận", "cancelled", 0m)
        };
        return new WalletMovementDto
        {
            Id = order.Id,
            Kind = WalletMovementKind.Commission,
            Title = "Hoàn tiền đơn Shopee",
            Description = order.ExternalOrderId,
            OccurredAt = conversion.LastProviderUpdateAt,
            Amount = amount,
            StatusLabel = label,
            StatusClass = css
        };
    }

    private static WalletMovementDto MapWithdrawalMovement(WithdrawalRequest request, bool hasProof)
    {
        var (label, css) = GetWithdrawalStatus(request.Status);
        return new WalletMovementDto
        {
            Id = request.Id,
            Kind = WalletMovementKind.Withdrawal,
            Title = "Yêu cầu rút tiền",
            Description = request.RequestCode,
            OccurredAt = request.ProcessedAt ?? request.CreationTime,
            Amount = -request.Amount,
            StatusLabel = label,
            StatusClass = css,
            CanCancel = request.Status == WithdrawalRequestStatus.Pending,
            HasProof = hasProof
        };
    }

    internal static WithdrawalRequestDto MapWithdrawal(WithdrawalRequest request, bool hasProof)
    {
        var bank = PayoutBankCatalog.Banks.FirstOrDefault(x => x.Code.Equals(request.BankCode, StringComparison.OrdinalIgnoreCase));
        return new WithdrawalRequestDto
        {
            Id = request.Id, CreationTime = request.CreationTime, CreatorId = request.CreatorId,
            LastModificationTime = request.LastModificationTime, LastModifierId = request.LastModifierId,
            IsDeleted = request.IsDeleted, DeleterId = request.DeleterId, DeletionTime = request.DeletionTime,
            RequestCode = request.RequestCode, Amount = request.Amount, FeeAmount = request.FeeAmount,
            NetAmount = request.NetAmount, Status = request.Status, BankCode = request.BankCode,
            BankName = bank?.Name ?? request.BankCode, MaskedAccountNumber = MaskAccount(request.AccountNumber),
            AccountHolderName = request.AccountHolderName, ProcessedAt = request.ProcessedAt,
            PaymentReference = request.PaymentReference, AdminNote = request.AdminNote,
            RejectionReason = request.RejectionReason, CanCancel = request.Status == WithdrawalRequestStatus.Pending,
            HasProof = hasProof
        };
    }

    internal static WithdrawalProofDto MapProof(WithdrawalPaymentProof proof) => new()
    {
        FileName = proof.FileName,
        ContentType = proof.ContentType,
        Content = proof.Content.ToArray()
    };

    private static PayoutAccountDto MapPayoutAccount(UserPayoutAccount account) => new()
    {
        BankCode = account.BankCode,
        AccountNumber = account.AccountNumber,
        AccountHolderName = account.AccountHolderName
    };

    private static (string Label, string Css) GetWithdrawalStatus(WithdrawalRequestStatus status) => status switch
    {
        WithdrawalRequestStatus.Pending => ("Đang xử lý", "pending"),
        WithdrawalRequestStatus.Paid => ("Đã thanh toán", "confirmed"),
        WithdrawalRequestStatus.Rejected => ("Từ chối", "cancelled"),
        _ => ("Đã hủy", "cancelled")
    };

    private static string MaskAccount(string value) => value.Length <= 4 ? value : new string('*', Math.Min(4, value.Length - 4)) + value[^4..];

    private static bool ContainsDatabaseMarker(Exception exception, params string[] markers)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
            if (markers.Any(marker => current.Message.Contains(marker, StringComparison.OrdinalIgnoreCase))) return true;
        return false;
    }
}
