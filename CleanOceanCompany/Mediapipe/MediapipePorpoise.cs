using Photon.Pun.Demo.Cockpit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MediapipePorpoise : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    [SerializeField] private Texture2D _dirtMaskBase;
    [SerializeField] private Texture2D _brush;

    [SerializeField] private Material _material;
    [SerializeField] private RawImage image;

    Vector3 halfImage;

    private Texture2D _templateDirtMask;

    public GameObject brushImage; // UI 2D 이미지 오브젝트

    public RectTransform rectTransformToBrushImage; // brushImage 의 RectTransform

    public MeshCollider mediapipePorpoise;

    public bool isClean;

    bool isStart;

    public MediapipeThirdManager mediapipeThirdManager;

    public TMP_Text CurrentCleanTrashCountText;

    public GameObject shineParticlePrefab;

    float currentOilIndex = 0;
    float maxOliIndex = 0;

    public float oilPercentage = 0;

    Vector2 screenPosition;
    Vector2 lastPos;

    Animator animator;

    public Interaction_MediapipeThird interaction_MediapipeThird;

    private void Start()
    {
        if (mediapipeThirdManager == null) mediapipeThirdManager = GameObject.FindAnyObjectByType<MediapipeThirdManager>();


        

        animator = transform.parent.GetComponent<Animator>();
    }

    public void SetStartSetting()
    {
        mediapipeThirdManager.OnChangedBrushObject += ChangeBrush;


        CreateTexture();
    }

    void ChangeBrush(GameObject brush)
    {
        brushImage = brush;
        if (brushImage != null)
        {
            rectTransformToBrushImage = brushImage.GetComponent<RectTransform>();
            image = brushImage.GetComponent<RawImage>();

            halfImage = new Vector3(image.texture.width / 2, image.texture.height / 2, 0);

            //CreateTexture();
            //Debug.Log("브러시 할당");
        }
        else if (brushImage == null)
        {
            //Debug.Log("브러시 제거");
        }
    }


    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    SetPorpoiseColor(isClean);
        //}

        if (brushImage != null)
        {
            // 화면 공간 좌표로 변환
            screenPosition = RectTransformUtility.WorldToScreenPoint(null, rectTransformToBrushImage.position);
            Ray ray = _camera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, (1 << 25)) && (screenPosition - lastPos).magnitude > 0.8f)
            {
                lastPos = screenPosition;
                Vector2 textureCoord = hit.textureCoord;

                int pixelX = Mathf.Clamp((int)(textureCoord.x * _templateDirtMask.width), 0, _templateDirtMask.width - 1);
                int pixelY = Mathf.Clamp((int)(textureCoord.y * _templateDirtMask.height), 0, _templateDirtMask.height - 1);

                Color[] dirtMaskColors = _templateDirtMask.GetPixels();
                Color[] brushColors = _brush.GetPixels();


                for (int x = 0; x < _brush.width; x++)
                {
                    for (int y = 0; y < _brush.height; y++)
                    {
                        int targetX = pixelX + x;
                        int targetY = pixelY + y;

                        // 텍스처 범위를 초과하면 무시
                        if (targetX < 0 || targetX >= _templateDirtMask.width || targetY < 0 || targetY >= _templateDirtMask.height)
                            continue;


                        int targetIndex = targetY * _templateDirtMask.width + targetX;
                        int brushIndex = y * _brush.width + x;



                        Color pixelDirt = brushColors[brushIndex];
                        Color pixelDirtMask = dirtMaskColors[targetIndex];

                        float epsilon = 0.1f; // 임계값 설정
                        float adjustedGreen = Mathf.Max(0, pixelDirtMask.g * pixelDirt.g); // 음수 방지

                        if (mediapipeThirdManager.brushTriggerEvent.cleanCount > 0)
                        {
                            if (dirtMaskColors[targetIndex].g <= epsilon)
                            {
                                // 이미 깨끗한 상태
                            }
                            else
                            {
                                // 닦는 동작 수행
                                dirtMaskColors[targetIndex] = new Color(0, adjustedGreen, 0);

                                if (dirtMaskColors[targetIndex].g <= epsilon)
                                {
                                    // 닦아서 깨끗해진 상태
                                    currentOilIndex -= 10;
                                    mediapipeThirdManager.brushTriggerEvent.BrushEvent();
                                    oilPercentage = OilPercentage(currentOilIndex, hit);

                                    CurrentCleanTrashCountText.text = $"청소 완료까지 : {100 - (int)oilPercentage}%";
                                }
                            }
                        }
                    }
                }
                _templateDirtMask.SetPixels(dirtMaskColors);
                _templateDirtMask.Apply();
            }
        }
    }


    // 첫 세팅용
    float OilPercentage(float currentIndex)
    {
        float currentPercentage = currentIndex / maxOliIndex * 100;

        return currentPercentage;
    }


    // 닦기 비율이 10의 배수일 때마다, 파티클 생성
    float OilPercentage(float currentIndex, RaycastHit hit)
    {
        float currentPercentage = currentIndex / maxOliIndex * 100;

        if ((int)currentPercentage % 10 != 0)
        {
            isStart = false;
        }

        if ((int)currentPercentage % 10 == 0 && !isStart)
        {
            isStart = true;
            print("OilPercentage 함수 실행됨!");
            Instantiate(shineParticlePrefab, hit.point, Quaternion.identity);
        }

        if (currentPercentage <= 0 && brushImage != null)
        {
            Destroy(brushImage);
            mediapipeThirdManager.BrushObject = null;
            interaction_MediapipeThird.ClearScore();
        }

        return currentPercentage;
    }

    public void SetPorpoiseColor(bool turn)
    {
        float value = turn ? 0 : 1; // true 면 깨끗하게 / false 면 더러운 원상태로
        if (_templateDirtMask == null) return;
        for (int x = 0; x < _templateDirtMask.width; x++)
        {
            for (int y = 0; y < _templateDirtMask.height; y++)
            {
                _templateDirtMask.SetPixel(x, y, new Color(0, value, 0));
            }
        }

        _templateDirtMask.Apply();
    }

    private void CreateTexture()
    {
        _templateDirtMask = new Texture2D(_dirtMaskBase.width, _dirtMaskBase.height);

        //_templateDirtMask = new Texture2D(image.texture.width, image.texture.height);
        _templateDirtMask.SetPixels(_dirtMaskBase.GetPixels());
        _templateDirtMask.Apply();

        currentOilIndex = _templateDirtMask.width * _templateDirtMask.height;
        maxOliIndex = currentOilIndex;

        oilPercentage = OilPercentage(currentOilIndex);

        CurrentCleanTrashCountText.text = $"청소 완료까지 : {0}%";

        _material.SetTexture("_DirtMask", _templateDirtMask);
    }

    private void OnDrawGizmos()
    {
        if (brushImage != null)
        {
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(null, rectTransformToBrushImage.position);
            Ray ray = _camera.ScreenPointToRay(screenPosition);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(ray.origin, ray.direction);
        }
    }
}
