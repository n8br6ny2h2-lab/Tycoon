using System;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Simulation;
using UnityEngine;

namespace FactoryKing.Runtime.Platform
{
    /// <summary>
    /// Glue between the store backend and <see cref="PurchaseService"/>: grants, saves, then confirms the
    /// transaction (so an interrupted purchase is re-delivered by StoreKit and granted exactly once).
    /// </summary>
    public sealed class StoreController
    {
        private readonly GameSession _s;
        private readonly SaveManager _save;

        public IStoreBackend Backend { get; }

        /// <summary>UI feedback: product, result message key.</summary>
        public event Action<StoreProductDef, string> Message;

        public StoreController(GameSession session, SaveManager save)
        {
            _s = session;
            _save = save;
            Backend = PlatformRegistry.StoreFactory?.Invoke();
            if (Backend == null)
                Backend = Debug.isDebugBuild || Application.isEditor ? new TestStoreBackend() : new UnavailableStoreBackend();
            Backend.Initialize(_s.Purchases.Catalog, OnTransaction, OnFailure);
        }

        public string PriceText(StoreProductDef p) => Backend.LocalizedPrice(p.StoreId) ?? p.FallbackPrice;

        public void Buy(StoreProductDef p)
        {
            if (!_s.Purchases.CanPurchase(p))
            {
                Message?.Invoke(p, "shop.msg.owned");
                return;
            }
            if (!Backend.IsReady)
            {
                Message?.Invoke(p, "shop.msg.unavailable");
                return;
            }
            Backend.Purchase(p.StoreId);
        }

        public void Restore() => Backend.RestorePurchases(ok => Message?.Invoke(null, ok ? "shop.msg.restored" : "shop.msg.restore_failed"));

        private void OnTransaction(StoreTransaction t)
        {
            var result = _s.Purchases.Process(t.StoreId, t.TransactionId, t.ReceiptValid, t.IsRestore);
            var p = _s.Purchases.Catalog.ByStoreId(t.StoreId);
            if (result == PurchaseResult.Granted || result == PurchaseResult.AlreadyProcessed || result == PurchaseResult.AlreadyOwned)
            {
                _save.SaveNow();          // persist before finishing the transaction
                Backend.Confirm(t.StoreId);
            }
            if (!t.IsRestore)
                Message?.Invoke(p, result == PurchaseResult.Granted ? "shop.msg.thanks"
                    : result == PurchaseResult.InvalidReceipt ? "shop.msg.invalid" : "shop.msg.owned");
        }

        private void OnFailure(string storeId, StoreFailure failure)
        {
            var p = _s.Purchases.Catalog.ByStoreId(storeId);
            string key;
            switch (failure)
            {
                case StoreFailure.Cancelled: key = "shop.msg.cancelled"; break;
                case StoreFailure.PaymentDeclined: key = "shop.msg.declined"; break;
                case StoreFailure.Pending: key = "shop.msg.pending"; break;
                case StoreFailure.NotInitialized:
                case StoreFailure.ProductUnavailable: key = "shop.msg.unavailable"; break;
                default: key = "shop.msg.failed"; break;
            }
            Message?.Invoke(p, key);
        }
    }
}
