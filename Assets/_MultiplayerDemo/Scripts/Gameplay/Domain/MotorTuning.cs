namespace Game.Gameplay
{
    /// Настройки мотора одной структурой: скорости — из DemoConfig (Ц16), остальное —
    /// из ассета провайдера.
    public readonly struct MotorTuning
    {
        public readonly float WalkSpeed;
        public readonly float SprintSpeed;
        public readonly float SpeedChangeRate;
        public readonly float JumpHeight;
        public readonly float Gravity;
        public readonly float TopClamp;
        public readonly float BottomClamp;

        public MotorTuning(
            float walkSpeed,
            float sprintSpeed,
            float speedChangeRate,
            float jumpHeight,
            float gravity,
            float topClamp,
            float bottomClamp)
        {
            WalkSpeed = walkSpeed;
            SprintSpeed = sprintSpeed;
            SpeedChangeRate = speedChangeRate;
            JumpHeight = jumpHeight;
            Gravity = gravity;
            TopClamp = topClamp;
            BottomClamp = bottomClamp;
        }
    }
}
