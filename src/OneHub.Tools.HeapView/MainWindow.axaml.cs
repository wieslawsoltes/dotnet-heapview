using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using OneHub.Diagnostics.HeapView;
using System.Linq;
using System;

namespace OneHub.Tools.HeapView;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }
    
    public void Open(string fileName)
    {
        try
        {
            var heapDump = new GCHeapDump(fileName);
            var heapSnapshot = new HeapSnapshot(heapDump);
            heapView.Snapshot = heapSnapshot;
        }
        catch (Exception ex)
        {
            // TODO: Show error
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files) || e.Data.GetFiles()?.FirstOrDefault() is not IStorageFile)
            e.DragEffects = DragDropEffects.None;
        else
            e.DragEffects = DragDropEffects.Move;
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles()?.OfType<IStorageFile>().ToArray();
            if (files?.FirstOrDefault()?.TryGetLocalPath() is string path)
            {
                Open(path);
            }
        }
    }

    public async void OnOpenClicked(object? sender, EventArgs args)
    {
        var options = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("GC dump") { Patterns = new[] { "*.gcdump" } } }
        };
        var result = await StorageProvider.OpenFilePickerAsync(options);
        if (result != null && result.Count == 1 && result[0].TryGetLocalPath() is string path)
        {
            Open(path);
        }
    }
}