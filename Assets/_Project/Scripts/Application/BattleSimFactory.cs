using TacticaUniversalis.Domain;

namespace TacticaUniversalis.Application
{
    public static class BattleSimFactory
    {
        public static IRoundSimulator CreateRoundSimulator(
            BattleSimMode mode, WeaponCatalog catalog = null)
        {
            if (mode == BattleSimMode.BehaviorTree)
                return new BehaviorTreeRoundSimulator(catalog ?? WeaponCatalog.CreateDefault());
            return new FastRoundSimulator();
        }
    }
}
