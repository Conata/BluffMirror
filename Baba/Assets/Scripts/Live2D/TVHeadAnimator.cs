using UnityEngine;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;

/// <summary>
/// TVHead Live2Dモデルのアニメーション制御。
/// アイドル（呼吸・まばたき・揺れ）と表情変化を管理する。
/// </summary>
public class TVHeadAnimator : MonoBehaviour
{
    [Header("Idle Animation")]
    [SerializeField] private float _breathSpeed = 1.2f;
    [SerializeField] private float _breathAmplitude = 0.4f;
    [SerializeField] private float _swaySpeed = 0.3f;
    [SerializeField] private float _swayAmplitude = 3f;
    [SerializeField] private float _hairSwaySpeed = 0.5f;
    [SerializeField] private float _hairSwayAmplitude = 0.3f;

    [Header("Blink Settings")]
    [SerializeField] private float _blinkInterval = 3.5f;
    [SerializeField] private float _blinkIntervalRandom = 1.5f;
    [SerializeField] private float _blinkDuration = 0.15f;

    [Header("Eye Movement (AngleX/Y/Z = 視線)")]
    [Tooltip("一つ目キャラ: AngleX/Y/Zで目玉の向きを制御")]
    [SerializeField] private float _eyeAngleRange = 30f;
    [Tooltip("視線が次のランダム位置に移るまでの間隔(秒)")]
    [SerializeField] private float _eyeGlanceInterval = 2f;
    [SerializeField] private float _eyeGlanceIntervalRandom = 1.5f;
    [Tooltip("視線のスムージング速度")]
    [SerializeField] private float _eyeLerpSpeed = 6f;
    [Tooltip("ワールド空間のターゲット（nullならアイドル視線）")]
    [SerializeField] private Transform _lookTarget;

    [Header("Expression Transition")]
    [SerializeField] private float _expressionLerpSpeed = 5f;

    // Cached parameters
    private CubismParameter _paramAngleX;
    private CubismParameter _paramAngleY;
    private CubismParameter _paramAngleZ;
    private CubismParameter _paramEyeLOpen;
    private CubismParameter _paramEyeROpen;
    private CubismParameter _paramEyeLSmile;
    private CubismParameter _paramEyeRSmile;
    private CubismParameter _paramBrowLY;
    private CubismParameter _paramBrowRY;
    private CubismParameter _paramBrowLAngle;
    private CubismParameter _paramBrowRAngle;
    private CubismParameter _paramMouthForm;
    private CubismParameter _paramMouthOpenY;
    private CubismParameter _paramCheek;
    private CubismParameter _paramBodyAngleX;
    private CubismParameter _paramBodyAngleY;
    private CubismParameter _paramBodyAngleZ;
    private CubismParameter _paramBreath;
    private CubismParameter _paramHairFront;
    private CubismParameter _paramHairSide;
    private CubismParameter _paramHairBack;

    // Blink state
    private float _blinkTimer;
    private float _nextBlinkTime;
    private float _blinkProgress; // 0=open, 1=closed
    private bool _isBlinking;

    // Expression targets (lerped toward)
    private float _targetEyeSmile;
    private float _targetBrowY;
    private float _targetBrowAngle;
    private float _targetMouthForm;
    private float _targetMouthOpen;
    private float _targetCheek;

    // Current expression values
    private float _currentEyeSmile;
    private float _currentBrowY;
    private float _currentBrowAngle;
    private float _currentMouthForm;
    private float _currentMouthOpen;
    private float _currentCheek;

    // Eye movement state (AngleX/Y/Z values, typically -30~30)
    private float _currentAngleX;
    private float _currentAngleY;
    private float _currentAngleZ;
    private float _targetAngleX;
    private float _targetAngleY;
    private float _targetAngleZ;
    private float _eyeGlanceTimer;
    private float _nextGlanceTime;

    // Time offsets for varied sine waves
    private float _timeOffset;

    private CubismModel _model;

    private void Start()
    {
        _model = GetComponent<CubismModel>();
        if (_model == null)
        {
            Debug.LogError("TVHeadAnimator: CubismModel not found on this GameObject.");
            enabled = false;
            return;
        }

        CacheParameters();
        _timeOffset = Random.Range(0f, 100f);
        _nextBlinkTime = Random.Range(_blinkInterval - _blinkIntervalRandom, _blinkInterval + _blinkIntervalRandom);
        _nextGlanceTime = Random.Range(_eyeGlanceInterval - _eyeGlanceIntervalRandom, _eyeGlanceInterval + _eyeGlanceIntervalRandom);
        PickNewGlanceTarget();
    }

