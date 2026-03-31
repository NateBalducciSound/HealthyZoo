using UnityEngine;

// Plays a one-shot startup animation then transitions into a looping animation.
// Drop this on an AR character prefab instead of an Animator + Animation Controller.
[RequireComponent(typeof(SpriteRenderer))]
public class LightweightSpriteAnimator : MonoBehaviour
{
    [Header("Startup (plays once)")]
    [Tooltip("Frames for the intro animation — use every Nth frame via the Editor tool to save memory")]
    public Sprite[] startupFrames;

    [Header("Loop (plays after startup)")]
    [Tooltip("Frames for the idle loop")]
    public Sprite[] loopFrames;

    [Header("Settings")]
    [Range(1, 30)]
    public int fps = 12;

    private SpriteRenderer _renderer;
    private int _currentFrame;
    private float _timer;
    private float _frameInterval;
    private bool _inStartup = true;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _frameInterval = 1f / Mathf.Max(fps, 1);
    }

    void OnEnable()
    {
        _currentFrame = 0;
        _timer = 0f;
        _inStartup = startupFrames != null && startupFrames.Length > 0;
        ShowFrame();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < _frameInterval) return;
        _timer -= _frameInterval;

        _currentFrame++;

        if (_inStartup)
        {
            if (_currentFrame >= startupFrames.Length)
            {
                // Startup done — switch to loop
                _inStartup = false;
                _currentFrame = 0;
            }
        }
        else
        {
            if (loopFrames == null || loopFrames.Length == 0) return;
            if (_currentFrame >= loopFrames.Length)
                _currentFrame = 0;
        }

        ShowFrame();
    }

    void ShowFrame()
    {
        Sprite[] active = _inStartup ? startupFrames : loopFrames;
        if (active == null || active.Length == 0) return;
        int idx = Mathf.Clamp(_currentFrame, 0, active.Length - 1);
        _renderer.sprite = active[idx];
    }
}
