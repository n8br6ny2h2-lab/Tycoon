using System.Collections;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Bootstrap;
using FactoryKing.Runtime.Visuals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FactoryKing.Tests.PlayMode
{
    /// <summary>Boots the real game in the Editor/player and checks the scene, UI and economy come alive.</summary>
    public class GameSmokeTests
    {
        /// <summary>Reuses an auto-created bootstrap if the test scene already has one; never writes the save.</summary>
        private static int ExpectedMachines(GameBootstrap b)
        {
            int n = 0;
            foreach (var l in b.Session.State.GetFactory(b.Session.State.ActiveFactoryId).Lines) if (l.Unlocked) n += l.Machines.Count;
            return n;
        }

        private static GameBootstrap Boot(out GameObject go)
        {
            var boot = GameBootstrap.Instance;
            if (boot == null)
            {
                go = new GameObject("FactoryKing");
                boot = go.AddComponent<GameBootstrap>();
            }
            go = boot.gameObject;
            boot.Save.Suppressed = true;
            return boot;
        }

        [UnityTest]
        public IEnumerator Game_Boots_Builds_Factory_And_Earns_Money()
        {
            var boot = Boot(out var go);
            yield return null;
            Assert.That(boot.Session, Is.Not.Null);
            Assert.That(boot.Ui, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<MachineView>(FindObjectsSortMode.None).Length, Is.EqualTo(ExpectedMachines(boot)));

            boot.Driver.Paused = false; // skip the main menu
            var cash = boot.Session.Wallet.Cash;
            float start = Time.realtimeSinceStartup;
            while (boot.Session.State.TotalUnitsSold == 0 && Time.realtimeSinceStartup - start < 30f) yield return null;
            Assert.That(boot.Session.State.TotalUnitsSold, Is.GreaterThan(0));

            boot.Session.State.Cash = 1e6;
            Assert.That(boot.Session.Upgrades.TryUpgrade(new MachineRef(boot.Session.State.ActiveFactoryId, 0, 1), 1), Is.True);
            boot.Session.Factories.TryUnlockLine(boot.Session.State.ActiveFactoryId, 1);
            yield return null;
            Assert.That(Object.FindObjectsByType<MachineView>(FindObjectsSortMode.None).Length, Is.EqualTo(ExpectedMachines(boot)));

            boot.Save.Suppressed = true;
            Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Frame_Time_Is_Reasonable_With_All_Lines()
        {
            var boot = Boot(out var go);
            yield return null;
            boot.Session.State.Cash = 1e9;
            boot.Session.Factories.TryUnlockLine("steel", 1);
            boot.Session.Factories.TryUnlockLine("steel", 2);
            boot.Driver.Paused = false;
            for (int i = 0; i < 30; i++) yield return null;
            float t0 = Time.realtimeSinceStartup;
            for (int i = 0; i < 120; i++) yield return null;
            float avgMs = (Time.realtimeSinceStartup - t0) / 120f * 1000f;
            Debug.Log($"[FactoryKing] average frame {avgMs:0.0} ms in editor");
            Assert.That(avgMs, Is.LessThan(50f), "editor frame time sanity bound");
            boot.Save.Suppressed = true;
            Object.Destroy(go);
            yield return null;
        }
    }
}
