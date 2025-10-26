using Newtonsoft.Json;
using UnityEngine;

public class RefreshToken
{
    [JsonProperty("refresh_token")]
    public string refreshToken;

    public RefreshToken(string refreshToken)
    {
        this.refreshToken = refreshToken;
    }
}
