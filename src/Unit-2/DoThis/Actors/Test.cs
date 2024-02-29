using Akka.Actor;

namespace ChartApp.Actors
{
    public class Test : ReceiveActor
    {
        public Test()
        {
            //Receive<int>(i => { }, i => i > 1);

            Receive<int>(i => i > 1, i => { });
            Receive<int>(i => i > 0, i => { });
            Receive<int>(i => i <= 1 && i > 0, i => { });
        }
    }
}
