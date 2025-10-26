using Newtonsoft.Json;
using UnityEngine;

public class Tokens
{
    [JsonProperty("access_token")]
    public string accessToken;

    [JsonProperty("refresh_token")]
    public string refreshToken;
}
