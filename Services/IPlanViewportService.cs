using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace ReisingerIntelliApp_V4.Services;

/// <summary>
/// Provides mapping and state for the floor plan viewport (zoom/pan) so ViewModels can add devices at the visible center.
/// Implemented by the view hosting the plan (e.g., MainPage + PanPinchContainer).
/// </summary>
public interface IPlanViewportService
{
    bool IsPlanReady { get; }
    Point ScreenToPlan(Point screenPoint);
    double PlanWidth { get; }   // intrinsic plan width in pixels of the rendered image (unscaled)
    double PlanHeight { get; }  // intrinsic plan height in pixels of the rendered image (unscaled)

    // Optional: expose current scale and translation if available
    double Scale { get; }
    double TranslationX { get; }
    double TranslationY { get; }

    // Convenience: current viewport center in plan coordinates
    Point GetViewportCenterInPlan();
}
