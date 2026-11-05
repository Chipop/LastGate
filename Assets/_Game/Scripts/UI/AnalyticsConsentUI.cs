using System;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UnityConsent;

// Visuals and references are authored in Game.unity; this handles behavior only.
public class AnalyticsConsentUI : MonoBehaviour
{
    private const string ChoiceKey = "analytics_consent_v2";
    [SerializeField] private GameObject overlay;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button allowButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Button[] settingsButtons;
    private bool servicesReady;
    private int lastInteractionFrame = -1;

    public bool BlocksStart
    {
        get
        {
            if (overlay.activeSelf || lastInteractionFrame == Time.frameCount)
                return true;
            if (Pointer.current != null)
                foreach (Button button in settingsButtons)
                    if (button.gameObject.activeInHierarchy &&
                        RectTransformUtility.RectangleContainsScreenPoint(
                            (RectTransform)button.transform,
                            Pointer.current.position.ReadValue(),
                            button.GetComponentInParent<Canvas>().worldCamera))
                        return true;
            return false;
        }
    }

    public void Initialize()
    {
        allowButton.onClick.AddListener(Allow);
        declineButton.onClick.AddListener(Decline);
        foreach (Button button in settingsButtons)
            button.onClick.AddListener(Open);
        overlay.SetActive(!PlayerPrefs.HasKey(ChoiceKey));
        InitializeServices();
    }

    private void Allow() => Choose(true);
    private void Decline() => Choose(false);

    private void OnDestroy()
    {
        if (allowButton != null) allowButton.onClick.RemoveListener(Allow);
        if (declineButton != null) declineButton.onClick.RemoveListener(Decline);
        if (settingsButtons == null) return;
        foreach (Button button in settingsButtons)
            if (button != null) button.onClick.RemoveListener(Open);
    }

    private async void InitializeServices()
    {
        // No analytics collection until the saved/player choice is applied.
        ApplyConsent(false);

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                InitializationOptions options = new InitializationOptions();
                options.SetEnvironmentName("dev");

                await UnityServices.InitializeAsync(options);
            }

            if (this == null) return;

            servicesReady = true;
            ApplyConsent(PlayerPrefs.GetInt(ChoiceKey, 0) == 1);

            Debug.Log("[UGS] Services initialized.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[UGS] Initialization failed. You can still play. {exception.Message}"
            );

            return;
        }

        // Authentication is separate from analytics consent.
        // A guest account will also be useful for the leaderboard.
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (this == null) return;

            Debug.Log(
                $"[UGS] Signed in. Player ID: " +
                AuthenticationService.Instance.PlayerId
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[UGS] Guest login failed. You can still play. {exception.Message}"
            );
        }
    }

    private static void ApplyConsent(bool allowed)
    {
        EndUserConsent.SetConsentState(new ConsentState
        {
            AnalyticsIntent = allowed ? ConsentStatus.Granted : ConsentStatus.Denied,
            AdsIntent = ConsentStatus.Denied
        });
    }

    private void Choose(bool allowed)
    {
        PlayerPrefs.SetInt(ChoiceKey, allowed ? 1 : 0);
        PlayerPrefs.Save();
        // Denial is immediate; granting waits until initialization has completed.
        ApplyConsent(allowed && servicesReady);
        lastInteractionFrame = Time.frameCount;
        overlay.SetActive(false);
    }

    private void Open()
    {
        lastInteractionFrame = Time.frameCount;
        statusText.text = !PlayerPrefs.HasKey(ChoiceKey)
            ? "Your choice is optional. You can change it later."
            : "Current choice: " + (PlayerPrefs.GetInt(ChoiceKey) == 1 ? "allowed" : "declined") +
              ". Choose again below.";
        overlay.SetActive(true);
    }

}
