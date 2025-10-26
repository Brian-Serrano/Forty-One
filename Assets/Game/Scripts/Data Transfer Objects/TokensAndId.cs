using Newtonsoft.Json;
using UnityEngine;

public class TokensAndId
{
    [JsonProperty("access_token")]
    public string accessToken;

    [JsonProperty("refresh_token")]
    public string refreshToken;

    [JsonProperty("player_id")]
    public int playerId;
}