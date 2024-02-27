using System;
using System.Windows.Forms;
using Akka.Actor;
using Akka.Configuration;

namespace ChartApp
{
    static class Program
    {
        /// <summary>
        /// ActorSystem we'll be using to publish data to charts
        /// and subscribe from performance counters
        /// </summary>
        public static ActorSystem ChartActors;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var config = ConfigurationFactory.ParseString(@"
akka.remote.helios.tcp {
              transport-class =
           ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
              transport-protocol = tcp
              port = 8091
              hostname = ""127.0.0.1""
          }");

            ChartActors = ActorSystem.Create("ChartActors");

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }
    }
}
