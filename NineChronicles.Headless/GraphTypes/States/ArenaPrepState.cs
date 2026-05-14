namespace NineChronicles.Headless.GraphTypes.States
{
    public class ArenaPrepState
    {
        public ArenaPrepAvatarState my { get; set; } = new();
        public ArenaPrepAvatarState enemy { get; set; } = new();
    }
}
