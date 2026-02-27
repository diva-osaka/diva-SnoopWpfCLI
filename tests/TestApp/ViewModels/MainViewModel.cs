using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private string _inputText = "Initial Text";
    private bool _isChecked;
    private string _selectedCategory = "";
    private double _sliderValue = 50.0;
    private int _clickCount;
    private string _statusMessage = "Ready";

    public MainViewModel()
    {
        Categories = new ObservableCollection<string>
        {
            "Category A",
            "Category B",
            "Category C"
        };

        People = new ObservableCollection<PersonItem>
        {
            new() { Name = "Alice", Age = 30, Role = "Developer" },
            new() { Name = "Bob", Age = 25, Role = "Designer" },
            new() { Name = "Carol", Age = 35, Role = "Manager" }
        };

        TodoItems = new ObservableCollection<TodoItem>
        {
            new() { Title = "Write unit tests", IsCompleted = true },
            new() { Title = "Review pull request", IsCompleted = false },
            new() { Title = "Deploy to staging", IsCompleted = false }
        };
    }

    public string InputText
    {
        get => _inputText;
        set => SetField(ref _inputText, value);
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => SetField(ref _isChecked, value);
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set => SetField(ref _selectedCategory, value);
    }

    public double SliderValue
    {
        get => _sliderValue;
        set => SetField(ref _sliderValue, value);
    }

    public int ClickCount
    {
        get => _clickCount;
        set => SetField(ref _clickCount, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<PersonItem> People { get; }
    public ObservableCollection<TodoItem> TodoItems { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class PersonItem : INotifyPropertyChanged
{
    private string _name = "";
    private int _age;
    private string _role = "";

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public int Age
    {
        get => _age;
        set { _age = value; OnPropertyChanged(); }
    }

    public string Role
    {
        get => _role;
        set { _role = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class TodoItem : INotifyPropertyChanged
{
    private string _title = "";
    private bool _isCompleted;

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set { _isCompleted = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
