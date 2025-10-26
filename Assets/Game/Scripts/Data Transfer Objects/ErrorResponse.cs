using Newtonsoft.Json;
using UnityEngine;

public class ErrorResponse
{
    [JsonProperty("error")]
    public string error;

    [JsonProperty("details")]
    public string details;

    public ErrorResponse(string error, string details)
    {
        this.error = error;
        this.details = details;
    }
}