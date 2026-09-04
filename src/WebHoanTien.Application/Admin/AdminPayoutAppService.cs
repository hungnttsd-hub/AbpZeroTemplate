using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using Volo.Abp.Validation;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;
using WebHoanTien.Notifications;

namespace WebHoanTien.Admin;

[Authorize(WebHoanTienPermissions.Admin.Payouts)]
[RemoteService(IsEnabled = false)]
public class AdminPayoutAppService : WebHoanTienAppService, IAdminPayoutAppService
{
    private readonly IRepository<WithdrawalRequest, Guid> _withdrawals;
    private readonly IRepository<WithdrawalPaymentProof, Guid> _proofs;
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly WalletBalanceCalculator _balanceCalculator;
    private readonly WithdrawalProofValidator _proofValidator;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly CustomerNotificationManager _notificationManager;

    public AdminPayoutAppService(IRepository<WithdrawalRequest, Guid> withdrawals,
        IRepository<WithdrawalPaymentProof, Guid> proofs, IRepository<IdentityUser, Guid> users,
        WalletBalanceCalculator balanceCalculator, WithdrawalProofValidator proofValidator,
        IUnitOfWorkManager unitOfWorkManager, CustomerNotificationManager notificationManager)
    {
        _withdrawals = withdrawals;
        _proofs = proofs;
        _users = users;
        _balanceCalculator = balanceCalculator;
        _proofValidator = proofValidator;
        _unitOfWorkManager = unitOfWorkManager;
        _notificationManager = notificationManager;
    }

