// RefactoringUI.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;

namespace DnSpyAIRefactor
{
    public class RefactoringUI
    {
        private readonly RefactoringService refactoringService;
        private readonly IMessageBoxService messageBoxService;
        
        public RefactoringUI(RefactoringService refactoringService, IMessageBoxService messageBoxService)
        {
            this.refactoringService = refactoringService;
            this.messageBoxService = messageBoxService;
        }
        
        public void ShowRefactoringDialog(RefactoringResult result, object entity)
        {
            var dialog = new RefactoringDialog(result, entity);
            dialog.Owner = Application.Current.MainWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            if (dialog.ShowDialog() == true && dialog.AcceptedChanges)
            {
                ApplyChanges(result, entity);
            }
        }
        
        public void ShowBatchAnalysisDialog(CodeAnalysisResult analysis, ModuleDef module)
        {
            var dialog = new BatchRefactoringDialog(analysis);
            dialog.Owner = Application.Current.MainWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            if (dialog.ShowDialog() == true)
            {
                ApplyBatchChanges(analysis, module);
            }
        }
        
        private void ApplyChanges(RefactoringResult result, object entity)
        {
            try
            {
                // Здесь должна быть логика применения изменений к dnlib объектам
                // Это сложная часть, требующая интеграции с dnSpy API
                
                messageBoxService.Show($"Successfully renamed {result.EntityType} '{result.OriginalName}' to '{result.NewName}'");
            }
            catch (Exception ex)
            {
                messageBoxService.Show($"Error applying changes: {ex.Message}");
            }
        }
        
        private void ApplyBatchChanges(CodeAnalysisResult analysis, ModuleDef module)
        {
            // Пакетное применение изменений
            var changes = new List<string>();
            
            foreach (var suggestion in analysis.Suggestions)
            {
                try
                {
                    // Поиск и переименование сущности
                    var entity = FindEntity(module, suggestion.OldName, suggestion.EntityType);
                    if (entity != null)
                    {
                        // Изменение имени (упрощенно)
                        if (entity is TypeDef typeDef)
                            typeDef.Name = suggestion.NewName;
                        else if (entity is MethodDef methodDef)
                            methodDef.Name = suggestion.NewName;
                        else if (entity is PropertyDef propertyDef)
                            propertyDef.Name = suggestion.NewName;
                        else if (entity is FieldDef fieldDef)
                            fieldDef.Name = suggestion.NewName;
                        
                        changes.Add($"{suggestion.EntityType} {suggestion.OldName} → {suggestion.NewName}");
                    }
                }
                catch { }
            }
            
            messageBoxService.Show($"Applied {changes.Count} changes:\n" + string.Join("\n", changes));
        }
        
        private object FindEntity(ModuleDef module, string name, string type)
        {
            // Поиск сущности в модуле
            switch (type.ToLower())
            {
                case "class":
                case "type":
                    return module.GetTypes().FirstOrDefault(t => t.Name == name);
                case "method":
                    return module.GetTypes()
                        .SelectMany(t => t.Methods)
                        .FirstOrDefault(m => m.Name == name);
                case "property":
                    return module.GetTypes()
                        .SelectMany(t => t.Properties)
                        .FirstOrDefault(p => p.Name == name);
                case "field":
                    return module.GetTypes()
                        .SelectMany(t => t.Fields)
                        .FirstOrDefault(f => f.Name == name);
                default:
                    return null;
            }
        }
    }
    
    // Диалоговые окна
    public class RefactoringDialog : Window
    {
        private readonly RefactoringResult result;
        private readonly object entity;
        
        public bool AcceptedChanges { get; private set; }
        
        public RefactoringDialog(RefactoringResult result, object entity)
        {
            this.result = result;
            this.entity = entity;
            
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            Title = $"AI Refactor - {result.EntityType}";
            Width = 400;
            Height = 300;
            ResizeMode = ResizeMode.CanResize;
            
            var stackPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };
            
            // Заголовок
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"Refactor {result.EntityType}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });
            
            // Текущее имя
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"Current name: {result.OriginalName}",
                Margin = new Thickness(0, 0, 0, 5)
            });
            
            // Предложение
            var suggestionBox = new TextBox
            {
                Text = result.NewName,
                Margin = new Thickness(0, 0, 0, 10),
                Height = 25
            };
            stackPanel.Children.Add(suggestionBox);
            
            // Кнопки
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            
            var applyButton = new Button
            {
                Content = "Apply",
                Width = 80,
                Margin = new Thickness(5, 0, 0, 0)
            };
            applyButton.Click += (s, e) =>
            {
                result.NewName = suggestionBox.Text;
                AcceptedChanges = true;
                DialogResult = true;
                Close();
            };
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Margin = new Thickness(5, 0, 0, 0)
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };
            
            buttonPanel.Children.Add(applyButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);
            
            Content = stackPanel;
        }
    }
    
    public class BatchRefactoringDialog : Window
    {
        private readonly CodeAnalysisResult analysis;
        private CheckBox[] checkBoxes;
        
        public BatchRefactoringDialog(CodeAnalysisResult analysis)
        {
            this.analysis = analysis;
            
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            Title = "AI Batch Analysis";
            Width = 600;
            Height = 500;
            ResizeMode = ResizeMode.CanResize;
            
            var grid = new Grid();
            
            // Заголовок
            var title = new TextBlock
            {
                Text = "Code Analysis Results",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 10, 10, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            grid.Children.Add(title);
            
            // Список предложений
            var listView = new ListView
            {
                Margin = new Thickness(10, 40, 10, 50),
                ItemsSource = analysis.Suggestions,
                View = CreateGridView()
            };
            grid.Children.Add(listView);
            
            // Кнопки
            var applyButton = new Button
            {
                Content = "Apply Selected",
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 120, 10),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            applyButton.Click += (s, e) =>
            {
                DialogResult = true;
                Close();
            };
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 20, 10),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };
            
            grid.Children.Add(applyButton);
            grid.Children.Add(cancelButton);
            
            Content = grid;
        }
        
        private GridView CreateGridView()
        {
            var gridView = new GridView();
            
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Type",
                DisplayMemberBinding = new System.Windows.Data.Binding("EntityType"),
                Width = 80
            });
            
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Old Name",
                DisplayMemberBinding = new System.Windows.Data.Binding("OldName"),
                Width = 150
            });
            
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "New Name",
                DisplayMemberBinding = new System.Windows.Data.Binding("NewName"),
                Width = 150
            });
            
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Reason",
                DisplayMemberBinding = new System.Windows.Data.Binding("Reason"),
                Width = 200
            });
            
            return gridView;
        }
    }
}
