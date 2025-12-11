using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PowerRealms.Api.P2P;

public class PeerInfo
{
    public Guid NodeId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public DateTime ConnectedAt { get; set; }
}

public class P2PMessage
{
    public string Type { get; set; } = string.Empty;
    public Guid FromNodeId { get; set; }
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int Ttl { get; set; } = 3;
}

public interface IP2PNode
{
    Guid NodeId { get; }
    Task StartAsync(int port, CancellationToken ct = default);
    Task StopAsync();
    Task ConnectToPeerAsync(string endpoint, CancellationToken ct = default);
    Task BroadcastAsync(P2PMessage message, CancellationToken ct = default);
    Task<IEnumerable<PeerInfo>> GetPeersAsync();
    event EventHandler<P2PMessage>? MessageReceived;
}

public class P2PNode : IP2PNode, IDisposable
{
    private readonly ConcurrentDictionary<Guid, PeerInfo> _peers = new();
    private readonly ConcurrentDictionary<Guid, TcpClient> _connections = new();
    private readonly ConcurrentDictionary<Guid, bool> _processedMessages = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Timer? _heartbeatTimer;
    private Timer? _cleanupTimer;
    private bool _isRunning;
    private string _localEndpoint = "";

    public Guid NodeId { get; } = Guid.NewGuid();
    public int ListenPort { get; private set; }

    public event EventHandler<P2PMessage>? MessageReceived;

    public async Task StartAsync(int port, CancellationToken ct = default)
    {
        ListenPort = port;
        _localEndpoint = $"0.0.0.0:{port}";
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _isRunning = true;

        _heartbeatTimer = new Timer(SendHeartbeats, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        _cleanupTimer = new Timer(CleanupStaleConnections, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        Console.WriteLine($"[P2P] Node {NodeId} listening on port {port}");
        _ = AcceptConnectionsAsync(_cts.Token);
        await Task.CompletedTask;
    }

    private async Task AcceptConnectionsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isRunning)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                _ = HandleIncomingConnectionAsync(client, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[P2P] Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleIncomingConnectionAsync(TcpClient client, CancellationToken ct)
    {
        Guid? peerId = null;
        try
        {
            var buffer = new byte[16384];
            var stream = client.GetStream();
            stream.ReadTimeout = 60000;
            stream.WriteTimeout = 30000;

            while (!ct.IsCancellationRequested && client.Connected)
            {
                int bytesRead;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, ct);
                }
                catch (IOException) { break; }

                if (bytesRead == 0) break;

                var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var message = JsonSerializer.Deserialize<P2PMessage>(json);
                if (message == null) continue;

                if (_processedMessages.ContainsKey(message.MessageId))
                    continue;
                _processedMessages.TryAdd(message.MessageId, true);

                peerId = message.FromNodeId;

                switch (message.Type)
                {
                    case "HELLO":
                        await ProcessHelloAsync(message, client);
                        break;
                    case "HELLO_ACK":
                        await ProcessHelloAckAsync(message);
                        break;
                    case "HEARTBEAT":
                        UpdatePeerLastSeen(message.FromNodeId);
                        await SendMessageAsync(client, new P2PMessage { Type = "HEARTBEAT_ACK", FromNodeId = NodeId });
                        break;
                    case "HEARTBEAT_ACK":
                        UpdatePeerLastSeen(message.FromNodeId);
                        break;
                    case "PEER_LIST":
                        await ProcessPeerListAsync(message);
                        break;
                    default:
                        if (message.Ttl > 0)
                        {
                            message.Ttl--;
                            await ForwardMessageAsync(message, message.FromNodeId);
                        }
                        MessageReceived?.Invoke(this, message);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[P2P] Connection error: {ex.Message}");
        }
        finally
        {
            if (peerId.HasValue)
            {
                _connections.TryRemove(peerId.Value, out _);
                if (_peers.TryGetValue(peerId.Value, out var peer))
                    peer.IsOnline = false;
            }
            client.Dispose();
        }
    }

    private async Task ProcessHelloAsync(P2PMessage message, TcpClient client)
    {
        var payload = JsonSerializer.Deserialize<HelloPayload>(message.Payload);
        if (payload == null) return;

        var peer = new PeerInfo
        {
            NodeId = message.FromNodeId,
            Endpoint = payload.Endpoint,
            LastSeen = DateTime.UtcNow,
            IsOnline = true,
            ConnectedAt = DateTime.UtcNow
        };
        _peers[message.FromNodeId] = peer;
        _connections[message.FromNodeId] = client;

        Console.WriteLine($"[P2P] Peer connected: {message.FromNodeId}");

        await SendMessageAsync(client, new P2PMessage
        {
            Type = "HELLO_ACK",
            FromNodeId = NodeId,
            Payload = JsonSerializer.Serialize(new HelloPayload { Endpoint = _localEndpoint })
        });

        await SendPeerListAsync(client);
    }

    private async Task ProcessHelloAckAsync(P2PMessage message)
    {
        var payload = JsonSerializer.Deserialize<HelloPayload>(message.Payload);
        if (payload == null) return;

        if (_peers.TryGetValue(message.FromNodeId, out var peer))
        {
            peer.LastSeen = DateTime.UtcNow;
            peer.IsOnline = true;
        }
        Console.WriteLine($"[P2P] Handshake completed with: {message.FromNodeId}");
    }

    private async Task ProcessPeerListAsync(P2PMessage message)
    {
        var peerList = JsonSerializer.Deserialize<List<PeerEndpointInfo>>(message.Payload);
        if (peerList == null) return;

        foreach (var peerInfo in peerList)
        {
            if (peerInfo.NodeId != NodeId && !_peers.ContainsKey(peerInfo.NodeId))
            {
                _ = Task.Run(async () =>
                {
                    try { await ConnectToPeerAsync(peerInfo.Endpoint); }
                    catch { }
                });
            }
        }
    }

    private async Task SendPeerListAsync(TcpClient client)
    {
        var peerList = _peers.Values
            .Where(p => p.IsOnline)
            .Select(p => new PeerEndpointInfo { NodeId = p.NodeId, Endpoint = p.Endpoint })
            .ToList();

        await SendMessageAsync(client, new P2PMessage
        {
            Type = "PEER_LIST",
            FromNodeId = NodeId,
            Payload = JsonSerializer.Serialize(peerList)
        });
    }

    public async Task ConnectToPeerAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var parts = endpoint.Split(':');
            var host = parts[0];
            var port = int.Parse(parts[1]);

            var client = new TcpClient();
            client.ReceiveTimeout = 60000;
            client.SendTimeout = 30000;

            await client.ConnectAsync(host, port, ct);

            await SendMessageAsync(client, new P2PMessage
            {
                Type = "HELLO",
                FromNodeId = NodeId,
                Payload = JsonSerializer.Serialize(new HelloPayload { Endpoint = _localEndpoint })
            });

            _ = HandleIncomingConnectionAsync(client, ct);
            Console.WriteLine($"[P2P] Connected to peer at {endpoint}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[P2P] Failed to connect to {endpoint}: {ex.Message}");
            throw;
        }
    }

