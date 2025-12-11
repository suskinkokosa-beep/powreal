using System.Text.Json;
using PowerRealms.Api.Data;
using PowerRealms.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace PowerRealms.Api.P2P;

public interface ISyncService
{
    Task RequestSyncAsync(Guid peerId, CancellationToken ct = default);
    Task ProcessSyncDataAsync(string syncData, CancellationToken ct = default);
    Task<string> GetSyncPackageAsync(DateTime? since = null, CancellationToken ct = default);
}

public class SyncPackage
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid SourceNodeId { get; set; }
    public long Version { get; set; }
    public List<SyncEntity<User>> Users { get; set; } = new();
    public List<SyncEntity<Pool>> Pools { get; set; } = new();
    public List<SyncEntity<PoolMember>> PoolMembers { get; set; } = new();
    public List<SyncEntity<PoolLedgerEntry>> LedgerEntries { get; set; } = new();
    public List<SyncEntity<Offer>> Offers { get; set; } = new();
    public List<SyncEntity<Node>> Nodes { get; set; } = new();
}

public class SyncEntity<T>
{
    public T Entity { get; set; } = default!;
    public DateTime ModifiedAt { get; set; }
    public string Operation { get; set; } = "Upsert";
}

public class SyncService : ISyncService
{
    private readonly PowerRealmsDbContext _db;
    private readonly IP2PNode _p2pNode;
    private DateTime _lastSyncTime = DateTime.MinValue;
    private static long _syncVersion = 0;

    public SyncService(PowerRealmsDbContext db, IP2PNode p2pNode)
    {
        _db = db;
        _p2pNode = p2pNode;
    }

    public async Task RequestSyncAsync(Guid peerId, CancellationToken ct = default)
    {
        await _p2pNode.BroadcastAsync(new P2PMessage
        {
            Type = "SYNC_REQUEST",
            Payload = JsonSerializer.Serialize(new { Since = _lastSyncTime, RequesterId = _p2pNode.NodeId })
        }, ct);
    }

    public async Task<string> GetSyncPackageAsync(DateTime? since = null, CancellationToken ct = default)
    {
        var package = new SyncPackage
        {
            SourceNodeId = _p2pNode.NodeId,
            Version = Interlocked.Increment(ref _syncVersion)
        };

        var cutoff = since ?? DateTime.MinValue;

        package.Users = (await _db.Users.ToListAsync(ct))
            .Select(u => new SyncEntity<User> { Entity = u, ModifiedAt = DateTime.UtcNow })
            .ToList();

        package.Pools = (await _db.Pools.ToListAsync(ct))
            .Select(p => new SyncEntity<Pool> { Entity = p, ModifiedAt = DateTime.UtcNow })
            .ToList();

        package.PoolMembers = (await _db.PoolMembers.ToListAsync(ct))
            .Select(pm => new SyncEntity<PoolMember> { Entity = pm, ModifiedAt = DateTime.UtcNow })
            .ToList();

        package.LedgerEntries = (await _db.Ledger.Where(l => l.CreatedAt > cutoff).ToListAsync(ct))
            .Select(l => new SyncEntity<PoolLedgerEntry> { Entity = l, ModifiedAt = l.CreatedAt })
            .ToList();

        package.Offers = (await _db.Offers.ToListAsync(ct))
            .Select(o => new SyncEntity<Offer> { Entity = o, ModifiedAt = DateTime.UtcNow })
            .ToList();

        package.Nodes = (await _db.Nodes.ToListAsync(ct))
            .Select(n => new SyncEntity<Node> { Entity = n, ModifiedAt = DateTime.UtcNow })
            .ToList();

        return JsonSerializer.Serialize(package);
    }

    public async Task ProcessSyncDataAsync(string syncData, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(syncData) || syncData.Length > 10_000_000)
            return;

        SyncPackage? package;
        try
        {
            package = JsonSerializer.Deserialize<SyncPackage>(syncData);
        }
        catch
        {
            return;
        }

        if (package == null || package.SourceNodeId == _p2pNode.NodeId)
            return;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var syncUser in package.Users)
            {
                var existing = await _db.Users.FindAsync(new object[] { syncUser.Entity.Id }, ct);
                if (existing == null)
                {
                    _db.Users.Add(syncUser.Entity);
                }
            }

            foreach (var syncPool in package.Pools)
            {
                var existing = await _db.Pools.FindAsync(new object[] { syncPool.Entity.Id }, ct);
                if (existing == null)
                {
                    _db.Pools.Add(syncPool.Entity);
                }
            }

            foreach (var syncMember in package.PoolMembers)
            {
                var existing = await _db.PoolMembers.FindAsync(new object[] { syncMember.Entity.Id }, ct);
                if (existing == null)
                {
                    _db.PoolMembers.Add(syncMember.Entity);
                }
            }

            foreach (var syncEntry in package.LedgerEntries)
            {
                var existing = await _db.Ledger.FindAsync(new object[] { syncEntry.Entity.Id }, ct);
                if (existing == null)
                {
                    _db.Ledger.Add(syncEntry.Entity);
                }
            }

            foreach (var syncOffer in package.Offers)
            {
                var existing = await _db.Offers.FindAsync(new object[] { syncOffer.Entity.Id }, ct);
                if (existing == null)
                {
                    _db.Offers.Add(syncOffer.Entity);
                }
            }

            foreach (var syncNode in package.Nodes)
            {
                var existing = await _db.Nodes.FindAsync(new object[] { syncNode.Entity.Id }, ct);
                if (existing == null)
                {
                    _db.Nodes.Add(syncNode.Entity);
                }
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            _lastSyncTime = package.Timestamp;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
