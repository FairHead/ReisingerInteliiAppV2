using ReisingerIntelliApp_V4.ViewModels;

namespace ReisingerIntelliApp_V4.Views;

public partial class StructureEditorPage : ContentPage
{
    private StructureEditorViewModel? _viewModel;

    public StructureEditorPage(StructureEditorViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = _viewModel;
        SetupFooterEvents();
        Loaded += async (s, e) =>
        {
            await vm.InitializeAsync();
        };
    }

    private void SetupFooterEvents()
    {
        if (_viewModel != null)
        {
            // Bind the center button (bookmark icon) to SaveCommand
            Footer.CenterButtonTapped += async (s, e) =>
            {
                if (_viewModel.SaveCommand.CanExecute(null))
                    await _viewModel.SaveCommand.ExecuteAsync(null);
            };

            // Bind the left section (My Place) to navigate back to MainPage
            Footer.LeftSectionTapped += async (s, e) =>
            {
                if (_viewModel.BackToMainPageCommand.CanExecute(null))
                    await _viewModel.BackToMainPageCommand.ExecuteAsync(null);
            };
        }
    }
}
