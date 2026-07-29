namespace MiniCover.Core.Model
{
    public class SkippedAssembly
    {
        public string AssemblyFile { get; set; }
        public InstrumentationSkipReason Reason { get; set; }
    }
}
