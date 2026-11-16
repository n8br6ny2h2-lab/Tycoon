import UIKit
import Capacitor

/// Main screen. Registers the app's own plugins (in-app purchases) with the Capacitor bridge.
class ViewController: CAPBridgeViewController {
    override open func capacitorDidLoad() {
        bridge?.registerPluginInstance(StorePlugin())
    }
}
