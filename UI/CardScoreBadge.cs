using UnityEngine;
using UnityEngine.UI;

namespace AutoLunDao.UI;

/// <summary>
///     卡牌评分徽标。
/// </summary>
public class CardScoreBadge : MonoBehaviour
{
    private GameObject? _badgeGo;
    private Text? _badgeText;

    private void OnDestroy()
    {
        if (_badgeGo == null) return;
        Destroy(_badgeGo);
        _badgeGo = null;
    }

    /// <summary>
    ///     初始化徽标组件。
    /// </summary>
    /// <param name="card">要显示徽标组件的卡牌</param>
    public void Initialize(LunDaoPlayerCard card)
    {
        // 尝试找到 card 上的 Image/RectTransform 作为挂点
        var anchorRt = card.cardImage.GetComponent<RectTransform>() ??
                       card.cardImage.gameObject.AddComponent<RectTransform>();

        // 已存在
        if (_badgeGo != null) return;

        _badgeGo = new GameObject("AdvisorBadge", typeof(RectTransform))
        {
            layer = LayerMask.NameToLayer("UI")
        };
        _badgeGo.transform.SetParent(anchorRt, false);

        var rt = _badgeGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);

        rt.anchoredPosition = new Vector2(-16f, -8f);
        rt.sizeDelta = card.cardLevel.rectTransform.sizeDelta;

        // 保证能绘制 Text
        _badgeGo.AddComponent<CanvasRenderer>();
        _badgeText = _badgeGo.AddComponent<Text>();

        _badgeText.font = card.cardLevel.font;
        _badgeText.alignment = TextAnchor.MiddleCenter;
        _badgeText.fontSize = card.cardLevel.fontSize;
        _badgeText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _badgeText.verticalOverflow = VerticalWrapMode.Overflow;
        _badgeText.raycastTarget = false;
        _badgeText.color = Color.yellow;
        _badgeText.text = "";
    }

    /// <summary>
    ///     重置评分，清除徽标显示。
    /// </summary>
    public void ResetScore()
    {
        if (_badgeText == null) return;
        _badgeText.text = "";
    }

    /// <summary>
    ///     标记为最佳推荐，徽标将显示「荐」。
    /// </summary>
    public void MarkAsBest()
    {
        if (_badgeText == null) return;
        _badgeText.text = "荐";
    }
}