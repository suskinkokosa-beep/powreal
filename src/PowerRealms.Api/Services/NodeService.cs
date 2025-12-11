using PowerRealms.Api.Data;
using PowerRealms.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace PowerRealms.Api.Services;

public interface INodeService
{
    Task<Node> RegisterNodeAsync(Node node, CancellationToken ct = default);
    Task<Node?> GetNodeAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Node>> GetUserNodesAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Node>> GetAvailableNodesAsync(CancellationToken ct = default);
    Task<Node?> UpdateNodeAsync(Guid id, double? cpuPower, double? gpuPower, CancellationToken ct = default);
}

public class NodeService : INodeService
{
    private readonly PowerRealmsDbContext _db;

    public NodeService(PowerRealmsDbContext db)
    {
        _db = db;
    }

    public async Task<Node> RegisterNodeAsync(Node node, CancellationToken ct = default)
    {
        _db.Nodes.Add(node);
        await _db.SaveChangesAsync(ct);
        return node;
    }

    public Task<Node?> GetNodeAsync(Guid id, CancellationToken ct = default)
        => _db.Nodes.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IEnumerable<Node>> GetUserNodesAsync(Guid userId, CancellationToken ct = default)
        => await _db.Nodes.Where(n => n.OwnerId == userId).ToListAsync(ct);

    public async Task<IEnumerable<Node>> GetAvailableNodesAsync(CancellationToken ct = default)
        => await _db.Nodes.OrderByDescending(n => n.Rating).ToListAsync(ct);

    public async Task<Node?> UpdateNodeAsync(Guid id, double? cpuPower, double? gpuPower, CancellationToken ct = default)
    {
        var node = await _db.Nodes.FindAsync(new object[] { id }, ct);
        if (node == null) return null;

        if (cpuPower.HasValue) node.CpuPower = cpuPower.Value;
        if (gpuPower.HasValue) node.GpuPower = gpuPower.Value;

        _db.Nodes.Update(node);
        await _db.SaveChangesAsync(ct);
        return node;
    }
}
