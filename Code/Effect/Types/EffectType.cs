using UnityEngine;

namespace Nonsliep.Core.Effect
{
    // Puzzle/mobile focused event keys
    public enum EffectType
    {
        // Lifecycle
        OnAwake,
        OnEnable,
        OnDisable,
        OnDestroy,

        // UI
        UIClick,
        UIConfirm,
        UIBack,
        UIToggleOn,
        UIToggleOff,
        UISliderStart,
        UISliderEnd,
        UIPopupOpen,
        UIPopupClose,

        // Feedback
        Success,
        Perfect,
        Fail,
        BasketballShoot,
        BasketballRimHit,

        // Achievements
        BestMoves,
        BestTime
    }
}
