using System.Diagnostics;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace BeatSaberOffsetMigrator.UI;

[ViewDefinition("BeatSaberOffsetMigrator.UI.BSML.DocumentationView.bsml")]
[HotReload(RelativePathToLayout = @"BSML\DocumentationView.bsml")]
public class DocumentationViewController: BSMLAutomaticViewController
{
    [UIAction("open_readme")]
    private void OpenReadMe()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/qe201020335/BeatSaberOffsetMigrator/blob/master/README.md",
            UseShellExecute = true,
            Verb = "open"
        });
    }
}