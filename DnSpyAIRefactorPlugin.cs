// DnSpyAIRefactorPlugin.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Themes;
using System.ComponentModel.Composition;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Decompiler;

namespace DnSpyAIRefactor
{
    [Export(typeof(IPlugin))]
    [ExportAutoLoaded]
    public class DnSpyAIRefactorPlugin : IPlugin, IAutoLoaded
    {
        private readonly IThemeService themeService;
        private readonly IMenuService menuService;
        private readonly ITreeViewService treeViewService;
        private readonly IDocumentTreeService documentTreeService;
        private readonly IDecompilerService decompilerService;
        private readonly IMessageBoxService messageBoxService;
        
        private OllamaService ollamaService;
        private RefactoringService refactoringService;
        private RefactoringUI refactoringUI;

        [ImportingConstructor]
        public DnSpyAIRefactorPlugin(
            IThemeService themeService,
            IMenuService menuService,
            ITreeViewService treeViewService,
            IDocumentTreeService documentTreeService,
            IDecompilerService decompilerService,
            IMessageBoxService messageBoxService)
        {
            this.themeService = themeService;
            this.menuService = menuService;
            this.treeViewService = treeViewService;
            this.documentTreeService = documentTreeService;
            this.decompilerService = decompilerService;
            this.messageBoxService = messageBoxService;
        }

        public void OnLoaded()
        {
            // Инициализация сервисов
            ollamaService = new OllamaService("http://192.168.31.153:11434", "codellama:7b");
            refactoringService = new RefactoringService(ollamaService, decompilerService);
            refactoringUI = new RefactoringUI(refactoringService, messageBoxService);
            
            // Регистрация команд
            RegisterCommands();
            
            // Загрузка конфигурации
            LoadConfiguration();
        }

        private void RegisterCommands()
        {
            // Добавление пунктов меню
            menuService.AddContextMenuItem(
                new Guid("C5E3A6E0-1A2B-4C7D-9E8F-0A1B2C3D4E5F"),
                new RefactorMethodCommand(refactoringService, refactoringUI),
                MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID,
                GroupConstants.GROUP_CTX_DOCUMENTVIEWER_CODE,
                OrderConstants.ORDER_CTX_DOCUMENTVIEWER_CODE + 100);
            
            menuService.AddContextMenuItem(
                new Guid("C5E3A6E0-1A2B-4C7D-9E8F-0A1B2C3D4E5F"),
                new RefactorClassCommand(refactoringService, refactoringUI),
                MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID,
                GroupConstants.GROUP_CTX_DOCUMENTVIEWER_CODE,
                OrderConstants.ORDER_CTX_DOCUMENTVIEWER_CODE + 101);
            
            menuService.AddContextMenuItem(
                new Guid("C5E3A6E0-1A2B-4C7D-9E8F-0A1B2C3D4E5F"),
                new RefactorPropertyCommand(refactoringService, refactoringUI),
                MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID,
                GroupConstants.GROUP_CTX_DOCUMENTVIEWER_CODE,
                OrderConstants.ORDER_CTX_DOCUMENTVIEWER_CODE + 102);
            
            menuService.AddContextMenuItem(
                new Guid("C5E3A6E0-1A2B-4C7D-9E8F-0A1B2C3D4E5F"),
                new RefactorVariableCommand(refactoringService, refactoringUI),
                MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID,
                GroupConstants.GROUP_CTX_DOCUMENTVIEWER_CODE,
                OrderConstants.ORDER_CTX_DOCUMENTVIEWER_CODE + 103);
            
            menuService.AddContextMenuItem(
                new Guid("C5E3A6E0-1A2B-4C7D-9E8F-0A1B2C3D4E5F"),
                new BatchRefactorCommand(refactoringService, refactoringUI),
                MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID,
                GroupConstants.GROUP_CTX_DOCUMENTS_TREEVIEW,
                OrderConstants.ORDER_CTX_DOCUMENTS_TREEVIEW + 100);
        }

        private void LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "dnSpy",
                    "AI_Refactor_Config.json");
                
                if (File.Exists(configPath))
                {
                    var config = JsonConvert.DeserializeObject<Configuration>(
                        File.ReadAllText(configPath));
                    
                    if (config != null)
                    {
                        ollamaService.UpdateConfiguration(config.OllamaServer, config.Model);
                    }
                }
            }
            catch (Exception ex)
            {
                messageBoxService.Show($"Ошибка загрузки конфигурации: {ex.Message}");
            }
        }

        public void OnUnloaded()
        {
            // Сохранение конфигурации
            SaveConfiguration();
        }

        private void SaveConfiguration()
        {
            try
            {
                var config = new Configuration
                {
                    OllamaServer = ollamaService.ServerUrl,
                    Model = ollamaService.ModelName
                };
                
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "dnSpy",
                    "AI_Refactor_Config.json");
                
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            catch { }
        }
    }

    public class Configuration
    {
        public string OllamaServer { get; set; } = "http://192.168.31.153:11434";
        public string Model { get; set; } = "codellama:7b";
        public bool AutoSaveChanges { get; set; } = false;
        public bool CreateBackup { get; set; } = true;
        public double Temperature { get; set; } = 0.3;
    }
}
