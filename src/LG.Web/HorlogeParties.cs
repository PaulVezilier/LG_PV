namespace LG.Web;

/// <summary>Fait avancer les parties dont le temps de phase est écoulé.</summary>
public sealed class HorlogeParties(GestionnaireParties gestionnaire, Diffuseur diffuseur, ILogger<HorlogeParties> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalle = TimeSpan.FromMilliseconds(500);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var minuteur = new PeriodicTimer(Intervalle);
        while (await minuteur.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var partie in gestionnaire.Parties)
            {
                try
                {
                    bool aChange;
                    lock (partie)
                        aChange = partie.Actualiser();
                    if (aChange)
                        await diffuseur.DiffuserAsync(partie);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Erreur lors de l'actualisation de la partie {PartieId}", partie.Id);
                }
            }
        }
    }
}
