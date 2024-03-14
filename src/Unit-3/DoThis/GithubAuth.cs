using System;
using System.Diagnostics;
using System.Windows.Forms;
using Akka.Actor;
using GithubActors.Actors;

namespace GithubActors
{
    public partial class GithubAuth : Form
    {
        #region Messages

        public class Authenticate
        {
            public Authenticate(string oAuthToken)
            {
                OAuthToken = oAuthToken;
            }

            public string OAuthToken { get; private set; }
        }

        #endregion

        private IActorRef _authActor;

        public GithubAuth()
        {
            InitializeComponent();
        }

        private void GithubAuth_Load(object sender, EventArgs e)
        {
            linkGhLabel.Links.Add(new LinkLabel.Link
                { LinkData = "https://help.github.com/articles/creating-an-access-token-for-command-line-use/" });
            _authActor =
                Program.GithubActors.ActorOf(Props.Create(() => new GithubAuthenticationActor(lblAuthStatus, this)),
                    GithubAuthenticationActor.Name);
        }

        private void linkGhLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var link = e.Link.LinkData as string;
            if (link != null)
            {
                //Send the URL to the operating system via windows shell
                Process.Start(link);
            }
        }

        private void btnAuthenticate_Click(object sender, EventArgs e)
        {
            _authActor.Tell(new Authenticate(tbOAuth.Text));
        }
    }
}