    private void CacheParameters()
    {
        var parameters = _model.Parameters;
        foreach (var p in parameters)
        {
            switch (p.Id)
            {
                case "ParamAngleX": _paramAngleX = p; break;
                case "ParamAngleY": _paramAngleY = p; break;
                case "ParamAngleZ": _paramAngleZ = p; break;
                case "ParamEyeLOpen": _paramEyeLOpen = p; break;
                case "ParamEyeROpen": _paramEyeROpen = p; break;
                case "ParamEyeLSmile": _paramEyeLSmile = p; break;
                case "ParamEyeRSmile": _paramEyeRSmile = p; break;
                case "ParamBrowLY": _paramBrowLY = p; break;
                case "ParamBrowRY": _paramBrowRY = p; break;
                case "ParamBrowLAngle": _paramBrowLAngle = p; break;
                case "ParamBrowRAngle": _paramBrowRAngle = p; break;
                case "ParamMouthForm": _paramMouthForm = p; break;
                case "ParamMouthOpenY": _paramMouthOpenY = p; break;
                case "ParamCheek": _paramCheek = p; break;
                case "ParamBodyAngleX": _paramBodyAngleX = p; break;
                case "ParamBodyAngleY": _paramBodyAngleY = p; break;
                case "ParamBodyAngleZ": _paramBodyAngleZ = p; break;
                case "ParamBreath": _paramBreath = p; break;
                case "ParamHairFront": _paramHairFront = p; break;
                case "ParamHairSide": _paramHairSide = p; break;
                case "ParamHairBack": _paramHairBack = p; break;
            }
        }
    }

    private void LateUpdate()
    {
        if (_model == null) return;

        float t = Time.time + _timeOffset;

        UpdateIdle(t);
        UpdateBlink();
        UpdateEyeMovement();
        UpdateExpression();
    }

