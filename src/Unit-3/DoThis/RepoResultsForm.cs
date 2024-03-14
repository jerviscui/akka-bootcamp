using System;
using System.Windows.Forms;
using Akka.Actor;
using GithubActors.Actors;

namespace GithubActors
{
    public partial class RepoResultsForm : Form
    {
        #region Message

        public class SubscribeToProgressUpdates
        {
            public SubscribeToProgressUpdates(IActorRef subscriber)
            {
                Subscriber = subscriber;
            }

            public IActorRef Subscriber { get; private set; }
        }

        #endregion

        private IActorRef _formActor;

        private readonly IActorRef _githubCoordinator;

        private readonly RepoKey _repo;

        public RepoResultsForm(IActorRef githubCoordinator, RepoKey repo)
        {
            _githubCoordinator = githubCoordinator;
            _repo = repo;
            InitializeComponent();
        }

        private void RepoResultsForm_Load(object sender, EventArgs e)
        {
            _formActor =
                Program.GithubActors.ActorOf(
                    Props.Create(() => new RepoResultsActor(dgUsers, tsStatus, tsProgress))
                        .WithDispatcher("akka.actor.synchronized-dispatcher")); //run on the UI thread

            Text = string.Format("Repos Similar to {0} / {1}", _repo.Owner, _repo.Repo);

            //start subscribing to updates
            _githubCoordinator.Tell(new SubscribeToProgressUpdates(_formActor));
        }

        private void RepoResultsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //kill the form actor
            _formActor.Tell(PoisonPill.Instance);
        }
    }
}
