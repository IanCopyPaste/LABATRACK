using System.Collections.Generic;

namespace LABATRACK.models
{
    public static class JobStatus
    {
        public const string Queued = "Queued";
        public const string IntakeInspection = "Intake inspection";
        public const string Washing = "Washing";
        public const string Drying = "Drying";
        public const string Folding = "Folding";
        public const string Inspection = "Inspection";
        public const string Ready = "Ready for pick-up";
        public const string Claimed = "Claimed";
        public const string Voided = "Voided";

        public const string AlreadyUpdated = "This job was already updated. Refresh.";

        public static readonly string[] BoardStages = { Queued, IntakeInspection, Washing, Drying, Folding, Inspection, Ready };
    }
}
