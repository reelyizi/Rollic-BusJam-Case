using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stickman : MonoBehaviour
{
    public StickmanColor Color { get; private set; }
    public int GridRow { get; set; }
    public int GridCol { get; set; }
    public bool HasPath { get; private set; }
    public bool IsHidden { get; private set; }
    public bool IsReserved { get; private set; }

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private GameObject reservedCanvas;

    private Animator animator;
    private Outline outline;
    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private Coroutine moveCoroutine;
    private ColorConfig colorConfig;

    private static readonly int RunTrigger = Animator.StringToHash("Run");
    private static readonly int IdleTrigger = Animator.StringToHash("Idle");
    private static readonly int SitTrigger = Animator.StringToHash("Sit");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        outline = GetComponent<Outline>();
        meshRenderer = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    public void Initialize(StickmanColor color, int row, int col, ColorConfig config, bool isHidden = false, GameConfig gameConfig = null, bool isReserved = false)
    {
        Color = color;
        GridRow = row;
        GridCol = col;
        HasPath = false;
        IsHidden = isHidden;
        IsReserved = isReserved;
        colorConfig = config;

        Color renderColor = isHidden && gameConfig != null
            ? gameConfig.hiddenStickmanColor
            : config.GetRenderColor(color);

        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", renderColor);
        meshRenderer.SetPropertyBlock(propBlock);

        if (outline != null)
            outline.enabled = false;

        if (reservedCanvas != null)
            reservedCanvas.SetActive(isReserved);

        if (animator != null)
            animator.SetTrigger(IdleTrigger);
    }

    public void Reveal()
    {
        if (!IsHidden) return;
        IsHidden = false;

        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", colorConfig.GetRenderColor(Color));
        meshRenderer.SetPropertyBlock(propBlock);
    }

    public void SetHasPath(bool hasPath)
    {
        if (IsHidden)
        {
            if (hasPath)
                Reveal();
            else
            {
                HasPath = false;
                return;
            }
        }

        HasPath = hasPath;
        if (outline != null)
            outline.enabled = hasPath;
    }

    public void MoveAlongPath(List<Vector3> worldPath, Action onComplete)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveAlongPathCoroutine(worldPath, onComplete));
    }

    private IEnumerator MoveAlongPathCoroutine(List<Vector3> worldPath, Action onComplete)
    {
        if (animator != null)
            animator.SetTrigger(RunTrigger);

        for (int i = 0; i < worldPath.Count; i++)
        {
            Vector3 target = worldPath[i];

            while (Vector3.Distance(transform.position, target) > 0.05f)
            {
                RotateToward(target);
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target;
        }

        // Face forward at bus stop
        transform.rotation = Quaternion.LookRotation(Vector3.forward);

        if (animator != null)
            animator.SetTrigger(IdleTrigger);

        moveCoroutine = null;
        onComplete?.Invoke();
    }

    private void RotateToward(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }
    }

    public void BoardBus(Vector3 busPosition, Action onComplete)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(BoardBusCoroutine(busPosition, onComplete));
    }

    private IEnumerator BoardBusCoroutine(Vector3 busPosition, Action onComplete)
    {
        if (animator != null)
            animator.SetTrigger(RunTrigger);

        while (Vector3.Distance(transform.position, busPosition) > 0.05f)
        {
            RotateToward(busPosition);
            transform.position = Vector3.MoveTowards(transform.position, busPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        if (animator != null)
            animator.SetTrigger(SitTrigger);

        moveCoroutine = null;
        onComplete?.Invoke();
    }
}
