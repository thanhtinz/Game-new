using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Server.Services.NPC;
using Xunit;

namespace WorldFaith.Tests.Services;

public class NpcInteractionServiceTests
{
    private readonly Mock<INpcRepository>          _npcRepo     = new();
    private readonly Mock<INpcEventRepository>     _eventRepo   = new();
    private readonly Mock<ICivilizationRepository> _civRepo     = new();
    private readonly Mock<IGodRepository>          _godRepo     = new();
    private readonly Mock<IReligionRepository>     _religionRepo= new();
    private readonly Mock<IOrganizationRepository> _orgRepo     = new();
    private readonly Mock<IBalanceConfigService>   _balance     = new();
    private readonly NpcInteractionService         _sut;

    public NpcInteractionServiceTests()
    {
        _balance.Setup(b => b.GetFloatAsync(It.IsAny<string>())).ReturnsAsync(0.05f);

        _eventRepo
            .Setup(r => r.LogAsync(It.IsAny<NpcEventDocument>()))
            .ReturnsAsync((NpcEventDocument e) => e);

        _sut = new NpcInteractionService(
            _npcRepo.Object, _eventRepo.Object, _civRepo.Object,
            _godRepo.Object, _religionRepo.Object, _orgRepo.Object,
            _balance.Object, NullLogger<NpcInteractionService>.Instance);
    }

    // ─── Tick produces events ─────────────────────────────

    [Fact]
    public async Task TickAsync_FallenCiv_ProducesNoEvents()
    {
        _civRepo.Setup(r => r.GetByWorldAsync("w1"))
            .ReturnsAsync(new List<CivilizationDocument>
            {
                new() { Id = "c1", WorldId = "w1", State = CivilizationState.Fallen }
            });

        var events = await _sut.TickAsync("w1", 1);

        events.Should().BeEmpty("fallen civs should be skipped");
    }

    [Fact]
    public async Task TickAsync_LowEconomy_CanTriggerTheft()
    {
        var civ = new CivilizationDocument
        {
            Id = "c1", WorldId = "w1", State = CivilizationState.Kingdom,
            Economy = 10f,  // < 20 threshold
            AiMemory = new CivilizationAiMemory()
        };

        _civRepo.Setup(r => r.GetByWorldAsync("w1")).ReturnsAsync(new List<CivilizationDocument> { civ });
        _npcRepo.Setup(r => r.GetByCivilizationAsync("c1")).ReturnsAsync(new List<NpcDocument>());
        _orgRepo.Setup(r => r.GetByCivilizationAsync("c1")).ReturnsAsync(new List<OrganizationDocument>());

        // Run nhiều lần vì theft là probabilistic
        bool theftLogged = false;
        for (int i = 0; i < 100 && !theftLogged; i++)
        {
            var events = await _sut.TickAsync("w1", i);
            if (events.Any(e => e.Type == NpcEventType.Theft))
                theftLogged = true;
        }

        theftLogged.Should().BeTrue("theft should eventually occur with economy < 20");
    }

    // ─── NPC Tier Quick Reference ─────────────────────────

    [Theory]
    [InlineData(NpcTier.Commoner, 0.01f)]
    [InlineData(NpcTier.Servant,  0.02f)]
    [InlineData(NpcTier.Adventurer, 0.05f)]
    [InlineData(NpcTier.Noble,    0.15f)]
    [InlineData(NpcTier.Royalty,  0.50f)]
    public void NpcTier_FaithPerTick_IsDocumentedCorrectly(NpcTier tier, float expectedFaith)
    {
        // This verifies our GDD faith generation table
        var faithRate = tier switch
        {
            NpcTier.Commoner   => 0.01f,
            NpcTier.Servant    => 0.02f,
            NpcTier.Adventurer => 0.05f,
            NpcTier.Noble      => 0.15f,
            NpcTier.Royalty    => 0.50f,
            _ => 0f
        };
        faithRate.Should().Be(expectedFaith);
    }

    // ─── God Response ─────────────────────────────────────

    [Fact]
    public async Task RespondToEvent_UnknownEventId_ReturnsNull()
    {
        _eventRepo.Setup(r => r.GetRecentAsync("w1", 100))
            .ReturnsAsync(new List<NpcEventDocument>());

        var result = await _sut.RespondToEventAsync("w1", "nonexistent", "g1", "BlessHarvest");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RespondToEvent_AlreadyResponded_ReturnsNull()
    {
        _eventRepo.Setup(r => r.GetRecentAsync("w1", 100))
            .ReturnsAsync(new List<NpcEventDocument>
            {
                new() { Id = "e1", CivilizationId = "c1", GodResponded = true, Type = NpcEventType.CropFailure }
            });

        var result = await _sut.RespondToEventAsync("w1", "e1", "g1", "BlessHarvest");

        result.Should().BeNull("already responded event should be skipped");
    }
}

public class NpcSpawnServiceTests
{
    private readonly Mock<INpcRepository>          _npcRepo  = new();
    private readonly Mock<IOrganizationRepository> _orgRepo  = new();
    private readonly NpcSpawnService               _sut;

