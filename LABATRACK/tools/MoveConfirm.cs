using System.Globalization;
using LABATRACK.models;

namespace LABATRACK.tools
{
    public static class MoveConfirm
    {
        public static string Title(JobOrder job)
        {
            string next = job.NextStage;
            if (next == JobStatus.Ready) return "Pass inspection for " + job.ClaimNumber + "?";
            if (next == JobStatus.Claimed) return "Hand " + job.ClaimNumber + " over to the customer?";
            return "Move " + job.ClaimNumber + " to " + next + "?";
        }

        public static string Text(JobOrder job)
        {
            string next = job.NextStage;
            string load = job.CustomerName + " · " + job.ServiceName + ", "
                        + job.WeightKg.ToString("0.0#", CultureInfo.InvariantCulture) + " kg";

            string what;
            if (next == JobStatus.Ready)
                what = "Inspection → Ready for pick-up. Go ahead only if the load passed the final check. "
                     + "If the customer gave an email, they are told it is ready.";
            else if (next == JobStatus.Claimed)
                what = "Check that the customer's slip shows this claim number. Claimed is final: the job closes and leaves the board.";
            else if (job.CurrentStatus == JobStatus.Queued)
                what = "Queued → " + next + ". Go ahead only if the load is going into " + next.ToLowerInvariant() + " now.";
            else
                what = job.CurrentStatus + " → " + next + ". Go ahead only if " + job.CurrentStatus.ToLowerInvariant() + " is finished.";

            return load + "\n\n" + what + "\nThe move is saved in the job's history with your name and the time.";
        }
    }
}
