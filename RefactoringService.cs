// RefactoringService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;

namespace DnSpyAIRefactor
{
    public class RefactoringService
    {
        private readonly OllamaService ollamaService;
        private readonly IDecompilerService decompilerService;
        
        public RefactoringService(OllamaService ollamaService, IDecompilerService decompilerService)
        {
            this.ollamaService = ollamaService;
            this.decompilerService = decompilerService;
        }
        
        public async Task<RefactoringResult> RefactorMethodAsync(
            IDecompilerOutput output, 
            MethodDef method, 
            string contextCode)
        {
            var result = new RefactoringResult();
            
            try
            {
                // Получаем предложение от ИИ
                var newName = await ollamaService.GenerateRenameSuggestionAsync(
                    contextCode,
                    method.Name,
                    "method",
                    $"Return type: {method.ReturnType}, Parameters: {method.Parameters.Count}");
                
                if (string.IsNullOrEmpty(newName) || newName == method.Name)
                {
                    result.Success = false;
                    result.Message = "No suitable name suggestion received";
                    return result;
                }
                
                // Создаем рефакторинг
                result.Success = true;
                result.OriginalName = method.Name;
                result.NewName = newName;
                result.EntityType = "method";
                result.SuggestedChanges = new List<CodeChange>
                {
                    new CodeChange
                    {
                        Entity = method,
                        OldName = method.Name,
                        NewName = newName,
                        ChangeType = "rename"
                    }
                };
                
                // Добавляем обновление сигнатуры
                result.SuggestedChanges.AddRange(
                    method.Parameters.Select(p => new CodeChange
                    {
                        Entity = p,
                        OldName = p.Name,
                        NewName = await SuggestParameterNameAsync(p, method),
                        ChangeType = "parameter_rename"
                    }));
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            
            return result;
        }
        
        public async Task<RefactoringResult> RefactorClassAsync(
            IDecompilerOutput output, 
            TypeDef type, 
            string contextCode)
        {
            var result = new RefactoringResult();
            
            try
            {
                var newName = await ollamaService.GenerateRenameSuggestionAsync(
                    contextCode,
                    type.Name,
                    "class",
                    $"Base type: {type.BaseType}, Methods: {type.Methods.Count}, Properties: {type.Properties.Count}");
                
                if (string.IsNullOrEmpty(newName) || newName == type.Name)
                {
                    result.Success = false;
                    result.Message = "No suitable name suggestion received";
                    return result;
                }
                
                result.Success = true;
                result.OriginalName = type.Name;
                result.NewName = newName;
                result.EntityType = "class";
                result.SuggestedChanges = new List<CodeChange>
                {
                    new CodeChange
                    {
                        Entity = type,
                        OldName = type.Name,
                        NewName = newName,
                        ChangeType = "rename"
                    }
                };
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            
            return result;
        }
        
        public async Task<RefactoringResult> RefactorPropertyAsync(
            IDecompilerOutput output, 
            PropertyDef property, 
            string contextCode)
        {
            var result = new RefactoringResult();
            
            try
            {
                var newName = await ollamaService.GenerateRenameSuggestionAsync(
                    contextCode,
                    property.Name,
                    "property",
                    $"Type: {property.PropertySig.GetRetType()}");
                
                if (string.IsNullOrEmpty(newName) || newName == property.Name)
                {
                    result.Success = false;
                    result.Message = "No suitable name suggestion received";
                    return result;
                }
                
                result.Success = true;
                result.OriginalName = property.Name;
                result.NewName = newName;
                result.EntityType = "property";
                result.SuggestedChanges = new List<CodeChange>
                {
                    new CodeChange
                    {
                        Entity = property,
                        OldName = property.Name,
                        NewName = newName,
                        ChangeType = "rename"
                    }
                };
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            
            return result;
        }
        
        public async Task<RefactoringResult> RefactorVariableAsync(
            IDecompilerOutput output, 
            Local local, 
            string contextCode)
        {
            var result = new RefactoringResult();
            
            try
            {
                var newName = await ollamaService.GenerateRenameSuggestionAsync(
                    contextCode,
                    local.Name,
                    "variable",
                    $"Type: {local.Type}");
                
                if (string.IsNullOrEmpty(newName) || newName == local.Name)
                {
                    result.Success = false;
                    result.Message = "No suitable name suggestion received";
                    return result;
                }
                
                result.Success = true;
                result.OriginalName = local.Name;
                result.NewName = newName;
                result.EntityType = "variable";
                result.SuggestedChanges = new List<CodeChange>
                {
                    new CodeChange
                    {
                        Entity = local,
                        OldName = local.Name,
                        NewName = newName,
                        ChangeType = "rename"
                    }
                };
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            
            return result;
        }
        
        public async Task<CodeAnalysisResult> AnalyzeModuleAsync(ModuleDef module)
        {
            try
            {
                // Декомпилируем весь модуль
                var output = new StringBuilderDecompilerOutput();
                decompilerService.Decompiler.WriteModule(output, module);
                
                var code = output.ToString();
                
                // Анализируем код
                return await ollamaService.AnalyzeCodeAsync(code);
            }
            catch (Exception ex)
            {
                return new CodeAnalysisResult
                {
                    Analysis = $"Analysis failed: {ex.Message}"
                };
            }
        }
        
        private async Task<string> SuggestParameterNameAsync(Parameter param, MethodDef method)
        {
            var context = $"Parameter in method {method.Name}, Type: {param.Type}";
            return await ollamaService.GenerateRenameSuggestionAsync("", param.Name, "parameter", context);
        }
    }
    
    public class RefactoringResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string OriginalName { get; set; } = "";
        public string NewName { get; set; } = "";
        public string EntityType { get; set; } = "";
        public List<CodeChange> SuggestedChanges { get; set; } = new List<CodeChange>();
    }
    
    public class CodeChange
    {
        public object Entity { get; set; }
        public string OldName { get; set; } = "";
        public string NewName { get; set; } = "";
        public string ChangeType { get; set; } = "";
        public int LineNumber { get; set; }
        public string Context { get; set; } = "";
    }
}
