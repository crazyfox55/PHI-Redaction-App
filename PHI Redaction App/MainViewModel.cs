using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;

namespace PHI_Redaction_App
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        private string _outputFolderPath = "";
        private string _status = "";
        private bool _isProcessing;

        public ObservableCollection<string> SelectedFiles { get; } = [];

        public string OutputFolderPath
        {
            get => _outputFolderPath;
            set
            {
                _outputFolderPath = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
                ProcessCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand AddFilesCommand { get; }
        public RelayCommand SelectFolderCommand { get; }
        public AsyncCommand ProcessCommand { get; }

        public MainViewModel()
        {
            AddFilesCommand = new RelayCommand(AddFiles);
            SelectFolderCommand = new RelayCommand(SelectFolder);
            ProcessCommand = new AsyncCommand(ProcessFiles, CanProcessFiles);
        }

        private void AddFiles()
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Text files (*.txt)|*.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    if (!SelectedFiles.Contains(file))
                    {
                        SelectedFiles.Add(file);
                        ProcessCommand.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        private void SelectFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Output Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                OutputFolderPath = dialog.FolderName;

                ProcessCommand.RaiseCanExecuteChanged();
            }
        }

        private bool CanProcessFiles() =>
            !IsProcessing && SelectedFiles.Count > 0 && !string.IsNullOrEmpty(OutputFolderPath);

        private async Task ProcessFiles()
        {
            IsProcessing = true;
            Status = string.Empty;

            try
            {
                var processingTasks = SelectedFiles.Select(async file =>
                {
                    try
                    {
                        await ProcessFileAsync(file);
                    }
                    catch (Exception ex)
                    {
                        Status += $"Error processing {file}: {ex.Message}\n";
                    }
                });

                await Task.WhenAll(processingTasks);

                Status += "Processing complete.\n";
            }
            catch (Exception ex)
            {
                Status += $"Unexpected error: {ex.Message}\n";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task ProcessFileAsync(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            string outputFilePath = Path.Combine(OutputFolderPath, fileName + "_sanitized" + extension);

            Status += $"Processing {fileName}{extension}...\n";

            try
            {
                // Open input and output files with stream readers/writers
                using (var reader = new StreamReader(filePath))
                using (var writer = new StreamWriter(outputFilePath))
                {
                    while (await reader.ReadLineAsync() is string line)
                    {
                        // Process each line individually
                        string sanitizedLine = PHIData().Replace(line, "[REDACTED]");

                        // Write the sanitized line to the output file
                        await writer.WriteLineAsync(sanitizedLine);
                    }
                }

                Status += $"Saved: {outputFilePath}\n";
            }
            catch (Exception ex)
            {
                Status += $"Error processing {fileName}{extension}: {ex.Message}\n";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [GeneratedRegex(@"(?<=Patient Name:\s)(.+)|(?<=Date of Birth:\s)(\d{2}/\d{2}/\d{4})|(?<=Social Security Number:\s)(\d{3}-\d{2}-\d{4})|(?<=Address:\s)(.+)|(?<=Phone Number:\s)(\(\d{3}\) \d{3}-\d{4})|(?<=Email:\s)(\S+@\S+\.\S+)|(?<=Medical Record Number:\s)(MRN-\d+)")]
        private static partial Regex PHIData();
    }

    public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => execute();
    }
}

public class AsyncCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool> _canExecute;
    private bool _isExecuting;

    public AsyncCommand(Func<Task> execute, Func<bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke() ?? true);
    }

    public async void Execute(object parameter)
    {
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public event EventHandler CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}