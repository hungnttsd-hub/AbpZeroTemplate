using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class AffiliateSyncRun : CreationAuditedAggregateRoot<Guid>
{
    public AffiliatePlatform Platform { get; private set; }
    public AffiliateSyncKind SyncKind { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public DateTime RangeFrom { get; private set; }
    public DateTime RangeTo { get; private set; }
    public AffiliateSyncRunStatus Status { get; private set; }
    public int FetchedCount { get; private set; }
    public int InsertedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int UnmatchedCount { get; private set; }
    public int ErrorCount { get; private set; }
    public string? ErrorSummary { get; private set; }

    protected AffiliateSyncRun() { }
    public AffiliateSyncRun(Guid id, AffiliatePlatform platform, AffiliateSyncKind kind, DateTime from, DateTime to, DateTime startedAt) : base(id)
    { Platform = platform; SyncKind = kind; RangeFrom = from; RangeTo = to; StartedAt = startedAt; Status = AffiliateSyncRunStatus.Running; }
    public void Complete(DateTime at, int fetched, int inserted, int updated, int unmatched, int errors, string? summary)
    { FinishedAt = at; FetchedCount = fetched; InsertedCount = inserted; UpdatedCount = updated; UnmatchedCount = unmatched; ErrorCount = errors; ErrorSummary = summary; Status = errors == 0 ? AffiliateSyncRunStatus.Succeeded : AffiliateSyncRunStatus.Failed; }
}
