using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

// 메인 스레드의 물리 마커를 별도로 기록한다. 중첩될 수 있으므로 합산하지 않는다.
internal sealed class ComparisonPhysicsMetrics : IDisposable
{
    internal const string CsvHeader = ",fixedPhysicsMainMeanMs,simulateMainMeanMs,processingMainMeanMs,simulateCallsPerFrame,fixedStepsPerFrame";
    private readonly string[] names = { "FixedUpdate.PhysicsFixedUpdate", "Physics.Simulate", "Physics.Processing" };
    private readonly ProfilerRecorder[] recorders = new ProfilerRecorder[3];
    private readonly double[] sumNs = new double[3];
    private readonly List<ProfilerRecorderHandle> handles = new();
    private int frames;
    private long steps;
    private double simulateCalls;

    internal void TryStartMissing()
    {
        handles.Clear();
        ProfilerRecorderHandle.GetAvailable(handles);
        for (int i = 0; i < recorders.Length; i++)
        {
            if (recorders[i].Valid) continue;
            for (int j = 0; j < handles.Count; j++)
            {
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handles[j]);
                if (description.Name != names[i]) continue;
                recorders[i] = ProfilerRecorder.StartNew(description.Category, names[i], 1,
                    ProfilerRecorderOptions.Default | ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                break;
            }
        }
    }

    internal void Reset()
    {
        Array.Clear(sumNs, 0, sumNs.Length);
        frames = 0;
        steps = 0;
        simulateCalls = 0;
    }

    internal void Sample(int completedFrameFixedSteps)
    {
        frames++;
        steps += completedFrameFixedSteps;
        // 물리 스텝 없는 프레임에서 이전 스텝의 LastValue를 다시 더하지 않는다.
        if (completedFrameFixedSteps == 0) return;
        for (int i = 0; i < recorders.Length; i++)
        {
            if (!recorders[i].Valid || recorders[i].Count == 0) continue;
            ProfilerRecorderSample sample = recorders[i].GetSample(recorders[i].Count - 1);
            sumNs[i] += sample.Value;
            if (i == 1) simulateCalls += sample.Count;
        }
    }

    internal string CsvValues()
    {
        StringBuilder text = new();
        for (int i = 0; i < recorders.Length; i++)
        {
            text.Append(',');
            text.Append(recorders[i].Valid && frames > 0
                ? (sumNs[i] * 0.000001 / frames).ToString("F3", CultureInfo.InvariantCulture) : "NA");
        }
        text.Append(',').Append(recorders[1].Valid && frames > 0
            ? (simulateCalls / frames).ToString("F3", CultureInfo.InvariantCulture) : "NA");
        text.Append(',').Append(frames > 0 ? ((double)steps / frames).ToString("F3", CultureInfo.InvariantCulture) : "NA");
        return text.ToString();
    }

    public void Dispose()
    {
        for (int i = 0; i < recorders.Length; i++) recorders[i].Dispose();
    }
}
