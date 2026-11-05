using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

public static class Analytics
{
    private const string ConsentKey = "analytics_consent_v2";

    public static void Track(
        string name,
        Dictionary<string, object> props = null)
    {
        string details = props == null
            ? ""
            : string.Join(", ", props);

        Debug.Log($"[analytics] {name} {details}");

        if (PlayerPrefs.GetInt(ConsentKey, 0) != 1)
        {
            if (name == "run_start")
                Debug.Log("[analytics] Online gameplay events skipped: analytics consent is not granted. Open Analytics and choose Allow analytics to enable sending.");
            return;
        }

        // PlayFab runs independently of UGS availability.
        PlayFabBackend.Track(name, props);

        // UGS also requires successful initialization and SDK consent.
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            return;
        }

        if (EndUserConsent.GetConsentState().AnalyticsIntent
            != ConsentStatus.Granted)
        {
            return;
        }

        try
        {
            CustomEvent analyticsEvent = new CustomEvent(name);

            if (props != null)
            {
                foreach (KeyValuePair<string, object> property in props)
                {
                    if (property.Value != null)
                    {
                        analyticsEvent.Add(property.Key, property.Value);
                    }
                }
            }

            AnalyticsService.Instance.RecordEvent(analyticsEvent);

            if (name == "run_end")
            {
                AnalyticsService.Instance.Flush();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[UGS] Could not record '{name}': {exception.Message}"
            );
        }
    }
}
