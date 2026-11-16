import Foundation
import Capacitor
import StoreKit

/// In-app purchases for Demir Baronu with StoreKit 2. No third-party service: Apple verifies every transaction.
/// JS side: window.Capacitor.Plugins.Store (see play.html, "IAP").
@objc(StorePlugin)
public class StorePlugin: CAPPlugin, CAPBridgedPlugin {
    public let identifier = "StorePlugin"
    public let jsName = "Store"
    public let pluginMethods: [CAPPluginMethod] = [
        CAPPluginMethod(name: "getProducts", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "purchase", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "entitlements", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "restore", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "manageSubscriptions", returnType: CAPPluginReturnPromise)
    ]

    private var products: [String: Product] = [:]
    private var updates: Task<Void, Never>?

    override public func load() {
        // Purchases completed outside the purchase sheet (Ask to Buy approvals, renewals, interrupted purchases).
        updates = Task.detached { [weak self] in
            for await result in Transaction.updates {
                guard case .verified(let transaction) = result else { continue }
                self?.notifyListeners("transaction", data: StorePlugin.data(transaction), retainUntilConsumed: true)
                await transaction.finish()
            }
        }
    }

    deinit { updates?.cancel() }

    static func data(_ t: Transaction) -> [String: Any] {
        var d: [String: Any] = ["productId": t.productID, "transactionId": String(t.id)]
        if let e = t.expirationDate { d["expires"] = e.timeIntervalSince1970 * 1000 }
        if t.revocationDate != nil { d["revoked"] = true }
        return d
    }

    @objc func getProducts(_ call: CAPPluginCall) {
        let ids = call.getArray("ids", String.self) ?? []
        Task {
            do {
                let list = try await Product.products(for: ids)
                var out: [[String: Any]] = []
                for p in list {
                    self.products[p.id] = p
                    out.append(["id": p.id, "title": p.displayName, "description": p.description, "price": p.displayPrice])
                }
                call.resolve(["products": out])
            } catch {
                call.reject(error.localizedDescription)
            }
        }
    }

    @objc func purchase(_ call: CAPPluginCall) {
        guard let id = call.getString("id") else { call.reject("Ürün kimliği yok"); return }
        Task {
            do {
                var product = self.products[id]
                if product == nil { product = try await Product.products(for: [id]).first }
                guard let p = product else { call.reject("Ürün App Store'da bulunamadı"); return }
                let result = try await p.purchase()
                switch result {
                case .success(let verification):
                    switch verification {
                    case .verified(let t):
                        var d = StorePlugin.data(t)
                        d["status"] = "purchased"
                        await t.finish()
                        call.resolve(d)
                    case .unverified(_, let error):
                        call.reject("Satın alma doğrulanamadı: \(error.localizedDescription)")
                    }
                case .userCancelled:
                    call.resolve(["status": "cancelled"])
                case .pending:
                    call.resolve(["status": "pending"])
                @unknown default:
                    call.resolve(["status": "unknown"])
                }
            } catch {
                call.reject(error.localizedDescription)
            }
        }
    }

    /// Non-consumables and active subscriptions the user currently owns.
    @objc func entitlements(_ call: CAPPluginCall) {
        Task {
            var owned: [[String: Any]] = []
            for await result in Transaction.currentEntitlements {
                if case .verified(let t) = result, t.revocationDate == nil { owned.append(StorePlugin.data(t)) }
            }
            call.resolve(["owned": owned])
        }
    }

    @objc func restore(_ call: CAPPluginCall) {
        Task {
            try? await AppStore.sync()
            self.entitlements(call)
        }
    }

    @objc func manageSubscriptions(_ call: CAPPluginCall) {
        Task { @MainActor in
            if let scene = UIApplication.shared.connectedScenes.first(where: { $0 is UIWindowScene }) as? UIWindowScene {
                try? await AppStore.showManageSubscriptions(in: scene)
            }
            call.resolve()
        }
    }
}
