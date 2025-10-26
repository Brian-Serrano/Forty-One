using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    // login info
    public string playerAccessToken;
    public string playerRefreshToken;
    public int playerId;
    public string playerName;

    // data
    public int computerGamesPlayed;
    public int computerGamesWon;
    public int multiplayerGamesPlayed;
    public int multiplayerGamesWon;
    public string[] playersName;
    public bool playerNameSet;

    // audio settings
    public float musicVolume;
    public float sfxVolume;

    public PlayerData()
    {
        playerAccessToken = "";
        playerRefreshToken = "";
        playerId = 0;
        playerName = "";

        computerGamesPlayed = 0;
        computerGamesWon = 0;
        multiplayerGamesPlayed = 0;
        multiplayerGamesWon = 0;
        playersName = new string[] { "Player", "West", "North", "East" };
        playerNameSet = false;

        musicVolume = 1f;
        sfxVolume = 1f;
    }

    public static string GetPath()
    {
        //string customPath = Application.persistentDataPath;

        //if (ParrelSync.ClonesManager.IsClone())
        //{
        //    string cloneName = ParrelSync.ClonesManager.GetCurrentProject().name;
        //    customPath = Path.Combine(Application.persistentDataPath, cloneName);
        //}

        //return Path.Combine(customPath, "player_data.fo");

        return Path.Combine(Application.persistentDataPath, "player_data.fo");
    }

    public static PlayerData LoadData()
    {
        return PersistentDataController.LoadData<PlayerData>(GetPath());
    }

    public bool SaveData()
    {
        return PersistentDataController.SaveData(this, GetPath());
    }

    public static bool SaveData(byte[] data)
    {
        try
        {
            File.WriteAllBytes(GetPath(), data);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    public static byte[] ReadData()
    {
        string path = GetPath();

        if (File.Exists(path))
        {
            return File.ReadAllBytes(path);
        }
        else
        {
            Debug.LogError("File not found: " + path);
            return null;
        }
    }

    public void SetPlayerDataFromServer(PlayerData playerData)
    {
        computerGamesPlayed = playerData.computerGamesPlayed;
        computerGamesWon = playerData.computerGamesWon;
        multiplayerGamesPlayed = playerData.multiplayerGamesPlayed;
        multiplayerGamesWon = playerData.multiplayerGamesWon;
        playersName = playerData.playersName;
        playerNameSet = playerData.playerNameSet;

        musicVolume = playerData.musicVolume;
        sfxVolume = playerData.sfxVolume;
    }
}
