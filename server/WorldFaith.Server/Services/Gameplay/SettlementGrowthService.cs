using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Gameplay;

// ─── Settlement Growth (Gameplay Spec §4) ─────────────────────
// NPCs grow a place Camp → Hamlet → Village → Town → City from their own needs
// and resources. A kingdom is a separate political milestone (§4.3): a large
// village does NOT automatically become a kingdom.

/// <summary>Snapshot of what a settlement currently has, used to classify its stage.</summary>
public record SettlementSnapshot(
    int Population,
    bool StableSupplies,   // permanent homes / repeated use of one location
    bool WorkRoles,        // specialized labor and roles
    bool LocalAuthority,   // elder / council / local law
    bool TradeRoutes,      // roads or routes to other places
    bool Surplus,          // food/goods beyond subsistence
    bool StrongEconomy,
    bool Defenses);

public record KingdomFormationInput(
    int SettlementCount,
    bool HasPowerfulCity,
    bool RecognizedLeadership,
    bool TerritorialClaims,
    bool EnforcementMeans,
    float Legitimacy);       // 0-100

public interface ISettlementGrowthService
{
    SettlementStage EvaluateStage(SettlementSnapshot s);
    bool CanFormKingdom(KingdomFormationInput input);
}

public class SettlementGrowthService : ISettlementGrowthService
{
    public SettlementStage EvaluateStage(SettlementSnapshot s)
    {
        // City: dense regional center with a strong economy, administration, defenses.
        if (s.Population >= 400 && s.StrongEconomy && s.Defenses && s.LocalAuthority)
            return SettlementStage.City;

        // Town: trade and specialization — routes, surplus, crafts, governance.
        if (s.Population >= 150 && s.TradeRoutes && s.Surplus && s.WorkRoles && s.LocalAuthority)
            return SettlementStage.Town;

        // Village: organized community with roles and local authority.
        if (s.Population >= 60 && s.WorkRoles && s.LocalAuthority)
            return SettlementStage.Village;

        // Hamlet: permanent homes / stable supplies at one location.
        if (s.Population >= 20 && s.StableSupplies)
            return SettlementStage.Hamlet;

        // Camp: a group with basic safety.
        return SettlementStage.Camp;
    }

    public bool CanFormKingdom(KingdomFormationInput input)
    {
        // Needs a critical mass of territory (several settlements or one powerful city),
        // recognized leadership, claims, a way to enforce decisions, and legitimacy.
        bool territory = input.SettlementCount >= 2 || input.HasPowerfulCity;
        return territory
            && input.RecognizedLeadership
            && input.TerritorialClaims
            && input.EnforcementMeans
            && input.Legitimacy >= 50f;
    }
}
