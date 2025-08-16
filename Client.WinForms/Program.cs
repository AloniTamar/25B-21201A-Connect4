using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Client.WinForms.Infrastructure;

namespace Client.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Register custom URL scheme (connect4://) for the current user
            ProtocolRegistrar.EnsureRegistered();

            // Parse connect4://new-game?playerId=#
            var (_, playerId) = ProtocolRegistrar.TryParseProtocolArgs(args);

            ApplicationConfiguration.Initialize();

            // Create the startup form (first public Form with a parameterless ctor)
            var mainForm = CreateMainForm();

            // Pass PlayerId to the form, but DO NOT auto-start a game
            if (playerId.HasValue)
            {
                var prop = mainForm.GetType().GetProperty("StartupPlayerId",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && prop.PropertyType == typeof(int?))
                    prop.SetValue(mainForm, playerId.Value);
                else
                    mainForm.Tag = playerId.Value; // fallback stash
            }

            Application.Run(mainForm);
        }

        private static Form CreateMainForm()
        {
            var formType = typeof(Program).Assembly
                .GetTypes()
                .FirstOrDefault(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.IsPublic &&
                    t.IsSubclassOf(typeof(Form)) &&
                    t.GetConstructor(Type.EmptyTypes) != null);

            if (formType == null)
                throw new InvalidOperationException("No startup Form found with a public parameterless constructor.");

            return (Form)Activator.CreateInstance(formType)!;
        }
    }
}
