using System;
using System.Windows.Forms;

namespace PupusariaApp
{
    public class LoginForm : Form
    {
        private TextBox txtUsuario = new TextBox();
        private CheckBox chkGerente = new CheckBox();      // <-- NUEVO
        private TextBox txtClave = new TextBox();          // <-- NUEVO
        private Button btnOk = new Button();
        private Button btnCancelar = new Button();

        private const string CLAVE_GERENCIA = "GERENTE2025"; // <-- cámbiala

        public string Usuario => txtUsuario.Text.Trim();
        public bool EsGerente { get; private set; } = false; // <-- NUEVO

        public LoginForm()
        {
            Text = "Identificación";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            Width = 380; Height = 210;

            var lblU = new Label { Text = "Usuario:", Left = 15, Top = 20, AutoSize = true };
            txtUsuario.Left = 90; txtUsuario.Top = 18; txtUsuario.Width = 260;

            chkGerente.Text = "Soy gerente";
            chkGerente.Left = 90; chkGerente.Top = 55; chkGerente.CheckedChanged += (_, __) =>
            {
                txtClave.Enabled = chkGerente.Checked;
            };

            var lblC = new Label { Text = "Clave:", Left = 15, Top = 85, AutoSize = true };
            txtClave.Left = 90; txtClave.Top = 83; txtClave.Width = 260; txtClave.PasswordChar = '*'; txtClave.Enabled = false;

            btnOk.Text = "Ingresar";
            btnOk.Left = 190; btnOk.Top = 120; btnOk.DialogResult = DialogResult.OK;
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(Usuario))
                {
                    MessageBox.Show("Ingresa tu nombre de usuario.");
                    this.DialogResult = DialogResult.None;
                    return;
                }
                if (chkGerente.Checked)
                {
                    if (txtClave.Text != CLAVE_GERENCIA)
                    {
                        MessageBox.Show("Clave de gerencia incorrecta.");
                        this.DialogResult = DialogResult.None;
                        return;
                    }
                    EsGerente = true;
                }
            };

            btnCancelar.Text = "Cancelar";
            btnCancelar.Left = 275; btnCancelar.Top = 120; btnCancelar.DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { lblU, txtUsuario, chkGerente, lblC, txtClave, btnOk, btnCancelar });
            AcceptButton = btnOk; CancelButton = btnCancelar;
        }
    }
}
