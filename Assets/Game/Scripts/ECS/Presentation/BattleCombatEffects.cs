using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: combat is already resolved before these short animations start.
public sealed class BattleCombatEffects : MonoBehaviour
{
    private sealed class AttackAnimation
    {
        public Vector2 RestPosition;
        public Coroutine Routine;
    }

    private readonly Dictionary<RectTransform, AttackAnimation> attacks = new Dictionary<RectTransform, AttackAnimation>();
    private RectTransform effectsLayer;

    public void Play(BattleActionResult action, Transform attackerSlot, IReadOnlyList<GameObject> slots)
    {
        EnsureLayer();
        StartCoroutine(PlaySequence(action, attackerSlot, slots));
    }

    private IEnumerator PlaySequence(BattleActionResult action, Transform attackerSlot, IReadOnlyList<GameObject> slots)
    {
        Font font = attackerSlot.Find("IndexText").GetComponent<Text>().font;
        if (action.Hits.Count == 0)
        {
            foreach (int slot in action.MoonMarkedSlots)
                if (slot >= 0 && slot < slots.Count)
                    StartCoroutine(ShowImpact(true, 0, effectsLayer.InverseTransformPoint(slots[slot].transform.position), null, font, "月光印记 +1", true));
            if (!string.IsNullOrEmpty(action.Message))
                StartCoroutine(ShowImpact(true, 0, effectsLayer.InverseTransformPoint(attackerSlot.position), null, font, action.Message, true));
            yield break;
        }
        foreach (var hit in action.Hits)
        {
            if (hit.TargetSlot < 0 || hit.TargetSlot >= slots.Count) continue;
            Transform targetSlot = slots[hit.TargetSlot].transform;
            if (action.Kind != BattleActionKind.Bleed && action.Kind != BattleActionKind.Lightning && action.Kind != BattleActionKind.Moonlight && action.Kind != BattleActionKind.Execution) PlayLunge(attackerSlot, targetSlot);
            Transform node = targetSlot.Find("Portrait");
            Image image = node != null ? node.GetComponent<Image>() : null;
            Vector2 position = effectsLayer.InverseTransformPoint(node != null ? node.position : targetSlot.position);
            StartCoroutine(ShowImpact(hit.Hit, hit.Damage, position, image, font,
                hit.Executed ? "斩杀" :
                action.Kind == BattleActionKind.Bleed ? "流血 -" + hit.Damage.ToString("0.##") :
                action.Kind == BattleActionKind.Lightning ? "雷击 -" + hit.Damage.ToString("0.##") :
                action.Kind == BattleActionKind.Moonlight ? "月印引爆 -" + hit.Damage.ToString("0.##") :
                hit.ShieldDamage > 0 ? "护盾吸收 " + hit.ShieldDamage.ToString("0.##") + " / 扣血 " + hit.Damage.ToString("0.##") : null));
            if (hit.Healing > 0)
                StartCoroutine(ShowImpact(true, 0, effectsLayer.InverseTransformPoint(attackerSlot.position), null, font, "+" + hit.Healing.ToString("0.##"), true));
            yield return new WaitForSeconds(0.15f);
        }
    }

    private void PlayLunge(Transform attackerSlot, Transform targetSlot)
    {
        Transform portraitNode = attackerSlot.Find("Portrait");
        if (portraitNode is RectTransform portrait)
        {
            if (attacks.TryGetValue(portrait, out AttackAnimation previous))
            {
                StopCoroutine(previous.Routine);
                portrait.anchoredPosition = previous.RestPosition;
            }
            Vector3 localTarget = portrait.parent.InverseTransformPoint(targetSlot.position);
            Vector2 direction = ((Vector2)(localTarget - portrait.localPosition)).normalized;
            var animation = new AttackAnimation { RestPosition = portrait.anchoredPosition };
            attacks[portrait] = animation;
            animation.Routine = StartCoroutine(AnimateAttack(portrait, animation, direction * 20f));
        }

    }

