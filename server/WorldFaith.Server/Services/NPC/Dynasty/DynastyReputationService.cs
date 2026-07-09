using WorldFaith.Server.Models;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// ─── Dynasty Reputation (Dynasty Spec §14) ────────────────────
// Family honor, infamy, divine favor and legitimacy shift as dynasty events
// happen — a blessing lifts a house, a curse stains it, a succession dispute
// erodes its legitimacy.

public interface IDynastyReputationService
{
    void ApplyEvent(FamilyHouseDocument family, DynastyHistoryEvent evt);
}

public class DynastyReputationService : IDynastyReputationService
{
    public void ApplyEvent(FamilyHouseDocument family, DynastyHistoryEvent evt)
    {
        switch (evt.EventType)
        {
            case DynastyEventType.Blessed:
                family.DivineFavor += 10f;
                family.Honor += 3f;
                break;
            case DynastyEventType.Cursed:
                family.Infamy += 8f;
                family.DivineFavor -= 5f;
                break;
            case DynastyEventType.HybridMutation:
                family.Honor += 5f;
                family.Infamy += 2f; // people may fear what is new
                break;
            case DynastyEventType.SuccessionDispute:
                family.PoliticalLegitimacy -= 12f;
                break;
            case DynastyEventType.Extinction:
                family.Status = FamilyStatus.Extinct;
                break;
            case DynastyEventType.Revival:
                family.Status = FamilyStatus.Revived;
                break;
        }

        family.Honor = Math.Clamp(family.Honor, -100f, 100f);
        family.Infamy = Math.Clamp(family.Infamy, 0f, 100f);
        family.DivineFavor = Math.Clamp(family.DivineFavor, -100f, 100f);
        family.PoliticalLegitimacy = Math.Clamp(family.PoliticalLegitimacy, -100f, 100f);
    }
}
