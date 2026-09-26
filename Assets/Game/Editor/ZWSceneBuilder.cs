using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// ZW > Build Scenes regenerates ZWSplash.unity and ZWMain.unity from the art and audio folders.
public static class ZWSceneBuilder
{
    const string ArtRoot = "Assets/Game/Art";
    const string Art = ArtRoot + "/assets_de123/assets_de/";
    const string Sfx = "Assets/Game/Audio/SFX/";
    const string BgmFolder = "Assets/Game/Audio/BGM";
    const string SplashPath = "Assets/Game/Scenes/ZWSplash.unity";
    const string ScenePath = "Assets/Game/Scenes/ZWMain.unity";

    // Zodiac order. Note: ox_seal.png is the Rat carving, bull_seal.png is the Ox.
    static readonly string[] SealFiles =
        { "ox_seal", "bull_seal", "tiger_seal", "rabbit_seal", "dragon_seal", "snake_seal",
          "horse_seal", "goat_seal", "monkey_seal", "roaster_seal", "dog_seal", "pig_seal" };

    static readonly Color Ink = new Color(0.29f, 0.17f, 0.10f);
    static readonly Color Parchment = new Color(0.96f, 0.90f, 0.78f);
    static readonly Color StampInk = new Color(0.62f, 0.38f, 0.24f);
    static Font font;
    static ZWAudio sound;   // set while building ZWMain so buttons get the click sound

