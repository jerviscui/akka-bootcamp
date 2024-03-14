using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Octokit;

namespace GithubActors.Actors
{
    /// <summary>
    /// Individual actor responsible for querying the Github API
    /// </summary>
    public class GithubWorkerActor : ReceiveActor
    {
        #region Message classes

        public class StarredReposForUser
        {
            public StarredReposForUser(string login, IEnumerable<Repository> repos)
            {
                Repos = repos;
                Login = login;
            }

            public string Login { get; private set; }

            public IEnumerable<Repository> Repos { get; private set; }
        }

        #endregion

        private IGitHubClient _gitHubClient;
        private readonly Func<IGitHubClient> _gitHubClientFactory;

        public GithubWorkerActor(Func<IGitHubClient> gitHubClientFactory)
        {
            _gitHubClientFactory = gitHubClientFactory;
            InitialReceives();
        }

        protected override void PreStart()
        {
            _gitHubClient = _gitHubClientFactory();
        }

        private void InitialReceives()
        {
            //query an individual starrer
            ReceiveAsync<RetryableQuery>(query => query.Query is GithubCoordinatorActor.QueryStarrer, async query =>
            {
                var starrer = ((GithubCoordinatorActor.QueryStarrer)query.Query).Login;
                try
                {
                    var getStarrer = await _gitHubClient.Activity.Starring.GetAllForUser(starrer);
                    Sender.Tell(new StarredReposForUser(starrer, getStarrer));
                }
                catch (Exception)
                {
                    Sender.Tell(query.NextTry());
                }
            });

            //1
            //query all starrers for a repository
            ReceiveAsync<RetryableQuery>(query => query.Query is GithubCoordinatorActor.QueryStarrers, async query =>
            {
                // ReSharper disable once PossibleNullReferenceException (we know from the previous IS statement that this is not null)
                var starrers = (query.Query as GithubCoordinatorActor.QueryStarrers).Key;
                try
                {
                    var getStars =
                        await _gitHubClient.Activity.Starring.GetAllStargazers(starrers.Owner, starrers.Repo);
                    Sender.Tell(getStars.ToArray());
                }
                catch (Exception)
                {
                    //operation failed - let the parent know
                    Sender.Tell(query.NextTry());
                }
            });
        }
    }
}
