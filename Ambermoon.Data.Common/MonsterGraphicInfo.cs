namespace Ambermoon.Data
{
    // Note: First frame is always the idle frame.
    public struct MonsterGraphicInfo
    {
        public uint Width;
        public uint Height;
        public uint FirstIdleAnimationFrame;
        public int IdleAnimationFrameCount;
        public uint FirstAttackFrame;
        public int AttackFrameCount;
        public uint? FirstCastFrame;
        public int CastFrameCount; // can be 0
        public uint FirstMoveFrame;
        public int MoveFrameCount;
        public uint HurtFrame; // always 1 frame only
        public uint? FirstTransformFrame; // transform at battle start
        public int TransformFrameCount; // can be 0
    }
}
