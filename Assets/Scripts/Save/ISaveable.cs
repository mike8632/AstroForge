using System;

public interface ISaveable
{
    // Return any serializable struct/class with your state (JsonUtility friendly).
    object CaptureState();

    // You'll receive the same type you returned from CaptureState().
    void RestoreState(object state);
}