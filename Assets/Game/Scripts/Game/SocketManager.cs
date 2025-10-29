using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SocketManager
{
    public SocketIOUnity socket;

    public event Action<string, SocketIOResponse> onServerEvent;

    public SocketManager(string playerName)
    {
        CreateSocket(playerName);
    }

    private void CreateSocket(string playerName)
    {
        Uri uri = new Uri("https://briser-games-multiplayer-server.onrender.com");

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

        socket.OnConnected += OnConnected;
        socket.OnDisconnected += OnDisconnected;

        RegisterEvent("room_players");
        RegisterEvent("player_index");
        RegisterEvent("start_game");
        RegisterEvent("after_draw_card");
        RegisterEvent("after_discard_card");
        RegisterEvent("win_after_discard");
        RegisterEvent("disconnect_on_game");
        RegisterEvent("win");
    }

    private void OnConnected(object sender, EventArgs e)
    {
        Debug.Log("Connected to Server");
    }

    private void OnDisconnected(object sender, string e)
    {
        Debug.Log("Disconnected from Server");
        onServerEvent?.Invoke("disconnected", null);
    }

    private void RegisterEvent(string eventName)
    {
        socket.On(eventName, (response) =>
        {
            onServerEvent?.Invoke(eventName, response);
        });
    }

    public void CleanupSocket()
    {
        if (socket == null)
            return;

        try
        {
            socket.Off("room_players");
            socket.Off("player_index");
            socket.Off("start_game");
            socket.Off("after_draw_card");
            socket.Off("after_discard_card");
            socket.Off("win_after_discard");
            socket.Off("disconnect_on_game");
            socket.Off("win");

            socket.OnConnected -= OnConnected;
            socket.OnDisconnected -= OnDisconnected;

            socket.Dispose();
            socket = null;

            Debug.Log("Socket cleaned up successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error cleaning up socket: {ex.Message}");
        }
    }
}
