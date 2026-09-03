using System;
using UnityEngine;
using GoogleMobileAds.Api;

/// <summary>
/// Central AdMob integration point. Lives on a persistent object created once in Start Menu
/// (DontDestroyOnLoad) so it survives every scene load thereafter - initializes the SDK once,
/// keeps a Rewarded ad "warm" (pre-loaded, so there's no wait when the player taps Watch Ad) for
/// the extra-life flow, and shows an Interstitial every few level completions rather than every
/// single one - frequent interstitials both violate AdMob policy and tank retention.
///
/// Ad units were created in the AdMob console under the "KoliGirl" app. The SDK only serves real
/// ads on an actual Android/iOS build - nothing renders in the Editor, so testing this end to end
/// needs a device build.
/// </summary>
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [Tooltip("Google's own test ad units - always fill instantly, so the continue/interstitial " +
        "flow can actually be verified on a device. Watching your OWN real ad units repeatedly " +
        "during development also risks an AdMob \"invalid traffic\" policy flag, on top of a " +
        "brand-new/unapproved app often having low or zero real fill anyway. Switch this off only " +
        "once you're deliberately testing with the real ad units before a store release.")]
    [SerializeField] private bool useTestAdUnits = true;

    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-4363065684282456/1574251717";
    [SerializeField] private string interstitialAdUnitId = "ca-app-pub-4363065684282456/9700153745";

    // Google's official sample ad units (https://developers.google.com/admob/android/test-ads) -
    // safe to hardcode, documented specifically for this purpose.
    private const string TestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    private const string TestInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";

    [Tooltip("Show an interstitial after this many level completions rather than every one - " +
        "frequent interstitials both risk an AdMob policy violation and tank retention.")]
    [SerializeField] private int levelsPerInterstitial = 4;

    [Tooltip("Retry a failed ad load with backoff instead of giving up for the rest of the " +
        "session. On a brand-new install, Google Play Services' Ads module commonly fails its " +
        "very first \"app settings\" fetch and every ad request that session errors out with a " +
        "generic network error even though the device's internet is completely fine - confirmed " +
        "via device logcat: the SAME device loads ads successfully on the very next app launch. " +
        "Without a retry, every real player's first-ever session would silently never see the " +
        "Watch Ad button.")]
    [SerializeField] private int maxLoadRetries = 5;
    [SerializeField] private float retryBaseDelaySeconds = 2f;

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private int levelsSinceLastInterstitial;
    private int rewardedRetryCount;
    private int interstitialRetryCount;

    private string ActiveRewardedAdUnitId => useTestAdUnits ? TestRewardedAdUnitId : rewardedAdUnitId;
    private string ActiveInterstitialAdUnitId => useTestAdUnits ? TestInterstitialAdUnitId : interstitialAdUnitId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("AdsManager: calling MobileAds.Initialize...");
        MobileAds.Initialize(status =>
        {
            // Log each adapter's own init state (e.g. "Google Mobile Ads SDK") - if this callback
            // never fires at all on device, that itself is the finding (network/Play Services
            // issue on that device); if it fires but an adapter reports NOT_READY, the message
            // says why.
            foreach (var kv in status.getAdapterStatusMap())
            {
                Debug.Log("AdsManager: adapter '" + kv.Key + "' state=" + kv.Value.InitializationState
                    + " latency=" + kv.Value.Latency + "ms desc=" + kv.Value.Description);
            }
            LoadRewardedAd();
            LoadInterstitialAd();
        });
    }

    /// <summary>True once a Rewarded ad has actually finished loading and is ready to show -
    /// check this before offering the "Watch Ad" button so it isn't shown for an ad that isn't
    /// ready yet (e.g. right after a fresh app launch, before the first load completes).</summary>
    public bool IsRewardedAdReady => rewardedAd != null && rewardedAd.CanShowAd();

    /// <summary>Called by the "Watch Ad for Extra Life" button on the continue-after-death flow.
    /// onRewardEarned fires only on a genuine full watch - AdMob's reward callback doesn't fire
    /// for an early skip/close, so a player who bails out mid-ad correctly gets nothing.</summary>
    public void ShowRewardedAd(Action onRewardEarned)
    {
        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            Debug.LogWarning("AdsManager: ShowRewardedAd called but no ad is ready (rewardedAd is "
                + (rewardedAd == null ? "null" : "non-null but CanShowAd()==false") + ") - re-requesting a load.");
            LoadRewardedAd();
            return;
        }

        rewardedAd.OnAdFullScreenContentClosed += HandleRewardedClosed;
        rewardedAd.OnAdFullScreenContentFailed += HandleRewardedShowFailed;
        rewardedAd.Show((Reward reward) => { onRewardEarned?.Invoke(); });
    }

    private void HandleRewardedShowFailed(AdError error)
    {
        Debug.LogWarning("AdsManager: Rewarded ad FAILED to show: " + error);
        rewardedAd.OnAdFullScreenContentClosed -= HandleRewardedClosed;
        rewardedAd.OnAdFullScreenContentFailed -= HandleRewardedShowFailed;
        LoadRewardedAd();
    }

    private void HandleRewardedClosed()
    {
        rewardedAd.OnAdFullScreenContentClosed -= HandleRewardedClosed;
        LoadRewardedAd(); // pre-load the next one right away so it's warm for next time
    }

    private void LoadRewardedAd()
    {
        Debug.Log("AdsManager: requesting Rewarded ad, unit=" + ActiveRewardedAdUnitId);
        var request = new AdRequest();
        RewardedAd.Load(ActiveRewardedAdUnitId, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("AdsManager: Rewarded ad failed to load: " + error);
                RetryLoad(ref rewardedRetryCount, LoadRewardedAd, "Rewarded");
                return;
            }
            Debug.Log("AdsManager: Rewarded ad loaded successfully.");
            rewardedRetryCount = 0;
            rewardedAd = ad;
        });
    }

    /// <summary>Shared retry-with-backoff for a failed load - see maxLoadRetries' tooltip for why
    /// this exists (a brand-new install's first ad request commonly fails once through no fault
    /// of the network or this app's config, per device logcat).</summary>
    private void RetryLoad(ref int retryCount, Action loadAgain, string label)
    {
        if (!gameObject.activeInHierarchy) return; // don't schedule work past teardown
        if (retryCount >= maxLoadRetries)
        {
            Debug.LogWarning("AdsManager: " + label + " gave up after " + retryCount + " retries.");
            return;
        }
        retryCount++;
        float delay = retryBaseDelaySeconds * retryCount; // simple linear backoff: 2s, 4s, 6s...
        Debug.Log("AdsManager: retrying " + label + " load in " + delay + "s (attempt " + retryCount + "/" + maxLoadRetries + ")");
        StartCoroutine(RetryAfterDelay(delay, loadAgain));
    }

    private System.Collections.IEnumerator RetryAfterDelay(float delay, Action loadAgain)
    {
        yield return new WaitForSeconds(delay);
        loadAgain();
    }

    /// <summary>Called by GameManager.CompleteLevel - counts toward the next interstitial
    /// instead of showing one after every level.</summary>
    public void NotifyLevelCompleted()
    {
        levelsSinceLastInterstitial++;
        if (levelsSinceLastInterstitial < levelsPerInterstitial) return;

        levelsSinceLastInterstitial = 0;
        ShowInterstitial();
    }

    private void ShowInterstitial()
    {
        if (interstitialAd == null || !interstitialAd.CanShowAd())
        {
            Debug.LogWarning("AdsManager: ShowInterstitial called but no ad is ready - re-requesting a load.");
            LoadInterstitialAd();
            return;
        }

        interstitialAd.OnAdFullScreenContentClosed += HandleInterstitialClosed;
        interstitialAd.OnAdFullScreenContentFailed += HandleInterstitialShowFailed;
        interstitialAd.Show();
    }

    private void HandleInterstitialShowFailed(AdError error)
    {
        Debug.LogWarning("AdsManager: Interstitial FAILED to show: " + error);
        interstitialAd.OnAdFullScreenContentClosed -= HandleInterstitialClosed;
        interstitialAd.OnAdFullScreenContentFailed -= HandleInterstitialShowFailed;
        LoadInterstitialAd();
    }

    private void HandleInterstitialClosed()
    {
        interstitialAd.OnAdFullScreenContentClosed -= HandleInterstitialClosed;
        LoadInterstitialAd();
    }

    private void LoadInterstitialAd()
    {
        Debug.Log("AdsManager: requesting Interstitial ad, unit=" + ActiveInterstitialAdUnitId);
        var request = new AdRequest();
        InterstitialAd.Load(ActiveInterstitialAdUnitId, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("AdsManager: Interstitial failed to load: " + error);
                RetryLoad(ref interstitialRetryCount, LoadInterstitialAd, "Interstitial");
                return;
            }
            Debug.Log("AdsManager: Interstitial loaded successfully.");
            interstitialRetryCount = 0;
            interstitialAd = ad;
        });
    }
}
