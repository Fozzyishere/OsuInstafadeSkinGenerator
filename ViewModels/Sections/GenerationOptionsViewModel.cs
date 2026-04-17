using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuInstaFadeSkinGenerator.ViewModels.Sections;

public sealed partial class GenerationOptionsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool backupFiles = true;

    [ObservableProperty]
    private bool processHd = true;

    [ObservableProperty]
    private bool enableTripleStacking;
}
