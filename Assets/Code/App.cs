using UnityEngine;

public class App : MonoBehaviour
{
    public static App Instance { get; private set; }

    [SerializeField] private ConfigPaths m_configPathConfig;
    [SerializeField] private Camera m_mainCamera;
    [SerializeField] private Transform m_uiRoot;

    public ResourceLoaderModule ResourceLoader { get; private set; }
    public LoadingScreenModule LoadingScreen { get; private set; }

    private GameController m_gameController;
    public GameController GameController => m_gameController;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (m_mainCamera == null)
        {
            m_mainCamera = Camera.main;
        }

        // App-wide resource loader
        ResourceLoader = new ResourceLoaderModule(m_configPathConfig);
        LoadingScreen = new LoadingScreenModule(m_configPathConfig);

        // Session controller
        m_gameController = new GameController(m_mainCamera, m_uiRoot);
    }

    private void Update()
    {
        m_gameController?.Tick(Time.deltaTime);
    }

    private void OnDisable()
    {
        // Any other shutdown you want...

        ResetShaderGlobals();
    }

    private void OnDestroy()
    {
        // In case App gets destroyed instead of just disabled
        ResetShaderGlobals();
    }

    private void ResetShaderGlobals()
    {
        // Put all your obstruction-related globals back to "off"
        Shader.SetGlobalInt("_HoleCount", 0);
        Shader.SetGlobalFloat("_HoleRadius", 0f);
        Shader.SetGlobalFloat("_HoleSoftness", 0f);
    }
}
