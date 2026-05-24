using Oculus.Interaction;
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 이 컴포넌트는 카메라의 near plane에 비네트를 그립니다.
/// 비네트의 방향은 조정 가능하며, 꼭 가운데에 위치할 필요는 없고
/// 뒤쪽을 가리켜서 완전히 채워진 원을 그릴 수도 있습니다.
/// </summary>
public class TunnelingEffect : MonoBehaviour
{
    /// <summary>
    /// 왼쪽 눈, IPD(눈 사이 거리)를 계산하는 데 사용됩니다.
    /// </summary>
    [Header("Mask Setup")]
    [SerializeField]
    private Transform _leftEyeAnchor;

    /// <summary>
    /// 오른쪽 눈, IPD(눈 사이 거리)를 계산하는 데 사용됩니다.
    /// </summary>
    [SerializeField]
    private Transform _rightEyeAnchor;

    /// <summary>
    /// 중심 눈, 이 눈의 near plane에 효과가 렌더링됩니다.
    /// </summary>
    [SerializeField]
    private Camera _centerEyeCamera;

    /// <summary>
    /// 비네트를 렌더링할 쿼드를 생성하기 위한 메시 필터입니다.
    /// </summary>
    [SerializeField]
    private MeshFilter _meshFilter;

    /// <summary>
    /// (선택사항) 비네트의 실제 중심을 나타냅니다.
    /// 이 값을 사용하려면 UseAimingTarget을 설정해야 합니다.
    /// </summary>
    [SerializeField, Optional]
    private Vector3 _aimingDirection;
    public Vector3 AimingDirection
    {
        get => _aimingDirection;
        set => _aimingDirection = value;
    }

    /// <summary>
    /// 설정 시 AimingDirection이 비네트의 중심을 결정합니다.
    /// 설정하지 않으면 카메라의 중앙을 중심으로 합니다.
    /// </summary>
    [SerializeField]
    private bool _useAimingTarget;
    public bool UseAimingTarget
    {
        get => _useAimingTarget;
        set => _useAimingTarget = value;
    }

    /// <summary>
    /// 효과를 그릴 카메라로부터의 거리입니다.
    /// </summary>
    [Header("Mask State")]
    [SerializeField]
    private float _planeDistance;
    public float PlaneDistance
    {
        get => _planeDistance;
        set => _planeDistance = value;
    }

    /// <summary>
    /// 조준 방향의 반대쪽에 있는 비네트의 색상입니다.
    /// </summary>
    [Header("Mask Properties")]
    [SerializeField]
    private Color _maskOuterColor = Color.black;
    public Color MaskOuterColor
    {
        get => _maskOuterColor;
        set => _maskOuterColor = value;
    }

    /// <summary>
    /// 비네트의 중심 방향으로 향하는 색상입니다.
    /// </summary>
    [SerializeField]
    private Color _maskInnerColor = Color.black;
    public Color MaskInnerColor
    {
        get => _maskInnerColor;
        set => _maskInnerColor = value;
    }

    /// <summary>
    /// 비네트가 허용할 실제 시야각(도 단위)입니다.
    /// 360으로 설정하면 플레이어가 전체 시야를 볼 수 있으며,
    /// 0으로 설정하면 전체를 비네트로 덮습니다.
    /// </summary>
    [SerializeField, Range(0f, 360f)]
    private float _userFOV = 360f;
    public float UserFOV
    {
        get => _userFOV;
        set => _userFOV = value;
    }

    /// <summary>
    /// 색상에서 완전 투명으로 전환되는 비네트의 페더링 전환값(도 단위)입니다.
    /// </summary>
    [SerializeField, Range(0f, 180f)]
    private float _featheredFOV = 10f;
    public float ExtraFeatheredFOV
    {
        get => _featheredFOV;
        set => _featheredFOV = value;
    }

    /// <summary>
    /// 비네트의 색상 영역에 대한 알파값 배율입니다.
    /// </summary>
    [SerializeField, Range(0f, 1f)]
    private float _alphaStrength = 1f;
    public float AlphaStrength
    {
        get => _alphaStrength;
        set => _alphaStrength = value;
    }

