using Fiddler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FiddlerCore_macOS
{
    class Program
    {
        private static List<Session> sessions = new List<Session>();
        static async Task Main(string[] args)
        {
            FiddlerApplication.Prefs.SetBoolPref("fiddler.certmaker.bc.Debug", true);

            //// Uncomment below line if you need verbose logs from FiddlerCore
            // Fiddler.FiddlerApplication.Log.OnLogString += delegate (object sender, LogEventArgs oLEA) { Console.WriteLine("** LogString: " + oLEA.LogString); };

            // Force BouncyCastle as certificate provideer
            BCCertMaker.BCCertMaker certProvider = new BCCertMaker.BCCertMaker();
            CertMaker.oCertProvider = certProvider;

            FiddlerApplication.AfterSessionComplete += FiddlerApplication_AfterSessionComplete;
            FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;

            // The code below will always create a new certificate and will trust it
            // Trusting the certificate requires user to enter their password in a native macOS prompt
            // If you want to do it only once for the app on this machine, you can store the .p12 file on a well-known place
            // Then use certProvider.ReadRootCertificateAndPrivateKeyFromPkcs12File(rootCertificatePath, rootCertificatePassword); to force the app to use this certificate.
            // After that get the sha1 of the certificate with certProvider.GetRootCertificate().GetCertHashString();
            // Then check with the bash command "security trust-settings-export <file path>" if the sha1 of the certificate is in the trusted certificates inside the exported XML file.
            if (!CertMaker.createRootCert())
            {
                Console.WriteLine("Unable to create cert for FiddlerCore.");
                return;
            }

            TrustRootCertificate();

            FiddlerCoreStartupSettings startupSettings =
                                            new FiddlerCoreStartupSettingsBuilder()
                                                .ListenOnPort(8899)
                                                .DecryptSSL()
                                                .RegisterAsSystemProxy() // Don't use on macOS as it will not work as expected. Use SetSystemProxy method below for this purpose
                                                .Build();

            
            FiddlerApplication.Startup(startupSettings);

            Console.WriteLine("Proxy is now set, press enter to remove it");
            Console.ReadLine();

            FiddlerApplication.Shutdown();

            bool success = Fiddler.Utilities.WriteSessionArchive("sessions.saz", sessions.ToArray(), "passwoRd");
            if (success)
            {
                Console.WriteLine("Successfully written the sessions to file!");
            }
        }

        private static void FiddlerApplication_AfterSessionComplete(Session oSession)
        {
            sessions.Add(oSession);
        }

        private static void FiddlerApplication_BeforeRequest(Session oSession)
        {
            // Console.WriteLine("Before executing requst for url: " + oSession.fullUrl);
        }

        private static void TrustRootCertificate()
        {
            CertMaker.EnsureReady();
            ICertificateProvider5 certificateProvider = (ICertificateProvider5)CertMaker.oCertProvider;

            // first export the certificate to a temp file, as the commands for import work with actual file
            string certificatePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            certificateProvider.WriteRootCertificateToDerEncodedFile(certificatePath);

        }
    }
}
