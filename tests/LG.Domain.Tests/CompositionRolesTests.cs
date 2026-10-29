namespace LG.Domain.Tests;

public class CompositionRolesTests
{
    [Theory]
    [InlineData(4, 1)]
    [InlineData(7, 1)]
    [InlineData(8, 2)]
    [InlineData(12, 3)]
    public void La_composition_automatique_a_le_bon_nombre_de_loups(int joueurs, int loups)
    {
        var composition = CompositionRoles.Automatique(joueurs);

        Assert.Equal(joueurs, composition.Count);
        Assert.Equal(loups, composition.Count(r => r is RoleId.LoupGarou or RoleId.LoupGarouNoir));
        Assert.Equal(loups >= 2 ? 1 : 0, composition.Count(r => r == RoleId.LoupGarouNoir));
    }
}
