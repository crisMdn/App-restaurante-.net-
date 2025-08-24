using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Windows.Forms;

namespace PupusariaApp
{
    public partial class Form1 : Form
    {
        // AJUSTA LA RUTA
        private const string DbPath = @"D:\sqlite\clientes.db";
        private readonly string _connStr = $"Data Source={DbPath};Version=3;Foreign Keys=True;";

        // Usuario/rol
        private readonly string _usuarioActual;
        private readonly bool _esGerente;

        // Estado de edición
        private bool _editMode = false;
        private long _editClienteId = 0;
        private long _editDireccionId = 0;

        // Búsqueda/impresión
        private TextBox txtTelefono = null!;
        private Button btnBuscar = null!;
        private Button btnImprimir = null!;
        private Button btnNuevo = null!;
        private Button btnEditar = null!;
        private DataGridView gridResultados = null!;
        private DataGridView gridHistorial = null!;

        // Gerencia
        private Button btnMantenimiento = null!;
        private Button btnLimpiarReportes = null!;
        private Button btnExportarMes = null!;

        // Alta / edición
        private Panel pnlNuevo = null!;
        private Button btnGuardar = null!;
        private TextBox txtNombreN = null!;
        private TextBox txtTel1N = null!;
        private TextBox txtTel2N = null!;
        private TextBox txtDirN = null!;
        private TextBox txtCiudadN = null!;
        private TextBox txtPaisN = null!;

        // Impresión
        private readonly PrintDocument _printDoc = new PrintDocument();
        private string _textoAImprimir = "";

        public Form1(string usuarioActual, bool esGerente)
        {
            _usuarioActual = string.IsNullOrWhiteSpace(usuarioActual) ? Environment.UserName : usuarioActual;
            _esGerente = esGerente;

            InitializeComponent();
            ConstruirUI();

            // eventos
            btnBuscar.Click += BtnBuscar_Click;
            btnImprimir.Click += BtnImprimir_Click;
            btnNuevo.Click += (s, e) => { _editMode = false; btnGuardar.Text = "Guardar"; pnlNuevo.Visible = !pnlNuevo.Visible; };
            btnEditar.Click += BtnEditar_Click;
            btnGuardar.Click += BtnGuardar_Click;
            _printDoc.PrintPage += PrintDoc_PrintPage;

            if (_esGerente)
            {
                btnMantenimiento.Click += BtnMantenimiento_Click;
                btnLimpiarReportes.Click += BtnLimpiarReportes_Click;
                btnExportarMes.Click += BtnExportarMes_Click;
            }

            CargarHistorial();
        }

