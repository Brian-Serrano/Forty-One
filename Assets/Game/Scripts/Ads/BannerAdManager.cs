using GoogleMobileAds.Api;
using UnityEngine;

public class BannerAdManager
{
    private static BannerAdManager instance;
    private BannerView bannerView;

#if UNITY_ANDROID
    private string adUnitId = "ca-app-pub-5542507601288937/1457358412";
#elif UNITY_IOS
    private string adUnitId = "ca-app-pub-5542507601288937/9000806513";
#else
    private string adUnitId = "unexpected_platform";
#endif

    public static BannerAdManager GetInstance()
    {
        instance ??= new BannerAdManager();

        return instance;
    }

    private BannerAdManager()
    {
        MobileAds.Initialize(initStatus => { });
        RequestBanner();
    }

    private void RequestBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
        }

        bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);

        AdRequest request = new AdRequest();

        bannerView.LoadAd(request);
    }

    public void EnsureBannerVisible()
    {
        RequestBanner();
    }
}
