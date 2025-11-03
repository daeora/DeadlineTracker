namespace DeadlineTracker.Services
{
    // Yksinkertainen "kuka on kirjautunut" -muisti appin sisällä.
    // Tämä EI mene kantaan, vaan elää sovelluksen ajon aikana.
    public static class Session
    {
        public static int CurrentUserId { get; set; }
        public static string CurrentUsername { get; set; }

        public static bool IsLoggedIn => CurrentUserId > 0;
    }
}

