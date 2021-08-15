using System;
using System.Diagnostics;
using ParrelSync;

public class LogicTimer
{
  public const float FramesPerSecond = 60.0f;
  public const float FixedDelta = 1.0f / FramesPerSecond;

  private double _accumulator;
  private long _lastTime;

  private readonly Stopwatch _stopwatch;
  private readonly Action _action;

  public float LerpAlpha => (float)_accumulator / FixedDelta;

  public LogicTimer(Action action)
  {
    _stopwatch = new Stopwatch();
    _action = action;
  }

  public void Start()
  {
    _lastTime = 0;
    _accumulator = 0.0;
    _stopwatch.Restart();

    // if (!ClonesManager.IsClone())
    // {
    //   UnityEngine.Application.targetFrameRate = (int)FramesPerSecond;
    //   UnityEngine.QualitySettings.vSyncCount = 0;
    // }
  }

  public void Stop()
  {
    _stopwatch.Stop();
  }

  public void Update()
  {
    long elapsedTicks = _stopwatch.ElapsedTicks;
    _accumulator += (double)(elapsedTicks - _lastTime) / Stopwatch.Frequency;
    _lastTime = elapsedTicks;

    while (_accumulator >= FixedDelta)
    {
      _action();
      _accumulator -= FixedDelta;
    }
  }
}