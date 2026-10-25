using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 强化经验条动画器
///
/// 只负责通过时间调整 Slider.value。
/// 不负责结算、不负责扣材料、不负责修改等级。
/// 按固定时间调整 Slider.value
/// 
/// 普通加经验时平滑增长
/// 跨级时先涨满，再瞬间归零，再继续涨
/// 一次升很多级时，最多视觉表现 3 次满条
/// 到达满级时经验条归零
/// 只负责 UI 动画，不改任何数据
/// </summary>
public class StrengthenExpSliderAnimator : MonoBehaviour
{
    [SerializeField] private Slider expSlider;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.45f;
    [SerializeField] private int maxVisualLevelUpCount = 3;

    // 保存当前正在运行的协程，以便在播放新动画前停止旧的
    private Coroutine currentRoutine;

    public void SetSlider(Slider slider)
    {
        expSlider = slider;
    }

    /// <summary>
    /// 立即设置 Slider 的值（无动画），用于直接显示当前经验进度
    /// </summary>
    public void SetInstant(
        int level,
        int currentExp,
        int maxLevel,
        Func<int, int> getNeedExp)
    {
        if (expSlider == null)
            return;

        expSlider.minValue = 0f;
        expSlider.maxValue = 1f;

        if (level >= maxLevel)
        {
            expSlider.value = 0f;
            return;
        }

        int needExp = getNeedExp != null ? getNeedExp(level) : 0;
        if (needExp <= 0)
        {
            expSlider.value = 0f;
            return;
        }

        // 计算当前经验在升级所需经验中的比例，并限制在 0~1 之间，设置给 Slider
        expSlider.value = Mathf.Clamp01((float)currentExp / needExp);
    }

    /// <summary>
    /// 播放经验变化的预览动画（从 current 到 preview）
    /// </summary>
    public void PlayPreviewAnimation(
        int currentLevel,
        int currentExp,
        int previewLevel,
        int previewExp,
        int maxLevel,
        Func<int, int> getNeedExp,
        bool useVisualLevelUpLimit = true)
    {
        if (expSlider == null)
            return;

        // 如果有正在运行的协程，先停止它，避免两个动画重叠
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        // 启动新的协程，传入所有参数，并把返回的 Coroutine 保存起来
        currentRoutine = StartCoroutine(PlayRoutine(
            currentLevel,
            currentExp,
            previewLevel,
            previewExp,
            maxLevel,
            getNeedExp,
            useVisualLevelUpLimit));
    }

    private IEnumerator PlayRoutine(
        int currentLevel,
        int currentExp,
        int previewLevel,
        int previewExp,
        int maxLevel,
        Func<int, int> getNeedExp,
        bool useVisualLevelUpLimit = true)
    {
        expSlider.minValue = 0f;
        expSlider.maxValue = 1f;

        if (getNeedExp == null)
        {
            expSlider.value = 0f;
            yield break;
        }

        // 只有“当前和目标都已经满级”时，才直接停在 0；
        // 否则允许从满级预览状态往回退
        if (currentLevel >= maxLevel && previewLevel >= maxLevel)
        {
            expSlider.value = 0f;
            yield break;
        }

        // 当前如果是满级预览状态，则起始进度视为 0；
        // 否则按当前等级经验比例计算
        int currentNeedExp = currentLevel < maxLevel ? getNeedExp(currentLevel) : 0;
        float startValue = currentNeedExp > 0
            ? Mathf.Clamp01((float)currentExp / currentNeedExp)
            : 0f;

        // 同级经验变化，支持正向和反向的平滑动画
        if (previewLevel == currentLevel)
        {
            int previewNeedExp = previewLevel < maxLevel ? getNeedExp(previewLevel) : 0;
            float endValue = previewNeedExp > 0
                ? Mathf.Clamp01((float)previewExp / previewNeedExp)
                : 0f;

            yield return AnimateValue(startValue, endValue, animationDuration);
            yield break;
        }

        // 处理升级动画
        if (previewLevel > currentLevel)
        {
            int gainedLevelCount = previewLevel - currentLevel;

            // 只有自动添加时才应用视觉升级次数限制
            int visualFullCount = useVisualLevelUpLimit
                ? Mathf.Min(gainedLevelCount, Mathf.Max(1, maxVisualLevelUpCount))
                : gainedLevelCount;

            float singleDuration = animationDuration / Mathf.Max(1, visualFullCount + 1);

            float firstStartValue = startValue;

            for (int i = 0; i < visualFullCount; i++)
            {
                yield return AnimateValue(firstStartValue, 1f, singleDuration);
                expSlider.value = 0f;
                firstStartValue = 0f;
            }

            if (previewLevel >= maxLevel)
            {
                expSlider.value = 0f;
                yield break;
            }

            int finalNeedExp = getNeedExp(previewLevel);
            float finalValue = finalNeedExp > 0
                ? Mathf.Clamp01((float)previewExp / finalNeedExp)
                : 0f;

            yield return AnimateValue(0f, finalValue, singleDuration);
            yield break;
        }

        // 处理“降级/经验回退”的动画逻辑
        int lostLevelCount = currentLevel - previewLevel;
        float reverseSingleDuration = animationDuration / Mathf.Max(1, lostLevelCount + 1);

        // 先把当前等级的经验从当前位置退到 0
        yield return AnimateValue(startValue, 0f, reverseSingleDuration);

        // 中间跨过的等级，从满条退到空条
        for (int i = 0; i < lostLevelCount - 1; i++)
        {
            expSlider.value = 1f;
            yield return AnimateValue(1f, 0f, reverseSingleDuration);
        }

        // 到目标等级后，从满条退到目标经验位置
        int targetNeedExp = getNeedExp(previewLevel);
        float targetValue = targetNeedExp > 0
            ? Mathf.Clamp01((float)previewExp / targetNeedExp)
            : 0f;

        expSlider.value = 1f;
        yield return AnimateValue(1f, targetValue, reverseSingleDuration);
    }

    /// <summary>
    /// 一个在指定时间内将 Slider 值从 from 平滑过渡到 to 的协程
    /// </summary>
    private IEnumerator AnimateValue(float from, float to, float duration)
    {
        if (expSlider == null)
            yield break;

        if (duration <= 0f)
        {
            expSlider.value = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // 计算当前进度比例 t
            float t = Mathf.Clamp01(timer / duration);
            // 使用线性插值计算中间值，并赋给 Slider
            expSlider.value = Mathf.Lerp(from, to, t);
            yield return null;
        }

        // 确保最终值精确等于 to（避免浮点数误差）
        expSlider.value = to;
    }
}