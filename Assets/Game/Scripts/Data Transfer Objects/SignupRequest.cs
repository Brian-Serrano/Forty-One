using Newtonsoft.Json;
using UnityEngine;

public class SignupRequest
{
    [JsonProperty("player_name")]
    public string playerName;

    [JsonProperty("email")]
    public string email;

    [JsonProperty("password")]
    public string password;

    [JsonProperty("confirm_password")]
    public string confirmPassword;

    public SignupRequest(string playerName, string email, string password, string confirmPassword)
    {
        this.playerName = playerName;
        this.email = email;
        this.password = password;
        this.confirmPassword = confirmPassword;
    }
}