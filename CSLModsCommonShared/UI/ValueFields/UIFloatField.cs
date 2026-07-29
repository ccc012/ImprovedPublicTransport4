namespace CSLModsCommon.UI.ValueFields;

public class UIFloatField : UIValueField<float, FloatValueBinder> {
    public void Setup(float initial = 0f, float step = 0.1f, float min = float.MinValue, float max = float.MaxValue) {
        var b = new FloatValueBinder(initial) { Step = step, MinValue = min, MaxValue = max, UseMin = true, UseMax = true };
        InitializeBinder(b);
        CanWheel = true;
    }
}