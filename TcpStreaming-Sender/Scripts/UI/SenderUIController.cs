using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct StreamSettings
{
    public int ResX;
    public int ResY;

    public int FrameRate;

    public int ColorDepth;

    public float Delay;

    public float BitRate;

    public int Codec;

    public override string ToString()
    {
        return $"Resolution: {ResX}x{ResY}, " +
               $"FrameRate: {FrameRate} FPS, " +
               $"ColorDepth: {ColorDepth}, " +
               $"Delay: {Delay} s, " +
               $"BitRate: {BitRate} Kbps, " +
               $"Codec: {Codec}";
    }
}

public class SenderUIController : MonoBehaviour
{
    [SerializeField] private GameObject _canvasGameObject;

    [Header("Stream Status")]
    [SerializeField] private GameObject _statusGameObject;
    [SerializeField] private Text _translationOff;
    [SerializeField] private Text _translationOn;

    [Header("Stream Status info")]
    [SerializeField] private GameObject _infoGameObject;
    [SerializeField] private Text _IPValue;
    [SerializeField] private Text _timerValue;
    [SerializeField] private Text _currentBitrateValue;

    [Header("Settings")]
    [SerializeField] private GameObject _settingsGameObject;

    [SerializeField] private Dropdown _resolution;

    [SerializeField] private Dropdown _colorDepth;

    [SerializeField] private Slider _delay;
    [SerializeField] private Text _delayStartLabel;
    [SerializeField] private Text _delayEndLabel;
    [SerializeField] private Text _delayCurrentLabel;

    [SerializeField] private Slider _bitrate;
    [SerializeField] private Text _bitrateStartLabel;
    [SerializeField] private Text _bitrateEndLabel;
    [SerializeField] private Text _bitrateCurrentLabel;

    [SerializeField] private Dropdown _codec;

    [SerializeField] private Dropdown _frameRate;

    [Header("Control buttons")]
    [SerializeField] private GameObject _controlButtonsGameObject;
    [SerializeField] private Button _startStreamButton;
    [SerializeField] private Button _endStreamButton;

    [Header("Server")]
    [SerializeField] private TextureSenderServer _server;
    [SerializeField] private float _updateCheckWaitTime; 

    private StreamSettings _streamSettings;
    private float _serverStartTime;

    private IEnumerator _updateServerInfoCoroutine;

    private void Start()
    {
        _updateServerInfoCoroutine = UpdateServerInfo();

        ConfigureSlider(_delay, _delayStartLabel, _delayEndLabel, _delayCurrentLabel);
        ConfigureSlider(_bitrate, _bitrateStartLabel, _bitrateEndLabel, _bitrateCurrentLabel);

        _startStreamButton.onClick.AddListener(StartStreamButtonClicked);
        _endStreamButton.onClick.AddListener(StopStreamButtonClicked);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _canvasGameObject.SetActive(!_canvasGameObject.activeSelf);
        }
    }

    private void StartStreamButtonClicked()
    {
        _server.StartConnection();
        _serverStartTime = Time.time;
        StartCoroutine(_updateServerInfoCoroutine);

        UpdateStreamSettings();
        _settingsGameObject.SetActive(false);
        _infoGameObject.SetActive(true);

        _startStreamButton.gameObject.SetActive(false);
        _endStreamButton.gameObject.SetActive(true);

        _translationOff.gameObject.SetActive(false);
        _translationOn.gameObject.SetActive(true);

        Debug.Log($"Start stream button clicked!");
        Debug.Log($"Stream settings: {_streamSettings}");
    }

    private void StopStreamButtonClicked()
    {
        _server.StopConnection();
        StopCoroutine(_updateServerInfoCoroutine);

        _infoGameObject.SetActive(false);
        _settingsGameObject.SetActive(true);

        _endStreamButton.gameObject.SetActive(false);
        _startStreamButton.gameObject.SetActive(true);

        _translationOn.gameObject.SetActive(false);
        _translationOff.gameObject.SetActive(true);

        Debug.Log($"Stop stream button clicked!");
    }

    private IEnumerator UpdateServerInfo()
    {
        while (true)
        {
            _IPValue.text = $"{_server.listenIp}:{_server.port}";
            _timerValue.text = $"{(int)(Time.time - _serverStartTime) / 3600:D2}:{(int)(Time.time - _serverStartTime) / 60 % 60:D2}:{(int)(Time.time - _serverStartTime) % 60:D2}";
            _currentBitrateValue.text = $"NaN";
            yield return new WaitForSeconds(_updateCheckWaitTime);
        }
    }

    private void ConfigureSlider(Slider slider, Text startValLabel, Text endValLabel, Text curValLabel)
    {
        startValLabel.text = slider.minValue.ToString();
        endValLabel.text = slider.maxValue.ToString();
        curValLabel.text = slider.value.ToString();
        slider.onValueChanged.AddListener(delegate { SliderUpdate(slider, curValLabel); });
    }

    private void SliderUpdate(Slider slider, Text curValLabel)
    {
        float val = slider.value;
        curValLabel.text = val.ToString();
    }

    private void UpdateStreamSettings()
    {
        StreamSettings streamSettings = new StreamSettings();
        Vector2 res = GetResolution();
        if (res == Vector2.zero) {
            Debug.LogError("Invalid resolution");
        }

        int fps = GetFrameRate();
        if (fps == -1)
        {
            Debug.LogError("Invalid Fps");
        }

        int colorDepth = GetColorDepth();
        if (fps == -1)
        {
            Debug.LogError("Invalid Color Depth");
        }

        float delay = _delay.value;
        float bitrate = _bitrate.value;

        int codec = GetCodec();
        if (codec == -1) {
            Debug.LogError("Invalid Codec");
        }

        streamSettings.ResX = (int)res.x;
        streamSettings.ResY = (int)res.y;
        streamSettings.FrameRate = fps;
        streamSettings.ColorDepth = colorDepth;
        streamSettings.Delay = delay;
        streamSettings.BitRate = bitrate;
        streamSettings.Codec = codec;

        _streamSettings = streamSettings;
    }
    
    private int GetFrameRate()
    {
        switch ( _frameRate.value )
        {
            case 0:
                return 30;
            case 1:
                return 60;
        }

        return -1;
    }

    private Vector2 GetResolution()
    {
        switch (_resolution.value ) 
        { 
            case 0:
                return new Vector2(1920, 1080);
            case 1:
                return new Vector2(1280, 720);
        }
        return Vector2.zero;
    }

    private int GetColorDepth()
    {
        switch (_colorDepth.value)
        {
            case 0:
            case 1:
                return _colorDepth.value;
        }

        return -1;
    }

    private int GetCodec()
    {
        switch (_codec.value)
        {
            case 0:
            case 1:
                return _codec.value;
        }

        return -1;
    }


}
