namespace MiniCover.CommandLine.Options
{
    public class FailOnSkippedAssembliesOption : INoValueOption
    {
        public bool Value { get; private set; }
        public string Name => "--fail-on-skipped-assemblies";
        public string Description => "Fail this command (non-zero exit code) if some assemblies could not be instrumented for an unexpected reason, e.g. their source files changed while instrumenting [default: false]";

        public void ReceiveValue(bool value)
        {
            Value = value;
        }
    }
}
