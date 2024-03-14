using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Routing;
using Octokit;

namespace GithubActors.Actors
{
    /// <summary>
    /// Actor responsible for publishing data about the results
    /// of a github operation
    /// </summary>
    public class GithubCoordinatorActor : ReceiveActor
    {
        #region Message classes

        public const string Name = "coordinator";

        public const string Path = $"akka://GithubActors/user/{GithubCommanderActor.Name}/{Name}";

        public class WrapGithubProgressStats : GithubProgressStats
        {
            public static WrapGithubProgressStats FromGithubProgressStats(GithubProgressStats stats)
            {
                return new WrapGithubProgressStats
                {
                    EndTime = stats.EndTime,
                    StartTime = stats.StartTime,
                    UsersThusFar = stats.UsersThusFar,
                    ExpectedUsers = stats.ExpectedUsers,
                    QueryFailures = stats.QueryFailures
                };
            }
        }

        public class WrapSimilarRepo : SimilarRepo
        {
            /// <inheritdoc />
            public WrapSimilarRepo(Repository repo) : base(repo)
            {
            }
        }

        /// <summary>
        /// Query an individual starrer
        /// </summary>
        public class QueryStarrer
        {
            public QueryStarrer(string login)
            {
                Login = login;
            }

            public string Login { get; private set; }
        }

        public class QueryStarrers
        {
            public QueryStarrers(RepoKey key)
            {
                Key = key;
            }

            public RepoKey Key { get; private set; }
        }

        public class UnableToAcceptJob
        {
            public UnableToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class AbleToAcceptJob
        {
            public AbleToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class PublishUpdate
        {
            private PublishUpdate() { }

            public static PublishUpdate Instance { get; } = new();
        }

        /// <summary>
        /// Let the subscribers know we failed
        /// </summary>
        public class JobFailed
        {
            public JobFailed(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        #endregion

        private IActorRef _githubWorker;

        private RepoKey _currentRepo;

        private Dictionary<string, WrapSimilarRepo> _similarRepos;

        private HashSet<IActorRef> _subscribers;
        private ICancelable _publishTimer;

        private WrapGithubProgressStats _githubProgressStats;

        private bool _receivedInitialUsers = false;

        public GithubCoordinatorActor()
        {
            Waiting();
        }

        protected override void PreStart()
        {
            _githubWorker = Context.ActorOf(Props.Create(() => new GithubWorkerActor(GithubClientFactory.GetClient))
                .WithRouter(new RoundRobinPool(10)));
        }

        private void Waiting()
        {
            Receive<GithubValidatorActor.CanAcceptJob>(job =>
                Sender.Tell(new AbleToAcceptJob(job.Repo)));
            Receive<GithubCommanderActor.BeginJob>(job =>
            {
                BecomeWorking(job.Repo);

                //kick off the job to query the repo's list of starrers
                _githubWorker.Tell(new RetryableQuery(new QueryStarrers(job.Repo), 4));
            });
        }

        private void BecomeWorking(RepoKey repo)
        {
            _receivedInitialUsers = false;
            _currentRepo = repo;
            _subscribers = new HashSet<IActorRef>();
            _similarRepos = new Dictionary<string, WrapSimilarRepo>();
            _publishTimer = new Cancelable(Context.System.Scheduler);
            _githubProgressStats = new WrapGithubProgressStats();
            Become(Working);
        }

        private void BecomeWaiting()
        {
            //stop publishing
            _publishTimer.Cancel();
            Become(Waiting);
        }

        private void Working()
        {
            //received a downloaded user back from the github worker
            Receive<GithubWorkerActor.StarredReposForUser>(user =>
            {
                _githubProgressStats =
                    WrapGithubProgressStats.FromGithubProgressStats(_githubProgressStats.UserQueriesFinished());
                foreach (var repo in user.Repos)
                {
                    if (!_similarRepos.ContainsKey(repo.HtmlUrl))
                    {
                        _similarRepos[repo.HtmlUrl] = new WrapSimilarRepo(repo);
                    }

                    //increment the number of people who starred this repo
                    _similarRepos[repo.HtmlUrl].SharedStarrers++;
                }
            });

            Receive<PublishUpdate>(update =>
            {
                //check to see if the job is done
                if (_receivedInitialUsers && _githubProgressStats.IsFinished)
                {
                    _githubProgressStats =
                        WrapGithubProgressStats.FromGithubProgressStats(_githubProgressStats.Finish());

                    //all repos minus forks of the current one
                    var sortedSimilarRepos = _similarRepos.Values
                        .Where(x => x.Repo.Name != _currentRepo.Repo).OrderByDescending(x => x.SharedStarrers).ToList();
                    foreach (var subscriber in _subscribers)
                    {
                        subscriber.Tell(sortedSimilarRepos);
                    }
                    BecomeWaiting();
                }

                foreach (var subscriber in _subscribers)
                {
                    subscriber.Tell(_githubProgressStats);
                }
            });

            //completed our initial job - we now know how many users we need to query
            Receive<User[]>(users =>
            {
                _receivedInitialUsers = true;
                _githubProgressStats =
                    WrapGithubProgressStats.FromGithubProgressStats(
                        _githubProgressStats.SetExpectedUserCount(users.Length));

                //queue up all of the jobs
                foreach (var user in users)
                {
                    _githubWorker.Tell(new RetryableQuery(new QueryStarrer(user.Login), 3));
                }
            });

            Receive<GithubValidatorActor.CanAcceptJob>(job =>
                Sender.Tell(new UnableToAcceptJob(job.Repo)));

            Receive<RepoResultsForm.SubscribeToProgressUpdates>(updates =>
            {
                //this is our first subscriber, which means we need to turn publishing on
                if (_subscribers.Count == 0)
                {
                    Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMilliseconds(100),
                        TimeSpan.FromMilliseconds(100),
                        Self, PublishUpdate.Instance, Self, _publishTimer);
                }

                _subscribers.Add(updates.Subscriber);
            });

            //query failed, but can be retried
            Receive<RetryableQuery>(query => query.CanRetry, query => _githubWorker.Tell(query));

            //query failed, can't be retried, and it's a QueryStarrers operation - means the entire job failed
            Receive<RetryableQuery>(query => !query.CanRetry && query.Query is QueryStarrers, query =>
            {
                _receivedInitialUsers = true;
                foreach (var subscriber in _subscribers)
                {
                    subscriber.Tell(new JobFailed(_currentRepo));
                }
                BecomeWaiting();
            });

            //query failed, can't be retried, and it's a QueryStarrers operation - means individual operation failed
            Receive<RetryableQuery>(query => !query.CanRetry && query.Query is QueryStarrer,
                query => _githubProgressStats.IncrementFailures());
        }
    }
}
