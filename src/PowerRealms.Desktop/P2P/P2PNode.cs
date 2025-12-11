using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PowerRealms.Desktop.P2P;

public class PeerInfo
{
    public Guid NodeId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public DateTime ConnectedAt { get; set; }
    public string StatusText => IsOnline ? "Online" : "Offline";
    public string StatusColor => IsOnline ? "#00ff00" : "#ff0000";
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

public class HelloPayload
{
    public string Endpoint { get; set; } = string.Empty;
}

public class P2PNode : IDisposable
{
    private readonly ConcurrentDictionary<Guid, PeerInfo> _peers = new();
    private readonly ConcurrentDictionary<Guid, TcpClient> _connections = new();
    private readonly ConcurrentDictionary<Guid, bool> _processedMessages = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Timer? _heartbeatTimer;
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
                _ = HandleConnectionAsync(client, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[P2P] Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleConnectionAsync(TcpClient client, CancellationToken ct)
    {
        Guid? peerId = null;
        try
        {
            var buffer = new byte[16384];
            var stream = client.GetStream();

            while (!ct.IsCancellationRequested && client.Connected)
            {
                int bytesRead;
                try { bytesRead = await stream.ReadAsync(buffer, ct); }
                catch (IOException) { break; }

                if (bytesRead == 0) break;

                var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var message = JsonSerializer.Deserialize<P2PMessage>(json);
                if (message == null) continue;

                if (_processedMessages.ContainsKey(message.MessageId)) continue;
                _processedMessages.TryAdd(message.MessageId, true);

                peerId = message.FromNodeId;

                switch (message.Type)
                {
                    case "HELLO":
                        var payload = JsonSerializer.Deserialize<HelloPayload>(message.Payload);
                        if (payload != null)
                        {
                            _peers[message.FromNodeId] = new PeerInfo
                            {
                                NodeId = message.FromNodeId,
                                Endpoint = payload.Endpoint,
                                LastSeen = DateTime.UtcNow,
                                IsOnline = true,
                                ConnectedAt = DateTime.UtcNow
                            };
                            _connections[message.FromNodeId] = client;
                            await SendMessageAsync(client, new P2PMessage
                            {
                                Type = "HELLO_ACK",
                                FromNodeId = NodeId,
                                Payload = JsonSerializer.Serialize(new HelloPayload { Endpoint = _localEndpoint })
                            });
                        }
                        break;
                    case "HELLO_ACK":
                        var ackPayload = JsonSerializer.Deserialize<HelloPayload>(message.Payload);
                        if (ackPayload != null && _peers.TryGetValue(message.FromNodeId, out var peer))
                        {
                            peer.LastSeen = DateTime.UtcNow;
                            peer.IsOnline = true;
                        }
                        break;
                    case "HEARTBEAT":
                        if (_peers.TryGetValue(message.FromNodeId, out var p)) p.LastSeen = DateTime.UtcNow;
                        await SendMessageAsync(client, new P2PMessage { Type = "HEARTBEAT_ACK", FromNodeId = NodeId });
                        break;
                    case "HEARTBEAT_ACK":
                        if (_peers.TryGetValue(message.FromNodeId, out var p2)) p2.LastSeen = DateTime.UtcNow;
                        break;
                    default:
                        MessageReceived?.Invoke(this, message);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[P2P] Error: {ex.Message}");
        }
        finally
        {
            if (peerId.HasValue)
            {
                _connections.TryRemove(peerId.Value, out _);
                if (_peers.TryGetValue(peerId.Value, out var peer)) peer.IsOnline = false;
            }
            client.Dispose();
        }
    }

    public async Task ConnectToPeerAsync(string endpoint, CancellationToken ct = default)
    {
        var parts = endpoint.Split(':');
        var host = parts[0];
        var port = int.Parse(parts[1]);

        var client = new TcpClient();
        await client.ConnectAsync(host, port, ct);

        await SendMessageAsync(client, new P2PMessage
        {
            Type = "HELLO",
            FromNodeId = NodeId,
            Payload = JsonSerializer.Serialize(new HelloPayload { Endpoint = _localEndpoint })
        });

        _ = HandleConnectionAsync(client, ct);
        Console.WriteLine($"[P2P] Connected to {endpoint}");
    }

    private async Task SendMessageAsync(TcpClient client, P2PMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var data = Encoding.UTF8.GetBytes(json);
        await client.GetStream().WriteAsync(data);
    }

    public async Task BroadcastAsync(P2PMessage message, CancellationToken ct = default)
    {
        message.FromNodeId = NodeId;
        _processedMessages.TryAdd(message.MessageId, true);
        foreach (var (_, client) in _connections.Where(c => c.Value.Connected))
        {
            try { await SendMessageAsync(client, message); } catch { }
        }
    }

    private void SendHeartbeats(object? state)
    {
        foreach (var (_, client) in _connections.Where(c => c.Value.Connected))
        {
            try { _ = SendMessageAsync(client, new P2PMessage { Type = "HEARTBEAT", FromNodeId = NodeId }); } catch { }
        }
    }

    public Task<IEnumerable<PeerInfo>> GetPeersAsync()
        => Task.FromResult<IEnumerable<PeerInfo>>(_peers.Values.ToList());

    public async Task StopAsync()
    {
        _isRunning = false;
        _heartbeatTimer?.Dispose();
        _cts?.Cancel();
        foreach (var client in _connections.Values) client.Dispose();
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