    private void UpdateIdle(float t)
    {
        // Breathing
        if (_paramBreath != null)
        {
            float breath = (Mathf.Sin(t * _breathSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            _paramBreath.Value = breath * _breathAmplitude;
        }

        // Body sway (AngleX/Y/Zは視線専用なのでBodyAngleのみ)
        if (_paramBodyAngleX != null)
            _paramBodyAngleX.Value = Mathf.Sin(t * _swaySpeed) * _swayAmplitude;
        if (_paramBodyAngleY != null)
            _paramBodyAngleY.Value = Mathf.Sin(t * _swaySpeed * 0.9f + 2.1f) * _swayAmplitude * 0.3f;
        if (_paramBodyAngleZ != null)
            _paramBodyAngleZ.Value = Mathf.Sin(t * _swaySpeed * 0.7f + 1.3f) * _swayAmplitude * 0.3f;

        // Hair physics
        if (_paramHairFront != null)
            _paramHairFront.Value = Mathf.Sin(t * _hairSwaySpeed * 1.1f) * _hairSwayAmplitude;
        if (_paramHairSide != null)
            _paramHairSide.Value = Mathf.Sin(t * _hairSwaySpeed * 0.8f + 1.5f) * _hairSwayAmplitude;
        if (_paramHairBack != null)
            _paramHairBack.Value = Mathf.Sin(t * _hairSwaySpeed * 0.9f + 3.0f) * _hairSwayAmplitude;
    }

    private void UpdateBlink()
    {
        if (_paramEyeLOpen == null || _paramEyeROpen == null) return;

        if (_isBlinking)
        {
            _blinkProgress += Time.deltaTime / _blinkDuration;
            float eyeValue;
            if (_blinkProgress < 0.5f)
            {
                // Closing
                eyeValue = 1f - (_blinkProgress * 2f);
            }
            else if (_blinkProgress < 1f)
            {
                // Opening
                eyeValue = (_blinkProgress - 0.5f) * 2f;
            }
            else
            {
                eyeValue = 1f;
                _isBlinking = false;
                _blinkTimer = 0f;
                _nextBlinkTime = Random.Range(
                    _blinkInterval - _blinkIntervalRandom,
                    _blinkInterval + _blinkIntervalRandom);
            }

            _paramEyeLOpen.Value = eyeValue;
            _paramEyeROpen.Value = eyeValue;
        }
        else
        {
            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= _nextBlinkTime)
            {
                _isBlinking = true;
                _blinkProgress = 0f;
            }
            else
            {
                _paramEyeLOpen.Value = 1f;
                _paramEyeROpen.Value = 1f;
            }
        }
    }

    private void UpdateEyeMovement()
    {
        if (_paramAngleX == null) return;

        float range = _eyeAngleRange;

        if (_lookTarget != null)
        {
            // ワールドターゲット追跡: モデルからの相対方向をAngle値に変換
            Vector3 dir = _lookTarget.position - transform.position;
            Vector3 localDir = transform.InverseTransformDirection(dir.normalized);
            _targetAngleX = Mathf.Clamp(localDir.x * range * 2f, -range, range);
            _targetAngleY = Mathf.Clamp(localDir.y * range * 2f, -range, range);
            _targetAngleZ = 0f;
        }
        else
        {
            // アイドル視線: ランダムにキョロキョロ
            _eyeGlanceTimer += Time.deltaTime;
            if (_eyeGlanceTimer >= _nextGlanceTime)
            {
                PickNewGlanceTarget();
                _eyeGlanceTimer = 0f;
                _nextGlanceTime = Random.Range(
                    _eyeGlanceInterval - _eyeGlanceIntervalRandom,
                    _eyeGlanceInterval + _eyeGlanceIntervalRandom);
            }
        }

        // 微細な揺れを加算（生きてる感）
        float t = Time.time + _timeOffset;
        float microX = Mathf.Sin(t * 1.7f + 4.1f) * 1.5f;
        float microY = Mathf.Sin(t * 1.3f + 1.9f) * 1.0f;
        float microZ = Mathf.Sin(t * 0.9f + 3.3f) * 0.8f;

        // スムーズ補間
        float dt = Time.deltaTime * _eyeLerpSpeed;
        _currentAngleX = Mathf.Lerp(_currentAngleX, _targetAngleX + microX, dt);
        _currentAngleY = Mathf.Lerp(_currentAngleY, _targetAngleY + microY, dt);
        _currentAngleZ = Mathf.Lerp(_currentAngleZ, _targetAngleZ + microZ, dt);

        _paramAngleX.Value = Mathf.Clamp(_currentAngleX, -range, range);
        if (_paramAngleY != null)
            _paramAngleY.Value = Mathf.Clamp(_currentAngleY, -range, range);
        if (_paramAngleZ != null)
            _paramAngleZ.Value = Mathf.Clamp(_currentAngleZ, -range * 0.5f, range * 0.5f);
    }

    private void PickNewGlanceTarget()
    {
        float range = _eyeAngleRange;
        // ランダムな視線位置（X方向広め、Y方向やや狭め）
        _targetAngleX = Random.Range(-range, range);
        _targetAngleY = Random.Range(-range * 0.6f, range * 0.6f);
        _targetAngleZ = Random.Range(-range * 0.2f, range * 0.2f);
    }

    private void UpdateExpression()
    {
        float dt = Time.deltaTime * _expressionLerpSpeed;

        _currentEyeSmile = Mathf.Lerp(_currentEyeSmile, _targetEyeSmile, dt);
        _currentBrowY = Mathf.Lerp(_currentBrowY, _targetBrowY, dt);
        _currentBrowAngle = Mathf.Lerp(_currentBrowAngle, _targetBrowAngle, dt);
        _currentMouthForm = Mathf.Lerp(_currentMouthForm, _targetMouthForm, dt);
        _currentMouthOpen = Mathf.Lerp(_currentMouthOpen, _targetMouthOpen, dt);
        _currentCheek = Mathf.Lerp(_currentCheek, _targetCheek, dt);

        if (_paramEyeLSmile != null) _paramEyeLSmile.Value = _currentEyeSmile;
        if (_paramEyeRSmile != null) _paramEyeRSmile.Value = _currentEyeSmile;
        if (_paramBrowLY != null) _paramBrowLY.Value = _currentBrowY;
        if (_paramBrowRY != null) _paramBrowRY.Value = _currentBrowY;
        if (_paramBrowLAngle != null) _paramBrowLAngle.Value = _currentBrowAngle;
        if (_paramBrowRAngle != null) _paramBrowRAngle.Value = _currentBrowAngle;
        if (_paramMouthForm != null) _paramMouthForm.Value = _currentMouthForm;
        if (_paramMouthOpenY != null) _paramMouthOpenY.Value = _currentMouthOpen;
        if (_paramCheek != null) _paramCheek.Value = _currentCheek;
    }

    // === Public API for game integration ===

    /// <summary>ニュートラル表情にリセット</summary>
    public void SetNeutral()
    {
        _targetEyeSmile = 0f;
        _targetBrowY = 0f;
        _targetBrowAngle = 0f;
        _targetMouthForm = 0f;
        _targetMouthOpen = 0f;
        _targetCheek = 0f;
    }

    /// <summary>嬉しい・自信のある表情</summary>
    public void SetHappy()
    {
        _targetEyeSmile = 0.8f;
        _targetBrowY = 0.3f;
        _targetBrowAngle = 0f;
        _targetMouthForm = 0.8f;
        _targetMouthOpen = 0.2f;
        _targetCheek = 0.6f;
    }

    /// <summary>悲しい・落胆した表情</summary>
    public void SetSad()
    {
        _targetEyeSmile = 0f;
        _targetBrowY = -0.5f;
        _targetBrowAngle = -0.5f;
        _targetMouthForm = -0.5f;
        _targetMouthOpen = 0.1f;
        _targetCheek = 0f;
    }

    /// <summary>緊張・焦りの表情</summary>
    public void SetNervous()
    {
        _targetEyeSmile = 0f;
        _targetBrowY = 0.5f;
        _targetBrowAngle = 0.3f;
        _targetMouthForm = -0.3f;
        _targetMouthOpen = 0.3f;
        _targetCheek = 0f;
    }

    /// <summary>不敵な笑み（ブラフ時）</summary>
    public void SetSmirk()
    {
        _targetEyeSmile = 0.4f;
        _targetBrowY = -0.2f;
        _targetBrowAngle = 0.3f;
        _targetMouthForm = 0.5f;
        _targetMouthOpen = 0.1f;
        _targetCheek = 0.3f;
    }

    /// <summary>驚きの表情</summary>
    public void SetSurprised()
    {
        _targetEyeSmile = 0f;
        _targetBrowY = 0.8f;
        _targetBrowAngle = 0f;
        _targetMouthForm = 0f;
        _targetMouthOpen = 0.7f;
        _targetCheek = 0f;
    }

    /// <summary>怒りの表情</summary>
    public void SetAngry()
    {
        _targetEyeSmile = 0f;
        _targetBrowY = -0.3f;
        _targetBrowAngle = -0.7f;
        _targetMouthForm = -0.6f;
        _targetMouthOpen = 0.2f;
        _targetCheek = 0f;
    }

    /// <summary>視線の方向を手動設定 (-1~1)。ターゲットをクリアする。</summary>
    public void SetLookDirection(float x, float y)
    {
        _lookTarget = null;
        float range = _eyeAngleRange;
        _targetAngleX = Mathf.Clamp(x, -1f, 1f) * range;
        _targetAngleY = Mathf.Clamp(y, -1f, 1f) * range;
        _targetAngleZ = 0f;
    }

    /// <summary>ワールド空間のターゲットを視線で追跡する。nullでアイドル視線に戻る。</summary>
    public void SetLookTarget(Transform target)
    {
        _lookTarget = target;
    }

    /// <summary>口の開閉を直接制御（リップシンク用）</summary>
    public void SetMouthOpen(float value)
    {
        _targetMouthOpen = Mathf.Clamp01(value);
    }

    /// <summary>パラメータを個別に設定</summary>
    public void SetExpressionValues(
        float? eyeSmile = null,
        float? browY = null,
        float? browAngle = null,
        float? mouthForm = null,
        float? mouthOpen = null,
        float? cheek = null)
    {
        if (eyeSmile.HasValue) _targetEyeSmile = eyeSmile.Value;
        if (browY.HasValue) _targetBrowY = browY.Value;
        if (browAngle.HasValue) _targetBrowAngle = browAngle.Value;
        if (mouthForm.HasValue) _targetMouthForm = mouthForm.Value;
        if (mouthOpen.HasValue) _targetMouthOpen = mouthOpen.Value;
        if (cheek.HasValue) _targetCheek = cheek.Value;
    }

    /// <summary>
    /// Phase 7-2: Curious表情（興味を持った状態）
    /// </summary>
    public void SetCurious()
    {
        _targetEyeSmile = 0f;
        _targetBrowY = 0.3f; // 眉を少し上げる
        _targetBrowAngle = 0f;
        _targetMouthForm = 0f;
        _targetMouthOpen = 0f;
        _targetCheek = 0f;
    }

    /// <summary>
    /// Phase 7-2: Focused表情（集中・分析中）
    /// </summary>
    public void SetFocused()
    {
        _targetEyeSmile = -0.3f; // 目を細める
        _targetBrowY = -0.2f; // 眉を下げる
        _targetBrowAngle = 0f;
        _targetMouthForm = 0f;
        _targetMouthOpen = 0f;
        _targetCheek = 0f;
    }
}
