using Newtonsoft.Json;
using UnityEngine;

public class LoginRequest
{
    [JsonProperty("player_name")]
    public string playerName;

    [JsonProperty("password")]
    public string password;

    public LoginRequest(string playerName, string password)
    {
        this.playerName = playerName;
        this.password = password;
    }
}