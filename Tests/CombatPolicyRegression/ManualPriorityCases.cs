using RotationSolver.Basic.Helpers;

internal static class ManualPriorityCases
{
    private const uint Helios = 37030, Malefic = 25871, Rend = 25753;
    private static ManualActionRequest Press(uint id, ulong target = 123) => new(1, id, id, target, 0, 0, 0, 123);

    internal static void Run()
    {
        var cases = new (string Name, Action Test)[]
        {
            ("AST: four automatic Malefic attempts yield to a pending Helios", () =>
            {
                var priority = new ManualActionPriority();
                var heal = Press(Helios);
                priority.Begin(heal, 0, 2.5);
                foreach (var time in new[] { 0.1, 0.6, 1.1, 1.6 })
                {
                    priority.Update(time, false, Malefic, 0);
                    Check(priority.BlocksAutomation(time), "Malefic overtook the heal");
                }
                priority.Queued(heal, 2, 0.5);
                priority.Update(2.1, true, Malefic, 0);
                Check(priority.IsQueued, "native queue ownership lost");
                priority.Issued(heal, 1, 1.5, 2.5);
                priority.Update(2.6, false, Helios, 0);
                Check(priority.OwnsCast(Helios, 2.6), "manual heal is not protected");
                priority.Update(4.1, false, 0, 1);
                Check(!priority.BlocksAutomation(4.1), "rotation did not resume after Helios");
            }),
            ("WAR: Primal Rend owns the next slot and is consumed once", () =>
            {
                var priority = new ManualActionPriority();
                var rend = Press(Rend);
                priority.Begin(rend, 0, 2.5);
                Check(priority.BlocksAutomation(1), "automatic attack won the slot");
                priority.Issued(rend, 15, 0, 2.5);
                Check(priority.Pending == null, "issued Rend was retained for a second use");
                priority.Update(2.6, false, 0, 15);
                Check(!priority.BlocksAutomation(2.6), "priority did not return after Rend");
            }),
            ("Latest press replaces an older queued request", () =>
            {
                var priority = new ManualActionPriority();
                priority.Queued(Press(Malefic), 0, 2.5);
                priority.Begin(Press(Helios), 0.1, 2.4);
                Check(priority.Pending == Press(Helios) && !priority.IsQueued, "old request retained");
            }),
            ("Same action on a different target is a new choice", () =>
            {
                var priority = new ManualActionPriority();
                priority.Begin(Press(Helios, 100), 0, 2.5);
                priority.Begin(Press(Helios, 200), 0.1, 2.4);
                Check(priority.Pending?.TargetId == 200, "new target was lost");
            }),
            ("An older action acknowledgement cannot consume newer input", () =>
            {
                var priority = new ManualActionPriority();
                priority.Issued(Press(Malefic), 12, 1.5, 0);
                priority.Begin(Press(Helios), 0.1, 1.4);
                priority.Update(1.6, false, 0, 12);
                Check(priority.Pending == Press(Helios) && priority.BlocksAutomation(1.6), "new heal discarded");
            }),
            ("Native cancellation discards a queued request instead of replaying it later", () =>
            {
                var priority = new ManualActionPriority();
                priority.Queued(Press(Rend), 0, 0.5);
                priority.Update(0.1, false, 0, 0);
                Check(priority.Pending == null && !priority.BlocksAutomation(0.1), "canceled request survived");
            }),
            ("Spam during an already issued manual cast creates no extra deferred cast", () =>
            {
                var priority = new ManualActionPriority();
                var heal = Press(Helios);
                priority.Issued(heal, 10, 2.5, 0);
                priority.Update(0.1, false, Helios, 9);
                for (var i = 1; i <= 20; i++)
                    Check(!priority.Begin(heal, i / 20d, 2.5 - i / 20d), "duplicate cast latched");
                Check(priority.Pending == null, "same-action spam created another request");
                priority.Update(2.6, false, 0, 10);
                Check(!priority.BlocksAutomation(2.6), "spam prevented resuming");
            }),
            ("The game can still accept an intentional repeat into its native queue", () =>
            {
                var priority = new ManualActionPriority();
                var heal = Press(Helios);
                priority.Issued(heal, 10, 2.5, 0);
                Check(!priority.Begin(heal, 2.1, 0.4), "same in-flight action was duplicated");
                priority.Queued(heal, 2.1, 0.4);
                priority.Update(2.2, true, Helios, 9);
                Check(priority.IsQueued, "native accepted repeat was discarded");
            }),
            ("No fixed delay after the game confirms an instant manual action", () =>
            {
                var priority = new ManualActionPriority();
                priority.Issued(Press(Rend), 5, 0, 0);
                priority.Update(0.04, false, 0, 5);
                Check(!priority.BlocksAutomation(0.04), "confirmation caused an unnecessary pause");
            }),
            ("Matching action ID with an old sequence does not confirm a new request", () =>
            {
                var priority = new ManualActionPriority();
                priority.Issued(Press(Helios), 6, 0, 0);
                priority.Update(0.1, false, 0, 5);
                Check(priority.BlocksAutomation(0.1), "old response cleared new request");
            }),
            ("Native sequence wraparound is handled by equality", () =>
            {
                var priority = new ManualActionPriority();
                priority.Issued(Press(Rend), 0, 0, 0);
                priority.Update(0.1, false, 0, ushort.MaxValue);
                Check(priority.BlocksAutomation(0.1), "wrapped sequence was treated as already handled");
                priority.Update(0.2, false, 0, 0);
                Check(!priority.BlocksAutomation(0.2), "wrapped confirmation was ignored");
            }),
            ("Unsent requests expire instead of executing in a later window", () =>
            {
                var priority = new ManualActionPriority();
                priority.Begin(Press(Rend), 0, 1);
                priority.Update(1.76, false, 0, 0);
                Check(priority.Pending == null && !priority.BlocksAutomation(1.76), "stale request survived");
            }),
            ("Missing confirmations cannot freeze the rotation", () =>
            {
                var priority = new ManualActionPriority();
                priority.Issued(Press(Rend), 10, 0, 0);
                priority.Update(1.01, false, 0, 9);
                Check(!priority.BlocksAutomation(1.01), "lost response froze the rotation");
            }),
            ("An interrupted manual cast releases promptly", () =>
            {
                var priority = new ManualActionPriority();
                priority.Issued(Press(Helios), 10, 8, 0);
                priority.Update(0.1, false, Helios, 9);
                priority.Update(0.2, false, 0, 9);
                priority.Update(0.46, false, 0, 9);
                Check(!priority.BlocksAutomation(0.46), "canceled cast held the old eight-second reservation");
            }),
            ("An acknowledged cast is still owned until casting ends", () =>
            {
                var priority = new ManualActionPriority();
                priority.Issued(Press(Helios), 10, 5, 0);
                priority.Update(0.1, false, Helios, 10);
                Check(priority.OwnsCast(Helios, 0.1), "manual cast lost ownership at early acknowledgement");
                priority.Update(5.1, false, 0, 10);
                Check(!priority.BlocksAutomation(5.1), "finished cast retained priority");
            }),
            ("Invalid input can be discarded without a pause", () =>
            {
                var priority = new ManualActionPriority();
                priority.Begin(Press(Rend), 0, 2.5);
                priority.ClearPending();
                Check(!priority.BlocksAutomation(0), "invalid request froze automation");
            }),
            ("Reset clears unsent, queued and in-flight ownership", () =>
            {
                var priority = new ManualActionPriority();
                priority.Issued(Press(Malefic), 1, 8, 0);
                priority.Queued(Press(Helios), 0.1, 7.9);
                priority.Reset();
                Check(!priority.BlocksAutomation(0.1) && priority.Pending == null
                    && !priority.OwnsCast(Malefic, 0.1), "reset left a manual request alive");
            }),
            ("Macro target and native parameters survive reservation unchanged", () =>
            {
                var priority = new ManualActionPriority();
                var macro = new ManualActionRequest(1, Helios, Helios, 987654, 321, 2, 7, 123);
                priority.Begin(macro, 0, 2.5);
                priority.Queued(macro, 2, 0.5);
                Check(priority.Pending == macro, "manual arguments were rewritten");
            }),
            ("Morphs match both requested and adjusted IDs", () =>
            {
                var request = Press(Rend) with { AdjustedId = 36925 };
                Check(request.Matches(Rend) && request.Matches(36925) && !request.Matches(Malefic), "morph identity failed");
            }),
            ("A frame that skips the queue window still permits the manual action at zero", () =>
            {
                Check(ManualActionPriority.CanWaitForTiming(true, 0, 0, 0), "ready manual action was dropped before dispatch");
            }),
            ("A thirty-second cooldown does not reserve a two-second slot", () =>
            {
                Check(!ManualActionPriority.CanWaitForTiming(true, 0, 30, 2), "long cooldown froze the rotation");
            }),
            ("Native resource and proc failures are not treated as timing restrictions", () =>
            {
                Check(!ManualActionPriority.CanWaitForTiming(true, 568, 0, 2.5), "insufficient MP retained a request");
                Check(!ManualActionPriority.CanWaitForTiming(true, 572, 0, 2.5), "missing Primal Rend proc retained a request");
            }),
            ("A changed adjusted action is not replayed as a different proc", () =>
            {
                Check(!ManualActionPriority.CanWaitForTiming(false, 0, 0, 2.5), "an expired morph was replayed");
            }),
            ("A charge already available may wait for the current animation lock", () =>
            {
                Check(ManualActionPriority.CanWaitForTiming(true, 0, 0, 0.7), "available charge was discarded");
            }),
        };

        foreach (var test in cases)
        {
            test.Test();
            Console.WriteLine($"PASS: {test.Name}");
        }
        Console.WriteLine($"Passed {cases.Length} manual-priority scenarios.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
