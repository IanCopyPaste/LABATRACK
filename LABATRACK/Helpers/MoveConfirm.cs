using System.Globalization;
using LABATRACK.Models;
using LABATRACK.Services;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// The words of the "are you sure?" window (Assets/js/Confirm.js) shown before a job moves one
    /// step forward. The job board and the job page both use it, so they ask the same question.
    /// The next stage comes from StatusService, the only place that knows the job's route.
    /// </summary>
    public static class MoveConfirm
    {
        public static string Title(JobOrder job)
        {
            string next = StatusService.NextStage(job);
            if (next == StatusService.Ready) return "Pass inspection for " + job.ClaimNumber + "?";
            if (next == StatusService.Claimed) return "Hand " + job.ClaimNumber + " over to the customer?";
            return "Move " + job.ClaimNumber + " to " + next + "?";
        }

        public static string Text(JobOrder job)
        {
            string next = StatusService.NextStage(job);
            string load = job.CustomerName + " · " + job.ServiceName + ", "
                        + job.WeightKg.ToString("0.0#", CultureInfo.InvariantCulture) + " kg";

            string what;
            if (next == StatusService.Ready)
                what = "Inspection → Ready for pick-up. Go ahead only if the load passed the final check. "
                     + "If the customer gave an email, they are told it is ready.";
            else if (next == StatusService.Claimed)
                what = "Check that the customer's slip shows this claim number. Claimed is final: the job closes and leaves the board.";
            else if (job.CurrentStatus == StatusService.Queued)
                what = "Queued → " + next + ". Go ahead only if the load is going into " + next.ToLowerInvariant() + " now.";
            else
                what = job.CurrentStatus + " → " + next + ". Go ahead only if " + job.CurrentStatus.ToLowerInvariant() + " is finished.";

            return load + "\n\n" + what + "\nThe move is saved in the job's history with your name and the time.";
        }
    }
}
