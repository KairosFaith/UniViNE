using UnityEngine;
using UnityEngine.UI;
public class UniVineTimerUI : MonoBehaviour
{
    public Image TimerClock;
    float _Duration;
    public void SetDuration(float duration)
    {
        _Duration = duration;
    }
    public void UpdateTimer(float timeLeft)
    {
        TimerClock.fillAmount = timeLeft / _Duration;
    }
}