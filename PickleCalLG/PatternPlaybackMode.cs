namespace PickleCalLG
{
    /// <summary>
    /// How calibration patterns are delivered to the display.
    /// </summary>
    public enum PatternPlaybackMode
    {
        Manual = 0,
        PGenerator = 1,
        LgTv = 2,
        /// <summary>Remote PGenerator (Android PickleGen app over network — Pro mode binary protocol).</summary>
        AndroidPGen = 3,
        /// <summary>PickleGen Easy mode (JSON protocol — full remote control from PickleCal).</summary>
        PickleGenEasy = 4
    }
}
