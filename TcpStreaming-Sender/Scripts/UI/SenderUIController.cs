using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using static StreamSettings;

[System.Serializable]
public class StreamSettings
{
    public int ResX;
    public int ResY;

    public int FrameRate;

    public enum ColorDepthEnum
    {
        ARGB32,
        RGB565,
        R8
    }
    public ColorDepthEnum ColorDepth;

    public float Delay;

    public enum CodecEnum
    {
        MJPG,
        MGP
    }

    public CodecEnum Codec;

    public RenderTextureFormat GetRenderTextureFormat()
    {
        switch (ColorDepth)
        {
            case ColorDepthEnum.ARGB32:
                return RenderTextureFormat.ARGB32;
            case ColorDepthEnum.RGB565:
                return RenderTextureFormat.RGB565;
            case ColorDepthEnum.R8:
                return RenderTextureFormat.R8;
            default:
                Debug.LogError("Invalid Color Depth selected. Defaulting to ARGB32.");
                return RenderTextureFormat.ARGB32;
        }
    }

    public TextureFormat GetTextureFormat()
    {
        switch (ColorDepth)
        {
            case ColorDepthEnum.ARGB32:
                return TextureFormat.ARGB32;
            case ColorDepthEnum.RGB565:
                return TextureFormat.RGB565;
            case ColorDepthEnum.R8:
                return TextureFormat.R8;
            default:
                Debug.LogError("Invalid Color Depth selected. Defaulting to ARGB32.");
                return TextureFormat.ARGB32;
        }
    }

    public override string ToString()
    {
        return $"Resolution: {ResX}x{ResY}, " +
               $"FrameRate: {FrameRate} FPS, " +
               $"ColorDepth: {ColorDepth}, " +
               $"Delay: {Delay} s, " +
               $"Codec: {Codec}";
    }
}

public class SenderUIController : MonoBehaviour
{

    [Header("Server")]
    [SerializeField] private Sender _sender;
    [SerializeField] private float _updateCheckWaitTime;

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
    [SerializeField] private Toggle _usePresetsWithBitrate;

    [SerializeField] private Dropdown _codec;

    [SerializeField] private Dropdown _frameRate;

    [Header("Control buttons")]
    [SerializeField] private GameObject _controlButtonsGameObject;
    [SerializeField] private Button _startStreamButton;
    [SerializeField] private Button _endStreamButton;

    private StreamSettings _streamSettings;
    private float _serverStartTime;

    private IEnumerator _updateServerInfoCoroutine;

