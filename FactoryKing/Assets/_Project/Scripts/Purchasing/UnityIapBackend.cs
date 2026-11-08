using System;
using FactoryKing.Core.Meta;
using FactoryKing.Runtime.Platform;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace FactoryKing.Purchasing
{
    /// <summary>
    /// StoreKit through Unity IAP 4.x. This assembly only compiles when com.unity.purchasing 4.9+ (&lt;5) is installed
    /// (asmdef versionDefines), and registers itself into <see cref="PlatformRegistry"/> at startup.
    /// Receipts are validated locally with Apple's root certificate via CrossPlatformValidator when the generated
    /// AppleTangle class exists (Services > In-App Purchasing > Receipt Validation Obfuscator).
    /// </summary>
    public sealed class UnityIapBackend : IStoreBackend, IDetailedStoreListener
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => PlatformRegistry.StoreFactory = () => new UnityIapBackend();

        private IStoreController _controller;
        private IExtensionProvider _extensions;
        private StoreCatalog _catalog;
        private Action<StoreTransaction> _onTx;
        private Action<string, StoreFailure> _onFail;
        private bool _restoring;

        public bool IsReady => _controller != null;
        public string Name => "Unity IAP";

        public void Initialize(StoreCatalog catalog, Action<StoreTransaction> onTransaction, Action<string, StoreFailure> onFailure)
        {
            _catalog = catalog;
            _onTx = onTransaction;
            _onFail = onFailure;
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var p in catalog.Products)
                builder.AddProduct(p.StoreId, p.Kind == ProductKind.Consumable ? ProductType.Consumable : ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public void Purchase(string storeId)
        {
            if (_controller == null) { _onFail?.Invoke(storeId, StoreFailure.NotInitialized); return; }
            var product = _controller.products.WithID(storeId);
            if (product == null || !product.availableToPurchase) { _onFail?.Invoke(storeId, StoreFailure.ProductUnavailable); return; }
            _controller.InitiatePurchase(product);
        }

        public void RestorePurchases(Action<bool> done)
        {
            if (_extensions == null) { done?.Invoke(false); return; }
            _restoring = true;
            _extensions.GetExtension<IAppleExtensions>().RestoreTransactions((ok, _) =>
            {
                _restoring = false;
                done?.Invoke(ok);
            });
        }

        public string LocalizedPrice(string storeId) => _controller?.products.WithID(storeId)?.metadata.localizedPriceString;

        public void Confirm(string storeId)
        {
            var product = _controller?.products.WithID(storeId);
            if (product != null) _controller.ConfirmPendingPurchase(product);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error) => Debug.LogWarning("[FactoryKing] IAP init failed: " + error);
        public void OnInitializeFailed(InitializationFailureReason error, string message) => Debug.LogWarning("[FactoryKing] IAP init failed: " + error + " " + message);

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var p = args.purchasedProduct;
            _onTx?.Invoke(new StoreTransaction
            {
                StoreId = p.definition.id,
                TransactionId = p.transactionID,
                ReceiptValid = ValidateReceipt(p),
                IsRestore = _restoring,
            });
            // Pending until StoreController saved the grant and calls Confirm().
            return PurchaseProcessingResult.Pending;
        }

        private static bool ValidateReceipt(Product p)
        {
#if !UNITY_EDITOR && FK_RECEIPT_VALIDATION
            try
            {
                var validator = new UnityEngine.Purchasing.Security.CrossPlatformValidator(null, AppleTangle.Data(), Application.identifier);
                validator.Validate(p.receipt);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FactoryKing] Receipt rejected: " + e.Message);
                return false;
            }
#else
            return !string.IsNullOrEmpty(p.transactionID);
#endif
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription d) => _onFail?.Invoke(product?.definition.id, Map(d.reason));
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) => _onFail?.Invoke(product?.definition.id, Map(reason));

        private static StoreFailure Map(PurchaseFailureReason r)
        {
            switch (r)
            {
                case PurchaseFailureReason.UserCancelled: return StoreFailure.Cancelled;
                case PurchaseFailureReason.PaymentDeclined: return StoreFailure.PaymentDeclined;
                case PurchaseFailureReason.ProductUnavailable: return StoreFailure.ProductUnavailable;
                case PurchaseFailureReason.PurchasingUnavailable: return StoreFailure.NotInitialized;
                case PurchaseFailureReason.ExistingPurchasePending: return StoreFailure.Pending;
                default: return StoreFailure.Unknown;
            }
        }
    }
}
