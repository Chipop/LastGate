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
    private const string ChoiceKey = Analytics.ConsentKey;
    [SerializeField] private GameObject overlay;
    [SerializeField] private TMP_Text headingText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button allowButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Button[] settingsButtons;
    private bool servicesReady;
    private bool servicesStarting;
    private bool showingPolicy;
    private string consentHeading;
    private string consentBody;
    private string declineCaption;
    private TMP_Text declineLabel;
    private int lastInteractionFrame = -1;

    public bool IsOpen => overlay.activeSelf;

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
        // An open Unity scene may still have the older serialization until it is reloaded.
        if (headingText == null || bodyText == null)
        {
            foreach (TMP_Text label in overlay.GetComponentsInChildren<TMP_Text>(true))
            {
                if (headingText == null && label.name == "HeadingLabel") headingText = label;
                if (bodyText == null && label.name == "DescriptionLabel") bodyText = label;
            }
        }
        consentHeading = headingText.text;
        consentBody = bodyText.text;
        declineLabel = declineButton.GetComponentInChildren<TMP_Text>(true);
        declineCaption = declineLabel.text;
        allowButton.onClick.AddListener(Allow);
        declineButton.onClick.AddListener(Decline);
        foreach (Button button in settingsButtons)
            button.onClick.AddListener(Open);
        overlay.SetActive(false);
        ApplyConsent(false);
        if (Analytics.HasConsent) StartOptionalServices();
    }

    private void Allow() => Choose(true);
    private void Decline()
    {
        if (showingPolicy) Close();
        else Choose(false);
    }

    public void OpenPrivacyPolicy()
    {
        showingPolicy = true;
        headingText.text = "Privacy Policy";
        bodyText.text =
            "Last Gate stores your best score and settings locally. It contacts our server for balance settings and public scores.\n\n" +
            "Analytics are on by default. You can turn them off in Settings. " +
            "Gameplay events go to Last Gate, Unity Analytics, and Microsoft PlayFab. " +
            "Last Gate may record device, browser, source, country and IP. Eligible scores can appear publicly under guest names.";
        statusText.text = "Last Gate playtest events: 90 days.\nQuestions: contact@yanvision.ma";
        allowButton.gameObject.SetActive(false);
        declineLabel.text = "Close";
        overlay.SetActive(true);
    }

    private void OnDestroy()
    {
        if (allowButton != null) allowButton.onClick.RemoveListener(Allow);
        if (declineButton != null) declineButton.onClick.RemoveListener(Decline);
        if (settingsButtons == null) return;
        foreach (Button button in settingsButtons)
            if (button != null) button.onClick.RemoveListener(Open);
    }

    private void StartOptionalServices()
    {
        PlayFabBackend.Initialize();
        if (!servicesStarting) InitializeServices();
    }

    private async void InitializeServices()
    {
        servicesStarting = true;
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
            ApplyConsent(Analytics.HasConsent);

            Debug.Log("[UGS] Services initialized.");

            if (!Analytics.HasConsent) return;
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (!Analytics.HasConsent && AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[UGS] Optional services failed. You can still play. {exception.Message}"
            );
        }
        finally
        {
            servicesStarting = false;
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
        if (allowed) StartOptionalServices();
        else
        {
            DjangoPlaytest.ClearPending();
            PlayFabBackend.EndSession();
            if (servicesReady && AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();
        }
        // Denial is immediate; granting waits until initialization has completed.
        ApplyConsent(allowed && servicesReady);
        Close();
    }

    private void Close()
    {
        lastInteractionFrame = Time.frameCount;
        overlay.SetActive(false);
    }

    public void Open()
    {
        showingPolicy = false;
        headingText.text = consentHeading;
        bodyText.text = consentBody;
        allowButton.gameObject.SetActive(true);
        declineLabel.text = declineCaption;
        lastInteractionFrame = Time.frameCount;
        statusText.text = Analytics.HasConsent
            ? "Analytics are on. You can turn them off below."
            : "Analytics are off. You can turn them on below.";
        overlay.SetActive(true);
    }

}
