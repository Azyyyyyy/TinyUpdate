namespace TinyUpdate.Desktop;

internal class MultiProgress : IProgress<double>
{
    private IProgress<double> _progress;
    private int _doneTaskCount;

    public MultiProgress(IProgress<double> progress, int taskCount)
    {
        _progress = progress;
        TaskCount = taskCount;
    }

    public int TaskCount { get; }


    public void Bump() => _doneTaskCount++;

    public void Report(double value)
    {
        //TODO: Add
    }
}