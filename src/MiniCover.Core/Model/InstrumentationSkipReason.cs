namespace MiniCover.Core.Model
{
    public enum InstrumentationSkipReason
    {
        AlreadyInstrumented,
        InvalidAssemblyFormat,
        SourceFilesChanged,
        NothingToInstrument
    }
}
