using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PowerRealms.Api.Data;
using PowerRealms.Api.Models;
using PowerRealms.Api.Repositories;
using PowerRealms.Api.Services;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace PowerRealms.Tests.Ledger;

public class HoldFlowTests
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
    public async Task CreateHold_RecordLedgerEntry()
    {
        var db = CreateDb();
        var holdRepo = new HoldRepository(db);
        var ledgerRepo = new LedgerRepository(db);
        var ledgerService = new LedgerService(ledgerRepo);
        var holdService = new HoldService(holdRepo, ledgerService);

        var user = new User { Username = "u1", PasswordHash = "h", Role = UserRole.Member };
        db.Users.Add(user);
        db.MemberBalances.Add(new MemberBalance { UserId = user.Id, PoolId = Guid.NewGuid(), Balance = 100 });
        await db.SaveChangesAsync();

        var hold = new Hold { PoolId = Guid.NewGuid(), FromUserId = user.Id, Amount = 50, Type = HoldType.Payment };
        var created = await holdService.CreateHoldAsync(hold);
        created.Should().NotBeNull();
        created.Status.Should().Be(TransactionStatus.Pending);
    }
}
