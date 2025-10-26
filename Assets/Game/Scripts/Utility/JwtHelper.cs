using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public static class JwtHelper
{
    public static Dictionary<string, object> DecodePayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
            throw new ArgumentException("Invalid JWT");

        string payload = parts[1];
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var jsonBytes = Convert.FromBase64String(payload);
        string json = Encoding.UTF8.GetString(jsonBytes);

        return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
    }

    public static bool IsExpired(string token)
    {
        try
        {
            var payload = DecodePayload(token);
            if (payload.ContainsKey("exp"))
            {
                long exp = Convert.ToInt64(payload["exp"]);
                DateTimeOffset expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
                return expDate < DateTimeOffset.UtcNow;
            }
            return true;
        }
        catch (Exception)
        {
            return true;
        }
    }
}