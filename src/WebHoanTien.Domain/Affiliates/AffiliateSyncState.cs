using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class AffiliateSyncState : FullAuditedAggregateRoot<Guid>
{
    public AffiliatePlatform Platform { get; private set; }
    public AffiliateSyncKind SyncKind { get; private set; }
    public DateTime? Watermark { get; private set; }
    public DateTime? InitialStartDate { get; private set; }
    public DateTime? LastSucceededAt { get; private set; }
    public string? LastError { get; private set; }

    protected AffiliateSyncState() { }
    public AffiliateSyncState(Guid id, AffiliatePlatform platform, AffiliateSyncKind kind) : base(id) { Platform = platform; SyncKind = kind; }
    public void SetInitialStartDate(DateTime value) => InitialStartDate = value;
    public void Succeeded(DateTime watermark, DateTime at) { Watermark = watermark; LastSucceededAt = at; LastError = null; }
    public void Failed(string error) => LastError = error.Length > 2000 ? error[..2000] : error;
}
