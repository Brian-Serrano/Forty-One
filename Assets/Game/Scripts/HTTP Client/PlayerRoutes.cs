using CI.HttpClient;
using System;
using System.IO;
using UnityEngine;

public class PlayerRoutes
{
    private static PlayerRoutes instance;
    private FortyOneHTTPClient client;

    public static PlayerRoutes GetInstance(FortyOneHTTPClient client)
    {
        instance ??= new PlayerRoutes(client);

        return instance;
    }

    private PlayerRoutes(FortyOneHTTPClient client)
    {
        this.client = client;
    }

    public void SavePlayerData(string token, Action<SuccessMessage> responseCallback, Action<ErrorResponse> errorCallback, Action<int> progressCallback)
    {
        string url = client.baseUrl + "fo_player_routes/save_player_data";
        byte[] compressedFile = PlayerData.ReadData();

        MultipartFormDataContent multipartFormData = new MultipartFormDataContent
        {
            { new ByteArrayContent(compressedFile, "application/octet-stream"), "data", "player_data.playerdata" }
        };

        HttpRequestMessage message = new HttpRequestMessage
        {
            Uri = new Uri(url),
            Method = HttpAction.Post,
            Content = multipartFormData
        };

        message.Headers.Add("Authorization", token);

        client.client.Send(message, HttpCompletionOption.StreamResponseContent, response => {

            if (response.PercentageComplete == 100 && response.IsSuccessStatusCode)
            {
                responseCallback?.Invoke(response.ReadAsJson<SuccessMessage>());
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                if (!response.HasContent)
                {
                    errorCallback?.Invoke(new ErrorResponse("Server unavailable", "Server not running."));
                    return;
                }

                errorCallback?.Invoke(response.ReadAsJson<ErrorResponse>());
                return;
            }
        }, upload =>
        {
            progressCallback?.Invoke(upload.PercentageComplete);
        });
    }

    public void LoadPlayerData(string token, Action<SuccessMessage> responseCallback, Action<ErrorResponse> errorCallback, Action<int> progressCallback)
    {
        string url = client.baseUrl + "fo_player_routes/load_player_data";

        HttpRequestMessage message = new HttpRequestMessage
        {
            Uri = new Uri(url),
            Method = HttpAction.Get
        };

        message.Headers.Add("Authorization", token);

        byte[] data = null;

        client.client.Send(message, HttpCompletionOption.StreamResponseContent, response => {
            try
            {
                data ??= new byte[response.ContentLength];

                response.ReadAsByteArray().CopyTo(data, response.TotalContentRead - response.ContentReadThisRound);

                if (response.PercentageComplete == 100 && response.IsSuccessStatusCode)
                {
                    PlayerData.SaveData(data);
                    responseCallback?.Invoke(new SuccessMessage { message = "Your data has been successfully restored." });
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    if (!response.HasContent)
                    {
                        errorCallback?.Invoke(new ErrorResponse("Server unavailable", "Server not running."));
                        return;
                    }

                    errorCallback?.Invoke(response.ReadAsJson<ErrorResponse>());
                    return;
                }

                progressCallback?.Invoke(response.PercentageComplete);
            }
            catch (Exception e)
            {
                errorCallback?.Invoke(new ErrorResponse("Server error", $"Error: {e.Message}"));
            }
        });
    }
}
