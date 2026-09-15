using UnityEngine;

namespace Game.Gameplay
{
    /// Всё, что игре нужно от контроллера: остальное он делает сам, в своём Update (спека Ц2).
    /// Причуда CharacterController — он помнит позицию и возвращается к ней — закрывается
    /// внутри каждой реализации и не расползается по игре.
    public interface IAvatarController
    {
        public void Teleport(Vector3 position, Quaternion rotation);
    }
}
