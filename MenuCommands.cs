// MenuCommands.cs
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;

namespace DnSpyAIRefactor
{
    [ExportMenuItem(Header = "AI Refactor Method", 
                   Icon = "Refactor", 
                   Group = MenuConstants.GROUP_CTX_DOCUMENTVIEWER_CODE,
                   Order = 100)]
    public class RefactorMethodCommand : MenuItemBase
    {
        private readonly RefactoringService refactoringService;
        private readonly RefactoringUI refactoringUI;
        
        [ImportingConstructor]
        public RefactorMethodCommand(RefactoringService refactoringService, RefactoringUI refactoringUI)
        {
            this.refactoringService = refactoringService;
            this.refactoringUI = refactoringUI;
        }
        
        public override void Execute(IMenuItemContext context)
        {
            if (context.CreatorObject.Guid != MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID)
                return;
            
            var methodNode = context.Find<IMethodNode>();
            if (methodNode == null)
                return;
            
            var method = methodNode.MethodDef;
            var ctx = context.Find<IDecompilerOutput>();
            
            if (method != null && ctx != null)
            {
                // Получаем контекст кода вокруг метода
                var contextCode = GetMethodContext(ctx, method);
                
                Task.Run(async () =>
                {
                    var result = await refactoringService.RefactorMethodAsync(ctx, method, contextCode);
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        refactoringUI.ShowRefactoringDialog(result, method);
                    });
                });
            }
        }
        
        public override bool IsEnabled(IMenuItemContext context)
        {
            return context.Find<IMethodNode>() != null;
        }
        
        private string GetMethodContext(IDecompilerOutput output, MethodDef method)
        {
            // Получаем текст из декомпилированного вывода
            // Это упрощенная версия - в реальности нужно парсить позиции
            return output.ToString();
        }
    }
    
    [ExportMenuItem(Header = "AI Refactor Class", 
                   Icon = "Class", 
                   Group = MenuConstants.GROUP_CTX_DOCUMENTVIEWER_CODE,
                   Order = 101)]
    public class RefactorClassCommand : MenuItemBase
    {
        private readonly RefactoringService refactoringService;
        private readonly RefactoringUI refactoringUI;
        
        [ImportingConstructor]
        public RefactorClassCommand(RefactoringService refactoringService, RefactoringUI refactoringUI)
        {
            this.refactoringService = refactoringService;
            this.refactoringUI = refactoringUI;
        }
        
        public override void Execute(IMenuItemContext context)
        {
            if (context.CreatorObject.Guid != MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID)
                return;
            
            var typeNode = context.Find<ITypeNode>();
            if (typeNode == null)
                return;
            
            var type = typeNode.TypeDef;
            var ctx = context.Find<IDecompilerOutput>();
            
            if (type != null && ctx != null)
            {
                var contextCode = GetTypeContext(ctx, type);
                
                Task.Run(async () =>
                {
                    var result = await refactoringService.RefactorClassAsync(ctx, type, contextCode);
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        refactoringUI.ShowRefactoringDialog(result, type);
                    });
                });
            }
        }
        
        public override bool IsEnabled(IMenuItemContext context)
        {
            return context.Find<ITypeNode>() != null;
        }
        
        private string GetTypeContext(IDecompilerOutput output, TypeDef type)
        {
            return output.ToString();
        }
    }
    
    [ExportMenuItem(Header = "AI Refactor Property", 
                   Icon = "Property", 
                   Group = MenuConstants.GROUP_CTX_DOCUMENTVIEWER_CODE,
                   Order = 102)]
    public class RefactorPropertyCommand : MenuItemBase
    {
        private readonly RefactoringService refactoringService;
        private readonly RefactoringUI refactoringUI;
        
        [ImportingConstructor]
        public RefactorPropertyCommand(RefactoringService refactoringService, RefactoringUI refactoringUI)
        {
            this.refactoringService = refactoringService;
            this.refactoringUI = refactoringUI;
        }
        
        public override void Execute(IMenuItemContext context)
        {
            if (context.CreatorObject.Guid != MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID)
                return;
            
            var propertyNode = context.Find<IPropertyNode>();
            if (propertyNode == null)
                return;
            
            var property = propertyNode.PropertyDef;
            var ctx = context.Find<IDecompilerOutput>();
            
            if (property != null && ctx != null)
            {
                var contextCode = GetPropertyContext(ctx, property);
                
                Task.Run(async () =>
                {
                    var result = await refactoringService.RefactorPropertyAsync(ctx, property, contextCode);
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        refactoringUI.ShowRefactoringDialog(result, property);
                    });
                });
            }
        }
        
        public override bool IsEnabled(IMenuItemContext context)
        {
            return context.Find<IPropertyNode>() != null;
        }
        
        private string GetPropertyContext(IDecompilerOutput output, PropertyDef property)
        {
            return output.ToString();
        }
    }
    
    [ExportMenuItem(Header = "AI Refactor Variable", 
                   Icon = "Variable", 
                   Group = MenuConstants.GROUP_CTX_DOCUMENTVIEWER_CODE,
                   Order = 103)]
    public class RefactorVariableCommand : MenuItemBase
    {
        private readonly RefactoringService refactoringService;
        private readonly RefactoringUI refactoringUI;
        
        [ImportingConstructor]
        public RefactorVariableCommand(RefactoringService refactoringService, RefactoringUI refactoringUI)
        {
            this.refactoringService = refactoringService;
            this.refactoringUI = refactoringUI;
        }
        
        public override void Execute(IMenuItemContext context)
        {
            // Для переменных нужен специальный контекст
            // В этом примере используем текущий выделенный текст
            var ctx = context.Find<IDecompilerOutput>();
            var textViewer = context.Find<ITextViewer>();
            
            if (textViewer != null && ctx != null)
            {
                var selectedText = textViewer.SelectedText;
                if (!string.IsNullOrEmpty(selectedText) && selectedText.Length < 100)
                {
                    var contextCode = GetSurroundingCode(textViewer, selectedText);
                    
                    Task.Run(async () =>
                    {
                        var result = await refactoringService.RefactorVariableAsync(
                            ctx, 
                            new Local { Name = selectedText }, 
                            contextCode);
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            refactoringUI.ShowRefactoringDialog(result, selectedText);
                        });
                    });
                }
            }
        }
        
        public override bool IsEnabled(IMenuItemContext context)
        {
            var textViewer = context.Find<ITextViewer>();
            return textViewer != null && !string.IsNullOrEmpty(textViewer.SelectedText);
        }
        
        private string GetSurroundingCode(ITextViewer textViewer, string selectedText)
        {
            // Получаем контекст вокруг выделения
            // Упрощенная реализация
            var position = textViewer.Selection.Start;
            var start = Math.Max(0, position - 500);
            var end = Math.Min(textViewer.Text.Length, position + 500);
            
            return textViewer.Text.Substring(start, end - start);
        }
    }
    
    [ExportMenuItem(Header = "AI Batch Refactor Module", 
                   Icon = "Batch", 
                   Group = MenuConstants.GROUP_CTX_DOCUMENTS_TREEVIEW,
                   Order = 100)]
    public class BatchRefactorCommand : MenuItemBase
    {
        private readonly RefactoringService refactoringService;
        private readonly RefactoringUI refactoringUI;
        
        [ImportingConstructor]
        public BatchRefactorCommand(RefactoringService refactoringService, RefactoringUI refactoringUI)
        {
            this.refactoringService = refactoringService;
            this.refactoringUI = refactoringUI;
        }
        
        public override void Execute(IMenuItemContext context)
        {
            if (context.CreatorObject.Guid != MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID)
                return;
            
            var moduleNode = context.Find<IModuleNode>();
            if (moduleNode == null)
                return;
            
            var module = moduleNode.Document.ModuleDef;
            
            Task.Run(async () =>
            {
                var analysis = await refactoringService.AnalyzeModuleAsync(module);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    refactoringUI.ShowBatchAnalysisDialog(analysis, module);
                });
            });
        }
        
        public override bool IsEnabled(IMenuItemContext context)
        {
            return context.Find<IModuleNode>() != null;
        }
    }
}
