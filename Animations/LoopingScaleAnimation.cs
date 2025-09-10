using CommunityToolkit.Maui.Animations;

namespace ReisingerIntelliApp_V4.Animations
{
    public class LoopingScaleAnimation : BaseAnimation
    {
        public LoopingScaleAnimation() 
        {
            // Parameterless constructor for XAML instantiation
        }
        
        public double From { get; set; } = 1.0;
        public double To { get; set; } = 1.08;

        public override async Task Animate(VisualElement view, CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                await view.ScaleTo(To, Length, Easing);
                await view.ScaleTo(From, Length, Easing);
            }
        }
    }
}
