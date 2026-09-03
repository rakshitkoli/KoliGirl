using System;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Handles the one-time "Act 2" purchase (Levels 11-20) via Google Play Billing (Unity IAP).
/// Lives on a persistent object created once in Start Menu (DontDestroyOnLoad), same pattern as
/// AdsManager, so it survives every scene load thereafter.
///
/// The product id below MUST match a Managed Product created in the Play Console (Monetize ->
/// Products -> In-app products) with the exact same id, type "Managed product" (non-consumable/
/// one-time). It only actually works on a real Android build signed with the release keystore,
/// installed from (or set up as a licence tester on) Play - nothing purchasable happens in the
/// Editor, same limitation as AdsManager's ads.
/// </summary>
public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance { get; private set; }

    [Tooltip("Must exactly match the Product ID configured in Play Console -> Monetize -> " +
        "Products -> In-app products for the Act 2 unlock.")]
    [SerializeField] private string act2ProductId = "unlock_act2";

    private IStoreController storeController;
    private IExtensionProvider storeExtensions;
    private bool isInitialized;

    /// <summary>Fires once initialization finishes (success or failure) - the Levels page uses
    /// this to know when it's safe to trust IsAct2Purchased()/CanPurchaseAct2 rather than
    /// showing a stale "not purchased" state while Billing is still connecting.</summary>
    public event Action OnReady;

    public bool IsReady => isInitialized;

    /// <summary>True once Billing is up AND the store actually has this product listed - guards
    /// against offering a "Buy" button that would just fail (e.g. product not yet propagated
    /// in Play Console, or Billing unavailable on this device).</summary>
    public bool CanPurchaseAct2 => isInitialized && storeController != null
        && storeController.products.WithID(act2ProductId) != null
        && storeController.products.WithID(act2ProductId).availableToPurchase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePurchasing();
    }

    private void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(act2ProductId, ProductType.NonConsumable);
        UnityPurchasing.Initialize(this, builder);
    }

    /// <summary>Wired to the Levels page's "Buy" prompt (shown in place of the lock icon's
    /// normal do-nothing tap, only for level 11+ while CanPurchaseAct2 is true).</summary>
    public void BuyAct2()
    {
        if (!CanPurchaseAct2)
        {
            Debug.LogWarning("IAPManager: BuyAct2 called but store isn't ready / product unavailable.");
            return;
        }
        storeController.InitiatePurchase(act2ProductId);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        storeExtensions = extensions;
        isInitialized = true;
        Debug.Log("IAPManager: Billing initialized. Act2 product available=" + CanPurchaseAct2);

        // A non-consumable the player already owns (prior purchase, reinstall, new device) is
        // reported here automatically on init - this is the "restore" path on Android, no
        // separate restore button needed.
        var product = controller.products.WithID(act2ProductId);
        if (product != null && product.hasReceipt)
        {
            OnPurchaseConfirmed();
        }

        OnReady?.Invoke();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogWarning("IAPManager: Billing initialization failed: " + error);
        isInitialized = false;
        OnReady?.Invoke();
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogWarning("IAPManager: Billing initialization failed: " + error + " - " + message);
        isInitialized = false;
        OnReady?.Invoke();
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        if (args.purchasedProduct.definition.id == act2ProductId)
        {
            OnPurchaseConfirmed();
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogWarning("IAPManager: Purchase failed for '" + product.definition.id + "': " + reason);
    }

    private void OnPurchaseConfirmed()
    {
        if (LevelProgress.IsAct2Purchased()) return; // already recorded, avoid redundant logging
        LevelProgress.SetAct2Purchased(true);
        Debug.Log("IAPManager: Act 2 unlocked.");
    }
}