    private readonly int _maskColorInnerID = Shader.PropertyToID("_ColorInner");
    private readonly int _maskColorOuterID = Shader.PropertyToID("_ColorOuter");
    private readonly int _maskDirectionID = Shader.PropertyToID("_Direction");
    private readonly int _minRadiusID = Shader.PropertyToID("_MinRadius");
    private readonly int _maxRadiusID = Shader.PropertyToID("_MaxRadius");
    private readonly int _alphaID = Shader.PropertyToID("_Alpha");

    private Mesh _maskMesh;
    private Transform _meshTransform;
    private MeshRenderer _meshRenderer;
    private MaterialPropertyBlock _materialPropertyBlock;

    protected bool _started;

    // ==== TunnelingEffect 제어용 ====
    [SerializeField] private float tunnelingHoldSeconds = 0.25f;   // 1로 켠 뒤 유지할 시간
    [SerializeField] private float tunnelingFadeOutSeconds = 0.25f; // 0으로 서서히 꺼지는 시간

    private Coroutine tunnelingCo;
    private float tunnelingKeepAliveUntil = 0f; // 유지 만료 시각 (Kick 시마다 갱신)

    /// 효과를 "지금부터 tunnelingHoldSeconds 동안 유지"로 갱신(리셋)한다.
    /// 코루틴이 없으면 시작하고, 이미 돌고 있으면 유지 만료 시간을 연장한다.
    public void KickTunneling()
    {
        tunnelingKeepAliveUntil = Time.time + tunnelingHoldSeconds;

        if (tunnelingCo == null)
            tunnelingCo = StartCoroutine(Co_TunnelingPulse());
    }

    /// 즉시 효과를 끈다(강제 종료).
    private void ForceHideTunneling()
    {
        if (tunnelingCo != null)
        {
            StopCoroutine(tunnelingCo);
            tunnelingCo = null;
        }
        AlphaStrength = 0f;
    }

