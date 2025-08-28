using ReisingerIntelliApp_V4.ViewModels;

namespace ReisingerIntelliApp_V4.Views;

public partial class StructureEditorPage : ContentPage
{
    public StructureEditorPage(StructureEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (s, e) =>
        {
            await vm.InitializeAsync();
        };
    }
}
