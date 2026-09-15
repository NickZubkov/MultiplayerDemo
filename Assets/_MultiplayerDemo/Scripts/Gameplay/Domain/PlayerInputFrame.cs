using UnityEngine;

namespace Game.Gameplay
{
    /// Ввод одного кадра как данные — страховка варианта А (спека Ц2): контроллер не читает
    /// устройства сам, поэтому его можно кормить подставным вводом, а позже прогнать шаг
    /// по чужому вводу («полный Б», спека § 13).
    ///
    /// Взгляд уже в градусах за кадр: разницу между мышью и стиком снял источник.
    public readonly struct PlayerInputFrame
    {
        public static readonly PlayerInputFrame EMPTY = default;

        public readonly Vector2 Move;
        public readonly Vector2 Look;
        public readonly bool Jump;
        public readonly bool Sprint;
        public readonly bool Interact;

        public PlayerInputFrame(Vector2 move, Vector2 look, bool jump, bool sprint, bool interact)
        {
            Move = move;
            Look = look;
            Jump = jump;
            Sprint = sprint;
            Interact = interact;
        }
    }
}
