using Newtonsoft.Json.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public Transform noInternetPanel;
    public Animator crossfade;

    public Transform settingsPanel;
    public Slider musicSlider;
    public Slider sfxSlider;
    public List<TMP_Text> playerNamesTxt;
    public List<Button> playerNamesEditButton;

    public Transform statsPanel;
    public TMP_Text computerGamesPlayedTxt;
    public TMP_Text computerGamesWonTxt;
    public TMP_Text multiplayerGamesPlayedTxt;
    public TMP_Text multiplayerGamesWonTxt;

    public Transform editNamePanel;
    public TMP_Text editNameLabel;
    public TMP_InputField editNameInputField;
    public Button editNameOkButton;

    public Transform accountPanel;
    public Transform loginPanel;
    public Transform signupPanel;
    public TMP_InputField loginUsernameTxt;
    public TMP_InputField loginPasswordTxt;
    public TMP_InputField signupUsernameTxt;
    public TMP_InputField signupEmailTxt;
    public TMP_InputField signupPasswordTxt;
    public TMP_InputField signupConfirmPasswordTxt;

    public Button signupButton;
    public Button loginButton;
    public Button saveButton;
    public Button loadButton;
    public Button logoutButton;
    public Button signupSubmitButton;
    public Button loginSubmitButton;

    public TMP_Text accountLoginText;
    public GameObject spinnerContainer;

    public Transform confirmPanel;
    public TMP_Text confirmPanelText;
    public Button confirmPanelOkButton;

    public AudioSource buttonClickSfx;

    public AudioMixer audioMixer;

    private PlayerData playerData;
    private ToastManager toastManager;
    private FortyOneHTTPClient client;

    private void Awake()
    {
        playerData = PlayerData.LoadData();
        toastManager = GetComponent<ToastManager>();
        client = FortyOneHTTPClient.GetInstance();

        BannerAdManager.GetInstance().EnsureBannerVisible();

        if (!playerData.playerNameSet)
        {
            editNameLabel.text = $"Enter Player Name";
            editNameInputField.text = playerData.playersName[0];

            editNamePanel.gameObject.SetActive(true);
            editNamePanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);

            editNameOkButton.onClick.RemoveAllListeners();
            editNameOkButton.onClick.AddListener(() =>
            {
                if (ValidateNickname(editNameInputField.text.Trim()))
                {
                    playerData.playersName[0] = editNameInputField.text.Trim();
                    playerData.playerNameSet = true;
                    playerData.SaveData();

                    toastManager.ShowToast($"Player Name Changed");

                    CloseEditNamePanel();
                }
            });
        }
    }

    private void Start()
    {
        UpdateMusicVolume();
        UpdateSfxVolume();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!spinnerContainer.activeSelf)
            {
                if (statsPanel.gameObject.activeSelf)
                {
                    CloseStatsPanel();
                }
                else if (settingsPanel.gameObject.activeSelf && !editNamePanel.gameObject.activeSelf)
                {
                    CloseSettingsPanel();
                }
                else if (editNamePanel.gameObject.activeSelf)
                {
                    CloseEditNamePanel();
                }
                else if (accountPanel.gameObject.activeSelf && !signupPanel.gameObject.activeSelf && !loginPanel.gameObject.activeSelf && !confirmPanel.gameObject.activeSelf)
                {
                    CloseAccountPanel();
                }
                else if (signupPanel.gameObject.activeSelf)
                {
                    CloseSignupPanel();
                }
                else if (loginPanel.gameObject.activeSelf)
                {
                    CloseLoginPanel();
                }
                else if (confirmPanel.gameObject.activeSelf)
                {
                    CloseConfirmPanel();
                }
                else if (noInternetPanel.gameObject.activeSelf)
                {
                    CloseNoInternetPanel();
                }
                else
                {
                    Quit();
                }
            }
        }
    }

    public void ComputerGame()
    {
        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Computer Game"));
    }

    public void MultiplayerGame()
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            buttonClickSfx.Play();
            StartCoroutine(SwitchScene("Multiplayer Game"));
        }
        else
        {
            noInternetPanel.gameObject.SetActive(true);
            buttonClickSfx.Play();
            noInternetPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        }
    }

    public void CloseNoInternetPanel()
    {
        noInternetPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(noInternetPanel));
    }

    public void Quit()
    {
        buttonClickSfx.Play();
        Application.Quit();
    }

    public void OpenStatsPanel()
    {
        computerGamesPlayedTxt.text = playerData.computerGamesPlayed.ToString();
        computerGamesWonTxt.text = playerData.computerGamesWon.ToString();
        multiplayerGamesPlayedTxt.text = playerData.multiplayerGamesPlayed.ToString();
        multiplayerGamesWonTxt.text = playerData.multiplayerGamesWon.ToString();

        statsPanel.gameObject.SetActive(true);
        buttonClickSfx.Play();
        statsPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
    }

    public void CloseStatsPanel()
    {
        statsPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(statsPanel));
    }

    public void OpenSettingsPanel()
    {
        List<string> labels = new List<string> { "Player", "Computer 1", "Computer 2", "Computer 3" };

        musicSlider.value = playerData.musicVolume;
        sfxSlider.value = playerData.sfxVolume;

        for (int i = 0; i < playerData.playersName.Length; i++)
        {
            int index = i;

            playerNamesTxt[index].text = playerData.playersName[index];

            playerNamesEditButton[index].onClick.RemoveAllListeners();
            playerNamesEditButton[index].onClick.AddListener(() =>
            {
                editNameLabel.text = $"Enter {labels[index]} Name";
                editNameInputField.text = playerData.playersName[index];

                buttonClickSfx.Play();
                editNamePanel.gameObject.SetActive(true);
                editNamePanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);

                editNameOkButton.onClick.RemoveAllListeners();
                editNameOkButton.onClick.AddListener(() =>
                {
                    if (ValidateNickname(editNameInputField.text.Trim()))
                    {
                        playerNamesTxt[index].text = editNameInputField.text.Trim();

                        playerData.playersName[index] = editNameInputField.text.Trim(); // saved when settings panel closed

                        toastManager.ShowToast($"{labels[index]} Name Changed");

                        CloseEditNamePanel();
                    }
                });
            });
        }

        settingsPanel.gameObject.SetActive(true);
        buttonClickSfx.Play();
        settingsPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
    }

    public void CloseSettingsPanel()
    {
        playerData.SaveData();

        settingsPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(settingsPanel));
    }

    public void CloseEditNamePanel()
    {
        editNamePanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(editNamePanel));
    }

    private void UpdateMusicVolume()
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(playerData.musicVolume) * 20);
    }

    private void UpdateSfxVolume()
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(playerData.sfxVolume) * 20);
    }

    public void OnMusicVolumeChange(float value)
    {
        playerData.musicVolume = value;
        UpdateMusicVolume();
    }

    public void OnSfxVolumeChange(float value)
    {
        playerData.sfxVolume = value;
        UpdateSfxVolume();
    }

    private bool ValidateNickname(string nickname)
    {
        if (nickname.Length > 20 || nickname.Length < 3)
        {
            toastManager.ShowToast("Nickname should be 3 to 20 characters");
            return false;
        }
        if (nickname.Any(u => !char.IsLetterOrDigit(u)))
        {
            toastManager.ShowToast("Nickname should only contain alphanumeric characters");
            return false;
        }

        return true;
    }

    private bool ValidateUsername(string username)
    {
        if (username.Length > 20 || username.Length < 8)
        {
            toastManager.ShowToast("Username should be 8 to 20 characters");
            return false;
        }
        if (username.Any(u => !char.IsLetterOrDigit(u)))
        {
            toastManager.ShowToast("Username should only contain alphanumeric characters");
            return false;
        }

        return true;
    }

    private bool ValidatePassword(string password)
    {
        if (password.Length > 20 || password.Length < 8)
        {
            toastManager.ShowToast("Password should be 8 to 20 characters");
            return false;
        }
        if (password.Any(u => !char.IsLetterOrDigit(u)))
        {
            toastManager.ShowToast("Password should only contain alphanumeric characters");
            return false;
        }

        return true;
    }

    private bool ValidateEmail(string email)
    {
        try
        {
            MailAddress address = new MailAddress(email);

            if (email.Length > 100 || email.Length < 15)
            {
                toastManager.ShowToast("Email should be 15 to 100 characters");
                return false;
            }
            if (address.Address != email)
            {
                toastManager.ShowToast("Invalid email");
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            toastManager.ShowToast("Invalid email");
            return false;
        }
    }

    private bool PasswordMatch(string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            toastManager.ShowToast("Passwords do not match");
            return false;
        }

        return true;
    }

    public void Login()
    {
        buttonClickSfx.Play();

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            if (ValidateUsername(loginUsernameTxt.text.Trim()) && ValidatePassword(loginPasswordTxt.text.Trim()))
            {
                LoginRequest request = new LoginRequest(loginUsernameTxt.text.Trim(), loginPasswordTxt.text.Trim());

                spinnerContainer.SetActive(true);

                client.GetAuthorizationRoutes().Login(request, response =>
                {
                    playerData.playerAccessToken = response.accessToken;
                    playerData.playerRefreshToken = response.refreshToken;
                    playerData.playerId = response.playerId;
                    playerData.playerName = loginUsernameTxt.text.Trim();

                    playerData.SaveData();

                    ClearLoginInputFields();

                    CheckLoginState();

                    toastManager.ShowToast("Successfully logged in");

                    spinnerContainer.SetActive(false);
                }, error =>
                {
                    toastManager.ShowToast(error.details.Truncate(60));

                    spinnerContainer.SetActive(false);
                });
            }
        }
        else
        {
            toastManager.ShowToast("No Internet Connection");
        }
    }

    public void Signup()
    {
        buttonClickSfx.Play();

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            bool nameValidate = ValidateUsername(signupUsernameTxt.text.Trim());
            bool emailValidate = ValidateEmail(signupEmailTxt.text.Trim());
            bool passwordValidate = ValidatePassword(signupPasswordTxt.text.Trim());
            bool passwordMatch = PasswordMatch(signupPasswordTxt.text.Trim(), signupConfirmPasswordTxt.text.Trim());

            if (nameValidate && emailValidate && passwordValidate && passwordMatch)
            {
                SignupRequest request = new SignupRequest(signupUsernameTxt.text.Trim(), signupEmailTxt.text.Trim(), signupPasswordTxt.text.Trim(), signupConfirmPasswordTxt.text.Trim());

                spinnerContainer.SetActive(true);

                client.GetAuthorizationRoutes().Signup(request, response =>
                {
                    playerData.playerAccessToken = response.accessToken;
                    playerData.playerRefreshToken = response.refreshToken;
                    playerData.playerId = response.playerId;
                    playerData.playerName = signupUsernameTxt.text.Trim();

                    playerData.SaveData();

                    ClearSignupInputFields();

                    CheckLoginState();

                    toastManager.ShowToast("Successfully signed up");

                    spinnerContainer.SetActive(false);
                }, error =>
                {
                    toastManager.ShowToast(error.details.Truncate(60));

                    spinnerContainer.SetActive(false);
                });
            }
        }
        else
        {
            toastManager.ShowToast("No Internet Connection");
        }
    }

    public void OpenAccountPanel()
    {
        CheckLoginState();
        accountPanel.gameObject.SetActive(true);
        accountPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();
    }

    public void CloseAccountPanel()
    {
        accountPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(accountPanel));
    }

    public void OpenSignupPanel()
    {
        signupPanel.gameObject.SetActive(true);
        signupPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        ClearSignupInputFields();
    }

    public void OpenLoginPanel()
    {
        loginPanel.gameObject.SetActive(true);
        loginPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        ClearLoginInputFields();
    }

    public void CloseSignupPanel()
    {
        signupPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(signupPanel));
    }

    public void CloseLoginPanel()
    {
        loginPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(loginPanel));
    }

    private void Logout()
    {
        CloseConfirmPanel();

        playerData.playerAccessToken = "";
        playerData.playerRefreshToken = "";
        playerData.playerId = 0;
        playerData.playerName = "";

        playerData.SaveData();

        CheckLoginState();
    }

    private void SaveDataToServer()
    {
        CloseConfirmPanel();

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            saveButton.interactable = false;
            loadButton.interactable = false;

            spinnerContainer.SetActive(true);

            if (JwtHelper.IsExpired(playerData.playerAccessToken))
            {
                RefreshToken refreshToken = new RefreshToken(playerData.playerRefreshToken);

                client.GetAuthorizationRoutes().Refresh(refreshToken, response =>
                {
                    playerData.playerAccessToken = response.accessToken;
                    playerData.playerRefreshToken = response.refreshToken;

                    playerData.SaveData();

                    Debug.Log("New access token has been issued");

                    SavePlayerData();
                }, error =>
                {
                    toastManager.ShowToast(error.details.Truncate(60));

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    spinnerContainer.SetActive(false);
                });
            }
            else
            {
                SavePlayerData();
            }

            void SavePlayerData()
            {
                client.GetPlayerRoutes().SavePlayerData(playerData.playerAccessToken, response =>
                {
                    toastManager.ShowToast(response.message.Truncate(60));

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    spinnerContainer.SetActive(false);
                }, error =>
                {
                    toastManager.ShowToast(error.details.Truncate(60));

                    Debug.Log(error.details);
                    Debug.Log(error.error);

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    spinnerContainer.SetActive(false);
                }, progress => { });
            }
        }
        else
        {
            toastManager.ShowToast("No Internet Connection");
        }
    }

    private void LoadDataFromServer()
    {
        CloseConfirmPanel();

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            saveButton.interactable = false;
            loadButton.interactable = false;

            spinnerContainer.SetActive(true);

            if (JwtHelper.IsExpired(playerData.playerAccessToken))
            {
                RefreshToken refreshToken = new RefreshToken(playerData.playerRefreshToken);

                client.GetAuthorizationRoutes().Refresh(refreshToken, response =>
                {
                    playerData.playerAccessToken = response.accessToken;
                    playerData.playerRefreshToken = response.refreshToken;

                    playerData.SaveData();

                    Debug.Log("New access token has been issued");

                    LoadPlayerData();
                }, error =>
                {
                    toastManager.ShowToast(error.details.Truncate(60));

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    spinnerContainer.SetActive(false);
                });
            }
            else
            {
                LoadPlayerData();
            }

            void LoadPlayerData()
            {
                client.GetPlayerRoutes().LoadPlayerData(playerData.playerAccessToken, response =>
                {
                    playerData.SetPlayerDataFromServer(PlayerData.LoadData());

                    playerData.SaveData();

                    toastManager.ShowToast(response.message.Truncate(60));

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    UpdateMusicVolume();
                    UpdateSfxVolume();

                    spinnerContainer.SetActive(false);
                }, error =>
                {
                    toastManager.ShowToast(error.details.Truncate(60));

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    spinnerContainer.SetActive(false);
                }, progress => { });
            }
        }
        else
        {
            toastManager.ShowToast("No Internet Connection");
        }
    }

    public void ConfirmLogout()
    {
        confirmPanel.gameObject.SetActive(true);
        confirmPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        confirmPanelText.text = "Are you sure, you want to logout? Your data will not be lost.";

        confirmPanelOkButton.onClick.RemoveAllListeners();

        confirmPanelOkButton.onClick.AddListener(Logout);
    }

    public void ConfirmSaveData()
    {
        confirmPanel.gameObject.SetActive(true);
        confirmPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        confirmPanelText.text = "This will save your data to the server and will overwrite the previous data.";

        confirmPanelOkButton.onClick.RemoveAllListeners();

        confirmPanelOkButton.onClick.AddListener(SaveDataToServer);
    }

    public void ConfirmLoadData()
    {
        confirmPanel.gameObject.SetActive(true);
        confirmPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        confirmPanelText.text = "This will load your data from the server and overwrite your current progress.";

        confirmPanelOkButton.onClick.RemoveAllListeners();

        confirmPanelOkButton.onClick.AddListener(LoadDataFromServer);
    }

    public void CloseConfirmPanel()
    {
        confirmPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(confirmPanel));
    }

    private void CheckLoginState()
    {
        if (playerData.playerName.Length > 0 && playerData.playerId > 0)
        {
            accountLoginText.text = $"LOGGED IN AS {playerData.playerName}";

            signupSubmitButton.interactable = false;
            loginSubmitButton.interactable = false;
            logoutButton.interactable = true;
            signupButton.interactable = false;
            loginButton.interactable = false;
            saveButton.interactable = true;
            loadButton.interactable = true;
        }
        else
        {
            accountLoginText.text = $"NOT LOGGED IN";

            signupSubmitButton.interactable = true;
            loginSubmitButton.interactable = true;
            logoutButton.interactable = false;
            signupButton.interactable = true;
            loginButton.interactable = true;
            saveButton.interactable = false;
            loadButton.interactable = false;
        }
    }

    private void ClearSignupInputFields()
    {
        signupUsernameTxt.text = "";
        signupEmailTxt.text = "";
        signupPasswordTxt.text = "";
        signupConfirmPasswordTxt.text = "";
    }

    private void ClearLoginInputFields()
    {
        loginUsernameTxt.text = "";
        loginPasswordTxt.text = "";
    }

    private IEnumerator DelayedPanelClose(Transform panel)
    {
        yield return new WaitForSecondsRealtime(0.2f);
        panel.gameObject.SetActive(false);
    }

    private IEnumerator SwitchScene(string name)
    {
        crossfade.GetComponent<CanvasGroup>().blocksRaycasts = true;
        crossfade.SetBool("isOpen", true);
        yield return new WaitForSecondsRealtime(0.3f);
        SceneManager.LoadScene(name);
    }
}
