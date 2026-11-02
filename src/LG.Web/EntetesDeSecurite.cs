namespace LG.Web;

public static class EntetesDeSecurite
{
    /// <summary>
    /// Le navigateur ne charge que nos propres scripts, nos images et les polices Google ;
    /// la page ne peut pas être affichée dans un cadre d'un autre site.
    /// </summary>
    private const string PolitiqueContenu =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' https://fonts.googleapis.com; " +
        "font-src https://fonts.gstatic.com; " +
        "img-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    public static IApplicationBuilder UseEntetesDeSecurite(this IApplicationBuilder app) => app.Use((contexte, suivant) =>
    {
        var entetes = contexte.Response.Headers;
        entetes.ContentSecurityPolicy = PolitiqueContenu;
        entetes.XContentTypeOptions = "nosniff";
        entetes.XFrameOptions = "DENY";
        entetes["Referrer-Policy"] = "same-origin";
        return suivant(contexte);
    });
}
