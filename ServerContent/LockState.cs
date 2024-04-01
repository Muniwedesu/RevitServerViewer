namespace RevitServerViewer;

public enum LockState
{
    Unlocked = 0
    , Locked = 1
    , AncestorLocked = 2
    , DescendantLocked = 3
    , BeingUnlocked = 4
    , BeingLocked = 5
}