using UnityEngine;

namespace Game.UI
{
    public sealed class UnityCursor : ICursor
    {
        public void SetFree(bool free)
        {
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }
    }
}