    /// 1로 켜고(즉시), 유지시간 끝나면 0으로 페이드 아웃.
    /// 유지/페이드 중에 Kick이 들어오면 다시 1로 만들고 유지시간을 재시작한다.
    private IEnumerator Co_TunnelingPulse()
    {
        AlphaStrength = 1f;

        // 유지 구간: Kick이 들어오는 동안 계속 연장
        while (Time.time < tunnelingKeepAliveUntil)
            yield return null;

        // 페이드 아웃
        float t = 0f;
        while (t < tunnelingFadeOutSeconds)
        {
            // 페이드 중에 Kick이 오면 다시 유지 구간으로 복귀
            if (Time.time < tunnelingKeepAliveUntil)
            {
                AlphaStrength = 1f;

                // Kick의 최신 만료 시각까지 다시 유지
                while (Time.time < tunnelingKeepAliveUntil)
                    yield return null;

                // 다시 페이드 처음부터
                t = 0f;
            }
            else
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, t / tunnelingFadeOutSeconds);
                AlphaStrength = a;
                yield return null;
            }
        }

        AlphaStrength = 0f;
        tunnelingCo = null;
    }


    private static readonly Vector3[] _vertices = new Vector3[4]
    {
            new Vector3(-1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f),
            new Vector3(-1.0f, -1.0f, 0.0f), new Vector3(1.0f, -1.0f, 0.0f)
    };

    private static readonly Vector3[] _uv0 = new Vector3[4]
    {
            new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f)
    };

    private static readonly int[] _triangles = new int[6]
    {
            0, 1, 3, 0, 3, 2
    };

    protected virtual void Start()
    {
        this.BeginStart(ref _started);

        this.AssertField(_leftEyeAnchor, nameof(_leftEyeAnchor));
        this.AssertField(_rightEyeAnchor, nameof(_rightEyeAnchor));
        this.AssertField(_centerEyeCamera, nameof(_centerEyeCamera));
        this.AssertField(_meshFilter, nameof(_meshFilter));

        _meshTransform = _meshFilter.gameObject.transform;
        _meshRenderer = _meshFilter.GetComponent<MeshRenderer>();
        _maskMesh = new Mesh();

        _maskMesh.SetVertices(_vertices);
        _maskMesh.SetTriangles(_triangles, 0);
        _maskMesh.SetUVs(0, _uv0);

        _maskMesh.name = "Tunnel";
        _meshFilter.sharedMesh = _maskMesh;
        _materialPropertyBlock = new MaterialPropertyBlock();
        this.EndStart(ref _started);
    }

    protected virtual void OnEnable()
    {
        if (_started)
        {
            _meshRenderer.enabled = true;
        }
    }

    protected virtual void OnDisable()
    {
        if (_started)
        {
            _meshRenderer.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (_meshRenderer is null || _meshTransform is null)
        {
            return;
        }

        this.transform.SetPose(_centerEyeCamera.transform.GetPose());

        float radFov = Mathf.Deg2Rad * _centerEyeCamera.fieldOfView;
        float planeHeight = (Mathf.Tan(radFov / 2.0f) * _planeDistance) * 2.0f;
        float planeWidth = planeHeight * _centerEyeCamera.aspect;
        planeWidth += GetIPD();
        Vector2 planeSize = new Vector2(planeWidth, planeHeight);

        // 그냥 매직 넘버입니다. 어떤 이유에서인지 쿼드가 프러스텀을 완전히 덮지 못합니다.
        // 실제 VR 투영 행렬은 약간 달라서 위의 수학적 가정이 정확하지 않습니다.
        planeSize *= 1.2f;

        _meshTransform.localPosition = new Vector3(0.0f, 0.0f, _planeDistance);
        _meshTransform.localScale = new Vector3(planeSize.x * 0.5f, planeSize.y * 0.5f, 1.0f);

        float fov = UserFOV * 0.5f * Mathf.Deg2Rad;
        float maxMask = Mathf.Cos(fov);
        float minMask = Mathf.Cos(fov - ExtraFeatheredFOV * Mathf.Deg2Rad);

        _materialPropertyBlock.SetFloat(_alphaID, _alphaStrength);
        _materialPropertyBlock.SetFloat(_minRadiusID, minMask);
        _materialPropertyBlock.SetFloat(_maxRadiusID, maxMask);
        _materialPropertyBlock.SetColor(_maskColorInnerID, _maskInnerColor);
        _materialPropertyBlock.SetColor(_maskColorOuterID, _maskOuterColor);

        Vector3 direction = _useAimingTarget ? _aimingDirection : _centerEyeCamera.transform.forward;
        _materialPropertyBlock.SetVector(_maskDirectionID, direction.normalized);

        _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    private float GetIPD()
    {
        return Vector3.Distance(_leftEyeAnchor.position, _rightEyeAnchor.position);
    }

    #region Inject

    /// <summary>
    /// TunnelingEffect의 모든 의존성을 주입합니다.
    /// </summary>
    public void InjectAllTunnelingEffect(Transform leftEyeAnchor, Transform rightEyeAnchor,
        Camera centerEyeCamera, MeshFilter meshFilter)
    {
        InjectLeftEyeAnchor(leftEyeAnchor);
        InjectRightEyeAnchor(rightEyeAnchor);
        InjectCenterEyeCamera(centerEyeCamera);
        InjectMeshFilter(meshFilter);
    }

    public void InjectLeftEyeAnchor(Transform leftEyeAnchor)
    {
        _leftEyeAnchor = leftEyeAnchor;
    }

    public void InjectRightEyeAnchor(Transform rightEyeAnchor)
    {
        _rightEyeAnchor = rightEyeAnchor;
    }

    public void InjectCenterEyeCamera(Camera centerEyeCamera)
    {
        _centerEyeCamera = centerEyeCamera;
    }

    public void InjectMeshFilter(MeshFilter meshFilter)
    {
        _meshFilter = meshFilter;
    }

    #endregion
}
