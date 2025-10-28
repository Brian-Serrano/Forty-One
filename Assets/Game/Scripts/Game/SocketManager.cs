using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SocketManager
{
    private static SocketManager instance;

    public SocketIOUnity socket;

    public event Action<string, SocketIOResponse> onServerEvent;

    public static SocketManager GetInstance(string playerName)
    {
        instance ??= new SocketManager(playerName);

        return instance;
    }

    private SocketManager(string playerName)
    {
        var uri = new Uri("https://briser-games-multiplayer-server.onrender.com");

        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
            {
                { "token", "UNITY" },
                { "player_name", playerName }
            },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        socket.OnConnected += (sender, e) => Debug.Log("Connected to Server");
        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("Disconnected from Server");
            onServerEvent?.Invoke("disconnected", null);
        };

        RegisterEvent("room_players");
        RegisterEvent("player_index");
        RegisterEvent("start_game");
        RegisterEvent("after_draw_card");
        RegisterEvent("after_discard_card");
        RegisterEvent("win");
        RegisterEvent("disconnect_on_game");
        RegisterEvent("one_player_win");
    }

    private void RegisterEvent(string eventName)
    {
        socket.On(eventName, (response) =>
        {
            onServerEvent?.Invoke(eventName, response);
        });
    }
}
