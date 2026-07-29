using ColossalFramework.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CSLModsCommon.UI; 
public class UIElementBase : UIComponent {
    public const string IgnoreUIElement = nameof(IgnoreUIElement);

    public int VisibleChildrenCount => GetVisibleChildrenCount();

    public int GetVisibleChildrenCount() => m_ChildComponents.Count(child => child.isVisible);

    public float CalculateMaxChildWidth() => GetActiveChildren().Aggregate(0f, (current, activeChild) => Mathf.Max(current, activeChild.width));

    public float CalculateMaxChildHeight() => GetActiveChildren().Aggregate(0f, (current, activeChild) => Mathf.Max(current, activeChild.height));

    public List<UIComponent> GetActiveChildren() => m_ChildComponents.Where(child => child.isVisible && child.gameObject.activeSelf && child.name != IgnoreUIElement).ToList();
}