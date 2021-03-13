using System;
using System.Windows;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Elmish.WPF.Samples.SingleCounter
{
  public static class Program
  {
    public record Model
    {
      public int Count { get; init; }
      public int StepSize { get; init; }
    }

    public static Model Init => new() {Count = 0, StepSize = 1};

    public record Msg
    {
      public record Increment() : Msg;
      public record Decrement() : Msg;
      public record SetStepSize(int StepSize) : Msg;
      public record Reset() : Msg;
    }

    public static bool CanReset(Model m) => !Init.Equals(m);

    public static Model Update(Msg msg, Model m)
    {
      return msg switch
      {
        Msg.Increment => m with {Count = m.Count + m.StepSize},
        Msg.Decrement => m with {Count = m.Count - m.StepSize},
        Msg.SetStepSize setStepSize => m with {StepSize = setStepSize.StepSize},
        Msg.Reset => Init,
        _ => throw new ArgumentOutOfRangeException(nameof(msg), msg, "Unhandled message")
      };
    }

    public static FSharpList<Binding<Model, Msg>> Bindings() =>
      ListModule.OfArray(
        new Binding<Model, Msg>[]
        {
          Binding.oneWay<Model, int, Msg>(FuncConvert.FromFunc((Model m) => m.Count)).Invoke("CounterValue"),
          Binding.cmd(FuncConvert.FromFunc<Model, Msg>(m => new Msg.Increment())).Invoke("Increment"),
          Binding.cmd(FuncConvert.FromFunc<Model, Msg>(m => new Msg.Decrement())).Invoke("Decrement"),
          Binding.twoWay<Model, float, Msg>(
            FuncConvert.FromFunc<Model, float>(m => (float) m.StepSize),
            FuncConvert.FromFunc<float, Model, Msg>((v, _) => new Msg.SetStepSize((int) v))
            // TODO: set is never called, so message not sent
          ).Invoke("StepSize"),
          Binding.cmdIf(
            FuncConvert.FromFunc<Model, Msg>(m => new Msg.Reset()),
            FuncConvert.FromFunc<Model, bool>(CanReset)
          ).Invoke("Reset"),
        }
      );

    public static readonly object DesignVm = ViewModel.designInstance(Init, Bindings());

    public static void Main(FrameworkElement window)
    {
      var logger =
        new LoggerConfiguration()
          .MinimumLevel.Override("Elmish.WPF.Update", LogEventLevel.Verbose)
          .MinimumLevel.Override("Elmish.WPF.Bindings", LogEventLevel.Verbose)
          .MinimumLevel.Override("Elmish.WPF.Performance", LogEventLevel.Verbose)
          .WriteTo.Console()
          .CreateLogger();

      WpfProgram.startElmishLoop(window,
        WpfProgram.withLogger(new SerilogLoggerFactory(logger),
          WpfProgram.mkSimple(FuncConvert.FromFunc(() => Init),
            FuncConvert.FromFunc<Msg, Model, Model>(Update),
            FuncConvert.FromFunc(Bindings))));
    }
  }
}
