using System;
using System.Linq;
using Akka.Actor;
using Akka.Routing;

namespace GithubActors.Actors
{
    /// <summary>
    /// Top-level actor responsible for coordinating and launching repo-processing jobs
    /// </summary>
    public class GithubCommanderActor : ReceiveActor, IWithUnboundedStash
    {
        #region Message classes

        public const string Name = "commander";

        public const string Path = $"akka://GithubActors/user/{Name}";

        public class BeginJob
        {
            public BeginJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class LaunchRepoResultsWindow
        {
            public LaunchRepoResultsWindow(RepoKey repo, IActorRef coordinator)
            {
                Repo = repo;
                Coordinator = coordinator;
            }

            public RepoKey Repo { get; private set; }

            public IActorRef Coordinator { get; private set; }
        }

        #endregion

        private IActorRef _canAcceptJobSender;

        private IActorRef _coordinator;

        private int pendingJobReplies;

        public GithubCommanderActor()
        {
            Ready();
        }

        /// <inheritdoc />
        public IStash Stash { get; set; }

        private RepoKey _repoJob;

        private void Ready()
        {
            Receive<GithubValidatorActor.CanAcceptJob>(job =>
            {
                _coordinator.Tell(job);

                _repoJob = job.Repo;

                BecomeAsking();
            });

            Receive<GithubCoordinatorActor.UnableToAcceptJob>(job => { });

            Receive<GithubCoordinatorActor.AbleToAcceptJob>(job =>
            {
                //BroadcastGroup CanAcceptJob 广播给三个 GithubCoordinatorActor
                //第一个 AbleToAcceptJob 在 Asking 中处理
                //此处接受另外两次 AbleToAcceptJob
            });
        }

        private void BecomeAsking()
        {
            _canAcceptJobSender = Sender;

            // block, but ask the router for the number of routees. Avoids magic numbers.
            pendingJobReplies = _coordinator.Ask<Routees>(new GetRoutees()).Result.Members.Count();

            Become(Asking);

            Context.SetReceiveTimeout(TimeSpan.FromSeconds(3));
        }

        private void Asking()
        {
            Receive<ReceiveTimeout>(timeout =>
            {
                _canAcceptJobSender.Tell(new GithubCoordinatorActor.UnableToAcceptJob(_repoJob));
                BecomeReady();
            });

            Receive<GithubValidatorActor.CanAcceptJob>(job =>
                Stash.Stash());

            Receive<GithubCoordinatorActor.UnableToAcceptJob>(job =>
            {
                pendingJobReplies--;

                if (pendingJobReplies == 0)
                {
                    _canAcceptJobSender.Tell(job);
                    BecomeReady();
                }
            });

            Receive<GithubCoordinatorActor.AbleToAcceptJob>(job =>
            {
                _canAcceptJobSender.Tell(job);

                //start processing messages
                Sender.Tell(new BeginJob(job.Repo));

                //launch the new window to view results of the processing
                Context.ActorSelection(MainFormActor.Path)
                    .Tell(new LaunchRepoResultsWindow(job.Repo, Sender));

                BecomeReady();
            });
        }

        private void BecomeReady()
        {
            Become(Ready);
            Stash.UnstashAll();

            // cancel ReceiveTimeout
            Context.SetReceiveTimeout(null);
        }

        protected override void PreStart()
        {
            // create a broadcast router who will ask all of them
            // if they're available for work
            _coordinator = Context.ActorOf(
                Props.Create(() => new GithubCoordinatorActor()).WithRouter(FromConfig.Instance),
                GithubCoordinatorActor.Name);

            base.PreStart();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            //kill off the old coordinator so we can recreate it from scratch
            _coordinator.Tell(PoisonPill.Instance);
            base.PreRestart(reason, message);
        }
    }
}