    public void ResetEffects()
    {
        StopAllCoroutines();
        foreach (var pair in attacks)
            if (pair.Key != null) pair.Key.anchoredPosition = pair.Value.RestPosition;
        attacks.Clear();
        if (effectsLayer == null) return;
        for (int i = effectsLayer.childCount - 1; i >= 0; i--)
        {
            GameObject effect = effectsLayer.GetChild(i).gameObject;
            effect.SetActive(false);
            Destroy(effect);
        }
    }

    private void OnDisable()
    {
        ResetEffects();
    }

    private void EnsureLayer()
    {
        if (effectsLayer == null)
        {
            var root = new GameObject("CombatEffects", typeof(RectTransform));
            root.layer = gameObject.layer;
            effectsLayer = root.GetComponent<RectTransform>();
            effectsLayer.SetParent(transform, false);
            effectsLayer.anchorMin = Vector2.zero;
            effectsLayer.anchorMax = Vector2.one;
            effectsLayer.offsetMin = effectsLayer.offsetMax = Vector2.zero;
        }
        effectsLayer.SetAsLastSibling();
    }

    private IEnumerator AnimateAttack(RectTransform portrait, AttackAnimation animation, Vector2 offset)
    {
        const float duration = 0.24f;
        float elapsed = 0;
        while (elapsed < duration && portrait != null)
        {
            float t = elapsed / duration;
            // Reach the target direction quickly, then settle back into the slot.
            float amount = t < 0.4f ? Mathf.Sin(t / 0.4f * Mathf.PI * 0.5f) :
                1f - Mathf.SmoothStep(0, 1, (t - 0.4f) / 0.6f);
            portrait.anchoredPosition = animation.RestPosition + offset * amount;
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (portrait != null) portrait.anchoredPosition = animation.RestPosition;
        attacks.Remove(portrait);
    }

    private IEnumerator ShowImpact(bool hit, float damage, Vector2 position, Image targetImage, Font font, string text = null, bool positive = false)
    {
        yield return new WaitForSeconds(0.09f);
        if (hit && !positive) StartCoroutine(AnimateHit(position, targetImage));

        var root = new GameObject(hit ? "DamageText" : "MissText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        root.layer = gameObject.layer;
        var rect = root.GetComponent<RectTransform>();
        rect.SetParent(effectsLayer, false);
        rect.sizeDelta = new Vector2(200, 48);
        Text label = root.GetComponent<Text>();
        label.font = font;
        label.fontSize = hit ? 30 : 23;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.text = text ?? (hit ? "-" + damage.ToString("0.##") : "未命中");
        if (text != null) label.fontSize = positive ? 19 : 23;
        Color color = positive ? new Color(0.4f, 1f, 0.62f) : hit ? new Color(1f, 0.36f, 0.28f) : new Color(0.87f, 0.91f, 1f);
        Outline outline = root.GetComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        const float duration = 0.8f;
        float elapsed = 0;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            rect.anchoredPosition = position + new Vector2(0, 22 + 58 * t);
            rect.localScale = Vector3.one * (1f + 0.22f * (1f - Mathf.Clamp01(t / 0.2f)));
            color.a = 1f - Mathf.Clamp01((t - 0.3f) / 0.7f);
            label.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(root);
    }

    private IEnumerator AnimateHit(Vector2 position, Image targetImage)
    {
        Image flash = CreateImage("HitFlash", position,
            targetImage != null ? targetImage.rectTransform.rect.size : new Vector2(100, 100));
        flash.sprite = targetImage != null ? targetImage.sprite : null;
        flash.preserveAspect = targetImage != null && targetImage.preserveAspect;
        float elapsed = 0;
        const float duration = 0.22f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            flash.color = new Color(1f, 0.12f, 0.08f, 0.8f * (1f - t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(flash.gameObject);
    }

    private Image CreateImage(string name, Vector2 position, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.layer = gameObject.layer;
        var rect = root.GetComponent<RectTransform>();
        rect.SetParent(effectsLayer, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = root.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }
}