    private void Start()
    {
        _updateServerInfoCoroutine = UpdateServerInfo();

        ConfigureSlider(_delay, _delayStartLabel, _delayEndLabel, _delayCurrentLabel);
        ConfigureBitrate();

        _usePresetsWithBitrate.onValueChanged.AddListener(y => OnPresetsWithBitrateToggle());
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

    private void OnPresetsWithBitrateToggle()
    {
        if (_usePresetsWithBitrate.isOn)
        {
            _bitrate.interactable = true;

            _resolution.interactable = false;
            _colorDepth.interactable = false;
            _codec.interactable = false;
            _frameRate.interactable = false;
        }
        else
        {
            _bitrate.interactable = false;

            _resolution.interactable = true;
            _colorDepth.interactable = true;
            _codec.interactable = true;
            _frameRate.interactable = true;
        }
    }

    private void StartStreamButtonClicked()
    {
        UpdateStreamSettings();
        
        _serverStartTime = Time.time;
        StartCoroutine(_updateServerInfoCoroutine);

        _settingsGameObject.SetActive(false);
        _infoGameObject.SetActive(true);

        _startStreamButton.gameObject.SetActive(false);
        _endStreamButton.gameObject.SetActive(true);

        _translationOff.gameObject.SetActive(false);
        _translationOn.gameObject.SetActive(true);

        _sender.StartSendingFrames();
        Debug.Log($"Start stream button clicked!");
        Debug.Log($"Stream settings: {_streamSettings}");
    }

    private void StopStreamButtonClicked()
    {
        _sender.StopSendingFrames();
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
            _IPValue.text = $"{_sender.GetIP()}:{_sender.GetPort()}";
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

    private void ConfigureBitrate()
    {
        
        _bitrateStartLabel.text = "Низкий";
        _bitrateEndLabel.text = "Высокий";
        BitrateSliderUpdate();
        _bitrate.onValueChanged.AddListener(f => BitrateSliderUpdate());
    }

    private void BitrateSliderUpdate()
    {
        switch (_bitrate.value)
        {
            case 1:
                _bitrateCurrentLabel.text = "100 Кб/С";
                break;
            case 2:
                _bitrateCurrentLabel.text = "500 Кб/С";
                break;
            case 3:
                _bitrateCurrentLabel.text = "1000 Кб/С";
                break;
            default:
                _bitrateCurrentLabel.text = "Неизвестно";
                break;
        }
    }

    private StreamSettings GetBitratePreset()
    {
        switch (_bitrate.value)
        {
            case 1: //100 Kbps
                return new StreamSettings { 
                    ResX = 640,
                    ResY = 480,
                    FrameRate = 30,
                    ColorDepth = ColorDepthEnum.RGB565,
                    Delay = _delay.value,
                    Codec = CodecEnum.MJPG
                    };
                case 2: //500 Kbps
                return new StreamSettings
                {
                    ResX = 1280,
                    ResY = 720,
                    FrameRate = 30,
                    ColorDepth = ColorDepthEnum.RGB565,
                    Delay = _delay.value,
                    Codec = CodecEnum.MJPG
                };
                case 3: //1000 Kbps
                return new StreamSettings
                {
                    ResX = 1920,
                    ResY = 1080,
                    FrameRate = 60,
                    ColorDepth = ColorDepthEnum.ARGB32,
                    Delay = _delay.value,
                    Codec = CodecEnum.MGP
                };
                default:
                return new StreamSettings
                {
                    ResX = 1920,
                    ResY = 1080,
                    FrameRate = 30,
                    ColorDepth = ColorDepthEnum.ARGB32,
                    Delay = _delay.value,
                    Codec = CodecEnum.MJPG
                };
        }
    }

    private void SliderUpdate(Slider slider, Text curValLabel)
    {
        float val = slider.value;
        curValLabel.text = val.ToString();
    }

    private void UpdateStreamSettings()
    {
        if (_usePresetsWithBitrate.isOn)
        {
            _sender.UpdateSettings(GetBitratePreset());
            return;
        }

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

        ColorDepthEnum colorDepth = GetColorDepth();
        if (fps == -1)
        {
            Debug.LogError("Invalid Color Depth");
        }

        float delay = _delay.value;

        CodecEnum codec = GetCodec();

        streamSettings.ResX = (int)res.x;
        streamSettings.ResY = (int)res.y;
        streamSettings.FrameRate = fps;
        streamSettings.ColorDepth = colorDepth;
        streamSettings.Delay = delay;
        streamSettings.Codec = codec;

        _streamSettings = streamSettings;

        _sender.UpdateSettings(streamSettings);
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

    private ColorDepthEnum GetColorDepth()
    {
        switch (_colorDepth.value)
        {
            case 0:
                return ColorDepthEnum.ARGB32;
            case 2:
                return ColorDepthEnum.RGB565;
            case 3:
                return ColorDepthEnum.R8;
            default:
                Debug.LogError("Invalid Color Depth selected. Switched to ARGB32");
                return ColorDepthEnum.ARGB32;

        }
    }

    private CodecEnum GetCodec()
    {
        switch (_codec.value)
        {
            case 0:
                return CodecEnum.MJPG;
            case 1:
                return CodecEnum.MGP;
            default:
                Debug.LogError("Invalid Codec selected. Switched to MJPG");
                return CodecEnum.MJPG;
        }
    }


}
