using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public static class PlayFabBackend
{
    // Replace this with the Title ID of Last Gate Dev.
    private const string TitleId = "10BD0E";

    private const string GuestIdKey = "playfab_guest_id";

    private static bool loginInProgress;

    public static void Initialize()
    {
        if (!Analytics.HasConsent || loginInProgress || PlayFabClientAPI.IsClientLoggedIn())
        {
            return;
        }

        PlayFabSettings.staticSettings.TitleId = TitleId;
        PlayFabSettings.DisableDeviceInfo = true;
        PlayFabSettings.DisableFocusTimeCollection = true;

        // Generate once and retain across scene reloads and launches.
        string guestId = PlayerPrefs.GetString(GuestIdKey, "");

        if (string.IsNullOrEmpty(guestId))
        {
            guestId = Guid.NewGuid().ToString("N");

            PlayerPrefs.SetString(GuestIdKey, guestId);
            PlayerPrefs.Save();
        }

        loginInProgress = true;

        LoginWithCustomIDRequest request = new LoginWithCustomIDRequest
        {
            CustomId = guestId,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(
            request,
            result =>
            {
                loginInProgress = false;
                if (!Analytics.HasConsent)
                {
                    EndSession();
                    return;
                }

                Debug.Log(
                    $"[PlayFab] Signed in. Player ID: {result.PlayFabId}"
                );
            },
            error =>
            {
                loginInProgress = false;

                Debug.LogWarning(
                    $"[PlayFab] Login failed: {error.GenerateErrorReport()}"
                );
            }
        );
    }

    public static void EndSession() => PlayFabClientAPI.ForgetAllCredentials();

    public static void Track(
        string eventName,
        Dictionary<string, object> properties = null)
    {
        // Enforce the same saved analytics choice for PlayFab.
        if (!Analytics.HasConsent)
        {
            return;
        }

        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            if (eventName == "run_start")
                Debug.LogWarning("[PlayFab] Skipped run_start: guest login has not completed. Wait for the Signed in message before starting a test run.");
            return;
        }

        try
        {
            WriteClientPlayerEventRequest request =
                new WriteClientPlayerEventRequest
                {
                    EventName = eventName,
                    Body = properties == null
                        ? new Dictionary<string, object>()
                        : new Dictionary<string, object>(properties)
                };

            PlayFabClientAPI.WritePlayerEvent(
                request,
                result =>
                {
                    if (eventName == "run_start" || eventName == "run_end")
                        Debug.Log($"[PlayFab] Accepted '{eventName}'. Event ID: {result.EventId}; Title: {TitleId}");
                },
                error =>
                {
                    Debug.LogWarning(
                        $"[PlayFab] Event '{eventName}' failed: " +
                        error.GenerateErrorReport()
                    );
                }
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[PlayFab] Could not record '{eventName}': " +
                exception.Message
            );
        }
    }
}
