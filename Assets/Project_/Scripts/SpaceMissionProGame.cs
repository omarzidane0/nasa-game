using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using TMPro;

/// <summary>
/// Complete runtime UI and simplified mission game loop for Space Mission Designer.
/// All engineering values are illustrative gameplay values, not NASA specifications.
/// </summary>
public class SpaceMissionProGame : MonoBehaviour
{
    [Serializable]
    private class Equipment
    {
        public string title;
        public string category;
        public string description;
        public int price;
        public float massKg;
        public float powerDrawKw;
        public float powerOutputKw;
        public float payloadCapacityKg;
        public bool owned;
        public bool selected;

        public Equipment(string title, string category, string description, int price,
            float massKg, float draw, float output, float capacity = 0f)
        {
            this.title = title;
            this.category = category;
            this.description = description;
            this.price = price;
            this.massKg = massKg;
            powerDrawKw = draw;
            powerOutputKw = output;
            payloadCapacityKg = capacity;
        }
    }

    private enum GameScreen { Home, Missions, Store, Assembly, Launch, MissionControl, MoonLanding, MoonSurface, EarthObservation, Results }

    // One decision in the flight simulation. Deltas are gameplay units, not real telemetry.
    private class MissionOption
    {
        public string title;
        public string detail;
        public string outcome;
        public int powerDelta, linkDelta, integrityDelta, dataPoints, scoreDelta, dayDelta;

        public MissionOption(string title, string detail, string outcome,
            int power, int link, int integrity, int data, int score, int days)
        {
            this.title = title; this.detail = detail; this.outcome = outcome;
            powerDelta = power; linkDelta = link; integrityDelta = integrity;
            dataPoints = data; scoreDelta = score; dayDelta = days;
        }
    }

    private class MissionEvent
    {
        public string title;
        public string prompt;
        public MissionOption[] options;
        public MissionEvent(string title, string prompt, params MissionOption[] options)
        { this.title = title; this.prompt = prompt; this.options = options; }
    }

    private const int StartingBudget = 13000;
    private readonly Color backgroundColor = new Color32(5, 12, 27, 255);
    private readonly Color cardColor = new Color32(13, 29, 50, 248);
    private readonly Color cardAltColor = new Color32(18, 41, 66, 255);
    private readonly Color cyan = new Color32(52, 196, 245, 255);
    private readonly Color cyanDark = new Color32(17, 103, 145, 255);
    private readonly Color gold = new Color32(255, 190, 82, 255);
    private readonly Color green = new Color32(73, 215, 158, 255);
    private readonly Color red = new Color32(255, 103, 111, 255);
    private readonly Color muted = new Color32(158, 180, 203, 255);
    // omar zidane
    private static Sprite roundedButtonSprite;
    private static Sprite roundedCardSprite;
    private readonly List<Equipment> equipment = new List<Equipment>();
    private readonly Dictionary<string, Sprite> planetSprites = new Dictionary<string, Sprite>();
    private Sprite circleSprite;
   
    private Canvas canvas;
    private RectTransform root;
    private TMP_FontAsset gameFont;
    private TMP_Text budgetLabel;
    private GameObject currentPage;
    private GameObject currentModal;
    private GameScreen currentScreen;
    private string selectedMission = "MOON";
    private string missionDescription = "Explore the lunar surface and analyze its composition.";
    private int budget = StartingBudget;
    private bool missionReady;
    private bool launchRunning;
    private string lastValidation = "";
    private int missionScore;

    // Mission Control state. These are simplified gameplay meters rather than live spacecraft telemetry.
    private readonly List<MissionEvent> flightEvents = new List<MissionEvent>();
    private readonly List<string> flightLog = new List<string>();
    private int flightEventIndex;
    private int flightDay;
    private int flightPower;
    private int flightLink;
    private int flightIntegrity;
    private int flightDataPoints;
    private int flightScore;
    private int flightCommandPoints;
    private bool usedCameraCommand;
    private bool usedSpectrometerCommand;
    private bool usedTransmitCommand;
    private bool usedPowerSaveCommand;
    private int transmittedDataPoints;
    private int earthImagesCaptured;
    private int earthTargetsCompleted;
    private int earthDataTransmitted;
    private string earthObservationStatus = "Awaiting orbital imaging pass.";
    private bool missionSuccess;
    private string lastFlightOutcome = "Flight plan loaded. Awaiting first command.";

    // Runtime 3D previews and launch sequence.
    private GameObject launchStageRoot;
    private RenderTexture launchStageTexture;
    private Camera launchStageCamera;
    private Transform launchRocketRoot;
    private Transform launchBoosterLeft;
    private Transform launchBoosterRight;
    private Transform launchSurfaceEnvironmentRoot;
    private Transform launchSpaceEnvironmentRoot;
    private Transform launchStarDome;
    private Transform launchEarthTransform;
    private ParticleSystem launchExhaust;
    private ParticleSystem launchSmoke;
    private ParticleSystem separationBurst;
    private GameObject launchFlameOuter;
    private GameObject launchFlameInner;
    private GameObject launchBoosterFlameLeft;
    private GameObject launchBoosterFlameRight;
    private TMP_Text launchCountdownText;
    private TMP_Text launchPhaseText;
    private RawImage launchDisplay;

    private GameObject craftPreviewRoot;
    private RenderTexture craftPreviewTexture;
    private Transform craftPreviewSpin;

    // A destination-specific 3D environment used behind the Mission Control and Results UI.
    private GameObject missionSceneRoot;
    private RenderTexture missionSceneTexture;
    private Camera missionSceneCamera;

    // Dedicated orbital-imaging rig for the Earth mission.
    private GameObject earthObservationRoot;
    private RenderTexture earthObservationTexture;
    private Camera earthObservationCamera;
    private Transform earthObservationGlobe;
    private int earthCurrentTargetIndex = -1;
    private Transform missionSpacecraftPivot;
    private Transform missionPlanetTransform;
    private Vector3 missionSpacecraftBasePosition;
    private readonly List<Texture2D> transientTextures = new List<Texture2D>();
    private readonly Dictionary<string, Texture2D> missionPlanetTextureCache = new Dictionary<string, Texture2D>();

    // Playable lunar surface expedition. All engineering and survival values below are
    // simplified gameplay values, not real astronaut suit specifications.
    private GameObject lunarWorldRoot;
    private RenderTexture lunarWorldTexture;
    private Camera lunarWorldCamera;
    private Transform lunarLanderRoot;
    private GameObject lunarAstronautObject;
    private Transform lunarAstronautVisual;
    private CharacterController lunarCharacterController;
    private Transform lunarCameraTarget;
    private Transform lunarRover;
    private Transform[] lunarRoverWheels = new Transform[6];
    private Vector3 lunarAstronautSavedPosition;
    private bool lunarRoverMode;
    private float lunarRoverBattery = 100f;
    private Transform lunarLeftArm, lunarRightArm, lunarLeftLeg, lunarRightLeg;
    private Transform[] lunarSampleMarkers = new Transform[3];
    private Vector3 lunarScanStationPosition;
    private Vector3 lunarBeaconStationPosition;
    private Vector3 lunarLanderPosition;
    private Vector2[] lunarCraterCenters;
    private float[] lunarCraterRadii;
    private float[] lunarCraterDepths;
    private float lunarYaw = 0f;
    private float lunarPitch = 13f;
    private float lunarCameraDistance = 6.35f;
    private float lunarVerticalVelocity;
    private float lunarBoundaryWarningTimer;
    private const float SurfaceBoundaryMin = -36f;
    private const float SurfaceBoundaryMax = 36f;
    private float lunarFootstepTimer;
    private float lunarOxygen = 100f;
    private float lunarSuitBattery = 100f;
    private float lunarTemperature = -18f;
    private int lunarSamplesCollected;
    private bool lunarTerrainScanned;
    private bool lunarBeaconDeployed;
    private bool lunarReturnedToLander;
    private bool lunarLandingRunning;
    private bool lunarControlsUnlocked;
    private bool lunarDescentEffectsActive;
    private Transform lunarDescentFlameLeft, lunarDescentFlameRight;
    private ParticleSystem lunarDescentDust;
    private TMP_Text lunarStatusText, lunarPromptText, lunarObjectivesText;
    private TMP_Text lunarOxygenText, lunarBatteryText, lunarSamplesText, lunarNavigationText;
    private Image lunarOxygenBarFill, lunarBatteryBarFill;
    private GameObject lunarLandingOverlay;
    private AudioClip lunarStepClip, lunarScanClip, lunarBeaconClip;

    private AudioSource gameAudioSource;
    private AudioClip uiClickClip, countdownClip, ignitionClip, rocketLoopClip;
    private AudioClip liftoffClip, radioClip, spaceAmbienceClip;
    private readonly List<Material> transientMaterials = new List<Material>();

    // Automatically starts the game when the MainMenu scene is played or built.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (UnityEngine.Object.FindAnyObjectByType<SpaceMissionProGame>() == null)
        {
            GameObject game = new GameObject("SPACE MISSION DESIGNER - GAME MANAGER");
            game.AddComponent<SpaceMissionProGame>();
        }

        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void Awake()
    {
        BuildEquipmentCatalog();
        SetupAudioSystem();
        gameFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (gameFont == null && TMP_Settings.defaultFontAsset != null)
            gameFont = TMP_Settings.defaultFontAsset;
        BuildCanvas();
        ShowScreen(GameScreen.Home);
    }

    private void Update()
    {
        if (craftPreviewSpin != null)
            craftPreviewSpin.Rotate(Vector3.up, 13f * Time.deltaTime, Space.World);

        if (launchSpaceEnvironmentRoot != null && launchSpaceEnvironmentRoot.gameObject.activeSelf && launchRocketRoot != null)
            launchSpaceEnvironmentRoot.position = launchRocketRoot.position;

        if (missionSpacecraftPivot != null)
        {
            missionSpacecraftPivot.Rotate(Vector3.up, 8f * Time.deltaTime, Space.Self);
            float bob = Mathf.Sin(Time.time * 0.85f) * 0.12f;
            missionSpacecraftPivot.localPosition = missionSpacecraftBasePosition + new Vector3(0f, bob, 0f);
        }
        if (missionPlanetTransform != null)
            missionPlanetTransform.Rotate(Vector3.up, (selectedMission == "EARTH" ? 2.4f : 0.8f) * Time.deltaTime, Space.Self);
        if (earthObservationGlobe != null && currentScreen == GameScreen.EarthObservation && earthCurrentTargetIndex < 0)
            earthObservationGlobe.Rotate(Vector3.up, 5.5f * Time.deltaTime, Space.Self);

        if (currentScreen == GameScreen.MoonSurface && lunarControlsUnlocked)
            UpdateLunarExplorer();
    }

    private void SetupAudioSystem()
    {
        gameAudioSource = gameObject.AddComponent<AudioSource>();
        gameAudioSource.playOnAwake = false;
        gameAudioSource.spatialBlend = 0f;
        gameAudioSource.volume = 0.78f;

        uiClickClip = Resources.Load<AudioClip>("Audio/UI_Click");
        countdownClip = Resources.Load<AudioClip>("Audio/Countdown_Beep");
        ignitionClip = Resources.Load<AudioClip>("Audio/Ignition_Whoosh");
        rocketLoopClip = Resources.Load<AudioClip>("Audio/Rocket_Engine_Loop");
        liftoffClip = Resources.Load<AudioClip>("Audio/Liftoff_Stinger");
        radioClip = Resources.Load<AudioClip>("Audio/Radio_Chirp");
        spaceAmbienceClip = Resources.Load<AudioClip>("Audio/Deep_Space_Ambience");
        lunarStepClip = Resources.Load<AudioClip>("Audio/Lunar_Step");
        lunarScanClip = Resources.Load<AudioClip>("Audio/Lunar_Scan");
        lunarBeaconClip = Resources.Load<AudioClip>("Audio/Lunar_Beacon");
    }

    private void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (gameAudioSource != null && clip != null)
            gameAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void StartAudioLoop(AudioClip clip, float volume)
    {
        if (gameAudioSource == null || clip == null) return;
        gameAudioSource.Stop();
        gameAudioSource.clip = clip;
        gameAudioSource.loop = true;
        gameAudioSource.volume = volume;
        gameAudioSource.Play();
    }

    private void StopAudioLoop()
    {
        if (gameAudioSource == null) return;
        gameAudioSource.Stop();
        gameAudioSource.loop = false;
        gameAudioSource.clip = null;
        gameAudioSource.volume = 0.78f;
    }

    private void CleanupTransient3D()
    {
        if (launchStageRoot != null) Destroy(launchStageRoot);
        if (craftPreviewRoot != null) Destroy(craftPreviewRoot);
        if (missionSceneRoot != null) Destroy(missionSceneRoot);
        if (earthObservationRoot != null) Destroy(earthObservationRoot);
        if (lunarWorldRoot != null) Destroy(lunarWorldRoot);
        launchStageRoot = null;
        craftPreviewRoot = null;
        missionSceneRoot = null;
        earthObservationRoot = null;
        earthObservationCamera = null;
        earthObservationGlobe = null;
        lunarWorldRoot = null;
        lunarWorldCamera = null;
        lunarLanderRoot = null;
        lunarAstronautObject = null;
        lunarAstronautVisual = null;
        lunarCharacterController = null;
        lunarCameraTarget = null;
        lunarRover = null;
        lunarRoverWheels = new Transform[6];
        lunarRoverMode = false;
        lunarRoverBattery = 100f;
        lunarDescentFlameLeft = null;
        lunarDescentFlameRight = null;
        lunarDescentDust = null;
        lunarSampleMarkers = new Transform[3];
        lunarLandingOverlay = null;
        lunarStatusText = lunarPromptText = lunarObjectivesText = null;
        lunarOxygenText = lunarBatteryText = lunarSamplesText = lunarNavigationText = null;
        lunarOxygenBarFill = lunarBatteryBarFill = null;
        missionSceneCamera = null;
        missionSpacecraftPivot = null;
        missionPlanetTransform = null;
        launchStageCamera = null;
        launchRocketRoot = null;
        launchBoosterLeft = null;
        launchBoosterRight = null;
        launchSurfaceEnvironmentRoot = null;
        launchSpaceEnvironmentRoot = null;
        launchStarDome = null;
        launchEarthTransform = null;
        launchExhaust = null;
        launchSmoke = null;
        separationBurst = null;
        launchFlameOuter = null;
        launchFlameInner = null;
        launchBoosterFlameLeft = null;
        launchBoosterFlameRight = null;
        craftPreviewSpin = null;
        launchDisplay = null;
        launchCountdownText = null;
        launchPhaseText = null;

        if (launchStageTexture != null)
        {
            launchStageTexture.Release();
            Destroy(launchStageTexture);
            launchStageTexture = null;
        }
        if (craftPreviewTexture != null)
        {
            craftPreviewTexture.Release();
            Destroy(craftPreviewTexture);
            craftPreviewTexture = null;
        }
        if (missionSceneTexture != null)
        {
            missionSceneTexture.Release();
            Destroy(missionSceneTexture);
            missionSceneTexture = null;
        }
        if (earthObservationTexture != null)
        {
            earthObservationTexture.Release();
            Destroy(earthObservationTexture);
            earthObservationTexture = null;
        }
        if (lunarWorldTexture != null)
        {
            lunarWorldTexture.Release();
            Destroy(lunarWorldTexture);
            lunarWorldTexture = null;
        }
        for (int i = 0; i < transientMaterials.Count; i++)
            if (transientMaterials[i] != null) Destroy(transientMaterials[i]);
        transientMaterials.Clear();
        for (int i = 0; i < transientTextures.Count; i++)
            if (transientTextures[i] != null) Destroy(transientTextures[i]);
        transientTextures.Clear();
    }

    private void BuildEquipmentCatalog()
    {
        equipment.Clear();
        // CR values are balanced in-game mission credits, not real procurement quotes.
        equipment.Add(new Equipment("ATLAS A", "ROCKET", "Medium-lift launch vehicle • 1,000 kg game payload limit", 4800, 0, 0, 0, 1000));
        equipment.Add(new Equipment("ATLAS B", "ROCKET", "Heavy-lift launch vehicle • 1,800 kg game payload limit", 7600, 0, 0, 0, 1800));
        equipment.Add(new Equipment("ORBITER", "SPACECRAFT", "Earth-observation spacecraft bus • 450 kg dry mass", 1600, 450, 0.2f, 0));
        equipment.Add(new Equipment("LUNAR LANDER", "SPACECRAFT", "Propulsive descent stage with surface payload bay", 2100, 760, 0.3f, 0));
        equipment.Add(new Equipment("SOLAR ARRAY", "POWER", "Deployable photovoltaic array • 3.5 kW game output", 850, 110, 0, 3.5f));
        equipment.Add(new Equipment("RTG POWER UNIT", "POWER", "Compact long-life power module • 5.5 kW game output", 1450, 145, 0, 5.5f));
        equipment.Add(new Equipment("HIGH-GAIN ANTENNA", "COMMUNICATION", "Steerable X-band high-gain deep-space link", 700, 70, 0.3f, 0));
        equipment.Add(new Equipment("SCIENCE CAMERA", "INSTRUMENT", "Multispectral optical imager for Earth and surface targets", 500, 35, 0.7f, 0));
        equipment.Add(new Equipment("SPECTROMETER", "INSTRUMENT", "VNIR/SWIR spectrometer for mineral and regolith analysis", 850, 65, 1.2f, 0));
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("SpaceMissionDesignerCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject rootObject = new GameObject("Root", typeof(RectTransform));
        root = rootObject.GetComponent<RectTransform>();
        root.SetParent(canvasObject.transform, false);
        Stretch(root);

        Image bg = CreateImage("DeepSpaceBackground", root, backgroundColor);
        Stretch(bg.rectTransform);

        // Procedural stars keep the project self-contained; no external image downloads are needed.
        System.Random rng = new System.Random(2026);
        for (int i = 0; i < 150; i++)
        {
            Image star = CreateImage("Star", root, new Color(0.58f, 0.78f, 1f, 0.12f + (float)rng.NextDouble() * 0.62f));
            RectTransform starRect = star.rectTransform;
            float x = (float)rng.NextDouble();
            float y = (float)rng.NextDouble();
            starRect.anchorMin = starRect.anchorMax = new Vector2(x, y);
            starRect.sizeDelta = Vector2.one * (1.2f + (float)rng.NextDouble() * 2.5f);
            starRect.anchoredPosition = Vector2.zero;
        }

        CreatePlanetAssets();
        circleSprite = BuildCircleSprite();
        Image glow = CreateImage("PlanetGlow", root, new Color(0.10f, 0.42f, 0.78f, 0.12f));
        glow.sprite = GetPlanetSprite("EARTH");
        glow.preserveAspect = true;
        RectTransform glowRect = glow.rectTransform;
        glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.94f, 0.79f);
        glowRect.sizeDelta = new Vector2(620, 620);
        glowRect.anchoredPosition = Vector2.zero;

        Image planet = CreateImage("BackgroundPlanet", root, Color.white);
        planet.sprite = GetPlanetSprite("EARTH");
        planet.preserveAspect = true;
        RectTransform planetRect = planet.rectTransform;
        planetRect.anchorMin = planetRect.anchorMax = new Vector2(0.95f, 0.80f);
        planetRect.sizeDelta = new Vector2(465, 465);
        planetRect.anchoredPosition = Vector2.zero;

        Image header = CreateImage("Header", root, new Color32(7, 18, 35, 242));
        RectTransform headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 92);
        headerRect.anchoredPosition = Vector2.zero;

        Label("SPACE MISSION DESIGNER", header.transform, 26, Color.white,
            TextAlignmentOptions.Left, new Vector2(620, 60), new Vector2(0, 0.5f), new Vector2(42, 0), true);
        Label("NASA SPACE APPS • MISSION LAB", header.transform, 12, muted,
            TextAlignmentOptions.Left, new Vector2(450, 24), new Vector2(0, 0.5f), new Vector2(45, -27), false);
        budgetLabel = Label("BUDGET  13,000 CR", header.transform, 20, gold,
            TextAlignmentOptions.Right, new Vector2(390, 54), new Vector2(1, 0.5f), new Vector2(-42, 0), true);

