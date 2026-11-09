using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

public static class Analytics
{
    public const string ConsentKey = "analytics_consent_v4";
    public static bool HasConsent => PlayerPrefs.GetInt(ConsentKey, 1) == 1;

    public static void Track(
        string name,
        Dictionary<string, object> props = null)
    {
        string details = props == null
            ? ""
            : string.Join(", ", props);

        Debug.Log($"[analytics] {name} {details}");

        if (!HasConsent)
        {
            if (name == "run_start")
                Debug.Log("[analytics] Online gameplay events skipped: analytics are off. Turn them on in Privacy / Analytics to enable sending.");
            return;
        }

        // PlayFab runs independently of UGS availability.
        PlayFabBackend.Track(name, props);
        // EndRun supplies the outcome and final score to the Django sender.
        if (name != "run_end") DjangoPlaytest.Track(name, props);

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
