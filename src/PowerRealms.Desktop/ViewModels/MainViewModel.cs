using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using PowerRealms.Desktop.P2P;
using PowerRealms.Desktop.Data;
using PowerRealms.Desktop.Models;
using Microsoft.EntityFrameworkCore;

namespace PowerRealms.Desktop.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly P2PNode _p2pNode;
    private readonly DesktopDbContext _dbContext;
    
    private string _nodeId = "";
    private int _peersCount;
    private int _nodesCount;
    private int _poolsCount;
    private string _statusMessage = "";
    private string _peerEndpoint = "";
    private int _selectedTabIndex;
    private bool _isApiRunning;
    private string _currentLanguage = "ru";

    public string NodeId
    {
        get => _nodeId;
        set => this.RaiseAndSetIfChanged(ref _nodeId, value);
    }

    public int PeersCount
    {
        get => _peersCount;
        set => this.RaiseAndSetIfChanged(ref _peersCount, value);
    }

    public int NodesCount
    {
        get => _nodesCount;
        set => this.RaiseAndSetIfChanged(ref _nodesCount, value);
    }

    public int PoolsCount
    {
        get => _poolsCount;
        set => this.RaiseAndSetIfChanged(ref _poolsCount, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string PeerEndpoint
    {
        get => _peerEndpoint;
        set => this.RaiseAndSetIfChanged(ref _peerEndpoint, value);
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    public bool IsApiRunning
    {
        get => _isApiRunning;
        set
        {
            this.RaiseAndSetIfChanged(ref _isApiRunning, value);
            this.RaisePropertyChanged(nameof(StatusColor));
            this.RaisePropertyChanged(nameof(StatusText));
        }
    }

    public string StatusColor => IsApiRunning ? "#00ff00" : "#ff0000";
    public string StatusText => IsApiRunning ? "Online" : "Offline";

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set => this.RaiseAndSetIfChanged(ref _currentLanguage, value);
    }

    public ObservableCollection<PeerInfo> Peers { get; } = new();
    public ObservableCollection<Node> Nodes { get; } = new();
    public ObservableCollection<Pool> Pools { get; } = new();

    public ReactiveCommand<Unit, Unit> StartApiCommand { get; }
    public ReactiveCommand<Unit, Unit> StopApiCommand { get; }
    public ReactiveCommand<Unit, Unit> ConnectPeerCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> CreatePoolCommand { get; }
    public ReactiveCommand<Unit, Unit> RegisterNodeCommand { get; }
    public ReactiveCommand<string, Unit> SwitchLanguageCommand { get; }

    public MainViewModel()
    {
        var options = new DbContextOptionsBuilder<DesktopDbContext>()
            .UseSqlite("Data Source=powerrealms.db")
            .Options;
        _dbContext = new DesktopDbContext(options);
        _dbContext.Database.EnsureCreated();

        _p2pNode = new P2PNode();
        NodeId = _p2pNode.NodeId.ToString();

        StartApiCommand = ReactiveCommand.CreateFromTask(StartApiAsync);
        StopApiCommand = ReactiveCommand.CreateFromTask(StopApiAsync);
        ConnectPeerCommand = ReactiveCommand.CreateFromTask(ConnectPeerAsync);
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshDataAsync);
        CreatePoolCommand = ReactiveCommand.CreateFromTask(CreatePoolAsync);
        RegisterNodeCommand = ReactiveCommand.CreateFromTask(RegisterNodeAsync);
        SwitchLanguageCommand = ReactiveCommand.Create<string>(SwitchLanguage);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await _p2pNode.StartAsync(5001);
        IsApiRunning = true;
        StatusMessage = GetLocalizedMessage("NodeStarted");
        await RefreshDataAsync();
    }

    private async Task StartApiAsync()
    {
        if (!IsApiRunning)
        {
            await _p2pNode.StartAsync(5001);
            IsApiRunning = true;
            StatusMessage = GetLocalizedMessage("NodeStarted");
        }
    }

    private async Task StopApiAsync()
    {
        if (IsApiRunning)
        {
            await _p2pNode.StopAsync();
            IsApiRunning = false;
            StatusMessage = GetLocalizedMessage("NodeStopped");
        }
    }

    private async Task ConnectPeerAsync()
    {
        if (string.IsNullOrWhiteSpace(PeerEndpoint))
        {
            StatusMessage = GetLocalizedMessage("EnterPeerAddress");
            return;
        }

        try
        {
            await _p2pNode.ConnectToPeerAsync(PeerEndpoint);
            StatusMessage = string.Format(GetLocalizedMessage("ConnectedToPeer"), PeerEndpoint);
            PeerEndpoint = "";
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(GetLocalizedMessage("ConnectionFailed"), ex.Message);
        }
    }

    private async Task RefreshDataAsync()
    {
        var peers = await _p2pNode.GetPeersAsync();
        Peers.Clear();
        foreach (var peer in peers)
        {
            Peers.Add(peer);
        }
        PeersCount = Peers.Count;

        var nodes = await _dbContext.Nodes.ToListAsync();
        Nodes.Clear();
        foreach (var node in nodes)
        {
            Nodes.Add(node);
        }
        NodesCount = Nodes.Count;

        var pools = await _dbContext.Pools.ToListAsync();
        Pools.Clear();
        foreach (var pool in pools)
        {
            Pools.Add(pool);
        }
        PoolsCount = Pools.Count;
    }

    private async Task CreatePoolAsync()
    {
        var pool = new Pool
        {
            Name = $"Pool-{DateTime.Now:HHmmss}",
            Type = PoolType.Public,
            OwnerId = Guid.NewGuid()
        };
        _dbContext.Pools.Add(pool);
        await _dbContext.SaveChangesAsync();
        StatusMessage = string.Format(GetLocalizedMessage("PoolCreated"), pool.Name);
        await RefreshDataAsync();
    }

    private async Task RegisterNodeAsync()
    {
        var node = new Node
        {
            Name = Environment.MachineName,
            OwnerId = Guid.NewGuid(),
            CpuPower = 100,
            GpuPower = 50,
            Rating = 5.0
        };
        _dbContext.Nodes.Add(node);
        await _dbContext.SaveChangesAsync();
        StatusMessage = string.Format(GetLocalizedMessage("NodeRegistered"), node.Name);
        await RefreshDataAsync();
    }

    private void SwitchLanguage(string lang)
    {
        CurrentLanguage = lang;
        StatusMessage = GetLocalizedMessage("LanguageChanged");
    }

    private string GetLocalizedMessage(string key)
    {
        var messages = CurrentLanguage == "ru" 
            ? new Dictionary<string, string>
            {
                {"NodeStarted", "Узел запущен на порту 5001"},
                {"NodeStopped", "Узел остановлен"},
                {"EnterPeerAddress", "Введите адрес узла"},
                {"ConnectedToPeer", "Подключено к {0}"},
                {"ConnectionFailed", "Ошибка подключения: {0}"},
                {"PoolCreated", "Пул '{0}' создан"},
                {"NodeRegistered", "Нода '{0}' зарегистрирована"},
                {"LanguageChanged", "Язык изменён"}
            }
            : new Dictionary<string, string>
            {
                {"NodeStarted", "Node started on port 5001"},
                {"NodeStopped", "Node stopped"},
                {"EnterPeerAddress", "Enter peer address"},
                {"ConnectedToPeer", "Connected to {0}"},
                {"ConnectionFailed", "Connection failed: {0}"},
                {"PoolCreated", "Pool '{0}' created"},
                {"NodeRegistered", "Node '{0}' registered"},
                {"LanguageChanged", "Language changed"}
            };

        return messages.TryGetValue(key, out var message) ? message : key;
    }
}
