using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class InterstitialAdManager
{
    private static InterstitialAdManager instance;
    private InterstitialAd interstitialAd;

#if UNITY_ANDROID
    private string adUnitId = "ca-app-pub-5542507601288937/8598493596";
#elif UNITY_IOS
    private string adUnitId = "ca-app-pub-5542507601288937/6518113403";
#else
    private string adUnitId = "unexpected_platform";
#endif

    public static InterstitialAdManager GetInstance()
    {
        instance ??= new InterstitialAdManager();

        return instance;
    }

    private InterstitialAdManager()
    {
        MobileAds.Initialize(initStatus => { });
        LoadInterstitial();
    }

    public void LoadInterstitial()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }

        AdRequest request = new AdRequest();
        InterstitialAd.Load(adUnitId, request, (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogWarning("Interstitial failed to load: " + error);
                return;
            }

            interstitialAd = ad;
            Debug.Log("Interstitial loaded.");
        });
    }

    public void ShowInterstitial(Action onAdClosed)
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.OnAdFullScreenContentClosed -= HandleAdClosed;
            interstitialAd.OnAdFullScreenContentFailed -= HandleAdFailed;

            interstitialAd.OnAdFullScreenContentClosed += HandleAdClosed;
            interstitialAd.OnAdFullScreenContentFailed += HandleAdFailed;

            interstitialAd.Show();

            void HandleAdClosed()
            {
                Debug.Log("Interstitial closed, reloading...");
                onAdClosed?.Invoke();
                LoadInterstitial();
            }

            void HandleAdFailed(AdError adError)
            {
                Debug.LogWarning($"Interstitial failed to show: {adError.GetMessage()}");
                onAdClosed?.Invoke();
                LoadInterstitial();
            }
        }
        else
        {
            Debug.Log("Interstitial not ready, loading now...");

            onAdClosed?.Invoke();

            LoadInterstitial();
        }
    }
}