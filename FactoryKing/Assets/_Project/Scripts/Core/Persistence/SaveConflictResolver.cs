using FactoryKing.Core.Content;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Persistence
{
    public enum CloudChoice { KeepLocal, UseCloud }

    /// <summary>
    /// Chooses between the local save and the iCloud copy. Progress wins over recency so a stale device can never
    /// overwrite a more advanced game: prestige count, then lifetime earnings, then the newest save time.
    /// Purchases (entitlements and transaction ids) are merged so nothing paid for is ever lost.
    /// </summary>
    public static class SaveConflictResolver
    {
        public static CloudChoice Choose(GameState local, GameState cloud)
        {
            if (cloud == null) return CloudChoice.KeepLocal;
            if (local == null) return CloudChoice.UseCloud;
            if (cloud.Prestige.Count != local.Prestige.Count)
                return cloud.Prestige.Count > local.Prestige.Count ? CloudChoice.UseCloud : CloudChoice.KeepLocal;
            if (!cloud.LifetimeEarnings.ApproximatelyEquals(local.LifetimeEarnings, 1e-9))
                return cloud.LifetimeEarnings > local.LifetimeEarnings ? CloudChoice.UseCloud : CloudChoice.KeepLocal;
            return cloud.Time.LastSavedUtcTicks > local.Time.LastSavedUtcTicks ? CloudChoice.UseCloud : CloudChoice.KeepLocal;
        }

        /// <summary>Returns the winning state with purchases from both merged in.</summary>
        public static GameState Resolve(GameState local, GameState cloud, GameContent content)
        {
            var choice = Choose(local, cloud);
            var winner = choice == CloudChoice.UseCloud ? cloud : local;
            var loser = choice == CloudChoice.UseCloud ? local : cloud;
            if (loser != null)
            {
                winner.Entitlements.UnionWith(loser.Entitlements);
                winner.ProcessedTransactions.UnionWith(loser.ProcessedTransactions);
                winner.Collection.UnionWith(loser.Collection);
                if (loser.Time.MaxSeenUtcTicks > winner.Time.MaxSeenUtcTicks) winner.Time.MaxSeenUtcTicks = loser.Time.MaxSeenUtcTicks;
            }
            winner.EnsureConsistent(content);
            return winner;
        }
    }
}
