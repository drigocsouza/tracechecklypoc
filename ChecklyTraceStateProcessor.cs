using System.Diagnostics;
using OpenTelemetry;

namespace TraceChecklyPoC.ChecklyTraceStateProcessor
{
    public class ChecklyTraceStateProcessor : BaseProcessor<Activity>
    {
        public override void OnStart(Activity activity)
        {
            if (activity == null)
                return;

            // Adiciona "checkly=true" ao trace state, preservando outros valores
            var currentState = activity.TraceStateString ?? "";
            if (!currentState.Contains("checkly=true"))
            {
                currentState = string.IsNullOrEmpty(currentState) ? "checkly=true" : currentState + ",checkly=true";
            }

            // Exemplo: adicione outros campos se existirem em Tag ou Header
            var checkId = activity.GetTagItem("check_id") as string;
            var checkResultId = activity.GetTagItem("check_result_id") as string;
            var checklyTimestamp = activity.GetTagItem("checkly_timestamp") as string;

            if (!string.IsNullOrEmpty(checkId))
                currentState += $",check_id={checkId}";
            if (!string.IsNullOrEmpty(checkResultId))
                currentState += $",check_result_id={checkResultId}";
            if (!string.IsNullOrEmpty(checklyTimestamp))
                currentState += $",checkly_timestamp={checklyTimestamp}";

            activity.TraceStateString = currentState;
            base.OnStart(activity);
        }
    }
}