    public async Task<AdminPayoutPageDto> GetListAsync(AdminPayoutListInput input)
    {
        var all = await _withdrawals.GetListAsync();
        var userEmails = await GetUserEmailsAsync(all.Select(x => x.UserId));
        IEnumerable<WithdrawalRequest> filtered = all;
        if (input.From.HasValue) filtered = filtered.Where(x => x.CreationTime >= input.From.Value);
        if (input.To.HasValue) filtered = filtered.Where(x => x.CreationTime < input.To.Value);
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var term = input.Filter.Trim();
            filtered = filtered.Where(x => x.RequestCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.AccountNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (userEmails.TryGetValue(x.UserId, out var email) && email.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var summaryRows = filtered.ToList();
        if (input.Status.HasValue) filtered = summaryRows.Where(x => x.Status == input.Status.Value);
        var sorted = filtered.OrderByDescending(x => x.CreationTime).ToList();
        var pageRows = sorted.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
        var proofRequestIds = await GetProofRequestIdsAsync(pageRows.Select(x => x.Id));
        var mapped = new List<AdminPayoutRequestDto>(pageRows.Count);
        foreach (var request in pageRows)
        {
            mapped.Add(await MapAsync(request, userEmails.GetValueOrDefault(request.UserId) ?? "Không xác định",
                proofRequestIds.Contains(request.Id)));
        }

        return new AdminPayoutPageDto
        {
            Summary = new AdminPayoutSummaryDto
            {
                PendingCount = summaryRows.Count(x => x.Status == WithdrawalRequestStatus.Pending),
                PendingAmount = summaryRows.Where(x => x.Status == WithdrawalRequestStatus.Pending).Sum(x => x.Amount),
                PaidCount = summaryRows.Count(x => x.Status == WithdrawalRequestStatus.Paid),
                PaidAmount = summaryRows.Where(x => x.Status == WithdrawalRequestStatus.Paid).Sum(x => x.Amount),
                RejectedCount = summaryRows.Count(x => x.Status == WithdrawalRequestStatus.Rejected)
            },
            Requests = new Volo.Abp.Application.Dtos.PagedResultDto<AdminPayoutRequestDto>(sorted.Count, mapped)
        };
    }

    public async Task<AdminPayoutRequestDto> GetAsync(Guid id)
    {
        var request = await _withdrawals.GetAsync(id);
        var user = await _users.FindAsync(request.UserId);
        var hasProof = await _proofs.AnyAsync(x => x.WithdrawalRequestId == id);
        return await MapAsync(request, user?.Email ?? user?.UserName ?? "Không xác định", hasProof);
    }

    [DisableAuditing]
    [DisableValidation]
    public async Task<AdminPayoutRequestDto> MarkPaidAsync(Guid id, MarkWithdrawalPaidInput input,
        Stream proofStream, string proofFileName, string proofContentType, long proofLength,
        CancellationToken cancellationToken = default)
    {
        ValidatePaymentInput(input);
        try
        {
            using var unitOfWork = _unitOfWorkManager.Begin(new AbpUnitOfWorkOptions
            {
                IsTransactional = true,
                IsolationLevel = IsolationLevel.Serializable
            }, requiresNew: true);
            var request = await _withdrawals.GetAsync(id, cancellationToken: cancellationToken);
            if (request.Status == WithdrawalRequestStatus.Paid)
            {
                var alreadyPaid = await GetAsync(id);
                await unitOfWork.CompleteAsync(cancellationToken);
                return alreadyPaid;
            }
            if (request.Status != WithdrawalRequestStatus.Pending)
                throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalInvalidState);

            var balance = await _balanceCalculator.GetAsync(request.UserId);
            if (balance.ConfirmedAmount - balance.PaidAmount < request.Amount)
                throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalNotBacked);
            if (await _proofs.AnyAsync(x => x.WithdrawalRequestId == id, cancellationToken))
                throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalInvalidState);

            var proof = await _proofValidator.ReadAsync(proofStream, proofFileName, proofContentType, proofLength,
                cancellationToken);
            var paidAt = NormalizePaidAt(input.PaidAt, request.CreationTime);
            request.MarkPaid(CurrentUser.GetId(), input.PaymentReference, paidAt, input.AdminNote);
            await _proofs.InsertAsync(new WithdrawalPaymentProof(GuidGenerator.Create(), request.Id, proof.FileName,
                proof.ContentType, proof.Sha256, proof.Content), autoSave: true, cancellationToken: cancellationToken);
            await _withdrawals.UpdateAsync(request, autoSave: true, cancellationToken: cancellationToken);
            await _notificationManager.NotifyWithdrawalStatusAsync(request);
            var user = await _users.FindAsync(request.UserId, cancellationToken: cancellationToken);
            var result = await MapAsync(request, user?.Email ?? user?.UserName ?? "Không xác định", true);
            await unitOfWork.CompleteAsync(cancellationToken);
            return result;
        }
        catch (Exception exception) when (ContainsDatabaseMarker(exception,
                   "IX_WithdrawalPaymentProof_WithdrawalRequestId", "Concurrency", "could not serialize access"))
        {
            var current = await GetAsync(id);
            if (current.Status == WithdrawalRequestStatus.Paid) return current;
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalInvalidState);
        }
    }

    [UnitOfWork]
    public async Task<AdminPayoutRequestDto> RejectAsync(Guid id, RejectWithdrawalInput input)
    {
        try
        {
            var request = await _withdrawals.GetAsync(id);
            request.Reject(CurrentUser.GetId(), input.Reason, Clock.Now, input.AdminNote);
            await _withdrawals.UpdateAsync(request, autoSave: true);
            await _notificationManager.NotifyWithdrawalStatusAsync(request);
            var user = await _users.FindAsync(request.UserId);
            return await MapAsync(request, user?.Email ?? user?.UserName ?? "Không xác định",
                await _proofs.AnyAsync(x => x.WithdrawalRequestId == id));
        }
        catch (Exception exception) when (ContainsDatabaseMarker(exception, "Concurrency"))
        {
            throw new BusinessException(WebHoanTienDomainErrorCodes.WithdrawalInvalidState);
        }
    }

    [DisableAuditing]
    public async Task<WithdrawalProofDto> GetProofAsync(Guid id)
    {
        await _withdrawals.GetAsync(id);
        return CustomerWalletAppService.MapProof(await _proofs.GetAsync(x => x.WithdrawalRequestId == id));
    }

    private async Task<AdminPayoutRequestDto> MapAsync(WithdrawalRequest request, string email, bool hasProof)
    {
        var balance = await _balanceCalculator.GetAsync(request.UserId);
        var bank = PayoutBankCatalog.Banks.FirstOrDefault(x => x.Code.Equals(request.BankCode, StringComparison.OrdinalIgnoreCase));
        return new AdminPayoutRequestDto
        {
            Id = request.Id, CreationTime = request.CreationTime, CreatorId = request.CreatorId,
            LastModificationTime = request.LastModificationTime, LastModifierId = request.LastModifierId,
            IsDeleted = request.IsDeleted, DeleterId = request.DeleterId, DeletionTime = request.DeletionTime,
            UserId = request.UserId, UserEmail = email, RequestCode = request.RequestCode,
            TransferContent = WithdrawalTransferContent.Create(request.RequestCode),
            Amount = request.Amount, FeeAmount = request.FeeAmount, NetAmount = request.NetAmount,
            Status = request.Status, BankCode = request.BankCode, BankName = bank?.Name ?? request.BankCode,
            AccountNumber = request.AccountNumber, AccountHolderName = request.AccountHolderName,
            ProcessedAt = request.ProcessedAt, ProcessedByUserId = request.ProcessedByUserId,
            PaymentReference = request.PaymentReference, AdminNote = request.AdminNote,
            RejectionReason = request.RejectionReason, HasProof = hasProof,
            IsBacked = request.Status != WithdrawalRequestStatus.Pending ||
                balance.ConfirmedAmount - balance.PaidAmount >= request.Amount,
            UserConfirmedAmount = balance.ConfirmedAmount, UserPaidAmount = balance.PaidAmount
        };
    }

    private async Task<Dictionary<Guid, string>> GetUserEmailsAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();
        var query = (await _users.GetQueryableAsync()).Where(x => ids.Contains(x.Id));
        return (await AsyncExecuter.ToListAsync(query)).ToDictionary(x => x.Id, x => x.Email ?? x.UserName);
    }

    private async Task<HashSet<Guid>> GetProofRequestIdsAsync(IEnumerable<Guid> requestIds)
    {
        var ids = requestIds.Distinct().ToList();
        if (ids.Count == 0) return new HashSet<Guid>();
        return (await _proofs.GetListAsync(x => ids.Contains(x.WithdrawalRequestId)))
            .Select(x => x.WithdrawalRequestId).ToHashSet();
    }

    private DateTime NormalizePaidAt(DateTime value, DateTime requestCreatedAt)
    {
        if (value == default) throw new UserFriendlyException("Vui lòng nhập thời gian chuyển khoản.");
        var normalized = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        if (normalized < requestCreatedAt.AddMinutes(-1) || normalized > Clock.Now.AddMinutes(5))
            throw new UserFriendlyException("Thời gian chuyển khoản không hợp lệ.");
        return normalized;
    }

    private static void ValidatePaymentInput(MarkWithdrawalPaidInput input)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.PaymentReference) ||
            input.PaymentReference.Trim().Length > 128)
            throw new UserFriendlyException("Mã giao dịch phải có từ 1 đến 128 ký tự.");
        if (input.AdminNote?.Trim().Length > 1000)
            throw new UserFriendlyException("Ghi chú không được vượt quá 1.000 ký tự.");
    }

    private static bool ContainsDatabaseMarker(Exception exception, params string[] markers)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
            if (markers.Any(marker => current.Message.Contains(marker, StringComparison.OrdinalIgnoreCase))) return true;
        return false;
    }
}
