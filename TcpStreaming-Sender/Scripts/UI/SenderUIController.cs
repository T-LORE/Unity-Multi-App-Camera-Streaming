using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using static StreamSettings;

[System.Serializable]
public class StreamSettings
{
    public int ResX;
    public int ResY;

    public int FrameRate;

    public int MJPGQuality = 70;

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
    [SerializeField] private float _averageBitratePeriod = 5f;

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
    [SerializeField] private Text _connectedReceivers;

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

    [SerializeField] private Toggle _isAutoAddress;
    [SerializeField] private GameObject _addressConfiguratorGameObject;
    [SerializeField] private InputField _portInput;
    [SerializeField] private InputField _ipAddressInput;
    [SerializeField] private Text _errorField;

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
        _isAutoAddress.onValueChanged.AddListener((val) =>
        {
            if (_isAutoAddress.isOn)
            {
                _addressConfiguratorGameObject.SetActive(false);
            }
            else
            {
                _addressConfiguratorGameObject.SetActive(true);
            }
        });
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

    private List<string> ValidateAddress()
    {
        string ip = _ipAddressInput.text.Trim();
        string port = _portInput.text.Trim();

        var errors = new List<string>();

        if (!Regex.IsMatch(ip, @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
        {
            errors.Add("Неверный IP адресс" );
        }

        if (!Regex.IsMatch(port, @"^\d{1,5}$") || !ushort.TryParse(port, out ushort portValue) || portValue == 0)
        {
            errors.Add("Неверный порт");
        }

        return errors;
    }

    private void StartStreamButtonClicked()
    {
        UpdateStreamSettings();
        bool res;

        if (_isAutoAddress.isOn)
        {
            res = _sender.StartSendingFramesAutoIP();

            if (!res)
            {
                _errorField.text = "Ошибка при запуске сервера на автоматическом IP, используйте ручной ввод";
                Debug.LogWarning("Error starting server on auto IP, please use manual input");
                return;
            }
        } else
        {
            string errorText = "";

            if (ValidateAddress().Count > 0)
            {
                foreach (var error in ValidateAddress())
                {
                    errorText += error + "\n";
                    Debug.LogError(error);
                }

                _errorField.text = errorText;

                return;
            }
            

            res = _sender.StartSendingFrames(_ipAddressInput.text.Trim(), _portInput.text.Trim());

            if (!res)
            {
                _errorField.text = "Ошибка при запуске сервера на адресе " + _ipAddressInput.text.Trim() + ":" + _portInput.text.Trim() + "\n пожалуйста используйте локальный IP адресс вашей машины";
                Debug.LogWarning($"Error starting server on address {_ipAddressInput.text.Trim()}:{_portInput.text.Trim()}");
                return;
            }
        }

        _serverStartTime = Time.time;

        _settingsGameObject.SetActive(false);
        _infoGameObject.SetActive(true);

        _startStreamButton.gameObject.SetActive(false);
        _endStreamButton.gameObject.SetActive(true);

        _translationOff.gameObject.SetActive(false);
        _translationOn.gameObject.SetActive(true);

        StartCoroutine(_updateServerInfoCoroutine);
        Debug.Log($"Start stream button clicked!");
        Debug.Log($"Stream settings: {_streamSettings}");
        _errorField.text = "";
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
            
            float avgKbps = _sender.GetAverageBytesPerSecond(_averageBitratePeriod) / 1024;
            
            avgKbps = Mathf.Round(avgKbps * 100f) / 100f;

            _currentBitrateValue.text = $"{avgKbps} Кб/с";

            _connectedReceivers.text = _sender.GetRecieversAmount().ToString();
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
                _bitrateCurrentLabel.text = "200 Кб/С";
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
            case 1: //200 Kbps
                return new StreamSettings { 
                    ResX = 640,
                    ResY = 360,
                    MJPGQuality = 30,
                    FrameRate = 24,
                    ColorDepth = ColorDepthEnum.RGB565,
                    Delay = _delay.value,
                    Codec = CodecEnum.MJPG
                    };
                case 2: //500 Kbps
                return new StreamSettings
                {
                    ResX = 854,
                    ResY = 480,
                    MJPGQuality = 70,
                    FrameRate = 30,
                    ColorDepth = ColorDepthEnum.ARGB32,
                    Delay = _delay.value,
                    Codec = CodecEnum.MJPG
                };
                case 3: //1000 Kbps
                return new StreamSettings
                {
                    ResX = 1280,
                    ResY = 720,
                    FrameRate = 55,
                    MJPGQuality = 80,
                    ColorDepth = ColorDepthEnum.ARGB32,
                    Delay = _delay.value,
                    Codec = CodecEnum.MJPG
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
                return new Vector2(1280, 720);
            case 1:
                return new Vector2(640, 360);
        }
        return Vector2.zero;
    }

    private ColorDepthEnum GetColorDepth()
    {
        switch (_colorDepth.value)
        {
            case 0:
                return ColorDepthEnum.ARGB32;
            case 1:
                return ColorDepthEnum.RGB565;
            case 2:
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
