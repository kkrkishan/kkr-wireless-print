using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.App.ViewModels;

public class PrinterItemViewModel : INotifyPropertyChanged
{
    private PrinterInfo _model;
    private bool _isPrintingTestPage;

    public PrinterItemViewModel(PrinterInfo model, Action<PrinterItemViewModel> onPrintTestPage)
    {
        _model = model;
        PrintTestPageCommand = new RelayCommand(() => onPrintTestPage(this), () => !_isPrintingTestPage && _model.IsOnline);
    }

    public string Name => _model.Name;
    public string DriverName => _model.DriverName;
    public string PortName => _model.PortName;
    public bool IsDefault => _model.IsDefault;
    public bool IsOnline => _model.IsOnline;
    public bool IsBusy => _model.IsBusy;
    public bool IsInError => _model.IsInError;
    public bool IsOutOfPaper => _model.IsOutOfPaper;
    public string StatusDescription => _model.StatusDescription;
    public bool SupportsColor => _model.Capabilities.SupportsColor;
    public bool CanDuplex => _model.Capabilities.CanDuplex;
    public int PaperSizesCount => _model.Capabilities.SupportedPaperSizes.Count;
    public int QueuedJobCount => _model.QueuedJobCount;

    public bool IsPrintingTestPage
    {
        get => _isPrintingTestPage;
        set
        {
            _isPrintingTestPage = value;
            OnPropertyChanged();
            ((RelayCommand)PrintTestPageCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand PrintTestPageCommand { get; }

    public void UpdateModel(PrinterInfo newModel)
    {
        _model = newModel;
        OnPropertyChanged(string.Empty); // Notify all properties
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class JobItemViewModel : INotifyPropertyChanged
{
    private PrintJobRecord _model;

    public JobItemViewModel(PrintJobRecord model, Action<JobItemViewModel> onCancel)
    {
        _model = model;
        CancelCommand = new RelayCommand(() => onCancel(this), () => CanCancel);
    }

    public Guid JobId => _model.JobId;
    public string DocumentName => _model.DocumentName;
    public string PrinterName => _model.PrinterName;
    public string ClientDeviceName => string.IsNullOrWhiteSpace(_model.ClientDeviceName) ? "Local Host" : _model.ClientDeviceName;
    public string ClientIp => _model.ClientIp;
    public int PageCount => _model.PageCount;
    public long FileSizeBytes => _model.FileSizeBytes;
    public string FileSizeFormatted => FormatFileSize(_model.FileSizeBytes);
    public JobStatus Status => _model.Status;
    public string StatusDescription => _model.Status.ToString();
    public string? StatusMessage => _model.StatusMessage;
    public string CreatedAtLocal => _model.CreatedAtUtc.ToLocalTime().ToString("HH:mm:ss");
    public string? ErrorMessage => _model.ErrorMessage;
    public bool CanCancel => _model.Status is JobStatus.Accepted or JobStatus.Queued or JobStatus.Processing;

    public ICommand CancelCommand { get; }

    public void UpdateModel(PrintJobRecord newModel)
    {
        _model = newModel;
        OnPropertyChanged(string.Empty);
        ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
