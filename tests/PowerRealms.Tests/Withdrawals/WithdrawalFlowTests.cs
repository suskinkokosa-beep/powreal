using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PowerRealms.Api.Data;
using PowerRealms.Api.Models;
using PowerRealms.Api.Repositories;
using PowerRealms.Api.Services;
using System;
using System.Threading.Tasks;

namespace PowerRealms.Tests.Withdrawals;

public class WithdrawalFlowTests
{
    private PowerRealmsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PowerRealmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PowerRealmsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task CreateAndConfirmWithdrawal_CompletesFlow()
    {
        var db = CreateDb();
        var holdRepo = new HoldRepository(db);
        var ledgerRepo = new LedgerRepository(db);
        var withdrawalRepo = new WithdrawalRepository(db);

        var ledgerService = new LedgerService(ledgerRepo);
        var holdService = new HoldService(holdRepo, ledgerService);
        var poolRepo = new PoolRepository(db);
        var withdrawalService = new WithdrawalService(withdrawalRepo, holdService, ledgerService, poolRepo);

        var pool = new Pool { Name = "p1", OwnerId = Guid.NewGuid() };
        db.Pools.Add(pool);
        var user = new User { Username = "u1", PasswordHash = "h", Role = UserRole.Member };
        db.Users.Add(user);
        db.MemberBalances.Add(new MemberBalance { UserId = user.Id, PoolId = pool.Id, Balance = 100 });
        await db.SaveChangesAsync();

        var req = await withdrawalService.CreateWithdrawalRequestAsync(pool.Id, user.Id, 25, "addr1");
        req.Should().NotBeNull();
        req.Status.Should().Be(WithdrawalStatus.Requested);
        req.HoldId.Should().NotBeNull();

        var confirmed = await withdrawalService.ConfirmWithdrawalAsync(req.Id, pool.OwnerId, true);
        confirmed.Should().NotBeNull();
        confirmed.Status.Should().Be(WithdrawalStatus.Completed);
    }

    [Fact]
    public async Task RejectWithdrawal_Refunds()
    {
        var db = CreateDb();
        var holdRepo = new HoldRepository(db);
        var ledgerRepo = new LedgerRepository(db);
        var withdrawalRepo = new WithdrawalRepository(db);

        var ledgerService = new LedgerService(ledgerRepo);
        var holdService = new HoldService(holdRepo, ledgerService);
        var poolRepo = new PoolRepository(db);
        var withdrawalService = new WithdrawalService(withdrawalRepo, holdService, ledgerService, poolRepo);

        var pool = new Pool { Name = "p2", OwnerId = Guid.NewGuid() };
        db.Pools.Add(pool);
        var user = new User { Username = "u2", PasswordHash = "h", Role = UserRole.Member };
        db.Users.Add(user);
        db.MemberBalances.Add(new MemberBalance { UserId = user.Id, PoolId = pool.Id, Balance = 100 });
        await db.SaveChangesAsync();

        var req = await withdrawalService.CreateWithdrawalRequestAsync(pool.Id, user.Id, 40, "addr2");
        var rejected = await withdrawalService.ConfirmWithdrawalAsync(req.Id, pool.OwnerId, false);
        rejected.Should().NotBeNull();
        rejected.Status.Should().Be(WithdrawalStatus.Rejected);
    }
}
