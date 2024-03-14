using System;
using System.Linq;
using Akka.Actor;
using Octokit;

namespace GithubActors.Actors
{
    /// <summary>
    /// Actor has one job - ensure that a public repo exists at the specified address
    /// </summary>
    public class GithubValidatorActor : ReceiveActor
    {
        #region Messages

        public const string Name = "validator";

        public const string Path = $"akka://GithubActors/user/{Name}";

        public class CanAcceptJob
        {
            public CanAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class InvalidRepo
        {
            public InvalidRepo(string repoUri, string reason)
            {
                Reason = reason;
                RepoUri = repoUri;
            }

            public string RepoUri { get; private set; }

            public string Reason { get; private set; }
        }

        /// <summary>
        /// System is unable to process additional repos at this time
        /// </summary>
        public class SystemBusy {  }

        /// <summary>
        /// This is a valid repository
        /// </summary>
        public class RepoIsValid
        {
            /*
             * Using singleton pattern here since it's a stateless message.
             * 
             * Considered to be a good practice to eliminate unnecessary garbage collection,
             * and it's used internally inside Akka.NET for similar scenarios.
             */
            private RepoIsValid() { }

            public static RepoIsValid Instance { get; } = new();
        }

        #endregion

        private readonly IGitHubClient _gitHubClient;

        public GithubValidatorActor(IGitHubClient gitHubClient)
        {
            _gitHubClient = gitHubClient;
            ReadyToValidate();
        }

        private void ReadyToValidate()
        {
            //Outright invalid URLs
            Receive<MainFormActor.ValidateRepo>(
                repo => string.IsNullOrEmpty(repo.RepoUri) ||
                    !Uri.IsWellFormedUriString(repo.RepoUri, UriKind.Absolute),
                repo => Sender.Tell(new InvalidRepo(repo.RepoUri, "Not a valid absolute URI")));

            //Repos that at least have a valid absolute URL
            Receive<MainFormActor.ValidateRepo>(repo =>
            {
                var userOwner = SplitIntoOwnerAndRepo(repo.RepoUri);
                //close over the sender in an instance variable
                var sender = Sender;
                _gitHubClient.Repository.Get(userOwner.Item1, userOwner.Item2)
                    .ContinueWith<object>(t =>
                    {
                        //Rule #1 of async in Akka.NET - turn exceptions into messages your actor understands
                        if (t.IsCanceled)
                        {
                            return new InvalidRepo(repo.RepoUri, "Repo lookup timed out");
                        }
                        if (t.IsFaulted)
                        {
                            return new InvalidRepo(repo.RepoUri,
                                t.Exception != null ? t.Exception.GetBaseException().Message : "Unknown Octokit error");
                        }

                        return t.Result;
                    }).PipeTo(Self, sender);
            });

            // something went wrong while querying github, sent to ourselves via PipeTo
            // however - Sender gets preserved on the call, so it's safe to use Forward here.
            Receive<InvalidRepo>(repo => Sender.Forward(repo));

            // Octokit was able to retrieve this repository
            Receive<Repository>(repository =>
            {
                //ask the GithubCommander if we can accept this job
                Context.ActorSelection(GithubCommanderActor.Path).Tell(
                    new CanAcceptJob(new RepoKey(repository.Owner.Login, repository.Name)));
            });

            /* REPO is valid, but can we process it at this time? */

            //yes
            Receive<GithubCoordinatorActor.UnableToAcceptJob>(job =>
                Context.ActorSelection(MainFormActor.Path).Tell(job));

            //no
            Receive<GithubCoordinatorActor.AbleToAcceptJob>(job =>
                Context.ActorSelection(MainFormActor.Path).Tell(job));
        }

        public static Tuple<string, string> SplitIntoOwnerAndRepo(string repoUri)
        {
            var split = new Uri(repoUri, UriKind.Absolute).PathAndQuery.TrimEnd('/').Split('/').Reverse().ToList(); //uri path without trailing slash
            return Tuple.Create(split[1], split[0]); //User, Repo
        }
    }
}
