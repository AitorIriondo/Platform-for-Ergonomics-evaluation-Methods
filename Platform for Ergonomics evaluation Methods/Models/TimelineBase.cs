using System.Collections;
using System.Collections.Generic;
[System.Serializable]
public class TimelineBase{
    public float time;
    public float duration;
    public virtual void SetTime(float newTime) {
        newTime = MathF.Min(duration, MathF.Max(0, newTime));
        bool changed = newTime != time;
        time = newTime;
        if (changed) {
            OnTimeChanged();
        }
    }
    public virtual void OnTimeChanged() {

    }
}
