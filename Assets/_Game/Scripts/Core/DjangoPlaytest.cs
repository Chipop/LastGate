using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PlayFab;
using UnityEngine;
using UnityEngine.Networking;

public sealed class DjangoPlaytest : MonoBehaviour
{
    public enum ReleaseSource { Auto, CrazyGames, ItchIo, Direct, YouTube, Facebook, TikTok }

    private const string PlayerKey = "last_gate_playtest_player";
    private static DjangoPlaytest instance;
    private readonly List<Dictionary<string, object>> pending = new();
    private string endpoint;
    private string balanceEndpoint;
    private string leaderboardEndpoint;
    private Dictionary<string, object> finishedRunEvent;
    private string runId;
    private int configVersion;
    private double startedAt;
    private int level;
    private int wave;
    private UnityWebRequest activeRequest;
    private int queueVersion;
    private ReleaseSource releaseSource;

    public static void Initialize(string baseUrl, ReleaseSource source)
    {
#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = "http://127.0.0.1:8011";
#endif
        if (instance != null || string.IsNullOrWhiteSpace(baseUrl)) return;
        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out Uri url) ||
            (url.Scheme != Uri.UriSchemeHttps &&
             !(Application.isEditor && url.IsLoopback && url.Scheme == Uri.UriSchemeHttp)))
        {
            Debug.LogWarning("[Django playtest] Use an HTTPS backend URL.");
            return;
        }

        GameObject uploader = new("Django Playtest");
        DontDestroyOnLoad(uploader);
        instance = uploader.AddComponent<DjangoPlaytest>();
        instance.releaseSource = source;
        string apiBase = baseUrl.Trim().TrimEnd('/') + "/api/playtests/";
        instance.endpoint = apiBase + "events/";
        instance.balanceEndpoint = apiBase + "balance/";
        instance.leaderboardEndpoint = apiBase + "leaderboard/";
        instance.StartCoroutine(instance.SendBatches());
    }

    [Serializable]
    public sealed class Balance
    {
        public int version;
        public int gateMaxHp;
        public int rerollBaseCost;
        public int rerollStep;
    }

    public static IEnumerator FetchBalance(Action<Balance> received)
    {
        if (instance == null) yield break;
        using UnityWebRequest request = UnityWebRequest.Get(instance.balanceEndpoint);
        request.timeout = 2;
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success || request.responseCode != 200)
            yield break;

        Balance balance;
        try
        {
            balance = JsonUtility.FromJson<Balance>(request.downloadHandler.text);
        }
        catch (ArgumentException)
        {
            yield break;
        }

        if (balance != null && balance.version > 0 &&
            balance.gateMaxHp > 0 && balance.gateMaxHp <= 100000 &&
            balance.rerollBaseCost >= 0 && balance.rerollBaseCost <= 10000 &&
            balance.rerollStep >= 0 && balance.rerollStep <= 10000)
            received(balance);
    }

    public static void BeginRun(int version)
    {
        if (instance == null) return;
        instance.runId = Guid.NewGuid().ToString();
        instance.configVersion = version;
        instance.startedAt = Time.realtimeSinceStartupAsDouble;
        instance.level = 1;
        instance.wave = 0;
    }

    public static void WaveStarted(int level, int wave)
    {
        if (instance == null) return;
        instance.level = level;
        instance.wave = wave;
        Track("wave_start");
    }

    public static void EndRun(string outcome, int score, int wavesCleared)
    {
        if (instance == null) return;
        int queued = instance.pending.Count;
        Track("run_end", new()
        {
            ["outcome"] = outcome,
            ["score"] = score,
            ["leaderboard_opt_in"] = outcome == "death" && Analytics.HasConsent,
            ["wavesCleared"] = wavesCleared
        });
        instance.finishedRunEvent = instance.pending.Count > queued ? instance.pending[queued] : null;
        instance.runId = null;
    }

    [Serializable]
    public sealed class LeaderboardEntry
    {
        public string name;
        public long score;
        public bool isYou;
    }

    [Serializable]
    public sealed class LeaderboardResult
    {
        public LeaderboardEntry[] entries;
        public long personalBest;
    }

    public static IEnumerator FetchLeaderboard(Action<LeaderboardResult> received, int? version = null)
    {
        if (instance == null) yield break;
        var finished = instance.finishedRunEvent;
        double deadline = Time.realtimeSinceStartupAsDouble + 10;
        while (finished != null && instance.pending.Contains(finished) &&
               Analytics.HasConsent && Time.realtimeSinceStartupAsDouble < deadline)
            yield return null;

        string url = instance.leaderboardEndpoint +
            "?build=" + UnityWebRequest.EscapeURL(Application.version) +
            "&version=" + (version ?? instance.configVersion);
        if (Analytics.HasConsent)
        {
            string player = PlayerPrefs.GetString(PlayerKey, "");
            if (Guid.TryParse(player, out _)) url += "&player=" + player;
        }

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 4;
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success) yield break;

        LeaderboardResult result;
        try
        {
            result = JsonUtility.FromJson<LeaderboardResult>(request.downloadHandler.text);
        }
        catch (ArgumentException)
        {
            yield break;
        }
        if (result?.entries != null) received(result);
    }

    public static void Track(string name, Dictionary<string, object> properties = null)
    {
        if (instance == null || instance.runId == null || !Analytics.HasConsent ||
            instance.pending.Count >= 512) return;

        string playerId = PlayerPrefs.GetString(PlayerKey, "");
        if (!Guid.TryParse(playerId, out _))
        {
            playerId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(PlayerKey, playerId);
            PlayerPrefs.Save();
        }

        Dictionary<string, object> payload = properties == null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(properties);
        payload["config_version"] = instance.configVersion;
        if (name == "run_start")
        {
            payload["platform"] = Application.isEditor ? "Editor" : Application.platform.ToString();
            payload["os"] = SystemInfo.operatingSystem;
            payload["device"] = SystemInfo.deviceModel;
            payload["viewport"] = $"{Screen.width} × {Screen.height}";
            if (instance.releaseSource != ReleaseSource.Auto)
                payload["source"] = instance.releaseSource.ToString();
        }

        instance.pending.Add(new()
        {
            ["event_id"] = Guid.NewGuid().ToString(),
            ["player_id"] = playerId,
            ["run_id"] = instance.runId,
            ["name"] = name,
            ["occurred_at"] = DateTime.UtcNow.ToString("O"),
            ["build"] = Application.version,
            ["level"] = instance.level,
            ["wave"] = instance.wave,
            ["elapsed_seconds"] = (int)(Time.realtimeSinceStartupAsDouble - instance.startedAt),
            ["properties"] = payload
        });
    }

    public static void ClearPending()
    {
        if (instance == null) return;
        instance.queueVersion++;
        instance.pending.Clear();
        instance.activeRequest?.Abort();
    }

    private IEnumerator SendBatches()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1);
            if (!Analytics.HasConsent || pending.Count == 0) continue;

            int count = Math.Min(pending.Count, 64);
            int version = queueVersion;
            var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
            string body = serializer.SerializeObject(new Dictionary<string, object>
            {
                ["events"] = pending.GetRange(0, count)
            });

            using UnityWebRequest request = new(endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;
            activeRequest = request;
            yield return request.SendWebRequest();
            activeRequest = null;
            if (version != queueVersion) continue;

            if (request.result == UnityWebRequest.Result.Success)
            {
                pending.RemoveRange(0, count);
            }
            else if (request.responseCode >= 400 && request.responseCode < 500 &&
                     request.responseCode != 429 && request.responseCode != 408)
            {
                pending.RemoveRange(0, count);
                Debug.LogWarning($"[Django playtest] Events rejected ({request.responseCode}): {request.downloadHandler.text}");
            }
            else
            {
                yield return new WaitForSecondsRealtime(5);
            }
        }
    }
}
