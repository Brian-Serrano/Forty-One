using CI.HttpClient;
using UnityEngine;

public class FortyOneHTTPClient
{
    private static FortyOneHTTPClient instance;

    public HttpClient client;
    public string baseUrl = "https://briser-games-server.onrender.com/";

    public static FortyOneHTTPClient GetInstance()
    {
        instance ??= new FortyOneHTTPClient();

        return instance;
    }

    private FortyOneHTTPClient()
    {
        client = new HttpClient();
    }

    public AuthorizationRoutes GetAuthorizationRoutes()
    {
        return AuthorizationRoutes.GetInstance(this);
    }

    public PlayerRoutes GetPlayerRoutes()
    {
        return PlayerRoutes.GetInstance(this);
    }
}
