using System;

namespace CSLModsCommon.UI.SettingsCard; 
public abstract class ValueFieldCardBase<TControl, TValue> : SettingsCardBase<TControl> where TControl : UIElement where TValue : IComparable<TValue> { }