namespace CSLModsCommon.UI.ValueFields; 
public class UIIntField : UIValueField<int, IntValueBinder> {
    public void Setup(int initial = 0, int step = 1, int min = int.MinValue, int max = int.MaxValue) {
        var b = new IntValueBinder(initial) { Step = step, MinValue = min, MaxValue = max, UseMin = true, UseMax = true };
        InitializeBinder(b);
        CanWheel = true;
    }
}