        Label("PLAN  •  BUILD  •  LAUNCH  •  DISCOVER", root, 13, muted,
            TextAlignmentOptions.Center, new Vector2(560, 28), new Vector2(0.5f, 0), new Vector2(0, 17), false);
    }

    private void CreatePlanetAssets()
    {
        planetSprites["MOON"] = BuildPlanetSprite(new Color32(160, 174, 191, 255), 12, false);
        planetSprites["EARTH"] = BuildPlanetSprite(new Color32(39, 133, 194, 255), 37, true);
        planetSprites["MARS"] = BuildPlanetSprite(new Color32(190, 89, 63, 255), 76, false);
    }

    private Sprite BuildPlanetSprite(Color baseColor, int seed, bool earthLike)
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "ProceduralPlanetTexture";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        System.Random random = new System.Random(seed);
        float noiseOffset = (float)random.NextDouble() * 100f;
        Color[] pixels = new Color[size * size];
        Vector3 lightDir = new Vector3(-0.55f, 0.7f, 0.8f).normalized;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float rr = nx * nx + ny * ny;
                int index = y * size + x;
                if (rr >= 1f)
                {
                    pixels[index] = Color.clear;
                    continue;
                }

                float nz = Mathf.Sqrt(1f - rr);
                Vector3 normal = new Vector3(nx, ny, nz).normalized;
                float light = Mathf.Clamp01(Vector3.Dot(normal, lightDir) * 0.72f + 0.35f);
                float n1 = Mathf.PerlinNoise(nx * 4.2f + noiseOffset, ny * 4.2f + seed);
                float n2 = Mathf.PerlinNoise(nx * 11f + seed, ny * 9f + noiseOffset);
                float terrain = 0.78f + (n1 - 0.5f) * 0.36f + (n2 - 0.5f) * 0.10f;
                Color c = baseColor * terrain * light;
                if (earthLike)
                {
                    float land = Mathf.PerlinNoise(nx * 5.5f + seed, ny * 4.5f + noiseOffset);
                    if (land > 0.57f) c = Color.Lerp(c, new Color(0.27f, 0.55f, 0.34f), (land - 0.57f) * 2.2f);
                    float cloud = Mathf.PerlinNoise(nx * 15f + noiseOffset, ny * 12f + seed);
                    if (cloud > 0.72f) c = Color.Lerp(c, new Color(0.82f, 0.90f, 0.96f), (cloud - 0.72f) * 1.8f);
                }
                float edge = Mathf.SmoothStep(0f, 0.08f, 1f - rr);
                pixels[index] = new Color(c.r, c.g, c.b, edge);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite GetPlanetSprite(string key)
    {
        return planetSprites.TryGetValue(key, out Sprite sprite) ? sprite : null;
    }

    private void ShowScreen(GameScreen screen)
    {
        CleanupTransient3D();
        if (screen != GameScreen.MissionControl) StopAudioLoop();
        currentScreen = screen;
        if (currentModal != null) Destroy(currentModal);
        currentModal = null;
        if (currentPage != null) Destroy(currentPage);
        currentPage = new GameObject(screen + "Page", typeof(RectTransform));
        currentPage.transform.SetParent(root, false);
        Stretch(currentPage.GetComponent<RectTransform>());
        budgetLabel.text = "BUDGET  " + budget.ToString("N0") + " CR";

        switch (screen)
        {
            case GameScreen.Home: BuildHome(); break;
            case GameScreen.Missions: BuildMissions(); break;
            case GameScreen.Store: BuildStore(); break;
            case GameScreen.Assembly: BuildAssembly(); break;
            case GameScreen.Launch: BuildLaunch(); break;
            case GameScreen.MissionControl: BuildMissionControl(); break;
            case GameScreen.MoonLanding: BuildMoonLanding(); break;
            case GameScreen.MoonSurface: BuildMoonSurface(); break;
            case GameScreen.EarthObservation: BuildEarthObservation(); break;
            case GameScreen.Results: BuildResults(); break;
        }
    }

    private void BuildHome()
    {
        // The hero image is a cropped scene-only artwork (no embedded title/buttons),
        // so every visible control is rendered once as real Unity UI.
        Texture2D heroTexture = Resources.Load<Texture2D>("MainMenuHero");
        if (heroTexture != null)
        {
            Image hero = CreateImage("HomeHeroArtwork", currentPage.transform, Color.white);
            hero.sprite = Sprite.Create(heroTexture, new Rect(0, 0, heroTexture.width, heroTexture.height),
                new Vector2(0.5f, 0.5f), 100f);
            hero.preserveAspect = false;
            hero.raycastTarget = false;
            RectTransform heroRect = hero.rectTransform;
            heroRect.anchorMin = new Vector2(0.49f, 0.08f);
            heroRect.anchorMax = new Vector2(1f, 0.91f);
            heroRect.offsetMin = Vector2.zero;
            heroRect.offsetMax = Vector2.zero;
        }

        // Left glass panel gives the title a clean reading area without covering the hero scene.
        Image glass = CreateImage("HomeLeftGlass", currentPage.transform,
            new Color(3f / 255f, 12f / 255f, 27f / 255f, 0.88f));
        glass.raycastTarget = false;
        RectTransform glassRect = glass.rectTransform;
        glassRect.anchorMin = new Vector2(0.035f, 0.11f);
        glassRect.anchorMax = new Vector2(0.535f, 0.89f);
        glassRect.offsetMin = Vector2.zero;
        glassRect.offsetMax = Vector2.zero;

        Image leftRule = CreateImage("HomeAccentRule", currentPage.transform, cyan);
        RectTransform ruleRect = leftRule.rectTransform;
        ruleRect.anchorMin = new Vector2(0.075f, 0.755f);
        ruleRect.anchorMax = new Vector2(0.235f, 0.761f);
        ruleRect.offsetMin = ruleRect.offsetMax = Vector2.zero;
        leftRule.raycastTarget = false;

        Label("NASA SPACE APPS CHALLENGE  /  MISSION LAB", currentPage.transform, 16, cyan,
            TextAlignmentOptions.Left, new Vector2(780, 30), new Vector2(0.075f, 0.81f), Vector2.zero, true);
        Label("DESIGN YOUR\nNEXT SPACE MISSION", currentPage.transform, 58, Color.white,
            TextAlignmentOptions.Left, new Vector2(830, 174), new Vector2(0.075f, 0.70f), Vector2.zero, true);
        Label("DESIGN  •  BUILD  •  LAUNCH  •  DISCOVER", currentPage.transform, 20,
            new Color32(97, 218, 255, 255), TextAlignmentOptions.Left,
            new Vector2(790, 38), new Vector2(0.075f, 0.535f), Vector2.zero, true);
        Label("Plan a space mission from the first decision to launch.\nChoose a destination, build your spacecraft, manage\nyour resources, then test the mission and review results.",
            currentPage.transform, 19, new Color32(211, 228, 243, 255), TextAlignmentOptions.Left,
            new Vector2(830, 112), new Vector2(0.075f, 0.445f), Vector2.zero, false);

        Button("START MISSION    ›", currentPage.transform, new Vector2(0.19f, 0.285f),
            new Vector2(420, 76), cyan, () => ShowScreen(GameScreen.Missions));
        Button("HOW TO PLAY", currentPage.transform, new Vector2(0.155f, 0.185f),
            new Vector2(260, 54), cardAltColor,
            () => ShowInfo("HOW TO PLAY", "1. Choose Moon, Earth or Mars.\n2. Purchase a launch vehicle and mission systems.\n3. Review payload mass, power and communications.\n4. Pass the pre-launch checklist and run the simulation.\n\nAll prices and engineering values are illustrative gameplay values, not official NASA specifications."));

        // Three compact feature chips make the game loop clear at a glance.
        HomeFeature("01  BUILD", "Design your spacecraft", new Vector2(0.075f, 0.095f));
        HomeFeature("02  MANAGE", "Balance mission resources", new Vector2(0.245f, 0.095f));
        HomeFeature("03  EXPLORE", "Launch and analyze", new Vector2(0.415f, 0.095f));

        Label("ORBITAL SYSTEMS  /  ONLINE", currentPage.transform, 13, green,
            TextAlignmentOptions.Right, new Vector2(360, 25), new Vector2(0.965f, 0.12f), Vector2.zero, true);
    }

    private void HomeFeature(string heading, string detail, Vector2 anchor)
    {
        Label(heading, currentPage.transform, 12, cyan, TextAlignmentOptions.Left,
            new Vector2(280, 20), anchor, Vector2.zero, true);
        Label(detail, currentPage.transform, 11, muted, TextAlignmentOptions.Left,
            new Vector2(280, 20), new Vector2(anchor.x, anchor.y - 0.025f), Vector2.zero, false);
    }

    private void BuildMissions()
    {
        ScreenTitle("SELECT DESTINATION", "Choose the mission profile you want to attempt.");
        BuildMissionCard("MOON", "Surface exploration\nMineral analysis and landing", new Vector2(0.20f, 0.435f));
        BuildMissionCard("EARTH", "Earth observation\nClimate imaging and data relay", new Vector2(0.50f, 0.435f));
        BuildMissionCard("MARS", "Deep-space exploration\nTerrain and science operations", new Vector2(0.80f, 0.435f));
        Button("BACK TO MENU", currentPage.transform, new Vector2(0.14f, 0.075f), new Vector2(205, 50), cardAltColor,
            () => ShowScreen(GameScreen.Home));
    }

    private void BuildMissionCard(string mission, string description, Vector2 center)
    {
        GameObject card = Card(currentPage.transform,
            new Vector2(center.x - 0.135f, 0.205f), new Vector2(center.x + 0.135f, 0.675f), cardColor);
        // AddAccent(card.transform, cyan);
        Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = roundedCardSprite;
        cardImage.type = Image.Type.Sliced;

        GameObject innerCard = Card(card.transform,
            new Vector2(0.012f, 0.012f), new Vector2(0.988f, 0.988f), cardColor);
        Image planet = CreateImage("DestinationPlanet", card.transform, Color.white);
        planet.sprite = GetPlanetSprite(mission);
        planet.preserveAspect = true;
        RectTransform p = planet.rectTransform;
        p.anchorMin = p.anchorMax = new Vector2(0.5f, 0.70f);
        p.sizeDelta = new Vector2(150, 150);
        Label(mission, card.transform, 25, Color.white, TextAlignmentOptions.Center,
            new Vector2(350, 42), new Vector2(0.5f, 0.405f), Vector2.zero, true);
        Label(description, card.transform, 15, muted, TextAlignmentOptions.Center,
            new Vector2(350, 68), new Vector2(0.5f, 0.285f), Vector2.zero, false);
        Button("SELECT MISSION", card.transform, new Vector2(0.5f, 0.105f), new Vector2(220, 50), cyan,
            () => SelectMission(mission));
    }

    private void SelectMission(string mission)
    {
        selectedMission = mission;
        switch (mission)
        {
            case "EARTH": missionDescription = "Earth observation: climate imaging and data relay."; break;
            case "MARS": missionDescription = "Mars exploration: deep-space communication and science."; break;
            default: missionDescription = "Moon exploration: surface operations and mineral analysis."; break;
        }
        ShowScreen(GameScreen.Store);
    }

    private void BuildStore()
    {
        ScreenTitle("EQUIPMENT STORE", selectedMission + " MISSION  /  " + missionDescription + "  /  COSTS IN NON-MONETARY MISSION CREDITS");
        string[] categories = { "ROCKET", "SPACECRAFT", "POWER", "COMMUNICATION", "INSTRUMENT" };
        for (int i = 0; i < equipment.Count; i++)
        {
            int col = i % 3;
            int row = i / 3;
            float x = 0.17f + col * 0.33f;
            float y = 0.625f - row * 0.215f;
            BuildEquipmentCard(equipment[i], new Vector2(x, y));
        }

        Button("BACK", currentPage.transform, new Vector2(0.12f, 0.055f), new Vector2(170, 48), cardAltColor,
            () => ShowScreen(GameScreen.Missions));
        Button("REVIEW ASSEMBLY", currentPage.transform, new Vector2(0.84f, 0.055f), new Vector2(300, 54), cyan,
            () => ShowScreen(GameScreen.Assembly));
    }

    private string GetEquipmentDisplayName(Equipment item)
    {
        switch (item.title)
        {
            case "ATLAS A": return "MEDIUM-LIFT LAUNCH VEHICLE";
            case "ATLAS B": return "HEAVY-LIFT LAUNCH VEHICLE";
            case "ORBITER": return "EARTH OBSERVATION BUS";
            case "LUNAR LANDER": return selectedMission == "MARS" ? "MARS EDL LANDER" : "LUNAR DESCENT LANDER";
            case "SOLAR ARRAY": return "DEPLOYABLE SOLAR ARRAY";
            case "RTG POWER UNIT": return "LONG-LIFE POWER MODULE";
            case "HIGH-GAIN ANTENNA": return "X-BAND HIGH-GAIN ANTENNA";
            case "SCIENCE CAMERA": return "MULTISPECTRAL IMAGER";
            case "SPECTROMETER": return "VNIR / SWIR SPECTROMETER";
            default: return item.title;
        }
    }

    private void BuildEquipmentCard(Equipment item, Vector2 center)
    {
        GameObject card = Card(currentPage.transform,
            new Vector2(center.x - 0.145f, center.y - 0.082f),
            new Vector2(center.x + 0.145f, center.y + 0.082f), item.selected ? new Color32(14, 49, 63, 252) : cardAltColor);
        //AddAccent(card.transform, item.selected ? green : cyan);

        BuildEquipmentIcon(card.transform, item, new Vector2(0.16f, 0.50f));

        Label(item.category, card.transform, 11, cyan, TextAlignmentOptions.Left,
            new Vector2(300, 20), new Vector2(0.32f, 0.86f), Vector2.zero, true);
        string displayTitle = GetEquipmentDisplayName(item);
        string displayDescription = item.description;
        if (item.title == "LUNAR LANDER") displayDescription = selectedMission == "MARS" ?
            "Mars atmospheric-entry, descent and landing module" : "Vacuum-rated lunar powered-descent and landing module";
        Label(displayTitle, card.transform, 18, Color.white, TextAlignmentOptions.Left,
            new Vector2(315, 34), new Vector2(0.32f, 0.64f), Vector2.zero, true);
        Label(displayDescription, card.transform, 12, muted, TextAlignmentOptions.Left,
            new Vector2(310, 28), new Vector2(0.32f, 0.42f), Vector2.zero, false);
        Label(item.price.ToString("N0") + " CR", card.transform, 16, gold, TextAlignmentOptions.Left,
            new Vector2(180, 26), new Vector2(0.32f, 0.16f), Vector2.zero, true);

        string buttonText = item.selected ? "SELECTED" : (item.owned ? "SELECT" : "BUY");
        Color buttonColor = item.selected ? green : (item.owned ? cyanDark : cyan);
        Button(buttonText, card.transform, new Vector2(item.owned ? 0.69f : 0.83f, 0.20f),
            new Vector2(item.owned ? 108 : 130, 42), buttonColor,
            () => PurchaseOrSelect(item));
        if (item.owned)
            Button("SELL", card.transform, new Vector2(0.89f, 0.20f), new Vector2(78, 42), gold,
                () => SellEquipment(item));
    }

    private void BuildEquipmentIcon(Transform parent, Equipment item, Vector2 center)
    {
        // Keep every icon part inside its own fixed-size container. Previously the
        // parts were anchored directly to the whole card using offsets larger than
        // the card's normalized range, which made fragments float outside the card.
        GameObject iconRoot = new GameObject("EquipmentIconRoot", typeof(RectTransform));
        iconRoot.transform.SetParent(parent, false);
        RectTransform iconRect = iconRoot.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = center;
        iconRect.sizeDelta = new Vector2(112, 112);
        iconRect.anchoredPosition = Vector2.zero;

        Image plate = CreateImage("EquipmentIconPlate", iconRoot.transform, new Color32(5, 20, 38, 255));
        Stretch(plate.rectTransform);
        plate.raycastTarget = false;
        Outline plateOutline = plate.gameObject.AddComponent<Outline>();
        plateOutline.effectColor = new Color(cyan.r, cyan.g, cyan.b, 0.55f);
        plateOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Color metal = new Color32(187, 204, 220, 255);
        Color darkMetal = new Color32(73, 104, 130, 255);
        Color panelBlue = new Color32(30, 150, 205, 255);
        Color orange = new Color32(255, 151, 73, 255);

        if (item.category == "ROCKET")
        {
            // Nose, fuselage, fins and engine are deliberately built as one compact icon.
            IconPart(iconRoot.transform, new Vector2(0f, 0.61f), new Vector2(16, 17), Color.white, 0);
            IconPart(iconRoot.transform, new Vector2(0f, -0.02f), new Vector2(23, 49), metal, 0);
            IconPart(iconRoot.transform, new Vector2(-0.43f, -0.47f), new Vector2(17, 23), cyanDark, -22);
            IconPart(iconRoot.transform, new Vector2(0.43f, -0.47f), new Vector2(17, 23), cyanDark, 22);
            IconPart(iconRoot.transform, new Vector2(0f, -0.63f), new Vector2(12, 12), orange, 0);
            IconPart(iconRoot.transform, new Vector2(0f, -0.83f), new Vector2(7, 9), gold, 0);
        }
        else if (item.category == "SPACECRAFT")
        {
            IconPart(iconRoot.transform, new Vector2(0f, 0f), new Vector2(31, 27), metal, 0);
            IconPart(iconRoot.transform, new Vector2(-0.91f, 0f), new Vector2(29, 43), panelBlue, -4);
            IconPart(iconRoot.transform, new Vector2(0.91f, 0f), new Vector2(29, 43), panelBlue, 4);
            IconPart(iconRoot.transform, new Vector2(0f, 0.52f), new Vector2(13, 13), gold, 0);
            IconPart(iconRoot.transform, new Vector2(0f, -0.54f), new Vector2(13, 15), darkMetal, 0);
            IconCircle(iconRoot.transform, new Vector2(0f, 0.02f), new Vector2(12, 12), cyan);
        }
        else if (item.category == "POWER")
        {
            if (item.title == "SOLAR ARRAY")
            {
                IconPart(iconRoot.transform, Vector2.zero, new Vector2(76, 48), panelBlue, 0);
                for (int i = -2; i <= 2; i++)
                    IconPart(iconRoot.transform, new Vector2(i * 0.38f, 0f), new Vector2(2, 44), new Color32(150, 224, 255, 255), 0);
                IconPart(iconRoot.transform, Vector2.zero, new Vector2(72, 2), new Color32(150, 224, 255, 255), 0);
                IconPart(iconRoot.transform, new Vector2(0f, -0.92f), new Vector2(5, 17), gold, 0);
            }
            else
            {
                IconPart(iconRoot.transform, Vector2.zero, new Vector2(48, 34), darkMetal, 0);
                IconPart(iconRoot.transform, new Vector2(-0.72f, 0f), new Vector2(12, 42), gold, 0);
                IconPart(iconRoot.transform, new Vector2(0.72f, 0f), new Vector2(12, 42), gold, 0);
                IconPart(iconRoot.transform, Vector2.zero, new Vector2(30, 25), metal, 0);
                IconPart(iconRoot.transform, new Vector2(0f, 0f), new Vector2(9, 9), cyan, 0);
            }
        }
        else if (item.category == "COMMUNICATION")
        {
            Image dish = IconCircle(iconRoot.transform, new Vector2(-0.12f, 0.18f), new Vector2(57, 57), metal);
            Outline dishOutline = dish.gameObject.AddComponent<Outline>();
            dishOutline.effectColor = new Color(cyan.r, cyan.g, cyan.b, 0.8f);
            dishOutline.effectDistance = new Vector2(2, -2);
            IconCircle(iconRoot.transform, new Vector2(-0.12f, 0.18f), new Vector2(31, 31), darkMetal);
            IconPart(iconRoot.transform, new Vector2(0.27f, -0.42f), new Vector2(7, 36), gold, -24);
            IconPart(iconRoot.transform, new Vector2(0.45f, -0.79f), new Vector2(43, 5), cyan, 0);
        }
        else
        {
            IconPart(iconRoot.transform, new Vector2(0f, 0f), new Vector2(53, 42), darkMetal, 0);
            IconPart(iconRoot.transform, new Vector2(-0.1f, 0f), new Vector2(31, 29), metal, 0);
            IconCircle(iconRoot.transform, new Vector2(-0.1f, 0f), new Vector2(22, 22), panelBlue);
            IconPart(iconRoot.transform, new Vector2(0f, -0.74f), new Vector2(61, 5), gold, 0);
            IconPart(iconRoot.transform, new Vector2(0.65f, 0.38f), new Vector2(9, 9), cyan, 0);
        }
    }

    private void IconPart(Transform parent, Vector2 relative, Vector2 size, Color color, float rotation)
    {
        Image part = CreateImage("EquipmentIconPart", parent, color);
        part.raycastTarget = false;
        RectTransform rect = part.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = relative * 28f;
        rect.localEulerAngles = new Vector3(0, 0, rotation);
    }

    private Image IconCircle(Transform parent, Vector2 relative, Vector2 size, Color color)
    {
        Image image = CreateImage("EquipmentIconCircle", parent, color);
        image.sprite = circleSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = relative * 28f;
        return image;
    }

    private Sprite BuildCircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeCircleSprite";
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                float dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 1f - Mathf.SmoothStep(0.94f, 1f, distance);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void PurchaseOrSelect(Equipment item)
    {
        if (!item.owned)
        {
            if (budget < item.price)
            {
                ShowInfo("BUDGET LIMIT", "Not enough credits.\nRequired: " + item.price.ToString("N0") + " CR\nAvailable: " + budget.ToString("N0") + " CR");
                return;
            }
            budget -= item.price;
            item.owned = true;
        }

        if (item.category == "INSTRUMENT")
        {
            item.selected = !item.selected;
        }
        else if (item.selected)
        {
            item.selected = false;
        }
        else
        {
            foreach (Equipment other in equipment)
                if (other.category == item.category && other != item) other.selected = false;
            item.selected = true;
        }
        budgetLabel.text = "BUDGET  " + budget.ToString("N0") + " CR";
        ShowScreen(GameScreen.Store);
    }

    private void SellEquipment(Equipment item)
    {
        if (!item.owned) return;

        item.owned = false;
        item.selected = false;
        budget += item.price;
        budgetLabel.text = "BUDGET  " + budget.ToString("N0") + " CR";
        ShowScreen(GameScreen.Store);
    }

    private void BuildAssembly()
    {
        ScreenTitle("SPACECRAFT ASSEMBLY", "Review the selected loadout and validate your mission design.");
        MissionStats stats = CalculateStats();

        GameObject left = Card(currentPage.transform, new Vector2(0.065f, 0.18f), new Vector2(0.68f, 0.68f), cardColor);
        //AddAccent(left.transform, cyan);
        Label("SELECTED SYSTEMS", left.transform, 22, cyan, TextAlignmentOptions.Left,
            new Vector2(470, 40), new Vector2(0.08f, 0.90f), Vector2.zero, true);
        string selectedText = "";
        foreach (Equipment item in equipment)
            if (item.selected)
            {
                string name = GetEquipmentDisplayName(item);
                selectedText += "•  " + name + "   —   " + item.category + "\n";
            }
        if (selectedText.Length == 0) selectedText = "No equipment selected. Return to the store to build your spacecraft.";
        Label(selectedText, left.transform, 18, Color.white, TextAlignmentOptions.Left,
            new Vector2(420, 300), new Vector2(0.08f, 0.60f), Vector2.zero, false);
        BuildCraft3DPreview(left.transform, new Vector2(0.69f, 0.50f));
        Label("DESTINATION  /  " + selectedMission + "     •     CREDITS ARE IN-GAME UNITS", left.transform, 12, gold,
            TextAlignmentOptions.Left, new Vector2(450, 28), new Vector2(0.08f, 0.10f), Vector2.zero, true);

        GameObject right = Card(currentPage.transform, new Vector2(0.69f, 0.18f), new Vector2(0.935f, 0.68f), cardAltColor);
        //AddAccent(right.transform, gold);
        Label("ENGINEERING SUMMARY", right.transform, 20, cyan, TextAlignmentOptions.Center,
            new Vector2(440, 42), new Vector2(0.5f, 0.90f), Vector2.zero, true);
        float capacityUse = stats.capacity > 0f ? stats.mass / stats.capacity * 100f : 0f;
        float powerMargin = stats.generated - stats.draw;
        string summary = "PAYLOAD MASS       " + stats.mass.ToString("N0") + " kg\n" +
                         "SIM CAPACITY      " + stats.capacity.ToString("N0") + " kg\n" +
                         "CAPACITY USED     " + capacityUse.ToString("N1") + "%\n" +
                         "POWER GENERATED   " + stats.generated.ToString("N1") + " kW\n" +
                         "POWER REQUIRED    " + stats.draw.ToString("N1") + " kW\n" +
                         "POWER MARGIN      " + powerMargin.ToString("+0.0;-0.0;0.0") + " kW\n" +
                         "MISSION DURATION  " + (selectedMission == "MARS" ? "210-260 days" : selectedMission == "MOON" ? "8-12 days" : "30 days") + "\n" +
                         "SELECTED SYSTEMS  " + stats.selectedCount + "\n" +
                         "CREDITS LEFT      " + budget.ToString("N0") + " CR";
        Label(summary, right.transform, 17, Color.white, TextAlignmentOptions.Center,
            new Vector2(390, 310), new Vector2(0.5f, 0.56f), Vector2.zero, false);
        bool prelim = HasAllRequiredCategories(stats);
        Label(prelim ? "CORE SYSTEMS PRESENT" : "EQUIPMENT INCOMPLETE", right.transform, 14,
            prelim ? green : red, TextAlignmentOptions.Center, new Vector2(360, 32), new Vector2(0.5f, 0.18f), Vector2.zero, true);

        Button("BACK TO STORE", currentPage.transform, new Vector2(0.16f, 0.075f), new Vector2(240, 50), cardAltColor,
            () => ShowScreen(GameScreen.Store));
        Button("PRE-LAUNCH CHECK", currentPage.transform, new Vector2(0.82f, 0.075f), new Vector2(300, 54), cyan,
            () => { missionReady = ValidateMission(out lastValidation); ShowScreen(GameScreen.Launch); });
    }

    private class MissionStats
    {
        public float mass, capacity, generated, draw;
        public int selectedCount;
        public Equipment rocket;
    }

    private MissionStats CalculateStats()
    {
        MissionStats stats = new MissionStats();
        foreach (Equipment item in equipment)
        {
            if (!item.selected) continue;
            stats.selectedCount++;
            if (item.category == "ROCKET") stats.rocket = item;
            else stats.mass += item.massKg;
            stats.draw += item.powerDrawKw;
            stats.generated += item.powerOutputKw;
        }
        stats.capacity = stats.rocket != null ? stats.rocket.payloadCapacityKg : 0;
        return stats;
    }

    private bool HasAllRequiredCategories(MissionStats stats)
    {
        bool spacecraft = HasSelected("SPACECRAFT");
        bool power = HasSelected("POWER");
        bool communication = HasSelected("COMMUNICATION");
        bool instrument = HasSelected("INSTRUMENT");
        return stats.rocket != null && spacecraft && power && communication && instrument;
    }

    private bool HasSelected(string category)
    {
        return equipment.Exists(item => item.category == category && item.selected);
    }

    private bool ValidateMission(out string report)
    {
        MissionStats stats = CalculateStats();
        List<string> issues = new List<string>();
        if (stats.rocket == null) issues.Add("Select a launch vehicle.");
        if (!HasSelected("SPACECRAFT")) issues.Add("Select a spacecraft.");
        if (!HasSelected("POWER")) issues.Add("Select a power source.");
        if (!HasSelected("COMMUNICATION")) issues.Add("Select a communication system.");
        if (!HasSelected("INSTRUMENT")) issues.Add("Select at least one scientific instrument.");
        if (stats.rocket != null && stats.mass > stats.capacity)
            issues.Add("Payload mass exceeds rocket capacity (" + stats.mass.ToString("N0") + "/" + stats.capacity.ToString("N0") + " kg).");
        if (stats.generated < stats.draw + GetMissionPowerMargin())
            issues.Add("Insufficient power. Add a stronger power source or reduce instrument load.");
        if (selectedMission == "EARTH" && !equipment.Exists(i => i.title == "ORBITER" && i.selected))
            issues.Add("Earth observation requires the ORBITER platform in this prototype.");
        if (selectedMission == "MOON" && !equipment.Exists(i => i.title == "LUNAR LANDER" && i.selected))
            issues.Add("The Moon surface mission requires the LUNAR LANDER in this prototype.");
        if (selectedMission == "MARS" && (stats.rocket == null || stats.rocket.title != "ATLAS B"))
            issues.Add("Mars transfer requires the ATLAS B heavy launch vehicle in this prototype.");
        if (selectedMission == "MARS" && !equipment.Exists(i => i.title == "LUNAR LANDER" && i.selected))
            issues.Add("Select the descent lander for the Mars surface expedition.");

        if (issues.Count == 0)
        {
            report = "ALL CHECKS PASSED\nPayload, power and mission-specific systems meet this prototype's rules.";
            return true;
        }
        report = "CHECKLIST NEEDS ATTENTION\n\n• " + string.Join("\n• ", issues);
        return false;
    }

    private float GetMissionPowerMargin()
    {
        switch (selectedMission)
        {
            case "MARS": return 1.0f;
            case "EARTH": return 0.5f;
            default: return 0.5f;
        }
    }

    private void BuildLaunch()
    {
        ScreenTitle("LAUNCH CONTROL", "Vehicle integration  /  ignition sequence  /  powered ascent");

        GameObject viewCard = Card(currentPage.transform, new Vector2(0.035f, 0.155f), new Vector2(0.675f, 0.685f), cardColor);
       // AddAccent(viewCard.transform, cyan);
        CreateLaunchStageView(viewCard.transform);

        Image countdownPlate = CreateImage("LaunchCountdownPlate", viewCard.transform, new Color32(3, 14, 28, 220));
        RectTransform cp = countdownPlate.rectTransform;
        cp.anchorMin = new Vector2(0.035f, 0.83f);
        cp.anchorMax = new Vector2(0.38f, 0.98f);
        cp.offsetMin = cp.offsetMax = Vector2.zero;
        launchCountdownText = Label("PAD READY", countdownPlate.transform, 25, cyan,
            TextAlignmentOptions.Center, new Vector2(390, 45), new Vector2(0.5f, 0.64f), Vector2.zero, true);
        launchPhaseText = Label("VEHICLE SECURED  /  SYSTEMS STANDBY", countdownPlate.transform, 10, muted,
            TextAlignmentOptions.Center, new Vector2(420, 22), new Vector2(0.5f, 0.23f), Vector2.zero, true);

        GameObject info = Card(currentPage.transform, new Vector2(0.705f, 0.155f), new Vector2(0.965f, 0.685f), cardAltColor);
        //AddAccent(info.transform, gold);
        MissionStats stats = CalculateStats();
        Label("FLIGHT VEHICLE", info.transform, 15, cyan, TextAlignmentOptions.Left,
            new Vector2(390, 28), new Vector2(0.08f, 0.91f), Vector2.zero, true);
        Label(stats.rocket != null ? stats.rocket.title : "NO LAUNCH VEHICLE", info.transform, 26, Color.white,
            TextAlignmentOptions.Left, new Vector2(410, 46), new Vector2(0.08f, 0.81f), Vector2.zero, true);
        string detail = "DESTINATION       " + selectedMission + "\n" +
                        "PAYLOAD MASS      " + stats.mass.ToString("N0") + " kg\n" +
                        "ROCKET CAPACITY   " + stats.capacity.ToString("N0") + " kg\n" +
                        "SYSTEMS INSTALLED " + stats.selectedCount + " / 5\n" +
                        "POWER BALANCE     " + (stats.generated - stats.draw).ToString("N1") + " kW";
        Label(detail, info.transform, 15, Color.white, TextAlignmentOptions.Left,
            new Vector2(410, 175), new Vector2(0.08f, 0.57f), Vector2.zero, false);
        Label(missionReady ? "ALL SYSTEMS GREEN" : "CHECKLIST NEEDS ATTENTION", info.transform, 15,
            missionReady ? green : red, TextAlignmentOptions.Center,
            new Vector2(390, 30), new Vector2(0.5f, 0.20f), Vector2.zero, true);
        Label("IGNITION  •  BOOST  •  STAGE SEPARATION  •  ASCENT", info.transform, 10, muted,
            TextAlignmentOptions.Center, new Vector2(405, 22), new Vector2(0.5f, 0.10f), Vector2.zero, true);

        Button("BACK TO ASSEMBLY", currentPage.transform, new Vector2(0.17f, 0.075f), new Vector2(250, 50), cardAltColor,
            () => { if (!launchRunning) ShowScreen(GameScreen.Assembly); });
        Button("BEGIN LAUNCH SEQUENCE", currentPage.transform, new Vector2(0.82f, 0.075f), new Vector2(350, 56), missionReady ? cyan : cyanDark,
            () => { if (missionReady && !launchRunning) StartCoroutine(LaunchSequence()); });
    }

    private IEnumerator LaunchSequence()
    {
        if (!missionReady || launchRunning || launchRocketRoot == null) yield break;
        launchRunning = true;
        if (launchCountdownText != null) launchCountdownText.text = "FLIGHT COMPUTERS ONLINE";
        if (launchPhaseText != null) launchPhaseText.text = "AUTOMATED SEQUENCE  /  T-05";

        for (int i = 5; i >= 1; i--)
        {
            if (launchCountdownText != null) launchCountdownText.text = "T - " + i.ToString("00");
            if (launchPhaseText != null) launchPhaseText.text = i > 4 ? "GUIDANCE  /  TELEMETRY  /  PROPULSION" : "FINAL ENGINE CHECK  /  LAUNCH COMMIT";
            PlaySfx(countdownClip, 0.82f);
            yield return new WaitForSeconds(0.78f);
        }

        if (launchCountdownText != null) launchCountdownText.text = "ENGINE START";
        if (launchPhaseText != null) launchPhaseText.text = "IGNITION SEQUENCE INITIATED";
        PlaySfx(ignitionClip, 0.95f);
        SetLaunchEffects(true);
        yield return new WaitForSeconds(1.8f);

        if (launchCountdownText != null) launchCountdownText.text = "LIFTOFF";
        if (launchPhaseText != null) launchPhaseText.text = "HOLD-DOWN RELEASE  /  POWERED ASCENT";
        PlaySfx(liftoffClip, 1f);
        StartAudioLoop(rocketLoopClip, 0.62f);
        yield return StartCoroutine(AnimateRocketLiftoff());
        StopAudioLoop();

        if (launchCountdownText != null) launchCountdownText.text = "ORBITAL INSERTION";
        if (launchPhaseText != null) launchPhaseText.text = "LAUNCH VEHICLE SEPARATED  /  TELEMETRY NOMINAL";
        PlaySfx(radioClip, 0.7f);
        yield return new WaitForSeconds(1.1f);

        InitializeFlightSimulation();
        launchRunning = false;
        ShowScreen(GameScreen.MissionControl);
    }

    private IEnumerator AnimateRocketLiftoff()
    {
        if (launchRocketRoot == null) yield break;
        Vector3 initialPosition = launchRocketRoot.position;
        Vector3 surfaceCameraOffset = launchStageCamera != null
            ? launchStageCamera.transform.position - (initialPosition + Vector3.up * 2f)
            : new Vector3(18f, 8f, 34f);
        Vector3 orbitalCameraOffset = new Vector3(7f, 13f, 48f);
        float elapsed = 0f;
        const float duration = 11.2f;
        bool separated = false;
        bool spaceTransitionStarted = false;
        bool surfaceHidden = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Accelerating ascent; the vehicle clears the launch tower before the booster event.
            float rise = 158f * t * t;
            launchRocketRoot.position = initialPosition + Vector3.up * rise;

            // Crossfade is handled by scene visibility: stars/Earth appear first, then the pad disappears.
            if (!spaceTransitionStarted && t >= 0.36f)
            {
                spaceTransitionStarted = true;
                if (launchSpaceEnvironmentRoot != null) launchSpaceEnvironmentRoot.gameObject.SetActive(true);
                PlaySfx(radioClip, 0.42f);
            }
            if (!surfaceHidden && t >= 0.54f)
            {
                surfaceHidden = true;
                if (launchSurfaceEnvironmentRoot != null) launchSurfaceEnvironmentRoot.gameObject.SetActive(false);
                if (launchSmoke != null) launchSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (launchPhaseText != null) launchPhaseText.text = "ABOVE THE ATMOSPHERE  /  ORBITAL TRACKING ACTIVE";
            }

            // Solid boosters detach only after the vehicle has visibly cleared the pad and tower.
            if (!separated && t >= 0.66f)
            {
                separated = true;
                PlaySfx(radioClip, 0.62f);
                if (separationBurst != null)
                {
                    separationBurst.transform.position = launchRocketRoot.position + Vector3.down * 5.5f;
                    separationBurst.Play();
                }
                if (launchBoosterLeft != null) launchBoosterLeft.SetParent(launchStageRoot.transform, true);
                if (launchBoosterRight != null) launchBoosterRight.SetParent(launchStageRoot.transform, true);
                if (launchBoosterFlameLeft != null) launchBoosterFlameLeft.SetActive(false);
                if (launchBoosterFlameRight != null) launchBoosterFlameRight.SetActive(false);
            }

            if (separated)
            {
                float sep = Mathf.Clamp01((t - 0.66f) / 0.25f);
                if (launchBoosterLeft != null && launchBoosterLeft.gameObject.activeSelf)
                {
                    launchBoosterLeft.position += new Vector3(-1.05f, -1.45f, -0.34f) * Time.deltaTime * 5.6f;
                    launchBoosterLeft.Rotate(new Vector3(0f, 0f, 1f), 28f * Time.deltaTime, Space.Self);
                    launchBoosterLeft.localScale = Vector3.one * (1f - sep * 0.28f);
                    if (t > 0.94f) launchBoosterLeft.gameObject.SetActive(false);
                }
                if (launchBoosterRight != null && launchBoosterRight.gameObject.activeSelf)
                {
                    launchBoosterRight.position += new Vector3(1.05f, -1.45f, -0.22f) * Time.deltaTime * 5.6f;
                    launchBoosterRight.Rotate(new Vector3(0f, 0f, -1f), 28f * Time.deltaTime, Space.Self);
                    launchBoosterRight.localScale = Vector3.one * (1f - sep * 0.28f);
                    if (t > 0.94f) launchBoosterRight.gameObject.SetActive(false);
                }
            }

            if (launchSpaceEnvironmentRoot != null && launchSpaceEnvironmentRoot.gameObject.activeSelf)
                launchSpaceEnvironmentRoot.position = launchRocketRoot.position;

            if (launchStageCamera != null)
            {
                float cameraBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.34f, 0.70f, t));
                Vector3 cameraOffset = Vector3.Lerp(surfaceCameraOffset, orbitalCameraOffset, cameraBlend);
                Vector3 target = launchRocketRoot.position + Vector3.up * 1.8f;
                Vector3 lookTarget = target + Vector3.down * Mathf.Lerp(0f, 18f, cameraBlend);
                Vector3 followPosition = target + cameraOffset;
                float shake = t < 0.50f ? (0.025f + t * 0.16f) : 0.018f;
                if (t < 0.73f) followPosition += UnityEngine.Random.insideUnitSphere * shake;
                launchStageCamera.transform.position = Vector3.Lerp(launchStageCamera.transform.position,
                    followPosition, Time.deltaTime * (t > 0.5f ? 2.6f : 1.7f));
                launchStageCamera.transform.LookAt(lookTarget);
            }

            if (launchFlameOuter != null && launchFlameOuter.activeSelf)
            {
                float flicker = 1f + Mathf.Sin(Time.time * 34f) * 0.12f + Mathf.Sin(Time.time * 19f) * 0.06f;
                float flameLength = Mathf.Lerp(2.85f, 4.1f, t);
                launchFlameOuter.transform.localScale = new Vector3(1.55f * flicker, flameLength * flicker, 1.55f * flicker);
                if (launchFlameInner != null)
                    launchFlameInner.transform.localScale = new Vector3(.82f * flicker, flameLength * .78f, .82f * flicker);
            }
            if (launchPhaseText != null)
            {
                if (t > 0.82f) launchPhaseText.text = "UPPER STAGE ASCENT  /  EARTH LIMB IN VIEW";
                else if (t > 0.66f) launchPhaseText.text = "BOOSTER SEPARATION  /  STAGE TWO IGNITION";
                else if (t > 0.54f) launchPhaseText.text = "SPACE TRANSITION  /  VEHICLE TRACKING";
            }
            yield return null;
        }

        if (launchExhaust != null) launchExhaust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (launchSmoke != null) launchSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (launchFlameOuter != null) launchFlameOuter.SetActive(false);
        if (launchFlameInner != null) launchFlameInner.SetActive(false);
        if (launchPhaseText != null) launchPhaseText.text = "ORBITAL INSERTION  /  SPACECRAFT SEPARATION";
    }

    private void SetLaunchEffects(bool enabled)
    {
        if (launchFlameOuter != null) launchFlameOuter.SetActive(enabled);
        if (launchFlameInner != null) launchFlameInner.SetActive(enabled);
        if (launchBoosterFlameLeft != null) launchBoosterFlameLeft.SetActive(enabled);
        if (launchBoosterFlameRight != null) launchBoosterFlameRight.SetActive(enabled);
        if (launchExhaust != null)
        {
            var emission = launchExhaust.emission;
            emission.rateOverTime = enabled ? 620f : 0f;
            if (enabled) launchExhaust.Play(); else launchExhaust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (launchSmoke != null)
        {
            var emission = launchSmoke.emission;
            emission.rateOverTime = enabled ? 95f : 0f;
            if (enabled) launchSmoke.Play(); else launchSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private Texture2D BuildLaunchSkyTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Launch_Twilight_Sky_Gradient";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            Color baseColor = v < 0.36f
                ? Color.Lerp(new Color(0.72f, 0.27f, 0.16f), new Color(0.22f, 0.20f, 0.34f), v / 0.36f)
                : Color.Lerp(new Color(0.22f, 0.20f, 0.34f), new Color(0.025f, 0.055f, 0.12f), (v - 0.36f) / 0.64f);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float cloud = Mathf.PerlinNoise(u * 8.5f + 3.1f, v * 9.0f + 7.7f);
                float streak = Mathf.PerlinNoise(u * 19f + v * 2.2f, v * 5.8f + 2.5f);
                float glow = Mathf.Clamp01(1f - Mathf.Abs(v - 0.31f) * 3.6f) * Mathf.Clamp01(1f - Mathf.Abs(u - 0.58f) * 1.6f);
                Color c = baseColor;
                c = Color.Lerp(c, new Color(0.54f, 0.37f, 0.39f), Mathf.Max(0f, cloud - 0.52f) * 0.26f);
                c = Color.Lerp(c, new Color(1f, 0.48f, 0.25f), glow * 0.24f + Mathf.Max(0f, streak - 0.7f) * 0.10f);
                pixels[y * width + x] = c;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private Material MakeUnlitTextureMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        transientMaterials.Add(material);
        return material;
    }

    private void CreateLaunchStageView(Transform parent)
    {
        launchStageRoot = new GameObject("3D_ROCKET_LAUNCH_STAGE");
        launchStageRoot.transform.position = Vector3.zero;
        launchStageTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
        launchStageTexture.name = "RocketLaunchCameraTexture";
        launchStageTexture.antiAliasing = 2;
        launchStageTexture.Create();

        GameObject cameraObject = new GameObject("LaunchStageCamera", typeof(Camera));
        cameraObject.transform.SetParent(launchStageRoot.transform, false);
        launchStageCamera = cameraObject.GetComponent<Camera>();
        launchStageCamera.targetTexture = launchStageTexture;
        launchStageCamera.clearFlags = CameraClearFlags.SolidColor;
        launchStageCamera.backgroundColor = new Color32(3, 9, 24, 255);
        launchStageCamera.fieldOfView = 38f;
        launchStageCamera.nearClipPlane = 0.1f;
        launchStageCamera.farClipPlane = 500f;
        launchStageCamera.transform.position = new Vector3(18f, 17f, 39f);
        launchStageCamera.transform.LookAt(new Vector3(0f, 10f, 0f));

        Light key = new GameObject("LaunchKeyLight", typeof(Light)).GetComponent<Light>();
        key.transform.SetParent(launchStageRoot.transform, false);
        key.type = LightType.Directional;
        key.intensity = 1.55f;
        key.color = new Color(0.70f, 0.84f, 1f);
        key.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light warm = new GameObject("PadWarmLight", typeof(Light)).GetComponent<Light>();
        warm.transform.SetParent(launchStageRoot.transform, false);
        warm.type = LightType.Point;
        warm.color = new Color(1f, 0.43f, 0.12f);
        warm.intensity = 12f;
        warm.range = 27f;
        warm.transform.position = new Vector3(2f, 3f, -4f);

        Material ground = MakeMaterial(new Color32(20, 31, 45, 255), 0.35f, 0f);
        Material concrete = MakeMaterial(new Color32(67, 79, 91, 255), 0.45f, 0f);
        Material towerMat = MakeMaterial(new Color32(115, 139, 157, 255), 0.72f, 0f);
        Material darkMetal = MakeMaterial(new Color32(29, 43, 57, 255), 0.82f, 0f);
        Material whiteMetal = MakeMaterial(new Color32(218, 229, 237, 255), 0.38f, 0f);
        Material orange = MakeMaterial(new Color32(236, 116, 35, 255), 0.30f, 0.15f);
        Material blue = MakeMaterial(new Color32(25, 111, 166, 255), 0.42f, 0.18f);
        Material glass = MakeMaterial(new Color32(30, 172, 224, 255), 0.1f, 0.95f);

        // Earth launch infrastructure is grouped so it can disappear once the rocket reaches space.
        launchSurfaceEnvironmentRoot = new GameObject("EARTH_LAUNCH_SURFACE_ENVIRONMENT").transform;
        launchSurfaceEnvironmentRoot.SetParent(launchStageRoot.transform, false);

        // A layered twilight sky, distant terrain and runway markings replace the empty studio-like backdrop.
        Texture2D launchSkyTexture = BuildLaunchSkyTexture(512, 256);
        transientTextures.Add(launchSkyTexture);
        Material launchSkyMaterial = MakeUnlitTextureMaterial(launchSkyTexture);
        GameObject launchSky = CreatePrimitiveObject("LaunchSkyBackdrop", PrimitiveType.Cube,
            launchSurfaceEnvironmentRoot, new Vector3(0f, 15f, -105f), new Vector3(225f, 112f, 0.35f), launchSkyMaterial);
        Material mountainMaterial = MakeMaterial(new Color32(11, 22, 38, 255), 0.05f, 0.02f);
        for (int i = 0; i < 12; i++)
        {
            float x = -66f + i * 12f;
            float height = 4f + (i % 4) * 2.1f;
            CreatePrimitiveObject("DistantMountain_" + i, PrimitiveType.Sphere, launchSurfaceEnvironmentRoot,
                new Vector3(x, -3.2f + height * 0.22f, -76f - (i % 3) * 5f),
                new Vector3(15f, height, 5.5f), mountainMaterial);
        }
        Material horizonGlow = MakeMaterial(new Color32(255, 132, 72, 255), 0f, 2.4f);
        CreatePrimitiveObject("HorizonSunGlow", PrimitiveType.Sphere, launchSurfaceEnvironmentRoot,
            new Vector3(19f, 11f, -70f), new Vector3(7f, 7f, 1.6f), horizonGlow);

        CreatePrimitiveObject("LaunchGround", PrimitiveType.Plane, launchSurfaceEnvironmentRoot,
            new Vector3(0, -0.45f, 0), new Vector3(13f, 1f, 13f), ground);
        CreatePrimitiveObject("LaunchPad", PrimitiveType.Cylinder, launchSurfaceEnvironmentRoot,
            new Vector3(0, 0.08f, 0), new Vector3(8.5f, 0.32f, 8.5f), concrete);
        CreatePrimitiveObject("PadCenter", PrimitiveType.Cylinder, launchSurfaceEnvironmentRoot,
            new Vector3(0, 0.27f, 0), new Vector3(4.8f, 0.12f, 4.8f), darkMetal);
        CreatePrimitiveObject("PadRing", PrimitiveType.Cylinder, launchSurfaceEnvironmentRoot,
            new Vector3(0, 0.36f, 0), new Vector3(4.3f, 0.035f, 4.3f), orange);
        for (int i = -5; i <= 5; i++)
        {
            CreatePrimitiveObject("PadSafetyMarking", PrimitiveType.Cube, launchSurfaceEnvironmentRoot,
                new Vector3(i * 1.05f, 0.105f, 5.6f), new Vector3(0.58f, 0.035f, 0.20f), orange);
        }
        CreatePrimitiveObject("ServiceRoad", PrimitiveType.Cube, launchSurfaceEnvironmentRoot,
            new Vector3(7.5f, -0.16f, 7f), new Vector3(18f, 0.12f, 4.4f), darkMetal);
        // Distant launch operations campus: low control buildings, observation windows, antenna dishes and access roads.
        Material facilityWall = MakeMaterial(new Color32(76,91,106,255), .48f, 0f);
        Material facilityGlass = MakeMaterial(new Color32(25,142,190,255), .18f, .8f);
        Material facilityRoof = MakeMaterial(new Color32(34,48,62,255), .62f, 0f);
        for(int b=0;b<3;b++)
        {
            float bx=34f+b*18f, bz=-22f-b*4f, by=2.6f+(b%2)*1.2f;
            CreatePrimitiveObject("DISTANT_MISSION_CONTROL_BUILDING_"+b,PrimitiveType.Cube,launchSurfaceEnvironmentRoot,
                new Vector3(bx,by,bz),new Vector3(17f,by*1.5f,10f),facilityWall);
            CreatePrimitiveObject("CONTROL_BUILDING_ROOF_"+b,PrimitiveType.Cube,launchSurfaceEnvironmentRoot,
                new Vector3(bx,by*2.0f,bz),new Vector3(17.8f,.45f,10.8f),facilityRoof);
            for(int w=0;w<5;w++) CreatePrimitiveObject("CONTROL_ROOM_WINDOW",PrimitiveType.Cube,launchSurfaceEnvironmentRoot,
                new Vector3(bx-6.4f+w*3.2f,by,bz+5.08f),new Vector3(2.3f,1.15f,.12f),facilityGlass);
            CreateBeam(launchSurfaceEnvironmentRoot,new Vector3(bx,by*2f,bz),new Vector3(bx,by*2f+5f,bz),.12f,darkMetal);
            CreatePrimitiveObject("FACILITY_ANTENNA_DISH",PrimitiveType.Sphere,launchSurfaceEnvironmentRoot,
                new Vector3(bx,by*2f+5.2f,bz),new Vector3(2.2f,.45f,2.2f),whiteMetal);
        }
        CreatePrimitiveObject("REMOTE_OBSERVATION_TOWER",PrimitiveType.Cylinder,launchSurfaceEnvironmentRoot,
            new Vector3(57f,9f,-48f),new Vector3(.7f,18f,.7f),towerMat);
        CreatePrimitiveObject("OBSERVATION_TOWER_CABIN",PrimitiveType.Cube,launchSurfaceEnvironmentRoot,
            new Vector3(57f,18f,-48f),new Vector3(4.5f,2.5f,4.5f),facilityGlass);
        CreatePrimitiveObject("REMOTE_ROAD",PrimitiveType.Cube,launchSurfaceEnvironmentRoot,
            new Vector3(35f,-.12f,2f),new Vector3(70f,.10f,3.2f),darkMetal);

        // Launch tower: four steel legs, cross-braces, platforms and access arms.
        float towerX = -5.0f, towerZ = -0.6f, towerHeight = 22f, towerWidth = 2.5f;
        Vector3[] baseCorners = {
            new Vector3(towerX-towerWidth*.5f, .5f, towerZ-towerWidth*.5f),
            new Vector3(towerX+towerWidth*.5f, .5f, towerZ-towerWidth*.5f),
            new Vector3(towerX-towerWidth*.5f, .5f, towerZ+towerWidth*.5f),
            new Vector3(towerX+towerWidth*.5f, .5f, towerZ+towerWidth*.5f)
        };
        for (int i=0;i<4;i++)
            CreateBeam(launchSurfaceEnvironmentRoot, baseCorners[i], baseCorners[i]+Vector3.up*towerHeight, .22f, towerMat);
        for (int y=3;y<=21;y+=3)
        {
            float half=towerWidth*.5f;
            CreateBeam(launchSurfaceEnvironmentRoot,new Vector3(towerX-half,y,towerZ-half),new Vector3(towerX+half,y,towerZ-half),.14f,towerMat);
            CreateBeam(launchSurfaceEnvironmentRoot,new Vector3(towerX-half,y,towerZ+half),new Vector3(towerX+half,y,towerZ+half),.14f,towerMat);
            CreateBeam(launchSurfaceEnvironmentRoot,new Vector3(towerX-half,y,towerZ-half),new Vector3(towerX-half,y,towerZ+half),.14f,towerMat);
            CreateBeam(launchSurfaceEnvironmentRoot,new Vector3(towerX+half,y,towerZ-half),new Vector3(towerX+half,y,towerZ+half),.14f,towerMat);
            if (y<21)
            {
                CreateBeam(launchSurfaceEnvironmentRoot,new Vector3(towerX-half,y,towerZ-half),new Vector3(towerX+half,y+3,towerZ-half),.09f,darkMetal);
                CreateBeam(launchSurfaceEnvironmentRoot,new Vector3(towerX+half,y,towerZ+half),new Vector3(towerX-half,y+3,towerZ+half),.09f,darkMetal);
            }
        }
        CreateBeam(launchSurfaceEnvironmentRoot,new Vector3(-3.8f,13f,0f),new Vector3(-0.7f,13f,0f),.34f,orange);
        CreateBeam(launchSurfaceEnvironmentRoot,new Vector3(-3.8f,8.5f,0f),new Vector3(-0.7f,8.5f,0f),.28f,towerMat);
        for(int i=0;i<5;i++)
            CreateBeam(launchSurfaceEnvironmentRoot,new Vector3(-6.6f+i*.75f,1f,3.9f),new Vector3(-6.6f+i*.75f,1f,4.4f),.12f,orange);

        // Mission vehicle: central core, tapered nose, engine bells, markings and four fins.
        MissionStats stats = CalculateStats();
        string rocketName = stats.rocket != null ? stats.rocket.title : "ATLAS A";
        launchRocketRoot = new GameObject("PRIMARY_ROCKET_STAGE_"+rocketName.Replace(" ","_" )).transform;
        launchRocketRoot.SetParent(launchStageRoot.transform, false);
        launchRocketRoot.localPosition = new Vector3(0.35f, 9.3f, 0f);
        GameObject body = CreatePrimitiveObject("CoreTank", PrimitiveType.Cylinder, launchRocketRoot,
            new Vector3(0,0,0), new Vector3(1.68f,7.0f,1.68f), whiteMetal);
        CreatePrimitiveObject("Interstage", PrimitiveType.Cylinder, launchRocketRoot,
            new Vector3(0,-5.75f,0), new Vector3(1.76f,0.9f,1.76f), darkMetal);
        CreatePrimitiveObject("UpperStage", PrimitiveType.Cylinder, launchRocketRoot,
            new Vector3(0,5.2f,0), new Vector3(1.32f,2.2f,1.32f), whiteMetal);
        CreateConeObject("NoseCone", launchRocketRoot, new Vector3(0,7.2f,0), .82f, 3.0f, whiteMetal);
        CreatePrimitiveObject("NoseTip", PrimitiveType.Sphere, launchRocketRoot,
            new Vector3(0,8.7f,0), new Vector3(.10f,.18f,.10f), orange);
        for(int band=-2;band<=2;band++)
            CreatePrimitiveObject("OrangeTankBand", PrimitiveType.Cylinder, launchRocketRoot,
                new Vector3(0,band*1.85f,0), new Vector3(1.69f,.06f,1.69f), orange);
        for(int i=0;i<4;i++)
        {
            float a=i*90f;
            GameObject fin=CreatePrimitiveObject("RocketFin",PrimitiveType.Cube,launchRocketRoot,
                new Vector3(Mathf.Sin(a*Mathf.Deg2Rad)*.75f,-5.65f,Mathf.Cos(a*Mathf.Deg2Rad)*.75f),
                new Vector3(.20f,2.0f,1.10f),orange);
            fin.transform.localRotation=Quaternion.Euler(0,a,0);
        }
        for(int i=-1;i<=1;i++)
            CreatePrimitiveObject("EngineBell",PrimitiveType.Cylinder,launchRocketRoot,
                new Vector3(i*.45f,-7.55f,0),new Vector3(.42f,.76f,.42f),darkMetal);
        for(int i=-1;i<=1;i++)
        {
            CreatePrimitiveObject("EngineInner",PrimitiveType.Cylinder,launchRocketRoot,
                new Vector3(i*.45f,-7.98f,0),new Vector3(.24f,.12f,.24f),orange);
        }
        // Small windows and a mission insignia strip.
        for(int i=0;i<3;i++)
        {
            GameObject window=CreatePrimitiveObject("CrewWindow",PrimitiveType.Sphere,launchRocketRoot,
                new Vector3(0,2.9f-i*.55f,.82f),new Vector3(.22f,.22f,.08f),glass);
        }
        CreatePrimitiveObject("MissionMark",PrimitiveType.Cube,launchRocketRoot,
            new Vector3(0,-1.25f,.84f),new Vector3(.72f,.42f,.035f),blue);

        // Side boosters are separate 3D bodies and detach during ascent.
        launchBoosterLeft=CreateSideBooster(launchRocketRoot,"LEFT_SOLID_BOOSTER",new Vector3(-1.48f,-2.1f,0),whiteMetal,orange);
        launchBoosterRight=CreateSideBooster(launchRocketRoot,"RIGHT_SOLID_BOOSTER",new Vector3(1.48f,-2.1f,0),whiteMetal,orange);

        Material flameOuterMat=MakeMaterial(new Color32(255,104,24,255),0f,4.8f);
        Material flameInnerMat=MakeMaterial(new Color32(255,218,106,255),0f,7.0f);
        launchFlameOuter=CreateConeObject("VisibleOuterEngineFlame",launchRocketRoot,new Vector3(0,-8.02f,0),.72f,3.0f,flameOuterMat);
        launchFlameOuter.transform.localRotation=Quaternion.Euler(180f,0f,0f);
        launchFlameOuter.transform.localScale=new Vector3(1.55f,2.85f,1.55f);
        launchFlameOuter.SetActive(false);
        launchFlameInner=CreateConeObject("VisibleInnerEngineFlame",launchRocketRoot,new Vector3(0,-8.02f,0),.34f,2.5f,flameInnerMat);
        launchFlameInner.transform.localRotation=Quaternion.Euler(180f,0f,0f);
        launchFlameInner.transform.localScale=new Vector3(.82f,2.35f,.82f);
        launchFlameInner.SetActive(false);
        launchBoosterFlameLeft = CreateConeObject("LEFT_BOOSTER_EXHAUST_FLAME", launchBoosterLeft,
            new Vector3(0f, -4.55f, 0f), .42f, 2.3f, flameOuterMat);
        launchBoosterFlameRight = CreateConeObject("RIGHT_BOOSTER_EXHAUST_FLAME", launchBoosterRight,
            new Vector3(0f, -4.55f, 0f), .42f, 2.3f, flameOuterMat);
        launchBoosterFlameLeft.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
        launchBoosterFlameRight.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
        launchBoosterFlameLeft.transform.localScale = new Vector3(1.25f, 2.5f, 1.25f);
        launchBoosterFlameRight.transform.localScale = new Vector3(1.25f, 2.5f, 1.25f);
        launchBoosterFlameLeft.SetActive(false);
        launchBoosterFlameRight.SetActive(false);
        launchExhaust=CreateLaunchParticleSystem("MainEngineExhaust",launchRocketRoot,
            new Vector3(0,-8.0f,0),new Color(1f,.72f,.16f,1f),new Color(1f,.16f,.03f,.05f),.85f,18f,1.25f,12f);
        launchSmoke=CreateLaunchParticleSystem("LaunchPadSmoke",launchStageRoot.transform,
            new Vector3(.35f,.55f,0),new Color(.82f,.88f,.94f,.48f),new Color(.34f,.42f,.52f,0f),3.8f,5.5f,3.4f,24f);
        separationBurst=CreateLaunchParticleSystem("StageSeparationParticles",launchStageRoot.transform,
            new Vector3(.35f,6f,0),new Color(1f,.77f,.32f,1f),new Color(.95f,.23f,.04f,0f),.65f,8f,.45f,65f);
        if (separationBurst != null) separationBurst.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);

        // Orbital backdrop stays hidden until the vehicle clears the launch tower.
        launchSpaceEnvironmentRoot = new GameObject("ORBITAL_SPACE_ENVIRONMENT").transform;
        launchSpaceEnvironmentRoot.SetParent(launchStageRoot.transform, false);
        launchSpaceEnvironmentRoot.gameObject.SetActive(false);
        launchStarDome = new GameObject("ORBITAL_STAR_FIELD").transform;
        launchStarDome.SetParent(launchSpaceEnvironmentRoot, false);
        Material starMat = MakeMaterial(new Color32(190, 218, 255, 255), 0f, 2.8f);
        System.Random random = new System.Random(77);
        for (int i = 0; i < 620; i++)
        {
            Vector3 direction = UnityEngine.Random.onUnitSphere;
            float distance = 280f + (float)random.NextDouble() * 110f;
            Vector3 pos = direction * distance;
            float size = 0.045f + (float)random.NextDouble() * 0.095f;
            GameObject star = CreatePrimitiveObject("OrbitalStar_" + i, PrimitiveType.Sphere, launchStarDome, pos,
                Vector3.one * size, starMat);
            Renderer starRenderer = star.GetComponent<Renderer>();
            starRenderer.shadowCastingMode = ShadowCastingMode.Off;
            starRenderer.receiveShadows = false;
        }
        Texture2D earthTexture = Resources.Load<Texture2D>("Environments/Earth_Diffuse");
        if (earthTexture == null) earthTexture = BuildMissionPlanetTexture("EARTH", 1024, 512);
        Material orbitalEarthMat = MakeMaterial(Color.white, 0.12f, 0.12f);
        if (orbitalEarthMat.HasProperty("_BaseMap")) orbitalEarthMat.SetTexture("_BaseMap", earthTexture);
        if (orbitalEarthMat.HasProperty("_MainTex")) orbitalEarthMat.SetTexture("_MainTex", earthTexture);
        launchEarthTransform = CreatePrimitiveObject("EARTH_CURVATURE_FROM_ORBIT", PrimitiveType.Sphere,
            launchSpaceEnvironmentRoot, new Vector3(0f, -142f, -232f), new Vector3(252f, 252f, 252f), orbitalEarthMat).transform;
        launchEarthTransform.localRotation = Quaternion.Euler(8f, 24f, -4f);
        launchEarthTransform.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        launchEarthTransform.GetComponent<Renderer>().receiveShadows = false;

        /* GameObject rawObject=new GameObject("RocketLaunch3DRender",typeof(RectTransform),typeof(RawImage));
         rawObject.transform.SetParent(parent,false);
         launchDisplay=rawObject.GetComponent<RawImage>();
         launchDisplay.texture=launchStageTexture;
         launchDisplay.color=Color.white;
         RectTransform rawRect=launchDisplay.rectTransform;
         rawRect.anchorMin=new Vector2(.012f,.012f);
         rawRect.anchorMax=new Vector2(.988f,.988f);
         rawRect.offsetMin=rawRect.offsetMax=Vector2.zero;*/
        //omar zidane
        GameObject viewport = new GameObject(
     "RoundedLaunchViewport",
     typeof(RectTransform),
     typeof(Image),
     typeof(Mask));

        viewport.transform.SetParent(parent, false);

        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(.012f, .012f);
        viewportRect.anchorMax = new Vector2(.988f, .988f);
        viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;

        Image maskImage = viewport.GetComponent<Image>();
        maskImage.sprite = roundedCardSprite;
        maskImage.type = Image.Type.Sliced;
        maskImage.color = Color.white;

        Mask mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject rawObject = new GameObject(
            "RocketLaunch3DRender",
            typeof(RectTransform),
            typeof(RawImage));

        rawObject.transform.SetParent(viewport.transform, false);

        launchDisplay = rawObject.GetComponent<RawImage>();
        launchDisplay.texture = launchStageTexture;
        launchDisplay.color = Color.white;

        RectTransform rawRect = launchDisplay.rectTransform;
        rawRect.anchorMin = Vector2.zero;
        rawRect.anchorMax = Vector2.one;
        rawRect.offsetMin = rawRect.offsetMax = Vector2.zero;
    }

    private Transform CreateSideBooster(Transform parent,string objectName,Vector3 position,Material bodyMat,Material finMat)
    {
        Transform booster=new GameObject(objectName).transform;
        booster.SetParent(parent,false);
        booster.localPosition=position;
        CreatePrimitiveObject("BoosterTank",PrimitiveType.Cylinder,booster,Vector3.zero,new Vector3(.68f,4.3f,.68f),bodyMat);
        CreateConeObject("BoosterNose",booster,new Vector3(0,4.35f,0),.33f,1.3f,bodyMat);
        CreatePrimitiveObject("BoosterInterstage",PrimitiveType.Cylinder,booster,new Vector3(0,-3.55f,0),new Vector3(.75f,.45f,.75f),finMat);
        for(int i=0;i<4;i++)
        {
            float a=i*90f;
            GameObject fin=CreatePrimitiveObject("BoosterFin",PrimitiveType.Cube,booster,
                new Vector3(Mathf.Sin(a*Mathf.Deg2Rad)*.35f,-3.2f,Mathf.Cos(a*Mathf.Deg2Rad)*.35f),
                new Vector3(.12f,1.25f,.55f),finMat);
            fin.transform.localRotation=Quaternion.Euler(0,a,0);
        }
        CreatePrimitiveObject("BoosterNozzle",PrimitiveType.Cylinder,booster,new Vector3(0,-4.3f,0),new Vector3(.36f,.55f,.36f),MakeMaterial(new Color32(45,55,68,255),.8f,0));
        return booster;
    }

    private ParticleSystem CreateLaunchParticleSystem(string objectName,Transform parent,Vector3 localPosition,
        Color startColor,Color endColor,float lifetime,float speed,float size,float angle)
    {
        GameObject go=new GameObject(objectName,typeof(ParticleSystem));
        go.transform.SetParent(parent,false);
        go.transform.localPosition=localPosition;
        go.transform.localRotation=Quaternion.Euler(90f,0f,0f);
        ParticleSystem ps=go.GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;
        main.playOnAwake=false;
        // Keep Unity's default duration; changing duration on a live particle system can emit a runtime warning.
        main.loop=true;
        main.startLifetime=lifetime; main.startSpeed=speed; main.startSize=size;
        main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.maxParticles=2200;
        main.startColor=new ParticleSystem.MinMaxGradient(startColor,endColor);
        var emission=ps.emission; emission.enabled=true; emission.rateOverTime=0f;
        var shape=ps.shape; shape.enabled=true; shape.shapeType=ParticleSystemShapeType.Cone;
        shape.angle=angle; shape.radius=.42f; shape.length=.6f;
        var renderer=go.GetComponent<ParticleSystemRenderer>();
        Shader particleShader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if(particleShader==null) particleShader=Shader.Find("Particles/Standard Unlit");
        if(particleShader!=null) renderer.material=new Material(particleShader);
        renderer.renderMode=ParticleSystemRenderMode.Billboard;
        return ps;
    }

    private void BuildCraft3DPreview(Transform parent,Vector2 center)
    {
        craftPreviewRoot=new GameObject("3D_SPACECRAFT_ASSEMBLY_PREVIEW");
        craftPreviewTexture=new RenderTexture(500,340,24,RenderTextureFormat.ARGB32);
        craftPreviewTexture.name="SpacecraftAssemblyPreviewTexture";
        craftPreviewTexture.antiAliasing=2; craftPreviewTexture.Create();
        GameObject camGo=new GameObject("CraftPreviewCamera",typeof(Camera));
        camGo.transform.SetParent(craftPreviewRoot.transform,false);
        Camera cam=camGo.GetComponent<Camera>(); cam.targetTexture=craftPreviewTexture;
        cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color32(5,14,29,255);
        cam.orthographic=true; cam.orthographicSize=4.1f; cam.nearClipPlane=.1f; cam.farClipPlane=100f;
        cam.transform.position=new Vector3(0,0,14); cam.transform.LookAt(Vector3.zero);
        Light light=new GameObject("CraftPreviewLight",typeof(Light)).GetComponent<Light>();
        light.transform.SetParent(craftPreviewRoot.transform,false); light.type=LightType.Directional; light.intensity=1.45f;
        light.transform.rotation=Quaternion.Euler(35,-25,0);

        Material metal=MakeMaterial(new Color32(200,218,230,255),.55f,0);
        Material dark=MakeMaterial(new Color32(37,61,81,255),.7f,0);
        Material solar=MakeMaterial(new Color32(28,110,177,255),.45f,.18f);
        Material goldMat=MakeMaterial(new Color32(231,164,65,255),.35f,.05f);
        Material glow=MakeMaterial(new Color32(40,197,245,255),.2f,.8f);
        craftPreviewSpin=new GameObject("RotatingSpacecraftModel").transform;
        craftPreviewSpin.SetParent(craftPreviewRoot.transform,false);
        CreatePrimitiveObject("SpacecraftBus",PrimitiveType.Cube,craftPreviewSpin,Vector3.zero,new Vector3(1.45f,1.05f,.9f),metal);
        CreatePrimitiveObject("ForwardInstrument",PrimitiveType.Cylinder,craftPreviewSpin,new Vector3(0,-.82f,.08f),new Vector3(.58f,.28f,.58f),dark);
        CreatePrimitiveObject("AntennaMast",PrimitiveType.Cylinder,craftPreviewSpin,new Vector3(0,.8f,0),new Vector3(.09f,.85f,.09f),goldMat);
        CreatePrimitiveObject("AntennaDish",PrimitiveType.Sphere,craftPreviewSpin,new Vector3(0,1.28f,.05f),new Vector3(1.05f,.20f,1.05f),metal);
        CreatePrimitiveObject("AntennaDishHub",PrimitiveType.Sphere,craftPreviewSpin,new Vector3(0,1.38f,.05f),new Vector3(.20f,.14f,.20f),glow);
        if(HasSelected("POWER"))
        {
            CreatePrimitiveObject("SolarArrayLeft",PrimitiveType.Cube,craftPreviewSpin,new Vector3(-2.0f,.05f,0),new Vector3(1.45f,.075f,.95f),solar);
            CreatePrimitiveObject("SolarArrayRight",PrimitiveType.Cube,craftPreviewSpin,new Vector3(2.0f,.05f,0),new Vector3(1.45f,.075f,.95f),solar);
            for(int i=-2;i<=2;i++)
            {
                CreateBeam(craftPreviewSpin,new Vector3(-2.0f+i*.23f,-.01f,-.48f),new Vector3(-2.0f+i*.23f,-.01f,.48f),.018f,glow);
                CreateBeam(craftPreviewSpin,new Vector3(2.0f+i*.23f,-.01f,-.48f),new Vector3(2.0f+i*.23f,-.01f,.48f),.018f,glow);
            }
        }
        if(HasSelected("SPACECRAFT") && equipment.Exists(e=>e.title=="LUNAR LANDER" && e.selected))
        {
            for(int i=0;i<3;i++)
            {
                float a=i*120*Mathf.Deg2Rad;
                Vector3 leg=new Vector3(Mathf.Cos(a)*.9f,-.95f,Mathf.Sin(a)*.9f);
                CreateBeam(craftPreviewSpin,new Vector3(0,-.42f,0),leg,.075f,goldMat);
                CreatePrimitiveObject("LanderFoot",PrimitiveType.Cube,craftPreviewSpin,leg+new Vector3(0,-.06f,0),new Vector3(.42f,.08f,.30f),dark);
            }
        }
        if(HasSelected("INSTRUMENT"))
            CreatePrimitiveObject("ScienceCameraHousing",PrimitiveType.Cube,craftPreviewSpin,new Vector3(.55f,-.58f,.58f),new Vector3(.45f,.42f,.38f),dark);
        if(HasSelected("COMMUNICATION"))
            CreatePrimitiveObject("HighGainDish",PrimitiveType.Sphere,craftPreviewSpin,new Vector3(-.62f,.82f,.12f),new Vector3(.60f,.14f,.60f),metal);

        GameObject frame=Card(parent,new Vector2(center.x-.235f,center.y-.30f),new Vector2(center.x+.235f,center.y+.30f),new Color32(5,16,31,255));
        RawImage raw=new GameObject("3DSpacecraftPreview",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
        raw.transform.SetParent(frame.transform,false); raw.texture=craftPreviewTexture;
        RectTransform rr=raw.rectTransform; rr.anchorMin=new Vector2(.025f,.025f); rr.anchorMax=new Vector2(.975f,.975f); rr.offsetMin=rr.offsetMax=Vector2.zero;
    }

    private GameObject CreatePrimitiveObject(string name,PrimitiveType type,Transform parent,Vector3 localPosition,Vector3 localScale,Material material)
    {
        GameObject go=GameObject.CreatePrimitive(type);
        go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=localPosition; go.transform.localScale=localScale;
        Renderer renderer=go.GetComponent<Renderer>();
        if(renderer!=null && material!=null) renderer.sharedMaterial=material;
        Collider collider=go.GetComponent<Collider>(); if(collider!=null) Destroy(collider);
        return go;
    }

    private Material MakeMaterial(Color color,float metallic,float emission)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null) shader=Shader.Find("Standard");
        if(shader==null) shader=Shader.Find("Unlit/Color");
        Material material=new Material(shader);
        if(material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        if(material.HasProperty("_Color")) material.SetColor("_Color",color);
        if(material.HasProperty("_Metallic")) material.SetFloat("_Metallic",metallic);
        if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.58f);
        if(emission>0f)
        {
            Color ec=color*emission;
            if(material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor",ec);
            material.EnableKeyword("_EMISSION");
        }
        transientMaterials.Add(material);
        return material;
    }

    private void CreateBeam(Transform parent,Vector3 start,Vector3 end,float thickness,Material material)
    {
        Vector3 delta=end-start;
        GameObject beam=CreatePrimitiveObject("StructuralBeam",PrimitiveType.Cube,parent,(start+end)*.5f,
            new Vector3(thickness,delta.magnitude,thickness),material);
        beam.transform.localRotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);
    }

    private GameObject CreateConeObject(string name,Transform parent,Vector3 localPosition,float radius,float height,Material material)
    {
        const int segments=32;
        Mesh mesh=new Mesh(); mesh.name=name+"Mesh";
        Vector3[] vertices=new Vector3[segments+2];
        for(int i=0;i<segments;i++)
        {
            float a=2f*Mathf.PI*i/segments;
            vertices[i]=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
        }
        vertices[segments]=new Vector3(0,height,0);
        vertices[segments+1]=Vector3.zero;
        int[] triangles=new int[segments*6];
        for(int i=0;i<segments;i++)
        {
            int next=(i+1)%segments;
            int k=i*6;
            triangles[k]=i; triangles[k+1]=segments; triangles[k+2]=next;
            triangles[k+3]=segments+1; triangles[k+4]=i; triangles[k+5]=next;
        }
        mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        GameObject cone=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
        cone.transform.SetParent(parent,false); cone.transform.localPosition=localPosition;
        cone.GetComponent<MeshFilter>().sharedMesh=mesh;
        cone.GetComponent<MeshRenderer>().sharedMaterial=material;
        return cone;
    }

    private void InitializeFlightSimulation()
    {
        MissionStats stats = CalculateStats();
        float surplus = Mathf.Max(0f, stats.generated - stats.draw);
        float payloadRatio = stats.capacity > 0f ? stats.mass / stats.capacity : 1f;

        flightPower = Mathf.Clamp(Mathf.RoundToInt(70f + surplus * 5f - payloadRatio * 8f), 42, 96);
        flightLink = HasSelected("COMMUNICATION") ? (selectedMission == "MARS" ? 70 : 84) : 25;
        flightIntegrity = Mathf.Clamp(Mathf.RoundToInt(100f - Mathf.Max(0f, payloadRatio - 0.45f) * 24f), 62, 100);
        flightDataPoints = 0;
        flightScore = 38;
        flightDay = 0;
        flightEventIndex = 0;
        ResetFlightCommands();
        transmittedDataPoints = 0;
        missionSuccess = false;
        missionScore = 0;
        flightLog.Clear();
        lastFlightOutcome = "Launch confirmed. Spacecraft has entered its mission phase.";
        BuildFlightEvents();
    }

    private void BuildFlightEvents()
    {
        flightEvents.Clear();

        if (selectedMission == "MOON")
        {
            flightEvents.Add(new MissionEvent("LUNAR ORBIT INSERTION",
                "Navigation telemetry shows a small trajectory error. Choose how to correct the approach.",
                new MissionOption("PRECISION CORRECTION BURN", "Power -10  /  Link +4  /  Score +8", "A precise burn corrected the lunar approach. Fuel and power reserves decreased.", -10, 4, 0, 1, 8, 1),
                new MissionOption("CONSERVATIVE LOW-THRUST PATH", "Power -3  /  Hull +2  /  Score +4", "The slower route preserved spacecraft stability but added travel time.", -3, -1, 2, 1, 4, 2),
                new MissionOption("SKIP THE CORRECTION", "Power +4  /  Link -8  /  Hull -12", "The spacecraft saved energy, but the uncorrected approach increased risk.", 4, -8, -12, 0, -10, 0)));
            flightEvents.Add(new MissionEvent("LUNAR DUST PLUME",
                "A dust plume is approaching the landing area. Protect the lander or keep collecting data?",
                new MissionOption("PAUSE AND SECURE THE LANDER", "Power -5  /  Hull +14  /  Science +5", "The lander entered a safe configuration and avoided most dust exposure.", -5, 0, 14, 5, 8, 1),
                new MissionOption("ACTIVATE DUST SHIELDING", "Power -12  /  Hull +20  /  Score +10", "Active shielding protected exposed systems at a significant power cost.", -12, 0, 20, 2, 10, 1),
                new MissionOption("CONTINUE FULL OPERATIONS", "Power -4  /  Hull -24  /  Science +13", "The instruments gathered valuable readings, but dust damaged exposed components.", -4, -2, -24, 13, -6, 0)));
            flightEvents.Add(new MissionEvent("EARTH-TO-MOON COMMUNICATION WINDOW",
                "The ground station reports a short communication window. Decide how to use it.",
                new MissionOption("REORIENT HIGH-GAIN ANTENNA", "Power -10  /  Link +22  /  Science +2", "The antenna locked onto the ground station and restored a strong data link.", -10, 22, 0, 2, 7, 1),
                new MissionOption("COMPRESS AND PRIORITIZE DATA", "Power -4  /  Link +10  /  Science +6", "Compressed telemetry preserved the most valuable science packets.", -4, 10, 0, 6, 6, 1),
                new MissionOption("WAIT FOR THE NEXT WINDOW", "Power +4  /  Link -8  /  +2 days", "The spacecraft conserved power but missed the current downlink opportunity.", 4, -8, 0, 0, -5, 2)));
        }
        else if (selectedMission == "EARTH")
        {
            flightEvents.Add(new MissionEvent("CLOUD COVER DETECTED",
                "Cloud cover is reducing visibility over the observation region. Choose an imaging strategy.",
                new MissionOption("SHIFT TO A CLEAR ORBITAL PASS", "Power -10  /  Link +4  /  Science +8", "The spacecraft adjusted its observation pass and found a clearer imaging window.", -10, 4, 0, 8, 9, 1),
                new MissionOption("WAIT FOR A CLOUD GAP", "Power -3  /  Science +5  /  +2 days", "Waiting improved the chance of a clean observation but extended the mission.", -3, 0, 0, 5, 5, 2),
                new MissionOption("CAPTURE A PARTIAL PASS", "Power -2  /  Science +3  /  Score -2", "Partial imagery was retained, though some target areas remained obscured.", -2, 0, 0, 3, -2, 0)));
            flightEvents.Add(new MissionEvent("ORBITAL DRIFT WARNING",
                "The orbit has drifted away from the planned observation corridor.",
                new MissionOption("RUN AN ORBITAL CORRECTION", "Power -10  /  Link +3  /  Score +8", "A correction maneuver returned the spacecraft to its planned observation corridor.", -10, 3, 0, 4, 8, 1),
                new MissionOption("STABILIZE WITH A SMALL BURN", "Power -5  /  Hull +4  /  Score +5", "A small maneuver reduced the drift while preserving most resources.", -5, 0, 4, 3, 5, 1),
                new MissionOption("IGNORE THE DRIFT", "Power +4  /  Hull -15  /  Science -2", "The orbit continued to drift, reducing the quality of later observations.", 4, -5, -15, 0, -10, 0)));
            flightEvents.Add(new MissionEvent("DOWNLINK CONGESTION",
                "Several data packets are waiting for transmission. Select a communication policy.",
                new MissionOption("PRIORITIZE SCIENCE PACKETS", "Power -10  /  Link +15  /  Science +10", "The most valuable science packets reached the ground station first.", -10, 15, 0, 10, 7, 1),
                new MissionOption("COMPRESS THE DATA STREAM", "Power -5  /  Link +8  /  Science +6", "Compression reduced the transmission load and preserved key observations.", -5, 8, 0, 6, 5, 1),
                new MissionOption("CACHE DATA FOR LATER", "Power +2  /  Link -10  /  +1 day", "The spacecraft stored the packets onboard and waited for another pass.", 2, -10, 0, 0, -2, 1)));
        }
        else // MARS
        {
            flightEvents.Add(new MissionEvent("DEEP-SPACE SIGNAL DELAY",
                "The long-distance link is unstable. Choose a strategy for maintaining contact.",
                new MissionOption("FOCUS THE HIGH-GAIN ANTENNA", "Power -8  /  Link +18  /  Score +8", "A narrow-beam link improved the connection to the ground station.", -8, 18, 0, 2, 8, 1),
                new MissionOption("USE LOW-RATE TELEMETRY", "Power -3  /  Link +10  /  Science +3", "A lower transmission rate kept essential spacecraft telemetry flowing.", -3, 10, 0, 3, 6, 1),
                new MissionOption("CONSERVE POWER", "Power +5  /  Link -18  /  Score -8", "The spacecraft conserved energy, but ground contact became intermittent.", 5, -18, 0, 0, -8, 1)));
            flightEvents.Add(new MissionEvent("MARTIAN DUST STORM",
                "A dust storm is approaching the exploration site. Protect the spacecraft or continue science operations?",
                new MissionOption("ENTER STORM-SAFE MODE", "Power -5  /  Hull +18  /  Science +2", "Safe mode protected sensitive equipment until conditions improved.", -5, -2, 18, 2, 8, 2),
                new MissionOption("REPOSITION BEHIND TERRAIN", "Power -10  /  Hull +12  /  Score +8", "The spacecraft used terrain as a partial shield from abrasive dust.", -10, 0, 12, 4, 8, 1),
                new MissionOption("CONTINUE FULL SCIENCE OPERATIONS", "Power -5  /  Hull -22  /  Science +12", "The instruments collected extra readings, but dust damaged exposed systems.", -5, -3, -22, 12, -7, 0)));
            flightEvents.Add(new MissionEvent("RADIATION ALERT",
                "Elevated radiation has been detected. Choose how to protect onboard systems.",
                new MissionOption("ENTER PROTECTED CONFIGURATION", "Power +3  /  Link -3  /  Hull +15", "Protected configuration reduced exposure while temporarily limiting operations.", 3, -3, 15, 1, 7, 2),
                new MissionOption("ORIENT THE SPACECRAFT FOR SHIELDING", "Power -12  /  Link +4  /  Hull +10", "The spacecraft reoriented to place protected structure toward the radiation source.", -12, 4, 10, 3, 9, 1),
                new MissionOption("IGNORE THE ALERT", "Power +5  /  Hull -30  /  Score -14", "Operations continued, but radiation exposure degraded spacecraft systems.", 5, 0, -30, 0, -14, 0)));
        }

        flightEvents.Add(BuildScienceEvent());
    }

    private MissionEvent BuildScienceEvent()
    {
        string target = selectedMission == "MOON" ? "LUNAR MINERAL SURVEY" :
                        selectedMission == "EARTH" ? "CLIMATE OBSERVATION PASS" : "MARTIAN SITE ANALYSIS";
        bool camera = HasSelected("INSTRUMENT") && equipment.Exists(e => e.title == "SCIENCE CAMERA" && e.selected);
        bool spectrometer = equipment.Exists(e => e.title == "SPECTROMETER" && e.selected);
        string primary = spectrometer ? "SPECTROMETER" : "SCIENCE CAMERA";
        string focusedTitle = spectrometer ? "RUN COMPOSITION SCAN" : "CAPTURE HIGH-RES IMAGES";
        string focusedDetail = primary + "  /  Power -10  /  Science +20";
        string focusedOutcome = spectrometer
            ? "The spectrometer recorded a detailed composition profile for the selected target."
            : "The science camera captured a high-resolution image set of the target region.";
        int focusData = spectrometer ? 20 : 17;
        int focusPower = spectrometer ? -12 : -9;
        string combinedTitle = camera && spectrometer ? "COMBINE BOTH INSTRUMENTS" : "RUN A TARGETED FOLLOW-UP";
        string combinedDetail = camera && spectrometer ? "Power -16  /  Science +28  /  Score +14" : "Power -5  /  Science +11  /  Score +7";
        string combinedOutcome = camera && spectrometer
            ? "Imaging and composition readings were combined into a stronger science result."
            : "A shorter follow-up observation confirmed part of the primary finding.";
        int combinedPower = camera && spectrometer ? -16 : -5;
        int combinedData = camera && spectrometer ? 28 : 11;
        int combinedScore = camera && spectrometer ? 14 : 7;

        return new MissionEvent(target,
            "The spacecraft has reached the science target. Decide how to spend the remaining observation window.",
            new MissionOption(focusedTitle, focusedDetail, focusedOutcome, focusPower, -2, 0, focusData, 11, 1),
            new MissionOption(combinedTitle, combinedDetail, combinedOutcome, combinedPower, -3, 0, combinedData, combinedScore, 2),
            new MissionOption("RUN A LOW-POWER SURVEY", "Power -2  /  Science +7  /  Score +4", "A shorter survey preserved resources while collecting a useful baseline data set.", -2, 0, 0, 7, 4, 1));
    }

    private void SetMissionCameraView(bool targetView)
    {
        if (missionSceneCamera == null) return;
        if (targetView && missionPlanetTransform != null)
        {
            Vector3 target = missionPlanetTransform.position;
            missionSceneCamera.orthographicSize = 11.0f;
            missionSceneCamera.transform.position = target + new Vector3(-3f, 0.3f, -42f);
            missionSceneCamera.transform.LookAt(target);
        }
        else if (missionSpacecraftPivot != null)
        {
            Vector3 target = missionSpacecraftPivot.position;
            missionSceneCamera.orthographicSize = 9.2f;
            missionSceneCamera.transform.position = target + new Vector3(-1.5f, -0.8f, -42f);
            missionSceneCamera.transform.LookAt(target);
        }
        PlaySfx(uiClickClip, 0.22f);
    }

    private void ResetFlightCommands()
    {
        flightCommandPoints = 2;
        usedCameraCommand = false;
        usedSpectrometerCommand = false;
        usedTransmitCommand = false;
        usedPowerSaveCommand = false;
    }

    private bool HasEquipmentSelected(string exactTitle)
    {
        return equipment.Exists(item => item.selected && item.title == exactTitle);
    }

    // Player-issued commands spend limited action points and alter mission resources.
    // All numbers are simplified gameplay values, not physical spacecraft telemetry.
    private void UseFlightCommand(int commandIndex)
    {
        if (flightCommandPoints <= 0) return;
        string commandName = "";
        string result = "";
        switch (commandIndex)
        {
            case 0:
                if (!HasEquipmentSelected("SCIENCE CAMERA") || usedCameraCommand || flightPower < 6) return;
                flightPower = Mathf.Max(0, flightPower - 6);
                flightDataPoints += 6;
                flightScore += 3;
                usedCameraCommand = true;
                commandName = "CAMERA SCAN";
                result = "Camera sweep captured six science data units from the target region.";
                break;
            case 1:
                if (!HasEquipmentSelected("SPECTROMETER") || usedSpectrometerCommand || flightPower < 9) return;
                flightPower = Mathf.Max(0, flightPower - 9);
                flightDataPoints += 10;
                flightScore += 5;
                usedSpectrometerCommand = true;
                commandName = "SPECTRUM ANALYSIS";
                result = "Spectrometer completed a composition scan and added ten science data units.";
                break;
            case 2:
                if (!HasSelected("COMMUNICATION") || flightDataPoints <= 0 || usedTransmitCommand || flightPower < 3) return;
                flightPower = Mathf.Max(0, flightPower - 3);
                flightLink = Mathf.Clamp(flightLink + 12, 0, 100);
                transmittedDataPoints += Mathf.Max(0, flightDataPoints - transmittedDataPoints);
                flightScore += 3;
                usedTransmitCommand = true;
                commandName = "TRANSMIT SCIENCE DATA";
                result = "Collected science packets were transmitted to the ground station.";
                break;
            case 3:
                if (usedPowerSaveCommand) return;
                flightPower = Mathf.Clamp(flightPower + 8, 0, 100);
                flightLink = Mathf.Max(0, flightLink - 4);
                flightScore += 1;
                usedPowerSaveCommand = true;
                commandName = "POWER SAVE MODE";
                result = "Non-essential systems entered low-power mode. Energy reserves improved while the data link paused briefly.";
                break;
            default:
                return;
        }

        flightCommandPoints--;
        lastFlightOutcome = result;
        flightLog.Add("FLIGHT COMMAND — " + commandName + ": " + result);
        PlaySfx(radioClip, 0.48f);
        ShowScreen(GameScreen.MissionControl);
    }

    private void CreateMissionControlBackdrop(Transform parent)
    {
        missionSceneRoot = new GameObject("MISSION_CONTROL_3D_ENVIRONMENT_" + selectedMission);
        missionSceneTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        missionSceneTexture.name = "MissionControl_" + selectedMission + "_RenderTexture";
        missionSceneTexture.antiAliasing = 2;
        missionSceneTexture.Create();

        GameObject cameraObject = new GameObject("MissionEnvironmentCamera", typeof(Camera));
        cameraObject.transform.SetParent(missionSceneRoot.transform, false);
        missionSceneCamera = cameraObject.GetComponent<Camera>();
        missionSceneCamera.targetTexture = missionSceneTexture;
        missionSceneCamera.clearFlags = CameraClearFlags.SolidColor;
        missionSceneCamera.backgroundColor = new Color32(2, 7, 18, 255);
        missionSceneCamera.orthographic = true;
        missionSceneCamera.orthographicSize = 12.8f;
        missionSceneCamera.nearClipPlane = 0.1f;
        missionSceneCamera.farClipPlane = 220f;
        missionSceneCamera.transform.position = new Vector3(0f, 0f, -42f);
        missionSceneCamera.transform.LookAt(Vector3.zero);

        Light sun = new GameObject("MissionSunLight", typeof(Light)).GetComponent<Light>();
        sun.transform.SetParent(missionSceneRoot.transform, false);
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.color = new Color(0.78f, 0.88f, 1f);
        sun.transform.rotation = Quaternion.Euler(28f, -42f, 0f);

        Material starMaterial = MakeMaterial(new Color32(177, 218, 255, 255), 0f, 2.0f);
        System.Random random = new System.Random(selectedMission == "MOON" ? 71 : selectedMission == "EARTH" ? 89 : 113);
        for (int i = 0; i < 160; i++)
        {
            Vector3 starPos = new Vector3((float)random.NextDouble() * 58f - 29f,
                (float)random.NextDouble() * 30f - 15f, 23f + (float)random.NextDouble() * 32f);
            float starSize = 0.035f + (float)random.NextDouble() * 0.075f;
            CreatePrimitiveObject("MissionStar_" + i, PrimitiveType.Sphere, missionSceneRoot.transform,
                starPos, Vector3.one * starSize, starMaterial);
        }

        // Distant secondary body gives every destination a recognizable environment.
        if (selectedMission == "MOON")
        {
            GameObject earth = CreatePrimitiveObject("DistantEarth", PrimitiveType.Sphere, missionSceneRoot.transform,
                new Vector3(-18f, 8.5f, 18f), new Vector3(3.8f, 3.8f, 3.8f), MakeMaterial(new Color32(28, 102, 174, 255), 0.1f, 0.35f));
            earth.transform.Rotate(18f, 25f, 0f);
        }
        else if (selectedMission == "MARS")
        {
            CreatePrimitiveObject("DistantMarsMoon", PrimitiveType.Sphere, missionSceneRoot.transform,
                new Vector3(-18f, 8f, 18f), new Vector3(2.1f, 2.1f, 2.1f), MakeMaterial(new Color32(145, 133, 123, 255), 0.05f, 0.04f));
        }
        else
        {
            CreatePrimitiveObject("DistantMoon", PrimitiveType.Sphere, missionSceneRoot.transform,
                new Vector3(-18f, 8f, 18f), new Vector3(2.8f, 2.8f, 2.8f), MakeMaterial(new Color32(149, 157, 169, 255), 0.1f, 0.03f));
        }

        float planetRadius = selectedMission == "EARTH" ? 11.5f : selectedMission == "MARS" ? 10.8f : 10.2f;
        Texture2D surfaceTexture;
        if (!missionPlanetTextureCache.TryGetValue(selectedMission, out surfaceTexture) || surfaceTexture == null)
        {
            surfaceTexture = BuildMissionPlanetTexture(selectedMission, 512, 256);
            missionPlanetTextureCache[selectedMission] = surfaceTexture;
        }
        Material planetMaterial = MakeMaterial(Color.white, 0.08f, selectedMission == "MARS" ? 0.02f : 0.06f);
        if (planetMaterial.HasProperty("_BaseMap")) planetMaterial.SetTexture("_BaseMap", surfaceTexture);
        if (planetMaterial.HasProperty("_MainTex")) planetMaterial.SetTexture("_MainTex", surfaceTexture);
        missionPlanetTransform = CreatePrimitiveObject(selectedMission + "_Mission_World", PrimitiveType.Sphere,
            missionSceneRoot.transform, new Vector3(15.6f, 1.4f, 9f),
            Vector3.one * (planetRadius * 2f), planetMaterial).transform;
        missionPlanetTransform.localRotation = Quaternion.Euler(-12f, 0f, 8f);

        // Animated science spacecraft in the foreground; its visible systems reflect the chosen loadout.
        missionSpacecraftPivot = new GameObject("MissionControl_Spacecraft_Model").transform;
        missionSpacecraftPivot.SetParent(missionSceneRoot.transform, false);
        float progress = flightEvents.Count > 0 ? Mathf.Clamp01((float)flightEventIndex / Mathf.Max(1, flightEvents.Count - 1)) : 0f;
        missionSpacecraftBasePosition = Vector3.Lerp(new Vector3(-9.0f, -2.1f, 1.4f), new Vector3(-4.5f, 0.1f, 1.4f), progress);
        missionSpacecraftPivot.localPosition = missionSpacecraftBasePosition;
        missionSpacecraftPivot.localRotation = Quaternion.Euler(20f, -18f, 13f);

        Material spacecraftMetal = MakeMaterial(new Color32(191, 211, 224, 255), 0.62f, 0.04f);
        Material spacecraftDark = MakeMaterial(new Color32(31, 49, 67, 255), 0.72f, 0.04f);
        Material solarBlue = MakeMaterial(new Color32(24, 92, 155, 255), 0.48f, 0.18f);
        Material goldMetal = MakeMaterial(new Color32(218, 156, 70, 255), 0.42f, 0.08f);
        Material cyanGlow = MakeMaterial(new Color32(34, 193, 245, 255), 0.12f, 1.0f);
        CreatePrimitiveObject("SpacecraftMainBus", PrimitiveType.Cube, missionSpacecraftPivot,
            Vector3.zero, new Vector3(2.35f, 1.28f, 1.15f), spacecraftMetal);
        CreatePrimitiveObject("SpacecraftForwardModule", PrimitiveType.Cylinder, missionSpacecraftPivot,
            new Vector3(1.28f, 0f, 0f), new Vector3(0.72f, 1.0f, 0.72f), spacecraftDark).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        CreatePrimitiveObject("SpacecraftRearEngine", PrimitiveType.Cylinder, missionSpacecraftPivot,
            new Vector3(-1.38f, 0f, 0f), new Vector3(0.56f, 0.74f, 0.56f), goldMetal).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        CreatePrimitiveObject("SpacecraftEngineGlow", PrimitiveType.Sphere, missionSpacecraftPivot,
            new Vector3(-1.85f, 0f, 0f), new Vector3(0.26f, 0.38f, 0.38f), cyanGlow);
        if (HasSelected("POWER"))
        {
            CreatePrimitiveObject("SpacecraftSolarWingLeft", PrimitiveType.Cube, missionSpacecraftPivot,
                new Vector3(0f, 2.05f, 0f), new Vector3(3.4f, 0.10f, 1.1f), solarBlue);
            CreatePrimitiveObject("SpacecraftSolarWingRight", PrimitiveType.Cube, missionSpacecraftPivot,
                new Vector3(0f, -2.05f, 0f), new Vector3(3.4f, 0.10f, 1.1f), solarBlue);
            for (int i = -4; i <= 4; i++)
            {
                CreateBeam(missionSpacecraftPivot, new Vector3(i * 0.35f, 2.0f, -0.52f), new Vector3(i * 0.35f, 2.0f, 0.52f), 0.025f, cyanGlow);
                CreateBeam(missionSpacecraftPivot, new Vector3(i * 0.35f, -2.0f, -0.52f), new Vector3(i * 0.35f, -2.0f, 0.52f), 0.025f, cyanGlow);
            }
        }
        if (HasSelected("COMMUNICATION"))
        {
            CreatePrimitiveObject("SpacecraftAntennaMast", PrimitiveType.Cylinder, missionSpacecraftPivot,
                new Vector3(0.15f, 1.0f, 0.2f), new Vector3(0.07f, 1.15f, 0.07f), goldMetal);
            CreatePrimitiveObject("SpacecraftAntennaDish", PrimitiveType.Sphere, missionSpacecraftPivot,
                new Vector3(0.15f, 1.72f, 0.2f), new Vector3(0.95f, 0.22f, 0.95f), spacecraftMetal);
        }
        if (HasEquipmentSelected("LUNAR LANDER"))
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 foot = new Vector3(Mathf.Cos(angle) * 1.45f, -1.15f, Mathf.Sin(angle) * 0.9f);
                CreateBeam(missionSpacecraftPivot, new Vector3(0f, -0.5f, 0f), foot, 0.07f, goldMetal);
                CreatePrimitiveObject("SpacecraftLanderFoot", PrimitiveType.Cube, missionSpacecraftPivot,
                    foot, new Vector3(0.5f, 0.09f, 0.38f), spacecraftDark);
            }
        }
        if (HasSelected("INSTRUMENT"))
        {
            CreatePrimitiveObject("ScienceInstrumentHousing", PrimitiveType.Cube, missionSpacecraftPivot,
                new Vector3(0.65f, -0.77f, 0.55f), new Vector3(0.58f, 0.42f, 0.48f), spacecraftDark);
            CreatePrimitiveObject("ScienceInstrumentLens", PrimitiveType.Sphere, missionSpacecraftPivot,
                new Vector3(0.65f, -0.78f, 0.83f), new Vector3(0.22f, 0.22f, 0.07f), cyanGlow);
        }

        // A cyan orbital arc ties the craft to its destination.
        GameObject orbitPath = new GameObject("MissionTrajectoryPath", typeof(LineRenderer));
        orbitPath.transform.SetParent(missionSceneRoot.transform, false);
        LineRenderer line = orbitPath.GetComponent<LineRenderer>();
        line.positionCount = 40;
        line.useWorldSpace = true;
        line.startWidth = 0.055f;
        line.endWidth = 0.025f;
        Material pathMaterial = MakeMaterial(new Color32(44, 190, 245, 255), 0f, 1.6f);
        line.material = pathMaterial;
        for (int i = 0; i < 40; i++)
        {
            float t = i / 39f;
            Vector3 point = Vector3.Lerp(missionSpacecraftBasePosition + new Vector3(2f, -1.1f, 0f),
                new Vector3(7.0f, 2.2f, 8f), t);
            point.y += Mathf.Sin(t * Mathf.PI) * 4.4f;
            line.SetPosition(i, point);
        }

        RawImage backgroundFeed = new GameObject("MissionEnvironmentBackground", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        backgroundFeed.transform.SetParent(parent, false);
        backgroundFeed.texture = missionSceneTexture;
        backgroundFeed.color = Color.white;
        backgroundFeed.raycastTarget = false;
        RectTransform backgroundRect = backgroundFeed.rectTransform;
        backgroundRect.anchorMin = new Vector2(0f, 0.045f);
        backgroundRect.anchorMax = new Vector2(1f, 0.915f);
        backgroundRect.offsetMin = backgroundRect.offsetMax = Vector2.zero;
        backgroundFeed.transform.SetAsFirstSibling();
        Image scrim = CreateImage("MissionEnvironmentScrim", parent, new Color(0.005f, 0.015f, 0.035f, 0.28f));
        scrim.raycastTarget = false;
        RectTransform scrimRect = scrim.rectTransform;
        scrimRect.anchorMin = new Vector2(0f, 0.045f);
        scrimRect.anchorMax = new Vector2(1f, 0.915f);
        scrimRect.offsetMin = scrimRect.offsetMax = Vector2.zero;
    }

    private Texture2D BuildMissionPlanetTexture(string mission, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.name = mission + "_ProceduralSurface";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        System.Random random = new System.Random(mission == "MOON" ? 23 : mission == "EARTH" ? 43 : 67);
        Vector2[] craterCenters = new Vector2[34];
        float[] craterRadii = new float[34];
        for (int i = 0; i < craterCenters.Length; i++)
        {
            craterCenters[i] = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
            craterRadii[i] = 0.012f + (float)random.NextDouble() * 0.035f;
        }

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)height;
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)width;
                float n1 = Mathf.PerlinNoise(u * 9.5f + 3.2f, v * 7.1f + 1.7f);
                float n2 = Mathf.PerlinNoise(u * 31f + 8.1f, v * 24f + 2.4f);
                Color c;
                if (mission == "EARTH")
                {
                    float land = Mathf.PerlinNoise(u * 5.2f + 8.4f, v * 4.0f + 1.1f);
                    c = new Color(0.025f + n2 * 0.025f, 0.12f + n1 * 0.10f, 0.30f + n2 * 0.22f);
                    if (land > 0.56f)
                    {
                        float terrain = Mathf.PerlinNoise(u * 18f + 5f, v * 15f + 9f);
                        c = Color.Lerp(new Color(0.12f, 0.28f, 0.17f), new Color(0.53f, 0.46f, 0.27f), terrain);
                    }
                    float clouds = Mathf.PerlinNoise(u * 42f + 2f, v * 30f + 6f);
                    if (clouds > 0.68f) c = Color.Lerp(c, new Color(0.82f, 0.9f, 0.96f), (clouds - 0.68f) * 2.3f);
                }
                else if (mission == "MARS")
                {
                    float terrain = Mathf.PerlinNoise(u * 7f + 4f, v * 5f + 12f);
                    c = Color.Lerp(new Color(0.20f, 0.055f, 0.035f), new Color(0.78f, 0.30f, 0.13f), terrain);
                    if (n2 > 0.77f) c = Color.Lerp(c, new Color(0.88f, 0.53f, 0.28f), 0.32f);
                    float pole = Mathf.Max(0f, 1f - Mathf.Abs(v - 0.5f) * 8f);
                    if ((v < 0.055f || v > 0.945f) && n1 > 0.35f) c = Color.Lerp(c, new Color(0.83f, 0.72f, 0.62f), pole * 0.7f);
                }
                else
                {
                    float shade = 0.58f + n1 * 0.28f + n2 * 0.12f;
                    c = new Color(shade, shade * 0.99f, shade * 0.95f);
                    for (int crater = 0; crater < craterCenters.Length; crater++)
                    {
                        float dx = Mathf.Abs(u - craterCenters[crater].x);
                        dx = Mathf.Min(dx, 1f - dx);
                        float dy = v - craterCenters[crater].y;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float radius = craterRadii[crater];
                        if (distance < radius)
                        {
                            float rim = Mathf.InverseLerp(radius, radius * 0.72f, distance);
                            c *= 0.64f + rim * 0.18f;
                        }
                        else if (distance < radius * 1.12f)
                        {
                            c = Color.Lerp(c, new Color(0.82f, 0.81f, 0.77f), 0.18f);
                        }
                    }
                }
                float latitudeShade = 0.78f + 0.22f * Mathf.Sin(v * Mathf.PI);
                pixels[y * width + x] = c * latitudeShade;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        return texture;
    }

    private void BuildMissionControl()
    {
        if (flightEvents == null || flightEvents.Count == 0)
        {
            missionReady = false;
            lastValidation = "The flight event list is empty. Return to assembly and run the pre-launch check again.";
            ShowScreen(GameScreen.Launch);
            return;
        }

        MissionEvent activeEvent = flightEvents[Mathf.Clamp(flightEventIndex, 0, flightEvents.Count - 1)];
        CreateMissionControlBackdrop(currentPage.transform);
        ScreenTitle("MISSION CONTROL", selectedMission + " FLIGHT OPERATIONS  /  EVENT " + (flightEventIndex + 1) + " OF " + flightEvents.Count);
        if (gameAudioSource != null && spaceAmbienceClip != null &&
            (gameAudioSource.clip != spaceAmbienceClip || !gameAudioSource.isPlaying))
            StartAudioLoop(spaceAmbienceClip, 0.24f);
        PlaySfx(radioClip, 0.20f);

        BuildFlightMetric("MISSION DAY", "DAY " + flightDay, Mathf.Clamp01(flightDay / 18f), new Vector2(0.12f, 0.635f), cyan);
        BuildFlightMetric("POWER RESERVE", flightPower + "%", flightPower / 100f, new Vector2(0.31f, 0.635f), flightPower < 25 ? red : gold);
        BuildFlightMetric("DATA LINK", flightLink + "%", flightLink / 100f, new Vector2(0.50f, 0.635f), flightLink < 25 ? red : cyan);
        BuildFlightMetric("HULL STATUS", flightIntegrity + "%", flightIntegrity / 100f, new Vector2(0.69f, 0.635f), flightIntegrity < 35 ? red : green);
        BuildFlightMetric("SCIENCE DATA", (flightDataPoints * 0.25f).ToString("N1") + " GB", Mathf.Clamp01(flightDataPoints / 80f), new Vector2(0.88f, 0.635f), gold);

        GameObject overview = Card(currentPage.transform, new Vector2(0.035f, 0.12f), new Vector2(0.465f, 0.55f), new Color32(5, 18, 34, 194));
        //AddAccent(overview.transform, cyan);
        Label("LIVE SPACECRAFT VIEW  /  " + selectedMission + " ENVIRONMENT", overview.transform, 16, cyan,
            TextAlignmentOptions.Left, new Vector2(520, 28), new Vector2(0.06f, 0.93f), Vector2.zero, true);

        Button("SHIP VIEW", overview.transform, new Vector2(0.77f, 0.82f), new Vector2(118, 30),
            new Color32(7, 32, 52, 240), () => SetMissionCameraView(false));
        Button("TARGET VIEW", overview.transform, new Vector2(0.91f, 0.82f), new Vector2(132, 30),
            new Color32(7, 32, 52, 240), () => SetMissionCameraView(true));

        Label("DESTINATION  /  " + selectedMission + "     •     PHASE " + (flightEventIndex + 1) + " / " + flightEvents.Count,
            overview.transform, 12, gold, TextAlignmentOptions.Center,
            new Vector2(700, 22), new Vector2(0.50f, 0.425f), Vector2.zero, true);
        Image routeTrack = CreateImage("FlightRouteTrack", overview.transform, new Color32(38, 71, 95, 255));
        RectTransform routeTrackRect = routeTrack.rectTransform;
        routeTrackRect.anchorMin = new Vector2(0.12f, 0.375f);
        routeTrackRect.anchorMax = new Vector2(0.88f, 0.389f);
        routeTrackRect.offsetMin = routeTrackRect.offsetMax = Vector2.zero;
        float progress = flightEvents.Count > 0 ? Mathf.Clamp01((float)flightEventIndex / flightEvents.Count) : 0f;
        Image routeProgress = CreateImage("FlightRouteProgress", overview.transform, cyan);
        RectTransform routeProgressRect = routeProgress.rectTransform;
        routeProgressRect.anchorMin = new Vector2(0.12f, 0.375f);
        routeProgressRect.anchorMax = new Vector2(0.12f + 0.76f * progress, 0.389f);
        routeProgressRect.offsetMin = routeProgressRect.offsetMax = Vector2.zero;
        for (int nodeIndex = 0; nodeIndex < flightEvents.Count; nodeIndex++)
        {
            float nodeX = 0.12f + 0.76f * nodeIndex / Mathf.Max(1f, flightEvents.Count - 1f);
            Image node = CreateImage("FlightRouteNode_" + nodeIndex, overview.transform,
                nodeIndex < flightEventIndex ? green : (nodeIndex == flightEventIndex ? cyan : new Color32(53, 85, 108, 255)));
            RectTransform nodeRect = node.rectTransform;
            nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(nodeX, 0.382f);
            nodeRect.sizeDelta = new Vector2(10, 10);
            nodeRect.anchoredPosition = Vector2.zero;
        }
        Label(lastFlightOutcome, overview.transform, 10, new Color32(190, 211, 229, 255),
            TextAlignmentOptions.Center, new Vector2(760, 24), new Vector2(0.5f, 0.325f), Vector2.zero, false);
        Label("FLIGHT COMMAND DECK     /     " + flightCommandPoints + " ACTION POINTS AVAILABLE",
            overview.transform, 11, cyan, TextAlignmentOptions.Left,
            new Vector2(700, 20), new Vector2(0.06f, 0.245f), Vector2.zero, true);

        bool cameraInstalled = HasEquipmentSelected("SCIENCE CAMERA");
        bool spectrumInstalled = HasEquipmentSelected("SPECTROMETER");
        bool linkInstalled = HasSelected("COMMUNICATION");
        bool cameraReady = cameraInstalled && !usedCameraCommand && flightPower >= 6;
        bool spectrumReady = spectrumInstalled && !usedSpectrometerCommand && flightPower >= 9;
        bool transmitReady = linkInstalled && flightDataPoints > transmittedDataPoints && !usedTransmitCommand && flightPower >= 3;
        bool powerSaveReady = !usedPowerSaveCommand;
        bool hasActionPoints = flightCommandPoints > 0;
        string cameraCommandLabel = !cameraInstalled ? "CAMERA SCAN  /  NOT INSTALLED" : usedCameraCommand ? "CAMERA SCAN  /  COMPLETE" : flightPower < 6 ? "CAMERA SCAN  /  LOW POWER" : "CAMERA SCAN  /  -6 P  +6 DATA";
        string spectrumCommandLabel = !spectrumInstalled ? "SPECTRUM SCAN  /  NOT INSTALLED" : usedSpectrometerCommand ? "SPECTRUM SCAN  /  COMPLETE" : flightPower < 9 ? "SPECTRUM SCAN  /  LOW POWER" : "SPECTRUM SCAN  /  -9 P  +10 DATA";
        string transmitCommandLabel = !linkInstalled ? "TRANSMIT DATA  /  NO ANTENNA" : flightDataPoints <= transmittedDataPoints ? "TRANSMIT DATA  /  NO NEW PACKETS" : usedTransmitCommand ? "TRANSMIT DATA  /  COMPLETE" : flightPower < 3 ? "TRANSMIT DATA  /  LOW POWER" : "TRANSMIT DATA  /  -3 P  +12 LINK";
        string powerSaveCommandLabel = usedPowerSaveCommand ? "POWER SAVE  /  ALREADY ACTIVE" : "POWER SAVE  /  +8 P  -4 LINK";
        Button(cameraCommandLabel, overview.transform, new Vector2(0.27f, 0.155f), new Vector2(330, 38),
            cardAltColor, () => UseFlightCommand(0), hasActionPoints && cameraReady);
        Button(spectrumCommandLabel, overview.transform, new Vector2(0.73f, 0.155f), new Vector2(330, 38),
            cardAltColor, () => UseFlightCommand(1), hasActionPoints && spectrumReady);
        Button(transmitCommandLabel, overview.transform, new Vector2(0.27f, 0.065f), new Vector2(330, 38),
            cardAltColor, () => UseFlightCommand(2), hasActionPoints && transmitReady);
        Button(powerSaveCommandLabel, overview.transform, new Vector2(0.73f, 0.065f), new Vector2(330, 38),
            cardAltColor, () => UseFlightCommand(3), hasActionPoints && powerSaveReady);

        GameObject decision = Card(currentPage.transform, new Vector2(0.49f, 0.12f), new Vector2(0.965f, 0.55f), new Color32(12, 28, 48, 205));
       // AddAccent(decision.transform, gold);
        Label(activeEvent.title, decision.transform, 19, gold, TextAlignmentOptions.Left,
            new Vector2(780, 34), new Vector2(0.06f, 0.90f), Vector2.zero, true);
        Label(activeEvent.prompt, decision.transform, 14, Color.white, TextAlignmentOptions.Left,
            new Vector2(820, 52), new Vector2(0.06f, 0.77f), Vector2.zero, false);

        float[] optionY = { 0.57f, 0.35f, 0.13f };
        for (int i = 0; i < activeEvent.options.Length; i++)
        {
            MissionOption option = activeEvent.options[i];
            int optionIndex = i;
            Button((i + 1) + "  " + option.title + "\n" + option.detail,
                decision.transform, new Vector2(0.50f, optionY[i]), new Vector2(790, 76),
                i == 0 ? cyanDark : cardColor, () => ApplyMissionChoice(optionIndex));
        }
        Label("SELECT ONE COMMAND  /  EACH DECISION CHANGES THE MISSION", decision.transform, 10, muted,
            TextAlignmentOptions.Center, new Vector2(780, 18), new Vector2(0.5f, 0.025f), Vector2.zero, true);
    }

    private void BuildFlightMetric(string label, string value, float fill, Vector2 center, Color tint)
    {
        GameObject card = Card(currentPage.transform,
            new Vector2(center.x - 0.085f, center.y - 0.052f),
            new Vector2(center.x + 0.085f, center.y + 0.052f), new Color32(11, 28, 48, 248));
        Label(label, card.transform, 10, muted, TextAlignmentOptions.Center,
            new Vector2(280, 18), new Vector2(0.5f, 0.78f), Vector2.zero, true);
        Label(value, card.transform, 17, tint, TextAlignmentOptions.Center,
            new Vector2(280, 28), new Vector2(0.5f, 0.43f), Vector2.zero, true);
        Image barBack = CreateImage("MetricBarBackground", card.transform, new Color32(31, 59, 80, 255));
        RectTransform backRect = barBack.rectTransform;
        backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0.15f);
        backRect.sizeDelta = new Vector2(220, 7);
        backRect.anchoredPosition = Vector2.zero;
        Image barFill = CreateImage("MetricBarFill", card.transform, tint);
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.fillAmount = Mathf.Clamp01(fill);
        RectTransform fillRect = barFill.rectTransform;
        fillRect.anchorMin = fillRect.anchorMax = new Vector2(0.5f, 0.15f);
        fillRect.sizeDelta = new Vector2(220, 7);
        fillRect.anchoredPosition = Vector2.zero;
    }

    private void BuildFlightCraftMarker(Transform parent, Vector2 center)
    {
        Image body = CreateImage("FlightCraftBody", parent, new Color32(208, 225, 237, 255));
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = bodyRect.anchorMax = center;
        bodyRect.sizeDelta = new Vector2(48, 18);
        bodyRect.anchoredPosition = Vector2.zero;
        Image wing = CreateImage("FlightCraftSolarWing", parent, cyanDark);
        RectTransform wingRect = wing.rectTransform;
        wingRect.anchorMin = wingRect.anchorMax = new Vector2(center.x, center.y - 0.04f);
        wingRect.sizeDelta = new Vector2(66, 7);
        wingRect.anchoredPosition = Vector2.zero;
        Image engine = CreateImage("FlightCraftEngine", parent, gold);
        RectTransform engineRect = engine.rectTransform;
        engineRect.anchorMin = engineRect.anchorMax = new Vector2(center.x - 0.055f, center.y);
        engineRect.sizeDelta = new Vector2(8, 11);
        engineRect.anchoredPosition = Vector2.zero;
    }

    private void ApplyMissionChoice(int optionIndex)
    {
        if (flightEventIndex < 0 || flightEventIndex >= flightEvents.Count) return;
        MissionEvent activeEvent = flightEvents[flightEventIndex];
        if (optionIndex < 0 || optionIndex >= activeEvent.options.Length) return;

        MissionOption choice = activeEvent.options[optionIndex];
        flightPower = Mathf.Clamp(flightPower + choice.powerDelta, 0, 100);
        flightLink = Mathf.Clamp(flightLink + choice.linkDelta, 0, 100);
        flightIntegrity = Mathf.Clamp(flightIntegrity + choice.integrityDelta, 0, 100);
        flightDataPoints = Mathf.Max(0, flightDataPoints + choice.dataPoints);
        flightScore += choice.scoreDelta;
        flightDay += choice.dayDelta;
        lastFlightOutcome = choice.outcome;
        flightLog.Add(activeEvent.title + " — " + choice.title + ": " + choice.outcome);

        flightEventIndex++;
        if (flightEventIndex >= flightEvents.Count)
        {
            if (selectedMission == "MOON" || selectedMission == "MARS")
            {
                InitializeLunarSurfaceMission();
                ShowScreen(GameScreen.MoonLanding);
            }
            else if (selectedMission == "EARTH")
            {
                InitializeEarthObservation();
                ShowScreen(GameScreen.EarthObservation);
            }
            else
            {
                CompleteMissionSimulation();
                ShowScreen(GameScreen.Results);
            }
        }
        else
        {
            ResetFlightCommands();
            ShowScreen(GameScreen.MissionControl);
        }
    }

    private void InitializeEarthObservation()
    {
        earthImagesCaptured = 0;
        earthTargetsCompleted = 0;
        earthDataTransmitted = 0;
        earthCurrentTargetIndex = -1;
        earthObservationStatus = "Orbital imaging pass acquired. Select a target region to capture.";
    }

    private void BuildEarthObservation()
    {
        // Keep this scene isolated: the mission-control planet render camera would otherwise overlap the orbital imager in world space.
        ScreenTitle("EARTH OBSERVATION OPERATIONS", "LOW EARTH ORBIT  /  OPTICAL IMAGING  /  DATA DOWNLINK  /  GROUND STATION");
        GameObject scene = Card(currentPage.transform, new Vector2(0.035f, 0.16f), new Vector2(0.48f, 0.68f), new Color32(4, 18, 35, 225));
      //  AddAccent(scene.transform, cyan);
        Label("LIVE ORBITAL CAMERA FEED", scene.transform, 16, cyan, TextAlignmentOptions.Left,
            new Vector2(520, 28), new Vector2(.06f,.92f), Vector2.zero, true);
        CreateEarthObservationStage();
        RawImage feed = new GameObject("EarthOrbitalCameraFeed", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        feed.transform.SetParent(scene.transform, false);
        feed.texture = earthObservationTexture;
        feed.color = Color.white;
        feed.raycastTarget = false;
        RectTransform feedRect = feed.rectTransform;
        feedRect.anchorMin = new Vector2(.035f,.17f); feedRect.anchorMax = new Vector2(.965f,.82f);
        feedRect.offsetMin = feedRect.offsetMax = Vector2.zero;
        feed.transform.SetAsFirstSibling();
        string[] earthTargetNames = { "AMAZONAS BASIN", "NORTH ATLANTIC", "CENTRAL SAHARA" };
        string targetLock = earthCurrentTargetIndex >= 0 ? "TARGET LOCK: " + earthTargetNames[earthCurrentTargetIndex] : "WIDE-AREA EARTH OBSERVATION / TARGET ACQUISITION";
        Label(targetLock + "  /  520 KM ORBIT", scene.transform, 10, gold,
            TextAlignmentOptions.Center, new Vector2(630,22), new Vector2(.5f,.105f), Vector2.zero, true);

        GameObject targets = Card(currentPage.transform, new Vector2(.505f,.16f), new Vector2(.965f,.68f), new Color32(9,25,44,240));
      //  AddAccent(targets.transform, gold);
        Label("SCIENCE TARGETS", targets.transform, 17, gold, TextAlignmentOptions.Left,
            new Vector2(600,28), new Vector2(.06f,.92f), Vector2.zero,true);
        string cameraStatus = HasEquipmentSelected("SCIENCE CAMERA") ? "MULTISPECTRAL IMAGER ONLINE" : "IMAGER OFFLINE — SELECT THE MULTISPECTRAL IMAGER IN THE STORE";
        string linkStatus = HasSelected("COMMUNICATION") ? "X-BAND LINK READY" : "X-BAND ANTENNA REQUIRED FOR DOWNLINK";
        Label("Point the orbital imager at each region and capture a science frame. Each exposure consumes 6 power units.\n" + cameraStatus + "  /  " + linkStatus, targets.transform, 11, muted,
            TextAlignmentOptions.Left, new Vector2(700,46), new Vector2(.06f,.78f), Vector2.zero,false);
        string[] names = {
            "AMAZONAS BASIN  /  03.5°S  062°W",
            "NORTH ATLANTIC  /  40°N  030°W",
            "CENTRAL SAHARA  /  23°N  013°E"
        };
        string[] details = {
            "Forest-cover change, vegetation health and canopy conditions",
            "Ocean colour, surface patterns and cloud-free observation window",
            "Desert reflectance, land surface and transported mineral dust"
        };
        for (int i=0;i<3;i++)
        {
            int index=i; float y=.60f-i*.19f;
            bool done = (earthTargetsCompleted & (1<<i)) != 0;
            GameObject row=Card(targets.transform,new Vector2(.035f,y-.075f),new Vector2(.965f,y+.075f),new Color32(15,39,62,255));
            Label((done?"[FRAME STORED] ":"[TARGET " +(i+1)+"] ")+names[i]+"\n"+details[i],row.transform,11,done?green:Color.white,
                TextAlignmentOptions.Left,new Vector2(490,54),new Vector2(.04f,.5f),Vector2.zero,false);
            Button(done?"FRAME CAPTURED":"CAPTURE SCIENCE FRAME",row.transform,new Vector2(.84f,.5f),new Vector2(184,38),done?green:cyan,
                ()=>CaptureEarthTarget(index),!done && HasEquipmentSelected("SCIENCE CAMERA") && flightPower>=6);
        }
        GameObject bottom=Card(currentPage.transform,new Vector2(.035f,.035f),new Vector2(.965f,.135f),new Color32(5,17,32,240));
        Label("FRAMES  "+earthImagesCaptured+" / 3     •     STORED DATA  "+flightDataPoints+"     •     POWER  "+flightPower+"%     •     LINK  "+flightLink+"%",bottom.transform,12,cyan,
            TextAlignmentOptions.Left,new Vector2(800,25),new Vector2(.03f,.68f),Vector2.zero,true);
        Label(earthObservationStatus,bottom.transform,10,muted,TextAlignmentOptions.Left,new Vector2(900,22),new Vector2(.03f,.24f),Vector2.zero,false);
        Button("DOWNLINK FRAMES TO GROUND",bottom.transform,new Vector2(.84f,.5f),new Vector2(260,42),gold,TransmitEarthImages,
            earthImagesCaptured>0 && earthDataTransmitted<earthImagesCaptured && flightPower>=3 && HasSelected("COMMUNICATION"));
        Button("COMPLETE ORBITAL SURVEY",currentPage.transform,new Vector2(.86f,.015f),new Vector2(260,42),green,FinishEarthObservation,
            (earthImagesCaptured>=2 && earthDataTransmitted>=2) || flightPower<=0);
    }

    private void CreateEarthObservationStage()
    {
        earthObservationRoot = new GameObject("EARTH_ORBITAL_IMAGING_SCENE");
        earthObservationTexture = new RenderTexture(900, 560, 24, RenderTextureFormat.ARGB32);
        earthObservationTexture.name = "EarthOrbitalImager_RenderTexture";
        earthObservationTexture.antiAliasing = 4;
        earthObservationTexture.Create();

        GameObject cameraObject = new GameObject("EarthOrbitalImagerCamera", typeof(Camera));
        cameraObject.transform.SetParent(earthObservationRoot.transform, false);
        earthObservationCamera = cameraObject.GetComponent<Camera>();
        earthObservationCamera.targetTexture = earthObservationTexture;
        earthObservationCamera.clearFlags = CameraClearFlags.SolidColor;
        earthObservationCamera.backgroundColor = new Color32(1, 5, 18, 255);
        earthObservationCamera.fieldOfView = earthCurrentTargetIndex >= 0 ? 31f : 40f;
        earthObservationCamera.nearClipPlane = .05f;
        earthObservationCamera.farClipPlane = 100f;
        earthObservationCamera.allowHDR = true;
        earthObservationCamera.allowMSAA = true;
        earthObservationCamera.transform.position = new Vector3(0f, .25f, -8.0f);
        earthObservationCamera.transform.LookAt(Vector3.zero);

        Light sun = new GameObject("OrbitalSunLight", typeof(Light)).GetComponent<Light>();
        sun.transform.SetParent(earthObservationRoot.transform, false);
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.color = new Color(0.82f, 0.90f, 1f);
        sun.transform.rotation = Quaternion.Euler(24f, -36f, 0f);

        earthObservationGlobe = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        earthObservationGlobe.name = "EARTH_TEXTURED_GLOBE";
        earthObservationGlobe.SetParent(earthObservationRoot.transform, false);
        earthObservationGlobe.localScale = Vector3.one * 3.65f;
        Quaternion earthBaseRotation = Quaternion.Euler(8f, 24f, -12f);
        if (earthCurrentTargetIndex >= 0)
        {
            Vector3 targetNormal = GetEarthTargetNormal(earthCurrentTargetIndex);
            Vector3 baseNormal = earthBaseRotation * targetNormal;
            earthObservationGlobe.localRotation = Quaternion.FromToRotation(baseNormal, Vector3.back) * earthBaseRotation;
        }
        else earthObservationGlobe.localRotation = earthBaseRotation;
        Collider globeCollider = earthObservationGlobe.GetComponent<Collider>();
        if (globeCollider != null) Destroy(globeCollider);
        Texture2D earthTexture = Resources.Load<Texture2D>("Environments/Earth_Diffuse");
        if (earthTexture == null)
        {
            earthTexture = BuildEarthObservationTexture(768, 384);
            transientTextures.Add(earthTexture);
        }
        Material earthMaterial = MakeMaterial(Color.white, .08f, .02f);
        if (earthMaterial.HasProperty("_BaseMap")) earthMaterial.SetTexture("_BaseMap", earthTexture);
        if (earthMaterial.HasProperty("_MainTex")) earthMaterial.SetTexture("_MainTex", earthTexture);
        if (earthMaterial.HasProperty("_Smoothness")) earthMaterial.SetFloat("_Smoothness", .24f);
        earthObservationGlobe.GetComponent<Renderer>().sharedMaterial = earthMaterial;

        // Thin transparent shell gives the globe a visible atmospheric limb.
        Material atmosphere = MakeMaterial(new Color32(55,155,235,38), 0f, .22f);
        if (atmosphere.HasProperty("_BaseColor")) atmosphere.SetColor("_BaseColor", new Color(0.22f,0.58f,0.92f,.10f));
        if (atmosphere.HasProperty("_Color")) atmosphere.SetColor("_Color", new Color(0.22f,0.58f,0.92f,.10f));
        if (atmosphere.HasProperty("_Surface")) atmosphere.SetFloat("_Surface", 1f);
        if (atmosphere.HasProperty("_Blend")) atmosphere.SetFloat("_Blend", 0f);
        if (atmosphere.HasProperty("_ZWrite")) atmosphere.SetFloat("_ZWrite", 0f);
        if (atmosphere.HasProperty("_Mode"))
        {
            atmosphere.SetFloat("_Mode", 3f);
            atmosphere.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            atmosphere.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        }
        atmosphere.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        atmosphere.renderQueue = 3000;
        GameObject atmoShell = CreatePrimitiveObject("EARTH_ATMOSPHERE_GLOW", PrimitiveType.Sphere, earthObservationGlobe, Vector3.zero, Vector3.one * 1.045f, atmosphere);
        Renderer atmoRenderer = atmoShell.GetComponent<Renderer>();
        atmoRenderer.shadowCastingMode = ShadowCastingMode.Off;
        atmoRenderer.receiveShadows = false;

        if (earthCurrentTargetIndex >= 0)
        {
            Vector3 targetNormal = GetEarthTargetNormal(earthCurrentTargetIndex);
            Material targetGlow = MakeMaterial(new Color32(255,197,83,255), .08f, 2.2f);
            Transform targetPin = CreatePrimitiveObject("ACTIVE_ORBITAL_TARGET_PIN", PrimitiveType.Sphere,
                earthObservationGlobe, targetNormal * 1.86f, Vector3.one * .055f, targetGlow).transform;
            targetPin.localRotation = Quaternion.FromToRotation(Vector3.up, targetNormal);
            GameObject scanRing = CreateTorusObject("ACTIVE_ORBITAL_TARGET_RETICLE", earthObservationGlobe,
                targetNormal * 1.86f, .18f, .012f, targetGlow);
            scanRing.transform.localRotation = Quaternion.FromToRotation(Vector3.up, targetNormal);
        }

        // Small orbital platform and deployed solar wings establish an actual camera-in-orbit view.
        Material satelliteBody = MakeMaterial(new Color32(182,195,207,255), .65f, .01f);
        Material satelliteDark = MakeMaterial(new Color32(34,52,70,255), .56f, 0f);
        Material solarPanel = MakeMaterial(new Color32(20,83,137,255), .48f, .18f);
        Transform satellite = new GameObject("EARTH_IMAGING_SATELLITE").transform;
        satellite.SetParent(earthObservationRoot.transform, false);
        satellite.localPosition = new Vector3(-2.35f, 1.72f, -.4f);
        satellite.localRotation = Quaternion.Euler(12f, -28f, 18f);
        CreatePrimitiveObject("SatelliteBus", PrimitiveType.Cube, satellite, Vector3.zero, new Vector3(.48f,.35f,.42f), satelliteBody);
        CreatePrimitiveObject("SatelliteOpticalTelescope", PrimitiveType.Cylinder, satellite, new Vector3(0f,-.05f,.34f), new Vector3(.16f,.42f,.16f), satelliteDark).transform.localRotation = Quaternion.Euler(90f,0f,0f);
        CreatePrimitiveObject("SatelliteSolarWingLeft", PrimitiveType.Cube, satellite, new Vector3(-.82f,0f,0f), new Vector3(1.08f,.045f,.48f), solarPanel);
        CreatePrimitiveObject("SatelliteSolarWingRight", PrimitiveType.Cube, satellite, new Vector3(.82f,0f,0f), new Vector3(1.08f,.045f,.48f), solarPanel);
        Material solarCell = MakeMaterial(new Color32(105,183,231,255), .48f, .04f);
        for (int side=-1; side<=1; side+=2)
            for (int i=-2; i<=2; i++)
                CreatePrimitiveObject("SolarCellGrid", PrimitiveType.Cube, satellite,
                    new Vector3(side*.82f, .026f, i*.082f), new Vector3(1.0f,.012f,.012f), solarCell);
        // Orbit path ring, kept behind the spacecraft and planet.
        GameObject orbit = CreateTorusObject("SATELLITE_ORBIT_RING", earthObservationRoot.transform,
            new Vector3(0f,0f,.55f), 2.65f, .012f, MakeMaterial(new Color32(49,177,227,90), .1f, .18f));
        orbit.transform.localRotation = Quaternion.Euler(84f,0f,0f);
    }

    private Vector3 GetEarthTargetNormal(int index)
    {
        float latitude = index == 0 ? -3.5f : (index == 1 ? 40f : 23f);
        float longitude = index == 0 ? -62f : (index == 1 ? -30f : 13f);
        float lat = latitude * Mathf.Deg2Rad;
        float lon = longitude * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon)).normalized;
    }

    private Texture2D BuildEarthObservationTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.name = "Earth_Orbital_Imaging_Albedo";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        Color[] pixels = new Color[width*height];
        for (int y=0; y<height; y++)
        {
            float v = y/(float)(height-1);
            float latitude = (v-.5f)*Mathf.PI;
            for (int x=0; x<width; x++)
            {
                float u = x/(float)(width-1);
                float n1 = Mathf.PerlinNoise(u*8.2f+2.1f, v*5.3f+7.4f);
                float n2 = Mathf.PerlinNoise(u*18.5f+11.7f, v*12.2f+3.9f);
                float n3 = Mathf.PerlinNoise(u*37f+5.2f, v*24f+18.3f);
                float cloud = Mathf.PerlinNoise(u*32f+31f, v*19f+4f);
                float landShape = n1*.62f+n2*.28f+n3*.10f;
                // Broad land masses are deliberately recognizable at the camera's display scale.
                bool land = landShape > .535f && Mathf.Abs(latitude) < 1.39f;
                Color c;
                if (land)
                {
                    float vegetation = Mathf.PerlinNoise(u*13f+8f,v*9f+17f);
                    c = Color.Lerp(new Color(.37f,.29f,.16f),new Color(.20f,.43f,.24f),Mathf.InverseLerp(.40f,.72f,vegetation));
                    if (Mathf.Abs(latitude) > 1.10f) c = Color.Lerp(c,new Color(.75f,.72f,.64f),.58f);
                }
                else
                {
                    float depth = Mathf.PerlinNoise(u*5.5f+4f,v*7f+2f);
                    c = Color.Lerp(new Color(.015f,.10f,.24f),new Color(.025f,.28f,.52f),depth);
                }
                if (cloud > .71f && n2 > .32f)
                    c = Color.Lerp(c,new Color(.88f,.93f,.97f),Mathf.Clamp01((cloud-.71f)*2.5f));
                float polar = Mathf.SmoothStep(.82f,.99f,Mathf.Abs(latitude)/1.5708f);
                c = Color.Lerp(c,new Color(.82f,.88f,.92f),polar*.82f);
                float lighting = .70f + .30f*Mathf.Clamp01(Mathf.Cos((u-.20f)*Mathf.PI*2f)*.35f+.65f);
                pixels[y*width+x] = c*lighting;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true,false);
        return texture;
    }

    private void CaptureEarthTarget(int index)
    {
        if (!HasEquipmentSelected("SCIENCE CAMERA") || flightPower<6 || index<0 || index>2) return;
        int bit=1<<index; if((earthTargetsCompleted&bit)!=0) return;
        earthCurrentTargetIndex = index;
        earthTargetsCompleted|=bit; earthImagesCaptured++; flightPower=Mathf.Clamp(flightPower-6,0,100);
        flightDataPoints+=8; flightScore+=8; earthObservationStatus="CAMERA SHUTTER: "+new[]{"Amazon forest canopy","North Atlantic ocean","Sahara surface reflectance"}[index]+" frame stored onboard. Downlink it to mission control.";
        flightLog.Add("EARTH IMAGING — "+earthObservationStatus); PlaySfx(lunarScanClip,0.7f); ShowScreen(GameScreen.EarthObservation);
    }

    private void TransmitEarthImages()
    {
        if(earthImagesCaptured<=earthDataTransmitted || flightPower<3 || !HasSelected("COMMUNICATION")) return;
        int count=earthImagesCaptured-earthDataTransmitted; flightPower=Mathf.Clamp(flightPower-3,0,100);
        earthDataTransmitted=earthImagesCaptured; transmittedDataPoints+=count*3; flightLink=Mathf.Clamp(flightLink+12,0,100);
        flightScore+=count*5; earthObservationStatus=count+" image packet(s) transmitted to the ground station successfully.";
        flightLog.Add("EARTH DOWNLINK — "+earthObservationStatus); PlaySfx(radioClip,0.75f); ShowScreen(GameScreen.EarthObservation);
    }

    private void FinishEarthObservation()
    {
        missionSuccess=earthImagesCaptured>=2 && earthDataTransmitted>=2 && flightPower>=10 && flightLink>=45;
        missionScore=Mathf.Clamp(flightScore+earthImagesCaptured*6+earthDataTransmitted*5+flightPower/5,0,100);
        if(missionSuccess) missionScore=Mathf.Max(72,missionScore); else missionScore=Mathf.Min(59,missionScore);
        lastFlightOutcome="Earth observation pass complete. "+earthImagesCaptured+" image targets captured; "+earthDataTransmitted+" transmitted.";
        flightLog.Add("EARTH OBSERVATION COMPLETE — "+lastFlightOutcome); ShowScreen(GameScreen.Results);
    }

    private void CompleteMissionSimulation()
    {
        // A mission must retain meaningful power, communication and vehicle health,
        // collect useful science data, and transmit at least one meaningful packet batch.
        missionSuccess = flightPower >= 20 && flightLink >= 50 && flightIntegrity >= 55 &&
                         flightDataPoints >= 12 && transmittedDataPoints >= 5;
        float resourceBonus = (flightPower + flightLink + flightIntegrity) * 0.075f;
        float scienceBonus = Mathf.Min(18f, flightDataPoints * 0.35f);
        missionScore = Mathf.Clamp(Mathf.RoundToInt(flightScore + resourceBonus + scienceBonus), 0, 100);
        if (missionSuccess) missionScore = Mathf.Max(60, missionScore);
        else missionScore = Mathf.Min(59, missionScore);
    }

    private void InitializeLunarSurfaceMission()
    {
        lunarOxygen = 100f;
        lunarSuitBattery = 100f;
        lunarTemperature = selectedMission == "MARS" ? -63f : -18f;
        lunarSamplesCollected = 0;
        lunarTerrainScanned = false;
        lunarBeaconDeployed = false;
        lunarReturnedToLander = false;
        lunarRoverMode = false;
        lunarRoverBattery = 100f;
        lunarRoverWheels = new Transform[6];
        lunarLandingRunning = false;
        lunarControlsUnlocked = false;
        lunarYaw = 0f;
        lunarPitch = 13f;
        lunarVerticalVelocity = 0f;
        flightLog.Add((selectedMission == "MARS" ? "MARTIAN SURFACE PHASE — Transfer sequence complete. Preparing atmospheric entry and powered descent." : "LUNAR SURFACE PHASE — Transfer sequence complete. Preparing powered descent."));
        lastFlightOutcome = (selectedMission == "MARS" ? "Mars transfer complete. The descent vehicle is ready for atmospheric entry." : "Transfer complete. The lunar lander is ready for powered descent.");
    }

    private void BuildMoonLanding()
    {
        CreateLunarEnvironment(currentPage.transform, true);
        AddLunarRenderFeed(currentPage.transform, new Vector2(0.025f, 0.075f), new Vector2(0.975f, 0.88f));
        bool marsMission = selectedMission == "MARS";
        ScreenTitle(marsMission ? "MARS LANDING" : "LUNAR LANDING", marsMission ?
            "MARTIAN ENTRY  /  POWERED DESCENT  /  SURFACE APPROACH" :
            "POWERED DESCENT  /  AUTONOMOUS GUIDANCE  /  FINAL APPROACH");

        GameObject telemetry = Card(currentPage.transform, new Vector2(0.045f, 0.22f), new Vector2(0.27f, 0.68f), new Color32(7, 22, 39, 232));
       // AddAccent(telemetry.transform, cyan);
        Label("LANDING TELEMETRY", telemetry.transform, 16, cyan, TextAlignmentOptions.Left,
            new Vector2(360, 30), new Vector2(0.08f, 0.90f), Vector2.zero, true);
        Label(marsMission ? "TARGET SITE     /     JEZERO CRATER\n\nALTITUDE            12,000 m\nVERTICAL SPEED       -18.0 m/s\nHORIZONTAL SPEED       2.1 m/s\nDESCENT FUEL             76%\nHEAT SHIELD            NOMINAL" :
            "TARGET SITE     /     MARE TRANQUILLITATIS\n\nALTITUDE            12,000 m\nVERTICAL SPEED       -18.0 m/s\nHORIZONTAL SPEED       2.1 m/s\nLANDER FUEL             76%\nGUIDANCE              NOMINAL",
            telemetry.transform, 14, Color.white, TextAlignmentOptions.Left,
            new Vector2(360, 180), new Vector2(0.08f, 0.60f), Vector2.zero, false);

        GameObject right = Card(currentPage.transform, new Vector2(0.73f, 0.22f), new Vector2(0.955f, 0.68f), new Color32(7, 22, 39, 232));
      //  AddAccent(right.transform, gold);
        Label("LANDING CHECKLIST", right.transform, 16, gold, TextAlignmentOptions.Left,
            new Vector2(360, 30), new Vector2(0.08f, 0.90f), Vector2.zero, true);
        Label(marsMission ? "[OK] Atmospheric entry data loaded\n[OK] Terrain-relative navigation active\n[OK] Descent propulsion armed\n[OK] Landing legs deployed\n--  Touchdown confirmation" :
            "[OK] Landing radar online\n[OK] Hazard avoidance active\n[OK] Landing legs deployed\n[OK] Surface beacon acquired\n--  Touchdown confirmation",
            right.transform, 14, Color.white, TextAlignmentOptions.Left,
            new Vector2(360, 175), new Vector2(0.08f, 0.60f), Vector2.zero, false);

        GameObject center = Card(currentPage.transform, new Vector2(0.32f, 0.12f), new Vector2(0.68f, 0.34f), new Color32(5, 17, 33, 235));
       // AddAccent(center.transform, cyan);
        Label("CONTROLLED DESCENT", center.transform, 19, cyan, TextAlignmentOptions.Center,
            new Vector2(600, 34), new Vector2(0.5f, 0.78f), Vector2.zero, true);
        Label("Initiate the lander's final approach. The camera will follow the descent, engine plume and touchdown.",
            center.transform, 13, muted, TextAlignmentOptions.Center,
            new Vector2(600, 54), new Vector2(0.5f, 0.48f), Vector2.zero, false);
        Button(lunarLandingRunning ? "DESCENT IN PROGRESS..." : "BEGIN POWERED DESCENT  ›", center.transform,
            new Vector2(0.5f, 0.16f), new Vector2(390, 54), cyan,
            () => { if (!lunarLandingRunning) StartCoroutine(AnimateMoonLanderDescent()); }, !lunarLandingRunning);
        Label(marsMission ? "Entry and descent are simplified gameplay models, not real EDL flight dynamics." :
            "Powered descent is a simplified gameplay model, not a real flight dynamics simulation.",
            currentPage.transform, 10, muted, TextAlignmentOptions.Center,
            new Vector2(780, 18), new Vector2(0.5f, 0.045f), Vector2.zero, false);
    }

    private IEnumerator AnimateMoonLanderDescent()
    {
        if (lunarLandingRunning || lunarLanderRoot == null) yield break;
        lunarLandingRunning = true;
        PlaySfx(ignitionClip, 0.8f);
        StartAudioLoop(rocketLoopClip, 0.26f);
        if (lunarDescentDust != null)
        {
            var emission = lunarDescentDust.emission;
            emission.rateOverTime = 0f;
            lunarDescentDust.Play();
        }
        Vector3 start = lunarLanderRoot.position;
        float endY = GetLunarTerrainHeight(lunarLanderPosition.x, lunarLanderPosition.z) - 0.05f;
        float duration = 8f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            lunarLanderRoot.position = new Vector3(start.x, Mathf.Lerp(start.y, endY, eased), start.z);
            float flame = Mathf.Lerp(2.1f, 0.55f, t) * (1f + Mathf.Sin(Time.time * 28f) * 0.06f);
            if (lunarDescentFlameLeft != null) lunarDescentFlameLeft.localScale = new Vector3(0.48f, flame, 0.48f);
            if (lunarDescentFlameRight != null) lunarDescentFlameRight.localScale = new Vector3(0.48f, flame, 0.48f);
            if (lunarDescentDust != null)
            {
                var emission = lunarDescentDust.emission;
                emission.rateOverTime = t > 0.68f ? Mathf.Lerp(18f, 95f, (t - 0.68f) / 0.32f) : 0f;
            }
            if (lunarWorldCamera != null)
            {
                Vector3 target = lunarLanderRoot.position + new Vector3(0f, 1.6f, 0f);
                lunarWorldCamera.transform.position = Vector3.Lerp(lunarWorldCamera.transform.position,
                    target + new Vector3(18f, 10f, -24f), Time.deltaTime * 1.4f);
                lunarWorldCamera.transform.LookAt(target);
            }
            yield return null;
        }
        if (lunarDescentFlameLeft != null) lunarDescentFlameLeft.gameObject.SetActive(false);
        if (lunarDescentFlameRight != null) lunarDescentFlameRight.gameObject.SetActive(false);
        if (lunarDescentDust != null)
        {
            var emission = lunarDescentDust.emission;
            emission.rateOverTime = 0f;
            lunarDescentDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        StopAudioLoop();
        PlaySfx(lunarBeaconClip != null ? lunarBeaconClip : radioClip, 0.7f);
        yield return new WaitForSeconds(1.0f);
        lunarLandingRunning = false;
        lunarControlsUnlocked = false;
        lastFlightOutcome = "Touchdown confirmed. Awaiting crew egress.";
        flightLog.Add((selectedMission == "MARS" ? "MARS LANDING — Touchdown confirmed in the selected exploration region." : "LUNAR LANDING — Touchdown confirmed at the selected lunar landing site."));
        ShowScreen(GameScreen.MoonSurface);
    }

    private void BuildMoonSurface()
    {
        if (gameAudioSource != null && spaceAmbienceClip != null)
            StartAudioLoop(spaceAmbienceClip, 0.16f);
        CreateLunarEnvironment(currentPage.transform, false);
        AddLunarRenderFeed(currentPage.transform, new Vector2(0f, 0.075f), new Vector2(1f, 0.90f));
        bool marsMission = selectedMission == "MARS";
        ScreenTitle(marsMission ? "MARTIAN SURFACE OPERATIONS" : "LUNAR SURFACE OPERATIONS",
            marsMission ? "EXIT THE LANDER  /  SURVEY JEZERO CRATER  /  COLLECT ROCK CORES  /  RETURN WITH DATA" :
            "EXIT THE LANDER  /  EXPLORE  /  COLLECT SAMPLES  /  RETURN WITH DATA");

        // Left-side survival telemetry.
        GameObject survival = Card(currentPage.transform, new Vector2(0.025f, 0.43f), new Vector2(0.235f, 0.855f), new Color32(5, 18, 34, 232));
       // AddAccent(survival.transform, cyan);
        Label("EXPLORER STATUS", survival.transform, 16, cyan, TextAlignmentOptions.Left,
            new Vector2(330, 28), new Vector2(0.08f, 0.91f), Vector2.zero, true);
        lunarOxygenText = Label("OXYGEN   100%", survival.transform, 14, green, TextAlignmentOptions.Left,
            new Vector2(330, 24), new Vector2(0.08f, 0.77f), Vector2.zero, true);
        lunarOxygenBarFill = BuildLunarBar(survival.transform, "OXYGEN RESERVE", 0.64f, lunarOxygen / 100f, green);
        lunarBatteryText = Label("SUIT BATTERY   100%", survival.transform, 14, gold, TextAlignmentOptions.Left,
            new Vector2(330, 24), new Vector2(0.08f, 0.43f), Vector2.zero, true);
        lunarBatteryBarFill = BuildLunarBar(survival.transform, "SUIT POWER", 0.31f, lunarSuitBattery / 100f, gold);
        Label("SURFACE TEMP   " + (marsMission ? "-63 °C" : "-18 °C"), survival.transform, 13, muted, TextAlignmentOptions.Left,
            new Vector2(330, 22), new Vector2(0.08f, 0.16f), Vector2.zero, false);
        lunarSamplesText = Label("SAMPLES   0 / 3", survival.transform, 14, cyan, TextAlignmentOptions.Left,
            new Vector2(330, 24), new Vector2(0.08f, 0.07f), Vector2.zero, true);

        // Right-side objectives.
        GameObject objectives = Card(currentPage.transform, new Vector2(0.765f, 0.43f), new Vector2(0.975f, 0.855f), new Color32(5, 18, 34, 232));
     //   AddAccent(objectives.transform, gold);
        Label(marsMission ? "MARTIAN FIELD OBJECTIVES" : "LUNAR FIELD OBJECTIVES", objectives.transform, 15, gold, TextAlignmentOptions.Left,
            new Vector2(350, 30), new Vector2(0.08f, 0.91f), Vector2.zero, true);
        lunarObjectivesText = Label("", objectives.transform, 13, Color.white, TextAlignmentOptions.Left,
            new Vector2(350, 190), new Vector2(0.08f, 0.64f), Vector2.zero, false);
        lunarPromptText = Label("EXIT THE LANDER TO BEGIN EXPLORATION", currentPage.transform, 15, cyan,
            TextAlignmentOptions.Center, new Vector2(720, 30), new Vector2(0.5f, 0.365f), Vector2.zero, true);
        lunarNavigationText = Label("NEXT TARGET  /  LUNAR SURVEY", currentPage.transform, 11, gold,
            TextAlignmentOptions.Center, new Vector2(520, 20), new Vector2(0.5f, 0.325f), Vector2.zero, true);

        // Compact control deck. Keyboard controls work as soon as the player exits the module.
        GameObject controls = Card(currentPage.transform, new Vector2(0.245f, 0.105f), new Vector2(0.755f, 0.285f), new Color32(5, 17, 32, 238));
      //  AddAccent(controls.transform, cyan);
        Label("EXPLORER TOOLKIT", controls.transform, 14, cyan, TextAlignmentOptions.Left,
            new Vector2(520, 24), new Vector2(0.05f, 0.88f), Vector2.zero, true);
        Button(marsMission ? "COLLECT ROCK CORE  [E]" : "COLLECT SAMPLE  [E]", controls.transform, new Vector2(0.25f, 0.56f), new Vector2(205, 42), cyanDark,
            CollectNearestLunarSample);
        Button(marsMission ? "ANALYZE REGOLITH  [F]" : "ANALYZE TERRAIN  [F]", controls.transform, new Vector2(0.75f, 0.56f), new Vector2(205, 42), cardAltColor,
            AnalyzeLunarTerrain);
        Button(marsMission ? "DEPLOY WEATHER BEACON  [B]" : "DEPLOY BEACON  [B]", controls.transform, new Vector2(0.25f, 0.22f), new Vector2(205, 42), cardAltColor,
            DeployLunarBeacon);
        Button("ROVER MODE  [Q]", controls.transform, new Vector2(0.75f, 0.22f), new Vector2(205, 42), cardAltColor,
            ToggleLunarRover);

        if (!lunarControlsUnlocked)
        {
            GameObject egress = Card(currentPage.transform, new Vector2(0.355f, 0.31f), new Vector2(0.645f, 0.60f), new Color32(5, 17, 32, 246));
            lunarLandingOverlay = egress;
          //  AddAccent(egress.transform, green);
            Label(marsMission ? "MARS TOUCHDOWN CONFIRMED" : "TOUCHDOWN CONFIRMED", egress.transform, 22, green, TextAlignmentOptions.Center,
                new Vector2(500, 34), new Vector2(0.5f, 0.78f), Vector2.zero, true);
            Label(marsMission ? "The descent vehicle is stable. Exit the cabin to begin your Martian field expedition." : "The lander is stable. Exit the cabin to begin your surface expedition.", egress.transform, 14, Color.white,
                TextAlignmentOptions.Center, new Vector2(500, 54), new Vector2(0.5f, 0.53f), Vector2.zero, false);
            Button("EXIT LANDER  ›", egress.transform, new Vector2(0.5f, 0.19f), new Vector2(250, 48), cyan,
                ExitLunarLander);
        }

        Label("WASD MOVE   •   HOLD RIGHT MOUSE TO LOOK   •   SCROLL ZOOM   •   SHIFT SPRINT   •   SPACE LOW-G JUMP   •   E SAMPLE   •   F SCAN   •   B BEACON   •   Q ROVER   •   R RETURN",
            currentPage.transform, 10, muted, TextAlignmentOptions.Center,
            new Vector2(1180, 18), new Vector2(0.5f, 0.035f), Vector2.zero, true);
        UpdateLunarObjectives();
    }

    private void AddLunarRenderFeed(Transform parent, Vector2 min, Vector2 max)
    {
        RawImage raw = new GameObject("LunarSurface3D_Feed", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        raw.transform.SetParent(parent, false);
        raw.texture = lunarWorldTexture;
        raw.color = Color.white;
        raw.raycastTarget = false;
        RectTransform rect = raw.rectTransform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        raw.transform.SetAsFirstSibling();
        Image scrim = CreateImage("LunarSceneScrim", parent, new Color(0.005f, 0.012f, 0.025f, 0.12f));
        scrim.raycastTarget = false;
        RectTransform scrimRect = scrim.rectTransform;
        scrimRect.anchorMin = min;
        scrimRect.anchorMax = max;
        scrimRect.offsetMin = Vector2.zero;
        scrimRect.offsetMax = Vector2.zero;
        // Raw image was inserted as the first sibling; the scrim remains above it but below HUD cards.
    }

    private void CreateLunarEnvironment(Transform parent, bool landingMode)
    {
        lunarWorldRoot = new GameObject("LUNAR_SURFACE_3D_WORLD");
        lunarWorldTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        lunarWorldTexture.name = landingMode ? "LunarLanding_RenderTexture" : "LunarSurface_RenderTexture";
        lunarWorldTexture.antiAliasing = 2;
        lunarWorldTexture.Create();

        GameObject cameraGo = new GameObject("LunarSurfaceCamera", typeof(Camera));
        cameraGo.transform.SetParent(lunarWorldRoot.transform, false);
        lunarWorldCamera = cameraGo.GetComponent<Camera>();
        lunarWorldCamera.targetTexture = lunarWorldTexture;
        lunarWorldCamera.clearFlags = CameraClearFlags.SolidColor;
        lunarWorldCamera.backgroundColor = selectedMission == "MARS" ? new Color32(91, 43, 25, 255) : new Color32(1, 4, 14, 255);
        lunarWorldCamera.fieldOfView = 58f;
        lunarWorldCamera.nearClipPlane = 0.08f;
        lunarWorldCamera.farClipPlane = 1000f;
        lunarWorldCamera.allowHDR = true;
        lunarWorldCamera.allowMSAA = true;

        Light sun = new GameObject("LunarSun", typeof(Light)).GetComponent<Light>();
        sun.transform.SetParent(lunarWorldRoot.transform, false);
        sun.type = LightType.Directional;
        sun.intensity = 1.65f;
        sun.color = new Color(0.88f, 0.92f, 1f);
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.72f;
        sun.transform.rotation = Quaternion.Euler(34f, -38f, 0f);

        GenerateLunarCraterField();
        BuildLunarTerrain();
        BuildLunarHorizonAndSky();
        lunarLanderPosition = new Vector3(8f, GetLunarTerrainHeight(8f, 9f), 9f);
        BuildLunarRocks();
        BuildLunarLander(landingMode);
        BuildLunarRover();
        BuildLunarSampleSites();
        BuildLunarScanStation();
        BuildLunarBeaconStation();
        BuildSurfaceSafetyBoundary();

        if (landingMode)
        {
            lunarWorldCamera.transform.position = new Vector3(24f, 13f, -16f);
            lunarWorldCamera.transform.LookAt(lunarLanderRoot.position + new Vector3(0f, 2f, 0f));
        }
        else
        {
            BuildLunarAstronaut();
            lunarYaw = 0f;
            lunarPitch = -7f;
            lunarCameraDistance = 6.35f;
            lunarVerticalVelocity = 0f;
            lunarBoundaryWarningTimer = 0f;
            UpdateLunarCameraImmediate();
        }
    }

    private void GenerateLunarCraterField()
    {
        lunarCraterCenters = new Vector2[]
        {
            new Vector2(-31f,-24f), new Vector2(-20f,25f), new Vector2(29f,28f),
            new Vector2(34f,-29f), new Vector2(-4f,42f), new Vector2(-42f,10f),
            new Vector2(20f,-43f), new Vector2(-8f,-42f), new Vector2(43f,2f),
            new Vector2(-43f,-38f), new Vector2(3f,32f), new Vector2(40f,39f),
            new Vector2(-7f,55f), new Vector2(-53f,28f), new Vector2(55f,-18f),
            new Vector2(8f,-58f), new Vector2(-30f,52f), new Vector2(56f,43f)
        };
        lunarCraterRadii = new float[] { 12f, 9f, 13f, 10f, 8f, 11f, 9f, 7f, 8f, 7f, 5.5f, 7f, 8f, 6f, 8f, 6.5f, 7.5f, 6f };
        lunarCraterDepths = new float[] { 3.8f, 2.6f, 4.1f, 3.1f, 2.3f, 3.4f, 2.8f, 2.2f, 2.4f, 2f, 1.7f, 2.4f, 2.5f, 1.9f, 2.2f, 1.7f, 2.1f, 1.8f };
    }

    private float GetLunarTerrainHeight(float x, float z)
    {
        float broad = Mathf.PerlinNoise((x + 101f) * 0.026f, (z + 53f) * 0.026f);
        float medium = Mathf.PerlinNoise((x + 7f) * 0.078f, (z - 13f) * 0.078f);
        float fine = Mathf.PerlinNoise((x - 19f) * 0.21f, (z + 31f) * 0.21f);
        bool marsTerrain = selectedMission == "MARS";
        float h = marsTerrain
            ? -0.22f + (broad - 0.5f) * 2.55f + (medium - 0.5f) * 0.90f + (fine - 0.5f) * 0.24f
              + Mathf.Sin(x * .085f + Mathf.PerlinNoise(z * .025f, x * .018f) * 2.2f) * .38f
            : -0.48f + (broad - 0.5f) * 2.0f + (medium - 0.5f) * 0.72f + (fine - 0.5f) * 0.20f;
        if (lunarCraterCenters != null)
        {
            for (int i = 0; i < lunarCraterCenters.Length; i++)
            {
                float d = Vector2.Distance(new Vector2(x, z), lunarCraterCenters[i]);
                float r = lunarCraterRadii[i] * (marsTerrain ? 1.12f : 1f);
                float craterScale = marsTerrain ? .46f : 1f;
                if (d < r)
                {
                    float bowl = 1f - d / r;
                    h -= lunarCraterDepths[i] * craterScale * bowl * bowl;
                }
                else if (d < r * 1.20f)
                {
                    float rim = 1f - Mathf.Abs(d - r * 1.08f) / (r * 0.12f);
                    h += Mathf.Max(0f, rim) * (marsTerrain ? .42f : .78f);
                }
            }
        }
        return h;
    }

    private void BuildLunarTerrain()
    {
        const int resolution = 156;
        const float size = 150f;
        Mesh mesh = new Mesh();
        mesh.name = "Procedural_Lunar_Regolith_Terrain";
        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];
        int v = 0;
        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float px = (x / (float)resolution - 0.5f) * size;
                float pz = (z / (float)resolution - 0.5f) * size;
                vertices[v] = new Vector3(px, GetLunarTerrainHeight(px, pz), pz);
                uv[v] = new Vector2(x / (float)resolution * 6f, z / (float)resolution * 6f);
                v++;
            }
        }
        int ti = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int a = z * (resolution + 1) + x;
                int b = a + resolution + 1;
                triangles[ti++] = a; triangles[ti++] = b; triangles[ti++] = a + 1;
                triangles[ti++] = a + 1; triangles[ti++] = b; triangles[ti++] = b + 1;
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject terrain = new GameObject("LUNAR_REGOLITH_TERRAIN", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        terrain.transform.SetParent(lunarWorldRoot.transform, false);
        terrain.GetComponent<MeshFilter>().sharedMesh = mesh;
        terrain.GetComponent<MeshCollider>().sharedMesh = mesh;
        Texture2D texture = Resources.Load<Texture2D>(selectedMission == "MARS" ? "Environments/MarsRegolithAlbedo" : "Environments/LunarRegolithAlbedo");
        if (texture == null)
        {
            texture = selectedMission == "MARS" ? BuildMarsRegolithTexture(768) : BuildLunarRegolithTexture(768);
            transientTextures.Add(texture);
        }
        Texture2D normalMap = Resources.Load<Texture2D>("Environments/LunarRegolithNormal");
        Material regolith = MakeMaterial(Color.white, 0.12f, 0f);
        if (regolith.HasProperty("_BaseMap")) regolith.SetTexture("_BaseMap", texture);
        if (regolith.HasProperty("_MainTex")) regolith.SetTexture("_MainTex", texture);
        if (normalMap != null && regolith.HasProperty("_BumpMap"))
        {
            regolith.SetTexture("_BumpMap", normalMap);
            if (regolith.HasProperty("_BumpScale")) regolith.SetFloat("_BumpScale", 0.72f);
            regolith.EnableKeyword("_NORMALMAP");
        }
        if (regolith.HasProperty("_Smoothness")) regolith.SetFloat("_Smoothness", 0.18f);
        terrain.GetComponent<MeshRenderer>().sharedMaterial = regolith;
    }

    private Texture2D BuildLunarRegolithTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.name = "Lunar_Regolith_Fallback_Rocky_Texture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        Color[] pixels = new Color[size * size];
        System.Random random = new System.Random(9217);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float broad = Mathf.PerlinNoise(u * 14f + 3f, v * 14f + 17f);
                float mid = Mathf.PerlinNoise(u * 58f + 8f, v * 58f + 5f);
                float fine = Mathf.PerlinNoise(u * 180f + 21f, v * 180f + 11f);
                float grain = (float)random.NextDouble();
                float shade = 0.36f + broad * 0.12f + mid * 0.10f + fine * 0.045f + grain * 0.025f;
                Color c = new Color(shade * 1.02f, shade * 1.025f, shade * 1.04f, 1f);
                if (mid > 0.80f) c *= 0.78f;
                pixels[y * size + x] = c;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        return texture;
    }

    private Texture2D BuildMarsRegolithTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.name = "Mars_Iron_Oxide_Regolith_Albedo";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        Color[] pixels = new Color[size*size];
        System.Random random = new System.Random(4187);
        for (int y=0; y<size; y++)
        for (int x=0; x<size; x++)
        {
            float u=x/(float)size, v=y/(float)size;
            float broad=Mathf.PerlinNoise(u*13f+3f,v*12f+19f);
            float mid=Mathf.PerlinNoise(u*63f+8f,v*59f+5f);
            float fine=Mathf.PerlinNoise(u*190f+21f,v*177f+11f);
            float grain=(float)random.NextDouble();
            Color rust=new Color(.40f,.19f,.12f);
            Color ochre=new Color(.61f,.31f,.18f);
            Color c=Color.Lerp(rust,ochre,Mathf.Clamp01((broad-.28f)*1.55f));
            c*=.78f+mid*.24f+fine*.075f+grain*.035f;
            if(mid>.82f) c*=.72f;
            pixels[y*size+x]=c;
        }
        texture.SetPixels(pixels);
        texture.Apply(true,false);
        return texture;
    }

    private void BuildLunarHorizonAndSky()
    {
        Texture2D panorama = selectedMission == "MARS" ? BuildMarsSkyTexture(1024, 512) : Resources.Load<Texture2D>("Environments/LunarSkyPanorama");
        if (selectedMission == "MARS" && panorama != null) transientTextures.Add(panorama);
        if (panorama != null)
        {
            GameObject dome = new GameObject(selectedMission == "MARS" ? "MARS_ATMOSPHERIC_SKY_DOME" : "LUNAR_360_DEEP_SPACE_SKY", typeof(MeshFilter), typeof(MeshRenderer));
            dome.transform.SetParent(lunarWorldRoot.transform, false);
            // The panorama's Earth is centered at 180 degrees longitude; rotate it into the forward camera view.
            dome.transform.localRotation = Quaternion.Euler(0f, selectedMission == "MARS" ? 0f : 90f, 0f);
            Mesh domeMesh = BuildInsideOutSkyDome(230f, 96, 48);
            dome.GetComponent<MeshFilter>().sharedMesh = domeMesh;
            Material skyMat = MakeUnlitTextureMaterial(panorama);
            skyMat.renderQueue = 1000;
            MeshRenderer domeRenderer = dome.GetComponent<MeshRenderer>();
            domeRenderer.sharedMaterial = skyMat;
            domeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            domeRenderer.receiveShadows = false;
        }
        else
        {
            Material stars = MakeMaterial(new Color32(180, 207, 255, 255), 0f, 2.2f);
            System.Random random = new System.Random(4242);
            for (int i = 0; i < 240; i++)
            {
                Vector3 p = UnityEngine.Random.onUnitSphere * 185f;
                float size = 0.07f + (float)random.NextDouble() * 0.11f;
                CreatePrimitiveObject("LunarStar_" + i, PrimitiveType.Sphere, lunarWorldRoot.transform, p, Vector3.one * size, stars);
            }
        }

        // The equirectangular panorama supplies distant mountains and deep-space depth;
        // the nearby procedural terrain remains fully 3D and provides the local parallax.
    }

    private Texture2D BuildMarsSkyTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.name = "Procedural_Mars_Dusty_Atmosphere_Panorama";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float haze = Mathf.PerlinNoise(u * 5.5f + 2.3f, v * 7.0f + 8.1f);
                Color zenith = new Color(0.055f, 0.018f, 0.014f);
                Color upper = new Color(0.22f, 0.075f, 0.035f);
                Color horizon = new Color(0.70f, 0.31f, 0.13f);
                Color c = v > 0.52f ? Color.Lerp(horizon, upper, (v - 0.52f) / 0.48f) : Color.Lerp(new Color(0.30f,0.095f,0.045f), horizon, v / 0.52f);
                c *= 0.88f + haze * 0.18f;
                float sunGlow = Mathf.Exp(-Mathf.Pow((u - 0.72f) * 8f, 2f) - Mathf.Pow((v - 0.58f) * 15f, 2f));
                c += new Color(0.42f, 0.15f, 0.045f) * sunGlow;
                pixels[y * width + x] = c;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        return texture;
    }

    private Mesh BuildInsideOutSkyDome(float radius, int segments, int rings)
    {
        Mesh mesh = new Mesh();
        mesh.name = "InsideOut_Lunar_Sky_Dome";
        Vector3[] vertices = new Vector3[(segments + 1) * (rings + 1)];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[segments * rings * 6];
        int vi = 0;
        for (int y = 0; y <= rings; y++)
        {
            float v = y / (float)rings;
            float phi = Mathf.PI * v;
            for (int x = 0; x <= segments; x++)
            {
                float u = x / (float)segments;
                float theta = u * Mathf.PI * 2f;
                Vector3 n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                vertices[vi] = n * radius;
                normals[vi] = -n;
                uv[vi] = new Vector2(u, 1f - v);
                vi++;
            }
        }
        int ti = 0;
        for (int y = 0; y < rings; y++)
        for (int x = 0; x < segments; x++)
        {
            int a = y * (segments + 1) + x;
            int b = a + segments + 1;
            triangles[ti++] = a; triangles[ti++] = b; triangles[ti++] = a + 1;
            triangles[ti++] = a + 1; triangles[ti++] = b; triangles[ti++] = b + 1;
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateLunarDistantRidge(string objectName, int segments, float distance, float heightScale,
        Material material, int seed)
    {
        Mesh mesh = new Mesh();
        mesh.name = objectName + "Mesh";
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];
        System.Random random = new System.Random(seed);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = Mathf.Lerp(-115f, 115f, t);
            float profile = 0.45f + Mathf.PerlinNoise(t * 9f + seed, seed * 0.013f) * 0.55f;
            float peak = 1.7f + profile * heightScale + (float)random.NextDouble() * 1.1f;
            vertices[i * 2] = new Vector3(x, peak, distance);
            vertices[i * 2 + 1] = new Vector3(x, -18f, distance);
            uv[i * 2] = new Vector2(t, 1f);
            uv[i * 2 + 1] = new Vector2(t, 0f);
        }
        int ti = 0;
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2;
            triangles[ti++] = a; triangles[ti++] = a + 2; triangles[ti++] = a + 1;
            triangles[ti++] = a + 2; triangles[ti++] = a + 3; triangles[ti++] = a + 1;
        }
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        GameObject ridge = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
        ridge.transform.SetParent(lunarWorldRoot.transform, false);
        ridge.GetComponent<MeshFilter>().sharedMesh = mesh;
        ridge.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private Mesh BuildIrregularRockMesh(int seed)
    {
        List<Vector3> verts = new List<Vector3>
        {
            Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
        };
        List<int> tris = new List<int>
        {
            0,4,3, 0,2,4, 0,5,2, 0,3,5,
            1,3,4, 1,4,2, 1,2,5, 1,5,3
        };
        for (int level = 0; level < 2; level++)
        {
            Dictionary<long, int> midpointCache = new Dictionary<long, int>();
            List<int> refined = new List<int>(tris.Count * 4);
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                int ab = RockMidpoint(a, b, verts, midpointCache);
                int bc = RockMidpoint(b, c, verts, midpointCache);
                int ca = RockMidpoint(c, a, verts, midpointCache);
                refined.AddRange(new int[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
            }
            tris = refined;
        }
        System.Random rand = new System.Random(seed);
        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 dir = verts[i].normalized;
            float pock = Mathf.PerlinNoise(dir.x * 5.3f + seed * 0.01f, dir.z * 6.1f + seed * 0.02f);
            float radius = 0.72f + (float)rand.NextDouble() * 0.30f + (pock - 0.5f) * 0.18f;
            Vector3 v = dir * radius;
            v.y *= 0.76f;
            if (v.y < -0.48f) v.y = -0.48f + (v.y + 0.48f) * 0.10f;
            verts[i] = v;
        }
        Mesh mesh = new Mesh();
        mesh.name = "Angular_Lunar_Basalt_Boulder_" + seed;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private int RockMidpoint(int a, int b, List<Vector3> verts, Dictionary<long, int> cache)
    {
        int min = Mathf.Min(a, b), max = Mathf.Max(a, b);
        long key = ((long)min << 32) | (uint)max;
        int index;
        if (cache.TryGetValue(key, out index)) return index;
        index = verts.Count;
        verts.Add((verts[a] + verts[b]).normalized);
        cache[key] = index;
        return index;
    }

    private GameObject CreateIrregularRock(string name, Transform parent, Vector3 position, Vector3 scale,
        int seed, Material material, bool addCollider)
    {
        GameObject rock = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        rock.transform.SetParent(parent, false);
        rock.transform.localPosition = position;
        rock.transform.localScale = scale;
        rock.transform.localRotation = Quaternion.Euler(seed * 17f % 28f, seed * 47f % 360f, seed * 29f % 24f);
        rock.GetComponent<MeshFilter>().sharedMesh = BuildIrregularRockMesh(seed);
        rock.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (addCollider)
        {
            MeshCollider collider = rock.AddComponent<MeshCollider>();
            collider.sharedMesh = rock.GetComponent<MeshFilter>().sharedMesh;
        }
        return rock;
    }

    private GameObject CreateTorusObject(string name, Transform parent, Vector3 center, float majorRadius,
        float minorRadius, Material material, int ringSegments = 28, int tubeSegments = 8)
    {
        Vector3[] vertices = new Vector3[(ringSegments + 1) * (tubeSegments + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[ringSegments * tubeSegments * 6];
        int vi = 0;
        for (int i = 0; i <= ringSegments; i++)
        {
            float u = i / (float)ringSegments * Mathf.PI * 2f;
            for (int j = 0; j <= tubeSegments; j++)
            {
                float v = j / (float)tubeSegments * Mathf.PI * 2f;
                float r = majorRadius + minorRadius * Mathf.Cos(v);
                vertices[vi] = new Vector3(Mathf.Cos(u) * r, Mathf.Sin(v) * minorRadius, Mathf.Sin(u) * r);
                uv[vi] = new Vector2(i / (float)ringSegments, j / (float)tubeSegments);
                vi++;
            }
        }
        int ti = 0;
        for (int i = 0; i < ringSegments; i++)
        for (int j = 0; j < tubeSegments; j++)
        {
            int a = i * (tubeSegments + 1) + j;
            int b = a + tubeSegments + 1;
            triangles[ti++] = a; triangles[ti++] = a + 1; triangles[ti++] = b;
            triangles[ti++] = a + 1; triangles[ti++] = b + 1; triangles[ti++] = b;
        }
        Mesh mesh = new Mesh();
        mesh.name = name + "Mesh";
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        GameObject ring = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        ring.transform.SetParent(parent, false);
        ring.transform.localPosition = center;
        ring.GetComponent<MeshFilter>().sharedMesh = mesh;
        ring.GetComponent<MeshRenderer>().sharedMaterial = material;
        return ring;
    }

    private void BuildLunarRocks()
    {
        bool mars = selectedMission == "MARS";
        Material rockA = MakeMaterial(mars ? new Color32(132, 69, 43, 255) : new Color32(92, 96, 101, 255), 0.08f, 0f);
        Material rockB = MakeMaterial(mars ? new Color32(171, 94, 58, 255) : new Color32(126, 126, 127, 255), 0.10f, 0f);
        Material rockDark = MakeMaterial(mars ? new Color32(76, 39, 30, 255) : new Color32(66, 70, 76, 255), 0.08f, 0f);
        System.Random random = new System.Random(611);
        int made = 0;
        for (int i = 0; i < 72; i++)
        {
            float x = (float)random.NextDouble() * 126f - 63f;
            float z = (float)random.NextDouble() * 122f - 58f;
            if (Mathf.Abs(x - lunarLanderPosition.x) < 6.5f && Mathf.Abs(z - lunarLanderPosition.z) < 7.5f) continue;
            float size = 0.38f + (float)random.NextDouble() * 1.55f;
            if (i % 13 == 0) size *= 1.45f;
            float ground = GetLunarTerrainHeight(x, z);
            CreateIrregularRock("LunarAngularBoulder_" + made++, lunarWorldRoot.transform,
                new Vector3(x, ground + size * 0.31f, z),
                new Vector3(size * 1.12f, size * 0.76f, size * 0.94f),
                611 + i * 17, (i % 7 == 0) ? rockDark : ((i % 3 == 0) ? rockB : rockA), size > 0.85f);
        }
        // Small angular fragments and regolith clasts break up the smooth foreground.
        for (int i = 0; i < 155; i++)
        {
            float x = (float)random.NextDouble() * 128f - 64f;
            float z = (float)random.NextDouble() * 124f - 60f;
            if (Mathf.Abs(x - lunarLanderPosition.x) < 3.2f && Mathf.Abs(z - lunarLanderPosition.z) < 3.5f) continue;
            float size = 0.10f + (float)random.NextDouble() * 0.28f;
            float ground = GetLunarTerrainHeight(x, z);
            CreateIrregularRock("LunarRegolithFragment_" + i, lunarWorldRoot.transform,
                new Vector3(x, ground + size * 0.20f, z), new Vector3(size * 1.3f, size * 0.75f, size),
                1700 + i * 13, (i % 5 == 0) ? rockB : rockA, false);
        }
    }

    private void BuildLunarLander(bool landingMode)
    {
        lunarLanderRoot = new GameObject("LUNAR_LANDER_3D_MODEL").transform;
        lunarLanderRoot.SetParent(lunarWorldRoot.transform, false);
        float ground = GetLunarTerrainHeight(lunarLanderPosition.x, lunarLanderPosition.z);
        lunarLanderRoot.position = new Vector3(lunarLanderPosition.x, ground + (landingMode ? 13f : -0.05f), lunarLanderPosition.z);

        Material white = MakeMaterial(new Color32(207, 220, 226, 255), 0.56f, 0.02f);
        Material gold = MakeMaterial(new Color32(192, 139, 57, 255), 0.54f, 0.08f);
        Material dark = MakeMaterial(new Color32(35, 49, 62, 255), 0.72f, 0.02f);
        Material foil = MakeMaterial(new Color32(150, 159, 165, 255), 0.42f, 0.03f);
        Material blue = MakeMaterial(new Color32(27, 91, 146, 255), 0.42f, 0.16f);
        Material glow = MakeMaterial(new Color32(44, 190, 245, 255), 0.12f, 1.15f);
        CreatePrimitiveObject("LanderDescentStage", PrimitiveType.Cylinder, lunarLanderRoot, new Vector3(0f, 1.45f, 0f),
            new Vector3(2.25f, 0.72f, 2.25f), gold);
        CreatePrimitiveObject("LanderCabin", PrimitiveType.Cube, lunarLanderRoot, new Vector3(0f, 2.65f, 0f),
            new Vector3(2.45f, 1.45f, 2.15f), white);
        CreatePrimitiveObject("LanderFrontWindow", PrimitiveType.Cube, lunarLanderRoot, new Vector3(0f, 2.83f, 1.09f),
            new Vector3(1.1f, 0.64f, 0.05f), blue);
        CreatePrimitiveObject("LanderLeftWindow", PrimitiveType.Cube, lunarLanderRoot, new Vector3(-1.24f, 2.82f, 0.12f),
            new Vector3(0.05f, 0.58f, 0.78f), blue);
        CreatePrimitiveObject("LanderRightWindow", PrimitiveType.Cube, lunarLanderRoot, new Vector3(1.24f, 2.82f, 0.12f),
            new Vector3(0.05f, 0.58f, 0.78f), blue);
        CreatePrimitiveObject("LanderFrontHatch", PrimitiveType.Cube, lunarLanderRoot, new Vector3(0f, 1.80f, 1.10f),
            new Vector3(.84f, .82f, .075f), foil);
        CreatePrimitiveObject("LanderHatchWindow", PrimitiveType.Cube, lunarLanderRoot, new Vector3(0f, 1.96f, 1.15f),
            new Vector3(.46f, .28f, .035f), blue);
        CreatePrimitiveObject("LanderHatchHandle", PrimitiveType.Cylinder, lunarLanderRoot, new Vector3(.34f, 1.72f, 1.19f),
            new Vector3(.055f, .13f, .055f), gold).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        CreatePrimitiveObject("LanderTopAntenna", PrimitiveType.Cylinder, lunarLanderRoot, new Vector3(0f, 3.55f, -0.18f),
            new Vector3(0.07f, 0.8f, 0.07f), foil);
        CreatePrimitiveObject("LanderAntennaDish", PrimitiveType.Sphere, lunarLanderRoot, new Vector3(0f, 4.0f, -0.18f),
            new Vector3(0.85f, 0.2f, 0.85f), white);
        CreateTorusObject("LanderDishRim", lunarLanderRoot, new Vector3(0f, 4.0f, -0.18f), .39f, .035f, gold);
        CreatePrimitiveObject("LanderEngineMount", PrimitiveType.Cylinder, lunarLanderRoot, new Vector3(0f, .66f, 0f),
            new Vector3(.92f, .42f, .92f), dark);
        CreatePrimitiveObject("LanderMainEngineBell", PrimitiveType.Cylinder, lunarLanderRoot, new Vector3(0f, .28f, 0f),
            new Vector3(.62f, .48f, .62f), foil);
        CreatePrimitiveObject("LanderEngineThroat", PrimitiveType.Cylinder, lunarLanderRoot, new Vector3(0f, .015f, 0f),
            new Vector3(.33f, .10f, .33f), dark);
        for (int side = -1; side <= 1; side += 2)
        {
            CreatePrimitiveObject("LanderAvionicsBox", PrimitiveType.Cube, lunarLanderRoot,
                new Vector3(side * 1.28f, 1.64f, -.42f), new Vector3(.38f, .55f, .58f), gold);
            CreatePrimitiveObject("LanderAvionicsVent", PrimitiveType.Cube, lunarLanderRoot,
                new Vector3(side * 1.49f, 1.65f, -.42f), new Vector3(.025f, .30f, .40f), dark);
        }
        // EVA ladder and grab rails on the hatch side.
        CreateBeam(lunarLanderRoot, new Vector3(-.55f, .25f, 1.48f), new Vector3(-.55f, 1.75f, 1.48f), .075f, foil);
        CreateBeam(lunarLanderRoot, new Vector3(.55f, .25f, 1.48f), new Vector3(.55f, 1.75f, 1.48f), .075f, foil);
        for (int rung = 0; rung < 5; rung++)
            CreateBeam(lunarLanderRoot, new Vector3(-.55f, .35f + rung * .28f, 1.48f),
                new Vector3(.55f, .35f + rung * .28f, 1.48f), .055f, gold);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 foot = new Vector3(Mathf.Cos(angle) * 2.0f, 0.2f, Mathf.Sin(angle) * 1.75f);
            CreateBeam(lunarLanderRoot, new Vector3(0f, 1.3f, 0f), foot, 0.13f, foil);
            Vector3 upperJoint = new Vector3(Mathf.Cos(angle) * 1.35f, 1.55f, Mathf.Sin(angle) * 1.22f);
            CreateBeam(lunarLanderRoot, upperJoint, foot + new Vector3(0f, .22f, 0f), .085f, gold);
            CreatePrimitiveObject("LanderFootPad", PrimitiveType.Cylinder, lunarLanderRoot,
                foot + new Vector3(0f, -0.08f, 0f), new Vector3(0.43f, 0.07f, 0.43f), dark);
            CreatePrimitiveObject("LanderLandingLight", PrimitiveType.Sphere, lunarLanderRoot,
                new Vector3(Mathf.Cos(angle) * 0.9f, 1.1f, Mathf.Sin(angle) * 0.9f),
                Vector3.one * 0.13f, glow);
        }
        if (HasSelected("POWER"))
        {
            CreatePrimitiveObject("LanderSolarPanelLeft", PrimitiveType.Cube, lunarLanderRoot,
                new Vector3(-2.1f, 2.2f, 0f), new Vector3(1.4f, 0.10f, 1.05f), blue);
            CreatePrimitiveObject("LanderSolarPanelRight", PrimitiveType.Cube, lunarLanderRoot,
                new Vector3(2.1f, 2.2f, 0f), new Vector3(1.4f, 0.10f, 1.05f), blue);
            for (int side = -1; side <= 1; side += 2)
            for (int line = -2; line <= 2; line++)
                CreateBeam(lunarLanderRoot, new Vector3(side * 2.1f - .62f, 2.27f, line * .19f),
                    new Vector3(side * 2.1f + .62f, 2.27f, line * .19f), .025f, foil);
        }
        BoxCollider landerCollider = lunarLanderRoot.gameObject.AddComponent<BoxCollider>();
        landerCollider.center = new Vector3(0f, 2.0f, 0f);
        landerCollider.size = new Vector3(2.85f, 4.1f, 2.65f);
        if (landingMode)
        {
            Material descentOuter = MakeMaterial(new Color32(255, 117, 38, 255), 0f, 3.0f);
            Material descentInner = MakeMaterial(new Color32(255, 224, 142, 255), 0f, 4.0f);
            lunarDescentFlameLeft = CreateConeObject("LanderDescentFlameLeft", lunarLanderRoot,
                new Vector3(-0.62f, 0.34f, 0f), .34f, 2.7f, descentOuter).transform;
            lunarDescentFlameRight = CreateConeObject("LanderDescentFlameRight", lunarLanderRoot,
                new Vector3(0.62f, 0.34f, 0f), .28f, 2.7f, descentInner).transform;
            lunarDescentFlameLeft.localRotation = Quaternion.Euler(180f, 0f, 0f);
            lunarDescentFlameRight.localRotation = Quaternion.Euler(180f, 0f, 0f);
            lunarDescentDust = CreateLaunchParticleSystem("LunarDescentDust", lunarWorldRoot.transform,
                new Vector3(lunarLanderPosition.x, ground + 0.45f, lunarLanderPosition.z),
                new Color32(180, 179, 174, 180), new Color32(73, 76, 82, 0), 2.4f, 2.6f, 0.65f, 42f);
        }
    }

    private void BuildLunarSampleSites()
    {
        Material rock = MakeMaterial(new Color32(139, 140, 142, 255), 0.2f, 0f);
        Material markerMat = MakeMaterial(new Color32(49, 210, 255, 255), 0.08f, 1.8f);
        Vector3[] positions =
        {
            new Vector3(-8f, 0f, 10f), new Vector3(20f, 0f, 18f), new Vector3(-28f, 24f, 13f)
        };
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 p = positions[i];
            p.y = GetLunarTerrainHeight(p.x, p.z);
            CreateIrregularRock("SampleBasaltCluster_Main_" + i, lunarWorldRoot.transform,
                p + new Vector3(0.35f, 0.34f, 0.18f), new Vector3(1.15f, 0.80f, 0.95f), 810 + i * 37, rock, true);
            CreateIrregularRock("SampleBasaltCluster_FragmentA_" + i, lunarWorldRoot.transform,
                p + new Vector3(-0.55f, 0.16f, 0.38f), new Vector3(.46f, .30f, .38f), 940 + i * 19, rock, false);
            CreateIrregularRock("SampleBasaltCluster_FragmentB_" + i, lunarWorldRoot.transform,
                p + new Vector3(.85f, 0.12f, -0.32f), new Vector3(.34f, .22f, .31f), 1080 + i * 23, rock, false);
            Transform marker = CreatePrimitiveObject("SampleMarker_" + i, PrimitiveType.Cylinder, lunarWorldRoot.transform,
                p + new Vector3(0f, 0.40f, 0f), new Vector3(0.08f, 0.42f, 0.08f), markerMat).transform;
            CreatePrimitiveObject("SampleMarkerBeacon_" + i, PrimitiveType.Sphere, marker,
                new Vector3(0f, 0.72f, 0f), Vector3.one * 0.26f, markerMat);
            lunarSampleMarkers[i] = marker;
        }
    }

    private void BuildLunarScanStation()
    {
        lunarScanStationPosition = new Vector3(-17f, GetLunarTerrainHeight(-17f, 15f), 15f);
        Material metal = MakeMaterial(new Color32(170, 185, 194, 255), 0.45f, 0f);
        Material cyanLight = MakeMaterial(new Color32(37, 193, 243, 255), 0.1f, 1.8f);
        CreatePrimitiveObject("TerrainScanTripod", PrimitiveType.Cylinder, lunarWorldRoot.transform,
            lunarScanStationPosition + new Vector3(0f, 1.0f, 0f), new Vector3(0.38f, 0.68f, 0.38f), metal);
        CreatePrimitiveObject("TerrainScanHead", PrimitiveType.Cube, lunarWorldRoot.transform,
            lunarScanStationPosition + new Vector3(0f, 1.95f, 0f), new Vector3(1.0f, 0.72f, 0.7f), metal);
        CreatePrimitiveObject("TerrainScanLens", PrimitiveType.Sphere, lunarWorldRoot.transform,
            lunarScanStationPosition + new Vector3(0f, 1.98f, 0.38f), new Vector3(0.42f, 0.42f, 0.12f), cyanLight);
    }

    private void BuildLunarBeaconStation()
    {
        lunarBeaconStationPosition = new Vector3(1f, GetLunarTerrainHeight(1f, -18f), -18f);
        Material marker = MakeMaterial(new Color32(255, 191, 71, 255), 0.15f, 1.1f);
        CreatePrimitiveObject("BeaconDeploymentPad", PrimitiveType.Cylinder, lunarWorldRoot.transform,
            lunarBeaconStationPosition + new Vector3(0f, 0.12f, 0f), new Vector3(1.25f, 0.12f, 1.25f), marker);
        CreatePrimitiveObject("BeaconSiteMarker", PrimitiveType.Sphere, lunarWorldRoot.transform,
            lunarBeaconStationPosition + new Vector3(0f, 1.2f, 0f), Vector3.one * 0.25f, marker);
    }

    private bool IsInsideSurfaceBoundary(float x, float z, float margin = .65f)
    {
        return x >= SurfaceBoundaryMin + margin && x <= SurfaceBoundaryMax - margin &&
               z >= SurfaceBoundaryMin + margin && z <= SurfaceBoundaryMax - margin;
    }

    private void BuildSurfaceSafetyBoundary()
    {
        bool mars = selectedMission == "MARS";
        Material perimeter = MakeMaterial(mars ? new Color32(255,112,62,255) : new Color32(54,196,239,255), .12f, 1.1f);
        Material postMat = MakeMaterial(mars ? new Color32(227,113,62,255) : new Color32(198,216,228,255), .5f, .12f);
        Material beacon = MakeMaterial(mars ? new Color32(255,166,67,255) : new Color32(76,215,255,255), .08f, 2f);
        float min=SurfaceBoundaryMin, max=SurfaceBoundaryMax;
        int divisions=9;
        for(int i=0;i<divisions;i++)
        {
            float t=i/(float)(divisions-1);
            float x=Mathf.Lerp(min,max,t);
            float z=Mathf.Lerp(min,max,t);
            CreateSurfaceBoundaryPost(x,min,postMat,beacon);
            CreateSurfaceBoundaryPost(x,max,postMat,beacon);
            CreateSurfaceBoundaryPost(min,z,postMat,beacon);
            CreateSurfaceBoundaryPost(max,z,postMat,beacon);
            if(i<divisions-1)
            {
                float nx=Mathf.Lerp(min,max,(i+1)/(float)(divisions-1));
                float nz=Mathf.Lerp(min,max,(i+1)/(float)(divisions-1));
                Vector3 a=new Vector3(x,GetLunarTerrainHeight(x,min)+.65f,min);
                Vector3 b=new Vector3(nx,GetLunarTerrainHeight(nx,min)+.65f,min);
                CreateBeam(lunarWorldRoot.transform,a,b,.075f,perimeter);
                a=new Vector3(x,GetLunarTerrainHeight(x,max)+.65f,max);
                b=new Vector3(nx,GetLunarTerrainHeight(nx,max)+.65f,max);
                CreateBeam(lunarWorldRoot.transform,a,b,.075f,perimeter);
                a=new Vector3(min,GetLunarTerrainHeight(min,z)+.65f,z);
                b=new Vector3(min,GetLunarTerrainHeight(min,nz)+.65f,nz);
                CreateBeam(lunarWorldRoot.transform,a,b,.075f,perimeter);
                a=new Vector3(max,GetLunarTerrainHeight(max,z)+.65f,z);
                b=new Vector3(max,GetLunarTerrainHeight(max,nz)+.65f,nz);
                CreateBeam(lunarWorldRoot.transform,a,b,.075f,perimeter);
            }
        }
        LabelSurfaceBoundaryMarker(selectedMission == "MARS" ? "MARS EVA LIMIT" : "LUNAR EVA LIMIT", new Vector3(0f,GetLunarTerrainHeight(0f,SurfaceBoundaryMax)+2.7f,SurfaceBoundaryMax));
    }

    private void CreateSurfaceBoundaryPost(float x,float z,Material postMat,Material beacon)
    {
        float ground=GetLunarTerrainHeight(x,z);
        CreatePrimitiveObject("EVA_LIMIT_MARKER_POST",PrimitiveType.Cylinder,lunarWorldRoot.transform,
            new Vector3(x,ground+1.05f,z),new Vector3(.095f,1.05f,.095f),postMat);
        CreatePrimitiveObject("EVA_LIMIT_WARNING_LAMP",PrimitiveType.Sphere,lunarWorldRoot.transform,
            new Vector3(x,ground+2.15f,z),Vector3.one*.22f,beacon);
    }

    private void LabelSurfaceBoundaryMarker(string text,Vector3 position)
    {
        // A simple elevated marker makes the operational limit legible from the surface camera.
        Material sign=MakeMaterial(new Color32(20,43,57,255),.4f,0f);
        Material light=MakeMaterial(new Color32(255,172,72,255),.05f,1.8f);
        CreatePrimitiveObject("EVA_BOUNDARY_SIGN",PrimitiveType.Cube,lunarWorldRoot.transform,
            position,new Vector3(7.5f,.85f,.22f),sign);
        CreatePrimitiveObject("EVA_BOUNDARY_SIGN_LAMP",PrimitiveType.Sphere,lunarWorldRoot.transform,
            position+Vector3.up*.55f,Vector3.one*.22f,light);
        TextMeshPro signText = new GameObject("EVA_BOUNDARY_SIGN_TEXT",typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        signText.transform.SetParent(lunarWorldRoot.transform,false);
        signText.text = text;
        if (gameFont != null) signText.font = gameFont;
        signText.fontSize = 2.4f;
        signText.alignment = TextAlignmentOptions.Center;
        signText.color = new Color32(255,190,105,255);
        signText.rectTransform.sizeDelta = new Vector2(7.0f,.72f);
        signText.transform.position = position + new Vector3(0f,0f,-.14f);
        signText.transform.rotation = Quaternion.Euler(0f,180f,0f);
    }

    private void BuildLunarAstronaut()
    {
        lunarAstronautObject = new GameObject("PLAYER_ASTRONAUT_EVA", typeof(CharacterController));
        lunarAstronautObject.transform.SetParent(lunarWorldRoot.transform, false);
        Vector3 start = new Vector3(-2f, GetLunarTerrainHeight(-2f, -5f), -5f);
        lunarAstronautObject.transform.position = start;
        lunarCharacterController = lunarAstronautObject.GetComponent<CharacterController>();
        lunarCharacterController.height = 2.38f;
        lunarCharacterController.radius = 0.40f;
        lunarCharacterController.center = new Vector3(0f, 1.19f, 0f);
        lunarCharacterController.skinWidth = 0.035f;
        lunarCharacterController.stepOffset = 0.25f;
        lunarCharacterController.slopeLimit = 42f;
        lunarCameraTarget = lunarAstronautObject.transform;

        lunarAstronautVisual = new GameObject(selectedMission == "MARS" ? "HUMAN_MARS_SURFACE_PRESSURE_SUIT" : "HUMAN_LUNAR_EVA_SUIT").transform;
        lunarAstronautVisual.SetParent(lunarAstronautObject.transform, false);
        bool marsSuit = selectedMission == "MARS";
        Material suit = MakeMaterial(marsSuit ? new Color32(210, 190, 158, 255) : new Color32(218, 226, 230, 255), 0.16f, 0.01f);
        Material suitShade = MakeMaterial(marsSuit ? new Color32(174, 143, 109, 255) : new Color32(167, 180, 188, 255), 0.24f, 0f);
        Material trim = MakeMaterial(marsSuit ? new Color32(67, 42, 34, 255) : new Color32(35, 48, 62, 255), 0.48f, 0.02f);
        Material rubber = MakeMaterial(new Color32(19, 27, 37, 255), 0.10f, 0f);
        Material gold = MakeMaterial(marsSuit ? new Color32(212, 119, 53, 255) : new Color32(197, 151, 73, 255), 0.58f, 0.05f);
        Material visorOuter = MakeMaterial(marsSuit ? new Color32(174, 101, 58, 255) : new Color32(112, 89, 48, 255), 0.82f, 0.12f);
        Material visor = MakeMaterial(marsSuit ? new Color32(51, 39, 35, 255) : new Color32(18, 46, 68, 255), 0.78f, 0.28f);
        Material blue = MakeMaterial(marsSuit ? new Color32(153, 72, 43, 255) : new Color32(19, 77, 132, 255), 0.38f, 0.14f);
        Material red = MakeMaterial(marsSuit ? new Color32(223, 112, 59, 255) : new Color32(184, 53, 48, 255), 0.26f, 0.04f);
        Material glow = MakeMaterial(new Color32(66, 205, 244, 255), 0.08f, 1.35f);

        // Pressure torso and flexible waist articulation.
        CreatePrimitiveObject("PRESSURE_SUIT_TORSO", PrimitiveType.Capsule, lunarAstronautVisual,
            new Vector3(0f, 1.24f, 0f), new Vector3(0.66f, 0.72f, 0.49f), suit);
        CreatePrimitiveObject("CHEST_HARD_SHELL", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(0f, 1.34f, 0.33f), new Vector3(0.48f, 0.46f, 0.18f), suitShade);
        CreateTorusObject("WAIST_PRESSURE_SEAL", lunarAstronautVisual, new Vector3(0f, 0.83f, 0f), .30f, .055f, trim);
        CreatePrimitiveObject("NECK_SEAL", PrimitiveType.Cylinder, lunarAstronautVisual,
            new Vector3(0f, 1.82f, 0f), new Vector3(.38f, .12f, .38f), rubber);

        // Helmet with layered gold visor, locking ring, comms pods and lamps.
        CreatePrimitiveObject("HELMET_OUTER_SHELL", PrimitiveType.Sphere, lunarAstronautVisual,
            new Vector3(0f, 2.10f, 0.02f), new Vector3(.76f, .76f, .70f), suit);
        CreatePrimitiveObject("HELMET_VISOR_GOLD_RIM", PrimitiveType.Sphere, lunarAstronautVisual,
            new Vector3(0f, 2.09f, .315f), new Vector3(.60f, .52f, .265f), visorOuter);
        CreatePrimitiveObject("HELMET_DARK_GOLD_TINTED_VISOR", PrimitiveType.Sphere, lunarAstronautVisual,
            new Vector3(0f, 2.10f, .365f), new Vector3(.525f, .435f, .205f), visor);
        CreateTorusObject("HELMET_NECK_LOCKING_RING", lunarAstronautVisual, new Vector3(0f, 1.86f, 0f), .34f, .045f, gold);
        CreatePrimitiveObject("VISOR_REFLECTION_STRIP", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(-.16f, 2.27f, .535f), new Vector3(.055f, .19f, .025f), glow);
        CreatePrimitiveObject("HELMET_LEFT_COMMS_POD", PrimitiveType.Cylinder, lunarAstronautVisual,
            new Vector3(-.39f, 2.06f, 0f), new Vector3(.18f, .24f, .18f), trim).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        CreatePrimitiveObject("HELMET_RIGHT_COMMS_POD", PrimitiveType.Cylinder, lunarAstronautVisual,
            new Vector3(.39f, 2.06f, 0f), new Vector3(.18f, .24f, .18f), trim).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        CreatePrimitiveObject("HELMET_WORK_LIGHT", PrimitiveType.Sphere, lunarAstronautVisual,
            new Vector3(.30f, 2.40f, .20f), Vector3.one * .10f, glow);

        // PLSS life-support backpack, paired oxygen bottles, valves and readout.
        CreatePrimitiveObject("PLSS_BACKPACK_MAIN", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(0f, 1.28f, -.49f), new Vector3(.78f, .93f, .43f), trim);
        CreatePrimitiveObject("PLSS_BACKPACK_TOP", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(0f, 1.78f, -.48f), new Vector3(.58f, .18f, .40f), suitShade);
        for (int side = -1; side <= 1; side += 2)
        {
            CreatePrimitiveObject("OXYGEN_BOTTLE", PrimitiveType.Cylinder, lunarAstronautVisual,
                new Vector3(side * .25f, 1.31f, -.73f), new Vector3(.17f, .66f, .17f), suitShade);
            CreatePrimitiveObject("OXYGEN_BOTTLE_VALVE", PrimitiveType.Cylinder, lunarAstronautVisual,
                new Vector3(side * .25f, 1.69f, -.73f), new Vector3(.09f, .12f, .09f), gold);
            CreatePrimitiveObject("BACKPACK_STATUS_LED", PrimitiveType.Sphere, lunarAstronautVisual,
                new Vector3(side * .30f, 1.12f, -.72f), Vector3.one * .08f, glow);
        }
        CreatePrimitiveObject("PLSS_REAR_SERVICE_PANEL", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(0f, 1.22f, -.725f), new Vector3(.34f, .45f, .035f), blue);
        CreatePrimitiveObject("CHEST_TELEMETRY_PANEL", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(0f, 1.40f, .445f), new Vector3(.38f, .28f, .07f), trim);
        CreatePrimitiveObject("CHEST_DISPLAY", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(0f, 1.43f, .488f), new Vector3(.26f, .12f, .018f), blue);
        for (int i = 0; i < 3; i++)
            CreatePrimitiveObject("CHEST_CONTROL_BUTTON", PrimitiveType.Sphere, lunarAstronautVisual,
                new Vector3(-.11f + i * .11f, 1.29f, .49f), Vector3.one * .045f, i == 1 ? red : glow);

        // Shoulder joints and segmented arms; pivots allow a readable low-gravity walk cycle.
        lunarLeftArm = new GameObject("LEFT_ARM_PIVOT").transform;
        lunarLeftArm.SetParent(lunarAstronautVisual, false);
        lunarLeftArm.localPosition = new Vector3(-.49f, 1.55f, .02f);
        lunarRightArm = new GameObject("RIGHT_ARM_PIVOT").transform;
        lunarRightArm.SetParent(lunarAstronautVisual, false);
        lunarRightArm.localPosition = new Vector3(.49f, 1.55f, .02f);
        BuildEVAArm(lunarLeftArm, suit, suitShade, trim, rubber, gold, blue, glow, -1f);
        BuildEVAArm(lunarRightArm, suit, suitShade, trim, rubber, gold, blue, glow, 1f);

        // Hips, articulated legs, knee hard-points and reinforced lunar boots.
        lunarLeftLeg = new GameObject("LEFT_LEG_PIVOT").transform;
        lunarLeftLeg.SetParent(lunarAstronautVisual, false);
        lunarLeftLeg.localPosition = new Vector3(-.22f, .89f, 0f);
        lunarRightLeg = new GameObject("RIGHT_LEG_PIVOT").transform;
        lunarRightLeg.SetParent(lunarAstronautVisual, false);
        lunarRightLeg.localPosition = new Vector3(.22f, .89f, 0f);
        BuildEVALeg(lunarLeftLeg, suit, suitShade, trim, rubber, gold, -1f);
        BuildEVALeg(lunarRightLeg, suit, suitShade, trim, rubber, gold, 1f);

        // Mission patches and waist-mounted tools make the suit read as a human EVA system.
        CreatePrimitiveObject("LEFT_SHOULDER_PATCH", PrimitiveType.Cylinder, lunarAstronautVisual,
            new Vector3(-.63f, 1.53f, .04f), new Vector3(.18f, .045f, .18f), blue).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        CreatePrimitiveObject("PATCH_RED_STRIPE", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(-.63f, 1.54f, .095f), new Vector3(.18f, .025f, .025f), red);
        CreatePrimitiveObject("WAIST_TOOL_POUCH_LEFT", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(-.38f, .78f, .24f), new Vector3(.22f, .28f, .22f), trim);
        CreatePrimitiveObject("WAIST_TOOL_POUCH_RIGHT", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(.38f, .78f, .24f), new Vector3(.22f, .28f, .22f), trim);
        CreatePrimitiveObject("SUIT_FRONT_ZIPPER", PrimitiveType.Cube, lunarAstronautVisual,
            new Vector3(0f, 1.02f, .455f), new Vector3(.025f, .42f, .018f), gold);
        CreateSuitHose(lunarAstronautVisual, new Vector3(.28f, 1.58f, -.64f), new Vector3(.46f, 1.42f, .43f), rubber, 9);

        lunarAstronautObject.SetActive(false);
    }

    private void BuildEVAArm(Transform pivot, Material suit, Material suitShade, Material trim,
        Material rubber, Material gold, Material blue, Material glow, float side)
    {
        CreatePrimitiveObject("SHOULDER_HARD_SHELL", PrimitiveType.Sphere, pivot,
            new Vector3(0f, -.03f, 0f), new Vector3(.34f, .35f, .34f), suitShade);
        CreatePrimitiveObject("UPPER_ARM_PRESSURE_SLEEVE", PrimitiveType.Capsule, pivot,
            new Vector3(0f, -.33f, 0f), new Vector3(.25f, .37f, .26f), suit);
        CreateTorusObject("ELBOW_FLEX_RING", pivot, new Vector3(0f, -.58f, .02f), .205f, .035f, trim);
        CreatePrimitiveObject("ELBOW_JOINT", PrimitiveType.Sphere, pivot,
            new Vector3(0f, -.61f, .02f), new Vector3(.23f, .23f, .24f), suitShade);
        CreatePrimitiveObject("FOREARM_PRESSURE_SLEEVE", PrimitiveType.Capsule, pivot,
            new Vector3(0f, -.84f, .05f), new Vector3(.24f, .34f, .25f), suit);
        CreateTorusObject("WRIST_SEAL", pivot, new Vector3(0f, -1.06f, .06f), .18f, .035f, gold);
        CreatePrimitiveObject("EVA_GLOVE_PALM", PrimitiveType.Sphere, pivot,
            new Vector3(0f, -1.23f, .10f), new Vector3(.26f, .24f, .27f), suitShade);
        for (int finger = 0; finger < 4; finger++)
            CreatePrimitiveObject("GLOVE_FINGER", PrimitiveType.Capsule, pivot,
                new Vector3((finger - 1.5f) * .095f, -1.37f, .19f), new Vector3(.055f, .12f, .06f), suit);
        CreatePrimitiveObject("GLOVE_THUMB", PrimitiveType.Capsule, pivot,
            new Vector3(side * .22f, -1.25f, .14f), new Vector3(.07f, .14f, .08f), suit);
        CreatePrimitiveObject("FOREARM_ID_BAND", PrimitiveType.Cube, pivot,
            new Vector3(0f, -.91f, .27f), new Vector3(.18f, .11f, .035f), blue);
        CreatePrimitiveObject("FOREARM_LED", PrimitiveType.Sphere, pivot,
            new Vector3(0f, -.91f, .30f), Vector3.one * .045f, glow);
    }

    private void BuildEVALeg(Transform pivot, Material suit, Material suitShade, Material trim,
        Material rubber, Material gold, float side)
    {
        CreatePrimitiveObject("THIGH_PRESSURE_SLEEVE", PrimitiveType.Capsule, pivot,
            new Vector3(0f, -.15f, 0f), new Vector3(.27f, .22f, .28f), suit);
        CreatePrimitiveObject("THIGH_TOOL_HARDPOINT", PrimitiveType.Cube, pivot,
            new Vector3(side * .12f, -.19f, .23f), new Vector3(.14f, .18f, .08f), trim);
        CreatePrimitiveObject("KNEE_PROTECTOR", PrimitiveType.Sphere, pivot,
            new Vector3(0f, -.37f, .15f), new Vector3(.29f, .24f, .21f), suitShade);
        CreateTorusObject("KNEE_FLEX_SEAL", pivot, new Vector3(0f, -.40f, .01f), .16f, .030f, gold);
        CreatePrimitiveObject("LOWER_LEG_PRESSURE_SLEEVE", PrimitiveType.Capsule, pivot,
            new Vector3(0f, -.57f, .02f), new Vector3(.25f, .20f, .26f), suit);
        CreatePrimitiveObject("SHIN_GUARD", PrimitiveType.Cube, pivot,
            new Vector3(0f, -.58f, .235f), new Vector3(.28f, .28f, .07f), suitShade);
        CreateTorusObject("ANKLE_SEAL", pivot, new Vector3(0f, -.75f, .02f), .15f, .030f, trim);
        CreatePrimitiveObject("LUNAR_BOOT", PrimitiveType.Cube, pivot,
            new Vector3(0f, -.80f, .13f), new Vector3(.37f, .17f, .48f), trim);
        CreatePrimitiveObject("BOOT_SOLE", PrimitiveType.Cube, pivot,
            new Vector3(0f, -.895f, .14f), new Vector3(.39f, .055f, .50f), rubber);
        CreatePrimitiveObject("BOOT_TOE_GUARD", PrimitiveType.Cube, pivot,
            new Vector3(0f, -.80f, .32f), new Vector3(.34f, .10f, .12f), suitShade);
    }

    private void CreateSuitHose(Transform parent, Vector3 start, Vector3 end, Material material, int segments)
    {
        Vector3 mid = Vector3.Lerp(start, end, .5f) + new Vector3(.20f, .18f, -.12f);
        Vector3 previous = start;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = (1f-t)*(1f-t)*start + 2f*(1f-t)*t*mid + t*t*end;
            Vector3 delta = point - previous;
            Transform segment = CreatePrimitiveObject("EVA_OXYGEN_HOSE_SEGMENT", PrimitiveType.Cylinder, parent,
                (point + previous) * .5f, new Vector3(.055f, delta.magnitude * .52f, .055f), material).transform;
            segment.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            previous = point;
        }
    }

    private void BuildLunarRover()
    {
        lunarRover = new GameObject(selectedMission == "MARS" ? "MARS_EXPLORATION_ROVER" : "LUNAR_ROVER").transform;
        Transform rover = lunarRover;
        rover.SetParent(lunarWorldRoot.transform, false);
        Vector3 pos = new Vector3(13f, GetLunarTerrainHeight(13f, 2f) + 0.1f, 2f);
        rover.position = pos;
        Material body = MakeMaterial(selectedMission == "MARS" ? new Color32(197, 157, 119, 255) : new Color32(193, 202, 207, 255), 0.38f, 0.02f);
        Material dark = MakeMaterial(new Color32(31, 43, 54, 255), 0.45f, 0f);
        Material solar = MakeMaterial(selectedMission == "MARS" ? new Color32(42, 76, 104, 255) : new Color32(26, 96, 151, 255), 0.42f, 0.16f);
        CreatePrimitiveObject("RoverChassis", PrimitiveType.Cube, rover, new Vector3(0f, 0.85f, 0f), new Vector3(2.2f, 0.52f, 1.5f), body);
        CreatePrimitiveObject("RoverSolarPanel", PrimitiveType.Cube, rover, new Vector3(0f, 1.25f, -0.1f), new Vector3(1.55f, 0.08f, 1.0f), solar);
        CreatePrimitiveObject("RoverMast", PrimitiveType.Cylinder, rover, new Vector3(0f, 1.75f, 0.35f), new Vector3(0.09f, 0.55f, 0.09f), dark);
        CreatePrimitiveObject("RoverCameraHead", PrimitiveType.Cube, rover, new Vector3(0f, 2.2f, 0.35f), new Vector3(0.5f, 0.42f, 0.42f), body);
        int wheelIndex = 0;
        for (int side = -1; side <= 1; side += 2)
        for (int axle = -1; axle <= 1; axle++)
        {
            Vector3 wheelPos = new Vector3(side * 1.0f, 0.47f, axle * 0.66f);
            CreateBeam(rover, new Vector3(side * .72f, .78f, axle * .40f), wheelPos, .075f, dark);
            Transform wheel = CreatePrimitiveObject("RoverWheel", PrimitiveType.Cylinder, rover,
                wheelPos, new Vector3(0.34f, 0.17f, 0.34f), dark).transform;
            wheel.localRotation = Quaternion.Euler(0f, 0f, 90f);
            lunarRoverWheels[wheelIndex++] = wheel;
            for (int tread = 0; tread < 8; tread++)
            {
                float a = tread * 45f * Mathf.Deg2Rad;
                CreatePrimitiveObject("RoverWheelTread", PrimitiveType.Cube, wheel,
                    new Vector3(0f, Mathf.Cos(a) * .30f, Mathf.Sin(a) * .30f),
                    new Vector3(.20f, .075f, .09f), body);
            }
        }
        BoxCollider roverCollider = rover.gameObject.AddComponent<BoxCollider>();
        roverCollider.center = new Vector3(0f, 0.9f, 0f);
        roverCollider.size = new Vector3(2.65f, 1.85f, 2.0f);
    }

    private void ExitLunarLander()
    {
        if (lunarAstronautObject == null) return;
        lunarControlsUnlocked = true;
        lunarAstronautObject.SetActive(true);
        lunarCharacterController.enabled = false;
        lunarAstronautObject.transform.position = new Vector3(-1.5f, GetLunarTerrainHeight(-1.5f, -3.2f), -3.2f);
        lunarCharacterController.enabled = true;
        lunarRoverMode = false;
        lunarCameraTarget = lunarAstronautObject.transform;
        UpdateLunarCameraImmediate();
        if (lunarLandingOverlay != null) lunarLandingOverlay.SetActive(false);
        lunarPromptText.text = "EXPLORE THE MARKED SITES  /  RIGHT MOUSE TO LOOK  /  WASD TO WALK";
        PlaySfx(radioClip, 0.4f);
        flightLog.Add("SURFACE OPERATIONS — Astronaut egress confirmed. EVA exploration started.");
        UpdateLunarObjectives();
    }

    private void UpdateLunarExplorer()
    {
        if (!lunarControlsUnlocked || lunarAstronautObject == null) return;
        if (lunarRoverMode)
        {
            UpdateLunarRoverControl();
            return;
        }
        if (!lunarAstronautObject.activeSelf || lunarCharacterController == null) return;
        if (Input.GetKeyDown(KeyCode.Q)) ToggleLunarRover();
        float dt = Time.deltaTime;
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            lunarYaw += Input.GetAxisRaw("Mouse X") * 3.4f;
            lunarPitch = Mathf.Clamp(lunarPitch - Input.GetAxisRaw("Mouse Y") * 2.7f, -24f, 38f);
        }
        float cameraZoom = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(cameraZoom) > .001f) lunarCameraDistance = Mathf.Clamp(lunarCameraDistance - cameraZoom * 2.8f, 4.2f, 10.5f);
        float x = 0f, z = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) z += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) z -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        Vector3 input = new Vector3(x, 0f, z).normalized;
        Quaternion yawRotation = Quaternion.Euler(0f, lunarYaw, 0f);
        Vector3 move = yawRotation * input;
        bool marsPhysics = selectedMission == "MARS";
        float surfaceGravity = marsPhysics ? 3.71f : 1.62f;
        float speed = (lunarOxygen < 12f || lunarSuitBattery < 10f) ? 1.25f :
                      (Input.GetKey(KeyCode.LeftShift) ? (marsPhysics ? 3.65f : 4.15f) : (marsPhysics ? 2.25f : 2.70f));
        if (lunarCharacterController.isGrounded)
        {
            if (lunarVerticalVelocity < 0f) lunarVerticalVelocity = -0.10f;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                float jumpHeight = marsPhysics ? .92f : 1.48f;
                lunarVerticalVelocity = Mathf.Sqrt(2f * surfaceGravity * jumpHeight);
            }
        }
        else lunarVerticalVelocity -= surfaceGravity * dt;
        Vector3 proposedPosition = lunarAstronautObject.transform.position + move * speed * dt;
        if (!IsInsideSurfaceBoundary(proposedPosition.x, proposedPosition.z))
        {
            move = Vector3.zero;
            lunarBoundaryWarningTimer = 2.5f;
        }
        else lunarBoundaryWarningTimer = Mathf.Max(0f, lunarBoundaryWarningTimer - dt);
        lunarCharacterController.Move((move * speed + Vector3.up * lunarVerticalVelocity) * dt);
        if (move.sqrMagnitude > 0.015f)
        {
            Quaternion desired = Quaternion.LookRotation(move, Vector3.up);
            lunarAstronautObject.transform.rotation = Quaternion.Slerp(lunarAstronautObject.transform.rotation, desired, dt * 7f);
            lunarFootstepTimer -= dt;
            if (lunarFootstepTimer <= 0f)
            {
                PlaySfx(lunarStepClip, 0.28f);
                lunarFootstepTimer = Input.GetKey(KeyCode.LeftShift) ? 0.42f : 0.68f;
            }
        }
        AnimateLunarAstronaut(move.magnitude > 0.1f);
        UpdateLunarCameraFollow();
        lunarOxygen = Mathf.Max(0f, lunarOxygen - dt * 0.018f);
        lunarSuitBattery = Mathf.Max(0f, lunarSuitBattery - dt * (move.sqrMagnitude > 0.01f ? 0.026f : 0.009f));
        if (Input.GetKeyDown(KeyCode.E)) CollectNearestLunarSample();
        if (Input.GetKeyDown(KeyCode.F)) AnalyzeLunarTerrain();
        if (Input.GetKeyDown(KeyCode.B)) DeployLunarBeacon();
        if (Input.GetKeyDown(KeyCode.R)) ReturnToLunarLander();
        UpdateLunarPrompt();
        UpdateLunarResourceLabels();
    }

    private void ToggleLunarRover()
    {
        if (!lunarControlsUnlocked || lunarAstronautObject == null || lunarRover == null) return;
        if (!lunarRoverMode)
        {
            if (!lunarAstronautObject.activeSelf) return;
            float distance = Vector3.Distance(lunarAstronautObject.transform.position, lunarRover.position);
            if (distance > 5.0f)
            {
                if (lunarPromptText != null) lunarPromptText.text = "ROVER OUT OF RANGE  /  WALK CLOSER TO THE VEHICLE";
                return;
            }
            lunarAstronautSavedPosition = lunarAstronautObject.transform.position;
            lunarAstronautObject.SetActive(false);
            lunarRoverMode = true;
            lunarCameraTarget = lunarRover;
            lunarPitch = -4f;
            lunarPromptText.text = "ROVER DRIVE ENABLED  /  WASD STEER  /  SHIFT BOOST  /  Q EXIT";
            PlaySfx(radioClip, 0.45f);
        }
        else
        {
            lunarRoverMode = false;
            lunarAstronautObject.SetActive(true);
            Vector3 exit = lunarRover.position + lunarRover.right * 1.7f;
            exit.y = GetLunarTerrainHeight(exit.x, exit.z);
            lunarCharacterController.enabled = false;
            lunarAstronautObject.transform.position = exit;
            lunarCharacterController.enabled = true;
            lunarCameraTarget = lunarAstronautObject.transform;
            lunarPromptText.text = "EVA MODE RESTORED  /  COLLECT THE REMAINING SAMPLES";
            PlaySfx(radioClip, 0.38f);
        }
        UpdateLunarCameraImmediate();
        UpdateLunarResourceLabels();
    }

    private void UpdateLunarRoverControl()
    {
        if (lunarRover == null) return;
        float dt = Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Q)) { ToggleLunarRover(); return; }
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            lunarYaw += Input.GetAxisRaw("Mouse X") * 3.4f;
            lunarPitch = Mathf.Clamp(lunarPitch - Input.GetAxisRaw("Mouse Y") * 2.4f, -24f, 38f);
        }
        float cameraZoom = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(cameraZoom) > .001f) lunarCameraDistance = Mathf.Clamp(lunarCameraDistance - cameraZoom * 2.8f, 4.2f, 10.5f);
        float x = 0f, z = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) z += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) z -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        Vector3 input = new Vector3(x, 0f, z).normalized;
        Vector3 move = Quaternion.Euler(0f, lunarYaw, 0f) * input;
        bool boosting = Input.GetKey(KeyCode.LeftShift) && lunarRoverBattery > 8f;
        float speed = lunarRoverBattery < 8f ? 1.15f : (boosting ? 6.2f : 3.5f);
        if (move.sqrMagnitude > .01f)
        {
            Vector3 next = lunarRover.position + move * speed * dt;
            next.x = Mathf.Clamp(next.x, SurfaceBoundaryMin + .8f, SurfaceBoundaryMax - .8f);
            next.z = Mathf.Clamp(next.z, SurfaceBoundaryMin + .8f, SurfaceBoundaryMax - .8f);
            if (Mathf.Abs(next.x - (lunarRover.position.x + move.x * speed * dt)) > .01f ||
                Mathf.Abs(next.z - (lunarRover.position.z + move.z * speed * dt)) > .01f)
                lunarBoundaryWarningTimer = 2.5f;
            else lunarBoundaryWarningTimer = Mathf.Max(0f, lunarBoundaryWarningTimer - dt);
            next.y = GetLunarTerrainHeight(next.x, next.z) + .12f;
            lunarRover.position = Vector3.Lerp(lunarRover.position, next, Mathf.Clamp01(dt * 12f));
            lunarRover.rotation = Quaternion.Slerp(lunarRover.rotation, Quaternion.LookRotation(move, Vector3.up), dt * 4f);
            for (int i = 0; i < lunarRoverWheels.Length; i++)
                if (lunarRoverWheels[i] != null) lunarRoverWheels[i].Rotate(Vector3.up, -speed * 42f * dt, Space.Self);
            if (boosting) lunarRoverBattery = Mathf.Max(0f, lunarRoverBattery - dt * .095f);
            else lunarRoverBattery = Mathf.Max(0f, lunarRoverBattery - dt * .045f);
        }
        else lunarRoverBattery = Mathf.Max(0f, lunarRoverBattery - dt * .008f);
        lunarOxygen = Mathf.Max(0f, lunarOxygen - dt * .008f);
        UpdateLunarCameraFollow();
        if (Input.GetKeyDown(KeyCode.E)) CollectNearestLunarSample();
        if (Input.GetKeyDown(KeyCode.F)) AnalyzeLunarTerrain();
        if (Input.GetKeyDown(KeyCode.B)) DeployLunarBeacon();
        if (Input.GetKeyDown(KeyCode.R)) ReturnToLunarLander();
        if (lunarPromptText != null && lunarPromptText.text.StartsWith("ROVER DRIVE ENABLED"))
            lunarPromptText.text = "ROVER DRIVE  /  BATTERY " + Mathf.RoundToInt(lunarRoverBattery) + "%  /  WASD STEER  /  SHIFT BOOST  /  Q EXIT";
        UpdateLunarResourceLabels();
    }

    private Vector3 GetLunarExplorerPosition()
    {
        if (lunarRoverMode && lunarRover != null) return lunarRover.position;
        return lunarAstronautObject != null ? lunarAstronautObject.transform.position : Vector3.zero;
    }

    private void AnimateLunarAstronaut(bool walking)
    {
        if (lunarLeftArm == null) return;
        float swing = walking ? Mathf.Sin(Time.time * 7.5f) * 23f : Mathf.Sin(Time.time * 1.1f) * 2f;
        lunarLeftArm.localRotation = Quaternion.Euler(swing, 0f, -8f);
        lunarRightArm.localRotation = Quaternion.Euler(-swing, 0f, 8f);
        lunarLeftLeg.localRotation = Quaternion.Euler(-swing * 0.72f, 0f, 0f);
        lunarRightLeg.localRotation = Quaternion.Euler(swing * 0.72f, 0f, 0f);
    }

    private void UpdateLunarCameraImmediate()
    {
        if (lunarWorldCamera == null || lunarCameraTarget == null) return;
        Vector3 focus = lunarCameraTarget.position + Vector3.up * (lunarRoverMode ? 1.58f : 1.52f);
        Quaternion rot = Quaternion.Euler(lunarPitch, lunarYaw, 0f);
        Vector3 pos = focus + rot * new Vector3(0f, 0.80f, -lunarCameraDistance);
        lunarWorldCamera.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(focus - pos));
    }

    private void UpdateLunarCameraFollow()
    {
        if (lunarWorldCamera == null || lunarCameraTarget == null) return;
        Vector3 focus = lunarCameraTarget.position + Vector3.up * (lunarRoverMode ? 1.58f : 1.52f);
        Quaternion rot = Quaternion.Euler(lunarPitch, lunarYaw, 0f);
        Vector3 desired = focus + rot * new Vector3(0f, 0.80f, -lunarCameraDistance);
        lunarWorldCamera.transform.position = Vector3.Lerp(lunarWorldCamera.transform.position, desired, Time.deltaTime * 8f);
        lunarWorldCamera.transform.rotation = Quaternion.Slerp(lunarWorldCamera.transform.rotation,
            Quaternion.LookRotation(focus - lunarWorldCamera.transform.position), Time.deltaTime * 9f);
    }

    private void CollectNearestLunarSample()
    {
        if (!lunarControlsUnlocked || lunarAstronautObject == null)
        {
            if (lunarPromptText != null) lunarPromptText.text = "EXIT THE LANDER BEFORE USING EXPLORATION TOOLS";
            return;
        }
        Vector3 explorerPosition = GetLunarExplorerPosition();
        int nearest = -1;
        float best = 4.3f;
        for (int i = 0; i < lunarSampleMarkers.Length; i++)
        {
            Transform marker = lunarSampleMarkers[i];
            if (marker == null || !marker.gameObject.activeSelf) continue;
            float distance = Vector3.Distance(explorerPosition, marker.position);
            if (distance < best) { best = distance; nearest = i; }
        }
        if (nearest < 0)
        {
            lunarPromptText.text = "NO SAMPLE IN RANGE  /  MOVE CLOSE TO A CYAN SAMPLE BEACON";
            return;
        }
        lunarSampleMarkers[nearest].gameObject.SetActive(false);
        lunarSamplesCollected++;
        flightDataPoints += 6;
        flightScore += 5;
        lunarSuitBattery = Mathf.Max(0f, lunarSuitBattery - 3.5f);
        lastFlightOutcome = "Lunar rock sample " + lunarSamplesCollected + " secured and tagged for return.";
        flightLog.Add((selectedMission == "MARS" ? "MARTIAN CORE" : "LUNAR SAMPLE") + " — Sample " + lunarSamplesCollected + " collected at a marked geology site.");
        PlaySfx(lunarScanClip, 0.65f);
        lunarPromptText.text = "SAMPLE SECURED  /  " + lunarSamplesCollected + " OF 3 STORED IN THE LANDER CONTAINER";
        UpdateLunarObjectives();
    }

    private void AnalyzeLunarTerrain()
    {
        if (!lunarControlsUnlocked || lunarAstronautObject == null)
        {
            if (lunarPromptText != null) lunarPromptText.text = "EXIT THE LANDER BEFORE USING EXPLORATION TOOLS";
            return;
        }
        float d = Vector3.Distance(GetLunarExplorerPosition(), lunarScanStationPosition);
        if (d > 5.2f)
        {
            lunarPromptText.text = "TERRAIN SCANNER OUT OF RANGE  /  FOLLOW THE CYAN SCAN STATION";
            return;
        }
        if (lunarTerrainScanned) { lunarPromptText.text = "TERRAIN ANALYSIS ALREADY COMPLETE"; return; }
        if (lunarSuitBattery < 9f) { lunarPromptText.text = "SUIT BATTERY TOO LOW TO RUN THE SCANNER"; return; }
        lunarTerrainScanned = true;
        lunarSuitBattery -= 9f;
        flightDataPoints += 12;
        flightScore += 9;
        lastFlightOutcome = "Terrain scan complete. Regolith density and crater structure were logged.";
        flightLog.Add("TERRAIN ANALYSIS — The field scanner completed a regolith and crater survey.");
        PlaySfx(lunarScanClip, 0.75f);
        lunarPromptText.text = (selectedMission == "MARS" ? "MARS REGOLITH SCAN COMPLETE  /  MINERAL DATA ADDED TO MISSION LOG" : "TERRAIN SCAN COMPLETE  /  GEOLOGY DATA ADDED TO MISSION LOG");
        UpdateLunarObjectives();
    }

    private void DeployLunarBeacon()
    {
        if (!lunarControlsUnlocked || lunarAstronautObject == null)
        {
            if (lunarPromptText != null) lunarPromptText.text = "EXIT THE LANDER BEFORE USING EXPLORATION TOOLS";
            return;
        }
        float d = Vector3.Distance(GetLunarExplorerPosition(), lunarBeaconStationPosition);
        if (d > 4.8f)
        {
            lunarPromptText.text = "BEACON PAD OUT OF RANGE  /  MOVE TO THE GOLD DEPLOYMENT MARKER";
            return;
        }
        if (lunarBeaconDeployed) { lunarPromptText.text = "NAVIGATION BEACON ALREADY DEPLOYED"; return; }
        if (lunarSuitBattery < 6f) { lunarPromptText.text = "SUIT BATTERY TOO LOW TO DEPLOY THE BEACON"; return; }
        lunarBeaconDeployed = true;
        lunarSuitBattery -= 6f;
        flightLink = Mathf.Clamp(flightLink + 14, 0, 100);
        flightScore += 7;
        Material beaconMat = MakeMaterial(new Color32(47, 212, 255, 255), 0.08f, 1.6f);
        Transform beacon = new GameObject(selectedMission == "MARS" ? "DEPLOYED_MARS_WEATHER_NAVIGATION_BEACON" : "DEPLOYED_LUNAR_NAVIGATION_BEACON").transform;
        beacon.SetParent(lunarWorldRoot.transform, false);
        beacon.position = lunarBeaconStationPosition + Vector3.up * 0.2f;
        CreatePrimitiveObject("BeaconBase", PrimitiveType.Cylinder, beacon, new Vector3(0f, 0.15f, 0f), new Vector3(0.55f, 0.15f, 0.55f), beaconMat);
        CreatePrimitiveObject("BeaconMast", PrimitiveType.Cylinder, beacon, new Vector3(0f, 1.15f, 0f), new Vector3(0.09f, 1.0f, 0.09f), beaconMat);
        CreatePrimitiveObject("BeaconPulse", PrimitiveType.Sphere, beacon, new Vector3(0f, 2.0f, 0f), Vector3.one * 0.25f, beaconMat);
        lastFlightOutcome = "Navigation beacon deployed. Surface ranging and local communications improved.";
        flightLog.Add("NAVIGATION BEACON — Local surface reference beacon deployed.");
        PlaySfx(lunarBeaconClip != null ? lunarBeaconClip : radioClip, 0.7f);
        lunarPromptText.text = "NAVIGATION BEACON DEPLOYED  /  LOCAL POSITION REFERENCE ACTIVE";
        UpdateLunarObjectives();
    }

    private void ReturnToLunarLander()
    {
        if (!lunarControlsUnlocked || lunarAstronautObject == null || lunarLanderRoot == null)
        {
            if (lunarPromptText != null) lunarPromptText.text = "EXIT THE LANDER BEFORE USING EXPLORATION TOOLS";
            return;
        }
        float d = Vector3.Distance(GetLunarExplorerPosition(), lunarLanderRoot.position);
        if (d > 6.2f)
        {
            lunarPromptText.text = "LANDER TOO FAR AWAY  /  RETURN TO THE LANDING MODULE";
            return;
        }
        if (lunarSamplesCollected < 3 || !lunarTerrainScanned || !lunarBeaconDeployed)
        {
            lunarPromptText.text = "EXPEDITION INCOMPLETE  /  COLLECT 3 SAMPLES, SCAN TERRAIN AND DEPLOY THE BEACON";
            return;
        }
        if (lunarRoverMode) ToggleLunarRover();
        lunarReturnedToLander = true;
        flightDataPoints += 5;
        flightScore += 10;
        if (HasSelected("COMMUNICATION"))
        {
            transmittedDataPoints = Mathf.Max(transmittedDataPoints, Mathf.Max(5, flightDataPoints / 2));
            flightLink = Mathf.Max(flightLink, 60);
        }
        flightPower = Mathf.Clamp(Mathf.RoundToInt(lunarSuitBattery), 0, 100);
        flightIntegrity = Mathf.Clamp(flightIntegrity + 4, 0, 100);
        missionSuccess = lunarOxygen > 10f && lunarSuitBattery > 8f && flightLink >= 45 && flightIntegrity >= 50 &&
                         lunarSamplesCollected == 3 && lunarTerrainScanned && lunarBeaconDeployed && transmittedDataPoints >= 5;
        missionScore = Mathf.Clamp(flightScore + lunarSamplesCollected * 5 + (lunarTerrainScanned ? 8 : 0) +
                                   (lunarBeaconDeployed ? 6 : 0) + Mathf.RoundToInt((lunarOxygen + lunarSuitBattery) * 0.08f), 0, 100);
        if (missionSuccess) missionScore = Mathf.Max(72, missionScore);
        else missionScore = Mathf.Min(59, missionScore);
        lastFlightOutcome = selectedMission == "MARS" ?
            "Explorer returned to the Mars lander with rock cores and field data." :
            "Crew returned to the lunar lander with the collected samples and field data.";
        flightLog.Add(selectedMission == "MARS" ?
            "MISSION RETURN — Explorer returned to the Mars lander. Rock cores and telemetry secured." :
            "MISSION RETURN — Astronaut returned to the lunar lander. Surface samples and telemetry secured.");
        PlaySfx(radioClip, 0.7f);
        ShowScreen(GameScreen.Results);
    }

    private void UpdateLunarObjectives()
    {
        if (lunarObjectivesText != null)
        {
            lunarObjectivesText.text =
                (lunarSamplesCollected >= 3 ? "OK" : "--") + (selectedMission == "MARS" ? "  Collect Martian rock cores  (" : "  Collect lunar rock samples  (") + lunarSamplesCollected + "/3)\n\n" +
                (lunarTerrainScanned ? "OK" : "--") + (selectedMission == "MARS" ? "  Analyze regolith and search for hydrated minerals\n\n" : "  Analyze terrain at scanner\n\n") +
                (lunarBeaconDeployed ? "OK" : "--") + (selectedMission == "MARS" ? "  Deploy weather and navigation beacon\n\n" : "  Deploy navigation beacon\n\n") +
                (lunarReturnedToLander ? "OK" : "--") + "  Return safely to lander\n\n" +
                "SAMPLES ARE ONLY STORED AFTER COLLECTION.";
        }
        if (lunarSamplesText != null) lunarSamplesText.text = "SAMPLES   " + lunarSamplesCollected + " / 3";
    }

    private void UpdateLunarPrompt()
    {
        if (lunarPromptText == null || lunarAstronautObject == null) return;
        Vector3 player = GetLunarExplorerPosition();
        string prompt = lunarRoverMode ? "ROVER DRIVE  /  WASD STEER  /  SHIFT BOOST  /  Q EXIT" : "WASD MOVE  /  RIGHT MOUSE LOOK  /  Q ROVER";
        if (lunarSamplesCollected < 3)
        {
            float best = 4.2f;
            for (int i = 0; i < lunarSampleMarkers.Length; i++)
            {
                Transform marker = lunarSampleMarkers[i];
                if (marker == null || !marker.gameObject.activeSelf) continue;
                float d = Vector3.Distance(player, marker.position);
                if (d < best) { best = d; prompt = (selectedMission == "MARS" ? "PRESS E TO COLLECT MARTIAN ROCK CORE  /  " : "PRESS E TO COLLECT LUNAR ROCK SAMPLE  /  ") + (lunarSamplesCollected + 1) + " OF 3"; }
            }
        }
        if (!lunarTerrainScanned && Vector3.Distance(player, lunarScanStationPosition) < 5.2f)
            prompt = selectedMission == "MARS" ? "PRESS F TO ANALYZE MARTIAN REGOLITH AND SEARCH FOR HYDRATED MINERALS" : "PRESS F TO ANALYZE CRATER WALLS AND REGOLITH";
        if (!lunarBeaconDeployed && Vector3.Distance(player, lunarBeaconStationPosition) < 4.8f)
            prompt = selectedMission == "MARS" ? "PRESS B TO DEPLOY WEATHER AND NAVIGATION BEACON" : "PRESS B TO DEPLOY THE SURFACE NAVIGATION BEACON";
        if (lunarSamplesCollected >= 3 && lunarTerrainScanned && lunarBeaconDeployed && Vector3.Distance(player, lunarLanderPosition) < 6.2f)
            prompt = "PRESS R TO RETURN TO THE LANDER WITH ALL SCIENCE DATA";
        if (lunarBoundaryWarningTimer > 0.05f || !IsInsideSurfaceBoundary(player.x,player.z,2.2f))
            prompt = selectedMission == "MARS" ? "MARS EVA SAFETY PERIMETER  /  TURN BACK TOWARD THE MARKED SCIENCE ROUTE" : "LUNAR EVA SAFETY PERIMETER  /  TURN BACK TOWARD THE MARKED SCIENCE ROUTE";
        lunarPromptText.text = prompt;

        if (lunarNavigationText != null)
        {
            float closest = float.MaxValue;
            string targetName = "LANDER";
            if (lunarSamplesCollected < 3)
            {
                for (int i = 0; i < lunarSampleMarkers.Length; i++)
                {
                    Transform marker = lunarSampleMarkers[i];
                    if (marker == null || !marker.gameObject.activeSelf) continue;
                    float d = Vector3.Distance(player, marker.position);
                    if (d < closest) { closest = d; targetName = "SAMPLE SITE"; }
                }
            }
            if (!lunarTerrainScanned)
            {
                float d = Vector3.Distance(player, lunarScanStationPosition);
                if (d < closest) { closest = d; targetName = "TERRAIN SCANNER"; }
            }
            if (!lunarBeaconDeployed)
            {
                float d = Vector3.Distance(player, lunarBeaconStationPosition);
                if (d < closest) { closest = d; targetName = "BEACON PAD"; }
            }
            if (targetName == "LANDER") closest = Vector3.Distance(player, lunarLanderPosition);
            lunarNavigationText.text = "NEXT TARGET  /  " + targetName + "  /  " + Mathf.RoundToInt(closest) + " m";
        }
    }

    private void UpdateLunarResourceLabels()
    {
        if (lunarOxygenText != null) lunarOxygenText.text = "OXYGEN   " + Mathf.RoundToInt(lunarOxygen) + "%";
        if (lunarBatteryText != null) lunarBatteryText.text = lunarRoverMode
            ? "ROVER BATTERY   " + Mathf.RoundToInt(lunarRoverBattery) + "%"
            : "SUIT BATTERY   " + Mathf.RoundToInt(lunarSuitBattery) + "%";
        if (lunarSamplesText != null) lunarSamplesText.text = "SAMPLES   " + lunarSamplesCollected + " / 3";
        if (lunarOxygenBarFill != null) lunarOxygenBarFill.fillAmount = Mathf.Clamp01(lunarOxygen / 100f);
        if (lunarBatteryBarFill != null) lunarBatteryBarFill.fillAmount = Mathf.Clamp01((lunarRoverMode ? lunarRoverBattery : lunarSuitBattery) / 100f);
        if (lunarOxygen <= 10f || lunarSuitBattery <= 8f)
            lunarPromptText.text = "WARNING  /  RESOURCES LOW  /  RETURN TO THE LANDER";
    }

    private Image BuildLunarBar(Transform parent, string label, float y, float value, Color tint)
    {
        Label(label, parent, 10, muted, TextAlignmentOptions.Left, new Vector2(310, 18), new Vector2(0.08f, y), Vector2.zero, true);
        Image back = CreateImage("LunarBarBack", parent, new Color32(28, 51, 69, 255));
        RectTransform br = back.rectTransform;
        br.anchorMin = new Vector2(0.08f, y - 0.08f); br.anchorMax = new Vector2(0.91f, y - 0.045f);
        br.offsetMin = br.offsetMax = Vector2.zero;
        Image fill = CreateImage("LunarBarFill", parent, tint);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = Mathf.Clamp01(value);
        RectTransform fr = fill.rectTransform;
        fr.anchorMin = new Vector2(0.08f, y - 0.08f); fr.anchorMax = new Vector2(0.91f, y - 0.045f);
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        return fill;
    }

    private void BuildResults()
    {
        CreateMissionControlBackdrop(currentPage.transform);
        ScreenTitle("MISSION REPORT", selectedMission + " destination  /  interactive prototype simulation");
        GameObject card = Card(currentPage.transform, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.68f), cardColor);
       // AddAccent(card.transform, missionSuccess ? green : red);
        Label(missionSuccess ? "MISSION SUCCESSFUL" : "MISSION COMPROMISED", card.transform, 34,
            missionSuccess ? green : red, TextAlignmentOptions.Center,
            new Vector2(900, 62), new Vector2(0.5f, 0.88f), Vector2.zero, true);
        Label("MISSION SCORE  " + missionScore + " / 100", card.transform, 22, gold, TextAlignmentOptions.Center,
            new Vector2(650, 42), new Vector2(0.5f, 0.77f), Vector2.zero, true);

        MissionStats stats = CalculateStats();
        string report = "DESTINATION: " + selectedMission + "\n" +
                        "FLIGHT DURATION: " + flightDay + " DAYS\n" +
                        "LAUNCH VEHICLE: " + (stats.rocket != null ? stats.rocket.title : "NOT SELECTED") + "\n" +
                        "PAYLOAD MASS: " + stats.mass.ToString("N0") + " kg\n" +
                        "SCIENCE DATA: " + (flightDataPoints * 0.25f).ToString("N1") + " GB\n" +
                        "DATA TRANSMITTED: " + (transmittedDataPoints * 0.25f).ToString("N1") + " GB\n" +
                        "DATA RETURN: " + (transmittedDataPoints >= 5 ? "VERIFIED" : "NOT VERIFIED") + "\n" +
                        "FINAL RESERVES  /  POWER " + flightPower + "%   •   LINK " + flightLink + "%   •   HULL " + flightIntegrity + "%";
        if (selectedMission == "MOON" || selectedMission == "MARS")
            report += "\n" + (selectedMission == "MARS" ? "MARTIAN ROCK CORES: " : "LUNAR SAMPLES: ") + lunarSamplesCollected + "/3\nTERRAIN ANALYSIS: " + (lunarTerrainScanned ? "COMPLETE" : "INCOMPLETE") +
                      "\nNAVIGATION BEACON: " + (lunarBeaconDeployed ? "DEPLOYED" : "NOT DEPLOYED") +
                      "\nRETURN TO LANDER: " + (lunarReturnedToLander ? "CONFIRMED" : "NOT CONFIRMED");
        Label(report, card.transform, 17, Color.white, TextAlignmentOptions.Center,
            new Vector2(1100, 190), new Vector2(0.5f, 0.62f), Vector2.zero, false);

        string log = "MISSION DECISIONS\n";
        for (int i = 0; i < flightLog.Count; i++)
            log += (i + 1) + ". " + flightLog[i].Split(':')[0] + "\n";
        Label(log, card.transform, 12, muted, TextAlignmentOptions.Left,
            new Vector2(1000, 115), new Vector2(0.5f, 0.27f), Vector2.zero, false);
        Button("DESIGN ANOTHER MISSION", currentPage.transform, new Vector2(0.37f, 0.075f), new Vector2(340, 54), cyan,
            ResetGame);
        Button("MAIN MENU", currentPage.transform, new Vector2(0.73f, 0.075f), new Vector2(210, 54), cardAltColor,
            () => ShowScreen(GameScreen.Home));
    }

    private void ResetGame()
    {
        StopAudioLoop();
        budget = StartingBudget;
        foreach (Equipment item in equipment)
        {
            item.owned = false;
            item.selected = false;
        }
        missionReady = false;
        launchRunning = false;
        flightEvents.Clear();
        flightLog.Clear();
        flightEventIndex = 0;
        flightDay = 0;
        flightPower = flightLink = flightIntegrity = flightDataPoints = flightScore = 0;
        flightCommandPoints = 0;
        usedCameraCommand = usedSpectrometerCommand = usedTransmitCommand = usedPowerSaveCommand = false;
        transmittedDataPoints = 0;
        missionScore = 0;
        missionSuccess = false;
        lunarOxygen = 100f;
        lunarSuitBattery = 100f;
        lunarSamplesCollected = 0;
        lunarTerrainScanned = false;
        lunarBeaconDeployed = false;
        lunarReturnedToLander = false;
        lunarRoverMode = false;
        lunarRoverBattery = 100f;
        lunarRoverWheels = new Transform[6];
        lunarLandingRunning = false;
        lunarControlsUnlocked = false;
        lastFlightOutcome = "Flight plan loaded. Awaiting first command.";
        ShowScreen(GameScreen.Missions);
    }

    private void ScreenTitle(string title, string subtitle)
    {
        Label(title, currentPage.transform, 30, Color.white, TextAlignmentOptions.Center,
            new Vector2(1160, 50), new Vector2(0.5f, 0.865f), Vector2.zero, true);
        Label(subtitle, currentPage.transform, 14, muted, TextAlignmentOptions.Center,
            new Vector2(1260, 28), new Vector2(0.5f, 0.815f), Vector2.zero, false);
        BuildProgressStrip();
    }

    private void BuildProgressStrip()
    {
        string[] steps = { "DESTINATION", "EQUIPMENT", "ASSEMBLY", "LAUNCH", "FLIGHT OPS", "SURFACE OPS", "RESULTS" };
        int active = 0;
        switch (currentScreen)
        {
            case GameScreen.Missions: active = 0; break;
            case GameScreen.Store: active = 1; break;
            case GameScreen.Assembly: active = 2; break;
            case GameScreen.Launch: active = 3; break;
            case GameScreen.MissionControl: active = 4; break;
            case GameScreen.MoonLanding: active = 5; break;
            case GameScreen.MoonSurface: active = 5; break;
            case GameScreen.Results: active = 6; break;
            default: return;
        }

        float startX = 0.22f;
        float gap = 0.093f;
        Image track = CreateImage("ProgressTrack", currentPage.transform, new Color32(31, 64, 88, 255));
        RectTransform trackRect = track.rectTransform;
        trackRect.anchorMin = new Vector2(startX, 0.756f);
        trackRect.anchorMax = new Vector2(startX + gap * 6f, 0.759f);
        trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;

        Image completed = CreateImage("ProgressCompleted", currentPage.transform, cyan);
        RectTransform doneRect = completed.rectTransform;
        doneRect.anchorMin = new Vector2(startX, 0.756f);
        doneRect.anchorMax = new Vector2(startX + gap * active, 0.759f);
        doneRect.offsetMin = doneRect.offsetMax = Vector2.zero;

        for (int i = 0; i < steps.Length; i++)
        {
            float x = startX + gap * i;
            bool passed = i < active;
            bool selected = i == active;
            Color tint = passed ? green : (selected ? cyan : muted);
            Image node = CreateImage("ProgressNode_" + i, currentPage.transform,
                selected ? cyan : (passed ? green : new Color32(28, 56, 78, 255)));
            RectTransform nodeRect = node.rectTransform;
            nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(x, 0.7575f);
            nodeRect.sizeDelta = selected ? new Vector2(18, 18) : new Vector2(13, 13);
            nodeRect.anchoredPosition = Vector2.zero;
            Label((i + 1).ToString(), currentPage.transform, 10, Color.white,
                TextAlignmentOptions.Center, new Vector2(25, 20), new Vector2(x, 0.7575f), Vector2.zero, true);
            Label(steps[i], currentPage.transform, 9, tint, TextAlignmentOptions.Center,
                new Vector2(126, 18), new Vector2(x, 0.724f), Vector2.zero, true);
        }
    }

    private void BuildCraftSchematic(Transform parent, Vector2 center)
    {
        Label("LOADOUT BLUEPRINT", parent, 12, cyan, TextAlignmentOptions.Center,
            new Vector2(300, 22), new Vector2(center.x, 0.84f), Vector2.zero, true);

        Image leftWing = CreateImage("SolarWingLeft", parent, new Color32(24, 107, 150, 220));
        RectTransform lw = leftWing.rectTransform;
        lw.anchorMin = lw.anchorMax = new Vector2(center.x - 0.16f, center.y);
        lw.sizeDelta = new Vector2(105, 58);
        lw.anchoredPosition = Vector2.zero;
        Image rightWing = CreateImage("SolarWingRight", parent, new Color32(24, 107, 150, 220));
        RectTransform rw = rightWing.rectTransform;
        rw.anchorMin = rw.anchorMax = new Vector2(center.x + 0.16f, center.y);
        rw.sizeDelta = new Vector2(105, 58);
        rw.anchoredPosition = Vector2.zero;
        Image core = CreateImage("SpacecraftCore", parent, new Color32(150, 178, 197, 255));
        RectTransform cr = core.rectTransform;
        cr.anchorMin = cr.anchorMax = new Vector2(center.x, center.y);
        cr.sizeDelta = new Vector2(118, 62);
        cr.anchoredPosition = Vector2.zero;
        Image nose = CreateImage("SpacecraftNose", parent, gold);
        RectTransform nr = nose.rectTransform;
        nr.anchorMin = nr.anchorMax = new Vector2(center.x, center.y - 0.07f);
        nr.sizeDelta = new Vector2(32, 18);
        nr.anchoredPosition = Vector2.zero;
        Label("MISSION PAYLOAD", parent, 10, muted, TextAlignmentOptions.Center,
            new Vector2(250, 18), new Vector2(center.x, center.y - 0.19f), Vector2.zero, true);
    }

    private void ShowInfo(string title, string message)
    {
        if (currentModal != null) Destroy(currentModal);
        currentModal = new GameObject("ModalOverlay", typeof(RectTransform), typeof(Image));
        currentModal.transform.SetParent(root, false);
        Stretch(currentModal.GetComponent<RectTransform>());
        currentModal.GetComponent<Image>().color = new Color(0.005f, 0.015f, 0.035f, 0.82f);
        GameObject modalCard = Card(currentModal.transform, new Vector2(0.23f, 0.23f), new Vector2(0.77f, 0.77f), cardAltColor);
       // AddAccent(modalCard.transform, cyan);
        Label(title, modalCard.transform, 28, cyan, TextAlignmentOptions.Center,
            new Vector2(760, 58), new Vector2(0.5f, 0.78f), Vector2.zero, true);
        Label(message, modalCard.transform, 18, Color.white, TextAlignmentOptions.Center,
            new Vector2(760, 245), new Vector2(0.5f, 0.50f), Vector2.zero, false);
        Button("CLOSE", modalCard.transform, new Vector2(0.5f, 0.13f), new Vector2(190, 52), cyan,
            () => { if (currentModal != null) Destroy(currentModal); currentModal = null; });
    }

    private GameObject Card(Transform parent, Vector2 min, Vector2 max, Color color)
    {
        Image image = CreateImage("Panel", parent, color);
        //omar zidane

        if (roundedCardSprite == null)
        {
            Debug.Log("Testing");
            roundedCardSprite = Resources.Load<Sprite>("UI/RoundedCard");
        }
        Debug.Log(roundedCardSprite);
        if (roundedCardSprite != null)
        {
            image.sprite = roundedCardSprite;
            image.type = Image.Type.Sliced;
        }
        RectTransform rect = image.rectTransform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Outline outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(cyan.r, cyan.g, cyan.b, 0.30f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
        return image.gameObject;
    }

    private void AddAccent(Transform parent, Color color)
    {
        Image accent = CreateImage("Accent", parent, color);
         RectTransform rect = accent.rectTransform;
         rect.anchorMin = new Vector2(0, 0);
         rect.anchorMax = new Vector2(0, 1);
         rect.sizeDelta = new Vector2(5, 0);
         rect.anchoredPosition = Vector2.zero;
        /*Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = roundedCardSprite;
        cardImage.type = Image.Type.Sliced;

        GameObject innerCard = Card(card.transform,
            new Vector2(0.012f, 0.012f), new Vector2(0.988f, 0.988f), cardColor);*/
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TMP_Text Label(string text, Transform parent, int size, Color color,
        TextAlignmentOptions alignment, Vector2 dimensions, Vector2 anchor, Vector2 offset, bool bold)
    {
        GameObject go = new GameObject("Text_" + (text.Length > 16 ? text.Substring(0, 16) : text),
            typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        if (gameFont != null) label.font = gameFont;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Max(9, size * 0.72f);
        label.fontSizeMax = size;
        label.margin = new Vector4(3, 2, 3, 2);
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.raycastTarget = false;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        // The anchor defines the text position; the pivot should be based on
        // alignment, not copied from the normalized anchor coordinates.
        float pivotX = alignment == TextAlignmentOptions.Left ? 0f :
                       alignment == TextAlignmentOptions.Right ? 1f : 0.5f;
        rect.pivot = new Vector2(pivotX, 0.5f);
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = offset;
        return label;
    }

    private void Button(string text, Transform parent, Vector2 anchor, Vector2 size, Color color,
        UnityEngine.Events.UnityAction action, bool interactable = true)
    {
        string safeButtonName = text.Replace("\n", "_").Replace(" ", "_");
        if (safeButtonName.Length > 48) safeButtonName = safeButtonName.Substring(0, 48);
        GameObject go = new GameObject("Button_" + safeButtonName,
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        // #omar_zidane making roundedbutton sprite "button radius"
        if (roundedButtonSprite == null)
        {
            roundedButtonSprite = Resources.Load<Sprite>("UI/RoundedButton");
            Debug.Log("test");
        }
        Debug.Log(roundedButtonSprite);
        if (roundedButtonSprite != null)
        {
            Debug.Log("test1");
            image.sprite = roundedButtonSprite;
            image.type = Image.Type.Sliced;
        }
       
        image.color = color;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(cyan.r, cyan.g, cyan.b, 0.72f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);
        outline.useGraphicAlpha = true;
        button.onClick.AddListener(() => PlaySfx(uiClickClip, 0.30f));
        button.onClick.AddListener(action);
        button.interactable = interactable;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
        button.colors = colors;

        TMP_Text caption = Label(text, go.transform, 15, Color.white,
            TextAlignmentOptions.Center, size - new Vector2(12, 8), new Vector2(0.5f, 0.5f), Vector2.zero, true);
        caption.raycastTarget = false;
    }

    private void OnDestroy()
    {
        CleanupTransient3D();
        foreach (Texture2D texture in missionPlanetTextureCache.Values)
            if (texture != null) Destroy(texture);
        missionPlanetTextureCache.Clear();
        if (gameAudioSource != null) gameAudioSource.Stop();
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