    private async Task SendMessageAsync(TcpClient client, P2PMessage message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);
            await client.GetStream().WriteAsync(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[P2P] Send error: {ex.Message}");
        }
    }

    private async Task ForwardMessageAsync(P2PMessage message, Guid excludeNodeId)
    {
        foreach (var (nodeId, client) in _connections)
        {
            if (nodeId != excludeNodeId && client.Connected)
            {
                try { await SendMessageAsync(client, message); }
                catch { }
            }
        }
    }

    public async Task BroadcastAsync(P2PMessage message, CancellationToken ct = default)
    {
        message.FromNodeId = NodeId;
        _processedMessages.TryAdd(message.MessageId, true);

        foreach (var (nodeId, client) in _connections)
        {
            if (client.Connected)
            {
                try { await SendMessageAsync(client, message); }
                catch { }
            }
        }
    }

    private void UpdatePeerLastSeen(Guid nodeId)
    {
        if (_peers.TryGetValue(nodeId, out var peer))
        {
            peer.LastSeen = DateTime.UtcNow;
            peer.IsOnline = true;
        }
    }

    private void SendHeartbeats(object? state)
    {
        foreach (var (nodeId, client) in _connections)
        {
            if (client.Connected)
            {
                try
                {
                    _ = SendMessageAsync(client, new P2PMessage { Type = "HEARTBEAT", FromNodeId = NodeId });
                }
                catch { }
            }
        }
    }

    private void CleanupStaleConnections(object? state)
    {
        var staleTimeout = DateTime.UtcNow.AddMinutes(-2);
        foreach (var (nodeId, peer) in _peers)
        {
            if (peer.LastSeen < staleTimeout)
            {
                peer.IsOnline = false;
                if (_connections.TryRemove(nodeId, out var client))
                {
                    client.Dispose();
                }
            }
        }

        var messageCutoff = DateTime.UtcNow.AddMinutes(-5);
        var keysToRemove = _processedMessages.Keys.Take(1000).ToList();
        foreach (var key in keysToRemove)
            _processedMessages.TryRemove(key, out _);
    }

    public Task<IEnumerable<PeerInfo>> GetPeersAsync()
    {
        return Task.FromResult<IEnumerable<PeerInfo>>(_peers.Values.ToList());
    }

    public async Task StopAsync()
    {
        _isRunning = false;
        _heartbeatTimer?.Dispose();
        _cleanupTimer?.Dispose();
        _cts?.Cancel();

        foreach (var client in _connections.Values)
            client.Dispose();
        _connections.Clear();
        _listener?.Stop();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _cts?.Dispose();
    }
}

public class HelloPayload
{
    public string Endpoint { get; set; } = string.Empty;
}

public class PeerEndpointInfo
{
    public Guid NodeId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
}
