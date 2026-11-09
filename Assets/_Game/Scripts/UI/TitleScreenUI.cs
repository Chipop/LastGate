using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class TitleScreenUI : MonoBehaviour
{
    [SerializeField] private TMP_Text fontSource;
    [SerializeField] private TMP_FontAsset smallTextFont;
    [SerializeField] private Image resultsFrame;
    [SerializeField] private Button privacyButton;
    [SerializeField] private AudioClip titleSlam;
    [SerializeField] private AudioClip buttonTap;
    [SerializeField] private AudioClip startTransition;

    private const string MotionKey = "last_gate_reduced_motion";
    private const float EntryDuration = 0.52f;
    private const float StartDuration = 0.24f;
    private const float MenuLabelSize = 54f;
    private const float LogoSize = 134f;
    private readonly TitleDungeonArt art = new();
    private readonly RectTransform[] embers = new RectTransform[10];
    private RectTransform stage, brand, actions, grille, fog;
    private RawImage wall, leftGlow, rightGlow;
    private Image flash;
    private CanvasGroup ctaGroup;
    private TMP_Text best, sound, motion, scores, ranks, scoreValues;
    private GameObject modal, settings, rankings;
    private AudioService audioService;
    private AnalyticsConsentUI consent;
    private Action start;
    private Coroutine scoresRequest;
    private bool reducedMotion, slamPlayed;
    private float enteredAt, startingAt = -1f;
    private Vector2 previousSize;
    private Vector2 brandHome;
    private bool wide;

    public bool TransitionPlaying => startingAt >= 0f && !reducedMotion
        && Time.unscaledTime - startingAt < StartDuration;

    public void Initialize(AudioService audio, AnalyticsConsentUI analyticsConsent, Action startRun)
    {
        audioService = audio;
        consent = analyticsConsent;
        start = startRun;
        reducedMotion = PlayerPrefs.GetInt(MotionKey, 0) == 1;

        // Replace the old labels, keeping the existing privacy button and its consent wiring.
        foreach (Transform child in transform)
            if (child != privacyButton.transform) child.gameObject.SetActive(false);

        wall = TextureLayer("Dungeon wall", transform, art.Wall(), Vector2.zero, Vector2.zero);
        Stretch(wall.rectTransform);
        wall.raycastTarget = true;
        Wire(wall.gameObject, wall, TryStart, false);

        stage = Rect("Title composition", transform, Vector2.zero, Vector2.zero);
        brand = Rect("Gate and logo", stage, Vector2.zero, Vector2.zero);
        BuildBrand();
        actions = Rect("Title actions", stage, Vector2.zero, Vector2.zero);
        BuildActions();
        BuildModal();

        flash = Solid("Transition overlay", transform, Color.clear, Vector2.zero, Vector2.zero);
        Stretch(flash.rectTransform);
        ResetPresentation();
    }

    private void BuildBrand()
    {
        fog = TextureLayer("Ground haze", brand, art.Glow(), new(0, -206), new(850, 130)).rectTransform;
        fog.GetComponent<RawImage>().color = new Color(0.5f, 0.65f, 1f, 0.26f);
        TextureLayer("Stone arch", brand, art.Arch(), new(0, 99), new(460, 495));
        grille = TextureLayer("Portcullis", brand, art.Grille(), new(0, 47), new(330, 385)).rectTransform;

        Texture2D glow = art.Glow();
        Texture2D torch = art.Icon("..#..", ".##..", ".###.", "#####", ".###.", "..#..", "..#..", "..#..");
        leftGlow = Torch(-290, glow, torch);
        rightGlow = Torch(290, glow, torch);
        // SFX hook: attach a quiet torch loop here, then start it after a user gesture on WebGL.
        for (int i = 0; i < embers.Length; i++)
            embers[i] = Solid("Ember", brand, new Color(1f, 0.58f, 0.24f, 0.55f),
                Vector2.zero, Vector2.one * (i % 3 == 0 ? 8 : 5)).rectTransform;

        Image plaque = Frame("Logo frame", brand, new(0, -178), new(940, 232));
        TMP_Text shadow = Label("Torchlit edge", plaque.transform, "LAST GATE", LogoSize,
            new Color32(171, 111, 55, 255), new(0, 33), new(910, 146));
        shadow.characterSpacing = 3;
        TMP_Text logo = Label("Logo", plaque.transform, "LAST GATE", LogoSize,
            Color.white, new(0, 40), new(910, 146));
        logo.characterSpacing = 3;
        logo.enableVertexGradient = true;
        logo.colorGradient = new VertexGradient(MenuTheme.Cream, MenuTheme.Cream,
            new Color32(155, 163, 178, 255), new Color32(155, 163, 178, 255));
        // The shared bitmap font uses hard edges. Offset copies outline it without a new material.
        foreach (Vector2 offset in new Vector2[] { new(-5, 0), new(5, 0), new(0, -5), new(0, 5) })
        {
            TMP_Text edge = Label("Iron outline", plaque.transform, "LAST GATE", LogoSize,
                new Color32(12, 15, 24, 255), new Vector2(0, 40) + offset, new(910, 146));
            edge.characterSpacing = logo.characterSpacing;
            edge.transform.SetSiblingIndex(logo.transform.GetSiblingIndex());
        }
        Label("Tagline", plaque.transform, "HOLD THE GATE. SURVIVE THE WAVES.",
            MenuLabelSize, MenuTheme.Cream, new(0, -65), new(890, 64));
    }

    private RawImage Torch(float x, Texture2D glow, Texture2D torch)
    {
        RawImage light = TextureLayer("Torchlight", brand, glow, new(x, 100), new(360, 360));
        TextureLayer("Wall torch", brand, torch, new(x, 83), new(45, 72)).color = MenuTheme.Gold;
        return light;
    }

    private void BuildActions()
    {
        Image primary = Frame("Start", actions, new(0, 220), new(680, 160));
        Image primaryFace = Solid("Amber inset", primary.transform, MenuTheme.Gold, Vector2.zero, new(644, 124));
        Label("Start label", primary.transform, "TAP TO START", 66, MenuTheme.Navy, Vector2.zero, new(640, 120));
        ctaGroup = primary.gameObject.AddComponent<CanvasGroup>();
        Wire(primary.gameObject, primaryFace, TryStart);

        TMP_Text hint = Label("Keyboard hint", actions, "ENTER OR SPACE TO START",
            MenuLabelSize, MenuTheme.Cream, new(0, 108), new(700, 56));
        hint.gameObject.SetActive(!Application.isMobilePlatform && Keyboard.current != null);

        Image chip = Frame("Best score chip", actions, new(0, 8), new(510, 105));
        TextureLayer("Trophy", chip.transform, art.Icon("#######", "#.###.#", ".#####.", "..###..", "...#...", "..###..", ".#####."),
            new(-172, 0), new(48, 48)).color = MenuTheme.Gold;
        best = Label("Local best", chip.transform, "", MenuLabelSize, MenuTheme.Gold,
            new(27, 0), new(365, 90));

        sound = ActionButton("Sound", -252, ToggleSound,
            art.Icon("...#...", "..##.#.", "###...#", "###...#", "###...#", "..##.#.", "...#..."));
        ActionButton("Settings", 0, () => OpenModal(false),
            art.Icon("..###..", ".#####.", "###.###", "##...##", "###.###", ".#####.", "..###.."));
        ActionButton("Scores", 252, () => OpenModal(true),
            art.Icon("...##...", "...##...", "...##...", "##.##...", "##.##.##", "##.##.##", "########"));

        Image policy = Frame("Privacy notice", actions, new(0, -344), new(680, MenuTheme.TouchTarget));
        Label("Privacy policy", policy.transform, "PRIVACY POLICY", MenuTheme.Action, MenuTheme.Gold,
            new(0, 27), new(650, 62));
        Label("Analytics notice", policy.transform, "MANAGE ANALYTICS IN SETTINGS", MenuTheme.Caption,
            MenuTheme.Cream, new(0, -27), new(650, 54));
        Wire(policy.gameObject, policy, consent.OpenPrivacyPolicy);

        TMP_Text version = Label("Build version", stage, "v" + Application.version, MenuTheme.Caption,
            MenuTheme.Steel, new(-143, 45), new(230, 42));
        version.rectTransform.anchorMin = version.rectTransform.anchorMax = new Vector2(1, 0);
        version.alignment = TextAlignmentOptions.MidlineRight;
    }

    private TMP_Text ActionButton(string name, float x, Action pressed, Texture2D icon)
    {
        Image button = Frame(name, actions, new(x, -154),
            new(228, MenuTheme.TouchTarget + MenuTheme.Spacing));
        TextureLayer(name + " icon", button.transform, icon, new(0, 23), new(48, 48)).color = MenuTheme.Steel;
        TMP_Text label = Label(name + " label", button.transform, name.ToUpperInvariant(), MenuLabelSize,
            MenuTheme.Cream, new(0, -36), new(216, 62));
        Wire(button.gameObject, button, () =>
        {
            audioService.PlaySfx(buttonTap);
            pressed();
        });
        return label;
    }

    private void BuildModal()
    {
        Image shield = Solid("Title dialog backdrop", stage, new Color(0.025f, 0.035f, 0.065f, 0.91f),
            Vector2.zero, Vector2.zero);
        shield.raycastTarget = true;
        modal = shield.gameObject;
        Image card = Frame("Title dialog", shield.transform, Vector2.zero, new(980, 812));
        SmallButton("BACK", card.transform, new(0, -302), CloseModal);

        settings = Rect("Settings contents", card.transform, Vector2.zero, Vector2.zero).gameObject;
        Label("Settings heading", settings.transform, "SETTINGS", 68, MenuTheme.Gold, new(0, 302), new(900, 100));
        motion = SmallButton("REDUCED MOTION", settings.transform, new(0, 110), ToggleMotion);
        RectTransform privacy = (RectTransform)privacyButton.transform;
        privacy.SetParent(settings.transform, false);
        Place(privacy, new(0, -74), new(680, MenuTheme.TouchTarget));
        TMP_Text privacyLabel = privacyButton.GetComponentInChildren<TMP_Text>();
        privacyLabel.font = smallTextFont;
        Stretch(privacyLabel.rectTransform);
        privacyLabel.text = "PRIVACY / ANALYTICS";
        privacyLabel.fontSize = MenuTheme.Action;
        privacyLabel.enableAutoSizing = false;
        privacyButton.navigation = new Navigation { mode = Navigation.Mode.None };

        rankings = Rect("Rankings contents", card.transform, Vector2.zero, Vector2.zero).gameObject;
        Label("Scores heading", rankings.transform, "ONLINE TOP 10", 64, MenuTheme.Gold,
            new(0, 321), new(900, 92));
        scores = Label("Player names", rankings.transform, "", MenuTheme.Caption, MenuTheme.Cream,
            new(-72, 48), new(477, 450));
        scores.alignment = TextAlignmentOptions.TopLeft;
        scores.richText = false;
        ranks = Label("Ranks", rankings.transform, "", MenuTheme.Caption, MenuTheme.Gold,
            new(-365, 48), new(90, 450));
        ranks.alignment = TextAlignmentOptions.TopLeft;
        scoreValues = Label("Scores", rankings.transform, "", MenuTheme.Caption, MenuTheme.Cream,
            new(308, 48), new(267, 450));
        scoreValues.alignment = TextAlignmentOptions.TopRight;
        modal.SetActive(false);
    }

    private TMP_Text SmallButton(string text, Transform parent, Vector2 center, Action action)
    {
        Image image = Frame(text, parent, center, new(680, MenuTheme.TouchTarget));
        Wire(image.gameObject, image, action);
        return Label(text + " label", image.transform, text, MenuTheme.Action,
            MenuTheme.Cream, Vector2.zero, new(660, 130));
    }

    private void OpenModal(bool leaderboard)
    {
        modal.SetActive(true);
        settings.SetActive(!leaderboard);
        rankings.SetActive(leaderboard);
        if (leaderboard) scoresRequest = StartCoroutine(LoadRankings());
    }

    private void CloseModal()
    {
        if (scoresRequest != null) StopCoroutine(scoresRequest);
        scoresRequest = null;
        modal.SetActive(false);
    }

    private IEnumerator LoadRankings()
    {
        scores.text = "LOADING SCORES...";
        ranks.text = scoreValues.text = "";
        DjangoPlaytest.Balance balance = null;
        yield return DjangoPlaytest.FetchBalance(value => balance = value);
        DjangoPlaytest.LeaderboardResult result = null;
        yield return DjangoPlaytest.FetchLeaderboard(value => result = value, balance?.version ?? 0);
        if (result == null) scores.text = "SCORES UNAVAILABLE";
        else if (result.entries.Length == 0) scores.text = "NO SCORES YET. BE THE FIRST!";
        else
        {
            var names = new System.Text.StringBuilder();
            var positions = new System.Text.StringBuilder();
            var values = new System.Text.StringBuilder();
            for (int i = 0; i < Mathf.Min(10, result.entries.Length); i++)
            {
                var entry = result.entries[i];
                string name = entry.isYou ? "YOU" : entry.name ?? "GUEST";
                if (name.Length > 18) name = name.Substring(0, 18);
                names.AppendLine(name);
                positions.AppendLine((i + 1).ToString("00"));
                values.AppendLine(entry.score.ToString("N0"));
            }
            scores.text = names.ToString().TrimEnd();
            ranks.text = positions.ToString().TrimEnd();
            scoreValues.text = values.ToString().TrimEnd();
        }
        scoresRequest = null;
    }

    private void ToggleSound()
    {
        audioService.ToggleMute();
        PlayerPrefs.Save();
        RefreshLabels();
    }

    private void ToggleMotion()
    {
        reducedMotion = !reducedMotion;
        PlayerPrefs.SetInt(MotionKey, reducedMotion ? 1 : 0);
        PlayerPrefs.Save();
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        best.text = $"BEST {PlayerPrefs.GetInt("best_score", 0):N0}";
        sound.text = audioService.IsMuted ? "SOUND OFF" : "SOUND ON";
        motion.text = reducedMotion ? "REDUCED MOTION: ON" : "REDUCED MOTION: OFF";
    }

    private void TryStart()
    {
        if (startingAt >= 0f || modal.activeSelf || consent.BlocksStart) return;
        // SFX hook: optional tap and transition clips use the existing sound service.
        audioService.PlaySfx(buttonTap);
        audioService.PlaySfx(startTransition);
        startingAt = Time.unscaledTime;
        start();
    }

    public void CancelStart() => startingAt = -1f;

    private void OnEnable()
    {
        if (stage != null) ResetPresentation();
    }

    private void OnDisable()
    {
        if (modal != null) CloseModal();
    }

    private void ResetPresentation()
    {
        enteredAt = Time.unscaledTime;
        startingAt = -1f;
        slamPlayed = false;
        RefreshLabels();
        Layout();
        Animate(0f);
    }

    private void Update()
    {
        if (stage == null) return;
        if (((RectTransform)transform).rect.size != previousSize) Layout();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && !consent.IsOpen)
        {
            if (modal.activeSelf && keyboard.escapeKey.wasPressedThisFrame) CloseModal();
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                TryStart();
        }
        Animate(Time.unscaledTime - enteredAt);
    }

    private void Layout()
    {
        previousSize = ((RectTransform)transform).rect.size;
        wide = previousSize.x >= previousSize.y * 1.25f;
        Vector2 design = wide ? new(1720, 880) : new(1080, 1630);
        float scale = Mathf.Max(0.001f, Mathf.Min(previousSize.x / design.x, previousSize.y / design.y));
        stage.localScale = Vector3.one * scale;
        stage.sizeDelta = previousSize / scale;
        ((RectTransform)modal.transform).sizeDelta = stage.sizeDelta;
        brandHome = wide ? new(-380, 20) : new(0, 308);
        actions.anchoredPosition = wide ? new(445, 0) : new(0, -362);

        // Keep the art full bleed, while the stage and all controls stay inside the safe area.
        Rect safe = Screen.safeArea;
        Vector2 fullSize = previousSize;
        if (safe.width > 0 && safe.height > 0)
        {
            Vector2 unitsPerPixel = new(previousSize.x / safe.width, previousSize.y / safe.height);
            wall.rectTransform.offsetMin = -Vector2.Scale(safe.min, unitsPerPixel);
            wall.rectTransform.offsetMax = Vector2.Scale(new Vector2(Screen.width, Screen.height) - safe.max, unitsPerPixel);
            fullSize = wall.rectTransform.rect.size;
        }
        float aspect = fullSize.x / Mathf.Max(1, fullSize.y);
        float width = Mathf.Min(1, aspect / (256f / 192f));
        float height = Mathf.Min(1, (256f / 192f) / Mathf.Max(0.001f, aspect));
        wall.uvRect = new Rect((1 - width) / 2, (1 - height) / 2, width, height);
    }

    private void Animate(float time)
    {
        float entry = reducedMotion ? 1f : Mathf.Clamp01(time / EntryDuration);
        float impact = reducedMotion ? 0f : Mathf.Sin(entry * 22f) * Mathf.Pow(1f - entry, 2) * 20f;
        brand.anchoredPosition = brandHome + new Vector2(impact, (1f - entry) * 80f);
        if (!slamPlayed && entry >= 0.55f)
        {
            slamPlayed = true;
            // SFX hook: title impact. Skip pre-gesture audio while the consent panel is open.
            if (!reducedMotion && !consent.IsOpen) audioService.PlaySfx(titleSlam);
        }

        float launch = startingAt < 0f || reducedMotion ? 0f
            : Mathf.Clamp01((Time.unscaledTime - startingAt) / StartDuration);
        brand.localScale = Vector3.one * ((wide ? 0.86f : 1f) * (1f + launch * 0.1f));
        grille.anchoredPosition = new Vector2(0, 47 + Mathf.Round((1f - entry) * 26f + launch * 22f));
        fog.anchoredPosition = new Vector2(reducedMotion ? 0 : Mathf.Round(Mathf.Sin(time * 0.5f) * 36), -206);
        leftGlow.color = new Color(1, 1, 1, reducedMotion ? 0.8f : 0.76f + Mathf.Sin(time * 13f) * 0.18f);
        rightGlow.color = new Color(1, 1, 1, reducedMotion ? 0.8f : 0.76f + Mathf.Sin(time * 11f + 2f) * 0.18f);
        for (int i = 0; i < embers.Length; i++)
        {
            float phase = Mathf.Repeat((reducedMotion ? 0f : time * 0.26f) + i * 0.17f, 1f);
            float x = (i % 2 == 0 ? -1f : 1f) * (244f + i * 10f);
            embers[i].anchoredPosition = new Vector2(x, Mathf.Floor(25f + phase * 260f));
        }

        float cta = reducedMotion ? 1f : Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.28f, 1f, entry));
        ctaGroup.alpha = cta;
        ctaGroup.interactable = cta > 0.8f;
        // Pulse the frame separately from the Button's selectable color transitions.
        ctaGroup.GetComponent<Image>().color = Color.Lerp(MenuTheme.Gold, Color.white,
            reducedMotion ? 0.5f : 0.5f + 0.5f * Mathf.Sin(time * 2f * Mathf.PI / 1.2f));
        flash.color = launch > 0f ? new Color(1, 0.67f, 0.26f, Mathf.Sin(launch * Mathf.PI) * 0.28f)
            : new Color(0, 0, 0, 1f - entry);
    }

    private Image Frame(string name, Transform parent, Vector2 center, Vector2 size)
    {
        Image image = Solid(name, parent, resultsFrame.color, center, size);
        image.sprite = resultsFrame.sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = resultsFrame.pixelsPerUnitMultiplier;
        return image;
    }

    private TMP_Text Label(string name, Transform parent, string value, float size, Color color, Vector2 center, Vector2 bounds)
    {
        var label = Rect(name, parent, center, bounds).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = size == LogoSize ? fontSource.font : smallTextFont;
        label.text = value;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Truncate;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static Image Solid(string name, Transform parent, Color color, Vector2 center, Vector2 size)
    {
        Image image = Rect(name, parent, center, size).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RawImage TextureLayer(string name, Transform parent, Texture texture, Vector2 center, Vector2 size)
    {
        RawImage image = Rect(name, parent, center, size).gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 center, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        Place(rect, center, size);
        return rect;
    }

    private static void Place(RectTransform rect, Vector2 center, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = center;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Wire(GameObject target, Graphic graphic, Action action, bool showFeedback = true)
    {
        var button = target.AddComponent<Button>();
        button.targetGraphic = graphic;
        // A child inset can provide press feedback, but the entire 48 px+ button is the hit area.
        target.GetComponent<Graphic>().raycastTarget = true;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.transition = showFeedback ? Selectable.Transition.ColorTint : Selectable.Transition.None;
        button.onClick.AddListener(() => action());
    }

    private void OnDestroy() => art.Dispose();
}