    [MenuItem("ZW/Build Scenes")]
    public static void Build()
    {
        ImportSprites();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        System.IO.Directory.CreateDirectory("Assets/Game/Scenes");
        BuildSplash();
        BuildMainScene();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SplashPath, true),
            new EditorBuildSettingsScene(ScenePath, true),
        };
        Debug.Log("ZW: built " + SplashPath + " and " + ScenePath);
    }

    static Transform NewCanvasScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.13f, 0.08f);
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var root = canvasGo.transform;

        var bg = Img("Background", root, S("bg"), Vector2.zero, Vector2.zero);
        bg.preserveAspect = false;
        var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 940f / 1672f;
        return root;
    }

    static void BuildSplash()
    {
        sound = null;
        var root = NewCanvasScene();
        var splash = new GameObject("ZW").AddComponent<ZWSplash>();

        Img("TitleScroll", root, S("ceremonical_scroll"), new Vector2(0, 620), new Vector2(1000, 350));
        Txt("Title", root, "Zodiac Workshop", 96, new Vector2(0, 650), new Vector2(900, 120), Ink).fontStyle = FontStyle.Bold;
        Txt("Subtitle", root, "A Sequence-Recall Game", 40, new Vector2(0, 560), new Vector2(900, 60), Ink);

        splash.ring = Img("SealRing", root, S("zodiac_seal_ring"), new Vector2(0, 60), new Vector2(700, 720)).rectTransform;
        Img("OxSeal", root, S("bull_seal"), new Vector2(0, 60), new Vector2(180, 240));

        // Loading bar on a parchment strip
        Img("BarScroll", root, S("ceremonical_scroll"), new Vector2(0, -520), new Vector2(900, 315));
        splash.stepText = Txt("Step", root, "", 36, new Vector2(0, -455), new Vector2(700, 60), Ink);
        var ui = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var track = Img("BarTrack", root, ui, new Vector2(0, -530), new Vector2(640, 44));
        track.type = Image.Type.Sliced;
        track.color = new Color(0.45f, 0.30f, 0.20f, 0.5f);
        var fill = Img("BarFill", track.transform, ui, Vector2.zero, Vector2.zero);
        fill.type = Image.Type.Sliced;
        fill.color = new Color(0.66f, 0.38f, 0.24f);   // terracotta
        splash.barFill = fill.rectTransform;
        splash.barFill.anchorMin = Vector2.zero;
        splash.barFill.anchorMax = new Vector2(0, 1);
        splash.barFill.pivot = new Vector2(0, 0.5f);
        splash.percentText = Txt("Percent", root, "0%", 34, new Vector2(0, -590), new Vector2(300, 50), Ink);

        splash.tipText = Txt("Tip", root, "", 36, new Vector2(0, -800), new Vector2(940, 140), Parchment);
        Outlined(splash.tipText);

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SplashPath);
    }

    static void BuildMainScene()
    {
        var root = NewCanvasScene();

        var zw = new GameObject("ZW");
        var game = zw.AddComponent<ZWGame>();
        var menu = zw.AddComponent<ZWMenu>();
        game.menu = menu;
        menu.game = game;
        game.sound = sound = BuildAudio(zw);
        game.sealSprites = SealFiles.Select(S).ToArray();
        game.starFill = S("star_fill");
        game.starEmpty = S("star_empty");

        BuildGame(root, game);
        BuildMain(root, menu);
        BuildLevelSelect(root, menu);
        BuildPause(root, game);
        BuildWin(root, game);
        BuildLose(root, game);

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
    }

    static ZWAudio BuildAudio(GameObject zw)
    {
        var a = zw.AddComponent<ZWAudio>();
        a.sfx = zw.AddComponent<AudioSource>();
        a.sfx.playOnAwake = false;
        a.bgm = zw.AddComponent<AudioSource>();
        a.bgm.loop = true;
        a.bgm.playOnAwake = true;
        a.bgm.volume = 0.5f;
        // First clip dropped into Audio/BGM becomes the music track.
        a.bgm.clip = AssetDatabase.FindAssets("t:AudioClip", new[] { BgmFolder })
            .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();

        a.click = Clip("Click");
        a.stamp = Clip("Stamp");
        a.wrongStamp = Clip("WrongStamp");
        a.bannerHide = Clip("BannerHide");
        a.win = Clip("Win");
        a.lose = Clip("Lose");
        return a;
    }

    static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + name + ".wav");

    static void BuildGame(Transform root, ZWGame game)
    {
        var p = Panel("GamePanel", root, false);
        game.gamePanel = p.gameObject;

        // Target banner
        Img("Banner", p, S("ceremonical_scroll"), new Vector2(0, 700), new Vector2(1000, 350));
        game.bannerTitle = Txt("BannerTitle", p, "Stamp this sequence", 40, new Vector2(0, 800), new Vector2(800, 60), Ink);
        game.bannerSlots = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            var slot = Img("Slot" + i, p, null, new Vector2(0, 680), new Vector2(105, 140));
            Txt("?", slot.transform, "?", 64, Vector2.zero, new Vector2(105, 140), Ink);
            game.bannerSlots[i] = slot;
        }
        game.hintText = Txt("Hint", p, "", 34, new Vector2(0, 470), new Vector2(960, 130), Parchment);
        Outlined(game.hintText);

        // Stamp scroll inside the ring
        var ringCenter = new Vector2(0, -60);
        var scroll = Img("StampScroll", p, S("ceremonical_scroll"), ringCenter, new Vector2(640, 225));
        game.scrollStamps = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            var stamp = Img("Stamp" + i, scroll.transform, null, Vector2.zero, new Vector2(72, 96));
            stamp.color = StampInk;
            stamp.raycastTarget = false;
            stamp.gameObject.SetActive(false);
            game.scrollStamps[i] = stamp;
        }

        // Seal ring: Rat at the top, clockwise in zodiac order
        game.seals = new Button[12];
        for (int i = 0; i < 12; i++)
        {
            float a = (90f - i * 30f) * Mathf.Deg2Rad;
            var pos = ringCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 400f;
            var seal = Btn("Seal" + i, p, game.sealSprites[i], pos, new Vector2(135, 180), null, clickSound: false);
            UnityEventTools.AddIntPersistentListener(seal.onClick, game.OnSeal, i);
            game.seals[i] = seal;
        }

        // Retry tokens resting on the bench
        Img("Bench", p, S("wooden_bench_stand"), new Vector2(0, -640), new Vector2(620, 238));
        game.retryTokens = new Image[5];
        for (int i = 0; i < 5; i++)
            game.retryTokens[i] = Img("Token" + i, p, S("retry_budget_token"), new Vector2((i - 2) * 100f, -560), new Vector2(90, 88));
        Img("InkPot", p, S("ink_pot"), new Vector2(-445, -660), new Vector2(150, 133));
        Img("Brush", p, S("brush"), new Vector2(445, -660), new Vector2(146, 134));

        var tag = Img("LevelTag", p, S("wooden_tag"), new Vector2(-400, -840), new Vector2(130, 170));
        game.levelText = Txt("LevelText", tag.transform, "Level\n01", 28, new Vector2(0, -18), new Vector2(130, 90), Parchment);
        Outlined(game.levelText);
        Btn("PauseButton", p, S("bt2"), new Vector2(400, -840), new Vector2(130, 128), game.Pause);

        p.gameObject.SetActive(false);
    }

    static void BuildMain(Transform root, ZWMenu menu)
    {
        var p = Panel("MainPanel", root, false);
        menu.mainPanel = p.gameObject;

        Img("TitleScroll", p, S("ceremonical_scroll"), new Vector2(0, 620), new Vector2(1000, 350));
        Txt("Title", p, "Zodiac Workshop", 96, new Vector2(0, 650), new Vector2(900, 120), Ink).fontStyle = FontStyle.Bold;
        Txt("Subtitle", p, "A Sequence-Recall Game", 40, new Vector2(0, 560), new Vector2(900, 60), Ink);

        Img("SealRing", p, S("zodiac_seal_ring"), new Vector2(0, 80), new Vector2(700, 720));
        Img("OxSeal", p, S("bull_seal"), new Vector2(0, 80), new Vector2(180, 240));

        Btn("PlayButton", p, S("bt1"), new Vector2(-180, -470), new Vector2(200, 207), menu.Play, "Play", true);
        Btn("LevelsButton", p, S("workshop_book"), new Vector2(180, -470), new Vector2(200, 217), menu.ShowLevels, "Levels", true);

        Img("StatsScroll", p, S("ceremonical_scroll"), new Vector2(0, -790), new Vector2(900, 315));
        menu.statsText = Txt("Stats", p, "", 34, new Vector2(0, -790), new Vector2(700, 200), Ink);
    }

    static void BuildLevelSelect(Transform root, ZWMenu menu)
    {
        var p = Panel("LevelPanel", root, true);
        menu.levelPanel = p.gameObject;

        Outlined(Txt("Title", p, "Workshop Scrolls", 72, new Vector2(0, 840), new Vector2(800, 100), Parchment));
        menu.progressText = Txt("Progress", p, "", 38, new Vector2(0, 765), new Vector2(800, 60), Parchment);
        Outlined(menu.progressText);
        Btn("BackButton", p, S("bt3"), new Vector2(-450, 840), new Vector2(110, 111), menu.ShowMain);

        var viewport = Rect("Viewport", p, new Vector2(0, -80), new Vector2(1000, 1580));
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect("Content", viewport, Vector2.zero, new Vector2(1000, 0));
        content.anchorMin = new Vector2(0.5f, 1f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        layout.spacing = 6;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = viewport.gameObject.AddComponent<ScrollRect>();
        sr.viewport = viewport;
        sr.content = content;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;

        // Row template: one scroll strip per level
        var row = Btn("RowTemplate", content, S("ceremonical_scroll"), Vector2.zero, new Vector2(900, 316), null);
        Txt("Number", row.transform, "01", 80, new Vector2(-290, 0), new Vector2(140, 110), Ink).fontStyle = FontStyle.Bold;
        Img("Icon", row.transform, S("tiger_seal"), new Vector2(-130, 0), new Vector2(105, 140)).raycastTarget = false;
        for (int s = 0; s < 3; s++)
            Img("Star" + s, row.transform, S("star_empty"), new Vector2(90 + s * 85, 0), new Vector2(70, 67)).raycastTarget = false;
        Img("Lock", row.transform, S("level_lock"), new Vector2(175, 0), new Vector2(92, 110)).raycastTarget = false;
        row.gameObject.SetActive(false);

        menu.levelList = content;
        menu.rowTemplate = row.gameObject;
        p.gameObject.SetActive(false);
    }

    static void BuildPause(Transform root, ZWGame game)
    {
        var p = Panel("PausePanel", root, true);
        game.pausePanel = p.gameObject;
        var plaque = Img("Plaque", p, S("pause_win_lose_panel"), Vector2.zero, new Vector2(700, 865)).transform;
        Outlined(Txt("Title", plaque, "Paused", 64, new Vector2(0, 250), new Vector2(500, 90), Parchment));
        SlotButton("Resume", plaque, new Vector2(0, 51), game.Resume);
        SlotButton("Restart", plaque, new Vector2(0, -102), game.Restart);
        SlotButton("Main Menu", plaque, new Vector2(0, -255), game.MainMenu);
        p.gameObject.SetActive(false);
    }

    static void BuildWin(Transform root, ZWGame game)
    {
        var p = Panel("WinPanel", root, true);
        game.winPanel = p.gameObject;
        var frame = Img("Frame", p, S("panel_empty_win_lose_pause33"), new Vector2(0, -80), new Vector2(720, 1030)).transform;
        game.winScroll = Img("WinScroll", p, S("win_scroll"), new Vector2(0, 540), new Vector2(560, 370)).rectTransform;

        Txt("Title", frame, "Scroll Complete!", 60, new Vector2(0, 250), new Vector2(600, 90), Ink).fontStyle = FontStyle.Bold;
        game.winStars = new Image[3];
        for (int i = 0; i < 3; i++)
            game.winStars[i] = Img("Star" + i, frame, S("star_fill"), new Vector2((i - 1) * 120f, 130), new Vector2(100, 96));
        game.winStats = Txt("Stats", frame, "", 40, new Vector2(0, -30), new Vector2(560, 200), Ink);

        game.nextButton = Btn("NextButton", frame, S("bt1"), new Vector2(120, -200), new Vector2(130, 134), game.Next).gameObject;
        Btn("ReplayButton", frame, S("bt3"), new Vector2(-120, -200), new Vector2(130, 131), game.Restart);
        TextButton("Main Menu", frame, new Vector2(0, -310), game.MainMenu);
        p.gameObject.SetActive(false);
    }

    static void BuildLose(Transform root, ZWGame game)
    {
        var p = Panel("LosePanel", root, true);
        game.losePanel = p.gameObject;
        var frame = Img("Frame", p, S("panel_empty_win_lose_pause33"), Vector2.zero, new Vector2(720, 1030)).transform;
        Txt("Title", frame, "Out of Retries", 60, new Vector2(0, 300), new Vector2(600, 90), Ink).fontStyle = FontStyle.Bold;
        Txt("Body", frame, "The retry tokens are spent\nand the scroll is unfinished.", 38, new Vector2(0, 130), new Vector2(580, 140), Ink);
        Btn("RetryButton", frame, S("bt3"), new Vector2(0, -100), new Vector2(160, 161), game.Restart, "Retry", true, Ink);
        TextButton("Main Menu", frame, new Vector2(0, -300), game.MainMenu);
        p.gameObject.SetActive(false);
    }

    // ---- helpers ----

    static void ImportSprites()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (ti.textureType == TextureImporterType.Sprite) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
    }

    static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");

    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Image Img(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        var img = Rect(name, parent, pos, size).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        return img;
    }

    static Text Txt(string name, Transform parent, string text, int fontSize, Vector2 pos, Vector2 size, Color color)
    {
        var t = Rect(name, parent, pos, size).gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        return t;
    }

    static void Outlined(Text t)
    {
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.15f, 0.08f, 0.04f, 0.9f);
        o.effectDistance = new Vector2(2, -2);
    }

    static Button Btn(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, UnityAction onClick,
                      string label = null, bool labelBelow = false, Color? labelColor = null, bool clickSound = true)
    {
        var img = Img(name, parent, sprite, pos, size);
        var b = img.gameObject.AddComponent<Button>();
        if (clickSound && sound != null) UnityEventTools.AddPersistentListener(b.onClick, sound.Click);
        if (onClick != null) UnityEventTools.AddPersistentListener(b.onClick, onClick);
        if (label != null)
        {
            var t = Txt("Label", img.transform, label, 46, labelBelow ? new Vector2(0, -size.y / 2 - 36) : Vector2.zero,
                        new Vector2(360, 70), labelColor ?? Parchment);
            if (labelColor == null) Outlined(t);
        }
        return b;
    }

    // Invisible hit area over a carved slot on the pause plaque.
    static void SlotButton(string label, Transform parent, Vector2 pos, UnityAction onClick)
    {
        var b = Btn(label, parent, null, pos, new Vector2(470, 130), onClick, label);
        b.image.color = new Color(1, 1, 1, 0);
    }

    static void TextButton(string label, Transform parent, Vector2 pos, UnityAction onClick)
    {
        var b = Btn(label, parent, null, pos, new Vector2(360, 80), onClick, label, false, Ink);
        b.image.color = new Color(1, 1, 1, 0);
    }

    static RectTransform Panel(string name, Transform parent, bool dim)
    {
        var rt = Rect(name, parent, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        if (dim) rt.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.06f, 0.03f, 0.6f);
        return rt;
    }

    // Self-check for the level data array.
    [MenuItem("ZW/Validate Levels")]
    public static void Validate()
    {
        for (int level = 1; level <= ZWLevels.Count; level++)
        {
            var d = ZWLevels.All[level - 1];
            ZWLevels.Build(level, out int[] seq, out int[] ring);
            Check(d.level == level, level, "level number");
            Check(seq.Length == d.sequenceLength, level, "sequence length");
            Check(seq.Distinct().Count() == seq.Length, level, "duplicate animal in sequence");
            var decoys = Enumerable.Range(0, 12).Where(s => ring[s] != s).ToArray();
            Check(decoys.Length == d.decoyCount, level, "decoy count");
            foreach (int s in decoys)
                Check(!seq.Contains(s) && seq.Contains(ring[s]), level, "decoy must copy a target and not replace one");
            if (d.targetVisibility == ZWVisibility.Partial)
                for (int i = 1; i < seq.Length; i++)
                    Check(seq[i] == (seq[i - 1] + 1) % 12, level, "partial target must be a zodiac run");
        }
        Debug.Log("ZW: all " + ZWLevels.Count + " levels valid");
    }

    static void Check(bool ok, int level, string what)
    {
        if (!ok) throw new System.Exception($"ZW level {level}: {what}");
    }
}
