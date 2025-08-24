using System;
using System.Windows.Forms;

namespace PupusariaApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Mostrar login antes de abrir el sistema
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK) return;

            var usuario = string.IsNullOrWhiteSpace(login.Usuario)
                ? Environment.UserName
                : login.Usuario;

            var esGerente = login.EsGerente;

            Application.Run(new Form1(usuario, esGerente));
        }
    }
}
