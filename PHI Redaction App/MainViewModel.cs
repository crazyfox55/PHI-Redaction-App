using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
        public RelayCommand ProcessCommand { get; }

        public MainViewModel()
        {
            AddFilesCommand = new RelayCommand(AddFiles);
            SelectFolderCommand = new RelayCommand(SelectFolder);
            ProcessCommand = new RelayCommand(ProcessFiles, CanProcessFiles);
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

        private async void ProcessFiles()
        {
            IsProcessing = true;
            Status = string.Empty;

            var processingTasks = SelectedFiles.Select(file =>
                ProcessFileAsync(file)  // Remove async lambda, directly return Task
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        Status += $"Error processing {file}: {task.Exception.InnerException?.Message}\n";
                    }
                }, TaskContinuationOptions.OnlyOnFaulted)
            );

            Task.WhenAll(processingTasks).Wait();

            Status += "Processing complete.\n";
            IsProcessing = false;
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