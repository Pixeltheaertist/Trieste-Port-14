namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void SetCustomSpecies(string customSpecies)
    {
        Profile = Profile?.WithCustomSpeciesName(customSpecies);
        SetDirty();
    }

    private void UpdateCustomSpecies()
    {
        CustomSpeciesName.Text = Profile?.CustomSpeciesName ?? "";
    }
}
