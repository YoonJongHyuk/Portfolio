using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Video_Screen_Manager : MonoBehaviour
{
    [SerializeField] private GameObject windowedScreen;
    [SerializeField] private GameObject fullScreen;

    [SerializeField] private GameObject lock_On_Button;
    [SerializeField] private GameObject lock_Off_Button;
    [SerializeField] private List<CanvasGroup> canvasGroups;
    [SerializeField] private List<Image> sliderImages;

    [SerializeField] private RectTransform topPanel;
    [SerializeField] private RectTransform bottomPanel;

    [SerializeField] private float moveDistance = 300f;
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float autoHideDelay = 3f;

    private Coroutine moveCoroutine;
    private float idleTimer = 0f;
    private bool isHidden = false;

    // ✅ UI는 anchoredPosition이 안정적
    private Vector2 topShownAnchored;
    private Vector2 bottomShownAnchored;

    private int interactionBlockCount = 0;

    private bool positionsBound = false;

    void OnEnable()
    {
        // fullScreen이 켜져있을 수도 있으니 안전하게 바인딩 시도
        if (fullScreen != null && fullScreen.activeInHierarchy)
            StartCoroutine(BindShownPositionsNextFrame());
    }

    void Update()
    {
        if (!fullScreen.activeSelf) return;

        // 드래그 중이면 자동 숨김 타이머 정지
        if (interactionBlockCount > 0) return;

        idleTimer += Time.deltaTime;

        if (!isHidden && idleTimer >= autoHideDelay)
        {
            HidingPanelSmooth(true);
        }
    }

    public void SetLockState(bool isLocked)
    {
        lock_On_Button.SetActive(!isLocked);
        lock_Off_Button.SetActive(isLocked);

        foreach (var cg in canvasGroups)
            cg.interactable = !isLocked;

        foreach (var img in sliderImages)
            img.raycastTarget = !isLocked;
    }

    public void OnUserInteraction()
    {
        idleTimer = 0f;
        if (isHidden) HidingPanelSmooth(false);
    }

    public void NotifyUserActivity()
    {
        idleTimer = 0f;
        if (isHidden) HidingPanelSmooth(false);
    }

    public void BeginInteractionBlock()
    {
        interactionBlockCount++;
        idleTimer = 0f;

        if (isHidden) HidingPanelSmooth(false);
    }

    public void EndInteractionBlock()
    {
        interactionBlockCount = Mathf.Max(0, interactionBlockCount - 1);
        idleTimer = 0f;
    }

    public void HidingPanelSmooth(bool hidden)
    {
        if (hidden && interactionBlockCount > 0) return;

        // ✅ 아직 positions 바인딩 안됐으면 여기서라도 한 번 바인딩
        if (!positionsBound)
            StartCoroutine(BindShownPositionsNextFrame());

        // 같은 상태로 또 호출되면 불필요 코루틴 방지
        if (isHidden == hidden && moveCoroutine != null) return;

        isHidden = hidden;
        idleTimer = 0f;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MovePanels(hidden));
    }

    private IEnumerator MovePanels(bool hidden)
    {
        // ✅ 바인딩이 끝날 때까지 1프레임 대기(레이아웃 튐 방지)
        if (!positionsBound)
            yield return null;

        Vector2 topStart = topPanel.anchoredPosition;
        Vector2 bottomStart = bottomPanel.anchoredPosition;

        // hidden이면 y만 이동
        Vector2 topTarget = hidden
            ? new Vector2(topShownAnchored.x, topShownAnchored.y + moveDistance)
            : topShownAnchored;

        Vector2 bottomTarget = hidden
            ? new Vector2(bottomShownAnchored.x, bottomShownAnchored.y - moveDistance)
            : bottomShownAnchored;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);

            topPanel.anchoredPosition = Vector2.Lerp(topStart, topTarget, t);
            bottomPanel.anchoredPosition = Vector2.Lerp(bottomStart, bottomTarget, t);

            yield return null;
        }

        topPanel.anchoredPosition = topTarget;
        bottomPanel.anchoredPosition = bottomTarget;

        moveCoroutine = null;
    }

    // ✅ 레이아웃 확정 후 “원래 위치”를 다시 잡는 핵심
    private IEnumerator BindShownPositionsNextFrame()
    {
        positionsBound = false;

        // 레이아웃 강제 반영
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)topPanel.parent);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)bottomPanel.parent);

        // 1프레임 대기(해상도/캔버스 스케일러/레이아웃 확정)
        yield return null;

        topShownAnchored = topPanel.anchoredPosition;
        bottomShownAnchored = bottomPanel.anchoredPosition;

        positionsBound = true;
    }

    // 외부에서 fullScreen 켰을 때 호출해도 좋음
    public void RebindShownPositions()
    {
        StartCoroutine(BindShownPositionsNextFrame());
    }
}