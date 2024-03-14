using System;
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

        private void Ready()
        {
            Receive<GithubValidatorActor.CanAcceptJob>(job =>
            {
                _coordinator.Tell(job);

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
            pendingJobReplies = 3; //the number of routees

            Become(Asking);
        }

        private void Asking()
        {
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
        }

        protected override void PreStart()
        {
            var coor1 = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()),
                GithubCoordinatorActor.Name + "1");
            var coor2 = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()),
                GithubCoordinatorActor.Name + "2");
            var coor3 = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()),
                GithubCoordinatorActor.Name + "3");

            // create a broadcast router who will ask all of them
            // if they're available for work
            _coordinator = Context.ActorOf(Props.Empty.WithRouter(new BroadcastGroup(
                coor1.Path.ToStringWithAddress(),
                coor2.Path.ToStringWithAddress(),
                coor3.Path.ToStringWithAddress())));

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