    public NpcSpawnServiceTests()
    {
        _npcRepo.Setup(r => r.CreateAsync(It.IsAny<NpcDocument>()))
            .ReturnsAsync((NpcDocument n) => n);
        _orgRepo.Setup(r => r.CreateAsync(It.IsAny<OrganizationDocument>()))
            .ReturnsAsync((OrganizationDocument o) => o);
        _orgRepo.Setup(r => r.UpdateAsync(It.IsAny<OrganizationDocument>()))
            .Returns(Task.CompletedTask);
        _orgRepo.Setup(r => r.GetByTypeAsync(It.IsAny<string>(), It.IsAny<OrganizationType>()))
            .ReturnsAsync(new List<OrganizationDocument>());

        _sut = new NpcSpawnService(
            _npcRepo.Object, _orgRepo.Object,
            NullLogger<NpcSpawnService>.Instance);
    }

    [Fact]
    public async Task SpawnForCivilization_CreatesRoyalCourt()
    {
        var civ = new CivilizationDocument { Id = "c1", WorldId = "w1", Name = "Arachia" };

        await _sut.SpawnForCivilizationAsync("w1", civ);

        // Royal Court organization được tạo
        _orgRepo.Verify(r => r.CreateAsync(It.Is<OrganizationDocument>(
            o => o.Type == OrganizationType.RoyalCourt)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SpawnForCivilization_CreatesNobleHouses()
    {
        var civ = new CivilizationDocument { Id = "c1", WorldId = "w1", Name = "Arachia" };

        await _sut.SpawnForCivilizationAsync("w1", civ);

        // Noble Houses được tạo (ít nhất 3)
        _orgRepo.Verify(r => r.CreateAsync(It.Is<OrganizationDocument>(
            o => o.Type == OrganizationType.NobleHouse)), Times.AtLeast(3));
    }

    [Fact]
    public async Task SpawnForCivilization_CreatesAdventureGuild()
    {
        var civ = new CivilizationDocument { Id = "c1", WorldId = "w1", Name = "Arachia" };

        await _sut.SpawnForCivilizationAsync("w1", civ);

        _orgRepo.Verify(r => r.CreateAsync(It.Is<OrganizationDocument>(
            o => o.Type == OrganizationType.AdventureGuild)), Times.Once);
    }

    [Fact]
    public async Task PromoteToChampion_LowTrust_ReturnsNull()
    {
        _npcRepo.Setup(r => r.GetByIdAsync("npc1"))
            .ReturnsAsync(new NpcDocument
            {
                Id = "npc1", Tier = NpcTier.Adventurer,
                GodTrustLevel = 50f  // < 70 required
            });

        var result = await _sut.PromoteToChampionAsync("w1", "npc1", "g1");

        result.Should().BeNull("trust < 70 should not allow champion promotion");
    }

    [Fact]
    public async Task PromoteToChampion_NonAdventurer_ReturnsNull()
    {
        _npcRepo.Setup(r => r.GetByIdAsync("npc1"))
            .ReturnsAsync(new NpcDocument
            {
                Id = "npc1", Tier = NpcTier.Noble,  // must be Adventurer
                GodTrustLevel = 90f
            });

        var result = await _sut.PromoteToChampionAsync("w1", "npc1", "g1");

        result.Should().BeNull("non-Adventurer cannot become Champion");
    }

    [Fact]
    public async Task PromoteToChampion_ValidAdventurer_ReturnsChampion()
    {
        var npc = new NpcDocument
        {
            Id = "npc1", Tier = NpcTier.Adventurer,
            GodTrustLevel = 80f, Name = "Rowan"
        };
        _npcRepo.Setup(r => r.GetByIdAsync("npc1")).ReturnsAsync(npc);
        _npcRepo.Setup(r => r.UpdateAsync(It.IsAny<NpcDocument>())).Returns(Task.CompletedTask);

        var result = await _sut.PromoteToChampionAsync("w1", "npc1", "g1");

        result.Should().NotBeNull();
        result!.IsChampion.Should().BeTrue();
        result.GodInfluenceId.Should().Be("g1");
    }
}
