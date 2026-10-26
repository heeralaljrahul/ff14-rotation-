using RotationSolver.Basic.Helpers;

var cases = new (string Name, Action Test)[]
{
    ("No automatic preparation before a countdown", () =>
    {
        var gate = new AutomaticPullGate();
        Expect(false, gate.CanAct(0, false, false, 1, 0));
        Expect(false, gate.CanAct(0, false, false, 1, 20));
    }),
    ("Countdown wins over a lingering combat flag", () =>
    {
        var gate = new AutomaticPullGate();
        Expect(true, gate.CanAct(0, true, false, 1, 0));
        Expect(false, gate.CanAct(10, true, false, 1, 1));
        Expect(false, gate.CanAct(0.01f, true, false, 1, 10.99));
    }),
    ("Zero releases an opener without requiring combat first", () =>
    {
        var gate = new AutomaticPullGate();
        Expect(false, gate.CanAct(1, false, false, 1, 0));
        Expect(true, gate.CanAct(0, false, false, 1, 1));
        // Dancer's steps and Pictomancer's motifs do not immediately enter combat.
        Expect(true, gate.CanAct(0, false, false, 1, 5));
    }),
    ("A missed final timer frame still releases at zero", () =>
    {
        var gate = new AutomaticPullGate();
        Expect(false, gate.CanAct(1, false, false, 1, 0));
        Expect(true, gate.CanAct(0, false, false, 1, 1.4));
    }),
    ("Early cancellation does not pull later", () =>
    {
        var gate = new AutomaticPullGate();
        Expect(false, gate.CanAct(10, false, false, 1, 0));
        Expect(false, gate.CanAct(0, false, false, 1, 1));
        Expect(false, gate.CanAct(0, false, false, 1, 20));
    }),
    ("Final-frame disappearance never releases before estimated zero", () =>
    {
        var gate = new AutomaticPullGate();
        Expect(false, gate.CanAct(1, false, false, 1, 0));
        Expect(false, gate.CanAct(0, false, false, 1, 0.9));
        Expect(true, gate.CanAct(0, false, false, 1, 1));
    }),
    ("A second countdown revokes the first pull's release", () =>
    {
        var gate = new AutomaticPullGate();
        gate.CanAct(1, false, false, 1, 0);
        Expect(true, gate.CanAct(0, false, false, 1, 1));
        Expect(false, gate.CanAct(10, false, false, 1, 2));
    }),
    ("Execution recheck catches a timer started after selection", () =>
    {
        var gate = new AutomaticPullGate();
        Expect(true, gate.CanAct(0, true, false, 1, 0));
        Expect(false, gate.CanAct(10, true, false, 1, 0.01));
    }),
    ("Death revokes an unused release", () =>
    {
        var gate = new AutomaticPullGate();
        gate.CanAct(1, false, false, 1, 0);
        gate.CanAct(0, false, false, 1, 1);
        Expect(false, gate.CanAct(0, false, true, 1, 2));
        Expect(false, gate.CanAct(0, false, false, 1, 3));
    }),
    ("Combat ending revokes a previous release", () =>
    {
        var gate = new AutomaticPullGate();
        gate.CanAct(1, false, false, 1, 0);
        gate.CanAct(0, false, false, 1, 1);
        gate.CanAct(0, true, false, 1, 2);
        Expect(false, gate.CanAct(0, false, false, 1, 3));
    }),
    ("Combat ending after a wipe preserves a running timer", () =>
    {
        var gate = new AutomaticPullGate();
        gate.CanAct(0, true, false, 1, 0);
        gate.CanAct(1, true, false, 1, 1);
        Expect(true, gate.CanAct(0, false, false, 1, 2));
    }),
    ("Territory change revokes the old release", () =>
    {
        var gate = new AutomaticPullGate();
        gate.CanAct(1, false, false, 1, 0);
        gate.CanAct(0, false, false, 1, 1);
        Expect(false, gate.CanAct(0, false, false, 2, 2));
    }),
    ("Without a countdown actual combat can start automation", () =>
    {
        var gate = new AutomaticPullGate();
        Expect(false, gate.CanAct(0, false, false, 1, 0));
        Expect(true, gate.CanAct(0, true, false, 1, 1));
    }),
    ("Outside high-end, out-of-combat actions stay allowed without a countdown", () =>
    {
        var gate = new AutomaticPullGate();
        Expect(true, gate.CanAct(0, false, false, 1, 0, requirePull: false));
    }),
    ("Outside high-end, a running countdown still blocks", () =>
    {
        var gate = new AutomaticPullGate();
        Expect(false, gate.CanAct(10, false, false, 1, 0, requirePull: false));
        Expect(true, gate.CanAct(0, false, false, 1, 10, requirePull: false));
        Expect(true, gate.Released);
    }),
    ("Outside high-end, a cancelled countdown is not a completed pull", () =>
    {
        var gate = new AutomaticPullGate();
        gate.CanAct(10, false, false, 1, 0, requirePull: false);
        Expect(true, gate.CanAct(0, false, false, 1, 1, requirePull: false));
        Expect(false, gate.Released);
    }),
};

foreach (var test in cases)
{
    test.Test();
    Console.WriteLine($"PASS: {test.Name}");
}
Console.WriteLine($"Passed {cases.Length} countdown lifecycle scenarios.");
ManualPriorityCases.Run();

static void Expect(bool expected, bool actual)
{
    if (expected != actual)
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}
