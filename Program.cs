using System;
using System.Collections.Generic;
using Limilabs.Client.IMAP;
using Limilabs.Mail;
using MySql.Data.MySqlClient;
using Limilabs.Mail.Headers;
using Limilabs.Mail.Licensing;
using Limilabs.Client;
using System.Net.Security;

namespace mailConnection
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileName = LicenseHelper.GetLicensePath();
            LicenseStatus status = LicenseHelper.GetLicenseStatus();

            if (status != LicenseStatus.Valid)
            {
                throw new Exception("License is not valid: " + status);

            }
            else
            {
                Console.WriteLine("Licencia valida");

                string connectionString = "Server=127.0.0.1;Port=3306;Database=SistemaTickets;Uid=user_sistema_tickets;password=tXDZD0VnznWLdZcB4QVN;";
                //MySqlConnection conexion = new MySqlConnection(connectionString);
                try
                {
                    MySqlConnection conn = new MySqlConnection();
                    conn.ConnectionString = connectionString;

                    Console.WriteLine("Connecting to MySQL...");
                    conn.Open();

                    MySqlCommand cmd = new MySqlCommand();

                    cmd.Connection = conn;

                    using (Imap imap = new Imap())
                    {

                        imap.ServerCertificateValidate += new ServerCertificateValidateEventHandler(Validate);

                        imap.ConnectSSL("open.cobama.com.mx");  // or ConnectSSL for SSL
                        imap.UseBestLogin("tickets@cobama.com.mx", "Mhtemplos2022+");
                        imap.SelectInbox();
                        List<long> uids = imap.Search(Flag.Unseen);
                        foreach (long uid in uids)
                        {
                            IMail email = new MailBuilder().CreateFromEml(imap.GetMessageByUID(uid));

                            cmd.CommandText = "INSERT INTO correos (asunto, enviado, mensaje, created_at) VALUES ( ?asunto, ?enviado, ?mensaje, ?created_at ) ";
                            cmd.Parameters.Add("?asunto", MySqlDbType.VarChar).Value = email.Subject;

                            foreach (MailBox m in email.From)
                            {
                                Console.WriteLine(m.Address);
                                Console.WriteLine(m.Name);
                                cmd.Parameters.Add("?enviado", MySqlDbType.VarChar).Value = m.Name + " " + m.Address;
                            }

                            cmd.Parameters.Add("?mensaje", MySqlDbType.VarChar).Value = email.Text;
                            cmd.Parameters.Add("?created_at", MySqlDbType.DateTime).Value = email.Date;
                            cmd.ExecuteNonQuery();

                            Console.WriteLine(email.Subject);
                            Console.WriteLine(email.From);
                            Console.WriteLine(email.Text);
                        }
                        imap.Close();
                    }
                    conn.Close();
                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void Validate(
        object sender,
        ServerCertificateValidateEventArgs e)
            {
                const SslPolicyErrors ignoredErrors =
                    // self-signed
                    SslPolicyErrors.RemoteCertificateChainErrors
                    // name mismatch
                    | SslPolicyErrors.RemoteCertificateNameMismatch;

                if ((e.SslPolicyErrors & ~ignoredErrors)
                    == SslPolicyErrors.None)
                {
                    e.IsValid = true;
                    return;
                }
                e.IsValid = false;
            }
    }
}
