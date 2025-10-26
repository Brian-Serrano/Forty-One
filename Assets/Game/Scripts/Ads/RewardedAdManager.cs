using GoogleMobileAds.Api;
using System;
using System.IO;
using UnityEngine;

public class RewardedAdManager
{
    private static RewardedAdManager instance;
    private RewardedAd rewardedAd;

#if UNITY_ANDROID
    private string adUnitId = "ca-app-pub-5542507601288937/1070631011";
#elif UNITY_IOS
    private string adUnitId = "ca-app-pub-5542507601288937/6187226323";
#else
    private string adUnitId = "unexpected_platform";
#endif

    public static RewardedAdManager GetInstance()
    {
        instance ??= new RewardedAdManager();

        return instance;
    }

    private RewardedAdManager()
    {
        MobileAds.Initialize(initStatus => { });
        LoadRewardedAd();
    }

    public void LoadRewardedAd()
    {
        // If an ad already exists, unsubscribe and destroy it
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        AdRequest request = new AdRequest();

        RewardedAd.Load(adUnitId, request, (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError($"Failed to load rewarded ad: {error.GetMessage()}");
                return;
            }

            Debug.Log("Rewarded ad loaded successfully.");
            rewardedAd = ad;
        });
    }

    public void ShowRewardedAd(Action onRewardReceived, Action onAdClosed, Action onError)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.OnAdFullScreenContentClosed -= HandleAdClosed;
            rewardedAd.OnAdFullScreenContentFailed -= HandleAdFailed;

            rewardedAd.OnAdFullScreenContentClosed += HandleAdClosed;
            rewardedAd.OnAdFullScreenContentFailed += HandleAdFailed;

            rewardedAd.Show(reward =>
            {
                Debug.Log($"User earned reward: {reward.Amount} {reward.Type}");
                onRewardReceived?.Invoke();
            });

            void HandleAdClosed()
            {
                Debug.Log("Rewarded ad closed, loading another...");
                onAdClosed?.Invoke();
                LoadRewardedAd();
            }

            void HandleAdFailed(AdError adError)
            {
                Debug.LogError($"Rewarded ad failed to show: {adError.GetMessage()}");
                onError?.Invoke();
                LoadRewardedAd();
            };
        }
        else
        {
            Debug.Log("Rewarded ad is not ready yet.");
            onError?.Invoke();
            LoadRewardedAd();
        }
    }
}
