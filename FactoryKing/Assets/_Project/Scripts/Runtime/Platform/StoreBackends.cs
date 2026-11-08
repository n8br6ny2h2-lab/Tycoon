using System;
using System.Collections;
using System.Collections.Generic;
using FactoryKing.Core.Meta;
using UnityEngine;

namespace FactoryKing.Runtime.Platform
{
    public enum StoreFailure { None, Cancelled, PaymentDeclined, NotInitialized, ProductUnavailable, Pending, Unknown }

    /// <summary>A completed store transaction to be granted by <see cref="PurchaseService"/>.</summary>
    public struct StoreTransaction
    {
        public string StoreId;
        public string TransactionId;
        public bool ReceiptValid;
        public bool IsRestore;
    }

    /// <summary>Real-money store backend (Unity IAP / StoreKit). The game logic never talks to StoreKit directly.</summary>
    public interface IStoreBackend
    {
        bool IsReady { get; }
        string Name { get; }
        void Initialize(StoreCatalog catalog, Action<StoreTransaction> onTransaction, Action<string, StoreFailure> onFailure);
        void Purchase(string storeId);
        void RestorePurchases(Action<bool> done);
        /// <summary>Localized price from the App Store (e.g. "₺49,99"), or null if unknown.</summary>
        string LocalizedPrice(string storeId);
        /// <summary>Must be called after the transaction was granted and saved (StoreKit "finish transaction").</summary>
        void Confirm(string storeId);
    }

    /// <summary>Lets optional plugin assemblies (Unity IAP, ad SDKs) register themselves without hard references.</summary>
    public static class PlatformRegistry
    {
        public static Func<IStoreBackend> StoreFactory;
        public static Func<IAdsProvider> AdsFactory;
    }

    /// <summary>Used when no store is available (e.g. Unity IAP not installed). Never pretends to sell anything.</summary>
    public sealed class UnavailableStoreBackend : IStoreBackend
    {
        private Action<string, StoreFailure> _fail;
        public bool IsReady => false;
        public string Name => "Unavailable";
        public void Initialize(StoreCatalog catalog, Action<StoreTransaction> onTransaction, Action<string, StoreFailure> onFailure) => _fail = onFailure;
        public void Purchase(string storeId) => _fail?.Invoke(storeId, StoreFailure.NotInitialized);
        public void RestorePurchases(Action<bool> done) => done?.Invoke(false);
        public string LocalizedPrice(string storeId) => null;
        public void Confirm(string storeId) { }
    }

    /// <summary>
    /// Editor/development test store: simulates success, cancellation and declined payments (selectable from the
    /// Settings debug panel) so the full purchase flow can be tested without App Store sandbox. Not used in release.
    /// </summary>
    public sealed class TestStoreBackend : IStoreBackend
    {
        public static StoreFailure NextOutcome = StoreFailure.None;

        private StoreCatalog _catalog;
        private Action<StoreTransaction> _tx;
        private Action<string, StoreFailure> _fail;
        private readonly List<StoreTransaction> _history = new List<StoreTransaction>();

        public bool IsReady => _catalog != null;
        public string Name => "Test Store";

        public void Initialize(StoreCatalog catalog, Action<StoreTransaction> onTransaction, Action<string, StoreFailure> onFailure)
        {
            _catalog = catalog;
            _tx = onTransaction;
            _fail = onFailure;
        }

        public void Purchase(string storeId)
        {
            if (NextOutcome != StoreFailure.None)
            {
                var o = NextOutcome;
                NextOutcome = StoreFailure.None;
                _fail?.Invoke(storeId, o);
                return;
            }
            var t = new StoreTransaction { StoreId = storeId, TransactionId = "test-" + Guid.NewGuid().ToString("N"), ReceiptValid = true };
            if (_catalog.ByStoreId(storeId)?.Kind == ProductKind.NonConsumable) _history.Add(t);
            _tx?.Invoke(t);
        }

        public void RestorePurchases(Action<bool> done)
        {
            foreach (var t in _history)
            {
                var r = t;
                r.IsRestore = true;
                _tx?.Invoke(r);
            }
            done?.Invoke(true);
        }

        public string LocalizedPrice(string storeId) => _catalog?.ByStoreId(storeId)?.FallbackPrice;
        public void Confirm(string storeId) { }
    }

    /// <summary>Editor/development rewarded-ad simulator (2 s "ad"). Release builds use the registered SDK or none.</summary>
    public sealed class TestAdsProvider : IAdsProvider
    {
        public static AdResult NextResult = AdResult.Completed;
        private readonly MonoBehaviour _host;

        public TestAdsProvider(MonoBehaviour host) => _host = host;

        public bool IsReady(string placement) => true;

        public void Show(string placement, Action<AdResult> done) => _host.StartCoroutine(Run(done));

        private static IEnumerator Run(Action<AdResult> done)
        {
            yield return new WaitForSecondsRealtime(2f);
            var r = NextResult;
            NextResult = AdResult.Completed;
            done?.Invoke(r);
        }
    }
}
