using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Server.Pages
{
    public class AboutModel : PageModel
    {
        public string ProjectName { get; private set; } =
            "Connect Four – Client–Server (.NET)";

        public string[] Authors { get; private set; } =
            new[] { "Tamar Aloni" }; // add teammates here if needed

        public string Course { get; private set; } =
            "Project 10212 • Semester B • 2025";

        public string RepoUrl { get; private set; } =
            "https://github.com/AloniTamar/25B-21201A-Connect4";

        public string Description { get; private set; } =
            "A client–server Connect Four system: ASP.NET Core Razor Pages + Web API server, " +
            "WinForms client with animations, SQL Server back-end, and a local SQLite replay store.";

        public void OnGet() { }
    }
}
