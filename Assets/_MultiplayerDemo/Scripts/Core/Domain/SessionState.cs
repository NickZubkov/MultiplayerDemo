namespace Game.Core
{
    /// Reason заполняется только для Failed и показывается игроку:
    /// молчаливый отказ — худший вид ошибки в сетевом приложении.
    public readonly struct SessionState
    {
        public readonly SessionPhase Phase;
        public readonly string Reason;

        public SessionState(SessionPhase phase, string reason = null)
        {
            Phase = phase;
            Reason = reason;
        }
    }
}