        private void ConstruirUI()
        {
            Text = "Pupusería - Búsqueda, Impresión y Altas";
            Width = 1100;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // fila búsqueda + botones
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // panel alta/edición
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 60));   // resultados
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));   // historial
            Controls.Add(root);

            // --- Fila 1: búsqueda + botones ---
            var fila1 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            var lblTel = new Label { Text = "Teléfono:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) };
            txtTelefono = new TextBox { Width = 200 };
            btnBuscar = new Button { Text = "Buscar", AutoSize = true, Margin = new Padding(6) };
            btnImprimir = new Button { Text = "Imprimir", AutoSize = true, Margin = new Padding(6) };
            btnNuevo = new Button { Text = "Nuevo registro", AutoSize = true, Margin = new Padding(6) };
            btnEditar = new Button { Text = "Editar", AutoSize = true, Margin = new Padding(6) };

            btnMantenimiento   = new Button { Text = "Mantenimiento", AutoSize = true, Margin = new Padding(6), Visible = _esGerente };
            btnLimpiarReportes = new Button { Text = "Limpiar reportes…", AutoSize = true, Margin = new Padding(6), Visible = _esGerente };
            btnExportarMes     = new Button { Text = "Exportar mes…", AutoSize = true, Margin = new Padding(6), Visible = _esGerente };

            fila1.Controls.AddRange(new Control[] {
                lblTel, txtTelefono, btnBuscar, btnImprimir, btnNuevo, btnEditar,
                btnMantenimiento, btnLimpiarReportes, btnExportarMes
            });
            root.Controls.Add(fila1);

            // --- Fila 2: panel ALTA/EDICIÓN (oculto por defecto) ---
            pnlNuevo = new Panel { Dock = DockStyle.Top, Visible = false, Padding = new Padding(6) };
            var gridAlta = new TableLayoutPanel
            {
                ColumnCount = 6,
                RowCount = 2,
                Dock = DockStyle.Top,
                AutoSize = true
            };
            // fila 1
            gridAlta.Controls.Add(new Label { Text = "Nombre*", AutoSize = true }, 0, 0);
            gridAlta.Controls.Add(new Label { Text = "Teléfono*", AutoSize = true }, 1, 0);
            gridAlta.Controls.Add(new Label { Text = "Tel. 2", AutoSize = true }, 2, 0);
            gridAlta.Controls.Add(new Label { Text = "Dirección*", AutoSize = true }, 3, 0);
            gridAlta.Controls.Add(new Label { Text = "Ciudad", AutoSize = true }, 4, 0);
            gridAlta.Controls.Add(new Label { Text = "País", AutoSize = true }, 5, 0);

            // fila 2
            txtNombreN = new TextBox { Width = 180 };
            txtTel1N   = new TextBox { Width = 120 };
            txtTel2N   = new TextBox { Width = 120 };
            txtDirN    = new TextBox { Width = 240 };
            txtCiudadN = new TextBox { Width = 120 };
            txtPaisN   = new TextBox { Width = 120 };

            gridAlta.Controls.Add(txtNombreN, 0, 1);
            gridAlta.Controls.Add(txtTel1N,   1, 1);
            gridAlta.Controls.Add(txtTel2N,   2, 1);
            gridAlta.Controls.Add(txtDirN,    3, 1);
            gridAlta.Controls.Add(txtCiudadN, 4, 1);
            gridAlta.Controls.Add(txtPaisN,   5, 1);

            btnGuardar = new Button { Text = "Guardar", AutoSize = true, Margin = new Padding(6) };
            var filaGuardar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            filaGuardar.Controls.Add(btnGuardar);

            pnlNuevo.Controls.Add(filaGuardar);
            pnlNuevo.Controls.Add(gridAlta);
            root.Controls.Add(pnlNuevo);

            // --- Fila 3: resultados ---
            gridResultados = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            root.Controls.Add(gridResultados);

            // --- Fila 4: historial ---
            gridHistorial = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            root.Controls.Add(gridHistorial);
        }

        // BUSCAR
        private void BtnBuscar_Click(object sender, EventArgs e)
        {
            string tel = txtTelefono.Text.Trim();
            if (string.IsNullOrEmpty(tel))
            {
                MessageBox.Show("Ingresa un teléfono.");
                return;
            }

            string sql = @"
SELECT c.id_cliente, c.nombre, c.telefono, c.telefono_secundario,
       d.id_direccion, d.direccion, IFNULL(d.ciudad,'') AS ciudad, IFNULL(d.pais,'') AS pais
FROM clientes c
LEFT JOIN direcciones d ON d.id_cliente = c.id_cliente
WHERE c.telefono = @tel OR c.telefono_secundario = @tel
ORDER BY c.nombre;";

            using var con = new SQLiteConnection(_connStr);
            con.Open();
            using var da = new SQLiteDataAdapter(sql, con);
            da.SelectCommand.Parameters.AddWithValue("@tel", tel);

            var dt = new DataTable();
            da.Fill(dt);
            gridResultados.DataSource = dt;
        }

        // IMPRIMIR
        private void BtnImprimir_Click(object sender, EventArgs e)
        {
            if (gridResultados.CurrentRow == null)
            {
                MessageBox.Show("Selecciona una fila para imprimir.");
                return;
            }

            try
            {
                int idDir        = Convert.ToInt32(gridResultados.CurrentRow.Cells["id_direccion"].Value);
                string cliente   = Convert.ToString(gridResultados.CurrentRow.Cells["nombre"].Value);
                string tel       = Convert.ToString(gridResultados.CurrentRow.Cells["telefono"].Value);
                string direccion = Convert.ToString(gridResultados.CurrentRow.Cells["direccion"].Value);

                string ciudad = gridResultados.Columns.Contains("ciudad")
                    ? Convert.ToString(gridResultados.CurrentRow.Cells["ciudad"].Value) : "";

                string pais = gridResultados.Columns.Contains("pais")
                    ? Convert.ToString(gridResultados.CurrentRow.Cells["pais"].Value) : "";

                using (var con = new SQLiteConnection(_connStr))
                {
                    con.Open();
                    using var cmd = new SQLiteCommand(
                        "INSERT INTO cola_impresion (id_direccion, usuario) VALUES (@id, @usr);", con);
                    cmd.Parameters.AddWithValue("@id", idDir);
                    cmd.Parameters.AddWithValue("@usr", _usuarioActual);
                    cmd.ExecuteNonQuery();
                }

                _textoAImprimir =
$@"CLIENTE: {cliente}
TEL: {tel}
DIR: {direccion}
{(string.IsNullOrWhiteSpace(ciudad) ? "" : "CIUDAD: " + ciudad)}
{(string.IsNullOrWhiteSpace(pais) ? "" : "PAÍS: " + pais)}
Imp.: {DateTime.Now:yyyy-MM-dd HH:mm}  Usuario: {_usuarioActual}";

                _printDoc.Print();
                CargarHistorial();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al imprimir: " + ex.Message);
            }
        }

        // EDITAR
        private void BtnEditar_Click(object sender, EventArgs e)
        {
            if (gridResultados.CurrentRow == null)
            {
                MessageBox.Show("Selecciona una fila para editar.");
                return;
            }

            try
            {
                _editClienteId   = Convert.ToInt64(gridResultados.CurrentRow.Cells["id_cliente"].Value);
                _editDireccionId = Convert.ToInt64(gridResultados.CurrentRow.Cells["id_direccion"].Value);

                txtNombreN.Text = Convert.ToString(gridResultados.CurrentRow.Cells["nombre"].Value);
                txtTel1N.Text   = Convert.ToString(gridResultados.CurrentRow.Cells["telefono"].Value);
                txtTel2N.Text   = Convert.ToString(gridResultados.CurrentRow.Cells["telefono_secundario"].Value);
                txtDirN.Text    = Convert.ToString(gridResultados.CurrentRow.Cells["direccion"].Value);

                txtCiudadN.Text = gridResultados.Columns.Contains("ciudad")
                    ? Convert.ToString(gridResultados.CurrentRow.Cells["ciudad"].Value) : "";

                txtPaisN.Text = gridResultados.Columns.Contains("pais")
                    ? Convert.ToString(gridResultados.CurrentRow.Cells["pais"].Value) : "";

                _editMode = true;
                btnGuardar.Text = "Actualizar";
                pnlNuevo.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo cargar la selección: " + ex.Message);
            }
        }

        // GUARDAR (ALTA o ACTUALIZAR)
        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            string nombre = txtNombreN.Text.Trim();
            string tel1   = txtTel1N.Text.Trim();
            string tel2   = string.IsNullOrWhiteSpace(txtTel2N.Text) ? null : txtTel2N.Text.Trim();
            string dir    = txtDirN.Text.Trim();
            string ciudad = string.IsNullOrWhiteSpace(txtCiudadN.Text) ? null : txtCiudadN.Text.Trim();
            string pais   = string.IsNullOrWhiteSpace(txtPaisN.Text) ? null : txtPaisN.Text.Trim();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(tel1) || string.IsNullOrEmpty(dir))
            {
                MessageBox.Show("Nombre, Teléfono y Dirección son obligatorios.");
                return;
            }

            using var con = new SQLiteConnection(_connStr);
            con.Open();
            using var tx = con.BeginTransaction();

            try
            {
                if (!_editMode)
                {
                    using (var ver = new SQLiteCommand("SELECT COUNT(1) FROM clientes WHERE telefono=@t", con, tx))
                    {
                        ver.Parameters.AddWithValue("@t", tel1);
                        long existe = (long)ver.ExecuteScalar();
                        if (existe > 0)
                        {
                            MessageBox.Show("Ya existe un cliente con ese teléfono.");
                            tx.Rollback();
                            return;
                        }
                    }

                    long idCliente;
                    using (var cmdC = new SQLiteCommand(
                        "INSERT INTO clientes (nombre, telefono, telefono_secundario) VALUES (@n,@t1,@t2);", con, tx))
                    {
                        cmdC.Parameters.AddWithValue("@n",  nombre);
                        cmdC.Parameters.AddWithValue("@t1", tel1);
                        cmdC.Parameters.AddWithValue("@t2", (object?)tel2 ?? DBNull.Value);
                        cmdC.ExecuteNonQuery();
                    }
                    using (var last = new SQLiteCommand("SELECT last_insert_rowid();", con, tx))
                        idCliente = (long)(long)last.ExecuteScalar();

                    using (var cmdD = new SQLiteCommand(
                        "INSERT INTO direcciones (id_cliente, direccion, ciudad, pais) VALUES (@c,@d,@ci,@p);", con, tx))
                    {
                        cmdD.Parameters.AddWithValue("@c", idCliente);
                        cmdD.Parameters.AddWithValue("@d", dir);
                        cmdD.Parameters.AddWithValue("@ci", (object?)ciudad ?? DBNull.Value);
                        cmdD.Parameters.AddWithValue("@p",  (object?)pais   ?? DBNull.Value);
                        cmdD.ExecuteNonQuery();
                    }

                    tx.Commit();
                    MessageBox.Show("Datos guardados.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    using (var cmdC = new SQLiteCommand(
                        "UPDATE clientes SET nombre=@n, telefono=@t1, telefono_secundario=@t2 WHERE id_cliente=@id;", con, tx))
                    {
                        cmdC.Parameters.AddWithValue("@n", nombre);
                        cmdC.Parameters.AddWithValue("@t1", tel1);
                        cmdC.Parameters.AddWithValue("@t2", (object?)tel2 ?? DBNull.Value);
                        cmdC.Parameters.AddWithValue("@id", _editClienteId);
                        cmdC.ExecuteNonQuery();
                    }

                    using (var cmdD = new SQLiteCommand(
                        "UPDATE direcciones SET direccion=@d, ciudad=@ci, pais=@p WHERE id_direccion=@idd;", con, tx))
                    {
                        cmdD.Parameters.AddWithValue("@d",  dir);
                        cmdD.Parameters.AddWithValue("@ci", (object?)ciudad ?? DBNull.Value);
                        cmdD.Parameters.AddWithValue("@p",  (object?)pais   ?? DBNull.Value);
                        cmdD.Parameters.AddWithValue("@idd", _editDireccionId);
                        cmdD.ExecuteNonQuery();
                    }

                    tx.Commit();
                    MessageBox.Show("Datos actualizados.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    _editMode = false;
                    _editClienteId = _editDireccionId = 0;
                    btnGuardar.Text = "Guardar";
                }

                txtNombreN.Clear(); txtTel1N.Clear(); txtTel2N.Clear();
                txtDirN.Clear(); txtCiudadN.Clear(); txtPaisN.Clear();

                if (!string.IsNullOrEmpty(txtTelefono.Text)) BtnBuscar_Click(this, EventArgs.Empty);
                CargarHistorial();
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                MessageBox.Show("Error al guardar/actualizar: " + ex.Message);
            }
        }

        // HISTORIAL
        private void CargarHistorial()
        {
            using var con = new SQLiteConnection(_connStr);
            con.Open();
            const string sql = @"SELECT id_impresion, id_direccion, fecha_impresion, usuario
                                 FROM historial_impresiones
                                 ORDER BY fecha_impresion DESC LIMIT 200;";
            using var da = new SQLiteDataAdapter(sql, con);
            var dt = new DataTable();
            da.Fill(dt);
            gridHistorial.DataSource = dt;
        }

        // IMPRESIÓN
        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            var font = new Font("Segoe UI", 10);
            e.Graphics.DrawString(_textoAImprimir, font, Brushes.Black,
                new RectangleF(50, 50, e.PageBounds.Width - 100, e.PageBounds.Height - 100));
            e.HasMorePages = false;
        }

        // ====== GERENCIA ======

        // Compactar DB
        private void BtnMantenimiento_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Esto compactará la base de datos.\nAsegúrate de que nadie esté usando la app.\n\n¿Continuar?",
                "Mantenimiento", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                using var con = new SQLiteConnection(_connStr);
                con.Open();
                using var cmd = new SQLiteCommand("VACUUM;", con);
                cmd.ExecuteNonQuery();
                MessageBox.Show("Base compactada correctamente.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en mantenimiento: " + ex.Message);
            }
        }

        // Diálogo simple para elegir mes/año
        private sealed class MesDialog : Form
        {
            public DateTime MesSeleccionado => new DateTime(_dtp.Value.Year, _dtp.Value.Month, 1);
            private readonly DateTimePicker _dtp = new DateTimePicker();

            public MesDialog(string titulo = "Elegir mes")
            {
                Text = titulo;
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false; MinimizeBox = false; Width = 300; Height = 130;

                _dtp.Format = DateTimePickerFormat.Custom;
                _dtp.CustomFormat = "MMMM yyyy";
                _dtp.ShowUpDown = true;
                _dtp.Left = 15; _dtp.Top = 10; _dtp.Width = 250;

                var ok = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Left = 115, Top = 45 };
                var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Left = 200, Top = 45 };

                Controls.AddRange(new Control[] { _dtp, ok, cancel });
                AcceptButton = ok; CancelButton = cancel;
            }
        }

        // Limpiar reportes por mes (confirmado por gerente)
        private void BtnLimpiarReportes_Click(object? sender, EventArgs e)
        {
            using var dlg = new MesDialog("Eliminar reportes hasta el mes…");
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var mes = dlg.MesSeleccionado;
            var finMes = mes.AddMonths(1).AddDays(-1);

            if (MessageBox.Show(
                $"Se eliminarán reportes con fecha de creación <= {finMes:yyyy-MM-dd}.\n" +
                "Esta acción no se puede deshacer.\n\n¿Continuar?",
                "Confirmar limpieza", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                using var con = new SQLiteConnection(_connStr);
                con.Open();
                using var tx = con.BeginTransaction();
                using (var cmd = new SQLiteCommand(
                    "DELETE FROM reportes WHERE date(fecha_creacion) <= date(@fin);", con, tx))
                {
                    cmd.Parameters.AddWithValue("@fin", finMes.ToString("yyyy-MM-dd"));
                    int n = cmd.ExecuteNonQuery();
                    tx.Commit();
                    MessageBox.Show($"Limpieza completa. Registros eliminados: {n}.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al limpiar: " + ex.Message);
            }
        }

        // Exportar reportes de un mes a CSV (y opción de enviar por correo)
        private void BtnExportarMes_Click(object? sender, EventArgs e)
        {
            using var dlg = new MesDialog("Exportar reportes del mes…");
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var mes = dlg.MesSeleccionado;
            var ini = mes;
            var fin = mes.AddMonths(1).AddDays(-1);

            // 1) Consultar
            DataTable dt = new DataTable();
            using (var con = new SQLiteConnection(_connStr))
            {
                con.Open();
                using var da = new SQLiteDataAdapter(@"
                    SELECT r.id_reporte, r.estado, r.motivo, r.observacion,
                           r.fecha_creacion, r.fecha_cierre,
                           c.nombre AS cliente, c.telefono, IFNULL(d.direccion,'') AS direccion
                    FROM reportes r
                    JOIN clientes c ON c.id_cliente = r.id_cliente
                    LEFT JOIN direcciones d ON d.id_direccion = r.id_direccion
                    WHERE date(r.fecha_creacion) BETWEEN date(@ini) AND date(@fin)
                    ORDER BY r.fecha_creacion;", con);
                da.SelectCommand.Parameters.AddWithValue("@ini", ini.ToString("yyyy-MM-dd"));
                da.SelectCommand.Parameters.AddWithValue("@fin", fin.ToString("yyyy-MM-dd"));
                da.Fill(dt);
            }

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("No hay reportes para ese mes.");
                return;
            }

            // 2) Guardar CSV
            string sugerido = $"reportes_{mes:yyyy_MM}.csv";
            using var sfd = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = sugerido };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            ExportarCSV(dt, sfd.FileName);
            MessageBox.Show("Archivo guardado.");

            // 3) Preguntar si desea enviar por correo
            if (MessageBox.Show("¿Enviar por correo al gerente?", "Enviar", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    // Ajusta tus credenciales/host
                    EnviarCorreoConAdjunto(
                        smtpHost: "smtp.tu-servidor.com",
                        smtpPort: 587,
                        smtpUsuario: "usuario@empresa.com",
                        smtpPass: "APP-PASSWORD",
                        from: "usuario@empresa.com",
                        to: "gerente@empresa.com",
                        asunto: $"Reportes {mes:MMMM yyyy}",
                        cuerpo: "Adjunto reportes del mes.",
                        adjuntoPath: sfd.FileName,
                        enableSsl: true
                    );
                    MessageBox.Show("Correo enviado.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al enviar correo: " + ex.Message);
                }
            }
        }

        private static void ExportarCSV(DataTable dt, string path)
        {
            var sb = new StringBuilder();

            // encabezados
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(dt.Columns[i].ColumnName.Replace("\"", "\"\"")).Append('"');
            }
            sb.AppendLine();

            // filas
            foreach (DataRow r in dt.Rows)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    var val = r[i]?.ToString() ?? "";
                    sb.Append('"').Append(val.Replace("\"", "\"\"")).Append('"');
                }
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static void EnviarCorreoConAdjunto(
            string smtpHost, int smtpPort, string smtpUsuario, string smtpPass,
            string from, string to, string asunto, string cuerpo, string adjuntoPath, bool enableSsl = true)
        {
            using var smtp = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(smtpUsuario, smtpPass)
            };
            using var mail = new MailMessage(from, to, asunto, cuerpo);
            mail.Attachments.Add(new Attachment(adjuntoPath));
            smtp.Send(mail);
        }
    }
}
