using System.Windows.Forms;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ButtonToggleActor : UntypedActor
    {
        #region Message types

        public class Toggle
        {
        }

        #endregion

        private readonly CounterType _counterType;

        private bool _isToggleOn;

        private readonly Button _button;

        private readonly IActorRef _coordinatorActor;

        /// <inheritdoc />
        public ButtonToggleActor(CounterType counterType, IActorRef coordinatorActor, Button button,
            bool isToggleOn = false)
        {
            _counterType = counterType;
            _coordinatorActor = coordinatorActor;
            _button = button;
            _isToggleOn = isToggleOn;
        }

        /// <inheritdoc />
        protected override void OnReceive(object message)
        {
            if (message is Toggle)
            {
                if (!_isToggleOn)
                {
                    _coordinatorActor.Tell(new PerformanceCounterDoordinatorActor.Watch(_counterType));
                    FlipToggle();
                }
                else
                {
                    _coordinatorActor.Tell(new PerformanceCounterDoordinatorActor.UnWatch(_counterType));
                    FlipToggle();
                }
            }
            else
            {
                Unhandled(message);
            }
        }

        private void FlipToggle()
        {
            _isToggleOn = !_isToggleOn;

            _button.Text = $"{_counterType.ToString()} ({(_isToggleOn ? "ON" : "OFF")})";
        }
    }
}
