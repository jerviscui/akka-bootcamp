using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Akka.Actor;
using Akka.Util.Internal;
using ChartApp.Actors;

namespace ChartApp
{
    public partial class Main : Form
    {
        private readonly AtomicCounter _seriesCounter = new AtomicCounter(1);

        private IActorRef _chartActor;

        public Main()
        {
            InitializeComponent();
        }

        private IActorRef _coordinatorActor;

        private Dictionary<CounterType, IActorRef> _toggleActors = new Dictionary<CounterType, IActorRef>();

        #region Initialization

        private void Main_Load(object sender, EventArgs e)
        {
            var test = Program.ChartActors.ActorOf(Props.Create(() => new Test()));
            test.Tell(2);

            _chartActor = Program.ChartActors.ActorOf(Props.Create(() => new ChartingActor(sysChart)), "charting");
            _chartActor.Tell(new ChartingActor.InitializeChart());

            _coordinatorActor = Program.ChartActors.ActorOf(Props.Create(() =>
                new PerformanceCounterDoordinatorActor(_chartActor)), "counters");

            _toggleActors.Add(CounterType.Cpu, Program.ChartActors.ActorOf(Props.Create(() =>
                    new ButtonToggleActor(CounterType.Cpu, _coordinatorActor, btnCpu, false))
                .WithDispatcher("akka.actor.synchronized-dispatcher")));

            _toggleActors.Add(CounterType.Memory, Program.ChartActors.ActorOf(Props.Create(() =>
                    new ButtonToggleActor(CounterType.Memory, _coordinatorActor, btnMem, false))
                .WithDispatcher("akka.actor.synchronized-dispatcher")));

            _toggleActors.Add(CounterType.Disk, Program.ChartActors.ActorOf(Props.Create(() =>
                    new ButtonToggleActor(CounterType.Disk, _coordinatorActor, btnDisk, false))
                .WithDispatcher("akka.actor.synchronized-dispatcher")));

            _toggleActors[CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            //shut down the charting actor
            _chartActor.Tell(PoisonPill.Instance);

            //shut down the ActorSystem
            Program.ChartActors.Terminate();
        }

        #endregion

        private void btnCpu_Click(object sender, EventArgs e)
        {
            _toggleActors[CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
        }

        private void btnMem_Click(object sender, EventArgs e)
        {
            _toggleActors[CounterType.Memory].Tell(new ButtonToggleActor.Toggle());
        }

        private void btnDisk_Click(object sender, EventArgs e)
        {
            _toggleActors[CounterType.Disk].Tell(new ButtonToggleActor.Toggle());
        }
    }
}
